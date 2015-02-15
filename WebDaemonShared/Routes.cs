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
    }
}
