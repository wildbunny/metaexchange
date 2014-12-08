using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using BitsharesCore;

using NUnit.Framework;
using Casascius.Bitcoin;

namespace BitsharesCoreUnitTests
{
	[TestFixture]
    public class AddressAndPubKeyTests
    {
		// in order to test this, I need:
		//
		// a list of bitcoin pubkeys
		// a list of bitcoin privkeys
		// a list of bitcoin addreses
		// a list of imported bts pub keys
		// a list of imported bts addresses

		List<string> m_bitcoinPrivKeys = new List<string>()
		{
			"5HqUkGuo62BfcJU5vNhTXKJRXuUi9QSE6jp8C3uBJ2BVHtB8WSd",
			"5JWcdkhL3w4RkVPcZMdJsjos22yB5cSkPExerktvKnRNZR5gx1S",
			"5HvVz6XMx84aC5KaaBbwYrRLvWE46cH6zVnv4827SBPLorg76oq",
			"5Jete5oFNjjk3aUMkKuxgAXsp7ZyhgJbYNiNjHLvq5xzXkiqw7R",
			"5KDT58ksNsVKjYShG4Ls5ZtredybSxzmKec8juj7CojZj6LPRF7"
		};

		List<string> m_bitcoinAddresses = new List<string>()
		{
			"141fYYgjgTfxWCzUhFwVrad54EWi8Yw29a",
			"19854zGaBhcgHV2hZa6bzqMBW5kHCbw7YA",
			"1G7qw8FiVfHEFrSt3tDi6YgfAdrDrEM44Z",
			"12c7KAAZfpREaQZuvjC5EhpoN6si9vekqK",
			"1Gu5191CVHmaoU3Zz3prept87jjnpFDrXL"
		};

		List<string> m_bitcoinPubKeys = new List<string>()
		{
			"04a05c6fd57267cc0f1b82e785f0261e1d25f6751978db972b39747393c489e614a6a45b3c07fe7e24c93846d8e2ce302fa7a9dadda308762bf63db28743d20bba",
			"04906435f6f9ce6f3d4e4cf8d3d0ddce209a83277738ffdc7eae6308f4ed2963df0da8746cf13a6189a281965c580f42415d86e96c232a8925ea153254bb879f60",
			"045836d5ecf8eb2572f8d3e45e9a401c3acbfffcabe76697ca3539b428aa8641b827fbcad3334e0bfc636463efb494e4a57525fce15721e2e15cfff15808859389",
			"04a71eb3edee13ab02f16900f820f66d59028f68ca5a1f48d1676fad9fd78e246191d4619203ce7fa1161a3fc6d3466adcae232559db620b3f74e7fea420c9a60d",
			"041e5080611e2f1d9d7a61146ae0dda74181ea557044cf6c418d9ffccd9ebefcf052895a13e0b184a10a95d82d5399a6ecdb36b4f852c6e8774a3aea658f626d64"
		};

		List<string> m_btsPubKeys = new List<string>()
		{
			"BTS677ZZd62Ca7SoUJoT1CytBhj4aJewzzi8tQZxYNqpSSK69FTuF",
			"BTS5z5e3BawwMY6UmcBQxYpkKZ8QQm4wdtS4KMZiWAcWBUC3RJuLT",
			"BTS7W5qsanXHgRAZPijbrLMDwX6VmHqUdL2s8PZiYKD5h1R7JaqRJ",
			"BTS86qPFWptPfUNKVi6hemeEWshoLerN6JvzCvFjqnRSEJg7nackU",
			"BTS57qhJwt9hZtBsGgV7J5ZPHFi5r5MEeommYnFpDb6grK3qev2qX"
		};

		List<string> m_btsAddresses = new List<string>()
		{
			"BTSFN9r6VYzBK8EKtMewfNbfiGCr56pHDBFi",
			"BTSdXrrTXimLb6TEt3nHnePwFmBT6Cck112",
			"BTSJQUAt4gz4civ8gSs5srTK4r82F7HvpChk",
			"BTSFPXXHXXGbyTBwdKoJaAPXRnhFNtTRS4EL",
			"BTS3qXyZnjJneeAddgNDYNYXbF7ARZrRv5dr"
		};

		RIPEMD160 m_ripe;

		public AddressAndPubKeyTests()
		{
			m_ripe = RIPEMD160.Create();
		}

		[Test]
		public void CheckBitcoinPubKeys()
		{
			for (int i=0; i<m_bitcoinPrivKeys.Count; i++)
			{
				string priv = m_bitcoinPrivKeys[i];
				string pub = m_bitcoinPubKeys[i];

				KeyPair kp = new KeyPair(priv);
				Assert.AreEqual( pub, kp.PublicKeyHex.ToLower() );
			}
		}

		[Test]
		public void CheckBitcoinAddresses()
		{
			for (int i = 0; i < m_bitcoinPrivKeys.Count; i++)
			{
				string priv = m_bitcoinPrivKeys[i];
				string addr = m_bitcoinAddresses[i];

				KeyPair kp = new KeyPair(priv);
				Assert.AreEqual(addr, kp.AddressBase58);
			}
		}

		[Test]
		public void CheckBtsPubKeys()
		{
			for (int i = 0; i < m_bitcoinPrivKeys.Count; i++)
			{
				string priv = m_bitcoinPrivKeys[i];
				string pub = m_btsPubKeys[i];

				KeyPair kp = new KeyPair(priv);
				string compare = BitsharesKeyPair.ComputeBitsharesPubKey(kp.GetCompressed(), m_ripe);

				Assert.AreEqual(pub, compare);
			}
		}

		[Test]
		public void CheckBtsAdresses()
		{
			for (int i = 0; i < m_bitcoinPrivKeys.Count; i++)
			{
				string priv = m_bitcoinPrivKeys[i];
				string addr = m_btsAddresses[i];

				KeyPair kp = new KeyPair(priv);
				string compare = BitsharesKeyPair.ComputeBitsharesAddress(kp.GetCompressed(), m_ripe);

				Assert.AreEqual(addr, compare);
			}
		}

		[Test]
		public void CheckBtsPubKeyFromBitcoinPubKeyHex()
		{
			for (int i = 0; i < m_bitcoinPrivKeys.Count; i++)
			{
				string bitcoinHex = m_bitcoinPubKeys[i];
				string btsHex = m_btsPubKeys[i];

				BitsharesPubKey key = BitsharesPubKey.FromBitcoinHex(bitcoinHex);
				string compare = key.m_PubKeyBase58;

				Assert.AreEqual(btsHex, compare);
			}
		}

		[Test]
		public void CheckBtsAddressFromBitcoinPubKeyHex()
		{
			for (int i = 0; i < m_bitcoinPrivKeys.Count; i++)
			{
				string bitcoinHex = m_bitcoinPubKeys[i];
				string btsAddress = m_btsAddresses[i];

				BitsharesPubKey key = BitsharesPubKey.FromBitcoinHex(bitcoinHex);
				string compare = key.m_Address;

				Assert.AreEqual(btsAddress, compare);
			}
		}

		[Test]
		public void CheckBitcoinAddressFromBitsharesPublicKey()
		{
			for (int i = 0; i < m_btsPubKeys.Count; i++)
			{
				string btsPubKey = m_btsPubKeys[i];
				string bitcoinAddress = m_bitcoinAddresses[i];

				BitsharesPubKey key = new BitsharesPubKey(btsPubKey);

				Assert.AreEqual(bitcoinAddress, key.ToBitcoinAddress(false));
			}
		}
    }
}
