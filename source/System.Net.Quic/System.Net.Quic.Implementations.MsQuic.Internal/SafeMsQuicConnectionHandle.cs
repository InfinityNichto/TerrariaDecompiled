using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class SafeMsQuicConnectionHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeMsQuicConnectionHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	public SafeMsQuicConnectionHandle(IntPtr connectionHandle)
		: this()
	{
		SetHandle(connectionHandle);
	}

	protected override bool ReleaseHandle()
	{
		MsQuicApi.Api.ConnectionCloseDelegate(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}
}
