using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BtsOnrampDaemon
{
	public class DaemonMySql : DaemonBase
	{
		public DaemonMySql(RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string bitsharesAsset,
							string bitcoinDespositAddress) : base(bitsharesConfig, bitcoinConfig, bitsharesAccount, bitsharesAsset, bitcoinDespositAddress)
		{

		}

		public override uint GetLastBitsharesBlock()
		{
			return 0;
		}

		public override void UpdateBitsharesBlock(uint blockNum)
		{
			
		}

		public override bool HasBitsharesDepositBeenCredited(string trxId)
		{
			return false;
		}

		public override void MarkBitsharesDespositAsCredited(string bitsharesTxId, string bitcoinTxId)
		{
			
		}
	}
}
