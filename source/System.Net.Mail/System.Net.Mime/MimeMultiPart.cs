using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Net.Mime;

internal sealed class MimeMultiPart : MimeBasePart
{
	internal sealed class MimePartContext
	{
		internal IEnumerator<MimeBasePart> _partsEnumerator;

		internal Stream _outputStream;

		internal System.Net.LazyAsyncResult _result;

		internal BaseWriter _writer;

		internal bool _completed;

		internal bool _completedSynchronously = true;

		internal MimePartContext(BaseWriter writer, System.Net.LazyAsyncResult result, IEnumerator<MimeBasePart> partsEnumerator)
		{
			_writer = writer;
			_result = result;
			_partsEnumerator = partsEnumerator;
		}
	}

	private Collection<MimeBasePart> _parts;

	private static int s_boundary;

	private AsyncCallback _mimePartSentCallback;

	private bool _allowUnicode;

	internal MimeMultiPartType MimeMultiPartType
	{
		set
		{
			if (value > MimeMultiPartType.Related || value < MimeMultiPartType.Mixed)
			{
				throw new NotSupportedException(value.ToString());
			}
			SetType(value);
		}
	}

	internal Collection<MimeBasePart> Parts
	{
		get
		{
			if (_parts == null)
			{
				_parts = new Collection<MimeBasePart>();
			}
			return _parts;
		}
	}

	internal MimeMultiPart(MimeMultiPartType type)
	{
		MimeMultiPartType = type;
	}

	private void SetType(MimeMultiPartType type)
	{
		base.ContentType.MediaType = "multipart/" + type.ToString().ToLowerInvariant();
		base.ContentType.Boundary = GetNextBoundary();
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
			mimePartContext._outputStream.Close();
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

	internal void MimeWriterCloseCallback(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		((MimePartContext)result.AsyncState)._completedSynchronously = false;
		try
		{
			MimeWriterCloseCallbackHandler(result);
		}
		catch (Exception e)
		{
			Complete(result, e);
		}
	}

	private void MimeWriterCloseCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		((MimeWriter)mimePartContext._writer).EndClose(result);
		Complete(result, null);
	}

	internal void MimePartSentCallback(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		((MimePartContext)result.AsyncState)._completedSynchronously = false;
		try
		{
			MimePartSentCallbackHandler(result);
		}
		catch (Exception e)
		{
			Complete(result, e);
		}
	}

	private void MimePartSentCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		MimeBasePart current = mimePartContext._partsEnumerator.Current;
		current.EndSend(result);
		if (mimePartContext._partsEnumerator.MoveNext())
		{
			current = mimePartContext._partsEnumerator.Current;
			IAsyncResult asyncResult = current.BeginSend(mimePartContext._writer, _mimePartSentCallback, _allowUnicode, mimePartContext);
			if (asyncResult.CompletedSynchronously)
			{
				MimePartSentCallbackHandler(asyncResult);
			}
		}
		else
		{
			IAsyncResult asyncResult2 = ((MimeWriter)mimePartContext._writer).BeginClose(MimeWriterCloseCallback, mimePartContext);
			if (asyncResult2.CompletedSynchronously)
			{
				MimeWriterCloseCallbackHandler(asyncResult2);
			}
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

	private void ContentStreamCallbackHandler(IAsyncResult result)
	{
		MimePartContext mimePartContext = (MimePartContext)result.AsyncState;
		mimePartContext._outputStream = mimePartContext._writer.EndGetContentStream(result);
		mimePartContext._writer = new MimeWriter(mimePartContext._outputStream, base.ContentType.Boundary);
		if (mimePartContext._partsEnumerator.MoveNext())
		{
			MimeBasePart current = mimePartContext._partsEnumerator.Current;
			_mimePartSentCallback = MimePartSentCallback;
			IAsyncResult asyncResult = current.BeginSend(mimePartContext._writer, _mimePartSentCallback, _allowUnicode, mimePartContext);
			if (asyncResult.CompletedSynchronously)
			{
				MimePartSentCallbackHandler(asyncResult);
			}
		}
		else
		{
			IAsyncResult asyncResult2 = ((MimeWriter)mimePartContext._writer).BeginClose(MimeWriterCloseCallback, mimePartContext);
			if (asyncResult2.CompletedSynchronously)
			{
				MimeWriterCloseCallbackHandler(asyncResult2);
			}
		}
	}

	internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, bool allowUnicode, object state)
	{
		_allowUnicode = allowUnicode;
		PrepareHeaders(allowUnicode);
		writer.WriteHeaders(base.Headers, allowUnicode);
		MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
		MimePartContext state2 = new MimePartContext(writer, result, Parts.GetEnumerator());
		IAsyncResult asyncResult = writer.BeginGetContentStream(ContentStreamCallback, state2);
		if (asyncResult.CompletedSynchronously)
		{
			ContentStreamCallbackHandler(asyncResult);
		}
		return result;
	}

	internal override void Send(BaseWriter writer, bool allowUnicode)
	{
		PrepareHeaders(allowUnicode);
		writer.WriteHeaders(base.Headers, allowUnicode);
		Stream contentStream = writer.GetContentStream();
		MimeWriter mimeWriter = new MimeWriter(contentStream, base.ContentType.Boundary);
		foreach (MimeBasePart part in Parts)
		{
			part.Send(mimeWriter, allowUnicode);
		}
		mimeWriter.Close();
		contentStream.Close();
	}

	internal string GetNextBoundary()
	{
		int value = Interlocked.Increment(ref s_boundary) - 1;
		return $"--boundary_{value}_{Guid.NewGuid()}";
	}
}
