using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BestPrice;
using WebDaemonShared;

namespace BestPrice
{
	public class PriceDiscovery
	{
		GlostenMilgromDesiredSpread m_glosten;
		decimal m_windowRange;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 27/02/2015. </remarks>
		///
		/// <param name="spreadPercent"> 	The spread percent. </param>
		/// <param name="windowPercent"> 	The window percent. </param>
		/// <param name="feedPrice">	 	The feed price. </param>
		/// <param name="inventoryRatio">	The inventory ratio. </param>
		public PriceDiscovery(decimal spreadPercent, decimal windowPercent, decimal feedPrice, decimal inventoryRatio)
		{
			decimal desiredSpread = GetSpreadBtc(feedPrice, spreadPercent);

			m_windowRange = GetSpreadBtc(feedPrice, windowPercent);
			m_glosten = new GlostenMilgromDesiredSpread(desiredSpread, feedPrice - m_windowRange / 2, feedPrice + m_windowRange / 2, inventoryRatio);
		}

		/// <summary>	Gets spread btc. </summary>
		///
		/// <remarks>	Paul, 26/02/2015. </remarks>
		///
		/// <param name="feedPrice">		The feed price. </param>
		/// <param name="spreadPercent">	The spread percent. </param>
		///
		/// <returns>	The spread btc. </returns>
		decimal GetSpreadBtc(decimal feedPrice, decimal spreadPercent)
		{
			return Numeric.TruncateDecimal(feedPrice * spreadPercent / 100, 8);
		}

		/// <summary>	Sets feed price. </summary>
		///
		/// <remarks>	Paul, 24/02/2015. </remarks>
		///
		/// <param name="feedPrice">	The feed price. </param>
		public void SetFeedPrice(decimal feedPrice, out decimal bid, out decimal ask)
		{
			m_glosten.SetLowHigh(feedPrice - m_windowRange / 2, feedPrice + m_windowRange / 2);
			m_glosten.ComputeAskBid(out ask, out bid);
		}

		/// <summary>	Gets bid ask for order. </summary>
		///
		/// <remarks>	Paul, 24/02/2015. </remarks>
		///
		/// <param name="buyOrder">	   	true to buy order. </param>
		/// <param name="informedProp">	The informed property. </param>
		/// <param name="bid">		   	[out] The bid. </param>
		/// <param name="ask">		   	[out] The ask. </param>
		public void GetBidAskForOrder(bool buyOrder, decimal informedProp, out decimal bid, out decimal ask)
		{
			decimal preAsk, preBid;
			decimal postAsk, postBid;

			m_glosten.ComputeAskBid(out preAsk, out preBid);
			m_glosten.TradeOccured(buyOrder);
			m_glosten.ComputeAskBid(out postAsk, out postBid);

			ask = preAsk + informedProp * (postAsk - preAsk);
			bid = preBid + informedProp * (postBid - preBid);
		}

		/// <summary>	Gets neutral bid ask. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="bid">	[out] The bid. </param>
		/// <param name="ask">	[out] The ask. </param>
		public void GetNeutralBidAsk(out decimal bid, out decimal ask)
		{
			m_glosten.ComputeAskBid(out ask, out bid);
		}

		/// <summary>	Gets bid for sell. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="informedProp">	The informed property. </param>
		///
		/// <returns>	The bid for sell. </returns>
		public decimal GetBidForSell(decimal informedProp)
		{
			decimal bid, ask;
			GetBidAskForOrder(false, informedProp, out bid, out ask);
			return bid;
		}

		/// <summary>	Gets ask for buy. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="informedProp">	The informed property. </param>
		///
		/// <returns>	The ask for buy. </returns>
		public decimal GetAskForBuy(decimal informedProp)
		{
			decimal bid, ask;
			GetBidAskForOrder(true, informedProp, out bid, out ask);
			return ask;
		}

		/// <summary>	Sets inventory ratio. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="ratio">	The ratio. </param>
		///
		/// <returns>	A decimal. </returns>
		public void SetInventoryRatio(decimal ratio, out decimal bid, out decimal ask)
		{
			m_glosten.SetInventoryRatio(ratio);
			m_glosten.ComputeAskBid(out ask, out bid);
		}
	}
}
