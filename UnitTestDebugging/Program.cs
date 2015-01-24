using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesCoreUnitTests;

namespace UnitTestDebugging
{
	class Program
	{
		static void Main(string[] args)
		{
			AddressAndPubKeyTests test = new AddressAndPubKeyTests();

			test.CheckBtsAddressFromBitcoinPubKeyHex();
		}
	}
}
