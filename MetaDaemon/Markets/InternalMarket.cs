using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesRpc;
using BitcoinRpcSharp.Responses;
using BitcoinRpcSharp;
using WebDaemonShared;
using Casascius.Bitcoin;
using ApiHost;
using WebDaemonSharedTables;
using MetaData;

namespace MetaDaemon.Markets
{

	/// <summary>	An internal market. This always has BTC as the quote symbol </summary>
	///
	/// <remarks>	Paul, 05/02/2015. </remarks>
	public class InternalMarket : MarketBase
	{
		#if MONO
		public const decimal kMaxTransactionFactor = 10.0M / 100.0M;
		public const decimal kMinBtcFee = 0.1M;
		#else
		public const decimal kMaxTransactionFactor = 0.1M;
		public const decimal kMinBtcFee = 0.00000M;
		#endif

		protected BitsharesAsset m_asset;
		
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="uid">			   	The UID. </param>
		/// <param name="base">			   	The base. </param>
		/// <param name="quote">		   	The quote. </param>
		/// <param name="bitshares">	   	The bitshares. </param>
		/// <param name="bitcoin">		   	The bitcoin. </param>
		/// <param name="bitsharesAccount">	The bitshares account. </param>
		public InternalMarket(	MetaDaemonApi daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, string bitsharesAccount, CurrencyTypes bitsharesAsset) : 
								base(daemon, market, bitshares, bitcoin, bitsharesAccount)
		{
			m_flipped = m_market.GetBase() != bitsharesAsset;

			m_asset = m_bitshares.BlockchainGetAsset(CurrencyHelpers.ToBitsharesSymbol(bitsharesAsset));
		}

		/// <summary>	Calculates the market prices and limits. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="market">				[in,out] The market. </param>
		/// <param name="bitsharesBalances">	The bitshares balances. </param>
		/// <param name="bitcoinBalance">   	The bitcoin balance. </param>
		public override void ComputeMarketPricesAndLimits(	ref MarketRow market, 
															Dictionary<int, ulong> bitsharesBalances, 
															decimal bitcoinBalance)
		{
			base.ComputeMarketPricesAndLimits(ref market, bitsharesBalances, bitcoinBalance);

			decimal baseBalance = 0;
			decimal quoteBalance = bitcoinBalance;

			if (bitsharesBalances.ContainsKey(m_asset.id))
			{
				// only non zero balances return data, so this guard is necessary
				baseBalance = m_asset.GetAmountFromLarimers(bitsharesBalances[m_asset.id]);
			}

			decimal newAskMax, newBidMax;

			#if MONO
			int dps=2;
			#else
			int dps = 8;
			#endif

			if (m_flipped)
			{
				decimal t = baseBalance;
				baseBalance = quoteBalance;
				quoteBalance = t;
			}
			
			newAskMax = Numeric.TruncateDecimal((baseBalance / m_market.ask) * kMaxTransactionFactor, dps);
			newBidMax = Numeric.TruncateDecimal(quoteBalance * kMaxTransactionFactor, dps);
			
			m_isDirty |= market.ask_max != newAskMax || market.bid_max != newBidMax;
			
			market.ask_max = newAskMax;
			market.bid_max = newBidMax;
		}

		/// <summary>	Determine if we can deposit asset. </summary>
		///
		/// <remarks>	Paul, 15/02/2015. </remarks>
		///
		/// <param name="asset">	The asset. </param>
		///
		/// <returns>	true if we can deposit asset, false if not. </returns>
		public override bool CanDepositAsset(CurrencyTypes asset)
		{
			CurrencyTypes baseSymbol, quoteSymbol;
			CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(m_market.symbol_pair, out baseSymbol, out quoteSymbol);

			return baseSymbol == asset || quoteSymbol == asset;
		}

		/// <summary>	Sell bit asset. </summary>
		///
		/// <remarks>	Paul, 16/02/2015. </remarks>
		///
		/// <param name="l">		The BitsharesLedgerEntry to process. </param>
		/// <param name="s2d">  	The 2D. </param>
		/// <param name="trxId">	Identifier for the trx. </param>
		protected virtual void SellBitAsset(BitsharesLedgerEntry l, SenderToDepositRow s2d, string trxId)
		{
			try
			{
				string btcAddress = s2d.receiving_address;
				SendBitcoinsToDepositor(btcAddress, trxId, l.amount.amount, m_asset, s2d.deposit_address, MetaOrderType.sell);
			}
			catch (Exception e)
			{
				// also lets now ignore this transaction so we don't keep failing
				RefundBitsharesDeposit(l.from_account, l.amount.amount, trxId, e.Message, m_asset, s2d.deposit_address, MetaOrderType.sell);
			}
		}

		/// <summary>	Handles the bitshares deposit described by kvp. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="kvp">	The kvp. </param>
		public override void HandleBitsharesDeposit(KeyValuePair<string, BitsharesLedgerEntry> kvp)
		{
			// get the btc address
			BitsharesLedgerEntry l = kvp.Value;
			string trxId = kvp.Key;

			SenderToDepositRow s2d = BitsharesTransactionToBitcoinAddress(l);

			SellBitAsset(l, s2d, trxId);
		}

		/// <summary>	Buy bit asset. </summary>
		///
		/// <remarks>	Paul, 16/02/2015. </remarks>
		///
		/// <param name="t">  	The TransactionSinceBlock to process. </param>
		/// <param name="s2d">	The 2D. </param>
		protected virtual void BuyBitAsset(TransactionSinceBlock t, SenderToDepositRow s2d)
		{
			try
			{
				SendBitAssetsToDepositor(t, m_asset, s2d, MetaOrderType.buy);
			}
			catch (Exception e)
			{
				// also lets now ignore this transaction so we don't keep failing
				RefundBitcoinDeposit(t, e.Message, s2d, MetaOrderType.buy);
			}
		}

		/// <summary>	Handles the bitcoin deposit described by t. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		public override void HandleBitcoinDeposit(TransactionSinceBlock t)
		{
			SenderToDepositRow s2d = GetBitsharesAccountFromBitcoinDeposit(t);

			BuyBitAsset(t, s2d);
		}

		/// <summary>	Executes the submit address action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionMessage">	  	Thrown when an API exception message error
		/// 											condition occurs. </exception>
		/// <exception cref="UnexpectedCaseException">	Thrown when an Unexpected Case error condition
		/// 											occurs. </exception>
		///
		/// <param name="receivingAddress">	The receiving address. </param>
		/// <param name="orderType">	   	Type of the order. </param>
		///
		/// <returns>	A SubmitAddressResponse. </returns>
		public override SubmitAddressResponse OnSubmitAddress(string receivingAddress, MetaOrderType orderType)
		{
			SubmitAddressResponse response;

			if (orderType == MetaOrderType.buy)
			{
				string accountName = receivingAddress;

				// check for theoretical validity
				if (!BitsharesWallet.IsValidAccountName(accountName))
				{
					throw new ApiExceptionInvalidAccount(accountName);
				}

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(accountName, m_market.symbol_pair);
				if (senderToDeposit == null)
				{
					// no dice, create a new entry

					// check for actual validity
					try
					{
						BitsharesAccount account = m_bitshares.WalletGetAccount(accountName);

						// generate a new bitcoin address and tie it to this account
						string depositAdress = m_bitcoin.GetNewAddress();
						senderToDeposit = m_daemon.InsertSenderToDeposit(account.name, depositAdress, m_market.symbol_pair);
					}
					catch (BitsharesRpcException)
					{
						throw new ApiExceptionInvalidAccount(accountName);
					}
				}

				response =	new SubmitAddressResponse 
							{ 
								deposit_address = senderToDeposit.deposit_address,
								receiving_address = senderToDeposit.receiving_address
							};
			}
			else if (orderType == MetaOrderType.sell)
			{
				string bitcoinAddress = receivingAddress;

				// validate bitcoin address
				byte[] check = Util.Base58CheckToByteArray(bitcoinAddress);
				if (check == null)
				{
					throw new ApiExceptionInvalidAddress(bitcoinAddress);
				}

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(bitcoinAddress, m_market.symbol_pair);
				if (senderToDeposit == null)
				{
					// generate a memo field to use instead
					senderToDeposit = m_daemon.InsertSenderToDeposit(bitcoinAddress, MarketBase.CreateMemo(bitcoinAddress, m_market.symbol_pair), m_market.symbol_pair);
				}

				response =	new SubmitAddressResponse 
							{ 
								deposit_address = m_bitsharesAccount, 
								receiving_address = senderToDeposit.receiving_address,
								memo = senderToDeposit.deposit_address 
							};
			}
			else
			{
				throw new UnexpectedCaseException();
			}
			
			return response;
		}

		/// <summary>	Collect fees. </summary>
		///
		/// <remarks>	Paul, 18/02/2015. </remarks>
		///
		/// <param name="bitcoinFeeAddress">  	The bitcoin fee address. </param>
		/// <param name="bitsharesFeeAccount">	The bitshares fee account. </param>
		public override void CollectFees(string bitcoinFeeAddress, string bitsharesFeeAccount)
		{
			List<TransactionsRow> transSince = m_daemon.GetTransactionsInMarketSince(m_market.symbol_pair, m_market.transaction_processed_uid);

			if (transSince.Count > kMaxTransactionsBeforeCollectFees)
			{
				decimal buyFees = transSince.Where(t => t.order_type == MetaOrderType.buy).Sum(t => t.fee);
				decimal sellFees = transSince.Where(t => t.order_type == MetaOrderType.sell).Sum(t => t.fee);

				// make sure these are in range
				buyFees = m_asset.Truncate(buyFees);
				sellFees = Numeric.TruncateDecimal(sellFees, 8);

				if (buyFees / m_market.ask > kMinBtcFee &&
					sellFees > kMinBtcFee)
				{
					// update this here to prevent failures from continually sending fees
					m_daemon.UpdateTransactionProcessedForMarket(m_market.symbol_pair, transSince.Last().uid);

					string bitsharesTrxId = null, bitcoinTxId = null;
					string exception = null;
					try
					{
						BitsharesTransactionResponse bitsharesTrx = m_bitshares.WalletTransfer(buyFees, m_asset.symbol, m_bitsharesAccount, bitsharesFeeAccount);
						bitsharesTrxId = bitsharesTrx.record_id;
						
						// WTUPID BTC DUST SIZE PREVENTS SMALL TRANSACGTIOJNs
						bitcoinTxId = m_bitcoin.SendToAddress(bitcoinFeeAddress, sellFees);
					}
					catch (Exception e)
					{
						exception = e.ToString();
					}

					m_daemon.InsertFeeTransaction(m_market.symbol_pair, bitsharesTrxId, bitcoinTxId, buyFees, sellFees, transSince.Last().uid, exception);
				}
			}
		}
	}
}
