using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using WebHost.Components;
using WebHost.WebSystem;

using Monsterer.Request;

namespace MetaExchange.Pages
{
	public class MaintenancePage : SharedPage
	{
		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if MONO
			AddResource(new JsResource(Constants.kWebRoot, "/js/faqPageCompiled.js", true));
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
							BaseComponent.SPAN(stream, "Maintenance", HtmlAttributes.@class, "noTopMargin h1");
							P("Metaexchange is currently in maintenance mode, please check back soon.");
						}
					}
				}
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
			return "Pages/RequiredJs/Faq.rs";
		}
	}
}
