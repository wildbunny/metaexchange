using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesRpc;
using BitcoinRpcSharp;
using BitcoinRpcSharp.Responses;
using WebDaemonShared;
using WebDaemonSharedTables;
using Casascius.Bitcoin;
using MetaData;

namespace MetaDaemon.Markets
{
	public abstract class MarketBase
	{
		protected MarketRow m_market;
		protected DaemonMySql m_daemon;

		protected BitsharesWallet m_bitshares;
		protected BitcoinWallet m_bitcoin;
		protected string m_bitsharesAccount;

		public MarketBase(DaemonMySql daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, string bitsharesAccount)
		{
			m_daemon = daemon;
			m_market = market;

			m_bitshares = bitshares;
			m_bitcoin = bitcoin;
			m_bitsharesAccount = bitsharesAccount;
		}
		
		public virtual void ComputeMarketPricesAndLimits(ref MarketRow market, Dictionary<int, ulong> bitsharesBalances, decimal bitcoinBalances)
		{
			m_market = market;
		}

		public abstract void HandleBitsharesDeposit(KeyValuePair<string, BitsharesLedgerEntry> kvp);
		public abstract void HandleBitcoinDeposit(TransactionSinceBlock t);
		public abstract SubmitAddressResponse OnSubmitAddress(string receivingAddress, MetaOrderType orderType);
		public abstract bool CanDepositAsset(CurrencyTypes asset);
		
		/// <summary>	Sends the bitcoins to depositor. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="RefundBitsharesException">	Thrown when a Refund Bitshares error condition
		/// 											occurs. </exception>
		///
		/// <param name="btcAddress">	The btc address. </param>
		/// <param name="trxId">	 	Identifier for the trx. </param>
		/// <param name="amount">	 	The amount. </param>
		/// <param name="asset">	 	The asset. </param>
		///
		/// <returns>	A string. </returns>
		protected virtual string SendBitcoinsToDepositor(string btcAddress, string trxId, ulong amount, BitsharesAsset asset, string depositAddress, MetaOrderType orderType)
		{
			decimal bitAssetAmount = asset.GetAmountFromLarimers(amount);

			// make sure failures after this point dont result in multiple credits
			m_daemon.MarkDespositAsCreditedStart(trxId, depositAddress, m_market.symbol_pair, orderType, bitAssetAmount);

			// get the BTC amount we need to transfer
			decimal btcToTransfer = bitAssetAmount * m_market.bid;

			if (btcToTransfer > m_market.bid_max)
			{
				throw new RefundBitsharesException("Over " + m_market.bid_max + " " + asset.symbol + "!");
			}

			// do the transfer
			string txid = m_bitcoin.SendToAddress(btcAddress, btcToTransfer);

			// mark this in our records
			m_daemon.MarkDespositAsCreditedEnd(trxId, txid, MetaOrderStatus.completed);

			return txid;
		}

		/// <summary>	Sends a bit assets to depositor. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="RefundBitcoinException">	Thrown when a Refund Bitcoin error condition
		/// 											occurs. </exception>
		///
		/// <param name="t">		The TransactionSinceBlock to process. </param>
		/// <param name="asset">	The asset. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		protected BitsharesTransactionResponse SendBitAssetsToDepositor(TransactionSinceBlock t, BitsharesAsset asset, SenderToDepositRow s2d, MetaOrderType orderType)
		{
			// make sure failures after this point do not result in repeated sending
			m_daemon.MarkDespositAsCreditedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, orderType, t.Amount);

			if (t.Amount > m_market.ask_max)
			{
				throw new RefundBitcoinException("Over " + Numeric.Format2Dps(m_market.ask_max) + " BTC!");
			}

			string bitsharesAccount = s2d.receiving_address;
			decimal amount = (1 / m_market.ask) * t.Amount;

			BitsharesTransactionResponse bitsharesTrx = m_bitshares.WalletTransfer(amount, asset.symbol, m_bitsharesAccount, bitsharesAccount);

			m_daemon.MarkDespositAsCreditedEnd(t.TxId, bitsharesTrx.record_id, MetaOrderStatus.completed);

			return bitsharesTrx;
		}

		/// <summary>	Bitshares transaction to bitcoin address. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="RefundBitsharesException">	Thrown when a Refund Bitshares error condition
		/// 											occurs. </exception>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		///
		/// <returns>	A string. </returns>
		protected SenderToDepositRow BitsharesTransactionToBitcoinAddress(BitsharesLedgerEntry l)
		{
			// look up the BTS address this transaction was sent to

			// look that address up in our map of sender->deposit address
			
			// pull the market uid out of the memo
			uint marketUid = MemoGetUid(l.memo);

			SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromDeposit(l.memo, marketUid);
			if (senderToDeposit != null)
			{
				return senderToDeposit;
			}
			else
			{
				throw new RefundBitsharesException("Missing/bad memo!");
			}
		}

		/// <summary>	Gets bitshares account from bitcoin deposit. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	The bitshares account from bitcoin deposit. </returns>
		protected SenderToDepositRow GetBitsharesAccountFromBitcoinDeposit(TransactionSinceBlock t)
		{
			// look up the deposit address in our map of sender->deposit
			SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromDeposit(t.Address, m_market.uid);
			if (senderToDeposit != null)
			{
				return senderToDeposit;
			}
			else
			{
				return null;
			}
		}

		/// <summary>	Gets account from ledger. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="l">	The BitsharesLedgerEntry to process. </param>
		///
		/// <returns>	The account from ledger. </returns>
		BitsharesAccount GetAccountFromLedger(string fromAccount)
		{
			return m_bitshares.WalletGetAccount(fromAccount);
		}

		/// <summary>	Refund bitshares deposit. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="fromAccount">	from account. </param>
		/// <param name="larimers">   	The larimers. </param>
		/// <param name="depositId">  	Identifier for the deposit. </param>
		/// <param name="memo">		  	The memo. </param>
		/// <param name="asset">	  	The asset. </param>
		protected void RefundBitsharesDeposit(string fromAccount, ulong larimers, string depositId, string memo, BitsharesAsset asset, string depositAddress, MetaOrderType orderType)
		{
			decimal amount = asset.GetAmountFromLarimers(larimers);

			// make sure failures after this point don't result in multiple refunds
			m_daemon.MarkTransactionAsRefundedStart(depositId, depositAddress, m_market.symbol_pair, orderType, amount);
			
			BitsharesAccount account = GetAccountFromLedger(fromAccount);
			BitsharesTransactionResponse response = m_bitshares.WalletTransfer(amount, asset.symbol, m_bitsharesAccount, fromAccount, memo);
			
			m_daemon.MarkTransactionAsRefundedEnd(depositId, response.record_id, MetaOrderStatus.refunded, memo);
		}

		/// <summary>	Gets all pubkeys from bitcoin transactions in this collection. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="txid">	The txid. </param>
		///
		/// <returns>
		/// An enumerator that allows foreach to be used to process all pubkeys from bitcoin transactions
		/// in this collection.
		/// </returns>
		IEnumerable<string> GetAllPubkeysFromBitcoinTransaction(string txid)
		{
			DecodedRawTransaction rawDeposit = m_bitcoin.GetRawTransaction(txid, 1);
			return rawDeposit.VIn.Select(vin => vin.ScriptSig.Asm.Split(' ')[1]);
		}

		/// <summary>	Refund bitcoin deposit. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		protected void RefundBitcoinDeposit(TransactionSinceBlock t, string notes, SenderToDepositRow s2d, MetaOrderType orderType)
		{
			m_daemon.MarkTransactionAsRefundedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, orderType, t.Amount);

			// get public key out of transaction
			string firstPubKey = GetAllPubkeysFromBitcoinTransaction(t.TxId).First();
			PublicKey pk = new PublicKey(firstPubKey, m_daemon.m_AddressByteType);

			// refund deposit
			string sentTxid = m_bitcoin.SendToAddress(pk.AddressBase58, t.Amount);

			// mark as such
			m_daemon.MarkTransactionAsRefundedEnd(t.TxId, sentTxid, MetaOrderStatus.refunded, notes);
		}

		/// <summary>	Sets prices from single unit quantities. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <exception cref="Exception">	Thrown when an exception error condition occurs. </exception>
		///
		/// <param name="baseQuantity"> 	The base quantity. </param>
		/// <param name="quoteQuantity">	The quote quantity. </param>
		public void SetPricesFromSingleUnitQuantities(decimal baseQuantity, decimal quoteQuantity)
		{
			decimal bid = baseQuantity;
			decimal ask = 1 / quoteQuantity;

			if (Math.Abs(bid - m_market.bid) > m_market.bid / 20 ||
				Math.Abs(ask - m_market.ask) > m_market.ask / 20)
			{
				throw new Exception("New prices are too different!");
			}

			int updated = m_daemon.UpdateMarketPrices(m_market.uid, bid, ask);
			if (updated == 0)
			{
				throw new Exception("No market row updated!");
			}
			else
			{
				m_market.ask = ask;
				m_market.bid = bid;
			}
		}

		/// <summary>	Memo get UID. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="memo">	The memo. </param>
		///
		/// <returns>	An uint. </returns>
		static public uint MemoGetUid(string memo)
		{
			return uint.Parse(memo.Split('-')[0]);
		}

		/// <summary>	Creates a memo. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="bitcoinAddress">	The bitcoin address. </param>
		/// <param name="marketUid">	 	The market UID. </param>
		///
		/// <returns>	The new memo. </returns>
		static public string CreateMemo(string bitcoinAddress, uint marketUid)
		{
			string start = marketUid.ToString() + "-";
			string memo = start + bitcoinAddress.Substring(0, Math.Min(BitsharesWallet.kBitsharesMaxMemoLength, bitcoinAddress.Length) - start.Length);
			return memo;
		}
	}
}
