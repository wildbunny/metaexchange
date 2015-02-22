using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

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

		public EventHandler<ExceptionWithCtx> ExceptionEvent;

		public MetaServer(	string uri, string webroot, 
							string database, string databaseUser, string databasePassword,
							bool maintenance)
		{
			RedisWrapper.Initialise("meta");
			Serialisation.Defaults();

			m_auth = new MysqlAuthenticator(database, databaseUser, databasePassword, Thread.CurrentThread.ManagedThreadId);

			string[] listenOn = uri.Split(',');

			#if MONO
			bool forwardToSsl=true;
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
				m_server.m_HttpServer.ReplaceUnhandledRouteObserver( async ctx => ctx.Respond( await m_server.HandleRequest<MaintenancePage>(ctx, null), HttpStatusCode.OK));
			}
			else
			{
				m_api = new SharedApi<IDummy>(m_auth.m_Database);

				PullInitialData();

				// forwarding routes on to actual api server
				m_server.HandlePostRoute(Routes.kSubmitAddress,			OnSubmitAddress, eDdosMaxRequests.Five, eDdosInSeconds.One, true, true);
				m_server.HandlePostRoute(Routes.kGetOrderStatus,		m_api.OnGetOrderStatus, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetMarket,				m_api.OnGetMarket, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetLastTransactions,	m_api.OnGetLastTransactions, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kGetMyLastTransactions, m_api.OnGetMyLastTransactions, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandleGetRoute(Routes.kGetAllMarkets,			m_api.OnGetAllMarkets, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);

				// handle push from daemons
				m_server.HandlePostRoute(Routes.kPushSenderToDeposit,	OnPushSenderToDeposit, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kPushTransactions,		OnPushTransactions, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				m_server.HandlePostRoute(Routes.kPushMarket,			OnPushMarket, eDdosMaxRequests.Five, eDdosInSeconds.One, true, false);
				
				
				// serve the pages
				m_server.HandlePageRequest<MainPage>("/", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
				m_server.HandlePageRequest<MainPage>("/markets/{market}", eDdosMaxRequests.Five, eDdosInSeconds.One, true);
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
			List<MarketRow> allMarkets = m_auth.m_Database.GetAllMarkets();
			List<string> allDaemons = allMarkets.Select<MarketRow, string>(r => r.daemon_url).Distinct().ToList();

			foreach (string daemon in allDaemons)
			{
				bool up;
				try
				{
					await Rest.ExecuteGetAsync(ApiUrl(daemon, Routes.kPing), 5000);
					up = true;
				}
				catch (Exception)
				{
					up = false;
				}

				m_auth.m_Database.UpdateMarketStatus(daemon, up);
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
	}
}
