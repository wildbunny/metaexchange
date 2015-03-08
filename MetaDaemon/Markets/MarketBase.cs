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
using ApiHost;

namespace MetaDaemon.Markets
{
	public abstract class MarketBase
	{
		protected const int kMaxTransactionsBeforeCollectFees = 0;

		protected MarketRow m_market;
		protected MetaDaemonApi m_daemon;

		protected BitsharesWallet m_bitshares;
		protected BitcoinWallet m_bitcoin;
		protected string m_bitsharesAccount;

		protected bool m_isDirty;
		protected bool m_flipped;

		public MarketBase(MetaDaemonApi daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, string bitsharesAccount)
		{
			m_daemon = daemon;
			m_market = market;

			m_bitshares = bitshares;
			m_bitcoin = bitcoin;
			m_bitsharesAccount = bitsharesAccount;
		}

		/// <summary>	Calculates the market prices and limits./ </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="market">				[in,out] The market. </param>
		/// <param name="bitsharesBalances">	The bitshares balances. </param>
		/// <param name="bitcoinBalances">  	The bitcoin balances. </param>
		///
		/// <returns>	Whether the prices were updated </returns>
		public virtual void ComputeMarketPricesAndLimits(ref MarketRow market, Dictionary<int, ulong> bitsharesBalances, decimal bitcoinBalances)
		{
			m_market = market;
		}

		public abstract void HandleBitsharesDeposit(KeyValuePair<string, BitsharesLedgerEntry> kvp);
		public abstract void HandleBitcoinDeposit(TransactionSinceBlock t);
		public abstract SubmitAddressResponse OnSubmitAddress(string receivingAddress, MetaOrderType orderType, uint referralUser);
		public abstract bool CanDepositAsset(CurrencyTypes asset);
		public abstract bool CollectFees(string bitcoinFeeAddress, string bitsharesFeeAccount);
		
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
			// make sure failures after this point dont result in multiple credits
			m_daemon.MarkDespositAsCreditedStart(trxId, depositAddress, m_market.symbol_pair, orderType);

			decimal bitAssetAmount = asset.GetAmountFromLarimers(amount);

			if (bitAssetAmount > m_market.bid_max)
			{
				throw new RefundBitsharesException("Over " + Numeric.TruncateDecimal(m_market.bid_max, 8) + " " + asset.symbol + "!");
			}

			// get the BTC amount we need to transfer
			decimal btcNoFee;

			if (m_flipped)
			{
				// they're sending us bitAssets, not BTC because the market is flipped, this is
				// equivelent to the opposite order type, so we have to use ask here
				btcNoFee = bitAssetAmount / m_market.ask;
			}
			else
			{
				btcNoFee = bitAssetAmount * m_market.bid;
			}

			// when selling, the fee is charged in BTC,
			// the amount recorded in the transaction is the amount of bitAssets sans fee, obv

			decimal fee = (m_market.bid_fee_percent / 100) * btcNoFee;
			decimal btcTotal = Numeric.TruncateDecimal(btcNoFee - fee, 8);
						
			// do the transfer
			string txid = m_bitcoin.SendToAddress(btcAddress, btcTotal);

			// mark this in our records
			m_daemon.MarkDespositAsCreditedEnd(trxId, txid, MetaOrderStatus.completed, bitAssetAmount, m_market.bid, fee);

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
			m_daemon.MarkDespositAsCreditedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, orderType);

			if (t.Amount > m_market.ask_max)
			{
				throw new RefundBitcoinException("Over " + Numeric.TruncateDecimal(m_market.ask_max, 8) + " " + asset.symbol + "!");
			}

			string bitsharesAccount = s2d.receiving_address;
			decimal bitAssetAmountNoFee;

			if (m_flipped)
			{
				// they're sending us BTC, not bitAssets because the market is flipped, this is
				// equivelent to the opposite order type, so we have to use bid here
				bitAssetAmountNoFee = t.Amount * m_market.bid;
			}
			else
			{
				bitAssetAmountNoFee = t.Amount / m_market.ask;
			}

			// when buying, the fee is charged in bitAssets,
			// the amount recorded in the transaction is the amount of bitAssets purchased sans fee

			bitAssetAmountNoFee = asset.Truncate(bitAssetAmountNoFee);

			decimal fee = (m_market.ask_fee_percent / 100) * bitAssetAmountNoFee;
			decimal amountAsset = bitAssetAmountNoFee - fee;

			amountAsset = asset.Truncate(amountAsset);
			
			BitsharesTransactionResponse bitsharesTrx = m_bitshares.WalletTransfer(amountAsset, asset.symbol, m_bitsharesAccount, bitsharesAccount);
			m_daemon.MarkDespositAsCreditedEnd(t.TxId, bitsharesTrx.record_id, MetaOrderStatus.completed, bitAssetAmountNoFee, m_market.ask, fee);

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
			string symbolPair;
			uint referralUser;
			MemoExtract(l.memo, out symbolPair, out referralUser);

			SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromDeposit(l.memo, symbolPair, referralUser);
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
			SenderToDepositRow senderToDeposit = m_daemon.m_Database.GetSenderDepositFromBitcoinDeposit(t.Address, m_market.symbol_pair);
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
			m_daemon.MarkTransactionAsRefundedStart(depositId, depositAddress, m_market.symbol_pair, orderType);
			
			BitsharesAccount account = GetAccountFromLedger(fromAccount);
			BitsharesTransactionResponse response = m_bitshares.WalletTransfer(amount, asset.symbol, m_bitsharesAccount, fromAccount, memo);
			
			m_daemon.MarkTransactionAsRefundedEnd(depositId, response.record_id, MetaOrderStatus.refunded, amount, memo);
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
			m_daemon.MarkTransactionAsRefundedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, orderType);

			// get public key out of transaction
			string firstPubKey = GetAllPubkeysFromBitcoinTransaction(t.TxId).First();
			PublicKey pk = new PublicKey(firstPubKey, m_daemon.m_AddressByteType);

			// refund deposit
			string sentTxid = m_bitcoin.SendToAddress(pk.AddressBase58, t.Amount);

			// mark as such
			m_daemon.MarkTransactionAsRefundedEnd(t.TxId, sentTxid, MetaOrderStatus.refunded, t.Amount, notes);
		}

		/// <summary>	Sets prices from single unit quantities. </summary>
		///
		/// <remarks>	Paul, 14/02/2015. </remarks>
		///
		/// <exception cref="Exception">	Thrown when an exception error condition occurs. </exception>
		///
		/// <param name="baseQuantity"> 	The base quantity. </param>
		/// <param name="quoteQuantity">	The quote quantity. </param>
		virtual public void SetPricesFromSingleUnitQuantities(decimal baseQuantity, decimal quoteQuantity, bool flipped, MarketRow market)
		{
			decimal bid, ask;

			decimal buyFee = baseQuantity * market.bid_fee_percent/100;
			decimal sellFee = quoteQuantity * market.ask_fee_percent/100;

			baseQuantity -= buyFee;
			quoteQuantity += sellFee;

			if (flipped)
			{
				bid = baseQuantity;
				ask = quoteQuantity;
			}
			else
			{
				bid = 1 / baseQuantity;
				ask = 1 / quoteQuantity;
			}

			if (Math.Abs(bid - m_market.bid) > m_market.bid / 10 ||
				Math.Abs(ask - m_market.ask) > m_market.ask / 10)
			{
				throw new Exception("New prices are too different!");
			}

			int updated = m_daemon.UpdateMarketPrices(m_market.symbol_pair, bid, ask);
			if (updated == 0)
			{
				throw new Exception("No market row updated!");
			}
			else
			{
				m_market.ask = ask;
				m_market.bid = bid;
			}

			m_isDirty = true;
		}

		/// <summary>	Gets the market UID. </summary>
		///
		/// <value>	The m market UID. </value>
		public string m_MarketSymbolPair
		{
			get { return m_market.symbol_pair; }
		}

		/// <summary>	Gets a value indicating whether this object is dirty. </summary>
		///
		/// <value>	true if dirty, false if not. </value>
		public bool m_IsDirty
		{
			get { return m_isDirty; }
			set { m_isDirty = value; }
		}

		/// <summary>	Gets the market. </summary>
		///
		/// <value>	The m market. </value>
		public MarketRow m_Market
		{
			get { return m_market; }
		}

		/// <summary>	Memo get UID. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="memo">	The memo. </param>
		///
		/// <returns>	An uint. </returns>
		static public void MemoExtract(string memo, out string symbolPair, out uint referralUser)
		{
			string[] parts = memo.Split('-');
			if (parts.Length == 2)
			{
				symbolPair = parts[0];
				referralUser = 0;
			}
			else if (parts.Length == 3)
			{
				symbolPair = parts[0];
				referralUser = uint.Parse(parts[1]);
			}
			else
			{
				throw new UnexpectedCaseException();
			}
		}

		/// <summary>	Creates a memo. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="bitcoinAddress">	The bitcoin address. </param>
		/// <param name="marketUid">	 	The market UID. </param>
		///
		/// <returns>	The new memo. </returns>
		static public string CreateMemo(string bitcoinAddress, string symbolPair, uint referralUser)
		{
			string start = symbolPair + "-" + referralUser + "-";
			string memo = start + bitcoinAddress.Substring(0, Math.Min(BitsharesWallet.kBitsharesMaxMemoLength, bitcoinAddress.Length) - start.Length);
			return memo;
		}
	}
}
