using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesCoreUnitTests;
using MetaDaemonUnitTests;

namespace UnitTestDebugging
{
	class Program
	{
		static void Main(string[] args)
		{
			using (MiscTest test = new MiscTest())
			{
				test.GetMyLastTransactionsNoMemoOrDepositAddress();
			}
		}
	}
}
