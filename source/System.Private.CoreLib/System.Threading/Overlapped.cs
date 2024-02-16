namespace System.Threading;

public class Overlapped
{
	private OverlappedData _overlappedData;

	public IAsyncResult? AsyncResult
	{
		get
		{
			return _overlappedData._asyncResult;
		}
		set
		{
			_overlappedData._asyncResult = value;
		}
	}

	public int OffsetLow
	{
		get
		{
			return _overlappedData.OffsetLow;
		}
		set
		{
			_overlappedData.OffsetLow = value;
		}
	}

	public int OffsetHigh
	{
		get
		{
			return _overlappedData.OffsetHigh;
		}
		set
		{
			_overlappedData.OffsetHigh = value;
		}
	}

	[Obsolete("Overlapped.EventHandle is not 64-bit compatible and has been deprecated. Use EventHandleIntPtr instead.")]
	public int EventHandle
	{
		get
		{
			return EventHandleIntPtr.ToInt32();
		}
		set
		{
			EventHandleIntPtr = new IntPtr(value);
		}
	}

	public IntPtr EventHandleIntPtr
	{
		get
		{
			return _overlappedData.EventHandle;
		}
		set
		{
			_overlappedData.EventHandle = value;
		}
	}

	public Overlapped()
	{
		_overlappedData = new OverlappedData(this);
	}

	public Overlapped(int offsetLo, int offsetHi, IntPtr hEvent, IAsyncResult? ar)
		: this()
	{
		_overlappedData.OffsetLow = offsetLo;
		_overlappedData.OffsetHigh = offsetHi;
		_overlappedData.EventHandle = hEvent;
		_overlappedData._asyncResult = ar;
	}

	[Obsolete("This constructor is not 64-bit compatible and has been deprecated. Use the constructor that accepts an IntPtr for the event handle instead.")]
	public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult? ar)
		: this(offsetLo, offsetHi, new IntPtr(hEvent), ar)
	{
	}

	[Obsolete("This overload is not safe and has been deprecated. Use Pack(IOCompletionCallback?, object?) instead.")]
	[CLSCompliant(false)]
	public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb)
	{
		return Pack(iocb, null);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
	{
		return _overlappedData.Pack(iocb, userData);
	}

	[Obsolete("This overload is not safe and has been deprecated. Use UnsafePack(IOCompletionCallback?, object?) instead.")]
	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb)
	{
		return UnsafePack(iocb, null);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
	{
		return _overlappedData.UnsafePack(iocb, userData);
	}

	[CLSCompliant(false)]
	public unsafe static Overlapped Unpack(NativeOverlapped* nativeOverlappedPtr)
	{
		if (nativeOverlappedPtr == null)
		{
			throw new ArgumentNullException("nativeOverlappedPtr");
		}
		return OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped;
	}

	[CLSCompliant(false)]
	public unsafe static void Free(NativeOverlapped* nativeOverlappedPtr)
	{
		if (nativeOverlappedPtr == null)
		{
			throw new ArgumentNullException("nativeOverlappedPtr");
		}
		OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped._overlappedData = null;
		OverlappedData.FreeNativeOverlapped(nativeOverlappedPtr);
	}

	internal bool IsUserObject(byte[] buffer)
	{
		return _overlappedData.IsUserObject(buffer);
	}
}
