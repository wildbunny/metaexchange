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
		static MetaServer m_gServer;

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

				using (m_gServer = new MetaServer(httpUrl, Constants.kWebRoot, database, databaseUser, databasePassword, maintenance))
				{
					AsyncPump scheduler = new AsyncPump(Thread.CurrentThread, OnException);

					m_gServer.ExceptionEvent += OnServerException;

					if (ipLock != null)
					{
						m_gServer.SetIpLock(ipLock);
					}

					scheduler.RunWithUpdate(m_gServer.Start, m_gServer.Update, Constants.kUpdateTimeoutSeconds);

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
			m_gServer.m_Api.OnApiException(sender, e);
			m_gServer.m_Database.LogGeneralException(e.m_e.ToString());
		}

		static void OnException(Exception e)
		{
			m_gServer.m_Database.LogGeneralException(e.ToString());
		}
	}
}
