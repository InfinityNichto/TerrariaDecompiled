using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.ExceptionServices;

namespace System.Net.Mail;

internal sealed class SendMailAsyncResult : System.Net.LazyAsyncResult
{
	private readonly SmtpConnection _connection;

	private readonly MailAddress _from;

	private readonly string _deliveryNotify;

	private static readonly AsyncCallback s_sendMailFromCompleted = SendMailFromCompleted;

	private static readonly AsyncCallback s_sendToCollectionCompleted = SendToCollectionCompleted;

	private static readonly AsyncCallback s_sendDataCompleted = SendDataCompleted;

	private readonly List<SmtpFailedRecipientException> _failedRecipientExceptions = new List<SmtpFailedRecipientException>();

	private Stream _stream;

	private readonly MailAddressCollection _toCollection;

	private int _toIndex;

	private readonly bool _allowUnicode;

	internal SendMailAsyncResult(SmtpConnection connection, MailAddress from, MailAddressCollection toCollection, bool allowUnicode, string deliveryNotify, AsyncCallback callback, object state)
		: base(null, state, callback)
	{
		_toCollection = toCollection;
		_connection = connection;
		_from = from;
		_deliveryNotify = deliveryNotify;
		_allowUnicode = allowUnicode;
	}

	internal void Send()
	{
		SendMailFrom();
	}

	internal static MailWriter End(IAsyncResult result)
	{
		SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult)result;
		object obj = sendMailAsyncResult.InternalWaitForCompletion();
		if (obj is Exception source && (!(obj is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)obj).fatal))
		{
			ExceptionDispatchInfo.Throw(source);
		}
		return new MailWriter(sendMailAsyncResult._stream, encodeForTransport: true);
	}

	private void SendMailFrom()
	{
		IAsyncResult asyncResult = MailCommand.BeginSend(_connection, SmtpCommands.Mail, _from, _allowUnicode, s_sendMailFromCompleted, this);
		if (asyncResult.CompletedSynchronously)
		{
			MailCommand.EndSend(asyncResult);
			SendToCollection();
		}
	}

	private static void SendMailFromCompleted(IAsyncResult result)
	{
		if (!result.CompletedSynchronously)
		{
			SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult)result.AsyncState;
			try
			{
				MailCommand.EndSend(result);
				sendMailAsyncResult.SendToCollection();
			}
			catch (Exception result2)
			{
				sendMailAsyncResult.InvokeCallback(result2);
			}
		}
	}

	private void SendToCollection()
	{
		while (_toIndex < _toCollection.Count)
		{
			MultiAsyncResult multiAsyncResult = (MultiAsyncResult)RecipientCommand.BeginSend(_connection, _toCollection[_toIndex++].GetSmtpAddress(_allowUnicode) + _deliveryNotify, s_sendToCollectionCompleted, this);
			if (!multiAsyncResult.CompletedSynchronously)
			{
				return;
			}
			if (!RecipientCommand.EndSend(multiAsyncResult, out var response))
			{
				_failedRecipientExceptions.Add(new SmtpFailedRecipientException(_connection.Reader.StatusCode, _toCollection[_toIndex - 1].GetSmtpAddress(_allowUnicode), response));
			}
		}
		SendData();
	}

	private static void SendToCollectionCompleted(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult)result.AsyncState;
		try
		{
			if (!RecipientCommand.EndSend(result, out var response))
			{
				sendMailAsyncResult._failedRecipientExceptions.Add(new SmtpFailedRecipientException(sendMailAsyncResult._connection.Reader.StatusCode, sendMailAsyncResult._toCollection[sendMailAsyncResult._toIndex - 1].GetSmtpAddress(sendMailAsyncResult._allowUnicode), response));
				if (sendMailAsyncResult._failedRecipientExceptions.Count == sendMailAsyncResult._toCollection.Count)
				{
					SmtpFailedRecipientException ex = ((sendMailAsyncResult._toCollection.Count == 1) ? sendMailAsyncResult._failedRecipientExceptions[0] : new SmtpFailedRecipientsException(sendMailAsyncResult._failedRecipientExceptions, allFailed: true));
					ex.fatal = true;
					sendMailAsyncResult.InvokeCallback(ex);
					return;
				}
			}
			sendMailAsyncResult.SendToCollection();
		}
		catch (Exception result2)
		{
			sendMailAsyncResult.InvokeCallback(result2);
		}
	}

	private void SendData()
	{
		IAsyncResult asyncResult = DataCommand.BeginSend(_connection, s_sendDataCompleted, this);
		if (asyncResult.CompletedSynchronously)
		{
			DataCommand.EndSend(asyncResult);
			_stream = _connection.GetClosableStream();
			if (_failedRecipientExceptions.Count > 1)
			{
				InvokeCallback(new SmtpFailedRecipientsException(_failedRecipientExceptions, _failedRecipientExceptions.Count == _toCollection.Count));
			}
			else if (_failedRecipientExceptions.Count == 1)
			{
				InvokeCallback(_failedRecipientExceptions[0]);
			}
			else
			{
				InvokeCallback();
			}
		}
	}

	private static void SendDataCompleted(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult)result.AsyncState;
		try
		{
			DataCommand.EndSend(result);
			sendMailAsyncResult._stream = sendMailAsyncResult._connection.GetClosableStream();
			if (sendMailAsyncResult._failedRecipientExceptions.Count > 1)
			{
				sendMailAsyncResult.InvokeCallback(new SmtpFailedRecipientsException(sendMailAsyncResult._failedRecipientExceptions, sendMailAsyncResult._failedRecipientExceptions.Count == sendMailAsyncResult._toCollection.Count));
			}
			else if (sendMailAsyncResult._failedRecipientExceptions.Count == 1)
			{
				sendMailAsyncResult.InvokeCallback(sendMailAsyncResult._failedRecipientExceptions[0]);
			}
			else
			{
				sendMailAsyncResult.InvokeCallback();
			}
		}
		catch (Exception result2)
		{
			sendMailAsyncResult.InvokeCallback(result2);
		}
	}

	internal SmtpFailedRecipientException GetFailedRecipientException()
	{
		if (_failedRecipientExceptions.Count == 1)
		{
			return _failedRecipientExceptions[0];
		}
		if (_failedRecipientExceptions.Count > 1)
		{
			return new SmtpFailedRecipientsException(_failedRecipientExceptions, allFailed: false);
		}
		return null;
	}
}
