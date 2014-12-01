using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsharesRpc
{
	static public class BitsharesDatetimeExtensions
	{
		public const string kBitsharesTimestampFormat = "yyyyMMddTHHmmff";

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		static public string ToBitsharesTimeStamp(this DateTime time)
		{
			return time.ToString(kBitsharesTimestampFormat);
		}

		/// <summary>
		/// "20110722T161114"
		/// </summary>
		/// <param name="dateString"></param>
		/// <returns></returns>
		static public DateTime ParseDateTime(string dateString)
		{
			string year, month, day, hour, min, sec;
			if (dateString.Length == 15)
			{
				year = dateString.Substring(0, 4);
				month = dateString.Substring(4, 2);
				day = dateString.Substring(6, 2);
				hour = dateString.Substring(9, 2);
				min = dateString.Substring(11, 2);
				sec = dateString.Substring(13, 2);

				dateString = year + "/" + month + "/" + day + "T" + hour + ":" + min + ":" + sec;
			}

			return DateTime.Parse(dateString);
		}
	}
}
