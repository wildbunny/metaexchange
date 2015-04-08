using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MetaData;
using NUnit.Framework;
using SharpTestsEx;
using RestLib;
using WebDaemonShared;
using ServiceStack.Text;
using MetaDaemon;
using Monsterer.Util;
using ApiHost;
using WebDaemonSharedTables;
using MetaDaemon.Markets;
using BitsharesRpc;

namespace MetaDaemonUnitTests
{
	[TestFixture]
	public class MiscTest : TestBase
	{
		[Test]
		public void GetMarketNoParams()
		{
			PostNoParameters(Routes.kGetMarket);
		}

		[Test]
		public void GetMarketInvalidParams()
		{
			PostInvalidParameters(Routes.kGetMarket);
		}

		[Test]
		public void GetMarketInvalidMarket()
		{
			string market = "BTC_BTC";
			string content = Post(Routes.kGetMarket, RestHelpers.BuildPostArgs(WebForms.kSymbolPair, market));
			ReplyIsApiError(content, new ApiExceptionUnknownMarket(market));
		}

		[Test]
		public void GetMarketSuccess()
		{
			GetMarketSuccess(m_defaultSymbolPair);
		}

		[Test]
		public void GetMarketSuccessTwoMarkets()
		{
			GetMarketSuccess(m_defaultSymbolPair);
			GetMarketSuccess(m_alternateMarket);
		}

		void GetMarketSuccess(string market)
		{
			string content = Post(Routes.kGetMarket, RestHelpers.BuildPostArgs(WebForms.kSymbolPair, market));

			MarketRow check = GetMarket(market);
			string reality = JsonSerializer.SerializeToString<MarketRow>(check);

			content.Should().Be.EqualTo(reality);
		}

		[Test]
		public void GetAllMarketsSuccess()
		{
			GetAllMarketsSuccess( Get(Routes.kGetAllMarkets) );
		}

		[Test]
		public void GetAllMarketsInvalidParams()
		{
			GetAllMarketsSuccess(Get(Routes.kGetAllMarkets + RestHelpers.BuildArgs("A", "F")));
		}

		void GetAllMarketsSuccess(string content)
		{
			List<MarketRow> allMarkets = JsonSerializer.DeserializeFromString<List<MarketRow>>(content);
			List<MarketRow> allMarketsCompare = GetAllMarkets();

			allMarkets.Count.Should().Be.EqualTo(allMarketsCompare.Count);

			for (int i=0; i<allMarkets.Count; i++)
			{
				Assert.IsTrue( PublicInstanceFieldsEqual<MarketRow>(allMarkets[i], allMarketsCompare[i]) );
			}
		}

		[Test]
		public void GetOrderStatusNoParams()
		{
			PostNoParameters(Routes.kGetOrderStatus);
		}

		[Test]
		public void GetOrderStatusInvalidParams()
		{
			PostInvalidParameters(Routes.kGetOrderStatus);
		}

		[Test]
		public void GetOrderStatusNotFound()
		{
			string txid = "sfkjshfkh";
			string content = Post(Routes.kGetOrderStatus, RestHelpers.BuildPostArgs(WebForms.kTxId, txid));
			ReplyIsApiError(content, new ApiExceptionOrderNotFound(txid));
		}

		[Test]
		public void GetLastTransactionsNoParams()
		{
			PostNoParameters(Routes.kGetLastTransactions);
		}

		[Test]
		public void GetLastTransactionsInvalidParams()
		{
			PostInvalidParameters(Routes.kGetLastTransactions);
		}

		[Test]
		public void GetMyLastTransactionsNoParams()
		{
			PostNoParameters(Routes.kGetMyLastTransactions);
		}

		[Test]
		public void GetMyLastTransactionsInvalidParams()
		{
			PostInvalidParameters(Routes.kGetMyLastTransactions);
		}

		[Test]
		public void GetLastTransactionsZeroLimit()
		{
			string content = Post(Routes.kGetLastTransactions, RestHelpers.BuildPostArgs(WebForms.kLimit, 0));

			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		[Test]
		public void GetLastTransactionsNegativeLimit()
		{
			string content = Post(Routes.kGetLastTransactions, RestHelpers.BuildPostArgs(WebForms.kLimit, -1));
			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		[Test]
		public void GetMyLastTransactionsNoMemoOrDepositAddress()
		{
			string content = Post(Routes.kGetMyLastTransactions, RestHelpers.BuildPostArgs(WebForms.kLimit, 1));
			string reality = JsonSerializer.SerializeToString<TransactionsRow[]>(new TransactionsRow[]{});

			content.Should().Be.EqualTo(reality);
		}

		[Test]
		public void GetMyLastTransactionsZeroLimit()
		{
			string content = Post(Routes.kGetMyLastTransactions, RestHelpers.BuildPostArgs(WebForms.kLimit, 0));

			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		[Test]
		public void GetMyLastTransactionsNegativeLimit()
		{
			string content = Post(Routes.kGetMyLastTransactions, RestHelpers.BuildPostArgs(WebForms.kLimit, -1));
			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		[Test]
		public void VerifiyAccountNameWithHipan()
		{
			Assert.IsTrue(BitsharesWallet.IsValidAccountName("argentina-marketing.matt608"));
		}

		[Test]
		public void DateTimeZone()
		{
			DateTime t = m_data.GetTransaction("4b9134079ae5d2091cab4a20de477959a10479040533ff0e2efea48511e4c76d").date;

			Console.WriteLine(t.Kind);
			Console.WriteLine(t.ToUniversalTime());
		}
	}
}
