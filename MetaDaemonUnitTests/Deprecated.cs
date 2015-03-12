using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebDaemonShared;

namespace BtsOnrampDaemon
{
	public enum CurrencyTypesDep
	{
		none,
		BTC,
		BTS,
		bitBTC,
		bitUSD,
		bitCNY,
		bitGOLD,
		max
	}

	public class CurrencyHelpersDep
	{
		const string kBitassetPrefix = "bit";

		/// <summary>	Query if 'type' is bitshares asset. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	true if bitshares asset, false if not. </returns>
		static public bool IsBitsharesAsset(CurrencyTypesDep type)
		{
			return type > CurrencyTypesDep.BTC;
		}

		/// <summary>	Converts a type to the bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	type as a string. </returns>
		static public string ToBitsharesSymbol(CurrencyTypesDep type)
		{
			return type.ToString().TrimStart(kBitassetPrefix);
		}

		/// <summary>	Initializes this object from the given from bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbol">	The symbol. </param>
		///
		/// <returns>	The CurrencyTypesDep. </returns>
		static public CurrencyTypesDep FromBitsharesSymbol(string symbol)
		{
			if (symbol == CurrencyTypesDep.BTS.ToString())
			{
				// special case for BTS
				return CurrencyTypesDep.BTS;
			}
			else
			{
				return FromSymbol(kBitassetPrefix + symbol);
			}
		}

		/// <summary>	Initializes this object from the given from symbol. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <param name="symbol">	The symbol. </param>
		///
		/// <returns>	The CurrencyTypesDep. </returns>
		static public CurrencyTypesDep FromSymbol(string symbol)
		{
			return (CurrencyTypesDep)Enum.Parse(typeof(CurrencyTypesDep), symbol);
		}

		/// <summary>	Gets market symbol pair. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="base"> 	The base. </param>
		/// <param name="quote">	The quote. </param>
		///
		/// <returns>	The market symbol pair. </returns>
		static public string GetMarketSymbolPair(CurrencyTypesDep @base, CurrencyTypesDep quote)
		{
			return @base + "_" + quote;
		}

		/// <summary>	Gets base and quote from symbol pair. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="base">		 	[out] The base. </param>
		/// <param name="quote">	 	[out] The quote. </param>
		static public void GetBaseAndQuoteFromSymbolPair(string symbolPair, out CurrencyTypesDep @base, out CurrencyTypesDep quote)
		{
			@base = CurrencyHelpersDep.FromSymbol(symbolPair.Split('_')[0]);
			quote = CurrencyHelpersDep.FromSymbol(symbolPair.Split('_')[1]);
		}

		/// <summary>	Rename symbol pair. </summary>
		///
		/// <remarks>	Paul, 01/03/2015. </remarks>
		///
		/// <param name="name">	The name. </param>
		///
		/// <returns>	A string. </returns>
		static public string RenameSymbolPair(string name)
		{
			return name.Replace('_', '/');
		}
	}
}
