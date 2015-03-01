using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mime;
using System.IO;

using ServiceStack.Text;

namespace RestLib
{
	public class WebClientTimeout : WebClient
	{
		/// <summary>
		/// Time in milliseconds
		/// </summary>
		public int Timeout { get; set; }

		public WebClientTimeout() : this(60000) { }

		public WebClientTimeout(int timeout)
		{
			this.Timeout = timeout;
		}
		
		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);
			if (request != null)
			{
				request.Timeout = this.Timeout;
			}
			return request;
		}
	}

	public class Rest
	{
		public const string kContentTypeForm = "application/x-www-form-urlencoded";
		public const string kContentTypeJson = "application/json";
		public const string kContentTypePlain = "text/plain";

		static int m_gTimeoutSeconds = 30;

		static public void SetTimeoutSeconds(int seconds)
		{
			m_gTimeoutSeconds = seconds;
		}

		static public WebClientTimeout ConfigurePost(string url, string query, string contentType = kContentTypeForm, int timeoutMillis=5000)
		{
			WebClientTimeout client = new WebClientTimeout(timeoutMillis);
			client.Encoding = System.Text.Encoding.UTF8;
			client.Headers[HttpRequestHeader.ContentType] = contentType;
			return client;
		}
		
		static public Task<string> ExecutePostAsync(string url, string query, string contentType = kContentTypeForm)
		{
			return ConfigurePost(url, query, contentType).UploadStringTaskAsync(url, query);
		}

		static public Task<string> ExecuteGetAsync(string url, int timeoutMillis = 20000)
		{
			WebClientTimeout client = new WebClientTimeout(timeoutMillis);
			client.Encoding = System.Text.Encoding.UTF8;
			return client.DownloadStringTaskAsync(url);
		}

		static public string ExecutePostSync(string url, string query, string contentType = kContentTypeForm, 
											string username=null, string password=null)
		{
			WebClientTimeout client = new WebClientTimeout(m_gTimeoutSeconds*1000);
			client.Encoding = System.Text.Encoding.UTF8;
			if (username != null)
			{
				client.Credentials = new NetworkCredential(username, password);
			}
			client.Headers[HttpRequestHeader.ContentType] = contentType;

			try
			{
				return client.UploadString(url, query);
			}
			catch (WebException e)
			{
				if (e.Response != null && e.Response.ContentLength > 0)
				{
					return new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
				}
				else
				{
					throw;
				}
			}
		}

		static public string ExecuteGetSync(string url, int timeoutMillis=20000)
		{
			WebClientTimeout client = new WebClientTimeout(timeoutMillis);
			client.Encoding = System.Text.Encoding.UTF8;
			return client.DownloadString(url);
		}

		static async public Task<T> JsonApiCallAsync<T>(string url, string query)
		{
			string result = await ExecutePostAsync(url, query, kContentTypeJson);
			return JsonSerializer.DeserializeFromString<T>(result);
		}

		static async public Task<T> JsonApiGetAsync<T>(string url, int timeoutMillis = 20000)
		{
			string result = await ExecuteGetAsync(url, timeoutMillis);
			return JsonSerializer.DeserializeFromString<T>(result);
		}

		static public T JsonApiCallSync<T>(string url, string query, string username=null, string password=null)
		{
			string result = ExecutePostSync(url, query, kContentTypeJson, username, password);
			return JsonSerializer.DeserializeFromString<T>(result);
		}

		static public T JsonApiGetSync<T>(string url)
		{
			string result = ExecuteGetSync(url);
			return JsonSerializer.DeserializeFromString<T>(result);
		}
	}
}
