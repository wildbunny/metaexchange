using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


using MetaData;

namespace WebDaemonShared
{
	/*public enum CurrencyTypes
	{
		none,
		BTC,
		BTS,
		bitBTC,
		bitUSD,
		bitCNY,
		bitGOLD
	}

	public class CurrencyHelpers
	{
		const string kBitassetPrefix = "bit";

		/// <summary>	Query if 'type' is bitshares asset. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	true if bitshares asset, false if not. </returns>
		static public bool IsBitsharesAsset(CurrencyTypes type)
		{
			return type > CurrencyTypes.BTC;
		}

		/// <summary>	Converts a type to the bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	type as a string. </returns>
		static public string ToBitsharesSymbol(CurrencyTypes type)
		{
			return type.ToString().TrimStart(kBitassetPrefix);
		}

		/// <summary>	Initializes this object from the given from bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbol">	The symbol. </param>
		///
		/// <returns>	The CurrencyTypes. </returns>
		static public CurrencyTypes FromBitsharesSymbol(string symbol)
		{
			if (symbol == CurrencyTypes.BTS.ToString())
			{
				// special case for BTS
				return CurrencyTypes.BTS;
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
		/// <returns>	The CurrencyTypes. </returns>
		static public CurrencyTypes FromSymbol(string symbol)
		{
			return (CurrencyTypes)Enum.Parse(typeof(CurrencyTypes), symbol);
		}

		/// <summary>	Gets market symbol pair. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="base"> 	The base. </param>
		/// <param name="quote">	The quote. </param>
		///
		/// <returns>	The market symbol pair. </returns>
		static public string GetMarketSymbolPair(CurrencyTypes @base, CurrencyTypes quote)
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
		static public void GetBaseAndQuoteFromSymbolPair(string symbolPair, out CurrencyTypes @base, out CurrencyTypes quote)
		{
			@base = CurrencyHelpers.FromSymbol(symbolPair.Split('_')[0]);
			quote = CurrencyHelpers.FromSymbol(symbolPair.Split('_')[1]);
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
	}*/

	public class CurrencyHelpers
	{
		const string kBitassetPrefix = "bit";
		public const string kBtsSymbol = "BTS";
		public const string kBtcSymbol = "BTC";

		/// <summary>	Query if 'type' is bitshares asset. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	true if bitshares asset, false if not. </returns>
		static public bool IsBitsharesAsset(CurrenciesRow type)
		{
			return type.bitshares;
		}

		/// <summary>	Converts a type to the bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="type">	The type. </param>
		///
		/// <returns>	type as a string. </returns>
		static public string ToBitsharesSymbol(CurrenciesRow type)
		{
			return type.symbol.TrimStart(kBitassetPrefix);
		}

		/// <summary>	Initializes this object from the given from bitshares symbol. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbol">	The symbol. </param>
		///
		/// <returns>	The CurrencyTypes. </returns>
		static public CurrenciesRow FromBitsharesSymbol(string symbol, Dictionary<string, CurrenciesRow> currencyMap, bool isUia)
		{
			if (symbol == kBtsSymbol)
			{
				// special case for BTS
				return currencyMap[kBtsSymbol];
			}
			else
			{
				string prefix = isUia ? "" : kBitassetPrefix;
				return FromSymbol(prefix + symbol, currencyMap);
			}
		}

		/// <summary>	Initializes this object from the given from symbol. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <param name="symbol">	The symbol. </param>
		///
		/// <returns>	The CurrencyTypes. </returns>
		static public CurrenciesRow FromSymbol(string symbol, Dictionary<string, CurrenciesRow> currencyMap)
		{
			return currencyMap[symbol];
		}

		/// <summary>	Gets market symbol pair. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="base"> 	The base. </param>
		/// <param name="quote">	The quote. </param>
		///
		/// <returns>	The market symbol pair. </returns>
		static public string GetMarketSymbolPair(CurrenciesRow @base, CurrenciesRow quote)
		{
			return @base.symbol + "_" + quote.symbol;
		}

		/// <summary>	Gets base and quote from symbol pair. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="symbolPair">	The symbol pair. </param>
		/// <param name="base">		 	[out] The base. </param>
		/// <param name="quote">	 	[out] The quote. </param>
		static public void GetBaseAndQuoteFromSymbolPair(string symbolPair, Dictionary<string, CurrenciesRow> currencyMap, out CurrenciesRow @base, out CurrenciesRow quote)
		{
			@base = CurrencyHelpers.FromSymbol(symbolPair.Split('_')[0], currencyMap);
			quote = CurrencyHelpers.FromSymbol(symbolPair.Split('_')[1], currencyMap);
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

		/// <summary>	Gets a currency. </summary>
		///
		/// <remarks>	Paul, 10/03/2015. </remarks>
		///
		/// <exception cref="ApiExceptionUnknownCurrency">	Thrown when an API exception unknown currency
		/// 												error condition occurs. </exception>
		///
		/// <param name="symbol">	  	The symbol. </param>
		/// <param name="currencyMap">	The currency map. </param>
		///
		/// <returns>	The currency. </returns>
		static CurrenciesRow GetCurrency(string symbol, Dictionary<string, CurrenciesRow> currencyMap)
		{
			if (currencyMap.ContainsKey(symbol))
			{
				return currencyMap[symbol];
			}
			else
			{
				throw new ApiExceptionUnknownCurrency(symbol);
			}
		}
	}
}
