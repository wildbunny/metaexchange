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
		public override Task Render<T>(RequestContext ctx, StringWriter stream, T authObj)
		{
			#if !MONO
			foreach (string name in m_sharedJsFilenames)
			{
				AddResource(new JsResource(Constants.kWebRoot, name, true));
			}
			#endif

			AddResource(new CssResource(Constants.kWebRoot, "/css/site.css", true));
			AddResource(new CssResource(Constants.kWebRoot, "/css/bootstrap.min.css", true));

			// render head
			base.Render(ctx, stream, authObj);

			// begin body
			m_stream.WriteLine("<body>");

			using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar navbar-default navbar-fixed-top"))
			{
				using (new DivContainer(m_stream, HtmlAttributes.@class, "container-fluid"))
				{
					using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar-header"))
					{
						Href(m_stream, "Brand", HtmlAttributes.href, "/", HtmlAttributes.@class, "navbar-brand");
					}
					using (new DivContainer(m_stream, HtmlAttributes.@class, "navbar-collapse collapse"))
					{
						string page = ctx.Request.Url.LocalPath.Split('/').Last();

						using (var ul = new UL(stream, "nav navbar-nav pull-left"))
						{
							WriteLiHref(stream, "Home", GetLiClass(page, ""), "", HtmlAttributes.href, "/");
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			// write footer as well
			using (var mp = new DivContainer(m_stream, "", "container-fluid"))
			{
				HR();
				using (var f = new Footer(m_stream))
				{
					f.Out("Copyright 2014 Wildbunny Ltd | ");
					Href(m_stream, "Terms of use", HtmlAttributes.href, "/terms");
				}
			}

			// end body
			m_stream.WriteLine("</body>");

			base.Dispose();
		}
	}
}
