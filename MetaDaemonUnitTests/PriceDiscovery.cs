using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BestPrice;

namespace MetaDaemonUnitTests
{
	public class PriceDiscoveryTest
	{
		
		static public void TestGlostenSimple()
		{

			PriceDiscovery glosten = new PriceDiscovery(1.0121M - 0.977M, 1.5M-0.5M, 1, 0.5M);

			Random r = new Random();
			for (int i = 0; i < 21; i++)
			{
				bool buy = r.Next() % 2 == 0;
				decimal prop = (decimal)r.NextDouble();

				decimal bid, ask;

				glosten.GetBidAskForOrder(buy, prop, out bid, out ask);
				
				Console.WriteLine("i=" + i + " bid=" + bid + ",ask=" + ask);
				Console.WriteLine("trade was " + (buy ? "buy" : "sell") + ", informed = "+prop);
			}

		}
	}
}
