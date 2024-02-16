using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles;

public sealed class MemoryMappedViewStream : UnmanagedMemoryStream
{
	private readonly MemoryMappedView _view;

	public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle => _view.ViewHandle;

	public long PointerOffset => _view.PointerOffset;

	internal MemoryMappedViewStream(MemoryMappedView view)
	{
		_view = view;
		Initialize(_view.ViewHandle, _view.PointerOffset, _view.Size, MemoryMappedFile.GetFileAccess(_view.Access));
	}

	public override void SetLength(long value)
	{
		if (value < 0)
		{
			throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		throw new NotSupportedException(System.SR.NotSupported_MMViewStreamsFixedLength);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && !_view.IsClosed)
			{
				Flush();
			}
		}
		finally
		{
			try
			{
				_view.Dispose();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
	}

	public override void Flush()
	{
		if (!CanSeek)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_StreamIsClosed);
		}
		_view.Flush((UIntPtr)(ulong)base.Capacity);
	}
}
