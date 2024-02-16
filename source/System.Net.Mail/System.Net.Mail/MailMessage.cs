using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public class MailMessage : IDisposable
{
	private AlternateViewCollection _views;

	private AttachmentCollection _attachments;

	private AlternateView _bodyView;

	private string _body = string.Empty;

	private Encoding _bodyEncoding;

	private TransferEncoding _bodyTransferEncoding = TransferEncoding.Unknown;

	private bool _isBodyHtml;

	private bool _disposed;

	private readonly Message _message;

	private DeliveryNotificationOptions _deliveryStatusNotification;

	public MailAddress? From
	{
		get
		{
			return _message.From;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_message.From = value;
		}
	}

	public MailAddress? Sender
	{
		get
		{
			return _message.Sender;
		}
		[param: DisallowNull]
		set
		{
			_message.Sender = value;
		}
	}

	[Obsolete("ReplyTo has been deprecated. Use ReplyToList instead, which can accept multiple addresses.")]
	public MailAddress? ReplyTo
	{
		get
		{
			return _message.ReplyTo;
		}
		set
		{
			_message.ReplyTo = value;
		}
	}

	public MailAddressCollection ReplyToList => _message.ReplyToList;

	public MailAddressCollection To => _message.To;

	public MailAddressCollection Bcc => _message.Bcc;

	public MailAddressCollection CC => _message.CC;

	public MailPriority Priority
	{
		get
		{
			return _message.Priority;
		}
		set
		{
			_message.Priority = value;
		}
	}

	public DeliveryNotificationOptions DeliveryNotificationOptions
	{
		get
		{
			return _deliveryStatusNotification;
		}
		set
		{
			if (7u < (uint)value && value != DeliveryNotificationOptions.Never)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_deliveryStatusNotification = value;
		}
	}

	public string Subject
	{
		get
		{
			if (_message.Subject == null)
			{
				return string.Empty;
			}
			return _message.Subject;
		}
		[param: AllowNull]
		set
		{
			_message.Subject = value;
		}
	}

	public Encoding? SubjectEncoding
	{
		get
		{
			return _message.SubjectEncoding;
		}
		set
		{
			_message.SubjectEncoding = value;
		}
	}

	public NameValueCollection Headers => _message.Headers;

	public Encoding? HeadersEncoding
	{
		get
		{
			return _message.HeadersEncoding;
		}
		set
		{
			_message.HeadersEncoding = value;
		}
	}

	public string Body
	{
		get
		{
			if (_body == null)
			{
				return string.Empty;
			}
			return _body;
		}
		[param: AllowNull]
		set
		{
			_body = value;
			if (_bodyEncoding == null && _body != null)
			{
				if (MimeBasePart.IsAscii(_body, permitCROrLF: true))
				{
					_bodyEncoding = Encoding.ASCII;
				}
				else
				{
					_bodyEncoding = Encoding.GetEncoding("utf-8");
				}
			}
		}
	}

	public Encoding? BodyEncoding
	{
		get
		{
			return _bodyEncoding;
		}
		set
		{
			_bodyEncoding = value;
		}
	}

	public TransferEncoding BodyTransferEncoding
	{
		get
		{
			return _bodyTransferEncoding;
		}
		set
		{
			_bodyTransferEncoding = value;
		}
	}

	public bool IsBodyHtml
	{
		get
		{
			return _isBodyHtml;
		}
		set
		{
			_isBodyHtml = value;
		}
	}

	public AttachmentCollection Attachments
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			return _attachments ?? (_attachments = new AttachmentCollection());
		}
	}

	public AlternateViewCollection AlternateViews
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			return _views ?? (_views = new AlternateViewCollection());
		}
	}

	public MailMessage()
	{
		_message = new Message();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, _message, ".ctor");
		}
	}

	public MailMessage(string from, string to)
	{
		if (from == null)
		{
			throw new ArgumentNullException("from");
		}
		if (to == null)
		{
			throw new ArgumentNullException("to");
		}
		if (from.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "from"), "from");
		}
		if (to.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "to"), "to");
		}
		_message = new Message(from, to);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, _message, ".ctor");
		}
	}

	public MailMessage(string from, string to, string? subject, string? body)
		: this(from, to)
	{
		Subject = subject;
		Body = body;
	}

	public MailMessage(MailAddress from, MailAddress to)
	{
		if (from == null)
		{
			throw new ArgumentNullException("from");
		}
		if (to == null)
		{
			throw new ArgumentNullException("to");
		}
		_message = new Message(from, to);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			if (_views != null)
			{
				_views.Dispose();
			}
			if (_attachments != null)
			{
				_attachments.Dispose();
			}
			if (_bodyView != null)
			{
				_bodyView.Dispose();
			}
		}
	}

	private void SetContent(bool allowUnicode)
	{
		if (_bodyView != null)
		{
			_bodyView.Dispose();
			_bodyView = null;
		}
		if (AlternateViews.Count == 0 && Attachments.Count == 0)
		{
			if (!string.IsNullOrEmpty(_body))
			{
				_bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, _isBodyHtml ? "text/html" : null);
				_message.Content = _bodyView.MimePart;
			}
		}
		else if (AlternateViews.Count == 0 && Attachments.Count > 0)
		{
			MimeMultiPart mimeMultiPart = new MimeMultiPart(MimeMultiPartType.Mixed);
			if (!string.IsNullOrEmpty(_body))
			{
				_bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, _isBodyHtml ? "text/html" : null);
			}
			else
			{
				_bodyView = AlternateView.CreateAlternateViewFromString(string.Empty);
			}
			mimeMultiPart.Parts.Add(_bodyView.MimePart);
			foreach (Attachment attachment in Attachments)
			{
				if (attachment != null)
				{
					attachment.PrepareForSending(allowUnicode);
					mimeMultiPart.Parts.Add(attachment.MimePart);
				}
			}
			_message.Content = mimeMultiPart;
		}
		else
		{
			MimeMultiPart mimeMultiPart2 = null;
			MimeMultiPart mimeMultiPart3 = new MimeMultiPart(MimeMultiPartType.Alternative);
			if (!string.IsNullOrEmpty(_body))
			{
				_bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, null);
				mimeMultiPart3.Parts.Add(_bodyView.MimePart);
			}
			foreach (AlternateView alternateView in AlternateViews)
			{
				if (alternateView == null)
				{
					continue;
				}
				alternateView.PrepareForSending(allowUnicode);
				if (alternateView.LinkedResources.Count > 0)
				{
					MimeMultiPart mimeMultiPart4 = new MimeMultiPart(MimeMultiPartType.Related);
					mimeMultiPart4.ContentType.Parameters["type"] = alternateView.ContentType.MediaType;
					mimeMultiPart4.ContentLocation = alternateView.MimePart.ContentLocation;
					mimeMultiPart4.Parts.Add(alternateView.MimePart);
					foreach (LinkedResource linkedResource in alternateView.LinkedResources)
					{
						linkedResource.PrepareForSending(allowUnicode);
						mimeMultiPart4.Parts.Add(linkedResource.MimePart);
					}
					mimeMultiPart3.Parts.Add(mimeMultiPart4);
				}
				else
				{
					mimeMultiPart3.Parts.Add(alternateView.MimePart);
				}
			}
			if (Attachments.Count > 0)
			{
				mimeMultiPart2 = new MimeMultiPart(MimeMultiPartType.Mixed);
				mimeMultiPart2.Parts.Add(mimeMultiPart3);
				foreach (Attachment attachment2 in Attachments)
				{
					if (attachment2 != null)
					{
						attachment2.PrepareForSending(allowUnicode);
						mimeMultiPart2.Parts.Add(attachment2.MimePart);
					}
				}
				_message.Content = mimeMultiPart2;
			}
			else if (mimeMultiPart3.Parts.Count == 1 && string.IsNullOrEmpty(_body))
			{
				_message.Content = mimeMultiPart3.Parts[0];
			}
			else
			{
				_message.Content = mimeMultiPart3;
			}
		}
		if (_bodyView != null && _bodyTransferEncoding != TransferEncoding.Unknown)
		{
			_bodyView.TransferEncoding = _bodyTransferEncoding;
		}
	}

	internal void Send(BaseWriter writer, bool sendEnvelope, bool allowUnicode)
	{
		SetContent(allowUnicode);
		_message.Send(writer, sendEnvelope, allowUnicode);
	}

	internal IAsyncResult BeginSend(BaseWriter writer, bool sendEnvelope, bool allowUnicode, AsyncCallback callback, object state)
	{
		SetContent(allowUnicode);
		return _message.BeginSend(writer, sendEnvelope, allowUnicode, callback, state);
	}

	internal void EndSend(IAsyncResult asyncResult)
	{
		_message.EndSend(asyncResult);
	}

	internal string BuildDeliveryStatusNotificationString()
	{
		if (_deliveryStatusNotification != 0)
		{
			StringBuilder stringBuilder = new StringBuilder(" NOTIFY=");
			bool flag = false;
			if (_deliveryStatusNotification == DeliveryNotificationOptions.Never)
			{
				stringBuilder.Append("NEVER");
				return stringBuilder.ToString();
			}
			if ((_deliveryStatusNotification & DeliveryNotificationOptions.OnSuccess) > DeliveryNotificationOptions.None)
			{
				stringBuilder.Append("SUCCESS");
				flag = true;
			}
			if ((_deliveryStatusNotification & DeliveryNotificationOptions.OnFailure) > DeliveryNotificationOptions.None)
			{
				if (flag)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append("FAILURE");
				flag = true;
			}
			if ((_deliveryStatusNotification & DeliveryNotificationOptions.Delay) > DeliveryNotificationOptions.None)
			{
				if (flag)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append("DELAY");
			}
			return stringBuilder.ToString();
		}
		return string.Empty;
	}
}
