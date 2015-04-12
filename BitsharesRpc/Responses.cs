using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsharesRpc
{
	public class BitsharesResponse<T>
	{
		public int id;
		public T result;
	}

	public class BitsharesErrorResponse
	{
		public int id;
		public BitsharesError error;
	}

	public class MarketStatusResponse
	{
		public int quote_id;
		public int base_id;
		public string last_error;
		public long bid_depth;
		public long ask_depth;
		public decimal current_feed_price;
		public BitsharesPrice center_price;
	}

	public class GetInfoResponse
	{
		public uint blockchain_head_block_num;
		public string blockchain_head_block_age;
		public DateTime? blockchain_head_block_timestamp;
		public decimal blockchain_average_delegate_participation;
		public int blockchain_confirmation_requirement;
		public string blockchain_delegate_pay_rate;
		public string blockchain_share_supply;
		public int blockchain_blocks_left_in_round;
		public string blockchain_next_round_time;
		public DateTime? blockchain_next_round_timestamp;
		public string blockchain_random_seed;
		public string client_data_dir;
		public string client_version;
		public int network_num_connections;
		public int network_num_connections_max;
		public DateTime? ntp_time;
		public DateTime? ntp_time_error;
		public bool wallet_open;
		public bool wallet_unlocked;
		public string wallet_unlocked_until;
		public DateTime? wallet_unlocked_until_timestamp;
		public DateTime? wallet_last_scanned_block_timestamp;
		public string wallet_scan_progress;
		public bool wallet_block_production_enabled;
		public DateTime? wallet_next_block_production_time;
		public DateTime? wallet_next_block_production_timestamp;
	}

	/// <summary>	The bitshares transaction response.
	/// 			{
	///  "index": 0,
	///  "record_id": "a0e6d81af8595f87992df53356d829a3c6b3d50b",
	///  "block_num": 0,
	///  "is_virtual": false,
	///  "is_confirmed": false,
	///  "is_market": false,
	///  "trx": {
	///    "expiration": "2014-12-22T17:55:57",
	///    "delegate_slate_id": null,
	///    "operations": [{
	///        "type": "deposit_op_type",
	///        "data": {
	///          "amount": 10000,
	///          "condition": {
	///            "asset_id": 0,
	///            "slate_id": 0,
	///            "type": "withdraw_signature_type",
	///            "data": {
	///              "owner": "BTSF2-----------------uVFcBq",
	///              "memo": {
	///                "one_time_key": "BTS8UWkj------------------Sb7Upfxc1Jw54",
	///                "encrypted_memo_data": "b0ff8ab5e3169------------------------65588de4bf10c13521157b6cb0f476468e3d0dc9ecbdf709493e49799eb1d"
	///              }
	///            }
	///          }
	///        }
	///      },{
	///        "type": "withdraw_op_type",
	///        "data": {
	///          "balance_id": "BTSH-------------------WecxUKF",
	///          "amount": 20000,
	///          "claim_input_data": ""
	///        }
	///      }
	///    ],
	///    "signatures": [
	///      "1f52ac36402ec839524df69a7c19-------------------------------------96b9d2c8d3332a59e9d2219850e7b149932375"
	///    ]
	///  },
	///  "ledger_entries": [{
	///      "from_account": "BTS8Rw-------------------2ietxF8APy",
	///      "to_account": "BTS-=------------------------LPrYfz4Lsib",
	///      "amount": {
	///        "amount": 10000,
	///        "asset_id": 0
	///      },
	///      "memo": "",
	///      "memo_from_account": null
	///    }
	///  ],
	///  "fee": {
	///    "amount": 10000,
	///    "asset_id": 0
	///  },
	///  "created_time": "2014-12-22T16:48:38",
	///  "received_time": "2014-12-22T16:48:38",
	///  "extra_addresses": []
	/// } </summary>
	///
	/// <remarks>	Paul, 22/12/2014. </remarks>
	public class BitsharesTransactionResponse
	{
		public int index;
		public string record_id;
		public ulong block_num;
		public bool is_virtual;
		public bool is_confirmed;
		public bool is_market;
		public BitsharesTrx trx;
		public BitsharesLedgerEntry[] ledger_entries;
		public BitsharesAmount fee;
		public DateTime created_time;
		public DateTime received_time;
		public string[] extra_addresses;
	}
}
