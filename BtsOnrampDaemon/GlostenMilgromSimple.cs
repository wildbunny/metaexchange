using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BtsOnrampDaemon
{
	public class GlostenMilgromSimple
	{
		decimal m_mu;
		decimal m_vLow;
		decimal m_vHigh;
		decimal m_pi;

		public GlostenMilgromSimple(decimal informedProportion, decimal vLow, decimal vHigh)
		{
			m_mu = informedProportion;
			m_vLow = vLow;
			m_vHigh = vHigh;
			m_pi = 0.5M;
		}

		public void ComputeAskBid(out decimal ask, out decimal bid)
		{
			ask = (m_pi * (1 + m_mu) * m_vHigh + (1 - m_pi) * (1 - m_mu) * m_vLow) / (1 + m_Shared);
			bid = (m_pi * (1 - m_mu) * m_vHigh + (1 - m_pi) * (1 + m_mu) * m_vLow) / (1 - m_Shared);
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
	}
}
