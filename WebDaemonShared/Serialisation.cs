using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceStack.Text;

namespace WebDaemonShared
{
	public class Serialisation
	{
		static public void Defaults()
		{
			JsConfig<DateTime>.RawSerializeFn = d => DateTimeExtensions.ToUnixTime(d).ToString();
			JsConfig.IncludeTypeInfo = false;
			JsConfig.IncludePublicFields = true;
			JsConfig.IncludeNullValues = true;
			JsConfig<decimal>.SerializeFn = d => Numeric.SerialisedDecimal(d);
		}
	}
}
