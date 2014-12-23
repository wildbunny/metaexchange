using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitcoinRpcSharp.Responses;
using BitsharesRpc;

namespace BtsOnrampDaemon
{
	public class UnsupportedTransactionException : Exception
	{
		BitsharesWalletTransaction m_t;

		public UnsupportedTransactionException(BitsharesWalletTransaction t)
		{
			m_t = t;
		}
	}

	public class MultiplePublicKeysException : Exception
	{
		DecodedRawTransaction m_t;

		public MultiplePublicKeysException(DecodedRawTransaction t)
		{
			m_t = t;
		}
	}
}
