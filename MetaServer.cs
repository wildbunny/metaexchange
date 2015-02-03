using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Monsterer.Util;
using Monsterer.Request;

using WebHost;
using WebHost.Components;
using WebHost.WebSystem;

using ApiHost;

using BitsharesRpc;
using BitcoinRpcSharp;

using MetaExchange.Pages;

using WebDaemonShared;
using RestLib;

using MySqlDatabase;

namespace MetaExchange
{
	public class IDummy
	{
		public Database m_database;
		public string m_bitsharesAccount;
	}

	/// <summary>	Dummy authenticator to give pages access to the database </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	class MysqlAuthenticator : Authentication<IDummy>
	{
		Database m_database;
		string m_bitsharesAccount;

		public MysqlAuthenticator(string database, string databaseUser, string password, 
									int allowedThreadId, string bitsharesAccount)
			: base()
		{
			m_database = new Database(database, databaseUser, password, allowedThreadId);
			m_bitsharesAccount = bitsharesAccount;
		}

		public override string GenerateToken(RequestContext ctx, IDummy authObj)
		{
			return "token";
		}

		public override void PostAuthorise(RequestContext ctx, IDummy authObj)
		{
		}

		public override IDummy Authorise(RequestContext ctx)
		{
			return new IDummy { m_database = m_database, m_bitsharesAccount = m_bitsharesAccount };
		}

		public Database m_Database
		{
			get { return m_database; }
		}
	}

	/// <summary>	Concrete implementation taking our auth object </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	public class MetaWebServer : WebServer<IDummy>
	{
		public MetaWebServer(IEnumerable<string> listenOn, string webRoot, Authentication<IDummy> authenticator = null, 
								bool considerBanOnApiException = true, 
								eDdosMaxRequests maxDdosAccesses = eDdosMaxRequests.Ten, 
								eDdosInSeconds inThisManySecondsDdos = eDdosInSeconds.One, 
								int maxExceptions = 10, int maxMultipartBodySize = 50000) :
								base(listenOn, webRoot, authenticator, considerBanOnApiException, maxDdosAccesses, inThisManySecondsDdos,
										maxExceptions, maxMultipartBodySize)
		{ 
		}
	}

	/// <summary>	Main metaexchange site </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	public class MetaServer : IDisposable
	{
		public static string m_gUrlBase;

		BitsharesWallet m_bitshares;
		BitcoinWallet m_bitcoin;

		MetaWebServer m_server;

		public EventHandler<ExceptionWithCtx> ExceptionEvent;

		MysqlAuthenticator m_authenticate;

		public MetaServer(	string uri, string webroot, RpcConfig bitsharesConfig, RpcConfig bitcoinRpcConfig, string apiBaseUrl,
							string database, string databaseUser, string databasePassword,
							string bitsharesAccount)
		{
			m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			m_bitcoin = new BitcoinWallet(bitcoinRpcConfig.m_url, bitcoinRpcConfig.m_rpcUser, bitcoinRpcConfig.m_rpcPassword, false);

			m_gUrlBase = apiBaseUrl.TrimEnd('/');

			m_authenticate = new MysqlAuthenticator(database, databaseUser, databasePassword, System.Threading.Thread.CurrentThread.ManagedThreadId, bitsharesAccount);

			m_server = new MetaWebServer(new string[] { uri }, webroot, m_authenticate);
			
			m_server.ExceptionEvent += OnServerException;
			m_server.ExceptionOnWebServer += OnServerException;

			List<IHeadResource> fonts = new List<IHeadResource>();
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.eot", FontResource.kEmeddedOpenTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.svg", FontResource.kSvgMimeType, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.ttf", FontResource.kTrueTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.woff", FontResource.kWoffTypeMime, true));
			m_server.AddGlobalResources(fonts);

			// forwarding routes on to actual api server
			m_server.HandlePostRoute(Routes.kSubmitAddress, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false, true);
			m_server.HandleGetRoute(Routes.kGetStats, OnGetStats, eDdosMaxRequests.Two, eDdosInSeconds.One, true, false);

			// serve the pages
			m_server.HandlePageRequest<MainPage>("/", eDdosMaxRequests.Two, eDdosInSeconds.One, true);
			m_server.HandlePageRequest<ApiPage>("/apiDocs", eDdosMaxRequests.Two, eDdosInSeconds.One, true);
			m_server.HandlePageRequest<FaqPage>("/faq", eDdosMaxRequests.Two, eDdosInSeconds.One, true);
		}

		public void Dispose()
		{
			m_server.Dispose();
		}

		void OnServerException(object sender, ExceptionWithCtx e)
		{
			if (ExceptionEvent != null)
			{
				ExceptionEvent(sender, e);
			}
		}

		/// <summary>	Starts this object. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		public void Start()
		{
			m_server.Start();
		}

		/// <summary>	Updates this object. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		public async void Update()
		{
			// call out to the daemon to get stats on transaction sizes and pricing info
			try
			{
				StatsPacket statsPacket = await Rest.JsonApiGetAsync<StatsPacket>(ApiUrl(Routes.kGetStats));

				SiteStatsRow stats = statsPacket.m_stats;

				// stuff it in our database
				m_authenticate.m_Database.Statement("UPDATE stats SET bid_price=@b, ask_price=@s, max_btc=@maxbtc, max_bitassets=@maxBit, last_update=@last;",
														stats.bid_price,
														stats.ask_price,
														stats.max_btc,
														stats.max_bitassets,
														DateTime.UtcNow);

				m_authenticate.m_Database.Statement("TRUNCATE transactions;");

				foreach (TransactionsRow t in statsPacket.m_lastTransactions)
				{
					m_authenticate.m_Database.Statement("INSERT INTO transactions (received_txid, sent_txid, amount,type,asset,date) VALUES(@a,@b,@c,@d,@e,@f);",
															t.received_txid, t.sent_txid, t.amount, t.type, t.asset, t.date);
				}
			}
			catch (WebException) { }
		}

		/// <summary>	API URL. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="relUrl">	URL of the relative. </param>
		///
		/// <returns>	A string. </returns>
		static public string ApiUrl(string relUrl)
		{
			return m_gUrlBase + relUrl;
		}

		/// <summary>	Forward post. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		async Task ForwardPost(RequestContext ctx, IDummy dummy)
		{
			// forward request on
			string result = await Rest.ExecutePostAsync(ApiUrl(ctx.Request.Url.LocalPath), ctx.Request.PostArgString);
			ctx.RespondJsonFromString(result);
		}

		/// <summary>	Executes the get statistics action. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetStats(RequestContext ctx, IDummy dummy)
		{
			SiteStatsRow stats = dummy.m_database.Query<SiteStatsRow>("SELECT * FROM stats;").FirstOrDefault();

			List<TransactionsRow> lastTransactions = dummy.m_database.Query<TransactionsRow>("SELECT * FROM transactions ORDER BY date DESC;");

			StatsPacket packet = new StatsPacket { m_stats = stats, m_lastTransactions = lastTransactions };

			ctx.Respond<StatsPacket>(packet);
			return null;
		}

		/*async Task GetMarkets(RequestContext ctx, IDummy authObj)
		{
			AllMetaMarkets markets = await m_api.GetAllMarkets();

			ctx.Respond<AllMetaMarkets>(markets);
		}

		async Task GetTrades(RequestContext ctx, IDummy authObj)
		{
			string market = RestHelpers.GetPostArg<string, ApiRuntimeExceptionMissingParameter>(ctx, "market");
			ctx.Respond<List<MetaTrade>>(await m_api.GetTrades(market));
		}

		async Task GetOrderbook(RequestContext ctx, IDummy authObj)
		{
			string market = RestHelpers.GetPostArg<string, ApiRuntimeExceptionMissingParameter>(ctx, "market");

			ctx.Respond<MetaOrderbook>( await m_api.GetOrderbook(market, 20) );
		}

		async Task GetOhlc(RequestContext ctx, IDummy authObj)
		{
			string market = RestHelpers.GetPostArg<string, ApiRuntimeExceptionMissingParameter>(ctx, "market");
			DateTime start = RestHelpers.GetPostArg<DateTime, ApiRuntimeExceptionMissingParameter>(ctx, "start");
			Timeframe timeframe = RestHelpers.GetPostArg<Timeframe, ApiRuntimeExceptionMissingParameter>(ctx, "timeframe");
			int numBars = RestHelpers.GetPostArg<int, ApiRuntimeExceptionMissingParameter>(ctx, "bars");

			ctx.Respond<List<MetaOhlc>>(await m_api.GetOlhc(market, start, timeframe, numBars));
		}*/

		public BitsharesWallet m_Wallet
		{
			get { return m_bitshares; }
		}

		/// <summary>	IP lock. </summary>
		///
		/// <remarks>	Paul, 03/02/2015. </remarks>
		///
		/// <param name="ipAddress">	The IP address. </param>
		public void SetIpLock(string ipAddress)
		{
			m_server.SetIpLock(ipAddress);
		}
	}
}
