using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

using ServiceStack.Text;
using MySqlDatabase;
using RedisCache;
using Monsterer.Util;
using MetaDaemon;
using BitsharesRpc;
using RestLib;
using NUnit;
using SharpTestsEx;
using WebDaemonShared;
using MetaData;

namespace MetaDaemonUnitTests
{
	public class TestBase : IDisposable
    {
		const string kDatabaseName = "metaexchange";
		const string kDatabaseUser = "metaexchange";
		const string kDatabasePassword = "ZSbyH7bGCz6BXHRY";
		protected string kApiRoot = "http://localhost:1236/";

		protected const string kBitcoinAddress = "n2HPFvf376vxQV1mour4pYjLRbrD9vZUpn";
		protected const string kBitsharesAccount = "monsterer";

		protected Database m_database;
		protected MetaDaemonApi m_api;

		protected string m_defaultSymbolPair;
		protected string m_alternateMarket;
 
		public TestBase()
		{
			m_database = new Database(kDatabaseName, kDatabaseUser, kDatabasePassword, Thread.CurrentThread.ManagedThreadId);

			RedisWrapper.Initialise("test");

			m_defaultSymbolPair = CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitBTC, CurrencyTypes.BTC);
			m_alternateMarket = CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitGOLD, CurrencyTypes.BTC);

			Thread thread = new Thread(() =>
			{
				string bitsharesUrl = "http://localhost:65066/rpc";
				string bitsharesUser = "rpc_user";
				string bitsharesPassword = "abcdefgh";
				string bitsharesAccount = "gatewaytest";

				string bitcoinUrl = "http://localhost:18332";
				string bitcoinUser = "bitcoinrpc";
				string bitcoinPassword = "HTQAHLqsETJJZ9WXpDg5jrU5bzLy9mnuV2qLG9gsHPoq";
				bool bitcoinUseTestNet = true;

				string database = kDatabaseName;
				string databaseUser = kDatabaseUser;
				string databasePassword = kDatabasePassword;

				string apiListen = kApiRoot;

				// create a scheduler so we can be sure of thread affinity
				AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

				m_api = new MetaDaemonApi(new RpcConfig { m_url = bitsharesUrl, m_rpcUser = bitsharesUser, m_rpcPassword = bitsharesPassword },
															new RpcConfig { m_url = bitcoinUrl, m_rpcUser = bitcoinUser, m_rpcPassword = bitcoinPassword, m_useTestnet = bitcoinUseTestNet },
															bitsharesAccount,
															database, databaseUser, databasePassword,
															apiListen, null, null, "gatewayclient", null, "192.168.0.2");

				m_api.m_ApiServer.m_HttpServer.m_DdosProtector.m_Enabled = false;

				scheduler.RunWithUpdate(m_api.Start, m_api.Update, 1);

				Console.WriteLine("meta thread exiting...");
			});

			thread.Start();
		}

		public void Dispose()
		{
			m_api.Dispose();
		}

		void OnException(Exception e)
		{
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
		}

		protected string Post(string url, string query)
		{
			return Rest.ExecutePostSync(kApiRoot + url.TrimStart('/'), query);
		}

		protected string Get(string url)
		{
			return Rest.ExecuteGetSync(kApiRoot + url.TrimStart('/'));
		}

		protected void PostNoParameters(string url)
		{
			string content = Post(url, "");
			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		protected void PostInvalidParameters(string url)
		{
			string content = Post(url, "v=3992399223");
			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		protected void ReplyIsApiError(string content, ApiException e)
		{
			string reality = JsonSerializer.SerializeToString<ApiError>(e.m_error);
			content.Should().Be.EqualTo(reality);
		}

		protected MarketRow GetMarket(string symbolPair)
		{
			return m_database.Query<MarketRow>("SELECT * FROM markets WHERE symbol_pair=@p;", symbolPair).FirstOrDefault();
		}

		protected List<MarketRow> GetAllMarkets()
		{
			return m_database.Query<MarketRow>("SELECT * FROM markets;");
		}

		public static bool PublicInstanceFieldsEqual<T>(T self, T to, params string[] ignore) where T : class
		{
			if (self != null && to != null)
			{
				Type type = typeof(T);
				List<string> ignoreList = new List<string>(ignore);
				foreach (FieldInfo pi in type.GetFields())
				{
					if (!ignoreList.Contains(pi.Name))
					{
						object selfValue = pi.GetValue(self);
						object toValue = pi.GetValue(to);

						if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
						{
							return false;
						}
					}
				}
				return true;
			}
			return self == to;
		}
    }
}
