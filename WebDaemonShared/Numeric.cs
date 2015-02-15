using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebDaemonShared
{
	public class Numeric
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="precision"></param>
		/// <returns></returns>
		static public decimal TruncateDecimal(decimal value, int precision)
		{
			decimal step = (decimal)Math.Pow(10, precision);
			value = Math.Floor(step * value);
			return value / step;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		static public string SerialisedDecimal(decimal d)
		{
			return d.ToString("0.##########");
		}

		/// <summary>	Random between. </summary>
		///
		/// <remarks>	Paul, 10/01/2015. </remarks>
		///
		/// <param name="r">  	The Random to process. </param>
		/// <param name="min">	. </param>
		/// <param name="max">	. </param>
		///
		/// <returns>	A decimal. </returns>
		static public decimal RandBetween(Random r, decimal min, decimal max)
		{
			return ((decimal)r.NextDouble() * (max - min) + min);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		static public decimal Clamp(decimal v, decimal min, decimal max)
		{
			return Math.Min(Math.Max(v, min), max);
		}

		/// <summary>	Format 2 dps. </summary>
		///
		/// <remarks>	Paul, 31/01/2015. </remarks>
		///
		/// <param name="price">	The price. </param>
		///
		/// <returns>	The formatted 2 dps. </returns>
		static public string Format2Dps(decimal price)
		{
			return String.Format("{0:0.00}", price);
		}
	}
}
