using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySqlDatabase;

namespace DatabaseTables
{
	public class SiteStatsRow : ICoreType
	{
		public decimal bid_price;
		public decimal ask_price;
		public decimal max_btc;
		public decimal max_bitassets;
		public DateTime last_update;
	}

	/// <summary>	Transaction types for logging purposes </summary>
	///
	/// <remarks>	Paul, 17/01/2015. </remarks>
	public enum DaemonTransactionType
	{
		bitcoinDeposit = 1,
		bitsharesDeposit,
		bitcoinRefund,
		bitsharesRefund,
		none
	}

	public class TransactionsRow : ICoreType
	{
		public uint uid;
		public string received_txid;
		public string sent_txid;

		public string asset;
		public decimal amount;

		public DaemonTransactionType type;

		public string notes;
		public DateTime date;
	}

	public class StatsPacket
	{
		public SiteStatsRow m_stats;
		public List<TransactionsRow> m_lastTransactions;
	}

	public class SubmitAddressResponse
	{
		public string deposit_address;
		public string memo;
	}
}
