using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using BitsharesRpc;
using BitcoinRpcSharp;
using BitsharesCore;
using Casascius.Bitcoin;

using BitcoinRpcSharp.Responses;

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
		const int kSleepTimeSeconds = 1;
		const int kBitcoinConfirms = 3;

		protected BitsharesWallet m_bitshares;
		protected BitcoinWallet m_bitcoin;

		protected BitsharesAsset m_asset;

		protected string m_bitsharesAccount;
		protected string m_bitsharesAsset;
		protected string m_bitcoinDespoitAddress;

		protected byte m_addressByteType;

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

			m_addressByteType = (byte)(bitcoinConfig.m_useTestnet ? AltCoinAddressTypeBytes.BitcoinTestnet : AltCoinAddressTypeBytes.Bitcoin);
		}

		protected abstract uint GetLastBitsharesBlock();
		protected abstract void UpdateBitsharesBlock(uint blockNum);
		protected abstract bool HasBitsharesDepositBeenCredited(string trxId);
		protected abstract void MarkBitsharesDespositAsCredited(string bitsharesTxId, string bitcoinTxId, decimal amount);
		protected abstract bool IsTransactionIgnored(string txid);

		/// <summary>	This is virtual because implementors might like a different way  </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <param name="account">	The account. </param>
		///
		/// <returns>	A string. </returns>
		protected virtual string BitsharesAccountToBitcoinAddress(BitsharesAccount account)
		{
			// turn that into a BTC address
			BitsharesPubKey pubKey = new BitsharesPubKey(account.owner_key);
			return pubKey.ToBitcoinAddress(false, m_addressByteType);
		}

		/// <summary>	Refund bitshares deposit. </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <param name="sender">   	The sender. </param>
		/// <param name="deposit">  	The deposit. </param>
		/// <param name="depositId">	Identifier for the deposit. </param>
		void RefundBitsharesDeposit(BitsharesAccount sender, BitsharesLedgerEntry deposit, string depositId)
		{
			throw new NotImplementedException();
		}

		/// <summary>	Default implementation sends an exactly matching bitcoin transaction to the depositor </summary>
		///
		/// <remarks>	Paul, 23/12/2014. </remarks>
		///
		/// <param name="btcAddress">	The btc address. </param>
		/// <param name="l">		 	The BitsharesLedgerEntry to process. </param>
		///
		/// <returns>	A string. </returns>
		protected virtual string SendBitcoinsToDepositor(string btcAddress, BitsharesWalletTransaction t, BitsharesLedgerEntry l)
		{
			// get the BTC amount we need to transfer
			decimal btcToTransfer = m_asset.GetAmountFromLarimers(l.amount.amount);

			// do the transfer
			string txid = m_bitcoin.SendToAddress(btcAddress, btcToTransfer);

			// mark this in our records
			MarkBitsharesDespositAsCredited(t.trx_id, txid, btcToTransfer);

			return txid;
		}

		/// <summary>	Handles the bitshares desposits. </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		///
		/// <exception cref="UnsupportedTransactionException">	Thrown when an Unsupported Transaction
		/// 													error condition occurs. </exception>
		void HandleBitsharesDesposits()
		{
			// which block do we start from
			uint lastBlockBitshares = GetLastBitsharesBlock();

			// which block do we end on
			GetInfoResponse info = m_bitshares.GetInfo();

			// get all relevant bitshares deposits
			List<BitsharesWalletTransaction> assetTransactions = m_bitshares.WalletAccountTransactionHistory(m_bitsharesAccount,
																												m_bitsharesAsset,
																												0,
																												lastBlockBitshares,
																												info.blockchain_head_block_num);

			IEnumerable<BitsharesWalletTransaction> assetDeposits = assetTransactions.Where(t => t.is_confirmed &&
																							t.ledger_entries.Any(l => l.to_account == m_bitsharesAccount && l.from_account != l.to_account));
			foreach (BitsharesWalletTransaction t in assetDeposits)
			{
				IEnumerable<BitsharesLedgerEntry> deposits = t.ledger_entries.Where(l => l.to_account == m_bitsharesAccount);

				if (deposits.Count() == 1)
				{
					BitsharesLedgerEntry l = deposits.First();

					// make sure we didn't already send bitcoins for this deposit
					if (!HasBitsharesDepositBeenCredited(t.trx_id) && !IsTransactionIgnored(t.trx_id))
					{
						// get the public key of the sender
						BitsharesAccount account = m_bitshares.WalletGetAccount(l.from_account);

						string btcAddress = BitsharesAccountToBitcoinAddress(account);

						if (btcAddress != null)
						{
							SendBitcoinsToDepositor(btcAddress, t, l);					
						}
						else
						{
							// were unable to get a bitcoin address from the bitshares account, so refund the transaction
							RefundBitsharesDeposit(account, l, t.trx_id);
						}
					}
				}
				else
				{
					// fail with unhandled case
					throw new UnsupportedTransactionException(t);
				}
			}

			UpdateBitsharesBlock(info.blockchain_head_block_num);
		}

		protected abstract string GetLastBitcoinBlockHash();
		protected abstract void UpdateBitcoinBlockHash(string lastBlock);
		protected abstract bool HasBitcoinDespoitBeenCredited(string txid);
		protected abstract void MarkBitcoinDespositAsCredited(string bitcoinTxid, string bitsharesTrxId, decimal amount);
		protected virtual bool BitcoinAddressIsDepositAddress(string bitcoinAddress)
		{
			return bitcoinAddress == m_bitcoinDespoitAddress;
		}

		/// <summary>	Take the given transaction, pull out the first input and get the public key, 
		/// 			turn that into a bitshares address </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	The bitshares address from bitcoin deposit. </returns>
		protected virtual string GetBitsharesAddressFromBitcoinDeposit(TransactionSinceBlock t)
		{
			DecodedRawTransaction rawDeposit = m_bitcoin.GetRawTransaction(t.TxId, 1);

			IEnumerable<string> allPubKeys = rawDeposit.VIn.Select(vin => vin.ScriptSig.Asm.Split(' ')[1]);

			if (allPubKeys.Distinct().Count() > 1)
			{
				// can't handle more than one sender case
				throw new MultiplePublicKeysException(rawDeposit);
			}

			string publicKey = allPubKeys.First();
			BitsharesPubKey btsPk = BitsharesPubKey.FromBitcoinHex(publicKey, m_addressByteType);
			return btsPk.m_Address;
		}

		/// <summary>	Compute and send the correct amount of bitassets to the depositor
		/// 			given the deposit transaction </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="t">	The TransactionSinceBlock to process. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		protected virtual BitsharesTransactionResponse SendBitAssetsToDepositor(TransactionSinceBlock t)
		{
			string bitsharesAddress = GetBitsharesAddressFromBitcoinDeposit(t);

			// send the bitAssets!
			BitsharesTransactionResponse bitsharesTrx = m_bitshares.WalletTransferToAddress(t.Amount, m_asset.symbol, m_bitsharesAccount, bitsharesAddress, t.TxId);

			MarkBitcoinDespositAsCredited(t.TxId, bitsharesTrx.record_id, t.Amount);

			return bitsharesTrx;
		}

		/// <summary>	Handles the bitcoin deposits. </summary>
		///
		/// <remarks>	Paul, 23/12/2014. </remarks>
		void HandleBitcoinDeposits()
		{
			string lastBlockHash = GetLastBitcoinBlockHash();
			if (lastBlockHash == null)
			{
				lastBlockHash = m_bitcoin.GetBlockHash(0);
			}

			long blockHeight = m_bitcoin.GetBlockCount();
			string latestBlockHash = m_bitcoin.GetBlockHash(blockHeight);

			// get all transactions of category 'receive'
			IEnumerable<TransactionSinceBlock> transactions = m_bitcoin.ListSinceBlock(lastBlockHash, kBitcoinConfirms).transactions.Where(t => t.Category == TransactionCategory.receive && t.Confirmations >= kBitcoinConfirms);

			foreach (TransactionSinceBlock t in transactions)
			{
				if (BitcoinAddressIsDepositAddress(t.Address))
				{
					// this is a confirmed bitcoin transaction
					//
					// make sure it hasn't already been credited
					if (!HasBitcoinDespoitBeenCredited(t.TxId) && !IsTransactionIgnored(t.TxId))
					{
						SendBitAssetsToDepositor(t);
					}
				}
			}

			UpdateBitcoinBlockHash(latestBlockHash);
		}

		/// <summary>	Joins the damon thread </summary>
		///
		/// <remarks>	Paul, 16/12/2014. </remarks>
		public void Join()
		{
			while (true)
			{
				//
				// handle bitshares->bitcoin
				//
				
				HandleBitsharesDesposits();	
				
				//
				// handle bitcoin->bitshares
				// 

				HandleBitcoinDeposits();
				

				Thread.Sleep(kSleepTimeSeconds*1000);
			}
		}
	}
}
