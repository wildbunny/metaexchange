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
//using BitsharesRpc;
//using BitcoinRpcSharp;
using MetaExchange.Pages;
using WebDaemonShared;
using WebDaemonSharedTables;
using RestLib;
using RedisCache;
using MySqlDatabase;

namespace MetaExchange
{
	public class IDummy
	{
		public Database m_database;
		public string m_bitsharesAccount;
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
	public class MetaServer : IDisposable
	{
		public static string m_gUrlBase;

		//BitsharesWallet m_bitshares;
		//BitcoinWallet m_bitcoin;

		MetaWebServer m_server;

		public EventHandler<ExceptionWithCtx> ExceptionEvent;

		public MetaServer(string uri, string webroot, string apiBaseUrl, bool maintenance)
		{
			RedisWrapper.Initialise("meta");
			Serialisation.Defaults();

			//m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			//m_bitcoin = new BitcoinWallet(bitcoinRpcConfig.m_url, bitcoinRpcConfig.m_rpcUser, bitcoinRpcConfig.m_rpcPassword, false);

			m_gUrlBase = apiBaseUrl.TrimEnd('/');

			string[] listenOn = uri.Split(',');

			#if MONO
			bool forwardToSsl=true;
			#else
			bool forwardToSsl=false;
			#endif

			m_server = new MetaWebServer(listenOn, webroot, null, true, eDdosMaxRequests.Ten, eDdosInSeconds.One, 10, 50000, forwardToSsl);
			
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
				// forwarding routes on to actual api server
				m_server.HandlePostRoute(Routes.kSubmitAddress, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePostRoute(Routes.kGetOrderStatus, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePostRoute(Routes.kGetMarket, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePostRoute(Routes.kGetLastTransactions, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePostRoute(Routes.kGetMyLastTransactions, ForwardPost, eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandleGetRoute(Routes.kGetAllMarkets, ForwardGet, eDdosMaxRequests.Two, eDdosInSeconds.One, false);


				// serve the pages
				m_server.HandlePageRequest<MainPage>("/", eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePageRequest<ApiPage>("/apiDocs", eDdosMaxRequests.Two, eDdosInSeconds.One, false);
				m_server.HandlePageRequest<FaqPage>("/faq", eDdosMaxRequests.Two, eDdosInSeconds.One, false);
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
		public async void Update()
		{
			
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
			try
			{
				string result = await Rest.ExecutePostAsync(ApiUrl(ctx.Request.Url.LocalPath), ctx.Request.PostArgString);
				ctx.RespondJsonFromString(result);
			}
			catch (WebException)
			{
				ctx.Respond(HttpStatusCode.InternalServerError);
			}
		}

		/// <summary>	Forward get. </summary>
		///
		/// <remarks>	Paul, 06/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		async Task ForwardGet(RequestContext ctx, IDummy dummy)
		{
			// forward request on
			try
			{
				string result = await Rest.ExecuteGetAsync(ApiUrl(ctx.Request.Url.LocalPath) + "?" + ctx.Request.Url.Query);
				ctx.RespondJsonFromString(result);
			}
			catch (WebException)
			{
				ctx.Respond(HttpStatusCode.InternalServerError);
			}
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
