using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using BitsharesRpc;
using BitcoinRpcSharp;
using BitsharesCore;
using Casascius.Bitcoin;
using DatabaseTables;
using BitcoinRpcSharp.Responses;
using WebDaemonShared;

namespace BtsOnrampDaemon
{

	public class AddressOrAccountName
	{
		public string m_text;
		public bool m_isAddress;
	}

	abstract public class DaemonBase
	{
		const int kSleepTimeSeconds = 1;
		const int kBitcoinConfirms = 1;
		public const decimal kMaxTransactionFactor = 10.0M / 100.0M;
		const string kFundingMemo = "FUND";
		
		protected BitsharesWallet m_bitshares;
		protected BitcoinWallet m_bitcoin;

		protected BitsharesAsset m_asset;

		protected string m_bitsharesAccount;
		protected string m_bitsharesAsset;
		protected string m_bitcoinDespoitAddress;

		protected byte m_addressByteType;

		protected decimal m_bitcoinDepositLimit = 0.01M;
		protected decimal m_bitsharesDepositLimit = 0.01M;
		protected decimal m_bidPrice;
		protected decimal m_askPrice;
		protected decimal m_bitsharesBalance;
		protected decimal m_bitcoinBalance;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 17/01/2015. </remarks>
		///
		/// <param name="bitsharesConfig">		 	The bitshares configuration. </param>
		/// <param name="bitcoinConfig">		 	The bitcoin configuration. </param>
		/// <param name="bitsharesAccount">		 	The bitshares account. </param>
		/// <param name="bitsharesAsset">		 	The bitshares asset. </param>
		/// <param name="bitcoinDespositAddress">	The bitcoin desposit address. </param>
		public DaemonBase(	RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string bitsharesAsset,
							string bitcoinDespositAddress)
		{
			m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			m_bitcoin = new BitcoinWallet(bitcoinConfig.m_url, bitcoinConfig.m_rpcUser, bitcoinConfig.m_rpcPassword, false);

			m_bitsharesAccount = bitsharesAccount;
			m_bitsharesAsset = bitsharesAsset;
			m_bitcoinDespoitAddress = bitcoinDespositAddress;

			m_asset = m_bitshares.BlockchainGetAsset(bitsharesAsset);

			m_addressByteType = (byte)(bitcoinConfig.m_useTestnet ? AltCoinAddressTypeBytes.BitcoinTestnet : AltCoinAddressTypeBytes.Bitcoin);
		}

		protected abstract uint GetLastBitsharesBlock();
		protected abstract void UpdateBitsharesBlock(uint blockNum);
		protected abstract bool HasBitsharesDepositBeenCredited(string trxId);
		protected abstract void MarkBitsharesDespositAsCreditedStart(string bitsharesTxId);
		protected abstract void MarkBitsharesDespositAsCreditedEnd(string bitsharesTxId, string bitcoinTxId, decimal amount);
		protected abstract bool IsTransactionIgnored(string txid);
		protected abstract void IgnoreTransaction(string txid);
		protected abstract void LogException(string txid, string message, DateTime date, DaemonTransactionType type);
		protected abstract void MarkTransactionAsRefundedStart(string receivedTxid);
		protected abstract void MarkTransactionAsRefundedEnd(string receivedTxid, string sentTxid, decimal amount, DaemonTransactionType type, string notes);
		protected abstract void LogGeneralException(string message);

		/// <summary>	This is virtual because implementors might like a different way  </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <param name="account">	The account. </param>
		///
		/// <returns>	A string. </returns>
		/*protected virtual string BitsharesTransactionToBitcoinAddress(BitsharesLedgerEntry l)
		{
			// THIS CANNOT WORK DUE TO MEMO SIZE = 19 bytes!!!!!!!
			

			// expect the BTC address to be inside the memo somewhere
			string[] memo = l.memo.Split(' ');
			AddressBase address = null;
			foreach (string s in memo)
			{
				try
				{
					address = new AddressBase(s);
					break;
				}
				catch (ArgumentException){}
			}

			if (address == null)
			{
				throw new RefundBitsharesException("Unable to find desired bitcoin address in transction memo!");
			}

			return address.AddressBase58;
		}*/

		/// <summary>	Bitshares transaction to bitcoin address. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <exception cref="RefundBitsharesException">	Thrown when a Refund Bitshares error condition
		/// 											occurs. </exception>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	A string. </returns>
		protected virtual string BitsharesTransactionToBitcoinAddress(BitsharesLedgerEntry l, BitsharesTransaction t)
		{
			try
			{
				// get the public key of the sender
				BitsharesAccount account = GetAccountFromLedger(l.from_account);

				// turn into into a bitshares address
				return BitsharesAccountToBitcoinAddress(account);
			}
			catch (BitsharesRpcException)
			{
				throw new RefundBitsharesException("Unregistered acct!");
			}
		}

		/// <summary>	Bitshares account to bitcoin address. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="account">	The account. </param>
		///
		/// <returns>	A string. </returns>
		protected string BitsharesAccountToBitcoinAddress(BitsharesAccount account)
		{
			// turn that into a BTC address
			BitsharesPubKey pubKey = new BitsharesPubKey(account.active_key_history.Last().Values.Last());
			return pubKey.ToBitcoinAddress(true, m_addressByteType);
		}

		/// <summary>	Gets account from ledger. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		///
		/// <returns>	The account from ledger. </returns>
		BitsharesAccount GetAccountFromLedger(string fromAccount)
		{
			return m_bitshares.WalletGetAccount(fromAccount);
		}

		/// <summary>	Refund bitshares deposit. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="fromAccount">	from account. </param>
		/// <param name="deposit">	  	The deposit. </param>
		/// <param name="depositId">  	Identifier for the deposit. </param>
		/// <param name="memo">		  	The memo. </param>
		protected virtual void RefundBitsharesDeposit(string fromAccount, ulong larimers, string depositId, string memo)
		{
			// make sure failures after this point don't result in multiple refunds
			MarkTransactionAsRefundedStart(depositId);

			BitsharesTransactionResponse response;
			decimal amount = m_asset.GetAmountFromLarimers(larimers);

			try
			{
				BitsharesAccount account = GetAccountFromLedger(fromAccount);
				response = m_bitshares.WalletTransfer(amount, m_asset.symbol, m_bitsharesAccount, fromAccount, memo);
			}
			catch (BitsharesRpcException)
			{
				BitsharesTransaction t = m_bitshares.BlockchainGetTransaction(depositId);

				// get the sender's address from the balance id
				BitsharesOperation op = t.trx.operations.First(o => o.type == BitsharesTransactionOp.withdraw_op_type);

				BitsharesBalanceRecord balance = m_bitshares.GetBalance(op.data.balance_id);
				string senderAddress = balance.condition.data.owner;

				
				response = m_bitshares.WalletTransferToAddress(amount, m_asset.symbol, m_bitsharesAccount, senderAddress, memo);
			}

			MarkTransactionAsRefundedEnd(depositId, response.record_id, amount, DaemonTransactionType.bitsharesRefund, memo);
		}

		/// <summary>	Refund bitcoin deposit. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		protected virtual void RefundBitcoinDeposit(TransactionSinceBlock t, string notes)
		{
			MarkTransactionAsRefundedStart(t.TxId);

			// get public key out of transaction
			string firstPubKey = GetAllPubkeysFromBitcoinTransaction(t.TxId).First();
			PublicKey pk = new PublicKey(firstPubKey, m_addressByteType);

			// refund deposit
			string sentTxid = m_bitcoin.SendToAddress(pk.AddressBase58, t.Amount);

			// mark as such
			MarkTransactionAsRefundedEnd(t.TxId, sentTxid, t.Amount, DaemonTransactionType.bitcoinRefund, notes);
		}

		/// <summary>	Default implementation sends an exactly matching bitcoin transaction to the depositor </summary>
		///
		/// <remarks>	Paul, 28/01/2015. </remarks>
		///
		/// <exception cref="RefundBitsharesException">	Thrown when a Refund Bitshares error condition
		/// 											occurs. </exception>
		///
		/// <param name="btcAddress">	The btc address. </param>
		/// <param name="trxId">	 	Identifier for the trx. </param>
		/// <param name="amount">	 	The amount. </param>
		///
		/// <returns>	A string. </returns>
		protected virtual string SendBitcoinsToDepositor(string btcAddress, string trxId, ulong amount)
		{
			// make sure failures after this point dont result in multiple credits
			MarkBitsharesDespositAsCreditedStart(trxId);

			// get the BTC amount we need to transfer
			decimal btcToTransfer = m_asset.GetAmountFromLarimers(amount) * m_bidPrice;

			if (btcToTransfer > m_bitsharesDepositLimit)
			{
				throw new RefundBitsharesException("Over " + m_bitsharesDepositLimit + " " + m_asset.symbol + "!");
			}

			// do the transfer
			string txid = m_bitcoin.SendToAddress(btcAddress, btcToTransfer);

			// mark this in our records
			MarkBitsharesDespositAsCreditedEnd(trxId, txid, btcToTransfer);

			return txid;
		}

		/// <summary>	Handles the bitshares desposits. </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <exception cref="UnsupportedTransactionException">	Thrown when an Unsupported Transaction
		/// 													error condition occurs. </exception>
		protected virtual void HandleBitsharesDesposits()
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

			// get all relevant bitshares deposits
			List<BitsharesWalletTransaction> assetTransactions = m_bitshares.WalletAccountTransactionHistory(m_bitsharesAccount,
																												m_bitsharesAsset,
																												0,
																												lastBlockBitshares,
																												info.blockchain_head_block_num);

			IEnumerable<BitsharesWalletTransaction> assetDeposits = assetTransactions.Where(t => t.is_confirmed &&
																							t.ledger_entries.Any(	l => l.to_account == m_bitsharesAccount && 
																													l.from_account != l.to_account && 
																													l.memo != kFundingMemo &&
																													l.from_account != BitsharesWallet.kNetworkAccount));
			foreach (BitsharesWalletTransaction t in assetDeposits)
			{
				IEnumerable<BitsharesLedgerEntry> deposits = t.ledger_entries.Where(l => l.to_account == m_bitsharesAccount);

				if (deposits.Count() == 1)
				{
					BitsharesLedgerEntry l = deposits.First();

					// make sure we didn't already send bitcoins for this deposit
					if (!HasBitsharesDepositBeenCredited(t.trx_id) && !IsTransactionIgnored(t.trx_id))
					{
						try
						{
							///
							/// STILL NEED A WAY TO GET SENDER BITCOIN ADDRESS FOR UNREGISTERED ACCOUNTS
							/// 
							// look up the transaction proper
							BitsharesTransaction fullT = m_bitshares.BlockchainGetTransaction(t.trx_id);

							// get the btc address
							string btcAddress = BitsharesTransactionToBitcoinAddress(l, fullT);
							
							try
							{
								SendBitcoinsToDepositor(btcAddress, t.trx_id, l.amount.amount);
							}
							catch (Exception e)
							{
								// problem sending bitcoins, lets log it
								LogException(t.trx_id, e.Message, DateTime.UtcNow, DaemonTransactionType.bitsharesDeposit);

								// also lets now ignore this transaction so we don't keep failing
								RefundBitsharesDeposit(l.from_account, l.amount.amount, t.trx_id, e.Message);
							}
						}
						catch (RefundBitsharesException r)
						{
							// were unable to get a bitcoin address from the bitshares account, so refund the transaction
							RefundBitsharesDeposit(l.from_account, l.amount.amount, t.trx_id, r.Message);
						}
					}
				}
				else
				{
					// fail with unhandled case
					throw new UnsupportedTransactionException(t.trx_id);
				}
			}

			UpdateBitsharesBlock(info.blockchain_head_block_num);
		}

		protected abstract string GetLastBitcoinBlockHash();
		protected abstract void UpdateBitcoinBlockHash(string lastBlock);
		protected abstract bool HasBitcoinDespoitBeenCredited(string txid);
		protected abstract void MarkBitcoinDespositAsCreditedStart(string bitcoinTxid);
		protected abstract void MarkBitcoinDespositAsCreditedEnd(string bitcoinTxid, string bitsharesTrxId, decimal amount);
		protected virtual bool BitcoinAddressIsDepositAddress(string bitcoinAddress)
		{
			return bitcoinAddress == m_bitcoinDespoitAddress;
		}

		/// <summary>	Gets all pubkeys from bitcoin transactions in this collection. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="txid">	The txid. </param>
		///
		/// <returns>
		/// An enumerator that allows foreach to be used to process all pubkeys from bitcoin transactions
		/// in this collection.
		/// </returns>
		IEnumerable<string> GetAllPubkeysFromBitcoinTransaction(string txid)
		{
			DecodedRawTransaction rawDeposit = m_bitcoin.GetRawTransaction(txid, 1);
			return rawDeposit.VIn.Select(vin => vin.ScriptSig.Asm.Split(' ')[1]);
		}

		/// <summary>	Take the given transaction, pull out the first input and get the public key, 
		/// 			turn that into a bitshares address </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	The bitshares address from bitcoin deposit. </returns>
		protected virtual AddressOrAccountName GetBitsharesAddressFromBitcoinDeposit(TransactionSinceBlock t)
		{
			IEnumerable<string> allPubKeys = GetAllPubkeysFromBitcoinTransaction(t.TxId);

			// this is probably ok to leave out because if the user imports their whole wallet, they will likely
			// have all the PKs they need since bitcoin pregenerates 100 or so of them. worst case the user
			// can re-import their wallet into bitshares to get access to missing transcations
			/*if (allPubKeys.Distinct().Count() > 1)
			{
				// can't handle more than one sender case
				throw new MultiplePublicKeysException();
			}*/

			string publicKey = allPubKeys.First();
			BitsharesPubKey btsPk = BitsharesPubKey.FromBitcoinHex(publicKey, m_addressByteType);
			return new AddressOrAccountName { m_text = btsPk.m_Address, m_isAddress = true };
		}

		/// <summary>	Compute and send the correct amount of bitassets to the depositor
		/// 			given the deposit transaction </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		protected virtual BitsharesTransactionResponse SendBitAssetsToDepositor(TransactionSinceBlock t)
		{
			// make sure failures after this point do not result in repeated sending
			MarkBitcoinDespositAsCreditedStart(t.TxId);

			if (t.Amount > m_bitcoinDepositLimit)
			{
				throw new RefundBitcoinException("Over " + Numeric.Format2Dps(m_bitcoinDepositLimit) + " BTC!");
			}

			AddressOrAccountName bitsharesAddress = GetBitsharesAddressFromBitcoinDeposit(t);

			BitsharesTransactionResponse bitsharesTrx;
			decimal amount = (1 / m_askPrice) * t.Amount;

			if (bitsharesAddress.m_isAddress)
			{
				bitsharesTrx = m_bitshares.WalletTransferToAddress(amount, m_asset.symbol, m_bitsharesAccount, bitsharesAddress.m_text);
			}
			else
			{
				bitsharesTrx = m_bitshares.WalletTransfer(amount, m_asset.symbol, m_bitsharesAccount, bitsharesAddress.m_text);
			}

			MarkBitcoinDespositAsCreditedEnd(t.TxId, bitsharesTrx.record_id, amount);

			return bitsharesTrx;
		}

		/// <summary>	Handles the bitcoin deposits. </summary>
		///
		/// <remarks>	Paul, 23/12/2014. </remarks>
		void HandleBitcoinDeposits()
		{
			long blockHeight = m_bitcoin.GetBlockCount();
			string lastBlockHash = GetLastBitcoinBlockHash();
			if (lastBlockHash == null)
			{
				lastBlockHash = m_bitcoin.GetBlockHash(blockHeight);
			}
					
			string latestBlockHash = m_bitcoin.GetBlockHash(blockHeight);

			// get all transactions of category 'receive'
			IEnumerable<TransactionSinceBlock> transactions = m_bitcoin.ListSinceBlock(lastBlockHash, 1).transactions.Where(t => t.Category == TransactionCategory.receive && t.Confirmations >= kBitcoinConfirms);

			foreach (TransactionSinceBlock t in transactions)
			{
				if (BitcoinAddressIsDepositAddress(t.Address))
				{
					// this is a confirmed bitcoin transaction
					//
					// make sure it hasn't already been credited
					if (!HasBitcoinDespoitBeenCredited(t.TxId) && !IsTransactionIgnored(t.TxId))
					{
						try
						{
							SendBitAssetsToDepositor(t);
						}
						catch (Exception e)
						{
							// problem sending bitassets, lets log it
							LogException(t.TxId, e.Message, DateTime.UtcNow, DaemonTransactionType.bitcoinDeposit);

							// also lets now ignore this transaction so we don't keep failing
							RefundBitcoinDeposit(t, e.Message);
						}
					}
				}
			}

			UpdateBitcoinBlockHash(latestBlockHash);
		}

		/// <summary>	Recompute transaction limits and prices. </summary>
		///
		/// <remarks>	Paul, 30/01/2015. </remarks>
		virtual protected void RecomputeTransactionLimitsAndPrices()
		{
			Dictionary<string, Dictionary<int, ulong>> accountBalances = m_bitshares.WalletAccountBalance(m_bitsharesAccount);
			m_bitsharesBalance = m_asset.GetAmountFromLarimers(accountBalances[m_bitsharesAccount][m_asset.id]);
			m_bitcoinBalance = m_bitcoin.GetBalance();

			m_bitcoinDepositLimit = Numeric.TruncateDecimal(m_bitsharesBalance * kMaxTransactionFactor, 2);
			m_bitsharesDepositLimit = Numeric.TruncateDecimal(m_bitcoinBalance * kMaxTransactionFactor, 2);

			m_bidPrice = 0.99M;
			m_askPrice = 1.01M;
		}

		/// <summary>	Starts this object. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		public virtual void Start()
		{

		}

		/// <summary>	Joins the damon thread </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		public void Update()
		{
			try
			{
				RecomputeTransactionLimitsAndPrices();

				//
				// handle bitshares->bitcoin
				//

				HandleBitsharesDesposits();

				//
				// handle bitcoin->bitshares
				// 

				HandleBitcoinDeposits();
			}
			catch (Exception e)
			{
				LogGeneralException(e.ToString());
			}
		}
	}
}
