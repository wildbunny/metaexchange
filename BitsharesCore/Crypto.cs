using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.BouncyCastle.Crypto.Digests;

namespace BitsharesCore
{
	public class Crypto
	{
		public static byte[] ComputeSha512(byte[] ofwhat)
		{
			Sha512Digest sha512 = new Sha512Digest();
			sha512.BlockUpdate(ofwhat, 0, ofwhat.Length);
			byte[] rv = new byte[64];
			sha512.DoFinal(rv, 0);
			return rv;
		}
	}
}
