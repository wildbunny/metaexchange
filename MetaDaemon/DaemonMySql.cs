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
		protected MySqlData m_dataAccess;

		public DaemonMySql(RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string adminUsernames,
							string databaseName, string databaseUser, string databasePassword)
			: base(bitsharesConfig, bitcoinConfig, bitsharesAccount, adminUsernames)
		{
			m_dataAccess = new MySqlData(databaseName, databaseUser, databasePassword);
		}

		protected override uint GetLastBitsharesBlock()
		{
			return m_dataAccess.GetLastBitsharesBlock();
		}

		protected override void UpdateBitsharesBlock(uint blockNum)
		{
			m_dataAccess.UpdateBitsharesBlock(blockNum);
		}

		public override bool HasDepositBeenCredited(string trxId)
		{
			return m_dataAccess.HasDepositBeenCredited(trxId);
		}

		public void MarkDespositAsCreditedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType)
		{
			m_dataAccess.MarkDespositAsCreditedStart(receivedTxid, depositAddress, symbolPair, orderType);
		}

		public void MarkDespositAsCreditedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, decimal price, decimal fee)
		{
			m_dataAccess.MarkDespositAsCreditedEnd(receivedTxid, sentTxid, status, amount, price, fee);
		}

		protected override string GetLastBitcoinBlockHash()
		{
			return m_dataAccess.GetLastBitcoinBlockHash();
		}

		protected override void UpdateBitcoinBlockHash(string lastBlock)
		{
			m_dataAccess.UpdateBitcoinBlockHash(lastBlock);
		}

		public override void MarkTransactionAsRefundedStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType)
		{
			m_dataAccess.MarkTransactionAsRefundedStart(receivedTxid, depositAddress, symbolPair, orderType);
		}

		public override void MarkTransactionAsRefundedEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, string notes)
		{
			m_dataAccess.MarkTransactionAsRefundedEnd(receivedTxid, sentTxid, status, amount, notes);
		}

		protected override bool IsTransactionIgnored(string txid)
		{
			return m_dataAccess.IsTransactionIgnored(txid);
		}

		protected override void IgnoreTransaction(string txid)
		{
			m_dataAccess.IgnoreTransaction(txid);
		}
		
		protected override void LogGeneralException(string message)
		{
			m_dataAccess.LogGeneralException(message);
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
								string receivedTxid, string sentTxid, decimal amount, decimal price, decimal fee,
								MetaOrderStatus status, DateTime date, string notes = null)
		{
			m_dataAccess.InsertTransaction(symbolPair, depositAddress, orderType, receivedTxid, sentTxid, amount, price, fee, status, date, notes);
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
		void MarkTransactionStart(string receivedTxid, string depositAddress, string symbolPair, MetaOrderType orderType)
		{
			m_dataAccess.MarkTransactionStart(receivedTxid, depositAddress, symbolPair, orderType);
		}

		/// <summary>	Mark transaction end./ </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="receivedTxid">	The received txid. </param>
		/// <param name="sentTxid">	   	The sent txid. </param>
		/// <param name="status">	   	The status. </param>
		/// <param name="notes">	   	(Optional) the notes. </param>
		void MarkTransactionEnd(string receivedTxid, string sentTxid, MetaOrderStatus status, decimal amount, decimal price, decimal fee, string notes = null)
		{
			m_dataAccess.MarkTransactionEnd(receivedTxid, sentTxid, status, amount, price, fee, notes);
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
			return m_dataAccess.IsPartTransaction(receivedTxid);
		}

		/// <summary>	Gets all markets. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <returns>	all markets. </returns>
		protected List<MarketRow> GetAllMarkets()
		{
			return m_dataAccess.GetAllMarkets();
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
			return m_dataAccess.GetMarket(symbolPair);
		}

		/// <summary>	Updates the market in database described by market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="market">	The market. </param>
		protected void UpdateMarketInDatabase(MarketRow market)
		{
			bool updated = m_dataAccess.UpdateMarketInDatabase(market);
			Debug.Assert(updated);
		}

		/// <summary>	Query if 'identifier' is deposit for market. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="identifier">	The identifier. </param>
		/// <param name="marketUid"> 	The market UID. </param>
		///
		/// <returns>	true if deposit for market, false if not. </returns>
		protected bool IsDepositForMarket(string identifier, string symbolPair)
		{
			return m_dataAccess.IsDepositForMarket(identifier, symbolPair);
		}

		/// <summary>	Gets sender deposit from deposit. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="depositAddress">	The deposit address. </param>
		/// <param name="marketUid">	 	The market UID. </param>
		///
		/// <returns>	The sender deposit from deposit. </returns>
		public SenderToDepositRow GetSenderDepositFromDeposit(string depositAddress, string symbolPair)
		{
			return m_dataAccess.GetSenderDepositFromDeposit(depositAddress, symbolPair);
		}

		/// <summary>	Gets sender deposit from receiver. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="recevingAddress">	The receving address. </param>
		/// <param name="marketUid">	  	The market UID. </param>
		///
		/// <returns>	The sender deposit from receiver. </returns>
		public SenderToDepositRow GetSenderDepositFromReceiver(string recevingAddress, string symbolPair)
		{
			return m_dataAccess.GetSenderDepositFromReceiver(recevingAddress, symbolPair);
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
		public SenderToDepositRow InsertSenderToDeposit(string recevingAddress, string depositAddress, string symbolPair)
		{
			return m_dataAccess.InsertSenderToDeposit(recevingAddress, depositAddress, symbolPair);
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
			return m_dataAccess.GetTransaction(txid);
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
			return m_dataAccess.GetLastTransactionsFromDeposit(memo, depositAddress, limit);
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
			return m_dataAccess.GetLastTransactions(limit, market);
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
			return m_dataAccess.UpdateMarketPrices(symbolPair, bid, ask);
		}

		/// <summary>	Gets transactions in market since. </summary>
		///
		/// <remarks>	Paul, 18/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="lastUid">   	The last UID. </param>
		///
		/// <returns>	The transactions in market since. </returns>
		public List<TransactionsRow> GetTransactionsInMarketSince(string symbolPair, uint lastUid)
		{
			return m_dataAccess.GetTransactionsInMarketSince(symbolPair, lastUid);
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
			return m_dataAccess.UpdateTransactionProcessedForMarket(market, last);
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
		public void InsertFeeTransaction(	string market, string buyTrxId, string sellTrxId, decimal buyFee, decimal sellFee, 
											uint transactionProcessedUid, string exception)
		{
			m_dataAccess.InsertFeeTransaction(market, buyTrxId, sellTrxId, buyFee, sellFee, transactionProcessedUid, exception);
		}

		/// <summary>	Enables the price discovery. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="enable">	 	true to enable, false to disable. </param>
		public void EnablePriceDiscovery(string symbolPair, bool enable)
		{
			m_dataAccess.EnablePriceDiscovery(symbolPair, enable);
		}
	}
}
