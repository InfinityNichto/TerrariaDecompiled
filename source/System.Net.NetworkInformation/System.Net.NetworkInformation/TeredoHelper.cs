using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.NetworkInformation;

internal sealed class TeredoHelper
{
	private readonly Action<object> _callback;

	private readonly object _state;

	private bool _runCallbackCalled;

	private GCHandle _gcHandle;

	private SafeCancelMibChangeNotify _cancelHandle;

	private TeredoHelper(Action<object> callback, object state)
	{
		_callback = callback;
		_state = state;
		_gcHandle = GCHandle.Alloc(this);
	}

	public unsafe static bool UnsafeNotifyStableUnicastIpAddressTable(Action<object> callback, object state)
	{
		TeredoHelper teredoHelper = new TeredoHelper(callback, state);
		try
		{
			SafeFreeMibTable table;
			uint num = global::Interop.IpHlpApi.NotifyStableUnicastIpAddressTable(AddressFamily.Unspecified, out table, (delegate* unmanaged<IntPtr, IntPtr, void>)(delegate*<IntPtr, IntPtr, void>)(&OnStabilized), GCHandle.ToIntPtr(teredoHelper._gcHandle), out teredoHelper._cancelHandle);
			table.Dispose();
			switch (num)
			{
			case 997u:
				teredoHelper = null;
				return false;
			default:
				throw new Win32Exception((int)num);
			case 0u:
				return true;
			}
		}
		finally
		{
			teredoHelper?.Dispose();
		}
	}

	private void Dispose()
	{
		_cancelHandle?.Dispose();
		if (_gcHandle.IsAllocated)
		{
			_gcHandle.Free();
		}
	}

	[UnmanagedCallersOnly]
	private static void OnStabilized(IntPtr context, IntPtr table)
	{
		global::Interop.IpHlpApi.FreeMibTable(table);
		TeredoHelper teredoHelper = (TeredoHelper)GCHandle.FromIntPtr(context).Target;
		if (teredoHelper._runCallbackCalled)
		{
			return;
		}
		lock (teredoHelper)
		{
			if (!teredoHelper._runCallbackCalled)
			{
				teredoHelper._runCallbackCalled = true;
				ThreadPool.QueueUserWorkItem(delegate(object o)
				{
					TeredoHelper teredoHelper2 = (TeredoHelper)o;
					teredoHelper2.Dispose();
					teredoHelper2._callback(teredoHelper2._state);
				}, teredoHelper);
			}
		}
	}
}
