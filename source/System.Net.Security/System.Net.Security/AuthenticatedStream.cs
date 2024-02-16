using System.IO;
using System.Threading.Tasks;

namespace System.Net.Security;

public abstract class AuthenticatedStream : Stream
{
	private readonly Stream _innerStream;

	private readonly bool _leaveStreamOpen;

	public bool LeaveInnerStreamOpen => _leaveStreamOpen;

	protected Stream InnerStream => _innerStream;

	public abstract bool IsAuthenticated { get; }

	public abstract bool IsMutuallyAuthenticated { get; }

	public abstract bool IsEncrypted { get; }

	public abstract bool IsSigned { get; }

	public abstract bool IsServer { get; }

	protected AuthenticatedStream(Stream innerStream, bool leaveInnerStreamOpen)
	{
		if (innerStream == null || innerStream == Stream.Null)
		{
			throw new ArgumentNullException("innerStream");
		}
		if (!innerStream.CanRead || !innerStream.CanWrite)
		{
			throw new ArgumentException(System.SR.net_io_must_be_rw_stream, "innerStream");
		}
		_innerStream = innerStream;
		_leaveStreamOpen = leaveInnerStreamOpen;
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				if (_leaveStreamOpen)
				{
					_innerStream.Flush();
				}
				else
				{
					_innerStream.Dispose();
				}
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override ValueTask DisposeAsync()
	{
		try
		{
			ValueTask result = (_leaveStreamOpen ? new ValueTask(_innerStream.FlushAsync()) : _innerStream.DisposeAsync());
			GC.SuppressFinalize(this);
			return result;
		}
		catch (Exception exception)
		{
			return ValueTask.FromException(exception);
		}
	}
}
