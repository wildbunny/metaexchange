using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitsharesRpc;

namespace BtsOnrampDaemon
{
	class Program
	{
		static void Main(string[] args)
		{
			BitsharesWallet w = new BitsharesWallet("http://localhost:65066/rpc", "rpc_user", "abcdefgh");

			List<BitsharesWalletTransaction> trans = w.WalletAccountTransactionHistory();

			if (trans.Count > 0)
			{
				BitsharesTransaction t = w.BlockchainGetTransaction(trans.First().trx_id);
			}
		}
	}
}
