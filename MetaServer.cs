using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics;

using Monsterer.Util;
using Monsterer.Request;
using WebHost;
using WebHost.Components;
using WebHost.WebSystem;
using ApiHost;
using ServiceStack.Text;
using MetaExchange.Pages;
using WebDaemonShared;
using WebDaemonSharedTables;
using RestLib;
using RedisCache;
using MySqlDatabase;
using MetaData;

namespace MetaExchange
{
	

	

	/// <summary>	Concrete implementation taking our auth object </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	public class MetaWebServer : WebServer<IDummy>
	{
		public MetaWebServer(IEnumerable<string> listenOn, string webRoot, Authentication<IDummy> authenticator = null, 
								bool considerBanOnApiException = true, 
								eDdosMaxRequests maxDdosAccesses = eDdosMaxRequests.Ten, 
								eDdosInSeconds inThisManySecondsDdos = eDdosInSeconds.One, 
								int maxExceptions = 10, int maxMultipartBodySize = 50000,
								bool forwardToSsl = false) :
								base(listenOn, webRoot, authenticator, considerBanOnApiException, maxDdosAccesses, inThisManySecondsDdos,
										maxExceptions, maxMultipartBodySize, forwardToSsl)
		{ 
		}
	}

	/// <summary>	Main metaexchange site </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	public partial class MetaServer : IDisposable
	{
		MetaWebServer m_server;
		SharedApi<IDummy> m_api;
		MysqlAuthenticator m_auth;
		Dictionary<string, CurrenciesRow> m_allCurrencies;

		string m_webAddress;

		public EventHandler<ExceptionWithCtx> ExceptionEvent;

		public MetaServer(	string uri, string webroot, 
							string database, string databaseUser, string databasePassword,
							bool maintenance)
		{
			RedisWrapper.Initialise("meta");
			Serialisation.Defaults();

			m_auth = new MysqlAuthenticator(database, databaseUser, databasePassword, Thread.CurrentThread.ManagedThreadId);

			m_allCurrencies = m_auth.m_Database.GetAllCurrencies();

			string[] listenOn = uri.Split(',');

			m_webAddress = listenOn.First();

			#if MONO
			bool forwardToSsl=!Debugger.IsAttached;
			#else
			bool forwardToSsl=false;
			#endif

			m_server = new MetaWebServer(listenOn, webroot, m_auth, true, eDdosMaxRequests.Ten, eDdosInSeconds.One, 10, 50000, forwardToSsl);
			
			m_server.ExceptionEvent += OnServerException;
			m_server.ExceptionOnWebServer += OnServerException;

			List<IHeadResource> fonts = new List<IHeadResource>();
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.eot", FontResource.kEmeddedOpenTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.svg", FontResource.kSvgMimeType, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.ttf", FontResource.kTrueTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.woff", FontResource.kWoffTypeMime, true));
			m_server.AddGlobalResources(fonts);

			if (maintenance)
			{
				m_server.m_HttpServer.ReplaceUnhandledRouteObserver( async ctx => ctx.Respond( await m_server.HandleRequest<MaintenancePage>(ctx, m_auth.Authorise(ctx)), HttpStatusCode.OK));
			}
			else
			{
				m_api = new SharedApi<IDummy>(m_auth.m_Database);

				PullInitialData();

				// forwarding routes on to actual api server
				m_server.HandlePostRoute(Routes.kSubmitAddress,			OnSubmitAddress, eDdosMaxRequests.Five, eDdosInSeconds.One, true, true);
				m_server.HandlePostRoute(Routes.kGetOrderStatus,		m_api.OnGetOrderStatus, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetMarket,				OnGetMarket, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetLastTransactions,	m_api.OnGetLastTransactions, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetMyLastTransactions, m_api.OnGetMyLastTransactions, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandleGetRoute(Routes.kGetAllMarkets,			OnGetAllMarkets, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandleGetRoute(Routes.kProduceReport,			m_api.OnProduceReport, eDdosMaxRequests.One, eDdosInSeconds.One, true, false);

				// handle push from daemons
				m_server.HandlePostRoute(Routes.kPushFees,				OnPushFeeCollection,	eDdosMaxRequests.Unlimited, eDdosInSeconds.Ignore, true, false);
				m_server.HandlePostRoute(Routes.kPushTransactions,		OnPushTransactions,		eDdosMaxRequests.Unlimited, eDdosInSeconds.Ignore, true, true);
				m_server.HandlePostRoute(Routes.kPushMarket,			OnPushMarket,			eDdosMaxRequests.Unlimited, eDdosInSeconds.Ignore, true, false);
				
				
				// serve the pages
				m_server.HandlePageRequest<MarketsPage>("/", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
				m_server.HandlePageRequest<MainPage>("/markets/{base}/{quote}", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
				m_server.HandlePageRequest<ApiPage>("/apiDocs", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
				m_server.HandlePageRequest<FaqPage>("/faq", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
			}
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
		async public void Update()
		{
			// ping all the daemons
			m_allCurrencies = m_auth.m_Database.GetAllCurrencies();
			List<MarketRow> allMarkets = m_auth.m_Database.GetAllMarkets();
			List<string> allDaemons = allMarkets.Select<MarketRow, string>(r => r.daemon_url).Distinct().ToList();

			foreach (string daemon in allDaemons)
			{
				bool up = false;
				try
				{
					string response = await Rest.ExecuteGetAsync(ApiUrl(daemon, Routes.kGetAllMarkets), 5000);

					ApiError exception = GetExceptionFromResult(response);
					if (exception == null)
					{
						List<MarketRow> daemonMarkets = JsonSerializer.DeserializeFromString<List<MarketRow>>(response);
						foreach (MarketRow m in daemonMarkets)
						{
							m_auth.m_Database.UpdateMarketInDatabase(m);
						}
						up = true;
					}
				}
				catch (Exception)
				{
					up = false;
				}

				m_auth.m_Database.UpdateMarketStatus(daemon, up);
			}

			// collect market stats
			foreach (MarketRow r in allMarkets)
			{
				decimal btcVolume24h = m_auth.m_Database.Get24HourBtcVolume(r.symbol_pair, r.flipped);
				LastPriceAndDelta lastPrice = m_auth.m_Database.GetLastPriceAndDelta(r.symbol_pair);

				decimal realisedSpreadPercent = 100 * (1 - r.bid/r.ask);

				m_Database.UpdateMarketStats(r.symbol_pair, btcVolume24h, lastPrice.last_price, lastPrice.price_delta, realisedSpreadPercent);
			}
		}

		

		/// <summary>	API URL. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="daemonBase">	The daemon base. </param>
		/// <param name="relUrl">	 	URL of the relative. </param>
		///
		/// <returns>	A string. </returns>
		static public string ApiUrl(string daemonBase, string relUrl)
		{
			return daemonBase.TrimEnd('/') + relUrl;
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

		/// <summary>	Gets the database. </summary>
		///
		/// <value>	The m database. </value>
		public MySqlData m_Database
		{
			get { return m_auth.m_Database; }
		}

		/// <summary>	Gets the API. </summary>
		///
		/// <value>	The m API. </value>
		public SharedApi<IDummy> m_Api
		{
			get { return m_api; }
		}
	}
}
