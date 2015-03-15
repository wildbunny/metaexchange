using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using System.Diagnostics;
using System.Threading;

using BitcoinRpcSharp.Responses;
using BitsharesRpc;
using ApiHost;
using WebDaemonShared;
using WebDaemonSharedTables;
using Monsterer.Request;
using Monsterer.Util;
using Casascius.Bitcoin;
using MySqlDatabase;
using MetaDaemon.Markets;
using MetaData;
using ServiceStack.Text;
using Pathfinder;

namespace MetaDaemon
{
	public interface IDummyDaemon { }

	public partial class MetaDaemonApi : DaemonMySql, IDisposable
	{
		AsyncPump m_scheduler;

		ApiServer<IDummyDaemon> m_server;
		SharedApi<IDummyDaemon> m_api;

		Dictionary<string, MarketBase> m_marketHandlers;
		Dictionary<int, BitsharesAsset> m_allBitsharesAssets;
		Dictionary<string, CurrenciesRow> m_allCurrencies;
		List<BitsharesMarket> m_allDexMarkets;

		string m_bitshaaresFeeAccount;
		string m_bitcoinFeeAddress;

		Task<string> m_lastCommand;
		
		public MetaDaemonApi(	RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
								string bitsharesAccount,
								string databaseName, string databaseUser, string databasePassword,
								string listenAddress,
								string bitcoinFeeAddress,
								string bitsharesFeeAccount,
								string adminUsernames,
								string masterSiteUrl,
								string masterSiteIp,
								AsyncPump scheduler) : 
								base(bitsharesConfig, bitcoinConfig, bitsharesAccount, adminUsernames,
								databaseName, databaseUser, databasePassword)
		{
			m_bitshaaresFeeAccount = bitsharesFeeAccount;
			m_bitcoinFeeAddress = bitcoinFeeAddress;
			m_masterSiteUrl = masterSiteUrl.TrimEnd('/');

			m_scheduler = scheduler;

			ServicePointManager.ServerCertificateValidationCallback = Validator;

			Serialisation.Defaults();

			// don't ban on exception here because we'll only end up banning the webserver!
			m_server = new ApiServer<IDummyDaemon>(new string[] { listenAddress }, null, false, eDdosMaxRequests.Ignore, eDdosInSeconds.One);

			m_api = new SharedApi<IDummyDaemon>(m_dataAccess);
			m_server.ExceptionEvent += m_api.OnApiException;
			
			// only allow the main site to post to us
			m_server.SetIpLock(masterSiteIp);

			m_marketHandlers = new Dictionary<string,MarketBase>();

			// get all market pegged assets
			m_allBitsharesAssets = m_bitshares.BlockchainListAssets("", int.MaxValue).Where(a => a.issuer_account_id <= 0).ToDictionary(a => a.id);

			// get all active markets containing those assets
			m_allDexMarkets = m_bitshares.BlockchainListMarkets().Where(m => m.last_error == null &&
																		m_allBitsharesAssets.ContainsKey(m.base_id) &&
																		m_allBitsharesAssets.ContainsKey(m.quote_id)).ToList();

			m_allCurrencies = m_dataAccess.GetAllCurrencies();

			List<MarketRow> markets = GetAllMarkets();
			foreach (MarketRow r in markets)
			{
				m_marketHandlers[r.symbol_pair] = CreateHandlerForMarket(r);
			}
			
			m_server.HandlePostRoute(Routes.kSubmitAddress,				OnSubmitAddress, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandleGetRoute(Routes.kGetAllMarkets,				m_api.OnGetAllMarkets, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
									  SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		/// <summary>	Starts this object. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		public override void Start()
		{
			base.Start();

			m_server.Start();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
		/// resources.
		/// </summary>
		///
		/// <remarks>	Paul, 10/02/2015. </remarks>
		public void Dispose()
		{
			m_server.Dispose();
		}

		/// <summary>	Creates handler for market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="UnexpectedCaseException">	Thrown when an Unexpected Case error condition
		/// 											occurs. </exception>
		///
		/// <param name="market">	The market. </param>
		///
		/// <returns>	The new handler for market. </returns>
		MarketBase CreateHandlerForMarket(MarketRow market)
		{
			CurrenciesRow @base, quote;
			CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(market.symbol_pair, m_allCurrencies, out @base, out quote);

			if ( CurrencyHelpers.IsBitsharesAsset(@base) && !CurrencyHelpers.IsBitsharesAsset(quote))
			{
				return new InternalMarket(this, market, m_bitshares, m_bitcoin, m_bitsharesAccount, @base);
			}
			else if (!CurrencyHelpers.IsBitsharesAsset(@base) && CurrencyHelpers.IsBitsharesAsset(quote))
			{
				return new InternalMarket(this, market, m_bitshares, m_bitcoin, m_bitsharesAccount, @quote);
			}
			else
			{
				throw new UnexpectedCaseException();
			}
		}

		/// <summary>	Check handlers. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		void CheckMarketHandlers(Dictionary<string, MarketRow> allMarkets)
		{
			// make sure we have handlers for all markets
			foreach (KeyValuePair<string, MarketRow> kvp in allMarkets)
			{
				MarketRow market = kvp.Value;

				if (!m_marketHandlers.ContainsKey(market.symbol_pair))
				{
					m_marketHandlers[market.symbol_pair] = CreateHandlerForMarket(market);
				}
			}
		}

		/// <summary>	Recompute transaction limits and prices. </summary>
		///
		/// <remarks>	Paul, 30/01/2015. </remarks>
		virtual protected void RecomputeTransactionLimitsAndPrices(Dictionary<string, MarketRow> allMarkets)
		{
			// get balances for both wallets
			Dictionary<int, ulong> bitsharesBalances = m_bitshares.WalletAccountBalance(m_bitsharesAccount)[m_bitsharesAccount];
			decimal bitcoinBalance = m_bitcoin.GetBalance("", kBitcoinConfirms);

			// update all the limits in our handlers
			foreach (KeyValuePair<string, MarketBase> kvp in m_marketHandlers)
			{
				MarketRow market = allMarkets[kvp.Key];

				// compute new limits and prices for this market
				kvp.Value.ComputeMarketPricesAndLimits(ref market, bitsharesBalances, bitcoinBalance);

				// write them back out
				UpdateMarketInDatabase(market);
			}
		}

		/// <summary>	Handles the price setting. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <param name="l">	  	The BitsharesLedgerEntry to process. </param>
		/// <param name="handler">	The handler. </param>
		/// <param name="market"> 	The market. </param>
		void HandlePriceSetting(string[] parts, BitsharesLedgerEntry l, MarketBase handler, MarketRow market)
		{
			// parse
			
			if (parts[0] == kSetPricesMemoStart)
			{
				if (parts[1] == market.symbol_pair)
				{
					// setting is for this market!
					decimal basePrice = decimal.Parse(parts[2]);
					decimal quotePrice = decimal.Parse(parts[3]);

					// go do it!
					handler.SetPricesFromSingleUnitQuantities(basePrice, quotePrice, market.flipped, market);
				}
			}
		}

		/// <summary>	Handles the command. </summary>
		///
		/// <remarks>	Paul, 26/02/2015. </remarks>
		///
		/// <param name="l">	  	The BitsharesLedgerEntry to process. </param>
		/// <param name="handler">	The handler. </param>
		/// <param name="market"> 	The market. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool HandleCommand(BitsharesLedgerEntry l, MarketBase handler, MarketRow market, string trxid)
		{
			if (m_adminUsernames.Contains(l.from_account))
			{
				try
				{
					string[] parts = l.memo.Split(' ');

					if (l.memo.StartsWith(kSetPricesMemoStart))
					{
						HandlePriceSetting(parts, l, handler, market);
						return true;
					}
					else if (l.memo.StartsWith(kWithdrawMemo))
					{
						// process withdrawal
						if (parts[0] == kWithdrawMemo)
						{
							// make sure we didn't already process this transaction!
							if (!m_dataAccess.IsWithdrawalProcessed(trxid))
							{
								decimal amount = decimal.Parse(parts[1]);
								CurrenciesRow type = CurrencyHelpers.FromSymbol(parts[2], m_allCurrencies);
								string to;

								string txid;
								if ( !CurrencyHelpers.IsBitsharesAsset(type) )
								{
									to = m_dataAccess.GetStats().bitcoin_withdraw_address;
									Debug.Assert(to != null);

									txid = m_bitcoin.SendToAddress(to, amount);
								}
								else
								{
									to = l.from_account;
									BitsharesTransactionResponse response = m_bitshares.WalletTransfer(amount, CurrencyHelpers.ToBitsharesSymbol(type), m_bitsharesAccount, to);
									txid = response.record_id;
								}

								// log in DB
								m_dataAccess.InsertWithdrawal(trxid, txid, type.ToString(), amount, to, DateTime.UtcNow);
							}

							return true;
						}
					}
				}
				catch (Exception e)
				{
					LogGeneralException(e.ToString());
				}
			}

			return false;
		}

		/// <summary>	Updates this object. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		async public override void Update()
		{
			try
			{
				Dictionary<string, MarketRow> allMarkets = GetAllMarkets().ToDictionary(m => m.symbol_pair);
				m_allCurrencies = m_dataAccess.GetAllCurrencies();

				// create any handlers we need for new markets
				CheckMarketHandlers(allMarkets);

				// get all markets
				RecomputeTransactionLimitsAndPrices(allMarkets);

				//
				// handle bitshares->bitcoin
				//

				Dictionary<string, BitsharesLedgerEntry> bitsharesDeposits = HandleBitsharesDesposits();

				//
				// handle bitcoin->bitshares
				// 

				List<TransactionSinceBlock> bitcoinDeposits = HandleBitcoinDeposits();

				//
				// process bitshares deposits
				//

				uint siteLastTid = m_dataAccess.GetSiteLastTransactionUid();
				
				foreach (KeyValuePair<string, BitsharesLedgerEntry> kvpDeposit in bitsharesDeposits)
				{
					// figure out which market each deposit belongs to
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						BitsharesLedgerEntry l = kvpDeposit.Value;
						MarketRow m = allMarkets[kvpHandler.Key];
						BitsharesAsset depositAsset = m_allBitsharesAssets[l.amount.asset_id];

						if (!HandleCommand(l, kvpHandler.Value, m, kvpDeposit.Key))
						{
							if (IsDepositForMarket(l.memo, m.symbol_pair))
							{
								// make sure the deposit is for this market!
								if (kvpHandler.Value.CanDepositAsset(CurrencyHelpers.FromBitsharesSymbol(depositAsset.symbol, m_allCurrencies)))
								{
									kvpHandler.Value.HandleBitsharesDeposit(kvpDeposit);
								}
							}
						}
					}

					// this needs to happen for every transaction
					RecomputeTransactionLimitsAndPrices(allMarkets);
				}

				//
				// process bitcoin deposits
				// 

				List<TransactionsRow> pendingTransactions = m_dataAccess.GetAllPendingTransactions();

				foreach (TransactionSinceBlock deposit in bitcoinDeposits)
				{
					// figure out which market each deposit belongs to
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						if (IsDepositForMarket(deposit.Address, allMarkets[kvpHandler.Key].symbol_pair))
						{
							kvpHandler.Value.HandleBitcoinDeposit(deposit);
						}
					}

					// this needs to happen for every transaction
					RecomputeTransactionLimitsAndPrices(allMarkets);
				}

				//
				// handle changes in transaction status
				//

				List<TransactionsRow> updatedTrans = new List<TransactionsRow>();
				foreach (TransactionsRow pending in pendingTransactions)
				{
					TransactionsRow updated = m_dataAccess.GetTransaction(pending.received_txid);
					if (updated.status != MetaOrderStatus.pending)
					{
						updatedTrans.Add(updated);
					}
				}
				
				//
				// push any new transactions, make sure site acknowledges receipt
				//

				uint latestTid = m_dataAccess.GetLastTransactionUid();
				if (latestTid > siteLastTid || updatedTrans.Count > 0)
				{
					List<TransactionsRow> newTrans = m_dataAccess.GetAllTransactionsSince(siteLastTid);

					// lump them together
					newTrans.AddRange(updatedTrans);

					// send 'em all
					string result = await ApiPush<List<TransactionsRow>>(Routes.kPushTransactions, newTrans);
					if (bool.Parse(result))
					{
						m_dataAccess.UpdateSiteLastTransactionUid(latestTid);
					}
					else
					{
						throw new Exception("API push response unknown! " + result);
					}
				}

				//
				// push market updates
				//

				foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
				{
					if (kvpHandler.Value.m_IsDirty)
					{
						m_dataAccess.UpdateMarketInDatabase(kvpHandler.Value.m_Market);

						ApiPush<MarketRow>(Routes.kPushMarket, kvpHandler.Value.m_Market);

						kvpHandler.Value.m_IsDirty = false;
					}
				}

				//
				// push fee collections
				// 

				if (m_bitcoinFeeAddress != null && m_bitshaaresFeeAccount != null)
				{
					uint lastFeeId = m_dataAccess.GetSiteLastFeeUid();

					// collect our fees
					foreach (KeyValuePair<string, MarketBase> kvpHandler in m_marketHandlers)
					{
						kvpHandler.Value.CollectFees(m_bitcoinFeeAddress, m_bitshaaresFeeAccount);
					}

					// keep the site up to date, make sure it acknowledges receipt
					uint latestFeeId = m_dataAccess.GetLastFeeCollectionUid();
					if (latestFeeId > lastFeeId)
					{
						List<FeeCollectionRow> fees = m_dataAccess.GetFeeCollectionsSince(lastFeeId);
						string result = await ApiPush<List<FeeCollectionRow>>(Routes.kPushFees, fees);
						if (bool.Parse(result))
						{
							m_dataAccess.UpdateSiteLastFeeUid(latestFeeId);
						}
						else
						{
							throw new Exception("API push response unknown! " + result);
						}
					}
				}

				//
				// wait for a stop command to exit gracefully
				//

				if (m_lastCommand == null)
				{
					m_lastCommand = ReadConsoleAsync();

					string command = await m_lastCommand;

					// remember we never get here unless a command was entered
				
					Console.WriteLine("got command: " + command);

					if (command == "stop")
					{
						m_scheduler.Dispose();
					}

					m_lastCommand = null;
				}
			}
			catch (Exception e)
			{
				LogGeneralException(e.ToString());
			}
		}

		/// <summary>	Reads console asynchronous. </summary>
		///
		/// <remarks>	Paul, 13/03/2015. </remarks>
		///
		/// <param name="cancel">	The cancel. </param>
		///
		/// <returns>	The console asynchronous. </returns>
		public static Task<string> ReadConsoleAsync()
		{
			return Task.Run(() => Console.ReadLine());
		}

		/// <summary>	Gets the API server. </summary>
		///
		/// <value>	The m API server. </value>
		public ApiServer<IDummyDaemon> m_ApiServer
		{
			get { return m_server; }
		}

		/// <summary>	Gets all dex markets. </summary>
		///
		/// <value>	The m all dex markets. </value>
		public List<BitsharesMarket> m_AllDexMarkets
		{
			get { return m_allDexMarkets; }
		}

		/// <summary>	Gets all dex assets. </summary>
		///
		/// <value>	The m all dex assets. </value>
		public Dictionary<int, BitsharesAsset> m_AllBitsharesAssets
		{
			get { return m_allBitsharesAssets; }
		}

		/// <summary>	Gets all currencies. </summary>
		///
		/// <value>	The m all currencies. </value>
		public Dictionary<string, CurrenciesRow> m_AllCurrencies
		{
			get { return m_allCurrencies; }
		}
	}
}
