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
//using BitsharesRpc;

using MetaExchange.Pages;

namespace MetaExchange
{
	public class Constants
	{
		public const string kWebRoot = ".";
		public const string kSharedJsListName = "Pages/RequiredJs/Shared.rs";
		public const double kUpdateTimeoutSeconds = 30;
	}
	
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length >= 5 && args.Length <= 6)
			{
				string httpUrl = args[0];

				string database = args[1];
				string databaseUser = args[2];
				string databasePassword = args[3];

				bool maintenance = bool.Parse(args[4]);
				string ipLock = null;
				if (args.Length == 6)
				{
					ipLock = args[5];
				}

				using (var server = new MetaServer(httpUrl, Constants.kWebRoot, database, databaseUser, databasePassword, maintenance))
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
