using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

using Monsterer.Util;
using Monsterer.Request;

using WebHost;
using WebHost.Components;
using ApiHost;
using BitsharesRpc;

using MetaExchange.Pages;

namespace MetaExchange
{
	public class Constants
	{
		public const string kWebRoot = ".";
		public const string kSharedJsListName = "Pages/RequiredJs/Shared.rs";
		public const double kUpdateTimeoutSeconds = 5;
	}
	
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length >= 13 && args.Length < 15)
			{
				string httpUrl = args[0];

				string bitsharesUrl = args[1];
				string bitsharesUser = args[2];
				string bitsharesPassword = args[3];
				string bitsharesAccount = args[4];
				
				string bitcoinUrl = args[5];
				string bitcoinUser = args[6];
				string bitcoinPassword = args[7];
				bool bitcoinUseTestNet = bool.Parse(args[8]);

				string database = args[9];
				string databaseUser = args[10];
				string databasePassword = args[11];

				string apiBaseUrl = args[12];
				string ipLock = null;
				if (args.Length == 14)
				{
					ipLock = args[13];
				}

				using (var server = new MetaServer(httpUrl, Constants.kWebRoot, new RpcConfig
																				{
																					m_rpcPassword = bitsharesPassword,
																					m_rpcUser = bitsharesUser,
																					m_url = bitsharesUrl,
																					m_useTestnet = false
																				},
																				new RpcConfig
																				{
																					m_rpcPassword = bitcoinPassword,
																					m_rpcUser = bitcoinUser,
																					m_url = bitcoinUrl,
																					m_useTestnet = bitcoinUseTestNet
																				},
																				apiBaseUrl,
																				database, databaseUser, databasePassword,
																				bitsharesAccount))
				{
					AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

					server.ExceptionEvent += OnServerException;

					if (ipLock != null)
					{
						server.SetIpLock(ipLock);
					}

					scheduler.RunWithUpdate(server.Start, server.Update, Constants.kUpdateTimeoutSeconds);

					Console.WriteLine("Exiting...");
				}	
			}
			else
			{
				Console.WriteLine("Error, usage.");
			}			
		}


		static void OnServerException(object sender, ExceptionWithCtx e)
		{
			Console.WriteLine(e.m_e.ToString());
			//throw e.m_e;
		}

		static void OnException(Exception e)
		{
			Console.WriteLine(e.ToString());
			//throw e;
		}
	}
}
