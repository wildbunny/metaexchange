using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebDaemonShared
{
    public class Routes
    {
		public const string kSubmitAddress = "/api/1/submitAddress";
		public const string kGetOrderStatus = "/api/1/getOrderStatus";
		public const string kGetStats = "/api/1/getStats";
		public const string kGetMarket = "/api/1/getMarket";
		public const string kGetAllMarkets = "/api/1/getAllMarkets";
		public const string kGetLastTransactions = "/api/1/getLastTransactions";
		public const string kGetMyLastTransactions = "/api/1/getMyLastTransactions";

		public const string kPushSenderToDeposit = "/api/1/pushSenderToDeposit";
		public const string kPushTransactions = "/api/1/pushTransactions";
		public const string kPushMarket = "/api/1/pushMarket";
		public const string kPushFees = "/api/1/pushFees";
		public const string kGetAllTransactionsSince = "/api/1/getAllTransactionsSince";
		public const string kPing = "/api/1/ping";
    }
}
