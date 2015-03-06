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
using MetaData;
using WebDaemonShared;

namespace MetaExchange.Pages
{
	public class SharedPage : BootstrapBase
	{
		#if !MONO
		protected List<string> m_sharedJsFilenames;
		protected List<string> m_pageSpecificJsFilenames;
		#endif

		public SharedPage()
		{
			#if !MONO
			m_sharedJsFilenames = new List<string>();
			m_pageSpecificJsFilenames = new List<string>();

			// read the shared javascript files and stick them in a list
			m_sharedJsFilenames.AddRange( GetFilenames(GetPageSpecificJsFilename()) );
			m_sharedJsFilenames.AddRange( GetFilenames(Constants.kSharedJsListName) );
			
			#endif
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected virtual string GetPageSpecificJsFilename()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="jsListFile"></param>
		/// <returns></returns>
		protected string[] GetFilenames(string jsListFile)
		{
			return File.OpenText(jsListFile).ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ctx"></param>
		/// <param name="stream"></param>
		/// <param name="authObj"></param>
		/// <returns></returns>
		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if !MONO
			foreach (string name in m_sharedJsFilenames)
			{
				AddResource(new JsResource(Constants.kWebRoot, name, true));
			}
			#endif

			AddResource(new CssResource(Constants.kWebRoot, "/css/site.css", true));
			AddResource(new CssResource(Constants.kWebRoot, "/css/bootstrap.min.css", true));
			AddResource(new FavIconResource(Constants.kWebRoot, "/images/favicon.ico"));
			AddResource(new TitleResource("Metaexchange"));
			AddResource(new MetaResource("viewport", "width=device-width, initial-scale=1"));

			ImgResource brand = new ImgResource(Constants.kWebRoot, "/images/brandTitle.png", "", false);
			AddResource(brand);

			// render head
			base.Render(ctx, stream, authObj);

			// begin body
			m_stream.WriteLine("<body>");

			using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar navbar-default navbar-fixed-top"))
			{
				using (new DivContainer(m_stream, HtmlAttributes.@class, "container"))
				{
					using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar-header"))
					{
						using (new HRef(m_stream, HtmlAttributes.@class, "navbar-brand", HtmlAttributes.href, "#"))
						{
							brand.Write(m_stream);
							m_stream.Write(" metaexchange");
						}
					}
					using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar-collapse collapse"))
					{
						string page = ctx.Request.Url.LocalPath.Split('/').Last();

						IEnumerable<MarketRow> allMarkets = authObj.m_database.GetAllMarkets().Where(m=>m.visible);

						using (var ul = new UL(stream, "nav navbar-nav pull-left"))
						{
							using (var li = new LI(stream, "dropdown"))
							{
								Href(stream, "Markets <span class=\"caret\">", HtmlAttributes.href, "/",
																				HtmlAttributes.@class, "disabled",
																				"data-toggle", "dropdown",
																				"role", "button",
																				"aria-expanded", "false");

								using (new UL(stream,	HtmlAttributes.@class, "dropdown-menu", 
														"role","menu"))
								{
									foreach (MarketRow m in allMarkets)
									{
										WriteLiHref(stream, CurrencyHelpers.RenameSymbolPair(m.symbol_pair), "", "", HtmlAttributes.href, "/markets/" + CurrencyHelpers.RenameSymbolPair(m.symbol_pair));
									}
								}
							}

							//WriteLiHref(stream, "Home", GetLiClass(page, ""), "", HtmlAttributes.href, "/");
							
							WriteLiHref(stream, "Api", GetLiClass(page, "apiDocs"), "", HtmlAttributes.href, "/apiDocs");
							WriteLiHref(stream, "Faq", GetLiClass(page, "faq"), "", HtmlAttributes.href, "/faq");
						}
					}
				}
			}

			return null;
		}

		/// <summary>	Renders the jumbo. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <param name="stream">	. </param>
		/// <param name="logo">  	The logo. </param>
		protected void RenderJumbo(StringWriter stream, ImgResource logo)
		{
			using (new DivContainer(stream, HtmlAttributes.@class, "clearfix"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "pull-left"))
				{
					logo.Write(stream);
				}
				using (new DivContainer(stream, HtmlAttributes.@class, "pull-left"))
				{
					BaseComponent.SPAN(stream, "metaexchange<sup>beta</sup>", HtmlAttributes.@class, "noTopMargin h1");
					P("The place to buy and sell bitAssets");
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			// write footer as well
			using (var mp = new DivContainer(m_stream, "", "container"))
			{
				HR();
				using (var f = new Footer(m_stream))
				{
					f.Out("Copyright 2015 Wildbunny Ltd | ");
					Href(m_stream, "Support", HtmlAttributes.href, "https://bitsharestalk.org/index.php?topic=12317.0");
					f.Out(" | ");
					Href(m_stream, "Github", HtmlAttributes.href, "https://github.com/wildbunny/metaexchange");
					f.Out(" | Please vote for ");
					Href(m_stream, "dev-metaexchange.monsterer", HtmlAttributes.href, "bts:dev-metaexchange.monsterer/approve");
				}
			}

			// end body
			m_stream.WriteLine("</body>");

			base.Dispose();
		}
	}
}
