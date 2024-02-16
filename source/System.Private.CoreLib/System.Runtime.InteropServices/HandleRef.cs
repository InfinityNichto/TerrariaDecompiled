namespace System.Runtime.InteropServices;

public readonly struct HandleRef
{
	private readonly object _wrapper;

	private readonly IntPtr _handle;

	public object? Wrapper => _wrapper;

	public IntPtr Handle => _handle;

	public HandleRef(object? wrapper, IntPtr handle)
	{
		_wrapper = wrapper;
		_handle = handle;
	}

	public static explicit operator IntPtr(HandleRef value)
	{
		return value._handle;
	}

	public static IntPtr ToIntPtr(HandleRef value)
	{
		return value._handle;
	}
}
