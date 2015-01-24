function ResizeCanvas()
{
	fitToContainer(document.getElementById('candlestickChartId'));
}

function Refresh()
{
	ResizeCanvas();
	CanvasChart.Refresh();
}

function OnChartTimeframe(data)
{
	var numDps = GetMarketDetails().quoteAsset.m_dps;

	var dataDef = {
		dataPointFont: '8pt Lato',
		pixelsPerBar: 10,
		candleStrokeBear: '#b94a48',
		candleFillBear: '#e74c3c',
		candleStrokeBull: '#468847',
		candleFillBull: '#18bc9c',
		timeFrameSeconds: (ParseDate(data[1].date).getTime() - ParseDate(data[0].date).getTime())/1000,
		dataPoints: data,
		decimalPlaces: numDps,
		gridColour: '#d0d0d0'
	};

	CanvasChart.Reset();
	CanvasChart.render('candlestickChartId', dataDef);
}

function OnLoad()
{
	var market = $('meta[name=market]').attr("content");

	ResizeCanvas();	

	$.post("/getOhlc", { market: market, start: new Date().toISOString(), timeframe: "H1", bars: 100 }).done(function (data)
	{
		OnChartTimeframe(data);

		window.addEventListener('resize', Refresh, false);
	});

	$("input[name=timeframe][value=" + "H1" + "]").prop('checked', true);
	$("input[name=timeframe][value=" + "H1" + "]").parent().addClass('active');

	$('input[type=radio]').change(function ()
	{
		$(this).closest("form").submit();
	});
}

function TruncateNumberAsString(number)
{
	return number.match(/^[-\d]+(?:\.\d{0,8})?/);
}

function NumberToString(number)
{
	if (typeof (number) == "string")
	{
		number = parseFloat(number);
	}

	number = number.toFixed(8).toString();

	return number;
}

function FormatPrice(price)
{
	return TruncateNumberAsString(NumberToString(price));
}

function fitToContainer(canvas)
{
	// Make it visually fill the positioned parent
	canvas.style.width = '100%';
	canvas.style.height = '100%';
	// ...then set the internal size to match
	canvas.width = canvas.offsetWidth;
	canvas.height = canvas.offsetHeight;
}

Date.prototype.format = function (integer)
{
	return (integer < 10) ? "0" + integer : integer.toString();
}

Date.prototype.timeNow = function (includeSeconds, hideNonTime)
{
	var m_shortMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
	var seconds = (includeSeconds != undefined) ? ":" + this.format(this.getSeconds()) : "";
	var nonTime = this.getDate() + " " + m_shortMonths[this.getMonth()] + " ";
	if (hideNonTime == true)
	{
		nonTime = "";
	}
	return nonTime + this.format(this.getHours()) + ":" + this.format(this.getMinutes()) + seconds;
};

function ParseDate(input)
{
	return new Date(input);
	var parts = input.split('-');
	var dayAndTime = parts[2].split(' ');
	var time = dayAndTime[1].split(':');

	// new Date(year, month [, date [, hours[, minutes[, seconds[, ms]]]]])
	return new Date(parts[0], parts[1] - 1, dayAndTime[0], time[0], time[1], time[2]); // months are 0-based
}