using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monsterer.Util;
using Monsterer.Request;

using WebHost;
using WebHost.Components;
using WebHost.WebSystem;

using ApiHost;

using BitsharesRpc;
using BitcoinRpcSharp;

using MetaExchange.Pages;



using Newtonsoft.Json;

namespace MetaExchange
{
	interface IDummy {}

	public class MetaServer : IDisposable
	{
		BitsharesWallet m_bitshares;
		BitcoinWallet m_bitcoin;

		WebServer<IDummy> m_server;

		public EventHandler<ExceptionWithCtx> ExceptionEvent;

		public MetaServer(string uri, string webroot, RpcConfig bitsharesConfig, RpcConfig bitcoinRpcConfig)
		{
			m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			m_bitcoin = new BitcoinWallet(bitcoinRpcConfig.m_url, bitcoinRpcConfig.m_rpcUser, bitcoinRpcConfig.m_rpcPassword, false);

			m_server = new WebServer<IDummy>(new string[] {uri}, webroot);
			
			m_server.ExceptionEvent += OnServerException;
			m_server.ExceptionOnWebServer += OnServerException;

			List<IHeadResource> fonts = new List<IHeadResource>();
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.eot", FontResource.kEmeddedOpenTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.svg", FontResource.kSvgMimeType, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.ttf", FontResource.kTrueTypeMime, true));
			fonts.Add(new FontResource(webroot, "/fonts/glyphicons-halflings-regular.woff", FontResource.kWoffTypeMime, true));
			m_server.AddGlobalResources(fonts);

			/*m_server.HandlePostRoute("/getMarkets", GetMarkets, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false, true);
			m_server.HandlePostRoute("/getOrderbook", GetOrderbook, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false, true);
			m_server.HandlePostRoute("/getTrades", GetTrades, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false, true);
			m_server.HandlePostRoute("/getOhlc", GetOhlc, eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false, true);*/

			m_server.HandlePageRequest<MainPage>("/", eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false);
			//m_server.HandlePageRequest<MarketsPage>("/markets/{base}/{quote}", eDdosMaxRequests.Ignore, eDdosInSeconds.Ignore, false, true);
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

		public void Start()
		{
			m_server.Start();
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
	}
}
