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

namespace MetaExchange.Pages
{
	public class MainPage : SharedPage
	{
		public override Task Render<T>(RequestContext ctx, StringWriter stream, T authObj)
		{
			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, HtmlAttributes.@class, "container"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "row",
												"ng-app", "BitShares"))
				{
					/*using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-2",
													"ng-controller", "GetMarkets"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "list-group smallFont"))
						{
							H5("Markets");

							Href(	stream, "<span class=\"pull-left\">{{m.m_base}}/{{m.m_quote}}</span><span class=\"pull-right\">{{m.m_lastPrice}}</span>", 
									HtmlAttributes.@class, "list-group-item clearfix",
									"ng-repeat", "m in results",
									HtmlAttributes.href, "/markets/{{m.m_base}}/{{m.m_quote}}");
						}
					}
					using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-10"))
					{
					
					}*/
					/*using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
					{
						using (new Panel(stream, "Please enter your Bitshares account name"))
						{
							using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																		HtmlAttributes.ajax, true,
																		HtmlAttributes.action, "/validateAccount"))
							{					
								using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
								{
									fm.Input(stream, HtmlAttributes.type, InputTypes.text,
														HtmlAttributes.name, "account_name",
														HtmlAttributes.@class, "form-control",
														HtmlAttributes.placeholder, "Bitshares account name");

									using (new Span(stream, HtmlAttributes.@class, "input-group-btn"))
									{
										Button("<span class='glyphicon glyphicon-info-sign'></span>", HtmlAttributes.@class, "btn btn-info");
									}
								}
							}
						}
					}*/
				}
				using (new DivContainer(stream, HtmlAttributes.@class, "row"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
					{
						using (new Panel(stream, "Buy bitBTC", "panel panel-warning"))
						{
							P("Once you enter your bitshares account name, we will generate your deposit address and send your bitBTC to you after 1 confirmation.");

							using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																		HtmlAttributes.ajax, true,
																		HtmlAttributes.action, "/validateAccount"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
								{
									fm.Label(stream, "Please enter your registered bitshares account name");
									fm.Input(stream, HtmlAttributes.type, InputTypes.text,
														HtmlAttributes.name, "account_name",
														HtmlAttributes.@class, "form-control",
														HtmlAttributes.placeholder, "Registered Bitshares account name");
								}
							}

							using (var fm = new FormContainer(stream))
							{
								fm.Label(stream, "Your bitcoin deposit address");
								fm.Input(stream, HtmlAttributes.type, InputTypes.text,
													HtmlAttributes.id, "bitcoinDespositId",
													HtmlAttributes.@class, "form-control",
													HtmlAttributes.@readonly, "readonly");
							}
						}
					}
					using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
					{
						using (new Panel(stream, "Buy bitcoin", "panel panel-success"))
						{
							P("Once you enter your bitcoin receiving address, we will generate your deposit address and send your bitcoins to you the instant we receive your bitBTC.");

							using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																		HtmlAttributes.ajax, true,
																		HtmlAttributes.action, "/validateAccount"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
								{
									fm.Label(stream, "Please enter your bitcoin receiving address");
									fm.Input(stream, HtmlAttributes.type, InputTypes.text,
														HtmlAttributes.name, "bitcoin_address",
														HtmlAttributes.@class, "form-control",
														HtmlAttributes.placeholder, "Bitcoin address from your wallet");
								}
							}

							using (var fm = new FormContainer(stream))
							{
								fm.Label(stream, "Your bitshares deposit address");
								fm.Input(stream, HtmlAttributes.type, InputTypes.text,
													HtmlAttributes.id, "bitsharesDespositId",
													HtmlAttributes.@class, "form-control",
													HtmlAttributes.@readonly, "readonly");
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
			return "pages/requiredjs/main.rs";
		}
	}
}
