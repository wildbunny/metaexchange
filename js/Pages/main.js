var countryApp = angular.module('myApp', []).config([
    '$compileProvider',
    function ($compileProvider)
    {
    	$compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|bts):/);
    }
]);


function PostForm($http, url, paramsObj, onSuccess)
{
	return $http({
		method: 'POST',
		url: url,
		data: $.param(paramsObj),
		headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
	}).success(onSuccess);
}

/**
	@ngInject
*/
var controlFunc = function ($scope, $http, $timeout)
{
	$scope.sell = {};

	var get = function()
	{
		return PostForm($http, "/api/1/getMarket", { symbol_pair: $('#symbolPairId').val() }, function (data)
		{
			if (!data.up)
			{
				$scope.status = "Down";
				$scope.label = "danger";
			}
			else
			{
				$scope.status = "Green";
				$scope.label = "success";
			}
						
			data.base_symbol = data.symbol_pair.split('_')[0];
			data.quote_symbol = data.symbol_pair.split('_')[1];
			var qa, qb;
			if (data.base_symbol == "BTC")
			{
				var tmp = data.base_symbol;
				data.base_symbol = data.quote_symbol;
				data.quote_symbol = tmp;

				qa = data.ask;
				qb = 1 / data.bid;
			}
			else
			{
				qa = 1 / data.ask;
				qb = data.bid;
			}

			var buyFee = (qa * data.ask_fee_percent / 100);
			var sellFee = (qb * data.bid_fee_percent / 100);

			data.buy_quantity = qa - buyFee;
			data.sell_quantity = qb - sellFee;
			
			$scope.market = data;
		}).finally(function (response)
		{
			PostForm($http, "/api/1/getLastTransactions", { limit: 6 }, function (data)
			{
				$scope.transactions = data;
			}).finally(function(reponse)
			{
				var memo = $('#bitsharesMemoId').val();
				var depositAddress = $('#bitcoinDespositId').val();

				if (memo.length > 0 || depositAddress.length > 0)
				{
					PostForm($http, "/api/1/getMyLastTransactions", { limit: 6, memo: memo, deposit_address: depositAddress }, function (data)
					{
						$scope.myTransactions = data;
					});
				}
			});
		});
	}
	var poll = function ()
	{
		$timeout(function ()
		{
			get().finally(function (response)
			{
				poll();
			});
		}, 5000);
	};

	get();
	poll();
};

countryApp.controller('StatsController', controlFunc);

function OnLoad()
{
	$(document).on('click', 'input[type=text]', function () { this.select(); });

	$('.unhideBtcId').hide();
	$('.unhideBtsId').hide();

	$('#gtxAmountId').bind("input",CreateLink);
	$('#gtxAccountId').bind("input",CreateLink);
}

function OnSubmitAddressBts(data)
{
	if (data.message != undefined)
	{
		$('#bitsharesErrorId').show();
		$('#bitsharesErrorId').text(data.message);
		$('.unhideBtsId').hide();
	}
	else
	{
		$('#bitsharesErrorId').hide();
		$('#bitcoinDespositId').val(data.deposit_address);
		$('.unhideBtsId').show();
	}
}

function OnSubmitAddressBtc(data)
{
	if (data.message != undefined)
	{
		$('#bitcoinErrorId').show();
		$('#bitcoinErrorId').text(data.message);
		$('.unhideBtcId').hide();
	}
	else
	{
		$('#bitcoinErrorId').hide();
		//$('#bitsharesMemoId').val(data.memo);
		//$('#bitsharesDespositAccountId').val(data.deposit_address);
		$('.unhideBtcId').show();
		$('#bitsharesMemoId').popover({ html: true, trigger: "hover", content: "Make sure to include this memo in the transaction otherwise your deposit wont credit.", placement: "auto" });

		var scope = angular.element($("#rootId")).scope();
		scope.$apply(function ()
		{
			scope.sell.memo = data.memo;
			scope.sell.sendToAccount = data.deposit_address;
		});
	}
}

function GenerateTransactionModal()
{
	$('#bitsharesModalId').modal();
}

function CreateLink()
{
	var fromAccount = $('#gtxAccountId').val();
	var amount = $('#gtxAmountId').val();

	if (fromAccount.length > 0 && amount > 0)
	{
		$('#bitsharesLinkId').show();
	}
	else
	{
		$('#bitsharesLinkId').hide();
	}	
}