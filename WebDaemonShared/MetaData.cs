using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

using MySqlDatabase;
using WebDaemonShared;

namespace MetaData
{
	public class SubmitAddressResponse
	{
		public string deposit_address;
		public string receiving_address;
		public string memo;
	}

	public class StatsRow : ICoreType
	{
		public uint last_bitshares_block;
		public string last_bitcoin_block;
	}

	public class IgnoreRow : ICoreType
	{
		public uint uid;
		public string txid;
	}

	public class MarketRow : ICoreType
	{
		public string symbol_pair;
		public decimal ask;
		public decimal bid;
		public decimal ask_max;
		public decimal bid_max;
		public decimal ask_fee_percent;
		public decimal bid_fee_percent;
		public bool up;

		[IgnoreDataMember]
		public uint transaction_processed_uid;

		[IgnoreDataMember]
		public string daemon_url;

		[IgnoreDataMember]
		public uint last_tid;
		
		public CurrencyTypes GetBase()
		{
			return CurrencyHelpers.FromSymbol(symbol_pair.Split('_')[0]);
		}

		public CurrencyTypes GetQuote()
		{
			return CurrencyHelpers.FromSymbol(symbol_pair.Split('_')[1]);
		}
	}

	public class SenderToDepositRow : ICoreType
	{
		public string deposit_address;
		public string receiving_address;
		public string symbol_pair;
	}

	public class FeeCollectionRow : ICoreType
	{
		public uint uid;
		public string buy_trxid;
		public string sell_trxid;
		public decimal buy_fee;
		public decimal sell_fee;
		public DateTime date;
	}
}
