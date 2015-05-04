using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using MySqlDatabase;
using MetaData;
using WebDaemonSharedTables;

namespace WebDaemonShared
{
	public enum TransactionPolicy
	{
		INSERT,
		REPLACE,
		IGNORE
	}

	public class MySqlData
	{
		protected Database m_database;

		public MySqlData(string databaseName, string databaseUser, string databasePassword)
		{
			m_database = new Database(databaseName, databaseUser, databasePassword, System.Threading.Thread.CurrentThread.ManagedThreadId);
		}

		public StatsRow GetStats()
		{
			return m_database.Query<StatsRow>("SELECT * FROM stats;").First();
		}

		public uint GetLastBitsharesBlock()
		{
			StatsRow stats = GetStats();
			return stats.last_bitshares_block;
		}

		public void UpdateBitsharesBlock(uint blockNum)
		{
			int updated = m_database.Statement("UPDATE stats SET last_bitshares_block=@block;", blockNum);
			Debug.Assert(updated > 0);
		}

		public bool HasDepositBeenCredited(string trxId)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@trx AND status!=@pend;", trxId, MetaOrderStatus.pending) > 0;
		}

		public void MarkDespositAsCreditedStart(string receivedTxid, string depositAddress, string symbolPair, 
												MetaOrderType orderType, MetaOrderStatus status = MetaOrderStatus.processing,
												TransactionPolicy policy = TransactionPolicy.INSERT)
		{
			MarkTransactionStart(receivedTxid, depositAddress, symbolPair, orderType, status, policy);
		}

		public void MarkDespositAsCreditedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, decimal price, decimal fee)
		{
			MarkTransactionEnd(receivedTxid, sentTxid, status, amount, price, fee);
		}

		public string GetLastBitcoinBlockHash()
		{
			return m_database.Query<StatsRow>("SELECT * FROM stats;").First().last_bitcoin_block;
		}

		public void UpdateBitcoinBlockHash(string lastBlock)
		{
			m_database.Statement("UPDATE stats SET last_bitcoin_block=@block;", lastBlock);
		}

		public void MarkTransactionAsRefundedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType)
		{
			if (!IsPartTransaction(receivedTxid))
			{
				MarkTransactionStart(receivedTxid, depositAddress, symbolPair, orderType);
			}
		}

		public void MarkTransactionAsRefundedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, string notes)
		{
			MarkTransactionEnd(receivedTxid, sentTxid, status, amount, 0, 0, notes);
		}

		public bool IsTransactionIgnored(string txid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM ignored WHERE ignored.txid=@txid;", txid) > 0;
		}

		public void IgnoreTransaction(string txid)
		{
			m_database.Statement("INSERT INTO ignored (txid) VALUES(@txid);", txid);
		}
		
		public void LogGeneralException(string message)
		{
			uint hash = (uint)message.GetHashCode();

			DateTime now = DateTime.UtcNow;
			int updated = m_database.Statement("UPDATE general_exceptions SET count=count+1,date=@d WHERE hash=@h;", now, hash);
			if (updated == 0)
			{
				m_database.Statement("INSERT INTO general_exceptions (hash,message,date) VALUES(@a,@b,@c);", hash, message, now);
			}
		}

		// ------------------------------------------------------------------------------------------------------------

		/// <summary>	Inserts a transaction. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbolPair">  	The symbol pair. </param>
		/// <param name="orderType">   	Type of the order. </param>
		/// <param name="receivedTxid">	The received txid. </param>
		/// <param name="sentTxid">	   	The sent txid. </param>
		/// <param name="amount">	   	The amount. </param>
		/// <param name="type">		   	The type. </param>
		/// <param name="notes">	   	(Optional) the notes. </param>
		public void InsertTransaction(	string symbolPair, string depositAddress, MetaOrderType orderType, 
										string receivedTxid, string sentTxid, decimal amount, decimal price, decimal fee,
										MetaOrderStatus status, DateTime date, string notes = null, TransactionPolicy policy = TransactionPolicy.INSERT)
		{
			string verb;
			if (policy == TransactionPolicy.INSERT || policy == TransactionPolicy.REPLACE)
			{
				verb = policy.ToString();
			}
			else if (policy == TransactionPolicy.IGNORE)
			{
				verb = "INSERT IGNORE";
			}
			else
			{
				throw new NotImplementedException();
			}

			m_database.Statement(	verb + " INTO transactions (received_txid, deposit_address, sent_txid, symbol_pair, amount, price, fee, date, status, notes, order_type) VALUES(@a,@b,@c,@d,@e,@f,@g,@h,@i,@j,@k);",
									receivedTxid, depositAddress, sentTxid, symbolPair, amount, price, fee, date, status, notes, orderType);
		}

		/// <summary>	Mark transaction start. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="receivedTxid">  	The received txid. </param>
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="symbolPair">	 	The symbol pair. </param>
		/// <param name="orderType">	 	Type of the order. </param>
		/// <param name="amount">		 	The amount. </param>
		public void MarkTransactionStart(	string receivedTxid, string depositAddress, string symbolPair, 
											MetaOrderType orderType, MetaOrderStatus status = MetaOrderStatus.processing,
											TransactionPolicy policy = TransactionPolicy.INSERT)
		{
			InsertTransaction(symbolPair, depositAddress, orderType, receivedTxid, null, 0, 0, 0, status, DateTime.UtcNow, null, policy);
		}

		/// <summary>	Mark transaction end./ </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		/// <param name="sentTxid">	   	The sent txid. </param>
		/// <param name="status">	   	The status. </param>
		/// <param name="notes">	   	(Optional) the notes. </param>
		public void MarkTransactionEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, decimal price, decimal fee, string notes = null)
		{
			m_database.Statement(	"UPDATE transactions SET sent_txid=@sent, status=@status, notes=@notes, amount=@amount, price=@price, fee=@fee WHERE received_txid=@txid;",
									sentTxid, status, notes, amount, price, fee, receivedTxid);
		}

		/// <summary>	Query if 'receivedTxid' is part transaction. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		///
		/// <returns>	true if part transaction, false if not. </returns>
		public bool IsPartTransaction(string receivedTxid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@txid AND sent_txid IS NULL;", receivedTxid) > 0;
		}

		/// <summary>	Gets all markets. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <returns>	all markets. </returns>
		public List<MarketRow> GetAllMarkets()
		{
			return m_database.Query<MarketRow>("SELECT * FROM markets;");
		}

		/// <summary>	Gets a market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		///
		/// <returns>	The market. </returns>
		public MarketRow GetMarket(string symbolPair)
		{
			return m_database.Query<MarketRow>("SELECT * FROM markets WHERE symbol_pair=@s;", symbolPair).FirstOrDefault();
		}

		/// <summary>	Updates the market in database described by market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="market">	The market. </param>
		public bool UpdateMarketInDatabase(MarketRow market)
		{
			return  m_database.Statement(	"UPDATE markets SET ask=@a, bid=@b, ask_max=@c, bid_max=@d, ask_fee_percent=@e, bid_fee_percent=@f WHERE symbol_pair=@g;",
											market.ask, market.bid, market.ask_max, market.bid_max, market.ask_fee_percent, market.bid_fee_percent, market.symbol_pair) > 0;
		}

		/// <summary>	Inserts a market. </summary>
		///
		/// <remarks>	Paul, 23/02/2015. </remarks>
		///
		/// <param name="symbolPair">   	The symbol pair. </param>
		/// <param name="ask">				The ask. </param>
		/// <param name="bid">				The bid. </param>
		/// <param name="askMax">			The ask maximum. </param>
		/// <param name="bidMax">			The bid maximum. </param>
		/// <param name="askFeePercent">	The ask fee percent. </param>
		/// <param name="bidFeePercent">	The bid fee percent. </param>
		public void InsertMarket(	string symbolPair, decimal ask, decimal bid, decimal askMax, decimal bidMax, 
									decimal askFeePercent, decimal bidFeePercent)
		{
			m_database.Statement(	"INSERT INTO markets (symbol_pair,ask,bid,ask_max,bid_max,ask_fee_percent,bid_fee_percent) VALUES(@a,@b,@c,@d,@e,@f,@g);",
									symbolPair, ask, bid, askMax, bidMax, askFeePercent, bidFeePercent);
		}

		/// <summary>	Query if 'identifier' is deposit for market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="identifier">	The identifier. </param>
		/// <param name="marketUid"> 	The market UID. </param>
		///
		/// <returns>	true if deposit for market, false if not. </returns>
		public bool IsDepositForMarket(string identifier, string symbolPair)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM sender_to_deposit WHERE deposit_address=@d AND symbol_pair=@m;", identifier, symbolPair) > 0;
		}

		/// <summary>	Gets sender deposit from deposit. </summary>
		///
		/// <remarks>	Paul, 04/03/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="symbolPair">	 	The symbol pair. </param>
		/// <param name="referralUser">  	The referral user. </param>
		///
		/// <returns>	The sender deposit from deposit. </returns>
		public SenderToDepositRow GetSenderDepositFromDeposit(string depositAddress, string symbolPair, uint referralUser)
		{
			return m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE deposit_address=@d AND symbol_pair=@m AND referral_user=@u;", depositAddress, symbolPair, referralUser).FirstOrDefault();
		}

		/// <summary>	Bitcoin deposits cannot know the referral user, but a unique address is generated anyway, so 
		/// 			doesn't matter, but we need a custom look up function</summary>
		///
		/// <remarks>	Paul, 04/03/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="symbolPair">	 	The symbol pair. </param>
		///
		/// <returns>	The sender deposit from bitcoin deposit. </returns>
		public SenderToDepositRow GetSenderDepositIgnoreReferral(string depositAddress, string symbolPair)
		{
			List<SenderToDepositRow> shouldOnlyBeOne = m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE deposit_address=@d AND symbol_pair=@m;", depositAddress, symbolPair);

			Debug.Assert(shouldOnlyBeOne.Count <= 1);

			return shouldOnlyBeOne.FirstOrDefault();
		}

		/// <summary>	Gets sender deposit from receiver. </summary>
		///
		/// <remarks>	Paul, 04/03/2015. </remarks>
		///
		/// <param name="recevingAddress">	The receving address. </param>
		/// <param name="symbolPair">	  	The symbol pair. </param>
		/// <param name="referralUser">   	The referral user. </param>
		///
		/// <returns>	The sender deposit from receiver. </returns>
		public SenderToDepositRow GetSenderDepositFromReceiver(string recevingAddress, string symbolPair, uint referralUser)
		{
			return m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE receiving_address=@r AND symbol_pair=@m AND referral_user=@u;", recevingAddress, symbolPair, referralUser).FirstOrDefault();
		}

		/// <summary>	Inserts a sender to deposit. </summary>
		///
		/// <remarks>	Paul, 10/04/2015. </remarks>
		///
		/// <param name="recevingAddress">	The receving address. </param>
		/// <param name="depositAddress"> 	The deposit address. </param>
		/// <param name="symbolPair">	  	The symbol pair. </param>
		/// <param name="referralUser">   	The referral user. </param>
		/// <param name="replace">		  	(Optional) true to replace. </param>
		///
		/// <returns>	A SenderToDepositRow. </returns>
		public SenderToDepositRow InsertSenderToDeposit(string recevingAddress, string depositAddress, string symbolPair, uint referralUser, bool replace=false)
		{
			string verb=replace?"REPLACE":"INSERT";

			m_database.Statement(	verb + " INTO sender_to_deposit (deposit_address, receiving_address, symbol_pair, referral_user) VALUES(@a,@b,@c,@d);",
									depositAddress, recevingAddress, symbolPair, referralUser);

			return	new SenderToDepositRow 
					{ 
						deposit_address = depositAddress, 
						receiving_address = recevingAddress, 
						symbol_pair = symbolPair
					};
		}

		/// <summary>	Gets a transaction. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="txid">	The txid. </param>
		///
		/// <returns>	The transaction. </returns>
		public TransactionsRow GetTransaction(string txid)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE received_txid=@txid;", txid).FirstOrDefault();
		}

		/// <summary>	Gets the last transactions from deposit. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="limit">		 	The limit. </param>
		///
		/// <returns>	The last transactions from deposit. </returns>
		public List<TransactionsRowNoUid> GetLastTransactionsFromDeposit(string memo, string depositAddress, uint limit)
		{
			return m_database.Query<TransactionsRowNoUid>("SELECT * FROM transactions WHERE deposit_address=@a OR deposit_address=@b ORDER BY date DESC LIMIT @l;", memo, depositAddress, limit);
		}

		/// <summary>	Gets the last transactions. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="limit"> 	The limit. </param>
		/// <param name="market">	(Optional) The market. </param>
		///
		/// <returns>	The last transactions. </returns>
		public List<TransactionsRowNoUid> GetLastTransactions(uint limit, string market = null)
		{
			if (market != null)
			{
				return m_database.Query<TransactionsRowNoUid>("SELECT * FROM transactions WHERE symbol_pair=@s AND status=@b ORDER BY date DESC LIMIT @l;", market, MetaOrderStatus.completed, limit);
			}
			else
			{
				return m_database.Query<TransactionsRowNoUid>("SELECT * FROM transactions WHERE status=@s ORDER BY date DESC LIMIT @l;", MetaOrderStatus.completed, limit);
			}
		}

		/// <summary>	Updates the market prices. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <param name="marketUid">	The market UID. </param>
		/// <param name="bid">			The bid. </param>
		/// <param name="ask">			The ask. </param>
		public int UpdateMarketPrices(string symbolPair, decimal bid, decimal ask)
		{
			return m_database.Statement("UPDATE markets SET bid=@b, ask=@a WHERE symbol_pair=@u;", bid, ask, symbolPair);
		}

		/// <summary>	Gets transactions in market since. </summary>
		///
		/// <remarks>	Paul, 18/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="lastUid">   	The last UID. </param>
		///
		/// <returns>	The transactions in market since. </returns>
		public List<TransactionsRow> GetCompletedTransactionsInMarketSince(string symbolPair, uint lastUid)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE symbol_pair=@s AND uid>@lastUid AND status=@comp ORDER BY uid;", symbolPair, lastUid, MetaOrderStatus.completed);
		}

		/// <summary>	Gets transactions in market between. </summary>
		///
		/// <remarks>	Paul, 08/03/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="startUid">  	The start UID. </param>
		/// <param name="endUid">	 	The end UID. </param>
		///
		/// <returns>	The transactions in market between. </returns>
		public List<TransactionsRow> GetCompletedTransactionsInMarketBetween(string symbolPair, uint startUid, uint endUid)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE symbol_pair=@s AND uid>=@start AND uid<=@end AND status=@comp;", symbolPair, startUid, endUid, MetaOrderStatus.completed);
		}

		/// <summary>	Gets all referral transactions for user between. </summary>
		///
		/// <remarks>	Paul, 08/03/2015. </remarks>
		///
		/// <param name="referralUser">	The referral user. </param>
		/// <param name="symbolPair">  	The symbol pair. </param>
		/// <param name="startUid">	   	The start UID. </param>
		/// <param name="endUid">	   	The end UID. </param>
		///
		/// <returns>	all referral transactions for user between. </returns>
		public List<TransactionsRow> GetAllReferralTransactionsForUserBetween(uint referralUser, string symbolPair, uint startUid, uint endUid)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE uid>=@start AND uid<=@end AND symbol_pair=@sym AND status=@comp AND deposit_address IN (SELECT address FROM referral_addresses WHERE referral_user=@u);", startUid, endUid, symbolPair, MetaOrderStatus.completed, referralUser);
		}

		/// <summary>	Gets all transactions since. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="lastUid">	The last UID. </param>
		///
		/// <returns>	all transactions since. </returns>
		public List<TransactionsRow> GetAllTransactionsSince(uint lastUid)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE uid>@lastUid ORDER BY uid;", lastUid);
		}

		/// <summary>	Gets all pending transactions. </summary>
		///
		/// <remarks>	Paul, 13/03/2015. </remarks>
		///
		/// <returns>	all pending transactions. </returns>
		public List<TransactionsRow> GetAllPendingTransactions()
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE status=@s;", MetaOrderStatus.pending);
		}

		/// <summary>	Updates the transaction processed for market. </summary>
		///
		/// <remarks>	Paul, 18/02/2015. </remarks>
		///
		/// <param name="market">	The market. </param>
		/// <param name="last">  	The last. </param>
		///
		/// <returns>	An int. </returns>
		public int UpdateTransactionProcessedForMarket(string market, uint last)
		{
			return m_database.Statement("UPDATE markets SET transaction_processed_uid=@p WHERE symbol_pair=@s;", last, market);
		}

		/// <summary>	Null to empty. </summary>
		///
		/// <remarks>	Paul, 03/05/2015. </remarks>
		///
		/// <param name="s">	The string. </param>
		///
		/// <returns>	A string. </returns>
		string NullToEmpty(string s)
		{
			return s != null ? s : "";
		}

		/// <summary>	Inserts a fee transaction. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="market">				  	The market. </param>
		/// <param name="buyTrxId">				  	Identifier for the buy trx. </param>
		/// <param name="sellTrxId">			  	Identifier for the sell trx. </param>
		/// <param name="buyFee">				  	The buy fee. </param>
		/// <param name="sellFee">				  	The sell fee. </param>
		/// <param name="transactionProcessedUid">	The transaction processed UID. </param>
		/// <param name="exception">			  	The exception. </param>
		public long InsertFeeTransaction(string market, string buyTrxId, string sellTrxId, decimal buyFee, decimal sellFee, 
											uint transactionProcessedUid, string exception, string startTxId, string endTxId, bool replace=false)
		{
			string verb = replace ? "REPLACE" : "INSERT";
			long lastInsertedId;

			DateTime now = DateTime.UtcNow;
			object[] hashParts = { market, NullToEmpty(buyTrxId), NullToEmpty(sellTrxId), buyFee, sellFee, transactionProcessedUid, NullToEmpty(exception), startTxId, endTxId };
			
			uint hash = (uint)string.Join("|", hashParts).GetHashCode();

			m_database.StatementLastInserted(	verb + " INTO fee_collections (hash, symbol_pair, buy_trxid, sell_trxid, buy_fee, sell_fee, date, transaction_processed_uid, exception, start_txid, end_txid) VALUES(@aa,@a,@b,@c,@d,@e,@f,@g,@h,@i,@j);",
									out lastInsertedId,
									hash, market, buyTrxId, sellTrxId, buyFee, sellFee, now, transactionProcessedUid, exception, startTxId, endTxId);

			return lastInsertedId;
		}

		/// <summary>	Gets fee collection. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <param name="uid">	The UID. </param>
		///
		/// <returns>	The fee collection. </returns>
		public FeeCollectionRow GetFeeCollection(uint uid)
		{
			return m_database.Query<FeeCollectionRow>("SELECT * FROM fee_collections WHERE uid=@u;", uid).FirstOrDefault();
		}

		/// <summary>	Gets the last transaction UID. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <returns>	The last transaction UID. </returns>
		public uint GetLastTransactionUid()
		{
			return m_database.QueryScalar<uint>("SELECT MAX(uid) FROM transactions;");
		}

		/// <summary>	Gets the last fee collection UID. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <returns>	The last fee collection UID. </returns>
		public uint GetLastFeeCollectionUid()
		{
			return m_database.QueryScalar<uint>("SELECT MAX(uid) FROM fee_collections;");
		}

		/// <summary>	Gets site last transaction UID. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <returns>	The site last transaction UID. </returns>
		public uint GetSiteLastTransactionUid()
		{
			return m_database.QueryScalar<uint>("SELECT site_last_tid FROM stats;");
		}

		/// <summary>	Gets site last fee UID. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <returns>	The site last fee UID. </returns>
		public uint GetSiteLastFeeUid()
		{
			return m_database.QueryScalar<uint>("SELECT site_last_fee_tid FROM stats;");
		}

		/// <summary>	Updates the site last transaction UID described by lastTid. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <param name="lastTid">	The last tid. </param>
		public void UpdateSiteLastTransactionUid(uint lastTid)
		{
			m_database.Statement("UPDATE stats SET site_last_tid=@s;", lastTid);
		}

		/// <summary>	Updates the site last fee UID described by lastTid. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <param name="lastTid">	The last tid. </param>
		public void UpdateSiteLastFeeUid(uint lastTid)
		{
			m_database.Statement("UPDATE stats SET site_last_fee_tid=@s;", lastTid);
		}

		/// <summary>	Updates the last seen transaction for site. </summary>
		///
		/// <remarks>	Paul, 20/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="tid">		 	The tid. </param>
		public void UpdateLastSeenTransactionForSite(string symbolPair, uint tid)
		{
			m_database.Statement("UPDATE markets SET last_tid=@t WHERE symbol_pair=@s AND last_tid<@old;", tid, symbolPair, tid);
		}

		/// <summary>	Updates the market status. </summary>
		///
		/// <remarks>	Paul, 22/02/2015. </remarks>
		///
		/// <param name="daemonUrl">	URL of the daemon. </param>
		/// <param name="up">			true to up. </param>
		public void UpdateMarketStatus(string daemonUrl, bool up)
		{
			m_database.Statement("UPDATE markets SET up=@u WHERE daemon_url=@s;", up, daemonUrl);
		}

		/// <summary>	Enables the price discovert. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="enable">	 	true to enable, false to disable. </param>
		public void EnablePriceDiscovery(string symbolPair, bool enable)
		{
			m_database.Statement("UPDATE markets SET price_discovery=@p WHERE symbol_pair=@s;", enable, symbolPair);
		}

		/// <summary>	Inserts a withdrawal. </summary>
		///
		/// <remarks>	Paul, 26/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		/// <param name="txid">		   	The txid. </param>
		/// <param name="symbol">	   	The symbol. </param>
		/// <param name="amount">	   	The amount. </param>
		/// <param name="to">		   	to. </param>
		/// <param name="date">		   	The date Date/Time. </param>
		public void InsertWithdrawal(string receivedTxid, string txid, string symbol, decimal amount, string to, DateTime date)
		{
			m_database.Statement(	"INSERT INTO withdrawals (received_txid, sent_txid, symbol, amount, to_account, date) VALUES(@a,@b,@c,@d,@e,@f);", 
									receivedTxid, txid, symbol, amount, to, date);
		}

		/// <summary>	Withdrawal processed. </summary>
		///
		/// <remarks>	Paul, 26/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		public bool IsWithdrawalProcessed(string receivedTxid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM withdrawals WHERE received_txid=@a;", receivedTxid) > 0;
		}

		/// <summary>	Gets 24 hour btc volume. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
		///
		/// <param name="symbolPair">   	The symbol pair. </param>
		/// <param name="flippedMarket">	true to flipped market. </param>
		///
		/// <returns>	The 24 hour btc volume. </returns>
		public decimal Get24HourBtcVolume(string symbolPair, bool flippedMarket)
		{
			DateTime start = DateTime.UtcNow - new TimeSpan(1,0,0,0);

			if (flippedMarket)
			{
				return m_database.QueryScalar<decimal>("SELECT SUM(amount / price) FROM transactions WHERE symbol_pair=@market AND date>@start AND status=@s;", symbolPair, start, MetaOrderStatus.completed);
			}
			else
			{
				return m_database.QueryScalar<decimal>("SELECT SUM(amount * price) FROM transactions WHERE symbol_pair=@market AND date>@start AND status=@s;", symbolPair, start, MetaOrderStatus.completed);
			}
		}

		/// <summary>	Gets the last price. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		///
		/// <returns>	The last price. </returns>
		public LastPriceAndDelta GetLastPriceAndDelta(string symbolPair)
		{
			List<TransactionsRow> lastTwo = m_database.Query<TransactionsRow>("SELECT price FROM transactions WHERE symbol_pair=@market AND status=@s ORDER BY date DESC LIMIT 2;", symbolPair, MetaOrderStatus.completed);

			decimal delta, last;
			if (lastTwo.Count == 2)
			{
				delta = lastTwo[0].price - lastTwo[1].price;
				last = lastTwo.Last().price;
			}
			else if (lastTwo.Count == 1)
			{
				delta = lastTwo.Last().price;
				last = lastTwo.Last().price;
			}
			else
			{
				delta = 0;
				last = 0;
			}

			return new LastPriceAndDelta { last_price = last, price_delta = delta };
		}

		/// <summary>	Updates the market statistics. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
		///
		/// <param name="symbolPair">				The symbol pair. </param>
		/// <param name="btcVolume24h">				The btc volume 24h. </param>
		/// <param name="lastPrice">				The last price. </param>
		/// <param name="priceDelta">				The price delta. </param>
		/// <param name="realisedSpreadPercent">	The realised spread percent. </param>
		public void UpdateMarketStats(	string symbolPair, decimal btcVolume24h, decimal lastPrice, 
										decimal priceDelta, decimal realisedSpreadPercent,
										decimal buyQuantity, decimal sellQuantity)
		{
			m_database.Statement("UPDATE markets SET btc_volume_24h=@vol, last_price=@price, price_delta=@delta, realised_spread_percent=@spread, buy_quantity=@ra, sell_quantity=@rb WHERE symbol_pair=@market;",
									btcVolume24h, lastPrice, priceDelta, realisedSpreadPercent, buyQuantity, sellQuantity, symbolPair);
		}

		/// <summary>	Begins a transaction. </summary>
		///
		/// <remarks>	Paul, 03/03/2015. </remarks>
		public void BeginTransaction()
		{
			m_database.BeginTransaction();
		}

		/// <summary>	Ends a transaction. </summary>
		///
		/// <remarks>	Paul, 03/03/2015. </remarks>
		public void EndTransaction()
		{
			m_database.EndTransaction();
		}

		/// <summary>	Rolls back a transaction. </summary>
		///
		/// <remarks>	Paul, 03/03/2015. </remarks>
		public void RollbackTransaction()
		{
			m_database.RollbackTransaction();
		}

		/// <summary>	Inserts a referral address. </summary>
		///
		/// <remarks>	Paul, 05/03/2015. </remarks>
		///
		/// <param name="address">	   	The address. </param>
		/// <param name="referralUser">	The referral user. </param>
		public void InsertReferralAddress(string address, uint referralUser)
		{
			m_database.Statement("INSERT IGNORE INTO referral_addresses (address, referral_user) VALUES(@a,@b);", address, referralUser);
		}

		/// <summary>	Gets all referral users. </summary>
		///
		/// <remarks>	Paul, 05/03/2015. </remarks>
		///
		/// <returns>	all referral users. </returns>
		public List<ReferralUserRow> GetAllReferralUsers()
		{
			return m_database.Query<ReferralUserRow>("SELECT * FROM referral_users;");
		}

		/// <summary>	Gets all referral transactions for user since. </summary>
		///
		/// <remarks>	Paul, 05/03/2015. </remarks>
		///
		/// <param name="referralUser">	The referral user. </param>
		/// <param name="sinceTid">	   	The since tid. </param>
		/// <param name="symbolPair">  	The symbol pair. </param>
		///
		/// <returns>	all referral transactions for user since. </returns>
		public List<TransactionsRow> GetAllReferralTransactionsForUserSince(uint referralUser, uint sinceTid, string symbolPair)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE uid>@uid AND symbol_pair=@sym AND deposit_address IN (SELECT address FROM referral_addresses WHERE referral_user=@u);", sinceTid, symbolPair, referralUser);
		}

		/// <summary>	Gets or create referral last tid. </summary>
		///
		/// <remarks>	Paul, 05/03/2015. </remarks>
		///
		/// <param name="referralUser">	The referral user. </param>
		/// <param name="symbolPair">  	The symbol pair. </param>
		///
		/// <returns>	The or create referral last tid. </returns>
		/*public ReferralLastTidRow GetOrCreateReferralLastTid(uint referralUser, string symbolPair)
		{
			ReferralLastTidRow existing = null;
			while (existing == null)
			{ 
				m_database.Statement("INSERT IGNORE INTO referral_last_tids (referral_user, symbol_pair) VALUES(@r, @s);", referralUser, symbolPair);
				existing = m_database.Query<ReferralLastTidRow>("SELECT * FROM referral_last_tids WHERE referral_user=@u AND symbol_pair=@s;", referralUser, symbolPair).FirstOrDefault();
			}

			return existing;
		}*/

		/*public void UpdateReferralLastTidForUser(uint referralUser, string symbolPair, uint lastTid)
		{
			m_database.Statement("UPDATE referral_last_tids SET last_tid=@l WHERE ")
		}*/

		/// <summary>	Gets fee collections since. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <param name="uid">	The UID. </param>
		///
		/// <returns>	The fee collections since. </returns>
		public List<FeeCollectionRow> GetFeeCollectionsSince(uint uid)
		{
			return m_database.Query<FeeCollectionRow>("SELECT * FROM fee_collections WHERE uid>@u;", uid);
		}

		/// <summary>	Gets the configuration. </summary>
		///
		/// <remarks>	Paul, 08/03/2015. </remarks>
		///
		/// <returns>	The configuration. </returns>
		public ConfigRow GetConfig()
		{
			return m_database.Query<ConfigRow>("SELECT * FROM config;").FirstOrDefault();
		}

		/// <summary>	Count fee rows. </summary>
		///
		/// <remarks>	Paul, 08/03/2015. </remarks>
		///
		/// <returns>	The total number of fee rows. </returns>
		public long CountFeeRows()
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM fee_collections;");
		}

		/// <summary>	Gets all currencies. </summary>
		///
		/// <remarks>	Paul, 10/03/2015. </remarks>
		///
		/// <returns>	all currencies. </returns>
		public Dictionary<string, CurrenciesRow> GetAllCurrencies()
		{
			return m_database.Query<CurrenciesRow>("SELECT * FROM currencies;").ToDictionary(r => r.symbol);
		}

		/// <summary>	Query if 'address' is any deposit address. </summary>
		///
		/// <remarks>	Paul, 10/04/2015. </remarks>
		///
		/// <param name="address">	The address. </param>
		///
		/// <returns>	true if any deposit address, false if not. </returns>
		public bool IsAnyDepositAddress(string address)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM sender_to_deposit WHERE deposit_address=@d;", address) > 0;
		}
	}
}
