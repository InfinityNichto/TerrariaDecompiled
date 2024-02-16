using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Runtime.ExceptionServices;

namespace System.Net.Mime;

internal abstract class BaseWriter
{
	private static readonly AsyncCallback s_onWrite = OnWrite;

	protected static readonly byte[] s_crlf = new byte[2] { 13, 10 };

	protected readonly BufferBuilder _bufferBuilder;

	protected readonly Stream _stream;

	private readonly EventHandler _onCloseHandler;

	private readonly bool _shouldEncodeLeadingDots;

	private readonly int _lineLength;

	protected Stream _contentStream;

	protected bool _isInContent;

	protected BaseWriter(Stream stream, bool shouldEncodeLeadingDots)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		_stream = stream;
		_shouldEncodeLeadingDots = shouldEncodeLeadingDots;
		_onCloseHandler = OnClose;
		_bufferBuilder = new BufferBuilder();
		_lineLength = 76;
	}

	internal abstract void WriteHeaders(NameValueCollection headers, bool allowUnicode);

	internal void WriteHeader(string name, string value, bool allowUnicode)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (_isInContent)
		{
			throw new InvalidOperationException(System.SR.MailWriterIsInContent);
		}
		CheckBoundary();
		_bufferBuilder.Append(name);
		_bufferBuilder.Append(": ");
		WriteAndFold(value, name.Length + 2, allowUnicode);
		_bufferBuilder.Append(s_crlf);
	}

	private void WriteAndFold(string value, int charsAlreadyOnLine, bool allowUnicode)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < value.Length; i++)
		{
			if (MailBnfHelper.IsFWSAt(value, i))
			{
				i += 2;
				_bufferBuilder.Append(value, num2, i - num2, allowUnicode);
				num2 = i;
				num = i;
				charsAlreadyOnLine = 0;
			}
			else if (i - num2 > _lineLength - charsAlreadyOnLine && num != num2)
			{
				_bufferBuilder.Append(value, num2, num - num2, allowUnicode);
				_bufferBuilder.Append(s_crlf);
				num2 = num;
				charsAlreadyOnLine = 0;
			}
			else if (value[i] == ' ' || value[i] == '\t')
			{
				num = i;
			}
		}
		if (value.Length - num2 > 0)
		{
			_bufferBuilder.Append(value, num2, value.Length - num2, allowUnicode);
		}
	}

	internal Stream GetContentStream()
	{
		return GetContentStream(null);
	}

	private Stream GetContentStream(MultiAsyncResult multiResult)
	{
		if (_isInContent)
		{
			throw new InvalidOperationException(System.SR.MailWriterIsInContent);
		}
		_isInContent = true;
		CheckBoundary();
		_bufferBuilder.Append(s_crlf);
		Flush(multiResult);
		return _contentStream = new ClosableStream(new EightBitStream(_stream, _shouldEncodeLeadingDots), _onCloseHandler);
	}

	internal IAsyncResult BeginGetContentStream(AsyncCallback callback, object state)
	{
		MultiAsyncResult multiAsyncResult = new MultiAsyncResult(this, callback, state);
		Stream contentStream = GetContentStream(multiAsyncResult);
		if (!(multiAsyncResult.Result is Exception))
		{
			multiAsyncResult.Result = contentStream;
		}
		multiAsyncResult.CompleteSequence();
		return multiAsyncResult;
	}

	internal Stream EndGetContentStream(IAsyncResult result)
	{
		object obj = MultiAsyncResult.End(result);
		if (obj is Exception source)
		{
			ExceptionDispatchInfo.Throw(source);
		}
		return (Stream)obj;
	}

	protected void Flush(MultiAsyncResult multiResult)
	{
		if (_bufferBuilder.Length <= 0)
		{
			return;
		}
		if (multiResult != null)
		{
			multiResult.Enter();
			IAsyncResult asyncResult = _stream.BeginWrite(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length, s_onWrite, multiResult);
			if (asyncResult.CompletedSynchronously)
			{
				_stream.EndWrite(asyncResult);
				multiResult.Leave();
			}
		}
		else
		{
			_stream.Write(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length);
		}
		_bufferBuilder.Reset();
	}

	protected static void OnWrite(IAsyncResult result)
	{
		if (!result.CompletedSynchronously)
		{
			MultiAsyncResult multiAsyncResult = (MultiAsyncResult)result.AsyncState;
			BaseWriter baseWriter = (BaseWriter)multiAsyncResult.Context;
			try
			{
				baseWriter._stream.EndWrite(result);
				multiAsyncResult.Leave();
			}
			catch (Exception result2)
			{
				multiAsyncResult.Leave(result2);
			}
		}
	}

	internal abstract void Close();

	protected abstract void OnClose(object sender, EventArgs args);

	protected virtual void CheckBoundary()
	{
	}
}
