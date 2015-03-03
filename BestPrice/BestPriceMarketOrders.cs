using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesRpc;
using WebDaemonShared;

namespace BestPrice
{
    public class BestPriceMarketOrders
    {
		Dictionary<int, BitsharesAsset> m_allAssets;
		List<BitsharesMarket> m_allMarkets;
		//MySqlData m_database;

		public BestPriceMarketOrders(Dictionary<int, BitsharesAsset> allAssets, List<BitsharesMarket> allMarkets, MySqlData database)
		{
			m_allAssets = allAssets;
			m_allMarkets = allMarkets;
		}

		//public 
    }
}
