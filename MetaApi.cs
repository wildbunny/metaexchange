using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;

using Monsterer.Request;
using WebDaemonShared;
using WebDaemonSharedTables;
using ApiHost;
using MetaData;
using RestLib;
using ServiceStack.Text;

namespace MetaExchange
{
	public partial class MetaServer : IDisposable
	{
		const int kAggregateTimeoutMillis = 5000;

		/// <summary>	Make sure this request really came from one of our daemons </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		bool ConfirmDaemon(RequestContext ctx, IDummy dummy)
		{
			string from = ctx.Request.RemoteEndPoint.Address.ToString();

			IEnumerable<string> daemons = m_auth.m_Database.GetAllMarkets().Select<MarketRow, string>(r => r.daemon_url);

			List<Uri> uris = new List<Uri>();
			List<string> ips = new List<string>();
			foreach (string d in daemons)
			{
				ips.Add(new Uri(d).Host);
			}
			
			return ips.Contains(from);
		}

		/// <summary>	Executes the submit address action. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		async Task OnSubmitAddress(RequestContext ctx, IDummy dummy)
		{
			// intercept the response and stick it in the site database so we can handle forwarding future queries

			// forward the post on
			string response = await ForwardPostSpecific(ctx, dummy);

			// get the juicy data out
			SubmitAddressResponse data = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(response);

			// pull the market out of the request
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);
			MarketRow m = dummy.m_database.GetMarket(symbolPair);

			// stick it in the master database
			dummy.m_database.InsertSenderToDeposit(data.receiving_address, data.deposit_address, m.symbol_pair, true);
		}

		/// <summary>	Executes the push sender to deposit action. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnPushSenderToDeposit(RequestContext ctx, IDummy dummy)
		{
			if (ConfirmDaemon(ctx, dummy))
			{
				SenderToDepositRow s2d = JsonSerializer.DeserializeFromString<SenderToDepositRow>(ctx.Request.Body);
				dummy.m_database.InsertSenderToDeposit(s2d.receiving_address, s2d.deposit_address, s2d.symbol_pair);
			}

			return null;
		}

		/// <summary>	Executes the push transactions action. </summary>
		///
		/// <remarks>	Paul, 20/02/2015. </remarks>
		///
		/// <param name="newTrans">	The new transaction. </param>
		/// <param name="database">	The database. </param>
		void OnPushTransactions(List<TransactionsRow> newTrans, MySqlData database)
		{
			Dictionary<string, uint> lastSeen = new Dictionary<string, uint>();

			foreach (TransactionsRow r in newTrans)
			{
				// this may have problems with partial transactions
				database.InsertTransaction(r.symbol_pair, r.deposit_address, r.order_type, r.received_txid, r.sent_txid, r.amount, r.price, r.fee, r.status, r.date, r.notes, true);

				if (lastSeen.ContainsKey(r.symbol_pair))
				{
					lastSeen[r.symbol_pair] = Math.Max(r.uid, lastSeen[r.symbol_pair]);
				}
				else
				{
					lastSeen[r.symbol_pair] = r.uid;
				}
			}

			// keep the site upto date with last seen transastion uids
			foreach (KeyValuePair<string, uint> kvp in lastSeen)
			{
				database.UpdateLastSeenTransactionForSite(kvp.Key, kvp.Value);
			}
		}

		/// <summary>	Executes the push transactions action. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnPushTransactions(RequestContext ctx, IDummy dummy)
		{
			if (ConfirmDaemon(ctx, dummy))
			{
				List<TransactionsRow> newTrans = JsonSerializer.DeserializeFromString<List<TransactionsRow>>(ctx.Request.Body);

				OnPushTransactions(newTrans, dummy.m_database);

				ctx.Respond<bool>(true);
			}

			return null;
		}

		/// <summary>	Executes the push market action. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnPushMarket(RequestContext ctx, IDummy dummy)
		{
			if (ConfirmDaemon(ctx, dummy))
			{
				MarketRow market = JsonSerializer.DeserializeFromString<MarketRow>(ctx.Request.Body);
				dummy.m_database.UpdateMarketInDatabase(market);
			}

			return null;
		}

		

		/// <summary>	Forward track IP bans. </summary>
		///
		/// <remarks>	Paul, 16/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionGeneral">	Thrown when an API exception general error condition
		/// 										occurs. </exception>
		///
		/// <param name="ctx">   	The context. </param>
		/// <param name="action">	The action. </param>
		///
		/// <returns>	A Task. </returns>
		async Task<string> ForwardTrackIpBans(RequestContext ctx, Func<RequestContext, Task<string>> action)
		{
			// forward request on
			try
			{
				string result = await action(ctx);
				ctx.RespondJsonFromString(result);

				// handle ban on exception forwarding
				try
				{
					ApiError errorCheck = JsonSerializer.DeserializeFromString<ApiError>(result);
					if (errorCheck.error != ApiErrorCode.None)
					{
						throw new ApiExceptionGeneral();
					}
				}
				catch (SerializationException) { }

				return result;
			}
			catch (WebException)
			{
				ctx.Respond(HttpStatusCode.InternalServerError);
			}

			return null;
		}

		/// <summary>	Forward post specific. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task<string> ForwardPostSpecific(RequestContext ctx, IDummy dummy)
		{
			// pull out the daemon address from the market row
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);
			MarketRow m = dummy.m_database.GetMarket(symbolPair);

			// forward the post on
			return ForwardTrackIpBans(ctx, c => Rest.ExecutePostAsync(ApiUrl(m.daemon_url, c.Request.Url.LocalPath), c.Request.PostArgString));
		}

		/// <summary>	Aggregate result tasks. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="c">		The RequestContext to process. </param>
		/// <param name="post"> 	true to post. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task&lt;T&gt;[]. </returns>
		Task<T>[] AggregateResultTasks<T>(string url, string postArgs, string getArgs, bool post, MySqlData database)
		{
			List<MarketRow> allMarkets = database.GetAllMarkets();

			// get all unique daemon urls
			List<string> allDaemons = allMarkets.Select<MarketRow, string>(r => r.daemon_url).Distinct().ToList();
			Task<T>[] allTasks = new Task<T>[allDaemons.Count()];

			// execute the get on each one			
			for (int i = 0; i < allTasks.Length; i++)
			{
				if (post)
				{
					allTasks[i] = Rest.JsonApiCallAsync<T>(ApiUrl(allDaemons[i], url), postArgs );
				}
				else
				{
					allTasks[i] = Rest.JsonApiGetAsync<T>(ApiUrl(allDaemons[i], url) + (getArgs.Length>0?"?" + getArgs:""));
				}
			}

			return allTasks;
		}

		/// <summary>	Wait and get aggregate list. </summary>
		///
		/// <remarks>	Paul, 21/02/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="allTasks">	all tasks. </param>
		///
		/// <returns>	A List&lt;T&gt; </returns>
		List<T> WaitAndGetAggregateList<T>(Task<List<T>>[] allTasks)
		{
			// wait for them all
			Task.WaitAll(allTasks, kAggregateTimeoutMillis);

			// aggregate the results
			List<T> aggregate = new List<T>();
			foreach (Task<List<T>> t in allTasks)
			{
				if (t.IsCompleted)
				{
					aggregate.AddRange(t.Result);
				}
			}
			return aggregate;
		}

		/// <summary>	Aggregate results of an API call to multiple deamons together in one list of Ts </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="c">		The RequestContext to process. </param>
		/// <param name="post"> 	true to post. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task&lt;string&gt; </returns>
		Task<string> AggregateResult<T>(RequestContext c, bool post, IDummy dummy)
		{
			Task<List<T>>[] allTasks = AggregateResultTasks<List<T>>(c.Request.Url.LocalPath, c.Request.PostArgString, c.Request.Url.Query, post, dummy.m_database);

			List<T> aggregate = WaitAndGetAggregateList<T>(allTasks);

			// return as a task
			return Task.FromResult<string>(JsonSerializer.SerializeToString<List<T>>(aggregate));
		}

		/// <summary>	Forward get. </summary>
		///
		/// <remarks>	Paul, 06/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task ForwardGetAggregate<T>(RequestContext ctx, IDummy dummy)
		{
			return ForwardTrackIpBans(ctx, c => 
			{
				return AggregateResult<T>(c, false, dummy);
			});
		}

		/// <summary>	Forward post aggregate. </summary>
		///
		/// <remarks>	Paul, 19/02/2015. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task ForwardPostAggregate<T>(RequestContext ctx, IDummy dummy)
		{
			return ForwardTrackIpBans(ctx, c =>
			{
				return AggregateResult<T>(c, true, dummy);
			});
		}

		/// <summary>	Pulls the initial data. </summary>
		///
		/// <remarks>	Paul, 20/02/2015. </remarks>
		void PullInitialData()
		{
			try
			{
				Task<List<MarketRow>>[] gets = AggregateResultTasks<List<MarketRow>>(Routes.kGetAllMarkets, null, "", false, m_auth.m_Database);

				List<MarketRow> aggregate = WaitAndGetAggregateList<MarketRow>(gets);
				foreach (MarketRow m in aggregate)
				{
					m_auth.m_Database.UpdateMarketInDatabase(m);
				}
			}
			catch (Exception) { }
		}
	}
}
