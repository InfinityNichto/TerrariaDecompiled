using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles;

public sealed class MemoryMappedViewAccessor : UnmanagedMemoryAccessor
{
	private readonly MemoryMappedView _view;

	public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle => _view.ViewHandle;

	public long PointerOffset => _view.PointerOffset;

	internal MemoryMappedViewAccessor(MemoryMappedView view)
	{
		_view = view;
		Initialize(_view.ViewHandle, _view.PointerOffset, _view.Size, MemoryMappedFile.GetFileAccess(_view.Access));
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

	public void Flush()
	{
		if (!base.IsOpen)
		{
			throw new ObjectDisposedException("MemoryMappedViewAccessor", System.SR.ObjectDisposed_ViewAccessorClosed);
		}
		_view.Flush((UIntPtr)(ulong)base.Capacity);
	}
}
