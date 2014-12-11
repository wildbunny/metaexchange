using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using BitsharesRpc;
using BitcoinRpcSharp;
using BitsharesCore;

namespace BtsOnrampDaemon
{
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
	abstract public class DaemonBase
	{
		const int kSleepTimeSeconds = 20;

		BitsharesWallet m_bitshares;
		BitcoinWallet m_bitcoin;

		BitsharesAsset m_asset;

		string m_bitsharesAccount;
		string m_bitsharesAsset;
		string m_bitcoinDespoitAddress;

		public EventHandler<Exception> ExceptionHandler;

		public DaemonBase(	RpcConfig bitsharesConfig, RpcConfig bitcoinConfig, 
							string bitsharesAccount, string bitsharesAsset,
							string bitcoinDespositAddress)
		{
			m_bitshares = new BitsharesWallet(bitsharesConfig.m_url, bitsharesConfig.m_rpcUser, bitsharesConfig.m_rpcPassword);
			m_bitcoin = new BitcoinWallet(bitcoinConfig.m_url, bitcoinConfig.m_rpcUser, bitcoinConfig.m_rpcPassword, false);

			m_bitsharesAccount = bitsharesAccount;
			m_bitsharesAsset = bitsharesAsset;
			m_bitcoinDespoitAddress = bitcoinDespositAddress;

			m_asset = m_bitshares.BlockchainGetAsset(bitsharesAsset);
		}

		public abstract uint GetLastBitsharesBlock();
		public abstract bool HasBitsharesDepositBeenCredited(string trxId);
		public abstract void MarkBitsharesDespositAsCredited(string bitsharesTxId, string bitcoinTxId);

		public void Join()
		{
			while (true)
			{
				try
				{
					// which block do we start from
					uint lastBlockBitshares = GetLastBitsharesBlock();

					// which block do we end on
					GetInfoResponse info = m_bitshares.GetInfo();

					// get all relevant bitshares deposits
					List<BitsharesWalletTransaction> assetTransactions = m_bitshares.WalletAccountTransactionHistory(	m_bitsharesAccount, 
																														m_bitsharesAsset, 
																														0, 
																														lastBlockBitshares, 
																														info.blockchain_head_block_num);

					List<BitsharesWalletTransaction> assetDeposits = assetTransactions.Where(	t=>t.is_confirmed && 
																								t.ledger_entries.Any(l=>l.to_account == m_bitsharesAccount && l.from_account != l.to_account)).ToList();
					foreach (BitsharesWalletTransaction t in assetDeposits)
					{
						// make sure we didn't already send bitcoins for this deposit
						if (!HasBitsharesDepositBeenCredited(t.trx_id))
						{
							foreach (BitsharesLedgerEntry l in t.ledger_entries)
							{
								if (l.to_account == m_bitsharesAccount)
								{
									// get the public key of the sender
									BitsharesAccount account = m_bitshares.WalletGetAccount(l.from_account);

									// turn that into a BTC address
									BitsharesPubKey pubKey = new BitsharesPubKey(account.owner_key);
									string btcAddress = pubKey.ToBitcoinAddress(false);

									// get the BTC amount we need to transfer
									decimal btcToTransfer = m_asset.GetAmountFromLarimers(l.amount.amount);

									// do the transfer
									string txid = m_bitcoin.SendToAddress(btcAddress, btcToTransfer);

									// mark this in our records
									MarkBitsharesDespositAsCredited(t.trx_id, txid);
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					if (ExceptionHandler != null)
					{
						ExceptionHandler(this, e);
					}
				}

				Thread.Sleep(kSleepTimeSeconds*1000);
			}
		}
	}
}
