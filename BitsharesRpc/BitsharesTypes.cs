using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsharesRpc
{
	public class BitsharesAsset
	{
		public int id;
		public string symbol;
		public string name;
		public string description;
		public string public_data;
		public long issuer_account_id;
		public long precision;
		public DateTime registration_date;
		public DateTime last_update;
		public long current_share_supply;
		public decimal collected_fees; 
	}
	
	public class BitsharesMarketHistoryPoint
	{
		public DateTime timestamp;
		public decimal highest_bid;
		public decimal lowest_ask;
		public decimal opening_price;
		public decimal closing_price;
		public long volume;

		public decimal? recent_average_price;
	}

	public enum BitsharesOrderType
	{
		null_order,
		bid_order,
		ask_order,
		short_order,
		cover_order
	}

	public class BitsharesPrice
	{
		public decimal ratio;
		public int quote_asset_id;
		public int base_asset_id;
	}

	public class BitsharesAmount
	{
		public long amount;
		public int asset_id;
	}

	public class BitsharesMarketIndex
	{
		public BitsharesPrice order_price;
		public string owner;
	}

	public class BitsharesOrderState
	{
		public long balance;
		public BitsharesPrice short_price_limit;
		public DateTime last_update;
	}
	
	public class BitsharesOrder
	{
		public BitsharesOrderType type;
		public BitsharesMarketIndex market_index;
		public BitsharesOrderState state;
		public long? collateral;
		public BitsharesPrice interest_rate;
		public DateTime? expiration;

		public decimal GetBaseAmount()
		{
			switch( type )
			{
				case BitsharesOrderType.bid_order:
				{ 
					// balance is in USD  divide by price
					return state.balance * market_index.order_price.ratio;
				}
				case BitsharesOrderType.ask_order:
				{	
					// balance is in USD  divide by price
					return state.balance;
				}
				case BitsharesOrderType.short_order:
				{
					return state.balance;
				}
				case BitsharesOrderType.cover_order:
				{
					return (long)collateral*3/4.0M;
				}
				default:
				{
					throw new NotImplementedException();
				}
			}
		}

		public decimal GetQuoteAmount()
		{
			switch (type)
			{
				case BitsharesOrderType.bid_order:
				{ 
					// balance is in USD  divide by price
					return state.balance;
				}
				case BitsharesOrderType.ask_order:
				{	
					// balance is in USD  divide by price
					return state.balance * market_index.order_price.ratio;
				}
				case BitsharesOrderType.short_order:
				{
					return state.balance * market_index.order_price.ratio;
				}
				case BitsharesOrderType.cover_order:
				{
					return state.balance;
				}
				default:
				{
					throw new NotImplementedException();
				}
			}
		}
	}

	public class BitsharesTrade
	{
		public string bid_owner;
		public string ask_owner;
		public BitsharesPrice bid_price;
		public BitsharesPrice ask_price;
		public BitsharesAmount bid_paid;
		public BitsharesAmount bid_received;

		/** if bid_type == short, then collateral will be paid from short to cover positon */
		public BitsharesAmount bid_collateral;
		public BitsharesAmount ask_paid;
		public BitsharesAmount ask_received;
		public BitsharesOrderType bid_type;
		public BitsharesOrderType ask_type;
		public BitsharesAmount fees_collected;
		public DateTime timestamp;
	}

	/// <summary>	{
	///        "from_account": "onceuponatime",
	///        "to_account": "monsterer",
	///        "amount": {
	///          "amount": 50000,
	///          "asset_id": 0
	///        },
	///        "memo": "here you go",
	///        "running_balances": [[
	///            "monsterer", [[0,{"amount": 50000,"asset_id": 0}]
	///            ]
	///          ]
	///        ]
	///      } </summary>
	///
	/// <remarks>	Paul, 27/11/2014. </remarks>
	public class BitsharesLedgerEntry
	{
		public string from_account;
		public string to_account;
		public BitsharesAmount amount;
		public string memo;
	}

	/// <summary>	{
	///    "is_virtual": false,
	///    "is_confirmed": true,
	///    "is_market": false,
	///    "is_market_cancel": false,
	///    "trx_id": "d4d012f9a517a5f7b1bdf0a8eeb39a3c24865df6",
	///    "block_num": 726271,
	///    "ledger_entries": [{
	///        "from_account": "onceuponatime",
	///        "to_account": "monsterer",
	///        "amount": {
	///          "amount": 50000,
	///          "asset_id": 0
	///        },
	///        "memo": "here you go",
	///        "running_balances": [[
	///            "monsterer",[[
	///                0,{
	///                  "amount": 50000,
	///                  "asset_id": 0
	///                }
	///              ]
	///            ]
	///          ]
	///        ]
	///      }
	///    ],
	///    "fee": {
	///      "amount": 10000,
	///      "asset_id": 0
	///    },
	///    "timestamp": "20141012T201350",
	///    "expiration_timestamp": "20141012T212116",
	///    "error": null
	///  } </summary>
	///
	/// <remarks>	Paul, 27/11/2014. </remarks>
	public class BitsharesTransaction
	{
		public bool is_virtual;
		public bool is_confirmed;
		public bool is_market;
		public bool is_market_cancel;
		public string trx_id;
		public ulong block_num;
		public BitsharesLedgerEntry[] ledger_entries;
		public BitsharesAmount fee;
		public DateTime timestamp;
		public DateTime expiration_timestamp;
		public string error;
	}
}
