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
			if (args.Length == 13)
			{
				string bitsharesUrl = args[0];
				string bitsharesUser = args[1];
				string bitsharesPassword = args[2];
				string bitsharesAccount = args[3];
				string bitsharesAssetName = args[4];
				
				string bitcoinUrl = args[5];
				string bitcoinUser = args[6];
				string bitcoinPassword = args[7];
				bool bitcoinUseTestNet = bool.Parse(args[8]);
				string bitcoinDepositAddress = args[9];

				string database = args[10];
				string databaseUser = args[11];
				string databasePassword = args[12];

				// create a scheduler so we can be sure of thread affinity
				AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

				DaemonMySql daemon = new DaemonMySql(new RpcConfig { m_url = bitsharesUrl, m_rpcUser = bitsharesUser, m_rpcPassword = bitsharesPassword },
														new RpcConfig { m_url = bitcoinUrl, m_rpcUser = bitcoinUser, m_rpcPassword = bitcoinPassword, m_useTestnet = bitcoinUseTestNet },
														bitsharesAccount, bitsharesAssetName, bitcoinDepositAddress,
														database, databaseUser, databasePassword);

				scheduler.Run( daemon.Join );

				Console.WriteLine("Exiting...");
			}
			else
			{
				Console.WriteLine("Error, usage: BtsOnRampDamon.exe <bitshares rpc url> <bitshares rpc user> <bitshares rpc password> " +
									"<bitshares asset name> <bitcoin rpc url> <bitcoin rpc user> <bitcoin rpc password> <use bitcoin testnet> <bitcoin deposit address>" +
									"<myql database name> <mysql database user> <mysql database password>");
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
