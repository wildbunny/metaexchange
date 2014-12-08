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

			// psudocode
			//
			// loop
			//    list all confirmed BTS transactions since last block checked
			//    if any is a deposit of the asset we are tracking to our deposit address
			//       get full details
			//       if we have not paid out for this deposit before
			//          turn the BTS address / one_time_key info into a bitcoin address
			//          send bitcoins to this address
			//          mark this deposit as paid
			//          log everything
			//    update last block checked 
			//       
			//    list all confirmed BTC transactions since last block checked
			//    for every deposit to our deposit address
			//       get full details
			//       if we have not sent the depositor the assets for this transaction
			//          turn the public key from the deposit transaction into a BTS address
			//          send assets to this BTS address
			//          mark this deposit as paid
			//          log everything
			//    update last block checked
		}
	}
}
