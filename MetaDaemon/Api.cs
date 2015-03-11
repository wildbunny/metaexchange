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
using ServiceStack.Text;

namespace MetaDaemon
{
	public partial class MetaDaemonApi
	{
		string m_masterSiteUrl;

		/// <summary>	API push. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="content">	The content. </param>
		///
		/// <returns>	A Task. </returns>
		Task<string> ApiPush<T>(string route, T content)
		{
			return Rest.ExecutePostAsync(m_masterSiteUrl + route, JsonSerializer.SerializeToString<T>(content));
		}

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
		Task OnSubmitAddress(RequestContext ctx, IDummyDaemon dummy)
		{
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);
			string receivingAddress = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kReceivingAddress);
			MetaOrderType orderType = RestHelpers.GetPostArg<MetaOrderType, ApiExceptionMissingParameter>(ctx, WebForms.kOrderType);
			uint referralUser = RestHelpers.GetPostArg<uint>(ctx, WebForms.kReferralId);

			if (!m_marketHandlers.ContainsKey(symbolPair))
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}

			// prevent our own deposit addresses from being used as receiving addresses
			if (m_dataAccess.GetSenderDepositFromDeposit(receivingAddress, symbolPair, referralUser) != null)
			{
				throw new ApiExceptionInvalidAddress("<internal deposit address>");
			}

			// get the handler for this market
			MarketBase market = m_marketHandlers[symbolPair];

			// get the response and send it
			SubmitAddressResponse response = market.OnSubmitAddress(receivingAddress, orderType, referralUser);

			ctx.Respond<SubmitAddressResponse>(response);

			return null;
		}

		/// <summary>	Executes the ping action. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnPing(RequestContext ctx, IDummyDaemon dummy)
		{
			ctx.Respond<bool>(true);
			return null;
		}
	}
}
