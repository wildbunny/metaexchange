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
using Casascius.Bitcoin;

namespace MetaDaemon
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 17)
			{
				string bitsharesUrl = args[0];
				string bitsharesUser = args[1];
				string bitsharesPassword = args[2];
				string bitsharesAccount = args[3];

				string bitcoinUrl = args[4];
				string bitcoinUser = args[5];
				string bitcoinPassword = args[6];
				bool bitcoinUseTestNet = bool.Parse(args[7]);

				string database = args[8];
				string databaseUser = args[9];
				string databasePassword = args[10];

				string apiListen = args[11];

				string bitcoinFeeAddress = args[12];
				string bitsharesFeeAccount = args[13];
				string adminUsernames = args[14];

				string masterSiteUrl = args[15];
				string masterSiteIp = args[16];

				// create a scheduler so we can be sure of thread affinity
				AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

				using (MetaDaemonApi daemon = new MetaDaemonApi(new RpcConfig { m_url = bitsharesUrl, m_rpcUser = bitsharesUser, m_rpcPassword = bitsharesPassword },
																new RpcConfig { m_url = bitcoinUrl, m_rpcUser = bitcoinUser, m_rpcPassword = bitcoinPassword, m_useTestnet = bitcoinUseTestNet },
																bitsharesAccount,
																database, databaseUser, databasePassword,
																apiListen,
																bitcoinFeeAddress, bitsharesFeeAccount, adminUsernames,
																masterSiteUrl, masterSiteIp,
																scheduler))
				{
					scheduler.RunWithUpdate(daemon.Start, daemon.Update, 5);
				}

				Console.WriteLine("Exiting...");
			}
			else
			{
				Console.WriteLine("Error, usage.");
			}
		}

		static void OnException(Exception e)
		{
			Console.WriteLine("Unhandled exception! This is a bug, please inform the developer.");
			Console.WriteLine(e.ToString());
			Console.WriteLine();
		}
	}
}
