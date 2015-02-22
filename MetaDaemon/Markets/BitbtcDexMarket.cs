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
	/*public class BitbtcDexMarket : InternalMarket
	{
		public BitbtcDexMarket(	MetaDaemonApi daemon, MarketRow market, BitsharesWallet bitshares, BitcoinWallet bitcoin, string bitsharesAccount, CurrencyTypes baseCurrency) : 
								base(daemon, market, bitshares, bitcoin, bitsharesAccount, baseCurrency)
		{
			// BTS_bitUSD
			// BTS_bitBTC
			// bitBTC_bitUSD
			
			IEnumerable<BitsharesMarket> markets = daemon.m_AllDexMarkets.Where( m=>m.base_id == BitsharesAsset.kBtsAssetId ||
																					m.base_id == BitsharesAsset.kbitBTCAssetId ||
																					m.base_id == m_asset.id ||
																					m.quote_id == BitsharesAsset.kBtsAssetId ||
																					m.quote_id == BitsharesAsset.kbitBTCAssetId ||
																					m.quote_id == m_asset.id);

			foreach (BitsharesMarket m in markets)
			{
				Console.WriteLine(daemon.m_AllBitsharesAssets[m.base_id].symbol + "/" + daemon.m_AllBitsharesAssets[m.quote_id].symbol);
			}
		}

		protected override void BuyBitAsset(TransactionSinceBlock t, SenderToDepositRow s2d)
		{
			// this marks the transaction as processing so it isn't considered again directly
			m_daemon.MarkDespositAsCreditedStart(t.TxId, s2d.deposit_address, m_market.symbol_pair, MetaOrderType.buy);
		}

		protected override void SellBitAsset(BitsharesLedgerEntry l, SenderToDepositRow s2d, string trxId)
		{
			m_daemon.MarkDespositAsCreditedStart(trxId, s2d.deposit_address, m_market.symbol_pair, MetaOrderType.sell);
		}
	}*/
}
