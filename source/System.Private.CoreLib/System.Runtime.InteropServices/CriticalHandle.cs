using System.Runtime.ConstrainedExecution;

namespace System.Runtime.InteropServices;

public abstract class CriticalHandle : CriticalFinalizerObject, IDisposable
{
	protected IntPtr handle;

	private bool _isClosed;

	public bool IsClosed => _isClosed;

	public abstract bool IsInvalid { get; }

	protected CriticalHandle(IntPtr invalidHandleValue)
	{
		handle = invalidHandleValue;
	}

	~CriticalHandle()
	{
		Dispose(disposing: false);
	}

	private void Cleanup()
	{
		if (!IsClosed)
		{
			_isClosed = true;
			if (!IsInvalid)
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				ReleaseHandle();
				Marshal.SetLastPInvokeError(lastPInvokeError);
				GC.SuppressFinalize(this);
			}
		}
	}

	protected void SetHandle(IntPtr handle)
	{
		this.handle = handle;
	}

	public void Close()
	{
		Dispose(disposing: true);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		Cleanup();
	}

	public void SetHandleAsInvalid()
	{
		_isClosed = true;
		GC.SuppressFinalize(this);
	}

	protected abstract bool ReleaseHandle();
}
