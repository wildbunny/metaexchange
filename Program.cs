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
		public const double kUpdateTimeoutSeconds = 5;
	}
	
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length >= 3 && args.Length < 4)
			{
				string httpUrl = args[0];

				string apiBaseUrl = args[1];
				bool maintenance = bool.Parse(args[2]);
				string ipLock = null;
				if (args.Length == 4)
				{
					ipLock = args[3];
				}

				using (var server = new MetaServer(httpUrl, Constants.kWebRoot, apiBaseUrl,
																				maintenance))
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
