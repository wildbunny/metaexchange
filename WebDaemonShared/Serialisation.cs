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
			JsConfig<DateTime>.RawDeserializeFn = d =>
			{
				uint unix;
				if (uint.TryParse(d, out unix))
				{
					return DateTimeExtensions.FromUnixTime(unix);
				}
				else
				{
					return DateTime.Parse(d);
				}
			};
			JsConfig.IncludeTypeInfo = false;
			JsConfig.IncludePublicFields = true;
			JsConfig.IncludeNullValues = true;
			JsConfig<decimal>.SerializeFn = d => Numeric.SerialisedDecimal(d);
		}
	}
}
