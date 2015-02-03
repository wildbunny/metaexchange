using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using MySql.Data;
using MySqlDatabase;
using BitsharesRpc;
using WebDaemonShared;

namespace BtsOnrampDaemon
{
	public class StatsRow : ICoreType
	{
		public uint last_bitshares_block;
		public string last_bitcoin_block;
		public decimal bid_price;
		public decimal ask_price;
	}

	public class IgnoreRow : ICoreType
	{
		public uint uid;
		public string txid;
	}

	public class ExceptionRow : ICoreType
	{
		public string txid;
		public string message;
		public DateTime date;
		public DaemonTransactionType type;
	}


	public class DaemonMySql : DaemonBase
	{
		protected Database m_database;

		public DaemonMySql(RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string bitsharesAsset,
							string bitcoinDespositAddress,
							string databaseName, string databaseUser, string databasePassword) : base(bitsharesConfig, bitcoinConfig, bitsharesAccount, bitsharesAsset, bitcoinDespositAddress)
		{
			m_database = new Database(databaseName, databaseUser, databasePassword, System.Threading.Thread.CurrentThread.ManagedThreadId);
		}

		protected override uint GetLastBitsharesBlock()
		{
			StatsRow stats = m_database.Query<StatsRow>("SELECT * FROM stats;").First();
			return stats.last_bitshares_block;
		}

		protected override void UpdateBitsharesBlock(uint blockNum)
		{
			int updated = m_database.Statement("UPDATE stats SET last_bitshares_block=@block;", blockNum);
			Debug.Assert(updated > 0);
		}

		protected override bool HasBitsharesDepositBeenCredited(string trxId)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@trx;", trxId) > 0;
		}

		protected override void MarkBitsharesDespositAsCreditedStart(string bitsharesTxId)
		{
			MarkTransactionStart(bitsharesTxId);
		}

		protected override void MarkBitsharesDespositAsCreditedEnd(string bitsharesTrx, string bitcoinTxid, decimal amount)
		{
			MarkTransactionEnd(bitsharesTrx, bitcoinTxid, amount, DaemonTransactionType.bitsharesDeposit);
		}

		protected override string GetLastBitcoinBlockHash()
		{
			return m_database.Query<StatsRow>("SELECT * FROM stats;").First().last_bitcoin_block;
		}

		protected override void UpdateBitcoinBlockHash(string lastBlock)
		{
			m_database.Statement("UPDATE stats SET last_bitcoin_block=@block;", lastBlock);
		}

		protected override bool HasBitcoinDespoitBeenCredited(string txid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@txid;", txid) > 0;
		}

		protected override void MarkBitcoinDespositAsCreditedStart(string bitcoinTxid)
		{
			MarkTransactionStart(bitcoinTxid);
		}

		protected override void MarkBitcoinDespositAsCreditedEnd(string bitcoinTxid, string bitsharesTrxId, decimal amount)
		{
			MarkTransactionEnd(bitcoinTxid, bitsharesTrxId, amount, DaemonTransactionType.bitcoinDeposit);
		}

		protected override void MarkTransactionAsRefundedStart(string receivedTxid)
		{
			if (!IsPartTransaction(receivedTxid))
			{
				MarkTransactionStart(receivedTxid);
			}
		}

		protected override void MarkTransactionAsRefundedEnd(	string receivedTxid, string sentTxid, decimal amount, 
																DaemonTransactionType type, string notes)
		{
			MarkTransactionEnd(receivedTxid, sentTxid, amount, type, notes);
		}

		protected override bool IsTransactionIgnored(string txid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM ignored,exceptions WHERE ignored.txid=@txid OR exceptions.txid=@txid2;", txid, txid) > 0;
		}

		protected override void IgnoreTransaction(string txid)
		{
			m_database.Statement("INSERT INTO ignored (txid) VALUES(@txid);", txid);
		}

		protected override void LogException(string txid, string message, DateTime date, DaemonTransactionType type)
		{
			m_database.Statement("INSERT INTO exceptions (txid, message, date, type) VALUES(@a,@b,@c,@d);", txid, message, date, type);
		}

		protected override void LogGeneralException(string message)
		{
			uint hash = (uint)message.GetHashCode();

			DateTime now = DateTime.UtcNow;
			int updated = m_database.Statement("UPDATE general_exceptions SET count=count+1,date=@d WHERE hash=@h;", now, hash);
			if (updated == 0)
			{
				m_database.Statement("INSERT INTO general_exceptions (hash,message,date) VALUES(@a,@b,@c);", hash, message, now);
			}
		}

		// ------------------------------------------------------------------------------------------------------------

		void InsertTransaction(string receivedTxid, string sentTxid, decimal amount, DaemonTransactionType type, string notes=null)
		{
			m_database.Statement(	"INSERT INTO transactions (received_txid, sent_txid, asset, amount, date, type, notes) VALUES(@a,@b,@c,@d,@e,@f,@g);",
									receivedTxid, sentTxid, m_asset.symbol, amount, DateTime.UtcNow, type, notes);
		}

		void MarkTransactionStart(string receivedTxid)
		{
			InsertTransaction(receivedTxid, null, 0, DaemonTransactionType.none);
		}

		void MarkTransactionEnd(string receivedTxid, string sentTxid, decimal amount, DaemonTransactionType type, string notes=null)
		{
			m_database.Statement("UPDATE transactions SET sent_txid=@sent, amount=@amount, type=@type, notes=@notes WHERE received_txid=@txid;",
									sentTxid, amount, type, notes, receivedTxid);
		}

		bool IsPartTransaction(string receivedTxid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@txid AND sent_txid IS NULL;", receivedTxid) > 0;
		}
	}
}
