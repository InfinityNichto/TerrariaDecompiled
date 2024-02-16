using System.Runtime.InteropServices;

namespace System.Threading;

internal sealed class UnmanagedThreadPoolWorkItem : IThreadPoolWorkItem
{
	private readonly IntPtr _callback;

	private readonly IntPtr _state;

	public UnmanagedThreadPoolWorkItem(IntPtr callback, IntPtr state)
	{
		_callback = callback;
		_state = state;
	}

	void IThreadPoolWorkItem.Execute()
	{
		ExecuteUnmanagedThreadPoolWorkItem(_callback, _state);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void ExecuteUnmanagedThreadPoolWorkItem(IntPtr callback, IntPtr state);
}
