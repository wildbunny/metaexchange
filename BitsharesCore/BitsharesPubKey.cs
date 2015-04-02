using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;

using Casascius.Bitcoin;

namespace BitsharesCore
{
	public class BadChecksumException : Exception { }

	public class BitsharesPubKey
	{
		byte[] m_compressed;
		RIPEMD160 m_ripe;

		/// <summary>	Construct from compressed public key </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="compressed">	The compressed. </param>
		public BitsharesPubKey(byte[] compressed)
		{
			m_compressed = compressed;
			m_ripe = RIPEMD160.Create();
		}

		/// <summary>	Construct from base58 bitshares public key </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="base58BtsPubKey">	The base 58 bts pub key. </param>
		public BitsharesPubKey(string base58BtsPubKey)
		{
			m_ripe = RIPEMD160.Create();

			if (base58BtsPubKey.Length == 54)
			{
				base58BtsPubKey = base58BtsPubKey.Substring(4, base58BtsPubKey.Length - 4);
			}
			else if (base58BtsPubKey.Length == 53)
			{
				base58BtsPubKey = base58BtsPubKey.Substring(3, base58BtsPubKey.Length - 3);
			}
			else
			{
				throw new NotImplementedException();
			}

			byte[] data = Base58.ToByteArray(base58BtsPubKey);
			Debug.Assert(data.Length == 37);

			byte[] pubkeyCheck = RIPEMD160.Create().ComputeHash(data, 0, 33);

			// check the hash first four bytes are equal to the last four bytes of the pub key
			for (int i = 0; i < 4; i++)
			{
				if (pubkeyCheck[i] != data[i + 33])
				{
					throw new BadChecksumException();
				}
			}

			m_compressed = new byte[33];
			Array.Copy(data, 0, m_compressed, 0, m_compressed.Length);
		}

		/// <summary>	Query if 'base58Hex' is valid public key. </summary>
		///
		/// <remarks>	Paul, 19/03/2015. </remarks>
		///
		/// <param name="base58Hex">	The base 58 hexadecimal. </param>
		///
		/// <returns>	true if valid public key, false if not. </returns>
		static public bool IsValidPublicKey(string base58Hex)
		{
			try
			{
				new BitsharesPubKey(base58Hex);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>	Create a bitshares public key from a hex bitcoin public key </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <param name="bitcoinHexPublicKey">	The bitcoin hexadecimal public key. </param>
		///
		/// <returns>	A BitsharesPubKey. </returns>
		static public BitsharesPubKey FromBitcoinHex(string bitcoinHexPublicKey, byte addressByteType=0)
		{
			PublicKey bitcoin = new PublicKey(bitcoinHexPublicKey, addressByteType);
			return new BitsharesPubKey(bitcoin.GetCompressed());
		}

		/// <summary>	Gets the public key in base 58. </summary>
		///
		/// <value>	The m pub key base 58. </value>
		public string m_PubKeyBase58
		{
			get { return BitsharesKeyPair.ComputeBitsharesPubKey(m_compressed, m_ripe); }
		}

		/// <summary>	Gets the BTS address associated with this public key. </summary>
		///
		/// <value>	The m address. </value>
		public string m_Address
		{
			get { return BitsharesKeyPair.ComputeBitsharesAddress(m_compressed, m_ripe); }
		}

		/// <summary>	Converts this bitshares public key into to a bitcoin address. </summary>
		///
		/// <remarks>	Paul, 08/12/2014. </remarks>
		///
		/// <returns>	This object as a string. </returns>
		public string ToBitcoinAddress(bool compressed, byte addressType)
		{
			PublicKey bitcoin = new PublicKey(m_compressed, addressType);

			if (!compressed)
			{
				bitcoin = new PublicKey( bitcoin.GetUncompressed(), addressType );
			}

			return bitcoin.AddressBase58;
		}
	}
}
