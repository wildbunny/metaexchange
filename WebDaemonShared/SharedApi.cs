using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Monsterer.Responses;
using Monsterer.Request;
using MetaData;
using ApiHost;
using WebDaemonSharedTables;

namespace WebDaemonShared
{
	public class SharedApi<T>
	{
		MySqlData m_database;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 16/03/2015. </remarks>
		///
		/// <param name="database">	The database. </param>
		public SharedApi(MySqlData database)
		{
			m_database = database;
		}

		/// <summary>	Sends the cors response. </summary>
		///
		/// <remarks>	Paul, 16/03/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="ctx"> 	The context. </param>
		/// <param name="data">	The data. </param>
		///
		/// <returns>	A JsonResponse. </returns>
		public JsonResponse SendCorsResponse<T>(RequestContext ctx, T data)
		{
			JsonResponse r = ctx.Response<T>(data);

			// allow all
			r.Headers[Response.kAccessControlOrigin] = "*";
			r.Send();

			return r;
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
		public Task OnGetMarket(RequestContext ctx, T dummy)
		{
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);

			MarketRow market = m_database.GetMarket(symbolPair);
			if (market == null)
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}
			else
			{
				//ctx.Respond<MarketRow>(market);
				SendCorsResponse<MarketRow>(ctx, market);
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
		public Task OnGetAllMarkets(RequestContext ctx, T dummy)
		{
			//ctx.Respond<List<MarketRow>>(m_database.GetAllMarkets());

			SendCorsResponse<List<MarketRow>>(ctx, m_database.GetAllMarkets());
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
		public Task OnGetOrderStatus(RequestContext ctx, T dummy)
		{
			string txid = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kTxId);

			TransactionsRow t = m_database.GetTransaction(txid);
			if (t==null)
			{
				throw new ApiExceptionOrderNotFound(txid);
			}

			//ctx.Respond<TransactionsRow>(t);
			SendCorsResponse<TransactionsRow>(ctx, t);
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
		public Task OnGetLastTransactions(RequestContext ctx, T dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string market = RestHelpers.GetPostArg<string>(ctx, WebForms.kSymbolPair);

			//ctx.Respond<List<TransactionsRowNoUid>>(m_database.GetLastTransactions(limit, market));
			SendCorsResponse<List<TransactionsRowNoUid>>(ctx, m_database.GetLastTransactions(limit, market));
			return null;
		}

		/// <summary>	Executes the produce report action. </summary>
		///
		/// <remarks>	Paul, 14/03/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnProduceReport(RequestContext ctx, T dummy)
		{
			uint sinceTid = RestHelpers.GetQueryArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kSince);
			string market = RestHelpers.GetQueryArg<string>(ctx, WebForms.kSymbolPair);

			MarketRow m = m_database.GetMarket(market);
			if (m == null)
			{
				throw new ApiExceptionUnknownMarket(market);
			}

			List<TransactionsRow> allTrans = m_database.GetCompletedTransactionsInMarketSince(market, sinceTid);

			StringWriter stream = new StringWriter();

			stream.WriteLine("All completed transactions in market " + market + " since tid " + sinceTid + "<br/>");
			stream.WriteLine("<br/>");
			stream.WriteLine("Tid, Type, Price, Amount, Fee, Date<br/>");
			foreach (TransactionsRow t in allTrans)
			{
				stream.WriteLine(t.uid + "," + t.order_type + "," + t.price + "," + t.amount + "," + t.fee + "," + t.date + "<br/>");
			}

			ctx.Respond(stream.ToString(), System.Net.HttpStatusCode.OK);
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
		public Task OnGetMyLastTransactions(RequestContext ctx, T dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string memo = RestHelpers.GetPostArg<string>(ctx, WebForms.kMemo);
			string depositAddress = RestHelpers.GetPostArg<string>(ctx, WebForms.kDepositAddress);

			//ctx.Respond<List<TransactionsRowNoUid>>(m_database.GetLastTransactionsFromDeposit(memo, depositAddress, limit));
			SendCorsResponse<List<TransactionsRowNoUid>>(ctx, m_database.GetLastTransactionsFromDeposit(memo, depositAddress, limit));
			return null;
		}

		/// <summary>	Executes the get all transactions since action. </summary>
		///
		/// <remarks>	Paul, 20/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetAllTransactionsSinceInternal(RequestContext ctx, T dummy)
		{
			uint tid = RestHelpers.GetPostArg<uint>(ctx, WebForms.kSince);
			//ctx.Respond<List<TransactionsRow>>(m_database.GetAllTransactionsSince(tid));
			SendCorsResponse<List<TransactionsRow>>(ctx, m_database.GetAllTransactionsSince(tid));
			return null;
		}

		/// <summary>	Executes the API exception action. </summary>
		///
		/// <remarks>	Paul, 25/01/2015. </remarks>
		///
		/// <param name="sender">	The sender. </param>
		/// <param name="e">	 	The ExceptionWithCtx to process. </param>
		public void OnApiException(object sender, ExceptionWithCtx e)
		{
			if (e.m_e is ApiException)
			{
				ApiException apiE = (ApiException)e.m_e;

				if (e.m_ctx.ListenerResponse.Headers.Count == 0)
				{
					//e.m_ctx.Respond<ApiError>(apiE.m_error);
					SendCorsResponse<ApiError>(e.m_ctx, apiE.m_error);
				}
			}
			else if (e.m_ctx != null)
			{
				m_database.LogGeneralException(e.m_e.ToString());

				if (e.m_ctx.ListenerResponse.Headers.Count == 0)
				{
					//e.m_ctx.Respond<ApiError>(new ApiExceptionGeneral().m_error);
					SendCorsResponse<ApiError>(e.m_ctx, new ApiExceptionGeneral().m_error);
				}
			}
			else
			{
				throw e.m_e;
			}
		}
	}
}
