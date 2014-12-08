using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Casascius.Bitcoin;

using Org.BouncyCastle.Crypto.Digests;

namespace BitsharesCore
{
    public class BitsharesKeyPair
    {
		public const string kAddressPrefix = "BTS";

		KeyPair m_btcKeyPair;
		RIPEMD160 m_ripe;

		public BitsharesKeyPair(string wifPrivateKey)
		{
			m_btcKeyPair = new KeyPair(wifPrivateKey);
			m_ripe = System.Security.Cryptography.RIPEMD160.Create();
		}

		public byte[] ComputeBitsharesPubKeyBytes()
		{
			return ComputeBitsharesPubKeyFromBtcPubKey(m_btcKeyPair.GetCompressed(), m_ripe);
		}

		/// <summary>	Calculates the bitshares pubilc key from bitcoin public key. </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="compressedBtcPubKey">	The compressed btc pub key. </param>
		/// <param name="ripe">				  	The ripe. </param>
		///
		/// <returns>	The calculated bitshares pub key from btc pub key bytes. </returns>
		static public byte[] ComputeBitsharesPubKeyFromBtcPubKey(byte[] compressedBtcPubKey, RIPEMD160 ripe)
		{
			byte[] pubkeyCheck = ripe.ComputeHash(compressedBtcPubKey);
			byte[] pubkeyFinal = new byte[37];
			compressedBtcPubKey.CopyTo(pubkeyFinal, 0);
			Buffer.BlockCopy(pubkeyCheck, 0, pubkeyFinal, 37 - 4, 4);
			return pubkeyFinal;
		}

		/// <summary>	Calculates the bitshares public key from bitcoin public key </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="compressedBtcPubKey">	The compressed btc pub key. </param>
		/// <param name="ripe">				  	The ripe. </param>
		///
		/// <returns>	The calculated bitshares pub key. </returns>
		static public string ComputeBitsharesPubKey(byte[] compressedBtcPubKey, RIPEMD160 ripe)
		{
			return kAddressPrefix + Base58.FromByteArray(ComputeBitsharesPubKeyFromBtcPubKey(compressedBtcPubKey, ripe));
		}

		public string ComputeBitsharesPubKey()
		{
			return kAddressPrefix + Base58.FromByteArray(ComputeBitsharesPubKeyBytes());
		}

		/// <summary>	Calculates the bitshares address from a bitcoin public key. </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="compressedBtcPubKey">	The compressed btc pub key. </param>
		/// <param name="ripe">				  	The ripe. </param>
		///
		/// <returns>	The calculated bitshares address from btc pub key bytes. </returns>
		static public byte[] ComputeBitsharesAddressFromBtcPubKey(byte[] compressedBtcPubKey, RIPEMD160 ripe)
		{
			byte[] sha512 = Crypto.ComputeSha512(compressedBtcPubKey);
			byte[] addr = ripe.ComputeHash(sha512);
			byte[] check = ripe.ComputeHash(addr);

			byte[] addrFinal = new byte[20 + 4];
			addr.CopyTo(addrFinal, 0);

			Buffer.BlockCopy(check, 0, addrFinal, 20, 4);
			return addrFinal;
		}

		/// <summary>	Calculates the bitshares address from bitcoin public key </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="compressedBtcPubKey">	The compressed btc pub key. </param>
		/// <param name="ripe">				  	The ripe. </param>
		///
		/// <returns>	The calculated bitshares address. </returns>
		static public string ComputeBitsharesAddress(byte[] compressedBtcPubKey, RIPEMD160 ripe)
		{
			return kAddressPrefix + Base58.FromByteArray(ComputeBitsharesAddressFromBtcPubKey(compressedBtcPubKey, ripe));
		}

		public byte[] ComputeBitsharesAddressBytes()
		{
			return ComputeBitsharesAddressFromBtcPubKey(m_btcKeyPair.GetCompressed(), m_ripe);
		}

		public string ComputeBitsharesAddress()
		{
			return ComputeBitsharesAddress(m_btcKeyPair.GetCompressed(), m_ripe);
		}
    }
}
