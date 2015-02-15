using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySqlDatabase;
using WebDaemonShared;

namespace MetaData
{
	public class SubmitAddressResponse
	{
		public string deposit_address;
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
		public uint uid;
		public string symbol_pair;
		public decimal ask;
		public decimal bid;
		public decimal ask_max;
		public decimal bid_max;

		public CurrencyTypes GetBase()
		{
			return CurrencyHelpers.FromBitsharesSymbol(symbol_pair.Split('_')[0]);
		}

		public CurrencyTypes GetQuote()
		{
			return CurrencyHelpers.FromBitsharesSymbol(symbol_pair.Split('_')[1]);
		}
	}

	public class SenderToDepositRow : ICoreType
	{
		public string deposit_address;
		public string receiving_address;
		public uint market_uid;
	}
}
