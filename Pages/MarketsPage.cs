using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Monsterer.Request;

using WebHost;
using WebHost.Components;
using WebHost.WebSystem;

using WebDaemonShared;
using WebDaemonSharedTables;
using MetaExchange;

namespace MetaExchange.Pages
{
	public class MarketsPage : SharedPage
	{
		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if MONO
			AddResource(new JsResource(Constants.kWebRoot, "/js/marketsPageCompiled.js", true));
			#endif

			AddResource(new CssResource(Constants.kWebRoot, "/css/markets.css", true));

			ImgResource logo = CreateLogo();
			AddResource(logo);

			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, "ng-app", "myApp", "ng-controller", "MarketsController", HtmlAttributes.id, "rootId"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "jumbotron clearfix no-padding-bottom-top"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "container"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "row"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "col-xs-12"))
							{
								RenderJumbo(stream, logo);
							}
						}
					}
				}

				using (new DivContainer(stream, HtmlAttributes.@class, "container"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
						{
							using (new Table(stream, "", 4, 4, "table table-striped table-hover noMargin", new string[] 
								{ "",		"hidden-sm hidden-xs hidden-md",	"",			"",						"",			"hidden-xs",		"hidden-xs",	"hidden-xs hidden-sm",	"hidden-xs hidden-sm" },
								"Market",	"Currency",						"Price",	"Volume (BTC)", "Spread %", "Ask",				"Bid",				"Buy fee (%)",	"Sell fee (%)"))
							{
								using (var tr = new TR(stream, "ng-if", "!t.flipped", "ng-repeat", "t in allMarkets", HtmlAttributes.@class, "clickable-row",
																								"ng-click", 
																								"go(t)"))
								{
									tr.TD("{{renameSymbolPair(t.symbol_pair)}}");
									tr.TD("{{t.asset_name}}", HtmlAttributes.@class, "hidden-sm hidden-xs hidden-md");

									tr.TD("{{t.last_price}} <i class=\"glyphicon glyphicon-arrow-up text-success\"/>", "ng-if", "t.price_delta>0");
									tr.TD("{{t.last_price}} <i class=\"glyphicon glyphicon-arrow-down text-danger\"/>", "ng-if", "t.price_delta<0");
									tr.TD("{{t.last_price}} <i class=\"glyphicon glyphicon glyphicon-minus text-info\"/>", "ng-if", "t.price_delta==0");

									tr.TD("{{t.btc_volume_24h | number:2}}");
									tr.TD("{{t.realised_spread_percent | number:2}}");
									tr.TD("{{t.ask}}", HtmlAttributes.@class, "hidden-xs");
									tr.TD("{{t.bid}}", HtmlAttributes.@class, "hidden-xs");
									tr.TD("{{t.ask_fee_percent | number:2}}", HtmlAttributes.@class, "hidden-sm hidden-xs");
									tr.TD("{{t.bid_fee_percent | number:2}}", HtmlAttributes.@class, "hidden-sm hidden-xs");
								}
							}
						}
					}
				}
				

				//
				// bullet points
				// 
				using (new DivContainer(stream, HtmlAttributes.@class, "container",
													HtmlAttributes.style, "margin-top:20px"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-ok text-info\"></i>  No registration required");
							P("There is no need to register an account, just tell us where you'd like to receive the coins that you buy or sell.");
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-flash text-info\"></i>  Fast transactions");
							P("Only one confirmation is neccessary for buying or selling, which is around 7 minutes for a buy and around 3 seconds for a sell.");
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-lock text-info\"></i>  Safe");
							P("We don't hold any of our customer's funds, so there is nothing to get lost or stolen.");
						}
					}
				}

				using (new DivContainer(stream, HtmlAttributes.@class, "bg-primary hidden-xs",
												HtmlAttributes.style, "margin-top:20px"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "container"))
					{
						using (new DivContainer(stream, HtmlAttributes.style, "margin:30px 0px 30px 0px"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "row"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
								{
									H3("Recent transactions");

									using (new Table(stream, "", 4, 4, "table noMargin", "Market", "Type", "Price", "Amount", "Fee", "Date"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in transactions"))
										{
											tr.TD("{{renameSymbolPair(t.symbol_pair)}}");
											tr.TD("{{t.order_type}}");
											tr.TD("{{t.price}}");
											tr.TD("{{t.amount}}");
											tr.TD("{{t.fee}}");
											tr.TD("{{t.date*1000 | date:'MMM d, HH:mm'}}");
										}
									}
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override string GetPageSpecificJsFilename()
		{
			return "Pages/RequiredJs/Markets.rs";
		}
	}
}
