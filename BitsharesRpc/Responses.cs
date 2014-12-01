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
		public ulong blockchain_head_block_num;
		public string blockchain_head_block_age;
		public DateTime? blockchain_head_block_timestamp;
		public string blockchain_average_delegate_participation;
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
}
