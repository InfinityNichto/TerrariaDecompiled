using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class SafeMsQuicListenerHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeMsQuicListenerHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		MsQuicApi.Api.ListenerCloseDelegate(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}
}
