using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using WebHost;
using WebHost.Components;

namespace MetaExchange.Pages
{
	public class BootstrapBase : BasePage<IDummy>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="text"></param>
		/// <param name="liClass"></param>
		/// <param name="liID"></param>
		/// <param name="hrefParams"></param>
		protected void WriteLiHref(StringWriter stream, string text, string liClass, string liID, params object[] hrefParams)
		{
			using (var li = new LI(stream, HtmlAttributes.@class, liClass, HtmlAttributes.id, liID))
			{
				Href(stream, text, hrefParams);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="compare"></param>
		/// <returns></returns>
		protected string GetLiClass(string page, string compare)
		{
			return page == compare ? "active" : "";
		}
	}
}
