using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;

using BitsharesRpc;
using BitsharesCore;
using Monsterer.Util;

namespace BtsOnrampDaemon
{
	class Program
	{
		static void Main(string[] args)
		{
			/*BitsharesWallet w = new BitsharesWallet("http://localhost:65066/rpc", "rpc_user", "abcdefgh");

			List<BitsharesWalletTransaction> trans = w.WalletAccountTransactionHistory();

			if (trans.Count > 0)
			{
				BitsharesTransaction t = w.BlockchainGetTransaction(trans.First().trx_id);


				string btsPubKey = t.trx.operations[1].data.condition.data.memo.one_time_key;

				BitsharesPubKey key = new BitsharesPubKey(btsPubKey);
			}*/

			// create a scheduler so we can be sure of thread affinity
			AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

			DaemonMySql daemon = new DaemonMySql(	new RpcConfig { m_url = "http://localhost:65066/rpc", m_rpcUser = "rpc_user", m_rpcPassword = "abcdefgh" },
													new RpcConfig { m_url = "http://localhost:18332", m_rpcUser = "bitcoinrpc", m_rpcPassword = "HTQAHLqsETJJZ9WXpDg5jrU5bzLy9mnuV2qLG9gsHPoq", m_useTestnet = true },
													"gatewaytest", "BTS", "midNXX13beTs1bha8xYcuLBu24egN4DVKt",
													"metaexchange", "metaexchange", "ZSbyH7bGCz6BXHRY");

			scheduler.Run( daemon.Join );

			Console.WriteLine("Exiting...");
		}

		static void OnException(Exception e)
		{
			Console.WriteLine(e.ToString());
			Debugger.Break();
		}
	}
}
