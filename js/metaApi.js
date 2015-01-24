var countryApp = angular.module('BitShares', ['link']);
var gPageConstants =
{
	kPostHeaders: { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
};

function GetMarketDetails()
{
	var market = $('meta[name=market]').attr("content");
	var baseAsset =  JSON.parse( decodeURIComponent( $('meta[name=baseAsset]').attr("content") ) );
	var quoteAsset = JSON.parse(decodeURIComponent( $('meta[name=quoteAsset]').attr("content") ) );

	return { market: market, baseAsset: baseAsset, quoteAsset: quoteAsset };
}

countryApp.controller('GetMarkets', function ($scope, $http)
{
	$http.post("/getMarkets").success(function (data)
	{
		$scope.results = data;
	});
});

countryApp.controller('GetTrades', function ($scope, $http)
{
	var market = GetMarketDetails();

	$http.post("/getTrades", $.param({ market: market.market }), gPageConstants.kPostHeaders).success(function (data)
	{
		$scope.dpBase = market.baseAsset.m_dps;
		$scope.dpQuote = market.quoteAsset.m_dps;
		$scope.results = data;
	});
});

countryApp.controller('GetOrderbook', function ($scope, $http)
{
	var market = GetMarketDetails();

	$http.post("/getOrderbook", $.param({ market: market.market }), gPageConstants.kPostHeaders).success(function (data)
	{
		$scope.dpBase = market.baseAsset.m_dps;
		$scope.dpQuote = market.quoteAsset.m_dps;
		$scope.results = data;
	});

	$scope.getFraction = function(index, arrayInQuestion, ob)
	{
		var t = $scope.getTotal(ob.m_asks) + $scope.getTotal(ob.m_bids);
		var a = $scope.getAccumulated(index, arrayInQuestion);
		var r = ((100 * a) / t).toFixed(0);

		/*if (arrayInQuestion == ob.m_asks)
		{
			console.log(index + ") t=" + t + " a=" + a + " r="+r);
		}*/

		return r
	}

	$scope.getTotal = function (array)
	{
		var acc = 0
		for (var i = 0; i < array.length; i++)
		{
			acc += array[i].m_volume;
		}
		return acc
	}

	$scope.getAccumulated = function (index, array)
	{
		var acc = 0
		for (var i = 0; i <= index; i++)
		{
			acc += array[i].m_volume;
		}
		return acc
	}
});
