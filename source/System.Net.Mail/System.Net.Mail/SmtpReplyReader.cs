namespace System.Net.Mail;

internal sealed class SmtpReplyReader
{
	private readonly SmtpReplyReaderFactory _reader;

	internal SmtpReplyReader(SmtpReplyReaderFactory reader)
	{
		_reader = reader;
	}

	internal IAsyncResult BeginReadLines(AsyncCallback callback, object state)
	{
		return _reader.BeginReadLines(this, callback, state);
	}

	internal IAsyncResult BeginReadLine(AsyncCallback callback, object state)
	{
		return _reader.BeginReadLine(this, callback, state);
	}

	public void Close()
	{
		_reader.Close(this);
	}

	internal LineInfo[] EndReadLines(IAsyncResult result)
	{
		return _reader.EndReadLines(result);
	}

	internal LineInfo EndReadLine(IAsyncResult result)
	{
		return _reader.EndReadLine(result);
	}

	internal LineInfo[] ReadLines()
	{
		return _reader.ReadLines(this);
	}

	internal LineInfo ReadLine()
	{
		return _reader.ReadLine(this);
	}
}
