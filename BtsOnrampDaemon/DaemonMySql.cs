using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using MySql.Data;
using MySqlDatabase;


namespace BtsOnrampDaemon
{
	public class StatsRow : ICoreType
	{
		public uint last_bitshares_block;
		public string last_bitcoin_block;
	}

	public class TransactionsRow : ICoreType
	{
		public string bitshares_trx;
		public string bitcoin_txid;

		public string asset;
		public decimal amount;

		public DaemonTransactionType type;
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
		Database m_database;

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

		protected override void MarkBitsharesDespositAsCredited(string bitsharesTrx, string bitcoinTxid, decimal amount)
		{
			InsertTransaction(bitsharesTrx, bitcoinTxid, amount, DaemonTransactionType.bitsharesDeposit);
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

		protected override void MarkBitcoinDespositAsCredited(string bitcoinTxid, string bitsharesTrxId, decimal amount)
		{
			InsertTransaction(bitcoinTxid, bitsharesTrxId, amount, DaemonTransactionType.bitcoinDeposit);
		}

		protected override void MarkTransactionAsRefunded(string receivedTxid, string sentTxid, decimal amount, DaemonTransactionType type)
		{
			InsertTransaction(receivedTxid, sentTxid, amount, type);
		}

		protected override bool IsTransactionIgnored(string txid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM ignored WHERE txid=@txid;", txid) > 0;
		}

		protected override void IgnoreTransaction(string txid)
		{
			m_database.Statement("INSERT INTO ignored (txid) VALUES(@txid);", txid);
		}

		protected override void LogException(string txid, string message, DateTime date, DaemonTransactionType type)
		{
			m_database.Statement("INSERT INTO exceptions (txid, message, date, type) VALUES(@a,@b,@c,@d);", txid, message, date, type);
		}

		// ------------------------------------------------------------------------------------------------------------

		void InsertTransaction(string receivedTxid, string sentTxid, decimal amount, DaemonTransactionType type)
		{
			m_database.Statement(	"INSERT INTO transactions (received_txid, sent_txid, asset, amount, date, type) VALUES(@a,@b,@c,@d,@e,@f);",
									receivedTxid, sentTxid, m_asset.symbol, amount, DateTime.UtcNow, type);
		}
	}
}
