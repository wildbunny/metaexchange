using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

using BitsharesCoreUnitTests;
using MetaDaemonUnitTests;
using RestLib;
using WebDaemonShared;
using ServiceStack.Text;
using MetaData;


namespace UnitTestDebugging
{
	class Program
	{
		public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
									  SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		static void Main(string[] args)
		{
			/*using (MiscTest test = new MiscTest())
			{
				test.VerifiyAccountNameWithHipan();
			}*/

			ServicePointManager.ServerCertificateValidationCallback = Validator;

			var h = new Program();

			h.Test();
		}

		void Test()
		{
			string result = Rest.ExecutePostSync("https://metaexchange.info" + Routes.kPushMarket, JsonSerializer.SerializeToString<MarketRow>(
				new MarketRow
				{
					ask=100,
					bid=200,
					symbol_pair="BTC_bitUSD"
				}));

			Console.WriteLine(result);
		}
	}
}
