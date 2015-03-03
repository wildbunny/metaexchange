using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebDaemonShared;

namespace BestPrice
{
	public class GlostenMilgromSimple
	{
		decimal m_mu;
		decimal m_vLow;
		decimal m_vHigh;
		protected decimal m_pi;

		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Paul, 24/02/2015. </remarks>
		///
		/// <param name="informedProportion">	The informed proportion. </param>
		/// <param name="vLow">				 	The low. </param>
		/// <param name="vHigh">			 	The high. </param>
		/// <param name="inventoryRatio">	 	The inventory ratio, 0=maximum inventory, 0.5=equal, 1.0=minimum </param>
		public GlostenMilgromSimple(decimal informedProportion, decimal vLow, decimal vHigh, decimal inventoryRatio)
		{
			m_mu = informedProportion;
			m_vLow = vLow;
			m_vHigh = vHigh;
			m_pi = inventoryRatio;
		}

		public void ComputeAskBid(out decimal ask, out decimal bid)
		{
			ask = (m_pi * (1 + m_mu) * m_vHigh + (1 - m_pi) * (1 - m_mu) * m_vLow) / (1 + m_Shared);
			bid = (m_pi * (1 - m_mu) * m_vHigh + (1 - m_pi) * (1 + m_mu) * m_vLow) / (1 - m_Shared);

			ask = Numeric.TruncateDecimal(ask, 8);
			bid = Numeric.TruncateDecimal(bid, 8);
		}

		decimal m_Shared
		{
			get { return (2 * m_pi - 1) * m_mu; }
		}

		/// <summary>
		/// 
		/// </summary>
		public decimal m_TrueValue
		{
			get { return m_vHigh * m_pi + m_vLow * (1 - m_pi); }
		}

		/// <summary>	Sets the proportion of informed traders </summary>
		///
		/// <value>	The m mu. </value>
		public decimal m_Mu
		{
			set { m_mu = value; }
			get { return m_mu; }
		}

		/// <summary>	Trade occured. </summary>
		///
		/// <remarks>	Paul, 01/02/2015. </remarks>
		///
		/// <param name="buy">	true to buy. </param>
		public void TradeOccured(bool buy)
		{
			// recompute the value of the likelyhood of vhigh
			if (buy)
			{
				m_pi = m_pi * (1 + m_mu) / (1 + m_Shared);
			}
			else
			{
				m_pi = m_pi * (1 - m_mu) / (1 - m_Shared);
			}
		}

		/// <summary>	Sets low high. </summary>
		///
		/// <remarks>	Paul, 24/02/2015. </remarks>
		///
		/// <param name="low"> 	The low. </param>
		/// <param name="high">	The high. </param>
		public void SetLowHigh(decimal low, decimal high)
		{
			m_vLow = low;
			m_vHigh = high;
		}
	}

	public class GlostenMilgromDesiredSpread : GlostenMilgromSimple
	{
		public GlostenMilgromDesiredSpread(	decimal spread, decimal vLow, decimal vHigh, decimal inventoryRatio) : 
											base(0, vLow, vHigh, inventoryRatio)
		{
			SetInventoryRatio(inventoryRatio, spread);
		}

		/// <summary>	Sets inventory ratio. </summary>
		///
		/// <remarks>	Paul, 25/02/2015. </remarks>
		///
		/// <param name="inventoryRatio">	The inventory ratio, 0=maximum inventory, 0.5=equal,
		/// 								1.0=minimum. </param>
		public void SetInventoryRatio(decimal inventoryRatio, decimal spread)
		{
			m_pi = inventoryRatio;
			m_Mu = 0;

			decimal currentSpread = 0;
			do
			{
				m_Mu += 0.01M;

				decimal bid, ask;
				ComputeAskBid(out ask, out bid);

				currentSpread = ask - bid;
			} while (currentSpread < spread);
		}
	}
}
