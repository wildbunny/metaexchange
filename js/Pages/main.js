var countryApp = angular.module('myApp', []);

/**
	@ngInject
*/
var controlFunc = function ($scope, $http, $timeout)
{
	var get = function()
	{
		$http.get("/getStats").success(function (data)
		{
			$scope.stats = data.m_stats;

			var prettyTrans = [];
			for (var i = 0; i < data.m_lastTransactions.length; i++)
			{
				var t = data.m_lastTransactions[i];
				var o = {};
				var bitAsset = "bit" + t.asset;
				if (t.type=='bitcoinDeposit')
				{
					o.from = "BTC";
					o.to = bitAsset;
				}
				else
				{
					o.from = bitAsset;
					o.to = "BTC";
				}

				o.amount = t.amount;
				o.date = t.date;

				prettyTrans.push(o);
			}

			$scope.transactions = prettyTrans;
			var delta = new Date().getTime() - new Date(data.m_stats.last_update).getTime();
			if (delta < 30000)
			{
				$scope.status = "Green";
				$scope.label = "success";
			}
			else
			{
				$scope.status = "Down";
				$scope.label = "danger";
			}
		});
	}
	var poll = function ()
	{
		$timeout(function ()
		{
			get();

			poll();
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
	if (data.m_errorMsg != undefined)
	{
		$('#bitsharesErrorId').show();
		$('#bitsharesErrorId').text(data.m_errorMsg);
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
	if (data.m_errorMsg != undefined)
	{
		$('#bitcoinErrorId').show();
		$('#bitcoinErrorId').text(data.m_errorMsg);
		$('.unhideBtcId').hide();
	}
	else
	{
		$('#bitcoinErrorId').hide();
		$('#bitsharesMemoId').val(data.memo);
		$('#bitsharesDespositAccountId').val(data.deposit_address);
		$('.unhideBtcId').show();
		$('#bitsharesMemoId').popover({ html: true, trigger: "hover", content: "Make sure to include this memo in the transaction otherwise your deposit wont credit.", placement: "auto" });
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
	var memo = $('#bitsharesMemoId').val();
	var toAccount = $('#bitsharesDespositAccountId').val();

	var url = "bts:" + fromAccount+"/transfer/amount/"+amount+"/memo/"+memo+"/"+toAccount+"/asset/BTC";

	$('#bitsharesLinkId').attr("href", url);
	$('#bitsharesLinkId').text(url);
}