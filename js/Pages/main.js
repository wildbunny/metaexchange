var countryApp = angular.module('myApp', []).config([
    '$compileProvider',
    function ($compileProvider)
    {
    	$compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|bts):/);
    }
]);




/**
	@ngInject
*/
var controlFunc = function ($scope, $http, $timeout)
{
	$scope.sell = {};
	$scope.renameSymbolPair = renameSymbolPair;

	var get = function()
	{
		return PostForm($http, "/api/1/getMarket", { symbol_pair: $('#symbolPairId').val() }, function (data)
		{
			if (!data.up)
			{
				$scope.status = "Down";
				$scope.label = "warning";
			}
			else
			{
				$scope.status = "Online";
				$scope.label = "info";
			}
						
			data.base_symbol = data.symbol_pair.split('_')[0];
			data.quote_symbol = data.symbol_pair.split('_')[1];
			var qa, qb;
			if (data.flipped)
			{
				var tmp = data.base_symbol;
				data.base_symbol = data.quote_symbol;
				data.quote_symbol = tmp;
			}

			$scope.market = data;
		}).finally(function (response)
		{
			PostForm($http, "/api/1/getLastTransactions", { limit: 6, symbol_pair: $('#symbolPairId').val() }, function (data)
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
	$('#gtxAccountId').bind("input", CreateLink);

	$('.submitOnBlur').blur(function ()
	{
		if ($(this).closest('form').valid())
		{
			$(this).closest('form').submit();
		}
		else
		{
			if ($(this).attr("id") == "bitsharesBlurId")
			{
				$('.unhideBtsId').hide();
			}
			if ($(this).attr("id") == "bitcoinBlurId")
			{
				$('.unhideBtcId').hide();
			}
		}
	});
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

function GenerateQrModal()
{
	$('#qrModalId').find("img").attr("src", "https://blockchain.info/qr?data=" + $('#bitcoinDespositId').val() + "&size=200");
	$('#qrModalId').modal();
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

function UpdateAssetDetails(data)
{
	data = JSON.parse(data.substr(5));

	$('#assetSymbolId').text(data.symbol);
	$('#assetNameId').text(data.name);
	$('#assetDescriptionId').text(data.description);

	var s = parseFloat(data.current_share_supply) / parseFloat(data.precision);
	var m = parseFloat(data.maximum_share_supply) / parseFloat(data.precision);

	$('#assetSupplyId').text(s + " / " + m);
}