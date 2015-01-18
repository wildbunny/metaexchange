using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using RestLib;
using ServiceStack.Text;
using BitsharesRpc;

namespace BitsharesRpc
{
	public class BitsharesRpcException : Exception
	{
		BitsharesError m_error;

		public BitsharesRpcException(BitsharesError error)
		{
			m_error = error;
		}

		public override string Message { get { return m_error.message; } }
	}

    public class BitsharesWallet
    {
		const int kBitsharesMaxMemoLength = 19;

		string m_rpcUrl;
		string m_rpcUsername;
		string m_rpcPassword;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 26/11/2014. </remarks>
		///
		/// <param name="rpcUrl">	  	Bitshares RPC root url. </param>
		/// <param name="rpcUsername">	The RPC username. </param>
		/// <param name="rpcPassword">	The RPC password. </param>
		public BitsharesWallet(string rpcUrl, string rpcUsername, string rpcPassword)
		{
			m_rpcUrl = rpcUrl;
			m_rpcUsername = rpcUsername;
			m_rpcPassword = rpcPassword;

			// configure servicestack.text to be able to parse the bitshares rpc responses
			JsConfig<DateTime>.DeSerializeFn = BitsharesDatetimeExtensions.ParseDateTime;
			JsConfig<decimal>.DeSerializeFn = s => decimal.Parse(s, NumberStyles.Float);
			JsConfig.DateHandler = JsonDateHandler.ISO8601;
			JsConfig.IncludeTypeInfo = false;
			JsConfig.IncludePublicFields = true;
			JsConfig.IncludeNullValues = true;
		}

		/// <summary>	Makes raw syncronus request </summary>
		///
		/// <remarks>	Paul, 26/11/2014. </remarks>
		///
		/// <typeparam name="T">	Request packet </typeparam>
		/// <param name="request">	. </param>
		///
		/// <returns>	A BitsharesResponse&lt;T&gt; </returns>
		public BitsharesResponse<T> MakeRawRequestSync<T>(BitsharesRequest request)
		{
			string result = Rest.ExecutePostSync(m_rpcUrl, JsonSerializer.SerializeToString(request), Rest.kContentTypeJson, m_rpcUsername, m_rpcPassword);
			BitsharesResponse<T> response = JsonSerializer.DeserializeFromString<BitsharesResponse<T>>(result);
			
			if (response.result == null)
			{
				// an error may have occured here
				BitsharesErrorResponse error = JsonSerializer.DeserializeFromString<BitsharesErrorResponse>(result);
				if (error.error != null)
				{
					throw new BitsharesRpcException(error.error);
				}
			}

			return response;
		}

		public List<BitsharesResponse<T>> MakeRawBatchRequestSync<T>(BitsharesRequest request)
		{
			return Rest.JsonApiCallSync<List<BitsharesResponse<T>>>(m_rpcUrl, JsonSerializer.SerializeToString(request),
																	m_rpcUsername, m_rpcPassword);
		}

		/// <summary>	Makes syncronus request </summary>
		///
		/// <remarks>	Paul, 26/11/2014. </remarks>
		///
		/// <typeparam name="T">	Request packet. </typeparam>
		/// <param name="request">	. </param>
		///
		/// <returns>	A T. </returns>
		public T MakeRequestSync<T>(BitsharesRequest request)
		{
			BitsharesResponse<T> response = MakeRawRequestSync<T>(request);
			return response.result;
		}

		public List<BitsharesResponse<T>> MakeBatchRequestSync<T>(BitsharesBatchRequest request)
		{
			List<BitsharesResponse<T>> responses = MakeRawBatchRequestSync<T>(request);
			return responses;
		}

		/// <summary>	Make a syncronus API post </summary>
		///
		/// <remarks>	Paul, 27/11/2014. </remarks>
		///
		/// <typeparam name="T">	Generic type parameter. </typeparam>
		/// <param name="method">	The method. </param>
		/// <param name="args">  	A variable-length parameters list containing arguments. </param>
		///
		/// <returns>	A T. </returns>
		public T ApiPostSync<T>(BitsharesMethods method, params object[] args)
		{
			return MakeRequestSync<T>(new BitsharesRequest(method, args));
		}

		/// <summary>	get_info command </summary>
		///
		/// <remarks>	Paul, 27/11/2014. </remarks>
		///
		/// <returns>	The information. </returns>
		public GetInfoResponse GetInfo()
		{
			return ApiPostSync<GetInfoResponse>(BitsharesMethods.get_info);
		}

		/// <summary>	Wallet get account. </summary>
		///
		/// <remarks>	Paul, 10/12/2014. </remarks>
		///
		/// <param name="accountName">	name of the account. </param>
		///
		/// <returns>	A BitsharesAccount. </returns>
		public BitsharesAccount WalletGetAccount(string accountName)
		{
			return ApiPostSync<BitsharesAccount>(BitsharesMethods.wallet_get_account, accountName);
		}

		/// <summary>	Wallet account transaction history. </summary>
		///
		/// <remarks>	Paul, 27/11/2014. </remarks>
		///
		/// <param name="accountName">	(Optional) name of the account. </param>
		/// <param name="assetSymbol">	(Optional) the asset symbol. </param>
		/// <param name="limit">	  	(Optional) the limit. </param>
		///
		/// <returns>	A List&lt;BitsharesTransaction&gt; </returns>
		public List<BitsharesWalletTransaction> WalletAccountTransactionHistory(string accountName=null, 
																				string assetSymbol=null, 
																				int limit=0,
																				uint startBlock=0,
																				uint endBlock=uint.MaxValue)
		{
			return	ApiPostSync<List<BitsharesWalletTransaction>>
					(
						BitsharesMethods.wallet_account_transaction_history, 
						accountName, 
						assetSymbol, 
						limit,
						startBlock,
						endBlock
					);
		}

		/// <summary>	Get one single transaction </summary>
		///
		/// <remarks>	Paul, 02/12/2014. </remarks>
		///
		/// <param name="txid">	The txid. </param>
		///
		/// <returns>	A BitsharesTransaction. </returns>
		public BitsharesTransaction BlockchainGetTransaction(string txid)
		{
			return ApiPostSync<Dictionary<string,BitsharesTransaction>>(BitsharesMethods.blockchain_get_transaction, txid).First().Value;
		}

		/// <summary>	Gets a balance. </summary>
		///
		/// <remarks>	Paul, 15/01/2015. </remarks>
		///
		/// <param name="balanceId">	Identifier for the balance. </param>
		///
		/// <returns>	The balance. </returns>
		public BitsharesBalanceRecord GetBalance(string balanceId)
		{
			return ApiPostSync<BitsharesBalanceRecord>(BitsharesMethods.get_balance, balanceId);
		}

		/// <summary>	Get a batch of transactions </summary>
		///
		/// <remarks>	Paul, 10/12/2014. </remarks>
		///
		/// <param name="txids">	The txids. </param>
		///
		/// <returns>	A List&lt;BitsharesTransaction&gt; </returns>
		public List<BitsharesTransaction> BlockchainGetTransactionBatch(IEnumerable<string> txids)
		{
			// convert into array of parameters
			IEnumerable<object[]> p = txids.Select<string, object[]>(s=>new object[] {s});

			return MakeRequestSync<List<BitsharesTransaction>>(new BitsharesBatchRequest
																(
																	BitsharesMethods.blockchain_get_transaction, 
																	p
																));
		}

		/// <summary>	Get the asset details from the name </summary>
		///
		/// <remarks>	Paul, 11/12/2014. </remarks>
		///
		/// <param name="name">	The name. </param>
		///
		/// <returns>	A Bitshares Asset. </returns>
		public BitsharesAsset BlockchainGetAsset(string name)
		{
			return ApiPostSync<BitsharesAsset>(BitsharesMethods.blockchain_get_asset, name);
		}

		/// <summary>	Trucate memo. </summary>
		///
		/// <remarks>	Paul, 18/01/2015. </remarks>
		///
		/// <param name="memo">	the memo. </param>
		///
		/// <returns>	A string. </returns>
		string TrucateMemo(string memo)
		{
			if (memo.Length > kBitsharesMaxMemoLength)
			{
				memo = memo.Substring(0, kBitsharesMaxMemoLength);
			}
			return memo;
		}

		/// <summary>	Wallet transfer. </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="amount">	  	The amount. </param>
		/// <param name="asset">	  	The asset. </param>
		/// <param name="fromAccount">	from account. </param>
		/// <param name="toAccount">  	to account. </param>
		/// <param name="memo">		  	(Optional) the memo. </param>
		/// <param name="voteMethod"> 	(Optional) the vote method. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		public BitsharesTransactionResponse WalletTransfer(	decimal amount, string asset, 
															string fromAccount,	string toAccount, 
															string memo="", 
															VoteMethod voteMethod = VoteMethod.vote_recommended)
		{
			memo = TrucateMemo(memo);

			return ApiPostSync<BitsharesTransactionResponse>(	BitsharesMethods.wallet_transfer, amount, asset, 
																fromAccount,
																toAccount,
																memo,
																voteMethod);
		}

		/// <summary>	Wallet transfer to address. </summary>
		///
		/// <remarks>	Paul, 22/12/2014. </remarks>
		///
		/// <param name="amount">	  	The amount. </param>
		/// <param name="asset">	  	The asset. </param>
		/// <param name="fromAccount">	from account. </param>
		/// <param name="toAddress">  	to address. </param>
		/// <param name="memo">		  	(Optional) the memo. </param>
		/// <param name="voteMethod"> 	(Optional) the vote method. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		public BitsharesTransactionResponse WalletTransferToAddress(decimal amount, string asset,
																	string fromAccount, string toAddress,
																	string memo = "",
																	VoteMethod voteMethod = VoteMethod.vote_recommended)
		{
			memo = TrucateMemo(memo);

			return ApiPostSync<BitsharesTransactionResponse>(	BitsharesMethods.wallet_transfer_to_address, amount, asset,
																fromAccount,
																toAddress,
																memo,
																voteMethod);
		}

		/// <summary>	Wallet issue asset. </summary>
		///
		/// <remarks>	Paul, 17/01/2015. </remarks>
		///
		/// <param name="amount">   	The amount. </param>
		/// <param name="symbol">   	The symbol. </param>
		/// <param name="toAccount">	to account. </param>
		/// <param name="memo">			(Optional) the memo. </param>
		///
		/// <returns>	A BitsharesTransactionResponse. </returns>
		public BitsharesTransactionResponse WalletIssueAsset(decimal amount, string symbol, string toAccount, string memo="")
		{
			memo = TrucateMemo(memo);

			return ApiPostSync<BitsharesTransactionResponse>(BitsharesMethods.wallet_asset_issue, amount, symbol, toAccount, memo);
		}
    }
}
