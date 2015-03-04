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
using Casascius.Bitcoin;


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
			string asm = "0 3045022100aa1725d151738b2fdac99bb0dd7349b8f28ff82df0008670e26b4dacc2ba6fee022020aa163873365dc23f4ee5d6a067ab6ea9a4553655708b65e6b727ec765a1baf01 304402207bda6b7ff3128d21149ca047c546a0e4b3c7d96a0bb0853fba83f8fab69484a402204bd696ff81eb9bec57f6e7c2c796c44eeb5526dcccdaa525412eb0b73ac788ac01 5221021f55a88ca40b2076297acc74c10b19fd048a2ccf94260a996f1fd2481eac4c812103abee3cc167f61dab119ba92eec629de7c62aa0f2a7c32f967990e77d6cd81a74210276efd2c834f19eaca70536e9706a73e43fc2bde98d2cefddc7f34bb3ffbdb00353ae";

			string hex = "00483045022100aa1725d151738b2fdac99bb0dd7349b8f28ff82df0008670e26b4dacc2ba6fee022020aa163873365dc23f4ee5d6a067ab6ea9a4553655708b65e6b727ec765a1baf0147304402207bda6b7ff3128d21149ca047c546a0e4b3c7d96a0bb0853fba83f8fab69484a402204bd696ff81eb9bec57f6e7c2c796c44eeb5526dcccdaa525412eb0b73ac788ac014c695221021f55a88ca40b2076297acc74c10b19fd048a2ccf94260a996f1fd2481eac4c812103abee3cc167f61dab119ba92eec629de7c62aa0f2a7c32f967990e77d6cd81a74210276efd2c834f19eaca70536e9706a73e43fc2bde98d2cefddc7f34bb3ffbdb00353ae";

			
			string[] parts = asm.Split(' ');

			string pk;
			if (parts.Length == 2)
			{
				pk = parts[1];
			}
			else if (parts.Length == 4)
			{
				pk = parts[3];
			}
			else
			{
				pk = null;
			}


			string addr = MofN.GetAddressFromScriptSig(hex);

			//Console.WriteLine(result);
		}
	}
}
