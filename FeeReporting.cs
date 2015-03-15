using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using MetaData;
using WebDaemonSharedTables;
using WebDaemonShared;
using ApiHost;
using RestLib;

namespace MetaExchange
{
	public class Pair<T1, T2>
	{
		public Pair(T1 a, T2 b) { First = a; Second = b; }
		public T1 First { get; set; }
		public T2 Second { get; set; }
	}

	public partial class MetaServer : IDisposable
	{
		const decimal kParnerFeeShare = 0.25M;

		void FeeReporting(List<FeeCollectionRow> allFees)
		{
			Dictionary<string, MarketRow> allMarkets = m_Database.GetAllMarkets().ToDictionary(m => m.symbol_pair);
			List<ReferralUserRow> allReferralUsers = m_Database.GetAllReferralUsers();

			Dictionary<uint, ReferralUserRow> usersByUid = allReferralUsers.ToDictionary(u=>u.uid);

			StringWriter email = new StringWriter();

			Dictionary<uint, Dictionary<string, decimal>> totalFeesPerUserPerAsset = new Dictionary<uint, Dictionary<string, decimal>>();
			Dictionary<string, decimal> totalFeesPerAsset = new Dictionary<string, decimal>();

			foreach (FeeCollectionRow feeRow in allFees)
			{
				CurrenciesRow @base, quote;
				CurrencyHelpers.GetBaseAndQuoteFromSymbolPair(feeRow.symbol_pair, m_allCurrencies, out @base, out quote);

				string bitAsset;
				string bitcoin;
				if (allMarkets[feeRow.symbol_pair].flipped)
				{
					bitAsset = quote.ToString();
					bitcoin = @base.ToString();
				}
				else
				{
					bitAsset = @base.ToString();
					bitcoin = quote.ToString();
				}

				if (!totalFeesPerAsset.ContainsKey(bitAsset))
				{
					totalFeesPerAsset[bitAsset] = 0;
				}
				if (!totalFeesPerAsset.ContainsKey(bitcoin))
				{
					totalFeesPerAsset[bitcoin] = 0;
				}

				totalFeesPerAsset[bitAsset] += feeRow.buy_fee;
				totalFeesPerAsset[bitcoin] += feeRow.sell_fee;

				// we know transactions are inserted by the block, so they will be in the same order as on the daemon
				uint startTid = m_Database.GetTransaction(feeRow.start_txid).uid;
				uint endTid = m_Database.GetTransaction(feeRow.end_txid).uid;

				List<TransactionsRow> transSince = m_Database.GetCompletedTransactionsInMarketBetween(feeRow.symbol_pair, startTid, endTid);

				email.WriteLine("Fees for market " + feeRow.symbol_pair + " " + feeRow.buy_fee + " " + bitAsset + "," +feeRow.sell_fee + " " + bitcoin + ", " + transSince.Count + " transactions " + feeRow.date);

				// make sure they add up!
				decimal buyFees = transSince.Where(t => t.order_type == MetaOrderType.buy).Sum(t => t.fee);
				decimal sellFees = transSince.Where(t => t.order_type == MetaOrderType.sell).Sum(t => t.fee);

				sellFees = Numeric.TruncateDecimal(sellFees, 8);

				Debug.Assert(sellFees == feeRow.sell_fee);

				// ok, now work out who referred these transactions
				
				// go through each user
				foreach (KeyValuePair<uint, ReferralUserRow> kvp in usersByUid)
				{
					List<TransactionsRow> referralsForUser = m_Database.GetAllReferralTransactionsForUserBetween(kvp.Key, feeRow.symbol_pair, startTid, endTid);
					if (referralsForUser.Count > 0)
					{
						ReferralUserRow user = usersByUid[kvp.Key];

						if (!totalFeesPerUserPerAsset.ContainsKey(user.uid))
						{
							totalFeesPerUserPerAsset[user.uid] = new Dictionary<string, decimal>();
						}

						decimal userBuyFees = referralsForUser.Where(t => t.order_type == MetaOrderType.buy).Sum(t => t.fee) * kParnerFeeShare;
						decimal userSellFees = referralsForUser.Where(t => t.order_type == MetaOrderType.sell).Sum(t => t.fee) * kParnerFeeShare;

						userBuyFees = Numeric.TruncateDecimal(userBuyFees, 8);
						userSellFees = Numeric.TruncateDecimal(userSellFees, 8);
						
						email.WriteLine("\tUser " + user.bitshares_username + " " + referralsForUser.Count + " transactions");
						foreach (TransactionsRow r in referralsForUser)
						{
							string asset = r.order_type == MetaOrderType.buy ? bitAsset : bitcoin;
							email.WriteLine("\t\t" + r.order_type + " " + r.amount + " " + bitAsset + ", whole_fee=" + r.fee + " " + asset);
						}
						email.WriteLine("\tTotal (their cut):" + userBuyFees + " "+bitAsset + ", " + userSellFees +" " + bitcoin);

						email.WriteLine();

						if (!totalFeesPerUserPerAsset[user.uid].ContainsKey(bitAsset))
						{
							totalFeesPerUserPerAsset[user.uid][bitAsset] = 0;
						}
						if (!totalFeesPerUserPerAsset[user.uid].ContainsKey(bitcoin))
						{
							totalFeesPerUserPerAsset[user.uid][bitcoin] = 0;
						}

						totalFeesPerUserPerAsset[user.uid][bitAsset] += userBuyFees;
						totalFeesPerUserPerAsset[user.uid][bitcoin] += userSellFees;
					}
				}

				email.WriteLine();
			}

			foreach (KeyValuePair<string, decimal> kvp in totalFeesPerAsset)
			{
				email.WriteLine("Total fees " + kvp.Value + " " + kvp.Key);
			}

			email.WriteLine();

			foreach (KeyValuePair<uint, Dictionary<string, decimal>> kvp in totalFeesPerUserPerAsset)
			{
				ReferralUserRow user = usersByUid[kvp.Key];

				email.WriteLine("Total fees for user " + user.bitshares_username + ", " + user.bitcoin_address);

				foreach (KeyValuePair<string, decimal> userKvp in kvp.Value)
				{
					email.WriteLine("\t" + Numeric.TruncateDecimal(userKvp.Value, 8) + " " + userKvp.Key);
				}
			}
			
			#if MONO
			ConfigRow config = m_Database.GetConfig();
			Email.SendMailAsync(config.email_from, config.email_to, "Fee report starting " + allFees.First().date, email.ToString());
			#else
			Console.WriteLine(email.ToString());
			#endif
		}
	}
}
