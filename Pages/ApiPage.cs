using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

using WebHost.Components;
using WebHost.WebSystem;
using Monsterer.Request;
using WebDaemonShared;
using WebDaemonSharedTables;
using RestLib;
using ApiHost;
using MetaData;
using ServiceStack.Text;


namespace MetaExchange.Pages
{
	enum DocType
	{
		integer,
		@long,
		@string,
		@decimal,
		boolean
	}
	class DocParam
	{
		internal string name;
		internal DocType type;
		internal string description;
	}
	
	public class ApiPage : SharedPage
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="url"></param>
		/// <param name="link"></param>
		string DocRefLink(string url)
		{
			var w = new StringWriter();
			string link = url.Trim('/');
			BaseComponent.Href(w, url, HtmlAttributes.href, "#" + link);
			return w.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		string HrefToString(string url)
		{
			var w = new StringWriter();
			BaseComponent.Href(w, url, HtmlAttributes.href, url);
			return w.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="list"></param>
		void ListParams(StringWriter stream, List<DocParam> list)
		{
			using (new UL(stream, ""))
			{
				foreach (DocParam p in list)
				{
					using (new LI(stream))
					{
						stream.WriteLine("<code>" + p.name + "</code> (" + p.type + "). " + p.description);
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="route"></param>
		/// <param name="link"></param>
		/// <param name="description"></param>
		/// <param name="requiredParams"></param>
		/// <param name="optionalParams"></param>
		void CodeExample<T>(string website, StringWriter stream, string route, string method, string description, List<DocParam> requiredParams, List<DocParam> optionalParams, string example, T results)
		{
			using (new CodeExample<string>(stream, route, route.Trim('/')))
			{
				P(description);

				P("HTTP Method: <b>" + method + "</b>");

				if (requiredParams != null)
				{
					P("Required parameters:");

					ListParams(stream, requiredParams);
				}

				if (optionalParams != null)
				{
					P("Optional parameters:");

					ListParams(stream, optionalParams);
				}
			}

			Uri uri = new Uri(website + example);
			string url = uri.LocalPath;
			string path = website + uri.LocalPath;

			string fullExample = "";
			if (method == WebRequestMethods.Http.Post)
			{
				fullExample += "POST " + path + " HTTP/1.1" + "<br/>";
				fullExample += "Connection: close" + "<br/>";
				fullExample += "Accept: " + Rest.kContentTypeJson + "<br/>";
				fullExample += "Content-Type: " + Rest.kContentTypeForm + "<br/>";
				fullExample += uri.Query.TrimStart('?');
			}
			else
			{
				fullExample = website + example;
			}

			fullExample += "<br/><br/>"; 

			stream.WriteLine("<pre class=\"prettyprint\">Example:<br/><br/>" + fullExample + "Results:<br/><br/>" + JsonSerializer.SerializeToString<T>(results) + "</pre>");

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="enumType"></param>
		/// <returns></returns>
		string EnumToPrettyTable(string title, Type enumType, bool ordered = true)
		{
			string[] allErrors = Enum.GetNames(enumType);
			string errorEnum = "";
			foreach (string e in allErrors)
			{
				errorEnum += "<li>" + e + "</li>";
			}

			if (ordered)
			{
				return "<p>" + title + ":<br/><pre class=\"prettyprint linenums\"><ol class=\"linenums\">" + errorEnum + "</ol></pre></p>";
			}
			else
			{
				return "<p>" + title + ":<br/><pre class=\"prettyprint\"><ul>" + errorEnum + "</ul></pre></p>";
			}
		}

		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if MONO
			AddResource(new JsResource(Constants.kWebRoot, "/js/apiPageCompiled.js", true));
			#endif
			
			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, HtmlAttributes.@class, "jumbotron clearfix"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "container"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-xs-12"))
						{
							BaseComponent.SPAN(stream, "API", HtmlAttributes.@class, "noTopMargin h1");
						}
					}
				}
			}

			using (new DivContainer(stream, HtmlAttributes.@class, "container"))
			{
				P("The API allows developers to access the functionality of Metaexchange without needing to use the website.");
				P("All API requests return data in JSON format.");

				HR();
				H4("API errors");

				P("When an API call fails, an error will be returned. The structure of an error is:");

				using (new Pre(stream, HtmlAttributes.@class, "prettyprint"))
				{
					stream.WriteLine("{<br/>" +
									"\t\"error\":&lt;error code&gt;,<br/>" +
									"\t\"message\":&lt;error message&gt,<br/>" +
									"}");
				}

				stream.WriteLine(EnumToPrettyTable("Error codes", typeof(ApiErrorCode)));

				HR();
				H4("API summary");

				string[] apiEndpointsAndDescriptions =
				{
					Routes.kSubmitAddress, "Submit your receiving address and get back deposit details",
					Routes.kGetOrderStatus, "Get the status of your order by TXID",
					Routes.kGetMyLastTransactions, "Get a list of your last transactions",
					Routes.kGetAllMarkets, "Get a list of all supported markets",
					Routes.kGetMarket, "Get details of a particular market",
					Routes.kGetLastTransactions, "Get a list of the last transactions"
				};

				for (int i = 0; i < apiEndpointsAndDescriptions.Length; i+=2 )
				{
					stream.WriteLine("<b>" + apiEndpointsAndDescriptions[i + 1] + "</b><br/>");
					stream.WriteLine( DocRefLink(apiEndpointsAndDescriptions[i + 0]) + "<br/><br/>");
				}

				HR();
				H4("API detail");

				string siteUrl = ctx.Request.Url.AbsoluteUri.TrimEnd(ctx.Request.Url.LocalPath);
				string market = CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitBTC, CurrencyTypes.BTC);

				CodeExample<SubmitAddressResponse>(siteUrl, stream, Routes.kSubmitAddress, WebRequestMethods.Http.Post, "The main function. This will take your supplied receiving address and provide you with a deposit address and a memo (depending on the market).",
							new List<DocParam>
							{
								new DocParam { description = "This is the symbol pair of the market you would like to trade in. Symbol pairs are of the form <b>" + market + "</b> and are case sensitive. Find out what markets are available by calling " + DocRefLink(Routes.kGetAllMarkets),
												name = WebForms.kSymbolPair,
												type = DocType.@string},
								new DocParam { description = "This is where you would like funds to be forwarded. It could be a bitcoin address or a bitshares account depending on the order type and market.",
												name = WebForms.kReceivingAddress,
												type = DocType.@string},
								new DocParam { description = "The type of order you want to place. Possible types are <b>" + MetaOrderType.buy + "</b> and <b>" + MetaOrderType.sell + "</b>.",
												name = WebForms.kOrderType,
												type = DocType.@string},
							},
							null, Routes.kSubmitAddress + RestHelpers.BuildArgs(WebForms.kSymbolPair, CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitBTC, CurrencyTypes.BTC),
																				WebForms.kReceivingAddress, "monsterer",
																				WebForms.kOrderType, MetaOrderType.buy), new SubmitAddressResponse { deposit_address = "mrveCRH4nRZDpS7fxgAiLTX7GKvJ1cARY9" });

				CodeExample<TransactionsRowNoUid>(siteUrl, stream, Routes.kGetOrderStatus, WebRequestMethods.Http.Post, "Get the status of a particular order when you know the blockchain transaction id. Transaction must be in a block before this function will return a result.",
							new List<DocParam>
							{
								new DocParam { description = "The transaction id of the transaction you sent.",
												name = WebForms.kTxId,
												type = DocType.@string},
								
							},
							null, Routes.kGetOrderStatus + RestHelpers.BuildArgs(WebForms.kTxId, "ed7364fd1b8ba4cc3428470072300fb88097c3a343c75b6e604c68799a0148cb"),
							new TransactionsRowNoUid
							{
								amount = 0.1M,
								date = DateTime.UtcNow,
								deposit_address = "1-n2HPFvf376vxQV1mo",
								notes = null,
								order_type = MetaOrderType.sell,
								sent_txid = "231500b3ecba3bf2ba3db650f5e7565478382c5b",
								received_txid = "70827daa9f08211491297a5ded2c3f8d7a2b654f1e6f3d4e2ff3ad7e14966a85",
								status = MetaOrderStatus.completed,
								symbol_pair = market
							});

				stream.WriteLine(EnumToPrettyTable("Possible order statuses", typeof(MetaOrderStatus)));

				CodeExample<MarketRow>(siteUrl, stream, Routes.kGetMarket, WebRequestMethods.Http.Post, "Get details about a particular market, such as bid/ask price and transaction limits.",
							new List<DocParam>
							{
								new DocParam { description = "The symbol pair identifier for the market. Symbol pairs are of the form <b>" + market + "</b>. Use " + DocRefLink(Routes.kGetAllMarkets) +" to query the list of available markets.",
												name = WebForms.kSymbolPair,
												type = DocType.@string},
								
							},
							null, Routes.kGetMarket + RestHelpers.BuildArgs(WebForms.kSymbolPair, market),
							new MarketRow
							{
								ask= Numeric.TruncateDecimal(1/0.994M, 3),
								bid=Numeric.TruncateDecimal(0.994M, 3),
								ask_max=1,
								bid_max=0.8M,
								symbol_pair=market,
							});


				CodeExample<List<MarketRow>>(siteUrl, stream, Routes.kGetAllMarkets, WebRequestMethods.Http.Get, "Get a list of all available markets along with the best prices and transaction limits.",
							null,
							null, Routes.kGetAllMarkets,
							new List<MarketRow>{new MarketRow
							{
								ask = Numeric.TruncateDecimal(1 / 0.994M, 3),
								bid = Numeric.TruncateDecimal(0.994M, 3),
								ask_max = 1,
								bid_max = 0.8M,
								symbol_pair = market,
							}});

				CodeExample<List<TransactionsRowNoUid>>(siteUrl, stream, Routes.kGetLastTransactions, WebRequestMethods.Http.Post, "Get the most recent transactions, sorted in descending order. This will only show transactions with status " + MetaOrderStatus.completed+".",
							new List<DocParam>
							{
								new DocParam { description = "The maximum number of results to return.",
												name = WebForms.kLimit,
												type = DocType.integer},
								
							},
							new List<DocParam>
							{
								new DocParam { description = "Market to query.",
												name = WebForms.kSymbolPair,
												type = DocType.@string},
								
							}, Routes.kGetLastTransactions + RestHelpers.BuildArgs(WebForms.kSymbolPair, market),
							new List<TransactionsRowNoUid>{
								new TransactionsRowNoUid
								{
									amount = 0.1M,
									date = DateTime.UtcNow,
									deposit_address = "1-n2HPFvf376vxQV1mo",
									notes = null,
									order_type = MetaOrderType.sell,
									sent_txid = "231500b3ecba3bf2ba3db650f5e7565478382c5b",
									received_txid = "70827daa9f08211491297a5ded2c3f8d7a2b654f1e6f3d4e2ff3ad7e14966a85",
									status = MetaOrderStatus.completed,
									symbol_pair = market
								},
								new TransactionsRowNoUid
								{
									amount = 0.00010000M,
									date = DateTime.UtcNow,
									deposit_address = "mpwPhGCtbe8AeoFq3FWq6ToKbeapL7zM8b",
									notes = null,
									order_type = MetaOrderType.buy,
									sent_txid = "4217b7b0dcb940e5732c977473c8d893f52370c4",
									received_txid = "70bc0017c21e29738a93b9f4ce21d36814898bf7bcdfc6feba5227e9ab3495d5",
									status = MetaOrderStatus.completed,
									symbol_pair = market
								},
							});

				CodeExample<List<TransactionsRowNoUid>>(siteUrl, stream, Routes.kGetMyLastTransactions, WebRequestMethods.Http.Post, "Get your most recent transactions, sorted in descending order. Use this when you know the deposit address to which you sent funds, or the transaction memo. This shows transactions with any status.",
							new List<DocParam>
							{
								new DocParam { description = "The maximum number of results to return.",
												name = WebForms.kLimit,
												type = DocType.integer},
								
							},
							new List<DocParam>
							{
								new DocParam { description = "Bitcoin deposit address.",
												name = WebForms.kDepositAddress,
												type = DocType.@string},
								new DocParam { description = "Bitshares transaction memo.",
												name = WebForms.kMemo,
												type = DocType.@string},
								
							}, Routes.kGetMyLastTransactions + RestHelpers.BuildArgs(WebForms.kMemo, "1-mqjz4GnADMucWuR4v"),
							new List<TransactionsRowNoUid>{
								new TransactionsRowNoUid
								{
									amount = 0.700000M,
									date = DateTime.UtcNow,
									deposit_address = "1-n2HPFvf376vxQV1mo",
									notes = "Over 0.2 BTC!",
									order_type = MetaOrderType.sell,
									sent_txid = "f94f79e29110107c917ba41fa02fcfc2ccb5e4cc",
									received_txid = "ab265b51259a651c68b4a82b8bce5c501325d323",
									status = MetaOrderStatus.refunded,
									symbol_pair = market
								},
								new TransactionsRowNoUid
								{
									amount = 0.0700000M,
									date = DateTime.UtcNow,
									deposit_address = "1-n2HPFvf376vxQV1mo",
									order_type = MetaOrderType.sell,
									sent_txid = "ed7364fd1b8ba4cc3428470072300fb88097c3a343c75b6e604c68799a0148cb",
									received_txid = "18159b00b90374cb467a7744a031d84663e92136",
									status = MetaOrderStatus.completed,
									symbol_pair = market
								},
							});
			}

			return null;
		}

		/// <summary>	Gets page specific js filename. </summary>
		///
		/// <remarks>	Paul, 03/02/2015. </remarks>
		///
		/// <returns>	The page specific js filename. </returns>
		protected override string GetPageSpecificJsFilename()
		{
			return "Pages/RequiredJs/Api.rs";
		}
	}
}
