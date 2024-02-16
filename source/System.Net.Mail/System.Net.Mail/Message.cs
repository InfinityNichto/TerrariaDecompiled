using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Text;

namespace System.Net.Mail;

internal sealed class Message
{
	internal sealed class EmptySendContext
	{
		internal System.Net.LazyAsyncResult _result;

		internal BaseWriter _writer;

		internal EmptySendContext(BaseWriter writer, System.Net.LazyAsyncResult result)
		{
			_writer = writer;
			_result = result;
		}
	}

	private MailAddress _from;

	private MailAddress _sender;

	private MailAddressCollection _replyToList;

	private MailAddress _replyTo;

	private MailAddressCollection _to;

	private MailAddressCollection _cc;

	private MailAddressCollection _bcc;

	private MimeBasePart _content;

	private HeaderCollection _headers;

	private HeaderCollection _envelopeHeaders;

	private string _subject;

	private Encoding _subjectEncoding;

	private Encoding _headersEncoding;

	private MailPriority _priority = (MailPriority)(-1);

	public MailPriority Priority
	{
		get
		{
			if (_priority != (MailPriority)(-1))
			{
				return _priority;
			}
			return MailPriority.Normal;
		}
		set
		{
			_priority = value;
		}
	}

	internal MailAddress From
	{
		get
		{
			return _from;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_from = value;
		}
	}

	internal MailAddress Sender
	{
		get
		{
			return _sender;
		}
		set
		{
			_sender = value;
		}
	}

	internal MailAddress ReplyTo
	{
		get
		{
			return _replyTo;
		}
		set
		{
			_replyTo = value;
		}
	}

	internal MailAddressCollection ReplyToList => _replyToList ?? (_replyToList = new MailAddressCollection());

	internal MailAddressCollection To => _to ?? (_to = new MailAddressCollection());

	internal MailAddressCollection Bcc => _bcc ?? (_bcc = new MailAddressCollection());

	internal MailAddressCollection CC => _cc ?? (_cc = new MailAddressCollection());

	internal string Subject
	{
		get
		{
			return _subject;
		}
		set
		{
			Encoding encoding = null;
			try
			{
				encoding = MimeBasePart.DecodeEncoding(value);
			}
			catch (ArgumentException)
			{
			}
			if (encoding != null && value != null)
			{
				try
				{
					value = MimeBasePart.DecodeHeaderValue(value);
					_subjectEncoding = _subjectEncoding ?? encoding;
				}
				catch (FormatException)
				{
				}
			}
			if (value != null && MailBnfHelper.HasCROrLF(value))
			{
				throw new ArgumentException(System.SR.MailSubjectInvalidFormat);
			}
			_subject = value;
			if (_subject != null)
			{
				_subject = _subject.Normalize(NormalizationForm.FormC);
				if (_subjectEncoding == null && !MimeBasePart.IsAscii(_subject, permitCROrLF: false))
				{
					_subjectEncoding = Encoding.GetEncoding("utf-8");
				}
			}
		}
	}

	internal Encoding SubjectEncoding
	{
		get
		{
			return _subjectEncoding;
		}
		set
		{
			_subjectEncoding = value;
		}
	}

	internal HeaderCollection Headers
	{
		get
		{
			if (_headers == null)
			{
				_headers = new HeaderCollection();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Associate(this, _headers, "Headers");
				}
			}
			return _headers;
		}
	}

	internal Encoding HeadersEncoding
	{
		get
		{
			return _headersEncoding;
		}
		set
		{
			_headersEncoding = value;
		}
	}

	internal HeaderCollection EnvelopeHeaders
	{
		get
		{
			if (_envelopeHeaders == null)
			{
				_envelopeHeaders = new HeaderCollection();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Associate(this, _envelopeHeaders, "EnvelopeHeaders");
				}
			}
			return _envelopeHeaders;
		}
	}

	internal MimeBasePart Content
	{
		get
		{
			return _content;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_content = value;
		}
	}

	internal Message()
	{
	}

	internal Message(string from, string to)
		: this()
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
		_from = new MailAddress(from);
		_to = new MailAddressCollection { to };
	}

	internal Message(MailAddress from, MailAddress to)
		: this()
	{
		_from = from;
		To.Add(to);
	}

	internal void EmptySendCallback(IAsyncResult result)
	{
		Exception result2 = null;
		if (!result.CompletedSynchronously)
		{
			EmptySendContext emptySendContext = (EmptySendContext)result.AsyncState;
			try
			{
				emptySendContext._writer.EndGetContentStream(result).Close();
			}
			catch (Exception ex)
			{
				result2 = ex;
			}
			emptySendContext._result.InvokeCallback(result2);
		}
	}

	internal IAsyncResult BeginSend(BaseWriter writer, bool sendEnvelope, bool allowUnicode, AsyncCallback callback, object state)
	{
		PrepareHeaders(sendEnvelope, allowUnicode);
		writer.WriteHeaders(Headers, allowUnicode);
		if (Content != null)
		{
			return Content.BeginSend(writer, callback, allowUnicode, state);
		}
		System.Net.LazyAsyncResult lazyAsyncResult = new System.Net.LazyAsyncResult(this, state, callback);
		IAsyncResult asyncResult = writer.BeginGetContentStream(EmptySendCallback, new EmptySendContext(writer, lazyAsyncResult));
		if (asyncResult.CompletedSynchronously)
		{
			writer.EndGetContentStream(asyncResult).Close();
			lazyAsyncResult.InvokeCallback();
		}
		return lazyAsyncResult;
	}

	internal void EndSend(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		if (Content != null)
		{
			Content.EndSend(asyncResult);
			return;
		}
		if (!(asyncResult is System.Net.LazyAsyncResult lazyAsyncResult) || lazyAsyncResult.AsyncObject != this)
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult);
		}
		if (lazyAsyncResult.EndCalled)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndSend"));
		}
		lazyAsyncResult.InternalWaitForCompletion();
		lazyAsyncResult.EndCalled = true;
		if (lazyAsyncResult.Result is Exception source)
		{
			ExceptionDispatchInfo.Throw(source);
		}
	}

	internal void Send(BaseWriter writer, bool sendEnvelope, bool allowUnicode)
	{
		if (sendEnvelope)
		{
			PrepareEnvelopeHeaders(sendEnvelope, allowUnicode);
			writer.WriteHeaders(EnvelopeHeaders, allowUnicode);
		}
		PrepareHeaders(sendEnvelope, allowUnicode);
		writer.WriteHeaders(Headers, allowUnicode);
		if (Content != null)
		{
			Content.Send(writer, allowUnicode);
		}
		else
		{
			writer.GetContentStream().Close();
		}
	}

	internal void PrepareEnvelopeHeaders(bool sendEnvelope, bool allowUnicode)
	{
		if (_headersEncoding == null)
		{
			_headersEncoding = Encoding.GetEncoding("utf-8");
		}
		EncodeHeaders(EnvelopeHeaders, allowUnicode);
		string @string = MailHeaderInfo.GetString(MailHeaderID.XSender);
		if (!IsHeaderSet(@string))
		{
			MailAddress mailAddress = Sender ?? From;
			EnvelopeHeaders.InternalSet(@string, mailAddress.Encode(@string.Length, allowUnicode));
		}
		string string2 = MailHeaderInfo.GetString(MailHeaderID.XReceiver);
		EnvelopeHeaders.Remove(string2);
		foreach (MailAddress item in To)
		{
			EnvelopeHeaders.InternalAdd(string2, item.Encode(string2.Length, allowUnicode));
		}
		foreach (MailAddress item2 in CC)
		{
			EnvelopeHeaders.InternalAdd(string2, item2.Encode(string2.Length, allowUnicode));
		}
		foreach (MailAddress item3 in Bcc)
		{
			EnvelopeHeaders.InternalAdd(string2, item3.Encode(string2.Length, allowUnicode));
		}
	}

	internal void PrepareHeaders(bool sendEnvelope, bool allowUnicode)
	{
		if (_headersEncoding == null)
		{
			_headersEncoding = Encoding.GetEncoding("utf-8");
		}
		Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentType));
		Headers[MailHeaderInfo.GetString(MailHeaderID.MimeVersion)] = "1.0";
		string @string = MailHeaderInfo.GetString(MailHeaderID.Sender);
		if (Sender != null)
		{
			Headers.InternalAdd(@string, Sender.Encode(@string.Length, allowUnicode));
		}
		else
		{
			Headers.Remove(@string);
		}
		@string = MailHeaderInfo.GetString(MailHeaderID.From);
		Headers.InternalAdd(@string, From.Encode(@string.Length, allowUnicode));
		@string = MailHeaderInfo.GetString(MailHeaderID.To);
		if (To.Count > 0)
		{
			Headers.InternalAdd(@string, To.Encode(@string.Length, allowUnicode));
		}
		else
		{
			Headers.Remove(@string);
		}
		@string = MailHeaderInfo.GetString(MailHeaderID.Cc);
		if (CC.Count > 0)
		{
			Headers.InternalAdd(@string, CC.Encode(@string.Length, allowUnicode));
		}
		else
		{
			Headers.Remove(@string);
		}
		@string = MailHeaderInfo.GetString(MailHeaderID.ReplyTo);
		if (ReplyTo != null)
		{
			Headers.InternalAdd(@string, ReplyTo.Encode(@string.Length, allowUnicode));
		}
		else if (ReplyToList.Count > 0)
		{
			Headers.InternalAdd(@string, ReplyToList.Encode(@string.Length, allowUnicode));
		}
		else
		{
			Headers.Remove(@string);
		}
		Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Bcc));
		if (_priority == MailPriority.High)
		{
			Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "1";
			Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "urgent";
			Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "high";
		}
		else if (_priority == MailPriority.Low)
		{
			Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "5";
			Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "non-urgent";
			Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "low";
		}
		else if (_priority != (MailPriority)(-1))
		{
			Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.XPriority));
			Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Priority));
			Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Importance));
		}
		Headers.InternalAdd(MailHeaderInfo.GetString(MailHeaderID.Date), MailBnfHelper.GetDateTimeString(DateTime.Now, null));
		@string = MailHeaderInfo.GetString(MailHeaderID.Subject);
		if (!string.IsNullOrEmpty(_subject))
		{
			if (allowUnicode)
			{
				Headers.InternalAdd(@string, _subject);
			}
			else
			{
				Headers.InternalAdd(@string, MimeBasePart.EncodeHeaderValue(_subject, _subjectEncoding, MimeBasePart.ShouldUseBase64Encoding(_subjectEncoding), @string.Length));
			}
		}
		else
		{
			Headers.Remove(@string);
		}
		EncodeHeaders(_headers, allowUnicode);
	}

	internal void EncodeHeaders(HeaderCollection headers, bool allowUnicode)
	{
		if (_headersEncoding == null)
		{
			_headersEncoding = Encoding.GetEncoding("utf-8");
		}
		for (int i = 0; i < headers.Count; i++)
		{
			string key = headers.GetKey(i);
			if (!MailHeaderInfo.IsUserSettable(key))
			{
				continue;
			}
			string[] values = headers.GetValues(key);
			string empty = string.Empty;
			for (int j = 0; j < values.Length; j++)
			{
				empty = ((!MimeBasePart.IsAscii(values[j], permitCROrLF: false) && (!allowUnicode || !MailHeaderInfo.AllowsUnicode(key) || MailBnfHelper.HasCROrLF(values[j]))) ? MimeBasePart.EncodeHeaderValue(values[j], _headersEncoding, MimeBasePart.ShouldUseBase64Encoding(_headersEncoding), key.Length) : values[j]);
				if (j == 0)
				{
					headers.Set(key, empty);
				}
				else
				{
					headers.Add(key, empty);
				}
			}
		}
	}

	private bool IsHeaderSet(string headerName)
	{
		for (int i = 0; i < Headers.Count; i++)
		{
			if (string.Equals(Headers.GetKey(i), headerName, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
