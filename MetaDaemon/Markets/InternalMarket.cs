using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesRpc;
using BitcoinRpcSharp.Responses;
using BitcoinRpcSharp;
using WebDaemonShared;
using Casascius.Bitcoin;
using ApiHost;
using WebDaemonSharedTables;
using MetaData;

namespace MetaDaemon.Markets
{

	/// <summary>	An internal market. This always has BTC as the quote symbol </summary>
	///
	/// <remarks>	Paul, 05/02/2015. </remarks>
	public class InternalMarket : MarketBase
	{
		#if MONO
		public const decimal kMaxTransactionFactor = 10.0M / 100.0M;
		#else
		public const decimal kMaxTransactionFactor = 0.1M;
		#endif

		BitsharesAsset m_baseAsset;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="uid">			   	The UID. </param>
		/// <param name="base">			   	The base. </param>
		/// <param name="quote">		   	The quote. </param>
		/// <param name="bitshares">	   	The bitshares. </param>
		/// <param name="bitcoin">		   	The bitcoin. </param>
		/// <param name="bitsharesAccount">	The bitshares account. </param>
		public InternalMarket(	DaemonMySql daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, string bitsharesAccount) : 
								base(daemon, market, bitshares, bitcoin, bitsharesAccount)
		{
			m_baseAsset = m_bitshares.BlockchainGetAsset(CurrencyHelpers.ToBitsharesSymbol(market.GetBase()));
		}

		/// <summary>	Calculates the market prices and limits. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="market">				[in,out] The market. </param>
		/// <param name="bitsharesBalances">	The bitshares balances. </param>
		/// <param name="bitcoinBalance">   	The bitcoin balance. </param>
		public override void ComputeMarketPricesAndLimits(	ref MarketRow market, 
															Dictionary<int, ulong> bitsharesBalances, 
															decimal bitcoinBalance)
		{
			base.ComputeMarketPricesAndLimits(ref market, bitsharesBalances, bitcoinBalance);

			decimal baseBalance = 0;
			decimal quoteBalance = bitcoinBalance;

			if (bitsharesBalances.ContainsKey(m_baseAsset.id))
			{
				// only non zero balances return data, so this guard is necessary
				baseBalance = m_baseAsset.GetAmountFromLarimers(bitsharesBalances[m_baseAsset.id]);
			}

			#if MONO
			int dps=2;
			#else
			int dps = 8;
			#endif
			
			market.ask_max = Numeric.TruncateDecimal(baseBalance * kMaxTransactionFactor, dps);
			market.bid_max = Numeric.TruncateDecimal(quoteBalance * kMaxTransactionFactor, dps);
		}

		/// <summary>	Determine if we can deposit asset. </summary>
		///
		/// <remarks>	Paul, 15/02/2015. </remarks>
		///
		/// <param name="asset">	The asset. </param>
		///
		/// <returns>	true if we can deposit asset, false if not. </returns>
		public override bool CanDepositAsset(CurrencyTypes asset)
		{
			CurrencyTypes baseSymbol, quoteSymbol;
			CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(m_market.symbol_pair, out baseSymbol, out quoteSymbol);

			return baseSymbol == asset || quoteSymbol == asset;
		}

		/// <summary>	Handles the bitshares deposit described by kvp. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="kvp">	The kvp. </param>
		public override void HandleBitsharesDeposit(KeyValuePair<string, BitsharesLedgerEntry> kvp)
		{
			// get the btc address
			BitsharesLedgerEntry l = kvp.Value;
			string trxId = kvp.Key;

			SenderToDepositRow s2d = BitsharesTransactionToBitcoinAddress(l);

			try
			{
				string btcAddress = s2d.receiving_address;
				SendBitcoinsToDepositor(btcAddress, trxId, l.amount.amount, m_baseAsset, s2d.deposit_address, MetaOrderType.sell);
			}
			catch (Exception e)
			{
				// also lets now ignore this transaction so we don't keep failing
				RefundBitsharesDeposit(l.from_account, l.amount.amount, trxId, e.Message, m_baseAsset, s2d.deposit_address, MetaOrderType.sell);
			}
		}

		/// <summary>	Handles the bitcoin deposit described by t. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		public override void HandleBitcoinDeposit(TransactionSinceBlock t)
		{
			SenderToDepositRow s2d = GetBitsharesAccountFromBitcoinDeposit(t);

			try
			{
				SendBitAssetsToDepositor(t, m_baseAsset, s2d, MetaOrderType.buy);
			}
			catch (Exception e)
			{
				// also lets now ignore this transaction so we don't keep failing
				RefundBitcoinDeposit(t, e.Message, s2d, MetaOrderType.buy);
			}
		}

		/// <summary>	Executes the submit address action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionMessage">	  	Thrown when an API exception message error
		/// 											condition occurs. </exception>
		/// <exception cref="UnexpectedCaseException">	Thrown when an Unexpected Case error condition
		/// 											occurs. </exception>
		///
		/// <param name="receivingAddress">	The receiving address. </param>
		/// <param name="orderType">	   	Type of the order. </param>
		///
		/// <returns>	A SubmitAddressResponse. </returns>
		public override SubmitAddressResponse OnSubmitAddress(string receivingAddress, MetaOrderType orderType)
		{
			SubmitAddressResponse response;

			if (orderType == MetaOrderType.buy)
			{
				string accountName = receivingAddress;

				// check for theoretical validity
				if (!BitsharesWallet.IsValidAccountName(accountName))
				{
					throw new ApiExceptionInvalidAccount(accountName);
				}

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(accountName, m_market.uid);
				if (senderToDeposit == null)
				{
					// no dice, create a new entry

					// check for actual validity
					try
					{
						BitsharesAccount account = m_bitshares.WalletGetAccount(accountName);

						// generate a new bitcoin address and tie it to this account
						string depositAdress = m_bitcoin.GetNewAddress();
						senderToDeposit = m_daemon.InsertSenderToDeposit(account.name, depositAdress, m_market.uid);
					}
					catch (BitsharesRpcException)
					{
						throw new ApiExceptionInvalidAccount(accountName);
					}
				}

				response = new SubmitAddressResponse { deposit_address = senderToDeposit.deposit_address };
			}
			else if (orderType == MetaOrderType.sell)
			{
				string bitcoinAddress = receivingAddress;

				// validate bitcoin address
				byte[] check = Util.Base58CheckToByteArray(bitcoinAddress);
				if (check == null)
				{
					throw new ApiExceptionInvalidAddress(bitcoinAddress);
				}

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(bitcoinAddress, m_market.uid);
				if (senderToDeposit == null)
				{
					// generate a memo field to use instead
					senderToDeposit = m_daemon.InsertSenderToDeposit(bitcoinAddress, MarketBase.CreateMemo(bitcoinAddress, m_market.uid), m_market.uid);
				}

				response =	new SubmitAddressResponse 
							{ 
								deposit_address = m_bitsharesAccount, 
								memo = senderToDeposit.deposit_address 
							};
			}
			else
			{
				throw new UnexpectedCaseException();
			}
			
			return response;
		}
	}
}
