using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using BitcoinRpcSharp.Responses;
using BitsharesRpc;
using ApiHost;
using WebDaemonShared;
using Monsterer.Request;
using Monsterer.Util;
using Casascius.Bitcoin;
using MySqlDatabase;
using DatabaseTables;

namespace BtsOnrampDaemon
{
	interface IDummy { }

	public class ApiError
	{
		public string m_errorMsg;

		public ApiError(string msg)
		{
			m_errorMsg = msg;
		}
	}

	public class SenderToDepositRow : ICoreType
	{
		public string receiving_address;
		public string deposit_address;
	}

	public class DaemonApi : DaemonMySql
	{
		//const int kNumBitsharesConfirmations = 2;

		ApiServer<IDummy> m_server;
		//GlostenMilgromSimple m_glosten;

		public DaemonApi(RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string bitsharesAsset,
							string bitcoinDespositAddress,
							string databaseName, string databaseUser, string databasePassword,
							string listenAddress) : 
							base(bitsharesConfig, bitcoinConfig, bitsharesAccount, bitsharesAsset, bitcoinDespositAddress,
							databaseName, databaseUser, databasePassword)
		{
			m_server = new ApiServer<IDummy>(new string[] { listenAddress });
			m_server.ExceptionEvent += OnApiException;
			
			m_server.HandlePostRoute(Routes.kSubmitAddress, OnSubmitAddress, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			m_server.HandlePostRoute(Routes.kGetStats, OnGetStats, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
		}

		/// <summary>	Starts this object. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		public override void Start()
		{
			base.Start();

			m_server.Start();
		}

		/// <summary>	Executes the API exception action. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="sender">	The sender. </param>
		/// <param name="e">	 	The ExceptionWithCtx to process. </param>
		void OnApiException(object sender, ExceptionWithCtx e)
		{
			if (e.m_e is ApiException)
			{
				ApiException apiE = (ApiException)e.m_e;
				e.m_ctx.Respond<ApiError>(apiE.m_error);
			}
			else
			{
				e.m_ctx.Respond<ApiError>(new ApiExceptionGeneral().m_error);
			}
		}

		/// <summary>	Bitshares transaction to bitcoin address. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	A string. </returns>
		/*protected override string BitsharesTransactionToBitcoinAddress(BitsharesLedgerEntry l, BitsharesTransaction t)
		{
			// look up the BTS address this transaction was sent to
			BitsharesOperation op = t.trx.operations.First(o => o.type == BitsharesTransactionOp.deposit_op_type);

			string depositAddress = op.data.condition.data.owner;

			// look that address up in our map of sender->deposit address
			SenderToDepositRow senderToDeposit = GetSenderDepositFromDeposit(depositAddress);
			if (senderToDeposit != null)
			{
				return senderToDeposit.sender;
			}
			else
			{
				return null;
			}
		}*/

		/// <summary>	Bitshares transaction to bitcoin address. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	A string. </returns>
		protected override string BitsharesTransactionToBitcoinAddress(BitsharesLedgerEntry l, BitsharesTransaction t)
		{
			// look up the BTS address this transaction was sent to
			
			// look that address up in our map of sender->deposit address
			SenderToDepositRow senderToDeposit = GetSenderDepositFromDeposit(l.memo);
			if (senderToDeposit != null)
			{
				return senderToDeposit.receiving_address;
			}
			else
			{
				throw new RefundBitsharesException("Missing memo!");
			}
		}

		/// <summary>	Gets bitshares address from bitcoin deposit. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	The bitshares address from bitcoin deposit. </returns>
		protected override AddressOrAccountName GetBitsharesAddressFromBitcoinDeposit(TransactionSinceBlock t)
		{
			// look up the deposit address in our map of sender->deposit
			SenderToDepositRow senderToDeposit = GetSenderDepositFromDeposit(t.Address);
			if (senderToDeposit != null)
			{
				return new AddressOrAccountName { m_text = senderToDeposit.receiving_address, m_isAddress = false };
			}
			else
			{
				return null;
			}
		}

		/// <summary>	Handles the bitshares desposits. </summary>
		///
		/// <remarks>	Paul, 28/01/2015. </remarks>
		/*protected override void HandleBitsharesDesposits()
		{
			// which block do we start from
			uint lastBlockBitshares = GetLastBitsharesBlock();

			// which block do we end on
			GetInfoResponse info = m_bitshares.GetInfo();

			if (lastBlockBitshares == 0)
			{
				// default to current block
				lastBlockBitshares = info.blockchain_head_block_num;
			}

			// make sure we only consider transactions with the specified number of confirmations
			uint confirm = (uint)Math.Max(0, kNumBitsharesConfirmations - 1);
			for (uint b = lastBlockBitshares - confirm; b < info.blockchain_head_block_num - confirm; b++)
			{
				if (lastBlockBitshares < info.blockchain_head_block_num - 1)
				{
					Console.WriteLine("Syncing " + b + "/" + info.blockchain_head_block_num);
				}

				Dictionary<string, BitsharesTransaction>[] transactions = m_bitshares.BlockchainGetBlockTransactions(b);

				foreach (Dictionary<string, BitsharesTransaction> trx in transactions)
				{
					Debug.Assert(trx.Count == 1, "Got an unexpected data configuration!");

					string trxId = trx.First().Key;
					BitsharesTransaction trans = trx.First().Value;

					IEnumerable<BitsharesOperation> ops = trans.trx.operations.Where(	o => o.type == BitsharesTransactionOp.deposit_op_type && 
																						o.data.condition.asset_id == m_asset.id);

					// in theory there could a transaction with multiple outputs which are all different deposit addresses
					// that we have stored, but in practice we only support sending to one of them
					Dictionary<SenderToDepositRow, BitsharesOperation> opsForUs = new Dictionary<SenderToDepositRow, BitsharesOperation>();
					foreach (BitsharesOperation operation in ops)
					{
						SenderToDepositRow sender = m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE deposit_address=@d;", operation.data.condition.data.owner).FirstOrDefault();
						if (sender != null)
						{
							opsForUs[sender] = operation;
						}
					}

					if (opsForUs.Count() > 0)
					{
						if (opsForUs.Count() > 1)
						{
							// no idea how to refund this transaction, so just log it
							LogException(trxId, "Multiple deposit addresses within one transaction!", DateTime.UtcNow, DaemonTransactionType.bitsharesDeposit);
						}
						else
						{
							BitsharesOperation operation = opsForUs.First().Value;
							SenderToDepositRow sender = opsForUs.First().Key;

							//
							// finally! this transaction was sent to one of our deposit addresses!!!!
							// 

							// make sure we didn't already send bitcoins for this deposit
							if (!HasBitsharesDepositBeenCredited(trxId) && !IsTransactionIgnored(trxId))
							{
								// pull out where to send to
								string btcAddress = sender.sender;

								try
								{
									SendBitcoinsToDepositor(btcAddress, trxId, operation.data.amount);
								}
								catch (Exception e)
								{
									// problem sending bitcoins, lets log it
									LogException(trxId, e.Message, DateTime.UtcNow, DaemonTransactionType.bitsharesDeposit);

									// also lets now ignore this transaction so we don't keep failing
									RefundBitsharesDeposit("", operation.data.amount, trxId, e.Message);
								}
							}
						}
					}
				}
			}

			UpdateBitsharesBlock(info.blockchain_head_block_num);
		}*/

		/// <summary>	Bitcoin address is deposit address. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="bitcoinAddress">	The bitcoin address. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		protected override bool BitcoinAddressIsDepositAddress(string bitcoinAddress)
		{
			return GetSenderDepositFromDeposit(bitcoinAddress) != null;
		}

		/// <summary>	Inserts a sender to deposit. </summary>
		///
		/// <remarks>	Paul, 04/02/2015. </remarks>
		///
		/// <param name="receivingAddress">	The receiving address. </param>
		/// <param name="depositAddress">  	The deposit address. </param>
		/// <param name="memo">			   	(Optional) the memo. </param>
		///
		/// <returns>	A SenderToDepositRow. </returns>
		SenderToDepositRow InsertSenderToDeposit(string receivingAddress, string depositAddress)
		{
			m_database.Statement("INSERT INTO sender_to_deposit (receiving_address, deposit_address) VALUES(@a,@b);", receivingAddress, depositAddress);
			return	new SenderToDepositRow 
					{ 
						deposit_address = depositAddress, 
						receiving_address = receivingAddress
					};
		}

		/// <summary>	Gets sender deposit from deposit. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		///
		/// <returns>	The sender deposit from deposit. </returns>
		SenderToDepositRow GetSenderDepositFromDeposit(string depositAddress)
		{
			return m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE deposit_address=@d;", depositAddress).FirstOrDefault();
		}

		/// <summary>	Recompute transaction limits and prices. </summary>
		///
		/// <remarks>	Paul, 02/02/2015. </remarks>
		protected override void RecomputeTransactionLimitsAndPrices()
		{
			base.RecomputeTransactionLimitsAndPrices();

			StatsRow stats = m_database.Query<StatsRow>("SELECT * FROM stats;").FirstOrDefault();
			m_bidPrice = stats.bid_price;
			m_askPrice = stats.ask_price;
		}

		/// <summary>	Executes the submit address action. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <exception cref="ApiExceptionMessage">		   	Thrown when an API exception message error
		/// 												condition occurs. </exception>
		/// <exception cref="ApiExceptionMissingParameter">	Thrown when an API exception missing
		/// 												parameter error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnSubmitAddress(RequestContext ctx, IDummy dummy)
		{
			CurrencyTypes fromCurrency = RestHelpers.GetPostArg<CurrencyTypes, ApiExceptionMissingParameter>(ctx, WebForms.kFromCurrency);
			CurrencyTypes toCurrency = RestHelpers.GetPostArg<CurrencyTypes, ApiExceptionMissingParameter>(ctx, WebForms.kToCurrency);
			string receivingAddress = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kReceivingAddress);

			SubmitAddressResponse response;

			if (fromCurrency == CurrencyTypes.BTC && toCurrency == CurrencyTypes.bitBTC)
			{
				string accountName = receivingAddress;

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE receiving_address=@s;", accountName).FirstOrDefault();
				if (senderToDeposit == null || !BitsharesWallet.IsValidAccountName(accountName))
				{
					// no dice, create a new entry

					// validate bitshares account name
					try
					{
						BitsharesAccount account = m_bitshares.WalletGetAccount(accountName);

						// generate a new bitcoin address and tie it to this account
						string depositAdress = m_bitcoin.GetNewAddress();
						senderToDeposit = InsertSenderToDeposit(account.name, depositAdress);
					}
					catch (BitsharesRpcException)
					{
						throw new ApiExceptionMessage(accountName + " is not an existing account! Are you sure it is registered?");
					}
				}

				response = new SubmitAddressResponse { deposit_address = senderToDeposit.deposit_address };
			}
			else if (fromCurrency == CurrencyTypes.bitBTC && toCurrency == CurrencyTypes.BTC)
			{
				string bitcoinAddress = receivingAddress;

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE receiving_address=@s;", bitcoinAddress).FirstOrDefault();
				if (senderToDeposit == null)
				{
					// validate bitcoin address
					byte[] check = Util.Base58CheckToByteArray(bitcoinAddress);
					if (check == null)
					{
						throw new ApiExceptionMessage(bitcoinAddress + " is not a valid bitcoin address!");
					}

					// generate a memo field to use instead
					string start = "meta-";
					string memo = start + bitcoinAddress.Substring(0, BitsharesWallet.kBitsharesMaxMemoLength - start.Length);
					senderToDeposit = InsertSenderToDeposit(bitcoinAddress, memo);
				}

				response = new SubmitAddressResponse { deposit_address = m_bitsharesAccount, memo = senderToDeposit.deposit_address };
			}
			else
			{
				throw new ApiExceptionUnsupportedTrade(fromCurrency, toCurrency);
			}

			ctx.Respond<SubmitAddressResponse>(response);

			return null;
		}

		/// <summary>	Executes the get statistics action. </summary>
		///
		/// <remarks>	Paul, 30/01/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetStats(RequestContext ctx, IDummy dummy)
		{
			uint sinceTid = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);

			List<TransactionsRow> lastTransactions = m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE uid>@uid ORDER BY uid;", sinceTid);
			SiteStatsRow stats =	new SiteStatsRow
									{
										bid_price = m_bidPrice,
										ask_price = m_askPrice,
										max_bitassets = m_bitsharesDepositLimit,
										max_btc = m_bitcoinDepositLimit
									};

			ctx.Respond<StatsPacket>(new StatsPacket { m_lastTransactions = lastTransactions, m_stats = stats });

			return null;
		}
	}
}
