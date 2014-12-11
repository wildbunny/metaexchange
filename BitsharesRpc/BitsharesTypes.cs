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
		public ulong precision;
		public DateTime registration_date;
		public DateTime last_update;
		public long current_share_supply;
		public decimal collected_fees; 

		public decimal GetAmountFromLarimers(ulong larmiers)
		{
			return (decimal)larmiers / precision;
		}
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
		public ulong amount;
		public int asset_id;
	}

	public class BitsharesMarketIndex
	{
		public BitsharesPrice order_price;
		public string owner;
	}

	public class BitsharesOrderState
	{
		public ulong balance;
		public BitsharesPrice short_price_limit;
		public DateTime last_update;
	}
	
	public class BitsharesOrder
	{
		public BitsharesOrderType type;
		public BitsharesMarketIndex market_index;
		public BitsharesOrderState state;
		public ulong? collateral;
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
	public class BitsharesWalletTransaction
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

	public enum BitsharesTransactionOp
	{
		null_op_type = 0,

		/** balance operations */
		withdraw_op_type,
		deposit_op_type,

		/** account operations */
		register_account_op_type,
		update_account_op_type,
		withdraw_pay_op_type,

		/** asset operations */
		create_asset_op_type,
		update_asset_op_type,
		issue_asset_op_type,

		/** delegate operations */
		fire_delegate_op_type,

		/** proposal operations */
		submit_proposal_op_type,
		vote_proposal_op_type,

		/** market operations */
		bid_op_type,
		ask_op_type,
		short_op_type, /* Deprecated */
		cover_op_type,
		add_collateral_op_type,
		remove_collateral_op_type,

		define_delegate_slate_op_type,

		update_feed_op_type,
		burn_op_type,
		link_account_op_type,
		withdraw_all_op_type,
		release_escrow_op_type,
		update_block_signing_key_type,

		short_op_v2_type
	}

	public enum BitsharesWithdrawCondition
	{
		/** assumes blockchain already knowws the condition, which
		* is provided the first time something is withdrawn */
		withdraw_null_type = 0,
		withdraw_signature_type,
		withdraw_vesting_type,
		withdraw_multi_sig_type,
		withdraw_password_type,
		withdraw_option_type,
		withdraw_escrow_type
	}

	public class BitsharesTransactionMemo
	{
		public string one_time_key;
		public string encrypted_memo_data;
	}

	public class BitsharesMemoOwner
	{
		public string owner;
		public BitsharesTransactionMemo memo;
	}

	/// <summary>	{
	///            "asset_id": 0,
	///            "delegate_slate_id": 0,
	///            "type": "withdraw_signature_type",
	///            "data": {
	///              "owner": "BTSX8oYY5PkyjxNCkE8Hsm65XJimfd7nqC1P9",
	///              "memo": {
	///                "one_time_key": "BTSX8asGa1sX5V17GBXhyTXoexfZcwWMTQw4jwFQjUmJavdM4egaBQ",
	///                "encrypted_memo_data": "ee5628f850cec8f828ff7db0419b8091a4e8aad2e9a976db332aad5bbf3d460715703ae3266c9094812a936351cda881ff05d4ae4f49c8affba4874a8f928284"
	///              }
	///            }
	///          } </summary>
	///
	/// <remarks>	Paul, 01/12/2014. </remarks>
	public class BitsharesOpCondition
	{
		public ulong asset_id;
		public ulong delegate_slate_id;
		public BitsharesWithdrawCondition type;
		public BitsharesMemoOwner data;
	}

	public class BitsharesOpData
	{
		public ulong amount;
		public string balance_id;
		public string claim_input_data;
		public BitsharesOpCondition condition;
	}

	public class BitsharesOperation
	{
		public BitsharesTransactionOp type;
		public BitsharesOpData data;
	}

	public class BitsharesChainPos
	{
		public ulong block_num;
		public ulong trx_num;
	}

	public class BitsharesVoteAmount
	{
		public long votes_for;
		public long votes_against;
	}

	public class BitsharesTrx
	{
		public DateTime expiration;
		public string delegate_slate_id;
		public BitsharesOperation[] operations;
		public string[] signatures;		
	}

	public class BitsharesTransaction
	{
		public BitsharesTrx trx;
		public string[] signed_keys;
		public string validation_error;
		public Dictionary<string, BitsharesAmount>[] required_deposits;
		public Dictionary<string, BitsharesAmount>[] provided_deposits;
		public Dictionary<long, BitsharesAmount>[] withdraws;
		public Dictionary<long, BitsharesAmount>[] deposits;
		public Dictionary<long, BitsharesAmount>[] deltas;
		public Dictionary<long, BitsharesVoteAmount>[] net_delegate_votes;
		public BitsharesAmount required_fees;
		public BitsharesAmount alt_fees_paid;
		public List<ulong[]> balance;

		public BitsharesChainPos chain_location;
	}

	/// <summary>	
	/// {
	///    "votes_for": 24809048496717,
	///    "blocks_produced": 1270,
	///    "blocks_missed": 1,
	///    "next_secret_hash": "fc1fbc50---------------------------",
	///    "last_block_num_produced": 1231903,
	///    "pay_rate": 100,
	///    "pay_balance": 15050000,
	///    "total_paid": 6350000000,
	///    "total_burned": 0,
	///    "block_signing_key": "BTSX5T-------------------------------ZdGvcqN"
	///  } </summary>
	///
	/// <remarks>	Paul, 10/12/2014. </remarks>
	public class BitsharesDelegateInfo
	{
		public ulong votes_for;
		public uint blocks_produced;
		public uint blocks_missed;
		public string next_secret_hash;
		public uint last_block_num_produced;
		public int pay_rate;
		public ulong pay_balance;
		public ulong total_paid;
		public ulong total_burned;
		public string block_signing_key;
	}

	/// <summary>
	/// {
	///  "id": 9569,
	///  "name": "shentist",
	///  "public_data": {
	///    "gravatarID": "8b9671d4fad2d25bf218a51538bcb192"
	///  },
	///  "owner_key": "BTS---------------------------------UwsBQKn845R",
	///  "active_key_history": [[
	///      "20140722T194340",
	///      "BTS---------------------------------UwsBQKn845R"
	///    ]
	///  ],
	///  "registration_date": "20140722T194340",
	///  "last_update": "20140722T194340",
	///  "delegate_info": null,
	///  "meta_data": null
	/// } </summary>
	///
	/// <remarks>	Paul, 10/12/2014. </remarks>
	public class BitsharesAccount
	{
		public uint id;
		public string name;
		public string public_data;
		public string owner_key;
		public List<Dictionary<DateTime, string>> active_key_history;
		public DateTime registration_date;
		public DateTime last_update;
		public BitsharesDelegateInfo delegate_info;
		public string meta_data;
	}
}
