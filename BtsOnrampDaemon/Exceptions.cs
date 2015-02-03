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
		string m_trxId;

		public UnsupportedTransactionException(string t)
		{
			m_trxId = t;
		}

		public override string ToString()
		{
			return m_trxId;
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

		public override string Message
		{
			get { return m_memo; }
		}
	}

	public class ApiException : Exception
	{
		public ApiError m_error;

		public ApiException( ApiError error )
		{
			m_error = error;
		}

		public override string ToString()
		{
			return m_error.m_errorMsg;
		}
	}

	public class ApiExceptionGeneral : ApiException
	{
		public ApiExceptionGeneral() : base(new ApiError { m_errorMsg = "Ooops, a general API exception occured!" }) { }
	}

	public class ApiExceptionMissingParameter : ApiException
	{
		public ApiExceptionMissingParameter() : base(new ApiError{m_errorMsg ="Missing parameter"}){}
	}

	public class ApiExceptionMessage : ApiException
	{
		public ApiExceptionMessage(string message) : base(new ApiError { m_errorMsg = message }) { }
	}
}
