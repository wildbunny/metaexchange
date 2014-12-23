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

		public bool bitcoin_deposit;
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
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE bitshares_trx=@trx;", trxId) > 0;
		}

		protected override void MarkBitsharesDespositAsCredited(string bitsharesTrx, string bitcoinTxid, decimal amount)
		{
			InsertTransaction(bitcoinTxid, bitsharesTrx, amount, false);
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
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE bitcoin_txid=@txid;", txid) > 0;
		}

		protected override void MarkBitcoinDespositAsCredited(string bitcoinTxid, string bitsharesTrxId, decimal amount)
		{
			InsertTransaction(bitcoinTxid, bitsharesTrxId, amount, true);
		}

		// ------------------------------------------------------------------------------------------------------------

		void InsertTransaction(string bitcoinTxid, string bitsharesTrx, decimal amount, bool wasBitcoinDeposit)
		{
			m_database.Statement(	"INSERT INTO transactions (bitshares_trx, bitcoin_txid, asset, amount, date, bitcoin_deposit) VALUES(@a,@b,@c,@d,@e,@f);",
									bitsharesTrx, bitcoinTxid, m_asset.symbol, amount, DateTime.UtcNow, wasBitcoinDeposit);
		}
	}
}
