using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;

using BitsharesRpc;
using BitsharesCore;

namespace BtsOnrampDaemon
{
	class Program
	{

		/*static bool is_valid_v2( string base58str )
	   {
		   try
		   {
			   string prefix = "BTS";
			   int prefix_len = prefix.Length;
			   Debug.Assert( base58str.Length > prefix_len );
			   Debug.Assert( base58str.Substring( 0, prefix_len ) == prefix );

			  
			   byte[] v =  Base58.ToByteArray(base58str.Substring( prefix_len ) );
			   
			   Debug.Assert( v.Length > 4, "all addresses must have a 4 byte checksum" );
			   Debug.Assert(v.Length <= 20 + 4, "all addresses are less than 24 bytes");

			   RIPEMD160 rip = System.Security.Cryptography.RIPEMD160.Create();
			   byte[] checksum = rip.ComputeHash(v);

			   const fc::ripemd160 checksum = fc::ripemd160::hash( v.data(), v.size() - 4 );
			   
			   Debug.Assert( memcmp( v.data() + 20, (char*)checksum._hash, 4 ) == 0, "address checksum mismatch" );
			   return true;
		   }
		   catch( ... )
		   {
			   return false;
		   }
	   }*/

		static void Main(string[] args)
		{
			BitsharesWallet w = new BitsharesWallet("http://localhost:65066/rpc", "rpc_user", "abcdefgh");

			List<BitsharesWalletTransaction> trans = w.WalletAccountTransactionHistory();

			if (trans.Count > 0)
			{
				BitsharesTransaction t = w.BlockchainGetTransaction(trans.First().trx_id);


				string btsPubKey = t.trx.operations[1].data.condition.data.memo.one_time_key;

				BitsharesPubKey key = new BitsharesPubKey(btsPubKey);
			}

			DaemonMySql daemon = new DaemonMySql(	new RpcConfig { m_url = "http://localhost:65066/rpc", m_rpcUser = "rpc_user", m_rpcPassword = "abcdefgh" },
													new RpcConfig { }, "monsterer", "BTS", "");

			daemon.Join();
		}
	}
}
