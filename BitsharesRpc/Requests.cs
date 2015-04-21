using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsharesRpc
{
	public enum BitsharesMethods
	{
		about,
		batch,
		blockchain_calculate_debt,
		blockchain_calculate_supply,
		blockchain_export_fork_graph,
		blockchain_get_account_wall,
		blockchain_get_asset,
		blockchain_get_balance,
		blockchain_get_block,
		blockchain_get_block_count,
		blockchain_get_block_signee,
		blockchain_get_block_transactions,
		blockchain_get_delegate_slot_records,
		blockchain_get_feeds_for_asset,
		blockchain_get_feeds_from_delegate,
		blockchain_get_info,
		blockchain_get_transaction,
		blockchain_is_synced,
		blockchain_list_active_delegates,
		blockchain_list_assets,
		blockchain_list_balances,
		blockchain_list_blocks,
		blockchain_list_delegates,
		blockchain_list_forks,
		blockchain_list_markets,
		blockchain_list_missing_block_delegates,
		blockchain_list_pending_transactions,
		blockchain_market_get_asset_collateral,
		blockchain_market_list_asks,
		blockchain_market_list_bids,
		blockchain_market_list_covers,
		blockchain_market_list_shorts,
		blockchain_market_order_book,
		blockchain_market_order_history,
		blockchain_market_price_history,
		blockchain_market_status,
		blockchain_median_feed_price,
		blockchain_unclaimed_genesis,
		debug_clear_errors,
		debug_enable_output,
		debug_filter_output_for_tests,
		debug_get_call_statistics,
		debug_list_errors_brief,
		debug_start_simulated_time,
		debug_update_logging_config,
		debug_wait,
		debug_wait_block_interval,
		debug_wait_for_block_by_number,
		debug_write_errors_to_file,
		execute_command_line,
		execute_script,
		get_account,
		get_info,
		get_balance,
		http_start_server,
		mail_archive_message,
		mail_check_new_messages,
		mail_fetch_message,
		mail_get_archive_messages,
		mail_get_messages_from,
		mail_get_messages_in_conversation,
		mail_get_messages_to,
		mail_get_processing_messages,
		mail_remove_message,
		mail_retry_send,
		mail_send,
		mail_store_message,
		meta_help,
		network_broadcast_transaction,
		network_get_advanced_node_parameters,
		network_get_connection_count,
		network_get_peer_info,
		network_get_transaction_propagation_data,
		network_get_upnp_info,
		network_list_potential_peers,
		network_set_allowed_peers,
		ntp_update_time,
		rpc_set_username,
		rpc_start_server,
		stop,
		wallet_account_balance,
		wallet_account_balance_ids,
		wallet_account_create,
		wallet_account_list_public_keys,
		wallet_account_order_list,
		wallet_account_register,
		wallet_account_rename,
		wallet_account_transaction_history,
		wallet_account_update_active_key,
		wallet_account_update_registration,
		wallet_account_vote_summary,
		wallet_account_yield,
		wallet_add_contact_account,
		wallet_address_create,
		wallet_backup_create,
		wallet_backup_restore,
		wallet_burn,
		wallet_change_passphrase,
		wallet_check_vote_proportion,
		wallet_close,
		wallet_delegate_set_block_production,
		wallet_delegate_withdraw_pay,
		wallet_dump_private_key,
		wallet_edit_transaction,
		wallet_get_account,
		wallet_get_info,
		wallet_get_pending_transaction_errors,
		wallet_get_setting,
		wallet_get_transaction,
		wallet_get_transaction_fee,
		wallet_import_armory,
		wallet_import_bitcoin,
		wallet_import_electrum,
		wallet_import_keyhotee,
		wallet_import_multibit,
		wallet_import_private_key,
		wallet_list,
		wallet_login_finish,
		wallet_login_start,
		wallet_mail_create,
		wallet_mail_encrypt,
		wallet_mail_open,
		wallet_market_add_collateral,
		wallet_market_cancel_order,
		wallet_market_cancel_orders,
		wallet_market_cover,
		wallet_market_order_list,
		wallet_market_submit_ask,
		wallet_market_submit_bid,
		wallet_market_submit_short,
		wallet_mia_create,
		wallet_uia_create,
		wallet_uia_issue,
		wallet_uia_issue_to_addresses,
		wallet_open,
		wallet_publish_feeds,
		wallet_publish_price_feed,
		wallet_publish_slate,
		wallet_publish_version,
		wallet_rebroadcast_transaction,
		wallet_recover_accounts,
		wallet_recover_transaction,
		wallet_regenerate_keys,
		wallet_remove_transaction,
		wallet_rescan_blockchain,
		wallet_scan_transaction,
		wallet_scan_transaction_experimental,
		wallet_set_automatic_backups,
		wallet_set_setting,
		wallet_set_transaction_expiration_time,
		wallet_set_transaction_fee,
		wallet_set_transaction_scanning,
		wallet_sign_hash,
		wallet_transfer,
		wallet_unlock
	}

	public enum RequestGranulatity
	{
		each_block = 0,
		each_hour,
		each_day
	}

	public enum VoteMethod
	{
		vote_none,
		vote_all,
		vote_random,
		vote_recommended
	}

	public enum BurnForOrAgainst
	{
		@for,
		against
	}

	/// <summary>
	/// {"jsonrpc":"2.0","id":1,"method":"blockchain_list_assets","params":[]}
	/// </summary>
	public class BitsharesRequest
	{
		public decimal jsonrpc;
		public int id;
		public BitsharesMethods method;
		public object[] @params;

		public BitsharesRequest(BitsharesMethods method, params object[] @params)
		{
			this.jsonrpc = 2;
			this.id = 1;
			this.method = method;
			this.@params = @params;
		}
	}

	public class BitsharesBatchRequest : BitsharesRequest
	{
		public BitsharesBatchRequest(	BitsharesMethods method, IEnumerable<object[]> @params) : 
										base(BitsharesMethods.batch, method, @params) { }
	}
}
