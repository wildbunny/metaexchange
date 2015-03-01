using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebDaemonShared
{
	public enum ApiErrorCode
	{
		None=0,
		GeneralException,
		MissingParameter,
		UnknownMarket,
		InvalidAddress,
		InvalidAccount,
		OrderNotFound,
	}

	public class ApiError
	{
		public string message;
		public ApiErrorCode error;

		public ApiError(string e, ApiErrorCode code)
		{
			message = e;
			error = code;
		}
	}

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
			return m_error.message;
		}
	}

	public class ApiExceptionGeneral : ApiException
	{
		public ApiExceptionGeneral() : base(new ApiError("Ooops, a general API exception occured!", ApiErrorCode.GeneralException)) { }
	}

	public class ApiExceptionMissingParameter : ApiException
	{
		public ApiExceptionMissingParameter() : base(new ApiError("Missing parameter", ApiErrorCode.MissingParameter)){}
	}

	public class ApiExceptionUnknownMarket : ApiException
	{
		public ApiExceptionUnknownMarket(string symbolPair) : base(new ApiError(symbolPair + " is not a recognised market!", ApiErrorCode.UnknownMarket)) { }
	}

	public class ApiExceptionInvalidAddress : ApiException
	{
		public ApiExceptionInvalidAddress(string address) : base(new ApiError(address + " is not a valid bitcoin address!", ApiErrorCode.InvalidAddress)) { }
	}

	public class ApiExceptionInvalidAccount : ApiException
	{
		public ApiExceptionInvalidAccount(string account) : base(new ApiError(account + " is not an existing account! Are you sure it is registered?", ApiErrorCode.InvalidAccount)) { }
	}

	public class ApiExceptionOrderNotFound : ApiException
	{
		public ApiExceptionOrderNotFound(string txid) : base(new ApiError("Order " + txid + " not found!", ApiErrorCode.OrderNotFound)) { }
	}
}
