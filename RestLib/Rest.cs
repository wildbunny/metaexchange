using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mime;

using ServiceStack.Text;

namespace RestLib
{
	public class Rest
	{
		public const string kContentTypeForm = "application/x-www-form-urlencoded";
		public const string kContentTypeJson = "application/json";
		public const string kContentTypePlain = "text/plain";
		
		static public Task<string> ExecutePostAsync(string url, string query, string contentType = kContentTypeForm)
		{
			WebClient client = new WebClient();
			client.Encoding = System.Text.Encoding.UTF8;
			client.Headers[HttpRequestHeader.ContentType] = contentType;

			

			return client.UploadStringTaskAsync(url, query);

			
		}

		static public Task<string> ExecuteGetAsync(string url)
		{
			WebClient client = new WebClient();
			client.Encoding = System.Text.Encoding.UTF8;
			return client.DownloadStringTaskAsync(url);
		}

		static public string ExecutePostSync(string url, string query, string contentType = kContentTypeForm, 
											string username=null, string password=null)
		{
			WebClient client = new WebClient();
			client.Encoding = System.Text.Encoding.UTF8;
			if (username != null)
			{
				client.Credentials = new NetworkCredential(username, password);

				//string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
				//client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", credentials);
			}
			client.Headers[HttpRequestHeader.ContentType] = contentType;
			return client.UploadString(url, query);
		}

		static public string ExecuteGetSync(string url)
		{
			WebClient client = new WebClient();
			client.Encoding = System.Text.Encoding.UTF8;
			return client.DownloadString(url);
		}

		static async public Task<T> JsonApiCallAsync<T>(string url, string query)
		{
			string result = await ExecutePostAsync(url, query, kContentTypeJson);
			return JsonSerializer.DeserializeFromString<T>(result);
		}

		static async public Task<T> JsonApiGetAsync<T>(string url)
		{
			string result = await ExecuteGetAsync(url);
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
