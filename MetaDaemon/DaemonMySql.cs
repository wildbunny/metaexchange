using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using MySql.Data;
using MySqlDatabase;
using BitsharesRpc;
using WebDaemonShared;
using WebDaemonSharedTables;
using MetaData;

namespace MetaDaemon
{
	

	public class DaemonMySql : DaemonBase
	{
		protected Database m_database;

		public DaemonMySql(RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount,
							string databaseName, string databaseUser, string databasePassword) : base(bitsharesConfig, bitcoinConfig, bitsharesAccount)
		{
			m_database = new Database(databaseName, databaseUser, databasePassword, System.Threading.Thread.CurrentThread.ManagedThreadId);
		}

		protected override uint GetLastBitsharesBlock()
		{
			StatsRow stats = m_database.Query<StatsRow>("SELECT * FROM stats;").First();
			return stats.last_bitshares_block;
		}

		protected override void UpdateBitsharesBlock(uint blockNum)
		{
			int updated = m_database.Statement("UPDATE stats SET last_bitshares_block=@block;", blockNum);
			Debug.Assert(updated > 0);
		}

		public override bool HasDepositBeenCredited(string trxId)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@trx;", trxId) > 0;
		}

		public void MarkDespositAsCreditedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType, decimal amount)
		{
			MarkTransactionStart(receivedTxid, depositAddress, symbolPair, orderType, amount);
		}

		public void MarkDespositAsCreditedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status)
		{
			MarkTransactionEnd(receivedTxid, sentTxid, status);
		}

		protected override string GetLastBitcoinBlockHash()
		{
			return m_database.Query<StatsRow>("SELECT * FROM stats;").First().last_bitcoin_block;
		}

		protected override void UpdateBitcoinBlockHash(string lastBlock)
		{
			m_database.Statement("UPDATE stats SET last_bitcoin_block=@block;", lastBlock);
		}

		public override void MarkTransactionAsRefundedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType, decimal amount)
		{
			if (!IsPartTransaction(receivedTxid))
			{
				MarkTransactionStart(receivedTxid, depositAddress, symbolPair, orderType, amount);
			}
		}

		public override void MarkTransactionAsRefundedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, string notes)
		{
			MarkTransactionEnd(receivedTxid, sentTxid, status, notes);
		}

		protected override bool IsTransactionIgnored(string txid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM ignored WHERE ignored.txid=@txid;", txid) > 0;
		}

		protected override void IgnoreTransaction(string txid)
		{
			m_database.Statement("INSERT INTO ignored (txid) VALUES(@txid);", txid);
		}
		
		protected override void LogGeneralException(string message)
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
		void InsertTransaction(	string symbolPair, string depositAddress, MetaOrderType orderType, 
								string receivedTxid, string sentTxid, decimal amount, 
								MetaOrderStatus status, string notes = null)
		{
			m_database.Statement(	"INSERT INTO transactions (received_txid, deposit_address, sent_txid, symbol_pair, amount, date, status, notes, order_type) VALUES(@a,@b,@c,@d,@e,@f,@g,@h,@i);",
									receivedTxid, depositAddress, sentTxid, symbolPair, amount, DateTime.UtcNow, status, notes, orderType);
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
		void MarkTransactionStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType, decimal amount)
		{
			InsertTransaction(symbolPair, depositAddress, orderType, receivedTxid, null, amount, MetaOrderStatus.processing);
		}

		/// <summary>	Mark transaction end./ </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		/// <param name="sentTxid">	   	The sent txid. </param>
		/// <param name="status">	   	The status. </param>
		/// <param name="notes">	   	(Optional) the notes. </param>
		void MarkTransactionEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, string notes = null)
		{
			m_database.Statement(	"UPDATE transactions SET sent_txid=@sent, status=@status, notes=@notes WHERE received_txid=@txid;",
									sentTxid, status, notes, receivedTxid);
		}

		/// <summary>	Query if 'receivedTxid' is part transaction. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		///
		/// <returns>	true if part transaction, false if not. </returns>
		bool IsPartTransaction(string receivedTxid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM transactions WHERE received_txid=@txid AND sent_txid IS NULL;", receivedTxid) > 0;
		}

		/// <summary>	Gets all markets. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <returns>	all markets. </returns>
		protected List<MarketRow> GetAllMarkets()
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
		protected void UpdateMarketInDatabase(MarketRow market)
		{
			int updated = m_database.Statement(	"UPDATE markets SET ask=@a, bid=@b, ask_max=@c, bid_max=@d WHERE symbol_pair=@e;",
												market.ask, market.bid, market.ask_max, market.bid_max, market.symbol_pair);

			Debug.Assert(updated > 0);
		}

		/// <summary>	Query if 'identifier' is deposit for market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="identifier">	The identifier. </param>
		/// <param name="marketUid"> 	The market UID. </param>
		///
		/// <returns>	true if deposit for market, false if not. </returns>
		protected bool IsDepositForMarket(string identifier, uint marketUid)
		{
			return m_database.QueryScalar<long>("SELECT COUNT(*) FROM sender_to_deposit WHERE deposit_address=@d AND market_uid=@m;", identifier, marketUid) > 0;
		}

		/// <summary>	Gets sender deposit from deposit. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="marketUid">	 	The market UID. </param>
		///
		/// <returns>	The sender deposit from deposit. </returns>
		public SenderToDepositRow GetSenderDepositFromDeposit(string depositAddress, uint marketUid)
		{
			return m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE deposit_address=@d AND market_uid=@m;", depositAddress, marketUid).FirstOrDefault();
		}

		/// <summary>	Gets sender deposit from receiver. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="recevingAddress">	The receving address. </param>
		/// <param name="marketUid">	  	The market UID. </param>
		///
		/// <returns>	The sender deposit from receiver. </returns>
		public SenderToDepositRow GetSenderDepositFromReceiver(string recevingAddress, uint marketUid)
		{
			return m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE receiving_address=@r AND market_uid=@m;", recevingAddress, marketUid).FirstOrDefault();
		}

		/// <summary>	Inserts a sender to deposit. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="recevingAddress">	The receving address. </param>
		/// <param name="depositAddress"> 	The deposit address. </param>
		/// <param name="marketUid">	  	The market UID. </param>
		///
		/// <returns>	A SenderToDepositRow. </returns>
		public SenderToDepositRow InsertSenderToDeposit(string recevingAddress, string depositAddress, uint marketUid)
		{
			m_database.Statement(	"INSERT INTO sender_to_deposit (deposit_address, receiving_address, market_uid) VALUES(@a,@b,@c);", 
									depositAddress, recevingAddress, marketUid);
			return	new SenderToDepositRow 
					{ 
						deposit_address = depositAddress, 
						receiving_address = recevingAddress, 
						market_uid = marketUid 
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
		public List<TransactionsRow> GetLastTransactionsFromDeposit(string memo, string depositAddress, uint limit)
		{
			return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE deposit_address=@a OR deposit_address=@b ORDER BY date DESC LIMIT @l;", memo, depositAddress, limit);
		}

		/// <summary>	Gets the last transactions. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="limit"> 	The limit. </param>
		/// <param name="market">	(Optional) The market. </param>
		///
		/// <returns>	The last transactions. </returns>
		public List<TransactionsRow> GetLastTransactions(uint limit, string market=null)
		{
			if (market != null)
			{
				return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE symbol_pair=@s AND status=@s ORDER BY date DESC LIMIT @l;", market, MetaOrderStatus.completed, limit);
			}
			else
			{
				return m_database.Query<TransactionsRow>("SELECT * FROM transactions WHERE status=@s ORDER BY date DESC LIMIT @l;", MetaOrderStatus.completed, limit);
			}
		}

		/// <summary>	Updates the market prices. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <param name="marketUid">	The market UID. </param>
		/// <param name="bid">			The bid. </param>
		/// <param name="ask">			The ask. </param>
		public int UpdateMarketPrices(uint marketUid, decimal bid, decimal ask)
		{
			return m_database.Statement("UPDATE markets SET bid=@b, ask=@a WHERE uid=@u;", bid, ask, marketUid);
		}
	}
}
