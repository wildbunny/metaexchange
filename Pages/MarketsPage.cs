using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

using ServiceStack.Text;

using Monsterer.Request;

using WebHost;
using WebHost.Components;
using WebHost.WebSystem;

using MetaExchange.ClientApis;
using MetaExchange.MetaApi;

namespace MetaExchange.Pages
{
	public class MarketsPage : SharedPage
	{
		public override async Task Render<T>(RequestContext ctx, StringWriter stream, T authObj)
		{
			string[] wildcards = ctx.Request.GetWildcardParameters(2).ToArray();
			
			string baseSymbol = wildcards[0];
			string quoteSymbol = wildcards[1];

			BitsharesApi api = new BitsharesApi(Constants.kClientUrl);

			MetaAsset baseAsset = await api.GetAsset(baseSymbol);
			MetaAsset quoteAsset = await api.GetAsset(quoteSymbol);

			string market = wildcards[0] + "/" + wildcards[1];
			
			AddResource(new MetaResource("market", market));
			AddResource(new MetaResource("baseAsset", WebUtility.UrlEncode( JsonSerializer.SerializeToString(baseAsset) )) );
			AddResource(new MetaResource("quoteAsset", WebUtility.UrlEncode( JsonSerializer.SerializeToString(quoteAsset) )) );
						

			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, HtmlAttributes.@class, "container-fluid"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "row",
												"ng-app", "BitShares"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-3 col-md-2",
													"ng-controller", "GetMarkets"))
					{
						using (new Panel(stream, "Active markets", "panel panel-default", true, "", null, "panel-body noPadding"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "list-group smallFont"))
							{
								Href(stream, "<span class=\"pull-left\">{{m.m_base}}/{{m.m_quote}}</span><span class=\"pull-right\">{{m.m_lastPrice | number : m.m_dps}}</span>",
										HtmlAttributes.@class, "list-group-item clearfix",
										"active-link", "active",
										"ng-repeat", "m in results.m_active",
										HtmlAttributes.href, "/markets/{{m.m_base}}/{{m.m_quote}}");
							}
						}

						using (new Panel(stream, "Inactive markets", "panel panel-default", true, "", null, "panel-body noPadding"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "list-group smallFont"))
							{
								Href(stream, "<span class=\"pull-left\">{{m.m_base}}/{{m.m_quote}}</span><span class=\"pull-right\">{{m.m_lastPrice | number : m.m_dps}}</span>",
										HtmlAttributes.@class, "list-group-item clearfix",
										"active-link", "active",
										"ng-repeat", "m in results.m_inactive",
										HtmlAttributes.href, "/markets/{{m.m_base}}/{{m.m_quote}}");
							}
						}
					}

					using (new DivContainer(stream, (object)"ng-controller", "GetOrderbook", 
													HtmlAttributes.@class, "col-sm-8 col-md-10"))
					{
						using (new Panel(stream, market + " market", "panel panel-default", true, "", (s)=>RenderTimeframeButtons(s,market)))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "candlestickChart"/*, 
															HtmlAttributes.id, "candlestickChartId"*/))
							{
								stream.WriteLine("<canvas id=\"candlestickChartId\"></canvas>");
							}
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "row"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
							{
								using (new Panel(stream, "Bids", "panel panel-success"))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "wbScrollList"))
									{
										using (var t = new Table(stream, "", 4, 1, "table table-striped table-condensed smallFont", "Price (" + quoteSymbol + ")", baseSymbol, "Depth"))
										{
											t.Out("<tr ng-repeat=\"d in results.m_bids\">");
											t.Out("<td>{{d.m_price  | number : dpQuote}}</td>");
											t.Out("<td>{{d.m_volume | number : dpBase}}</td>");
											t.Out("<td><div class=\"progress\"><div class=\"progress-bar progress-bar-success\" style=\"width:{{getFraction($index, results.m_bids, results)}}%; float:right\"</div></div></td>");
											t.Out("</tr>");
										}
									}
								}
							}
							using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
							{
								using (new Panel(stream, "Asks", "panel panel-danger"))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "wbScrollList"))
									{
										using (var t = new Table(stream, "", 4, 1, "table table-striped table-condensed smallFont", "Depth", baseSymbol, "Price (" + quoteSymbol + ")"))
										{
											t.Out("<tr ng-repeat=\"d in results.m_asks\">");
											t.Out("<td><div class=\"progress\"><div class=\"progress-bar progress-bar-danger\" style=\"width:{{getFraction($index, results.m_asks, results)}}%\"</div></div></td>");
											t.Out("<td>{{d.m_volume | number : dpBase}}</td>");
											t.Out("<td>{{d.m_price | number : dpQuote}}</td>");
											t.Out("</tr>");
										}
									}
								}
							}
						}

						using (new DivContainer(stream,  (object)"ng-controller", "GetTrades"))
						{
							using (new Panel(stream, "Recent trades"))
							{
								using (var t = new Table(stream, "", 4, 1, "table table-striped table-condensed smallFont", "Buy type", "Sell type", "Price (" + quoteSymbol + ")", baseSymbol, quoteSymbol, "Date"))
								{
									t.Out("<tr ng-repeat=\"d in results\">");
									t.Out("<td>{{d.m_buyType}}</td>");
									t.Out("<td>{{d.m_sellType}}</td>");
									t.Out("<td>{{d.m_buyPrice | number : dpQuote}}</td>");
									t.Out("<td>{{d.m_buyAmount | number : dpBase}}</td>");
									t.Out("<td>{{d.m_sellAmount | number : dpQuote}}</td>");
									t.Out("<td>{{d.m_date | date : 'HH:mm:ss MM/dd/yy'}}</td>");
									t.Out("</tr>");
								}
							}
						}
					}
				}
			}
		}


		void RenderDepthTable(StringWriter stream, string depthName)
		{
			using (var t = new Table(stream, "", 4, 1, "table table-striped table-condensed smallFont", "Price", "Volume", "Sum"))
			{
				t.Out("<tr ng-repeat=\"d in " + depthName + "\" ng->");
				t.Out("<td>{{d.m_price}}</td>");
				t.Out("<td>{{d.m_volume}}</td>");
				t.Out("<td>{{getAccumulated($index, " + depthName + ")}}</td>");
				t.Out("</tr>");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override string GetPageSpecificJsFilename()
		{
			return "pages/requiredjs/markets.rs";
		}

		void RenderTimeframeButtons(StringWriter stream, string market)
		{
			using (new Span(stream, HtmlAttributes.@class, "pull-right"))
			{
				using (var cf = new FormContainer(stream,	HtmlAttributes.action, "/getOhlc",
															HtmlAttributes.handler, "OnChartTimeframe",
															HtmlAttributes.@class, "btn-group inline",
															HtmlAttributes.ajax, "true",
															HtmlAttributes.method, "post",
															HtmlAttributes.id, "timeframeFormId"))
				{
					cf.Input(stream, HtmlAttributes.type, "hidden",	HtmlAttributes.name, "market", HtmlAttributes.value, market);
					cf.Input(stream, HtmlAttributes.type, "hidden", HtmlAttributes.name, "start", HtmlAttributes.value, DateTime.UtcNow.ToString());
					cf.Input(stream, HtmlAttributes.type, "hidden", HtmlAttributes.name, "bars", HtmlAttributes.value, 100);

					using (new DivContainer(stream, HtmlAttributes.@class, "btn-group",
													"data-toggle", "buttons"))
					{
						Timeframe [] timeframes = 
						{
							Timeframe.M1,
							Timeframe.M5,
							Timeframe.M15,
							Timeframe.M30,
							Timeframe.H1,
							Timeframe.H4,
							Timeframe.H12,
							Timeframe.D1,
							Timeframe.W1
						};

						foreach (Timeframe tf in timeframes)
						{
							using (new Label(stream, HtmlAttributes.@class, "btn btn-success btn-xs"))
							{
								cf.Input(stream,	HtmlAttributes.type, InputTypes.radio, 
													HtmlAttributes.value, tf, 
													HtmlAttributes.name, "timeframe");
								stream.WriteLine(Enum.GetName(typeof(Timeframe), tf));
							}
						}
					}
				}
			}
		}
	}
}
