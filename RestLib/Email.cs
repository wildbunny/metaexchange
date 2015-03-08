using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RestLib
{
	public class Email
	{
		static public void SendMail(string from, string to, string subject, string body, bool html = false)
		{
			MailMessage message = new MailMessage(from, to);
			message.Subject = subject;
			message.Body = body;
			message.IsBodyHtml = html;

			SmtpClient server = new SmtpClient("localhost");
			server.DeliveryMethod = SmtpDeliveryMethod.Network;
			server.Send(message);
		}

		/// MONO completely lacks SmtpClient.SendAsync, so this is a copy -----------------------------------------------------------------------------


		/// <summary>
		/// 
		/// </summary>
		/// <param name="tcs"></param>
		/// <param name="e"></param>
		/// <param name="handler"></param>
		/// <param name="client"></param>
		static void HandleCompletion(TaskCompletionSource<object> tcs, AsyncCompletedEventArgs e, SendCompletedEventHandler handler, SmtpClient client)
		{
			if (e.UserState == tcs)
			{
				try
				{
					client.SendCompleted -= handler;
				}
				finally
				{
					if (e.Error != null)
					{
						tcs.TrySetException(e.Error);
					}
					else if (!e.Cancelled)
					{
						tcs.TrySetResult(null);
					}
					else
					{
						tcs.TrySetCanceled();
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		static Task SendMailAsync(SmtpClient client, MailMessage message)
		{
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			SendCompletedEventHandler sendCompletedEventHandler = null;
			sendCompletedEventHandler = (object sender, AsyncCompletedEventArgs e) => HandleCompletion(taskCompletionSource, e, sendCompletedEventHandler, client);
			client.SendCompleted += sendCompletedEventHandler;
			try
			{
				client.SendAsync(message, taskCompletionSource);
			}
			catch
			{
				client.SendCompleted -= sendCompletedEventHandler;
				throw;
			}
			return taskCompletionSource.Task;
		}

		/// MONO completely lacks SmtpClient.SendAsync, so this is a copy -----------------------------------------------------------------------------

		static public Task SendMailAsync(string from, string to, string subject, string body, bool html = false)
		{
			MailMessage message = new MailMessage(from, to);
			message.Subject = subject;
			message.Body = body;
			message.IsBodyHtml = html;

			SmtpClient server = new SmtpClient("localhost");

			server.DeliveryMethod = SmtpDeliveryMethod.Network;
			return SendMailAsync(server, message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="username"></param>
		/// <param name="email"></param>
		/// <param name="ticket"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		static public string FilterReplaceMailText(string username, string email, long ticket, string body)
		{
			return body.Replace("<username>", username).Replace("<email>", email).Replace("<ticket>", ticket.ToString());
		}
	}
}
