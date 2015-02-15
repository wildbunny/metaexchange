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
			if (args.Length == 12)
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

				// create a scheduler so we can be sure of thread affinity
				AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

				MetaDaemonApi daemon = new MetaDaemonApi(	new RpcConfig { m_url = bitsharesUrl, m_rpcUser = bitsharesUser, m_rpcPassword = bitsharesPassword },
															new RpcConfig { m_url = bitcoinUrl, m_rpcUser = bitcoinUser, m_rpcPassword = bitcoinPassword, m_useTestnet = bitcoinUseTestNet },
															bitsharesAccount, 
															database, databaseUser, databasePassword,
															apiListen);

				scheduler.RunWithUpdate(daemon.Start, daemon.Update, 5);

				Console.WriteLine("Exiting...");
			}
			else
			{
				Console.WriteLine("Error, usage: MetaDamon.exe <bitshares rpc url> <bitshares rpc user> <bitshares rpc password> " +
									"<bitshares asset name> <bitcoin rpc url> <bitcoin rpc user> <bitcoin rpc password> <use bitcoin testnet> " +
									"<myql database name> <mysql database user> <mysql database password> <api listen address>");
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
