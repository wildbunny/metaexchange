using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitcoinRpcSharp.Responses;
using BitsharesRpc;
using WebDaemonShared;

namespace BtsOnrampDaemon
{
	/*public class ApiError
	{
		public string m_errorMsg;

		public ApiError(string e)
		{
			m_errorMsg = e;
		}
	}*/

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

		public ApiException(ApiError error)
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
		public ApiExceptionGeneral() : base(new ApiError("Ooops, a general API exception occured!")) { }
	}

	public class ApiExceptionMissingParameter : ApiException
	{
		public ApiExceptionMissingParameter() : base(new ApiError("Missing parameter")) { }
	}

	public class ApiExceptionUnsupportedTrade : ApiException
	{
		public ApiExceptionUnsupportedTrade(CurrencyTypes from, CurrencyTypes to) : base(new ApiError(from + "->" + to + " is not a recognised market!")) { }
	}

	public class ApiExceptionMessage : ApiException
	{
		public ApiExceptionMessage(string message) : base(new ApiError(message)) { }
	}
}
