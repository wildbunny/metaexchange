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
	public class MainPage : SharedPage
	{
		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if MONO
			AddResource(new JsResource(Constants.kWebRoot, "/js/mainPageCompiled.js", true));
			#endif

			IEnumerable<string> markets = ctx.Request.GetWildcardParameters(2);

			string @base="", quote="";
			if (markets.Count() == 2)
			{
				@base = markets.First();
				quote = markets.Last();
			}

			string market = @base + "_" + quote;

			if (authObj.m_database.GetMarket(market) == null)
			{
				ctx.Respond(System.Net.HttpStatusCode.NotFound);
			}

			ImgResource logo = CreateLogo();
			AddResource(logo);

			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, "ng-app", "myApp", "ng-controller", "StatsController", HtmlAttributes.id, "rootId"))
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
																
								using (new DivContainer(stream, HtmlAttributes.id, "serviceStatusId"))
								{
									SPAN("Service status: ");
									SPAN("{{status}}", "", "label label-{{label}}");
								}
							}
						}
					}
				}

				//
				// buy and sell section
				//
				// 
				using (new DivContainer(stream, HtmlAttributes.@class, "container"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							Button("Buy {{market.base_symbol}}<br/><span class='badge'>1.00 {{market.quote_symbol}}</span><span class='glyphicon glyphicon-arrow-right arrow'></span><span class='badge'>{{market.buy_quantity | number:3}} {{market.base_symbol}}</span>", 
													HtmlAttributes.@class, "btn btn-success btn-lg btn-block",
													"data-toggle", "collapse",
													"aria-expanded", "false",
													"aria-controls", "buyId",
													"data-target", "#buyId");

							using (new Panel(stream, "Buy {{market.base_symbol}}", "panel panel-success collapse in", false, "buyId"))
							{
								P("Once you enter your BitShares account name, we will generate your deposit address and send your {{market.base_symbol}} to you after 1 confirmation.");

								using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																			HtmlAttributes.ajax, true,
																			HtmlAttributes.handler, "OnSubmitAddressBts",
																			HtmlAttributes.action, Routes.kSubmitAddress))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
									{
										fm.Label(stream, "Where shall we send your {{market.base_symbol}}?");

										fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kOrderType,
														HtmlAttributes.value, MetaOrderType.buy.ToString());

										fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kReferralId,
														HtmlAttributes.value, "0");

										fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kSymbolPair,
														HtmlAttributes.value, market);

										using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
										{
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.name, WebForms.kReceivingAddress,
																HtmlAttributes.minlength, 1,
																HtmlAttributes.maxlength, 63,
																HtmlAttributes.required, true,
																HtmlAttributes.id, "bitsharesBlurId",
																HtmlAttributes.@class, "form-control submitOnBlur",
																HtmlAttributes.placeholder, "BitShares account name");

											using (new Span(stream, HtmlAttributes.@class, "input-group-btn"))
											{
												Button("Submit", HtmlAttributes.@class, "btn btn-info");
											}
										}
									}

									Alert("", "alert alert-danger", "bitsharesErrorId", true);
								}

								using (var fm = new FormContainer(stream))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success unhideBtsId"))
									{
										fm.Label(stream, "Your bitcoin deposit address");
										fm.Input(stream, HtmlAttributes.type, InputTypes.text,
															HtmlAttributes.id, "bitcoinDespositId",
															HtmlAttributes.@class, "form-control",
															HtmlAttributes.@readonly, "readonly",
															HtmlAttributes.style, "cursor:text;");
									}

									Button("Click to generate QR code", HtmlAttributes.@class, "btn btn-warning btn-xs pull-right unhideBtsId",
																			HtmlAttributes.onclick, "GenerateQrModal()");
									SPAN("Maximum {{market.ask_max | number:8}} {{market.quote_symbol}} per transaction", "maxBtcId", "label label-info");
								}
							}
							
						}
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							Button("Sell {{market.base_symbol}}<br/><span class='badge'>{{market.sell_quantity | number:3}} {{market.base_symbol}}</span><span class='glyphicon glyphicon-arrow-right arrow'></span><span class='badge'>1.00 {{market.quote_symbol}}</span>", HtmlAttributes.@class, "btn btn-danger btn-lg btn-block",
													"data-toggle", "collapse",
													"aria-expanded", "false",
													"aria-controls", "sellId",
													"data-target", "#sellId");

							using (new Panel(stream, "Sell {{market.base_symbol}}", "panel panel-danger collapse in", false, "sellId"))
							{
								P("Once you enter your bitcoin receiving address, we will generate your deposit address and send your bitcoins to you the instant we receive your {{market.base_symbol}}.");

								using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																			HtmlAttributes.ajax, true,
																			HtmlAttributes.action, Routes.kSubmitAddress,
																			HtmlAttributes.handler, "OnSubmitAddressBtc"))
								{
									fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kOrderType,
														HtmlAttributes.value, MetaOrderType.sell.ToString());

									fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kReferralId,
														HtmlAttributes.value, "0");

									fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.id, "symbolPairId",
														HtmlAttributes.name, WebForms.kSymbolPair,
														HtmlAttributes.value, market);

									using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
									{
										fm.Label(stream, "Where shall we send your bitcoins?");
										using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
										{
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.name, WebForms.kReceivingAddress,
																HtmlAttributes.minlength, 25,
																HtmlAttributes.maxlength, 34,
																HtmlAttributes.required, true,
																HtmlAttributes.id, "bitcoinBlurId",
																HtmlAttributes.@class, "form-control submitOnBlur",
																HtmlAttributes.placeholder, "Bitcoin address from your wallet");

											using (new Span(stream, HtmlAttributes.@class, "input-group-btn"))
											{
												Button("Submit", HtmlAttributes.@class, "btn btn-info");
											}
										}
									}

									Alert("", "alert alert-danger", "bitcoinErrorId", true);
								}

								using (var fm = new FormContainer(stream))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "unhideBtcId"))
									{
										using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success"))
										{
											fm.Label(stream, "Your BitShares deposit account");
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.id, "bitsharesDespositAccountId",
																"ng-model", "sell.sendToAccount",
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.@readonly, "readonly",
																HtmlAttributes.style, "cursor:text;");
										}

										using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success"))
										{
											fm.Label(stream, "Your BitShares deposit memo");
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.id, "bitsharesMemoId",
																"ng-model", "sell.memo",
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.@readonly, "readonly",
																HtmlAttributes.style, "cursor:text;");
										}
									}

									Button("Click to generate transaction", HtmlAttributes.@class, "btn btn-warning btn-xs pull-right unhideBtcId",
																			HtmlAttributes.onclick, "GenerateTransactionModal()");
									SPAN("Maximum {{market.bid_max | number:8}} {{market.base_symbol}} per transaction", "maxbitBtcId", "label label-info pull-left");
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
							using (new DivContainer(stream, HtmlAttributes.@class, "row unhideBtsId unhideBtcId",
															HtmlAttributes.id, "myTransactionsId"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
								{
									H3("Your transactions");

									using (new Table(stream, "", 4, 4, "table noMargin", "Market", "Type", "Price", "Amount", "Fee", "Date", "Status", "Notes"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in myTransactions"))
										{
											tr.TD("{{renameSymbolPair(t.symbol_pair)}}");
											tr.TD("{{t.order_type}}");
											tr.TD("{{t.price}}");
											tr.TD("{{t.amount}}");
											tr.TD("{{t.fee}}");
											tr.TD("{{t.date*1000 | date:'MMM d, HH:mm'}}");
											tr.TD("{{t.status}}");
											tr.TD("{{t.notes}}");
										}
									}
								}
							}

							using (new DivContainer(stream, HtmlAttributes.@class, "unhideBtsId unhideBtcId"))
							{
								HR();
							}
							
							using (new DivContainer(stream, HtmlAttributes.@class, "row"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
								{
									H3("Recent {{renameSymbolPair(market.symbol_pair)}} transactions");

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

				Modal("Generate BitShares transaction", "bitsharesModalId", () =>
				{
					using (var fm = new FormContainer(stream))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
						{
							fm.Label(stream, "Sending amount");

							using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
							{
								fm.Input(stream, HtmlAttributes.type, InputTypes.number,
												HtmlAttributes.name, "amount",
												HtmlAttributes.@class, "form-control",
												HtmlAttributes.required, true,
												HtmlAttributes.id, "gtxAmountId",
												"ng-model", "sell.quantity",
												HtmlAttributes.placeholder, "bitAsset quantity");

								SPAN("{{market.base_symbol}}", "", "input-group-addon");
							}
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
						{
							fm.Label(stream, "Account name to send from");

							fm.Input(stream, HtmlAttributes.type, InputTypes.text,
											HtmlAttributes.name, "account",
											HtmlAttributes.@class, "form-control",
											HtmlAttributes.required, true,
											HtmlAttributes.id, "gtxAccountId",
											"ng-model", "sell.payFrom",
											HtmlAttributes.placeholder, "Sending acount");
						}

						Href(stream, "Click to open in BitShares",	
											HtmlAttributes.id, "bitsharesLinkId",
											HtmlAttributes.href, "bts:{{sell.sendToAccount}}/transfer/amount/{{sell.quantity}}/memo/{{sell.memo}}/from/{{sell.payFrom}}/asset/{{(market.base_symbol).substr(3)}}",
											HtmlAttributes.style, "display:none",
											HtmlAttributes.@class, "btn btn-success");
					}
				}, true, "", "modal", "close", false);

				Modal("Scan for bitcoin address", "qrModalId", () =>
				{
					using (new DivContainer(stream, "row text-center"))
					{
						BaseComponent.IMG(stream, "", HtmlAttributes.@class, "center-block");
					}
				}, true, "", "modal", "close", false);
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override string GetPageSpecificJsFilename()
		{
			return "Pages/RequiredJs/Main.rs";
		}
	}
}
