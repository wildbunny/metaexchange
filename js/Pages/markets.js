var countryApp = angular.module('myApp', []);

/**
	@ngInject
*/
var controlFunc = function ($scope, $http, $timeout)
{
	$scope.go = function(m)
	{
		window.document.location = "/markets/" + $scope.renameSymbolPair(m.symbol_pair);
	};

	$scope.renameSymbolPair = renameSymbolPair;

	var get = function ()
	{
		return $http.get("/api/1/getAllMarkets").success(function (data)
		{
			$scope.allMarkets = data;
		}).finally(function (response)
		{
			PostForm($http, "/api/1/getLastTransactions", { limit: 6 }, function (data)
			{
				$scope.transactions = data;
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

countryApp.controller('MarketsController', controlFunc);