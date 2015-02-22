using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

using MySqlDatabase;

namespace WebDaemonSharedTables
{
	public enum MetaOrderStatus
	{
		none = 1,
		processing,
		completed,
		refunded
	}

	public enum MetaOrderType
	{
		none = 1,
		buy,
		sell
	}

	public class TransactionsRow : TransactionsRowNoUid
	{
		public uint uid;
	}

	public class TransactionsRowNoUid : ICoreType
	{
		public string received_txid;
		public string sent_txid;

		public MetaOrderType order_type;
		public string symbol_pair;
		public decimal amount;
		public decimal price;
		public decimal fee;

		public MetaOrderStatus status;

		public string notes;
		public DateTime date;

		public string deposit_address;
	}

	/*public class StatsPacket
	{
		public SiteStatsRow m_stats;
		public List<TransactionsRow> m_lastTransactions;
	}*/
}
