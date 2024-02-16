using System.IO;
using System.Threading;

namespace System.Net;

internal sealed class ClosableStream : DelegatedStream
{
	private readonly EventHandler _onClose;

	private int _closed;

	internal ClosableStream(Stream stream, EventHandler onClose)
		: base(stream)
	{
		_onClose = onClose;
	}

	public override void Close()
	{
		if (Interlocked.Increment(ref _closed) == 1)
		{
			_onClose?.Invoke(this, new EventArgs());
		}
	}
}
