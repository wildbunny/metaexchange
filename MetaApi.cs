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
		const int kMb = 1024 * 1024;

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

			IEnumerable<string> ourIps = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select<IPAddress, string>(ip => ip.ToString());
			IEnumerable<string> daemons = m_auth.m_Database.GetAllMarkets().Select<MarketRow, string>(r => r.daemon_url);

			List<Uri> uris = new List<Uri>();
			List<string> ips = new List<string>();
			foreach (string d in daemons)
			{
				IPAddress[] e = Dns.GetHostAddresses(new Uri(d).Host);

				foreach (IPAddress ip in e)
				{
					ips.Add(ip.ToString());
				}
			}

			bool allowed = ips.Contains(from) || ourIps.Contains(from);

			if (!allowed)
			{
				m_Database.LogGeneralException("ConfirmDaemon(" + from + ") failed...");
			}

			return allowed;
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

			if (response != null)
			{
				// get the juicy data out
				SubmitAddressResponse data = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(response);

				// pull the market out of the request
				string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);
				uint referralUser = RestHelpers.GetPostArg<uint>(ctx, WebForms.kReferralId);

				MarketRow m = dummy.m_database.GetMarket(symbolPair);

				// stick it in the master database
				dummy.m_database.InsertSenderToDeposit(data.receiving_address, data.deposit_address, m.symbol_pair, referralUser, true);
				
				if (referralUser > 0)
				{
					// track referrals
					string depositAddress;

					if (data.memo != null)
					{
						depositAddress = data.memo;
					}
					else
					{
						depositAddress = data.deposit_address;
					}

					dummy.m_database.InsertReferralAddress(depositAddress, referralUser);
				}
			}
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

			try
			{
				database.BeginTransaction();

				foreach (TransactionsRow r in newTrans)
				{
					// this may have problems with partial transactions
					database.InsertTransaction(r.symbol_pair, r.deposit_address, r.order_type, r.received_txid, r.sent_txid, r.amount, r.price, r.fee, r.status, r.date, r.notes, TransactionPolicy.REPLACE);

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

				database.EndTransaction();
			}
			catch (Exception)
			{
				database.RollbackTransaction();
				throw;
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
		async Task OnPushTransactions(RequestContext ctx, IDummy dummy)
		{
			if (ConfirmDaemon(ctx, dummy))
			{
				string allTrans = ctx.Request.Body;
				if (ctx.Request.m_Truncated)
				{
					allTrans += await ctx.Request.GetBody(kMb);
				}
							
				List<TransactionsRow> newTrans = JsonSerializer.DeserializeFromString<List<TransactionsRow>>(allTrans);

				OnPushTransactions(newTrans, dummy.m_database);

				ctx.Respond<bool>(true);
			}
			else
			{
				ctx.Respond<bool>(false);
			}
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
				ctx.Respond<bool>(true);
			}
			else
			{
				ctx.Respond<bool>(false);
			}

			return null;
		}

		/// <summary>	Executes the push fee collection action. </summary>
		///
		/// <remarks>	Paul, 06/03/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnPushFeeCollection(RequestContext ctx, IDummy dummy)
		{
			if (ConfirmDaemon(ctx, dummy))
			{
				long oldRow = m_Database.CountFeeRows();

				List<FeeCollectionRow> allFees = JsonSerializer.DeserializeFromString<List<FeeCollectionRow>>(ctx.Request.Body);

				try
				{
					dummy.m_database.BeginTransaction();

					foreach (FeeCollectionRow fee in allFees)
					{
						dummy.m_database.InsertFeeTransaction(fee.symbol_pair,
																fee.buy_trxid,
																fee.sell_trxid,
																fee.buy_fee,
																fee.sell_fee,
																fee.transaction_processed_uid,
																fee.exception, 
																fee.start_txid,
																fee.end_txid,
																true);
					}

					dummy.m_database.EndTransaction();

					long newRows = m_Database.CountFeeRows();

					if (newRows > oldRow)
					{
						// here we should process the fees
						FeeReporting(allFees);
					}

					ctx.Respond<bool>(true);
				}
				catch (Exception)
				{
					dummy.m_database.RollbackTransaction();

					ctx.Respond<bool>(false);

					throw;
				}
			}
			else
			{
				ctx.Respond<bool>(false);
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
			catch (WebException e)
			{
				// make sure to log this so we can check the details
				m_Database.LogGeneralException(e.ToString());

				throw new ApiExceptionGeneral();
			}
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

		/// <summary>	Gets the visisble markets in this collection. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
		///
		/// <param name="initial">	The initial. </param>
		///
		/// <returns>
		/// An enumerator that allows foreach to be used to process the visisble markets in this
		/// collection.
		/// </returns>
		IEnumerable<MarketRow> GetVisisbleMarkets(List<MarketRow> initial)
		{
			return initial.Where(m => m.visible);
		}

		/// <summary>	Executes the get all markets action. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		Task OnGetAllMarkets(RequestContext ctx, IDummy dummy)
		{
			ctx.Respond<List<MarketRow>>(GetVisisbleMarkets(m_Database.GetAllMarkets()).ToList());
			return null;
		}

		/// <summary>	Executes the get market action. </summary>
		///
		/// <remarks>	Paul, 28/02/2015. </remarks>
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

			MarketRow market = m_Database.GetMarket(symbolPair);
			if (market == null)// || !market.visible)
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}
			else
			{
				ctx.Respond<MarketRow>(market);
			}

			return null;
		}
	}
}
