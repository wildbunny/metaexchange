using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using SharpTestsEx;

using BtsOnrampDaemon;
using WebDaemonShared;
using MetaData;

namespace MetaDaemonUnitTests
{
	[TestFixture]
	public class CurrencyTypeMigration : TestBase
	{

		[Test]
		public void IsBitsharesAsset()
		{
			for (int i=0; i<(int)CurrencyTypesDep.max; i++)
			{
				CurrencyTypesDep type = (CurrencyTypesDep)i;
				CurrenciesRow newType = m_currencies[type.ToString()];

				Assert.AreEqual(CurrencyHelpersDep.IsBitsharesAsset(type), CurrencyHelpers.IsBitsharesAsset(newType));
			}
		}

		[Test]
		public void ToBitsharesSymbol()
		{
			for (int i = 0; i < (int)CurrencyTypesDep.max; i++)
			{
				CurrencyTypesDep type = (CurrencyTypesDep)i;
				CurrenciesRow newType = m_currencies[type.ToString()];

				Assert.AreEqual(CurrencyHelpersDep.ToBitsharesSymbol(type), CurrencyHelpers.ToBitsharesSymbol(newType));
			}
		}

		[Test]
		public void FromBitsharesSymbol()
		{
			string[] symbols =
			{
				"BTS","BTC","USD","GOLD","CNY"
			};

			foreach (string s in symbols)
			{
				string old = CurrencyHelpersDep.FromBitsharesSymbol(s).ToString();
				string n = CurrencyHelpers.FromBitsharesSymbol(s, m_currencies, false).ToString();

				Assert.AreEqual(old, n);
			}
		}

		[Test]
		public void FromSymbol()
		{
			List<string> symbols = new List<string>();
			for (int i = 0; i < (int)CurrencyTypesDep.max; i++)
			{
				CurrencyTypesDep type = (CurrencyTypesDep)i;
				symbols.Add(type.ToString());
			}

			foreach (string s in symbols)
			{
				string old = CurrencyHelpersDep.FromSymbol(s).ToString();
				string n = CurrencyHelpers.FromSymbol(s, m_currencies).ToString();

				Assert.AreEqual(old, n);
			}
		}

		[Test]
		public void GetMarketSymbolPair()
		{
			List<string> symbols = new List<string>();
			for (int i = 0; i < (int)CurrencyTypesDep.max; i++)
			{
				CurrencyTypesDep type = (CurrencyTypesDep)i;
				symbols.Add(type.ToString());
			}

			for (int i=0; i<symbols.Count-1; i++)
			{
				for (int j=i+1; j<symbols.Count; j++)
				{
					CurrencyTypesDep @base = CurrencyHelpersDep.FromSymbol(symbols[i]);
					CurrencyTypesDep quote = CurrencyHelpersDep.FromSymbol(symbols[j]);

					string marketOld = CurrencyHelpersDep.GetMarketSymbolPair(@base, quote);
					string marketNew = CurrencyHelpers.GetMarketSymbolPair(m_currencies[@base.ToString()], m_currencies[quote.ToString()]);

					Assert.AreEqual(marketOld, marketNew);
				}
			}
		}

		[Test]
		public void GetBaseAndQuoteFromSymbolPair()
		{
			IEnumerable<string> markets = m_data.GetAllMarkets().Select<MarketRow, string>(r => r.symbol_pair);

			foreach (string m in markets)
			{
				CurrencyTypesDep baseOld, quoteOld;
				CurrenciesRow baseNew, quoteNew;
				CurrencyHelpersDep.GetBaseAndQuoteFromSymbolPair(m, out baseOld, out quoteOld);
				CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(m, m_currencies, out baseNew, out quoteNew);
				
				Assert.AreEqual(baseOld.ToString(), baseNew.ToString());
				Assert.AreEqual(quoteOld.ToString(), quoteNew.ToString());
			}
		}

		[Test]
		public void UnknownAsset()
		{
			Assert.Throws( typeof(ArgumentException), ()=>CurrencyHelpersDep.FromSymbol("ldss") );
			Assert.Throws(typeof(KeyNotFoundException), () => CurrencyHelpers.FromSymbol("ldss", m_currencies));
		}
	}
}
