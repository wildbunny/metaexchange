using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monsterer.Request;
using RestLib;
using WebDaemonShared;
using ApiHost;
using BitsharesRpc;
using Casascius.Bitcoin;
using MetaDaemon.Markets;
using WebDaemonSharedTables;
using MetaData;

namespace MetaDaemon
{
	

	

	public partial class MetaDaemonApi
	{

		/// <summary>	Executes the submit address action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionUnknownMarket">	Thrown when an API exception unknown market
		/// 												error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnSubmitAddress(RequestContext ctx, IDummy dummy)
		{
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);
			string receivingAddress = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kReceivingAddress);
			MetaOrderType orderType = RestHelpers.GetPostArg<MetaOrderType, ApiExceptionMissingParameter>(ctx, WebForms.kOrderType);

			if (!m_marketHandlers.ContainsKey(symbolPair))
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}

			// get the handler for this market
			MarketBase market = m_marketHandlers[symbolPair];

			// get the response and send it
			SubmitAddressResponse response = market.OnSubmitAddress(receivingAddress, orderType);

			ctx.Respond<SubmitAddressResponse>(response);

			return null;
		}

		/// <summary>	Executes the get market action. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionUnknownMarket">	Thrown when an API exception unknown market
		/// 												error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetMarket(RequestContext ctx, IDummy dummy)
		{
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);

			MarketRow market = GetMarket(symbolPair);
			if (market == null)
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}
			else
			{
				ctx.Respond<MarketRow>(market);
			}

			return null;
		}

		/// <summary>	Executes the get all markets action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetAllMarkets(RequestContext ctx, IDummy dummy)
		{
			ctx.Respond<List<MarketRow>>(GetAllMarkets());
			return null;
		}

		/// <summary>	Executes the get order status action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionOrderNotFound">	Thrown when an API exception order not found
		/// 												error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetOrderStatus(RequestContext ctx, IDummy dummy)
		{
			string txid = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kTxId);

			TransactionsRow t = GetTransaction(txid);
			if (t==null)
			{
				throw new ApiExceptionOrderNotFound(txid);
			}

			ctx.Respond<TransactionsRow>(t);
			return null;
		}

		/// <summary>	Executes the get transactions action. </summary>
		///
		/// <remarks>	Paul, 06/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetLastTransactions(RequestContext ctx, IDummy dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string market = RestHelpers.GetPostArg<string>(ctx, WebForms.kSymbolPair);

			ctx.Respond<List<TransactionsRow>>(GetLastTransactions(limit, market));
			return null;
		}

		/// <summary>	Executes the get my last transactions action. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetMyLastTransactions(RequestContext ctx, IDummy dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string memo = RestHelpers.GetPostArg<string>(ctx, WebForms.kMemo);
			string depositAddress = RestHelpers.GetPostArg<string>(ctx, WebForms.kDepositAddress);

			ctx.Respond<List<TransactionsRow>>(GetLastTransactionsFromDeposit(memo, depositAddress, limit));
			return null;
		}
	}
}
