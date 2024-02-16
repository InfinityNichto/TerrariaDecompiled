using System.IO;

namespace System.Net.Http;

internal abstract class HttpContentStream : HttpBaseStream
{
	protected HttpConnection _connection;

	public HttpContentStream(HttpConnection connection)
	{
		_connection = connection;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		Write(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && _connection != null)
		{
			_connection.Dispose();
			_connection = null;
		}
		base.Dispose(disposing);
	}

	protected HttpConnection GetConnectionOrThrow()
	{
		return _connection ?? ThrowObjectDisposedException();
	}

	private HttpConnection ThrowObjectDisposedException()
	{
		throw new ObjectDisposedException(GetType().Name);
	}
}
