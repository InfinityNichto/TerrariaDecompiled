using System.IO;
using System.Net.Mail;
using System.Runtime.ExceptionServices;

namespace System.Net.Mime;

internal sealed class MimePart : MimeBasePart, IDisposable
{
	internal sealed class MimePartContext
	{
		internal Stream _outputStream;

		internal System.Net.LazyAsyncResult _result;

		internal int _bytesLeft;

		internal BaseWriter _writer;

		internal byte[] _buffer;

		internal bool _completed;

		internal bool _completedSynchronously = true;

		internal MimePartContext(BaseWriter writer, System.Net.LazyAsyncResult result)
		{
			_writer = writer;
			_result = result;
			_buffer = new byte[17408];
		}
	}

	private Stream _stream;

	private bool _streamSet;

	private bool _streamUsedOnce;

	private AsyncCallback _readCallback;

	private AsyncCallback _writeCallback;

	internal Stream Stream => _stream;

	internal ContentDisposition ContentDisposition
	{
		get
		{
			return _contentDisposition;
		}
		set
		{
			_contentDisposition = value;
			if (value == null)
			{
				((HeaderCollection)base.Headers).InternalRemove(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition));
			}
			else
			{
				_contentDisposition.PersistIfNeeded((HeaderCollection)base.Headers, forcePersist: true);
			}
		}
	}

	internal TransferEncoding TransferEncoding
	{
		get
		{
			string text = base.Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)];
			if (text.Equals("base64", StringComparison.OrdinalIgnoreCase))
			{
				return TransferEncoding.Base64;
			}
			if (text.Equals("quoted-printable", StringComparison.OrdinalIgnoreCase))
			{
				return TransferEncoding.QuotedPrintable;
			}
			if (text.Equals("7bit", StringComparison.OrdinalIgnoreCase))
			{
				return TransferEncoding.SevenBit;
			}
			if (text.Equals("8bit", StringComparison.OrdinalIgnoreCase))
			{
				return TransferEncoding.EightBit;
			}
			return TransferEncoding.Unknown;
		}
		set
		{
			switch (value)
			{
			case TransferEncoding.Base64:
				base.Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "base64";
				break;
			case TransferEncoding.QuotedPrintable:
				base.Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "quoted-printable";
				break;
			case TransferEncoding.SevenBit:
				base.Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "7bit";
				break;
			case TransferEncoding.EightBit:
				base.Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "8bit";
				break;
			default:
				throw new NotSupportedException(System.SR.Format(System.SR.MimeTransferEncodingNotSupported, value));
			}
		}
	}

	internal MimePart()
	{
	}

	public void Dispose()
	{
		_stream?.Close();
	}

	internal void SetContent(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (_streamSet)
		{
			_stream.Close();
			_stream = null;
			_streamSet = false;
		}
		_stream = stream;
		_streamSet = true;
		_streamUsedOnce = false;
		TransferEncoding = TransferEncoding.Base64;
	}

	internal void SetContent(Stream stream, string name, string mimeType)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (mimeType != null && mimeType != string.Empty)
		{
			_contentType = new ContentType(mimeType);
		}
		if (name != null && name != string.Empty)
		{
			base.ContentType.Name = name;
		}
		SetContent(stream);
	}

	internal void SetContent(Stream stream, ContentType contentType)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		_contentType = contentType;
		SetContent(stream);
	}

	internal void Complete(IAsyncResult result, Exception e)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		if (mimePartContext._completed)
		{
			ExceptionDispatchInfo.Throw(e);
		}
		try
		{
			if (mimePartContext._outputStream != null)
			{
				mimePartContext._outputStream.Close();
			}
		}
		catch (Exception ex)
		{
			if (e == null)
			{
				e = ex;
			}
		}
		mimePartContext._completed = true;
		mimePartContext._result.InvokeCallback(e);
	}

	internal void ReadCallback(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		((MimePartContext)result.AsyncState)._completedSynchronously = false;
		try
		{
			ReadCallbackHandler(result);
		}
		catch (Exception e)
		{
			Complete(result, e);
		}
	}

	internal void ReadCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		mimePartContext._bytesLeft = Stream.EndRead(result);
		if (mimePartContext._bytesLeft > 0)
		{
			IAsyncResult asyncResult = mimePartContext._outputStream.BeginWrite(mimePartContext._buffer, 0, mimePartContext._bytesLeft, _writeCallback, mimePartContext);
			if (asyncResult.CompletedSynchronously)
			{
				WriteCallbackHandler(asyncResult);
			}
		}
		else
		{
			Complete(result, null);
		}
	}

	internal void WriteCallback(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		((MimePartContext)result.AsyncState)._completedSynchronously = false;
		try
		{
			WriteCallbackHandler(result);
		}
		catch (Exception e)
		{
			Complete(result, e);
		}
	}

	internal void WriteCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		mimePartContext._outputStream.EndWrite(result);
		IAsyncResult asyncResult = Stream.BeginRead(mimePartContext._buffer, 0, mimePartContext._buffer.Length, _readCallback, mimePartContext);
		if (asyncResult.CompletedSynchronously)
		{
			ReadCallbackHandler(asyncResult);
		}
	}

	internal Stream GetEncodedStream(Stream stream)
	{
		Stream stream2 = stream;
		if (TransferEncoding == TransferEncoding.Base64)
		{
			stream2 = new Base64Stream(stream2, new Base64WriteStateInfo());
		}
		else if (TransferEncoding == TransferEncoding.QuotedPrintable)
		{
			stream2 = new QuotedPrintableStream(stream2, encodeCRLF: true);
		}
		else if (TransferEncoding == TransferEncoding.SevenBit || TransferEncoding == TransferEncoding.EightBit)
		{
			stream2 = new EightBitStream(stream2);
		}
		return stream2;
	}

	internal void ContentStreamCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		Stream stream = mimePartContext._writer.EndGetContentStream(result);
		mimePartContext._outputStream = GetEncodedStream(stream);
		_readCallback = ReadCallback;
		_writeCallback = WriteCallback;
		IAsyncResult asyncResult = Stream.BeginRead(mimePartContext._buffer, 0, mimePartContext._buffer.Length, _readCallback, mimePartContext);
		if (asyncResult.CompletedSynchronously)
		{
			ReadCallbackHandler(asyncResult);
		}
	}

	internal void ContentStreamCallback(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		((MimePartContext)result.AsyncState)._completedSynchronously = false;
		try
		{
			ContentStreamCallbackHandler(result);
		}
		catch (Exception e)
		{
			Complete(result, e);
		}
	}

	internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, bool allowUnicode, object state)
	{
		PrepareHeaders(allowUnicode);
		writer.WriteHeaders(base.Headers, allowUnicode);
		MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
		MimePartContext state2 = new MimePartContext(writer, result);
		ResetStream();
		_streamUsedOnce = true;
		IAsyncResult asyncResult = writer.BeginGetContentStream(ContentStreamCallback, state2);
		if (asyncResult.CompletedSynchronously)
		{
			ContentStreamCallbackHandler(asyncResult);
		}
		return result;
	}

	internal override void Send(BaseWriter writer, bool allowUnicode)
	{
		if (Stream != null)
		{
			byte[] buffer = new byte[17408];
			PrepareHeaders(allowUnicode);
			writer.WriteHeaders(base.Headers, allowUnicode);
			Stream contentStream = writer.GetContentStream();
			contentStream = GetEncodedStream(contentStream);
			ResetStream();
			_streamUsedOnce = true;
			int count;
			while ((count = Stream.Read(buffer, 0, 17408)) > 0)
			{
				contentStream.Write(buffer, 0, count);
			}
			contentStream.Close();
		}
	}

	internal void ResetStream()
	{
		if (_streamUsedOnce)
		{
			if (!Stream.CanSeek)
			{
				throw new InvalidOperationException(System.SR.MimePartCantResetStream);
			}
			Stream.Seek(0L, SeekOrigin.Begin);
			_streamUsedOnce = false;
		}
	}
}
