using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using SharpTestsEx;
using RestLib;
using WebDaemonShared;
using ServiceStack.Text;
using MetaDaemon;
using Monsterer.Util;
using ApiHost;
using WebDaemonSharedTables;
using MetaDaemon.Markets;
using MetaData;

namespace MetaDaemonUnitTests
{
	[TestFixture]
	public class SubmitAddressTest : TestBase
	{
		[Test]
		public void SubmitAddressNoParams()
		{
			PostNoParameters(Routes.kSubmitAddress);
		}

		[Test]
		public void SubmitAddressInvalidParams()
		{
			PostInvalidParameters(Routes.kSubmitAddress);
		}

		[Test]
		public void SubmitInvalidSymbolPair()
		{
			string market = "dfsdfsf";
			string content = Post(Routes.kSubmitAddress, RestHelpers.BuildPostArgs(WebForms.kReceivingAddress, kBitcoinAddress,
																					WebForms.kSymbolPair, market,
																					WebForms.kOrderType, MetaOrderType.sell));
			ReplyIsApiError(content, new ApiExceptionUnknownMarket(market));
		}

		[Test]
		public void SubmitInvalidOrderType()
		{
			string ordertype = "dfsdfsf";
			string content = Post(Routes.kSubmitAddress, WebForms.kOrderType + "=" + ordertype);
			ReplyIsApiError(content, new ApiExceptionMissingParameter());
		}

		string SubmitAddress(string receivingAddress, MetaOrderType type, string symbolPair=null)
		{
			if (symbolPair == null)
			{
				symbolPair = m_defaultSymbolPair;
			}
			return Post(Routes.kSubmitAddress, RestHelpers.BuildPostArgs(WebForms.kReceivingAddress, receivingAddress,
																		WebForms.kSymbolPair, symbolPair,
																		WebForms.kOrderType, type));
		}

		[Test]
		public void SubmitInvalidBitcoinAddress()
		{
			string address = "dfsdfsf";
			string content = SubmitAddress(address, MetaOrderType.sell);

			ReplyIsApiError(content, new ApiExceptionInvalidAddress(address));
		}

		[Test]
		public void SubmitInvalidBitsharesAccount()
		{
			string address = "dfsdfsf";
			string content = SubmitAddress(address, MetaOrderType.buy);

			ReplyIsApiError(content, new ApiExceptionInvalidAccount(address));
		}

		[Test]
		public void SubmitBitsharesAccountAsBitcoinAddress()
		{
			string content = SubmitAddress(kBitsharesAccount, MetaOrderType.sell);

			ReplyIsApiError(content, new ApiExceptionInvalidAddress(kBitsharesAccount));
		}

		[Test]
		public void SubmitBitcoinAddressAsBitsharesAccount()
		{
			string content = SubmitAddress(kBitcoinAddress, MetaOrderType.buy);

			ReplyIsApiError(content, new ApiExceptionInvalidAccount(kBitcoinAddress));
		}

		string SubmitValidBitcoinAddress(string market)
		{
			string content = SubmitAddress(kBitcoinAddress, MetaOrderType.sell, market);

			string memo = MarketBase.CreateMemo(kBitcoinAddress, GetMarket(market).symbol_pair, 0);

			string reality = JsonSerializer.SerializeToString<SubmitAddressResponse>(new SubmitAddressResponse { deposit_address = m_api.m_DaemonAccount, memo = memo });

			content.Should().Be.EqualTo(reality);

			return content;
		}

		string SubmitValidBitsharesAccount(string market)
		{
			string content = SubmitAddress(kBitsharesAccount, MetaOrderType.buy, market);

			MarketRow m = GetMarket(market);

			SenderToDepositRow s2d = m_database.Query<SenderToDepositRow>("SELECT * FROM sender_to_deposit WHERE receiving_address=@r AND symbol_pair=@m;", kBitsharesAccount, m.symbol_pair).FirstOrDefault();

			string reality = JsonSerializer.SerializeToString<SubmitAddressResponse>(new SubmitAddressResponse { deposit_address = s2d.deposit_address });

			content.Should().Be.EqualTo(reality);

			return content;
		}

		[Test]
		public void SubmitValidBitcoinAddress()
		{
			SubmitValidBitcoinAddress(m_defaultSymbolPair);
		}

		[Test]
		public void SubmitValidBitsharesAccount()
		{
			SubmitValidBitsharesAccount(m_defaultSymbolPair);
		}

		[Test]
		public void SubmitValidBitcoinAddressTwoMarkets()
		{
			string contentA = SubmitValidBitcoinAddress(m_defaultSymbolPair);
			string contentB = SubmitValidBitcoinAddress(m_alternateMarket);

			SubmitAddressResponse A = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(contentA);
			SubmitAddressResponse B = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(contentB);

			A.memo.Should().Not.Be.EqualTo(B.memo);

			contentA.Should().Not.Be.EqualTo(contentB);
		}

		[Test]
		public void SubmitValidBitsharesAccountTwoMarkets()
		{
			string contentA = SubmitValidBitsharesAccount(m_defaultSymbolPair);
			string contentB = SubmitValidBitsharesAccount(m_alternateMarket);

			SubmitAddressResponse A = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(contentA);
			SubmitAddressResponse B = JsonSerializer.DeserializeFromString<SubmitAddressResponse>(contentB);

			A.deposit_address.Should().Not.Be.EqualTo(B.deposit_address);

			contentA.Should().Not.Be.EqualTo(contentB);
		}

		[Test]
		public void SubmitValidBitsharesAccountTwice()
		{
			string contentA = SubmitValidBitsharesAccount(m_defaultSymbolPair);
			string contentB = SubmitValidBitsharesAccount(m_defaultSymbolPair);

			contentA.Should().Be.EqualTo(contentB);
		}

		[Test]
		public void SubmitValidBitcoinAddresssTwice()
		{
			string contentA = SubmitValidBitcoinAddress(m_defaultSymbolPair);
			string contentB = SubmitValidBitcoinAddress(m_defaultSymbolPair);

			contentA.Should().Be.EqualTo(contentB);
		}

		[Test]
		public void SubmitValidBitsharesAccountTwiceTwoMarkets()
		{
			string contentA = SubmitValidBitsharesAccount(m_defaultSymbolPair);
			string contentC = SubmitValidBitsharesAccount(m_alternateMarket);
			string contentB = SubmitValidBitsharesAccount(m_defaultSymbolPair);
			string contentD = SubmitValidBitsharesAccount(m_alternateMarket);

			contentA.Should().Be.EqualTo(contentB);
			contentC.Should().Be.EqualTo(contentD);
			contentA.Should().Not.Be.EqualTo(contentC);
			contentB.Should().Not.Be.EqualTo(contentD);
		}

		[Test]
		public void SubmitValidBitcoinAddresssTwiceTwoMarkets()
		{
			string contentA = SubmitValidBitcoinAddress(m_defaultSymbolPair);
			string contentC = SubmitValidBitcoinAddress(m_alternateMarket);
			string contentB = SubmitValidBitcoinAddress(m_defaultSymbolPair);
			string contentD = SubmitValidBitcoinAddress(m_alternateMarket);

			contentA.Should().Be.EqualTo(contentB);
			contentC.Should().Be.EqualTo(contentD);
			contentA.Should().Not.Be.EqualTo(contentC);
			contentB.Should().Not.Be.EqualTo(contentD);
		}
	}
}
