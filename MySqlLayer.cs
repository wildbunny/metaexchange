using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ApiHost;
using MySqlDatabase;
using Monsterer.Request;
using MetaData;
using WebDaemonShared;

namespace MetaExchange
{
	public class IDummy
	{
		public MySqlData m_database;
		//	public string m_bitsharesAccount;
	}

	/// <summary>	Dummy authenticator to give pages access to the database </summary>
	///
	/// <remarks>	Paul, 27/01/2015. </remarks>
	public class MysqlAuthenticator : Authentication<IDummy>
	{
		MySqlData m_database;

		public MysqlAuthenticator(string database, string databaseUser, string password,
									int allowedThreadId)
			: base()
		{
			m_database = new MySqlData(database, databaseUser, password);
			//m_bitsharesAccount = bitsharesAccount;
		}

		public override string GenerateToken(RequestContext ctx, IDummy authObj)
		{
			return "token";
		}

		public override void PostAuthorise(RequestContext ctx, IDummy authObj)
		{
		}

		public override IDummy Authorise(RequestContext ctx)
		{
			return new IDummy { m_database = m_database };//, m_bitsharesAccount = m_bitsharesAccount };
		}

		public MySqlData m_Database
		{
			get { return m_database; }
		}
	}
}
