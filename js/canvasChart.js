var CanvasChart = function () 
{
    var ctx;
    var margin = {top: 40, left: 75, right: 0, bottom: 75};
    var chartHeight, chartWidth, yMax, xMax, data;
    var maxYValue = 0;
	var m_minYValue = Number.MAX_VALUE;
	var m_dataRange;
    var ratio = 0;
    var renderType = {lines: 'lines', points: 'points', candles: 'candles'};
	var m_ohlcData;
	var m_numBars;
	var m_marginTop, m_marginLeft;
	var m_bottomBorder = 15;
	var m_timeFrameMillis;
	var m_canvasId;
	

    var render = function(canvasId, dataObj) 
	{
    	data = dataObj;

    	if (data.gridColour == null)
    	{
    		data.gridColour = '#a0a0a0';
    	}

    	m_canvasId = canvasId;
        var canvas = document.getElementById(canvasId);
        chartHeight = canvas.getAttribute('height');
        chartWidth = canvas.getAttribute('width');
        xMax = chartWidth - (margin.left + margin.right);
        yMax = chartHeight - m_bottomBorder;// - (m_marginTop + margin.bottom);
		
        ctx = canvas.getContext("2d");
        ctx.save();
        ctx.translate(0.5, 0.5);
		
		if (data.candleFillBear == undefined)
		{
			data.candleFillBear = 'Black';
		}
		if (data.candleFillBull == undefined)
		{
			data.candleFillBull = 'White';
		}
		if (data.candleStrokeBear == undefined)
		{
			data.candleStrokeBear = 'Black';
		}
		if (data.candleStrokeBull == undefined)
		{
			data.candleStrokeBull = 'Black';
		}
		if (data.timeFrameSeconds != undefined)
		{
			m_timeFrameMillis = data.timeFrameSeconds*1000;
		}
		else
		{
			m_timeFrameMillis = 60*60*1000;
		}
		
		m_marginTop = 0;
		m_marginLeft = 0;

		
		
		ctx.Stipple = function(startX, startY, endX, endY, colour, dotWidth, spaceBetweenDots, lineWidth )
		{
			ctx.strokeStyle = colour;
			if (lineWidth == undefined) lineWidth=1;
			ctx.lineWidth = lineWidth;
			
			var deltaX = endX - startX;
			var deltaY = endY - startY;
			var totalLen = Math.sqrt(deltaX*deltaX + deltaY*deltaY);
			var unitDeltaX = deltaX / totalLen;
			var unitDeltaY = deltaY / totalLen;
						
			var gapX = unitDeltaX * spaceBetweenDots;
			var gapY = unitDeltaY * spaceBetweenDots;
								
			var pX = startX;
			var pY = startY;
			var lenToGo = totalLen;
			
			this.beginPath();
			
			while (lenToGo > 0)
			{
				var segWidth = Math.min(dotWidth, lenToGo);
				var lineSegX = unitDeltaX * segWidth;
				var lineSegY = unitDeltaY * segWidth;
				
				this.moveTo(parseInt(pX), parseInt(pY));
				pX += lineSegX;
				pY += lineSegY;
				this.lineTo(parseInt(pX), parseInt(pY));
				
				pX += gapX;
				pY += gapY;
				
				lenToGo -= segWidth + spaceBetweenDots;
			}
			
			this.stroke();
			this.closePath();
		}
				
		m_numBars = Math.floor(xMax / getXInc());
		//m_ohlcData = BuildOhlcFromTrades(data.dataPoints, m_numBars);
		m_ohlcData = ParseDates(dataObj.dataPoints);
		
		renderChart();

		ctx.restore();
    };
	
	/**
	 * 
	 */
	var ParseDates = function(ohlcvData)
	{
		for (var i=0; i<ohlcvData.length; i++)
		{
			if (typeof ohlcvData[i].date != 'number')
			{
				ohlcvData[i].date = ParseDate(ohlcvData[i].date).getTime();
			}
		}
		
		return ohlcvData;
	}

    var renderChart = function () 
	{
		GetDataExtents();
		m_dataRange = maxYValue - m_minYValue;
        ratio = yMax / m_dataRange;
		
        //renderBackground();
        //renderText();
        renderLinesAndLabels();

        //render data based upon type of renderType(s) that client supplies
        if (data.renderTypes == undefined || data.renderTypes == null) data.renderTypes = [renderType.lines];
        for (var i = 0; i < data.renderTypes.length; i++) 
		{
            renderData(data.renderTypes[i]);
        }
    };

    var GetDataExtents = function () 
	{
		m_minYValue = Number.MAX_VALUE;
		maxYValue = 0;
		
        for (var i = 0; i < m_ohlcData.length; i++) 
		{
			m_minYValue = Math.min(m_ohlcData[i].low, m_minYValue);
			maxYValue = Math.max(m_ohlcData[i].high, maxYValue);
        }
		
		// add on some margin
		/*var marginPrice = data.marginPrice/2;
		var marginMin = Math.floor( (m_minYValue - marginPrice) / marginPrice) * marginPrice;
		var marginMax = Math.ceil( (maxYValue + marginPrice) / marginPrice) * marginPrice;
		
		m_minYValue = Math.min(marginMin, m_minYValue);
		maxYValue = Math.max(marginMax, maxYValue);*/
    };

    var renderText = function() 
	{
        var labelFont = (data.labelFont != null) ? data.labelFont : '20pt Arial';
        ctx.font = labelFont;
        ctx.textAlign = "center";

        //Title
        var txtSize = ctx.measureText(data.title);
        ctx.fillText(data.title, (chartWidth / 2), (m_marginTop / 2));

        //X-axis text
        txtSize = ctx.measureText(data.xLabel);
        ctx.fillText(data.xLabel, m_marginLeft + (xMax / 2) - (txtSize.width / 2), yMax + (margin.bottom / 1.2));

        //Y-axis text
        ctx.save();
        ctx.rotate(-Math.PI / 2);
        ctx.font = labelFont;
        ctx.fillText(data.yLabel, parseInt((yMax / 2) * -1), parseInt(m_marginLeft / 4));
        ctx.restore();
    };
	
	

    var renderLinesAndLabels = function () 
	{
        //Vertical guide lines
		if (m_ohlcData.length > 0)
		{
			var yInc = yMax / m_numBars;
			var yPos = 0;
			//var yLabelInc = (m_dataRange * ratio) / m_ohlcData.length;
			var xInc = getXInc();

			var txt = new Date().timeNow();
			var txtSizeX = ctx.measureText(txt).width + 5;
			var txtSizeY = ctx.measureText("m").width + 20;
			var lastTextX = Number.MAX_VALUE, lastTextY = 0;
			var barTime = m_ohlcData[m_ohlcData.length-1].date;

			for (var i = 0; i < m_numBars; i++) 
			{
				yPos += (i == 0) ? m_marginTop : yInc;

				//y axis labels
				ctx.font = (data.dataPointFont != null) ? data.dataPointFont : '10pt Calibri';
				//txt = FormatPrice(maxYValue - ((i == 0) ? 0 : yPos / ratio));

				//txt = FormatPrice(PixelsToPrice(yPos));
				txt = PixelsToPrice(yPos).toFixed(data.decimalPlaces);
				txtSize = ctx.measureText(txt);

				if (yPos - lastTextY > txtSizeY && yPos < yMax && yPos > m_marginTop)
				{
					//ctx.fillText(txt, m_marginLeft - ((txtSize.width >= 14) ? txtSize.width : 10) - 7, yPos + 4);
					ctx.textAlign = 'left';
					ctx.fillText(txt, xMax+7, yPos + 4);
					lastTextY = yPos;

					//Draw horizontal lines
					ctx.Stipple(m_marginLeft, yPos, xMax, yPos, data.gridColour, 5, 5);
				}

				//x axis labels
				var d = new Date(barTime);
				txt = d.timeNow();
				txtSize = ctx.measureText(txt);

				var xPos = xMax - (i * xInc);

				if (lastTextX-xPos > txtSizeX && xPos <= xMax && xPos > m_marginLeft)
				{
					ctx.textAlign = 'center';
					ctx.fillStyle = "Black";
					ctx.fillText(txt, xPos, yMax+m_bottomBorder-2);
					lastTextX = xPos;

					//drawLine(xPos, m_marginTop, xPos, yMax, '#E8E8E8');
					ctx.Stipple(xPos, m_marginTop, xPos, yMax, data.gridColour, 5, 5);
				}
				xPos += xInc;
				barTime -= m_timeFrameMillis;
			}

			//Vertical line
			//drawLine(m_marginLeft, m_marginTop, m_marginLeft, yMax, 'black');
			drawLine(m_marginLeft, yMax, xMax, yMax, 'black');
			drawLine(xMax, yMax, xMax, m_marginTop, 'black');
			//drawLine(xMax, m_marginTop, m_marginLeft, m_marginTop, 'black');
		}
    };
	
	var UpdateOhlcFromTrade = function(ohlc, t)
	{
		ohlc.high = Math.max(ohlc.high, t.price);
		ohlc.low = Math.min(ohlc.low, t.price);
		ohlc.close = t.price;
		ohlc.volume += t.volume;
	}
	
	var BuildOhlcFromTrades = function(trades, numBars)
	{
		var ohlcData = [];
				
		// find the start bar time
		if (trades.length > 0)
		{
			var unixDate = ParseDate(trades[0].x).getTime();
			var startBarTime = Math.floor(unixDate / m_timeFrameMillis) * m_timeFrameMillis;
			var currentBarTime = startBarTime;
			var tradeIndex = 0;
			var t = trades[tradeIndex].y;
			var tTime = trades[tradeIndex].x;

			for (var barIndex = 0; barIndex < numBars && tradeIndex < trades.length; barIndex++) 
			{
				// start at open of bar
				var ohlc = {time:currentBarTime, open:t.price, high:t.price, low:t.price, close:t.price, volume:0};
				var endBarTime = currentBarTime+m_timeFrameMillis;

				//console.log("b(" + barIndex + ")=" + new Date(currentBarTime).timeNow() + " -> " + new Date(endBarTime).timeNow());

				for(;tradeIndex < trades.length;)
				{
					var tradeUnixDate = ParseDate(tTime).getTime();
					if (tradeUnixDate < endBarTime)
					{
						// update from trade
						UpdateOhlcFromTrade(ohlc, t);

						tradeIndex++;
						if (tradeIndex < trades.length)
						{
							t = trades[tradeIndex].y;
							tTime = trades[tradeIndex].x;
						}
						else
						{
							// ran out of trades before bar was done, push results so far and then exit
							ohlcData.push( ohlc );
						}
					}
					else
					{
						// store this bar in the data we're building
						ohlcData.push( ohlc );
						break;
					}
				}

				currentBarTime += m_timeFrameMillis;
			}
		}
		
		return ohlcData;
	}
	
	/**
	 */
	var NewOhlc = function(date, price)
	{
		return {date:date, open:price, high:price, low:price, close:price, volume:0};
	}
	
	/**
	 * 
	 */
	var NewBar = function(lastBar, newTime)
	{
		// new bar, so shift old data down by one
		m_ohlcData.shift();

		// create new bar
		var ohlcv = NewOhlc(newTime, lastBar.close);

		// add to data
		m_ohlcData.push(ohlcv);
	}
	
	/**
	 */
	var GetBarTime = function(unixTimeMills)
	{
		return Math.floor(unixTimeMills / m_timeFrameMillis) * m_timeFrameMillis;
	}
	
	/*
	 */
	var GetOhlcFromTrade = function(trade, timeFrameOverride)
	{
		m_timeFrameMillis = timeFrameOverride;
		var tradeBarTime = ParseDate(trade.date).getTime();
		tradeBarTime = GetBarTime(tradeBarTime);
				
		return [NewOhlc(tradeBarTime, trade.price)];
	}
	
	/**
	 * 
	 */
	var UpdateFromTrades = function(trades)
	{
		if (m_ohlcData != undefined && m_ohlcData.length > 0)
		{
			for (var i=0; i<trades.length; i++)
			{
				var t = trades[i];

				// is this new trade in the last bar?
				var latestBarIndex = m_ohlcData.length-1;
				var latestBar = m_ohlcData[latestBarIndex];

				var tradeBarTime = ParseDate(t.date).getTime();
				tradeBarTime = GetBarTime(tradeBarTime);

				if (tradeBarTime == latestBar.date)
				{
					UpdateOhlcFromTrade(latestBar, t);
				}
				else
				{
					NewBar(latestBar, tradeBarTime);
				}
			}

			// re-render chart
			Redraw();
		}
	}
	
	var Clear = function()
	{
		if (ctx)
		{
			ctx.clearRect(0,0,chartWidth,chartHeight);
		}
	}
	
	/**
	 */
	var Redraw = function()
	{
		Clear();
		renderChart();
	}
	
	/**
	 */
	var Reset = function()
	{
		m_ohlcData = null;
		Clear();
	}
	
	/**
	 * 
	 */
	var UpdateFromTick = function(unixTimeMillis)
	{
		if (m_ohlcData != undefined && m_ohlcData.length > 0)
		{
			var latestBarIndex = m_ohlcData.length-1;
			var latestBar = m_ohlcData[latestBarIndex];

			if (unixTimeMillis - latestBar.date > m_timeFrameMillis)
			{
				NewBar(latestBar, GetBarTime(unixTimeMillis));
				
				// re-render chart
				Redraw();
			}
		}
	}

	var PriceToPixels = function(input)
	{
		return parseInt((maxYValue - input) * ratio);
	}
	
	var PixelsToPrice = function(pixelY)
	{
		return -((pixelY / ratio) - maxYValue);
	}
	
	var renderData = function(type) 
	{
		if (m_ohlcData.length > 0)
		{
			var xInc = getXInc();
			var width = parseInt(xInc/2);

			var prevX = 0, prevY = 0;

			ctx.lineWidth = 1;
			
			var firstPrice = m_ohlcData[0].open;
			var firstBar = {open:firstPrice, close:firstPrice, high:firstPrice, low:firstPrice};

			for (var i=m_numBars-1; i>=0; i--)
			{
				var ohlc;
				
				if (i < m_ohlcData.length)
				{
					ohlc = m_ohlcData[m_ohlcData.length-1-i];
				}
				else
				{
					ohlc = firstBar;
				}
				
				var ptX = xMax - (i * xInc);
				var close = PriceToPixels( ohlc.close );
				var open = PriceToPixels( ohlc.open );
				var high = PriceToPixels( ohlc.high );
				var low = PriceToPixels( ohlc.low );
				var top = Math.min(open, close);

				if (ohlc.open > ohlc.close)
				{
					ctx.fillStyle = data.candleFillBear;
					ctx.strokeStyle = data.candleStrokeBear;
				}
				else if (ohlc.open < ohlc.close)
				{
					ctx.fillStyle = data.candleFillBull;
					ctx.strokeStyle = data.candleStrokeBull;
				}
				else
				{
					ctx.fillStyle = ctx.strokeStyle = 'Black';
				}
				drawLine(ptX, low, ptX, high);
				ctx.fillRect( parseInt(ptX-width/2), top, width, Math.abs(open-close));
				ctx.strokeRect(parseInt(ptX-width/2), top, width, Math.abs(open-close));
			}
		}
    };

    var getXInc = function() 
	{
        //return Math.round(xMax / data.dataPoints.length) - 1;
		return data.pixelsPerBar;
    };

    var drawLine = function(startX, startY, endX, endY, strokeStyle, lineWidth) 
	{
        if (strokeStyle != null) ctx.strokeStyle = strokeStyle;
        if (lineWidth != null) ctx.lineWidth = lineWidth;
        ctx.beginPath();
        ctx.moveTo(parseInt(startX), parseInt(startY));
        ctx.lineTo(parseInt(endX), parseInt(endY));
        ctx.stroke();
        ctx.closePath();
    };

    var Refresh = function()
    {
    	render(m_canvasId, data);
    }

    return	{  	renderType: renderType, 
    	Refresh: Refresh,
				render: render, 
				UpdateFromTrades:UpdateFromTrades, 
				UpdateFromTick:UpdateFromTick, 
				Reset:Reset, 
				GetOhlcFromTrade:GetOhlcFromTrade
			};
} ();
