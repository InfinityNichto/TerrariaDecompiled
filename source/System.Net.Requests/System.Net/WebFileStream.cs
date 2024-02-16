using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class WebFileStream : FileStream
{
	private readonly FileWebRequest _request;

	public WebFileStream(FileWebRequest request, string path, FileMode mode, FileAccess access, FileShare sharing)
		: base(path, mode, access, sharing)
	{
		_request = request;
	}

	public WebFileStream(FileWebRequest request, string path, FileMode mode, FileAccess access, FileShare sharing, int length, bool async)
		: base(path, mode, access, sharing, length, async)
	{
		_request = request;
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				_request?.UnblockReader();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	internal void Abort()
	{
		SafeFileHandle.Close();
	}

	public override int Read(byte[] buffer, int offset, int size)
	{
		CheckAborted();
		try
		{
			return base.Read(buffer, offset, size);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override void Write(byte[] buffer, int offset, int size)
	{
		CheckAborted();
		try
		{
			base.Write(buffer, offset, size);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
	{
		CheckAborted();
		try
		{
			return base.BeginRead(buffer, offset, size, callback, state);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override int EndRead(IAsyncResult ar)
	{
		try
		{
			return base.EndRead(ar);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
	{
		CheckAborted();
		try
		{
			return base.BeginWrite(buffer, offset, size, callback, state);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override void EndWrite(IAsyncResult ar)
	{
		try
		{
			base.EndWrite(ar);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CheckAborted();
		try
		{
			return base.ReadAsync(buffer, offset, count, cancellationToken);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CheckAborted();
		try
		{
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		CheckAborted();
		try
		{
			return base.CopyToAsync(destination, bufferSize, cancellationToken);
		}
		catch
		{
			CheckAborted();
			throw;
		}
	}

	private void CheckAborted()
	{
		if (_request.Aborted)
		{
			throw new WebException(System.SR.Format(System.SR.net_requestaborted, WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
		}
	}
}
