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
	public class FaqPage : SharedPage
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
							BaseComponent.SPAN(stream, "FAQ", HtmlAttributes.@class, "noTopMargin h1");
						}
					}
				}
			}

			using (new DivContainer(stream, HtmlAttributes.@class, "container",
												HtmlAttributes.style, "margin-top:20px"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "row"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "col-xs-12"))
					{
						P("Q) What happens if I send over the transaction limit?");
						P("A) Your full transaction will be refunded");
						BR();
						P("Q) Where is the transaction limit displayed?");
						P("A) In the buy/sell box there is a small tag displaying the transaction limit at the bottom");
						BR();
						P("Q) Can I send multiple transactions with the same memo?");
						P("A) Yes, you can re-use the same memo in multiple different transactions");
						BR();
						P("Q) What if I forget to include the memo?");
						P("A) Your transaction will be automatically refunded");
						BR();
						P("Q) What is bitshares?");
						P("A) You can find out more here: <a href='https://bitshares.org'>bitshares.org</a>");
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
