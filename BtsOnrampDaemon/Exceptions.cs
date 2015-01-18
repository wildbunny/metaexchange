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

		public override string ToString()
		{
			return m_t.trx_id;
		}
	}

	public class RefundBitcoinException : RefundBitsharesException 
	{
		public RefundBitcoinException(string memo) : base(memo) { }
	}

	public class RefundBitsharesException : Exception
	{
		string m_memo;

		public RefundBitsharesException(string memo)
		{
			m_memo = memo;
		}

		public override string ToString()
		{
			return m_memo;
		}
	}
}
