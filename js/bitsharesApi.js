var BitShares =
{
	Api: function ($http, method, params, success)
	{
		//$.post("/rpc", JSON.stringify({ jsonrpc: "2.0", id: 1, method: method, params: params })).done(success);

		$http.post("/rpc", JSON.stringify({ jsonrpc: "2.0", id: 1, method: method, params: params })).success(success);
	},
	GetValidMarkets:function(success)
	{
		this.Api("blockchain_list_assets", [], function (data)
		{
			for (var i=0; i<data.result.length; i++)
			{
				console.log(data.result[i].symbol);
			}
		});
	}
};
