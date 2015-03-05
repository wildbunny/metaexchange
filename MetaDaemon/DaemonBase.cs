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
using WebDaemonShared;
using BitcoinRpcSharp.Responses;
using WebDaemonSharedTables;

namespace MetaDaemon
{

	public class AddressOrAccountName
	{
		public string m_text;
		public bool m_isAddress;
	}

	abstract public class DaemonBase
	{
		const int kSleepTimeSeconds = 10;
				
		public const string kFundingMemo = "FUND";
		public const string kSetPricesMemoStart = "SET";
		public const string kWithdrawMemo = "WITHDRAW";

		#if MONO
		protected const int kBitcoinConfirms = 1;
		#else
		protected const int kBitcoinConfirms = 0;
		#endif
		
		protected BitsharesWallet m_bitshares;
		protected BitcoinWallet m_bitcoin;

		protected string m_bitsharesAccount;
		protected string[] m_adminUsernames;

		protected byte m_addressByteType;

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
							string bitsharesAccount, string adminUsernames)
		{
			m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			m_bitcoin = new BitcoinWallet(bitcoinConfig.m_url, bitcoinConfig.m_rpcUser, bitcoinConfig.m_rpcPassword, false);

			m_bitsharesAccount = bitsharesAccount;
			m_adminUsernames = adminUsernames.Split(',');

			m_addressByteType = (byte)(bitcoinConfig.m_useTestnet ? AltCoinAddressTypeBytes.BitcoinTestnet : AltCoinAddressTypeBytes.Bitcoin);
		}

		protected abstract uint GetLastBitsharesBlock();
		protected abstract void UpdateBitsharesBlock(uint blockNum);
		public abstract bool HasDepositBeenCredited(string trxId);
		protected abstract bool IsTransactionIgnored(string txid);
		protected abstract void IgnoreTransaction(string txid);
		public abstract void MarkTransactionAsRefundedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType);
		public abstract void MarkTransactionAsRefundedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, string notes);
		public abstract void LogGeneralException(string message);
		protected abstract string GetLastBitcoinBlockHash();
		protected abstract void UpdateBitcoinBlockHash(string lastBlock);

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

		
		/// <summary>	Handles the bitshares desposits. </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <exception cref="UnsupportedTransactionException">	Thrown when an Unsupported Transaction
		/// 													error condition occurs. </exception>
		protected virtual Dictionary<string, BitsharesLedgerEntry> HandleBitsharesDesposits()
		{
			Dictionary<string, BitsharesLedgerEntry> results = new Dictionary<string, BitsharesLedgerEntry>();

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
			List<BitsharesWalletTransaction> assetTransactions = m_bitshares.WalletAccountTransactionHistory(	m_bitsharesAccount,
																												null,
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
					if (!HasDepositBeenCredited(t.trx_id) && !IsTransactionIgnored(t.trx_id))
					{
						results[t.trx_id] = l;						
					}
				}
				else
				{
					// fail with unhandled case
					throw new UnsupportedTransactionException(t.trx_id);
				}
			}

			UpdateBitsharesBlock(info.blockchain_head_block_num);

			return results;
		}

	

		

		/// <summary>	Handles the bitcoin deposits. </summary>
		///
		/// <remarks>	Paul, 23/12/2014. </remarks>
		protected List<TransactionSinceBlock> HandleBitcoinDeposits()
		{
			List<TransactionSinceBlock> results = new List<TransactionSinceBlock>();

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
				// this is a confirmed bitcoin transaction
				//
				// make sure it hasn't already been credited
				if (!HasDepositBeenCredited(t.TxId) && !IsTransactionIgnored(t.TxId))
				{
					results.Add(t);
				}
			}

			UpdateBitcoinBlockHash(latestBlockHash);

			return results;
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
		public virtual void Update()
		{
		}

		/// <summary>	Gets the type of the address byte. </summary>
		///
		/// <value>	The type of the address byte. </value>
		public byte m_AddressByteType
		{
			get { return m_addressByteType; }
		}

		/// <summary>	Gets the daemon account. </summary>
		///
		/// <value>	The m daemon account. </value>
		public string m_DaemonAccount
		{
			get { return m_bitsharesAccount; }
		}
	}
}
