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
using BestPrice;
using BitsharesCore;

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
		public const decimal kMinBtcFee = 0.00001M;
		#endif

		protected BitsharesAsset m_asset;
		protected PriceDiscovery m_prices;
		protected CurrenciesRow m_currency;

		protected decimal m_lastFeedPrice;
		
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
		public InternalMarket(	MetaDaemonApi daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, 
								string bitsharesAccount, CurrenciesRow bitsharesAsset) : 
								base(daemon, market, bitshares, bitcoin, bitsharesAccount)
		{
			m_currency = bitsharesAsset;
			m_flipped = m_market.GetBase(daemon.m_AllCurrencies) != bitsharesAsset;
			m_asset = m_bitshares.BlockchainGetAsset(CurrencyHelpers.ToBitsharesSymbol(bitsharesAsset));

			Dictionary<int, ulong> allBitsharesBalances = m_bitshares.WalletAccountBalance(bitsharesAccount)[bitsharesAccount];
			decimal bitcoinBalance = bitcoin.GetBalance();
			
			ComputeMarketPricesAndLimits(ref m_market, allBitsharesBalances, bitcoinBalance);
		}

		/// <summary>	Calculates the inventory ratio. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="allBitsharesBalances">	all bitshares balances. </param>
		/// <param name="bitcoinBalance">	   	The bitcoin balance. </param>
		///
		/// <returns>	The calculated inventory ratio. </returns>
		decimal ComputeInventoryRatio(Dictionary<int, ulong> allBitsharesBalances, decimal bitcoinBalance)
		{
			decimal bitsharesBalance = m_asset.GetAmountFromLarimers(allBitsharesBalances[m_asset.id]);

			// convert bitshares into bitcoin value
			bitsharesBalance *= m_lastFeedPrice;

			decimal inventoryRatio = (bitcoinBalance - bitsharesBalance) / (bitsharesBalance + bitcoinBalance);
			return inventoryRatio / 2 + 0.5M;
		}

		/// <summary>	Recompute feed price in btc. </summary>
		///
		/// <remarks>	Paul, 24/02/2015. </remarks>
		///
		/// <returns>	A decimal. </returns>
		decimal RecomputeFeedPriceInBtc()
		{
			decimal bitsharesPriceInBtc = m_bitshares.BlockchainMedianFeedPrice(CurrencyHelpers.kBtcSymbol);
			decimal bitsharesPriceInBitasset;

			if (m_asset.symbol != CurrencyHelpers.kBtsSymbol)
			{
				bitsharesPriceInBitasset = m_bitshares.BlockchainMedianFeedPrice(m_asset.symbol);
			}
			else
			{
				bitsharesPriceInBitasset = 1;
			}

			return bitsharesPriceInBtc / bitsharesPriceInBitasset;
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

			decimal maxTransactionFactor;

			if (m_currency.uia)
			{
				// with UIA we got to handle the maximum buy size differently
				BitsharesAccount account = m_bitshares.WalletGetAccount(m_bitsharesAccount);
				if (m_asset.issuer_account_id == account.id)
				{
					// we are the issuer!

					// refresh the asset
					m_asset = m_bitshares.BlockchainGetAsset(m_asset.symbol);

					// this is how much we can issue, so lets stick that in there
					baseBalance = m_asset.GetAmountFromLarimers(m_asset.maximum_share_supply - m_asset.current_share_supply);
				}
				else
				{
					throw new UnexpectedCaseException();
				}

				maxTransactionFactor = 1;
				//maxTransactionFactor = kMaxTransactionFactor;
			}
			else
			{
				maxTransactionFactor = kMaxTransactionFactor;
			}

			decimal newAskMax, newBidMax;

			// askMax is in BITCOINS
			// bidMax is in BITASSETS

			if (m_flipped)
			{
				// BTC_bitUSD
				
				// baseBalance = 10 bitUSD
				// ask = 240
				// askMax = 10 / 240 = 0.04 BTC

				newAskMax = Numeric.TruncateDecimal((baseBalance / m_market.ask) * maxTransactionFactor, 8);
				newBidMax = Numeric.TruncateDecimal((quoteBalance * m_market.bid) * maxTransactionFactor, 8);
			}
			else
			{
				// BTS_BTC
				//
				// baseBalance = 1 BTS
				// ask = 0.00004
				// askMax = 1 * 0.0004 = 0.0004 BTC

				newAskMax = Numeric.TruncateDecimal((baseBalance * m_market.ask) * maxTransactionFactor, 8);
				newBidMax = Numeric.TruncateDecimal((quoteBalance / m_market.bid) * maxTransactionFactor, 8);
			}

			m_isDirty |= newAskMax != m_market.ask_max || newBidMax != m_market.bid_max;
			
			market.ask_max = newAskMax;
			market.bid_max = newBidMax;

			if (m_market.price_discovery)
			{
				//
				// update price discovery engine
				//
				
				decimal bitsharesBalance = m_asset.GetAmountFromLarimers(bitsharesBalances[m_asset.id]);

				if (m_asset.symbol == CurrencyHelpers.kBtcSymbol)
				{
					m_lastFeedPrice = 1;
				}
				else
				{
					m_lastFeedPrice = RecomputeFeedPriceInBtc();
				}

				decimal inventoryRatio = ComputeInventoryRatio(bitsharesBalances, bitcoinBalance);

				if (m_prices == null)
				{
					//
					// initialise the price discovery engine
					// 

					m_prices = new PriceDiscovery(market.spread_percent, market.window_percent, m_lastFeedPrice, inventoryRatio);
				}

				decimal oldBid = m_market.bid;
				decimal oldAsk = m_market.ask;
				m_prices.UpdateParameters(m_lastFeedPrice, inventoryRatio, m_market.spread_percent, m_market.window_percent, out m_market.bid, out m_market.ask);

				m_isDirty |= oldBid != m_market.bid || oldAsk != m_market.ask;
			}
		}

		/// <summary>	Determine if we can deposit asset. </summary>
		///
		/// <remarks>	Paul, 15/02/2015. </remarks>
		///
		/// <param name="asset">	The asset. </param>
		///
		/// <returns>	true if we can deposit asset, false if not. </returns>
		public override bool CanDepositAsset(CurrenciesRow asset)
		{
			CurrenciesRow baseSymbol, quoteSymbol;
			CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(m_market.symbol_pair, m_daemon.m_AllCurrencies, out baseSymbol, out quoteSymbol);

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
			decimal oldBid = m_market.bid;

			try
			{
				if (m_market.price_discovery)
				{
					//
					// adjust prices based on order
					//

					decimal informed = m_asset.GetAmountFromLarimers(l.amount.amount) / m_market.bid_max;
					m_market.bid = m_prices.GetBidForSell(informed);
				}

				string btcAddress = s2d.receiving_address;
				SendBitcoinsToDepositor(btcAddress, trxId, l.amount.amount, m_asset, s2d.deposit_address, MetaOrderType.sell,  m_currency.uia);

				if (m_market.price_discovery)
				{
					// update database with new prices
					m_isDirty = true;
				}
			}
			catch (Exception e)
			{
				// also lets now ignore this transaction so we don't keep failing
				RefundBitsharesDeposit(l.from_account, l.amount.amount, trxId, e.Message, m_asset, s2d.deposit_address, MetaOrderType.sell);

				// restore this
				m_market.bid = oldBid;
			}
		}

		/// <summary>	Sets prices from single unit quantities. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="baseQuantity"> 	The base quantity. </param>
		/// <param name="quoteQuantity">	The quote quantity. </param>
		/// <param name="flipped">			true if flipped. </param>
		/// <param name="market">			The market. </param>
		public override void SetPricesFromSingleUnitQuantities(decimal baseQuantity, decimal quoteQuantity, bool flipped, MarketRow market)
		{
			base.SetPricesFromSingleUnitQuantities(baseQuantity, quoteQuantity, flipped, market);

			// disable further price discovery
			m_market.price_discovery = false;

			// disable in database too
			m_daemon.EnablePriceDiscovery(m_market.symbol_pair, false);
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
			decimal oldAsk = m_market.ask;

			try
			{
				if (m_market.price_discovery)
				{
					//
					// adjust prices based on order
					//

					decimal informed = t.Amount / m_market.ask_max;
					m_market.ask = m_prices.GetAskForBuy(informed);
				}

				SendBitAssetsToDepositor(t, m_asset, s2d, MetaOrderType.buy);

				if (m_market.price_discovery)
				{
					// update database with new prices
					m_isDirty = true;
				}
			}
			catch (Exception e)
			{
				// lets hear about what went wrong
				m_daemon.LogGeneralException(e.ToString());

				// also lets now ignore this transaction so we don't keep failing
				RefundBitcoinDeposit(t, e.Message, s2d, MetaOrderType.buy);

				// restore this
				m_market.ask = oldAsk;
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

			if (t.Confirmations >= DaemonBase.kBitcoinConfirms)
			{
				BuyBitAsset(t, s2d);
			}
			else
			{
				// mark this transaction as pending if it doesn't already exist
				if (m_daemon.m_Database.GetTransaction(t.TxId) == null)
				{
					m_daemon.m_Database.MarkDespositAsCreditedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, MetaOrderType.buy, MetaOrderStatus.pending);
				}
			}
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
		public override SubmitAddressResponse OnSubmitAddress(string receivingAddress, MetaOrderType orderType, uint referralUser)
		{
			SubmitAddressResponse response;

			if (orderType == MetaOrderType.buy)
			{
				string accountName = receivingAddress;
				bool isPublicKey = BitsharesPubKey.IsValidPublicKey(accountName);

				// check for theoretical validity
				
				if (!isPublicKey && !BitsharesWallet.IsValidAccountName(accountName))
				{
					throw new ApiExceptionInvalidAccount(accountName);
				}

				// try and retrieve a previous entry
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(accountName, m_market.symbol_pair, referralUser);
				if (senderToDeposit == null)
				{
					// no dice, create a new entry

					// check for actual validity
					try
					{
						string rcA;

						if (!isPublicKey)
						{
							BitsharesAccount account = m_bitshares.WalletGetAccount(accountName);
							rcA = account.name;
						}
						else
						{
							rcA = accountName;
						}

						// generate a new bitcoin address and tie it to this account
						string depositAdress = m_bitcoin.GetNewAddress();
						senderToDeposit = m_daemon.InsertSenderToDeposit(rcA, depositAdress, m_market.symbol_pair, referralUser);
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
				SenderToDepositRow senderToDeposit = m_daemon.GetSenderDepositFromReceiver(bitcoinAddress, m_market.symbol_pair, referralUser);
				if (senderToDeposit == null)
				{
					// generate a memo field to use instead
					senderToDeposit = m_daemon.InsertSenderToDeposit(bitcoinAddress, MarketBase.CreateMemo(bitcoinAddress, m_market.symbol_pair, referralUser), m_market.symbol_pair, referralUser);
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
		public override bool CollectFees(string bitcoinFeeAddress, string bitsharesFeeAccount)
		{
			List<TransactionsRow> transSince = m_daemon.GetCompletedTransactionsInMarketSince(m_market.symbol_pair, m_market.transaction_processed_uid);

			if (transSince.Count > kMaxTransactionsBeforeCollectFees)
			{
				decimal buyFees = transSince.Where(t => t.order_type == MetaOrderType.buy).Sum(t => t.fee);
				decimal sellFees = transSince.Where(t => t.order_type == MetaOrderType.sell).Sum(t => t.fee);

				// make sure these are in range
				buyFees = m_asset.Truncate(buyFees);
				sellFees = Numeric.TruncateDecimal(sellFees, 8);

				decimal btcWorthOfBitassets;
				if (m_flipped)
				{
					btcWorthOfBitassets = buyFees / m_market.bid;
				}
				else
				{
					btcWorthOfBitassets = buyFees * m_market.bid;
				}

				if (btcWorthOfBitassets > kMinBtcFee &&
					sellFees > kMinBtcFee)
				{
					// update this here to prevent failures from continually sending fees
					m_daemon.UpdateTransactionProcessedForMarket(m_market.symbol_pair, transSince.Last().uid);

					string bitsharesTrxId = null, bitcoinTxId = null;
					string exception = null;
					try
					{
						BitsharesTransactionResponse bitsharesTrx = SendBitAssets(buyFees, m_asset, bitsharesFeeAccount, "Fee payment");
						bitsharesTrxId = bitsharesTrx.record_id;
						
						// WTUPID BTC DUST SIZE PREVENTS SMALL TRANSACGTIOJNs
						bitcoinTxId = m_bitcoin.SendToAddress(bitcoinFeeAddress, sellFees, "Fee payment");
					}
					catch (Exception e)
					{
						exception = e.ToString();
					}

					m_daemon.m_Database.InsertFeeTransaction(	m_market.symbol_pair, 
																bitsharesTrxId, 
																bitcoinTxId, 
																buyFees, 
																sellFees, 
																transSince.Last().uid, 
																exception,
																transSince.First().received_txid,
																transSince.Last().received_txid);

					return true;
				}
			}

			return false;
		}
	}
}
