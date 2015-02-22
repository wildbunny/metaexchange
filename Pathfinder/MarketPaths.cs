using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using BitsharesRpc;
using WebDaemonShared;

namespace Pathfinder
{
	public class AssetNode
	{
		public BitsharesAsset m_asset;
		public List<AssetNode> m_links;

		public AssetNode(BitsharesAsset asset)
		{
			m_asset = asset;
			m_links = new List<AssetNode>();
		}

		public void AddLink(AssetNode n)
		{
			m_links.Add(n);
		}
	}

    public class MarketPaths
    {
		Dictionary<int, BitsharesAsset> m_allAssets;
		List<BitsharesMarket> m_allMarkets;

		Dictionary<int, AssetNode> m_directedGraph;

		public MarketPaths(Dictionary<int, BitsharesAsset> allAssets, List<BitsharesMarket> allMarkets)
		{
			m_allAssets = allAssets;
			m_allMarkets = allMarkets;

			m_directedGraph = new Dictionary<int, AssetNode>();

			// populate graph
			foreach (KeyValuePair<int, BitsharesAsset> kvp in allAssets)
			{
				m_directedGraph[kvp.Key] = new AssetNode(kvp.Value);
			}

			// connect nodes
			foreach (BitsharesMarket m in allMarkets)
			{
				Debug.Assert(m.quote_id != m.base_id);

				// connect the pairs in each market together
				m_directedGraph[m.base_id].AddLink(m_directedGraph[m.quote_id]);
				m_directedGraph[m.quote_id].AddLink(m_directedGraph[m.base_id]);
			}

			/*foreach (KeyValuePair<int, AssetNode> kvp in m_directedGraph)
			{
				Console.WriteLine(kvp.Value.m_asset.symbol);
				foreach (AssetNode n in kvp.Value.m_links)
				{
					Console.WriteLine("\t" + n.m_asset.symbol);
				}
			}*/
		}

		public List<List<AssetNode>> FindAllRoutes(int start, int end)
		{
		
			List<List<AssetNode>> allRoutes = new List<List<AssetNode>>();

			List<int> visitedAssets = new List<int>();
			List<AssetNode> route = new List<AssetNode>();

			FindRoute(m_directedGraph[start], m_directedGraph[end], visitedAssets, route, allRoutes);
			
			

			return allRoutes;
		}

		public bool FindRoute(AssetNode start, AssetNode end, List<int> visitedAssets, List<AssetNode> route, List<List<AssetNode>> allRoutes)
		{

			if (!visitedAssets.Contains(start.m_asset.id))
			{
				Console.Write(start.m_asset.symbol+"->");

				visitedAssets.Add(start.m_asset.id);

				route.Add(start);
				
				if (start == end)
				{
					// done
					return true;
				}
				else
				{
					foreach (AssetNode link in start.m_links)
					{
						if (FindRoute(link, end, visitedAssets, route, allRoutes))
						{
							Console.WriteLine("win");
						}
						else
						{
							Console.WriteLine("fail");
						}

						visitedAssets.RemoveRange(1, visitedAssets.Count - 1);
					}
				}
			}

			return false;
		}
    }
}
