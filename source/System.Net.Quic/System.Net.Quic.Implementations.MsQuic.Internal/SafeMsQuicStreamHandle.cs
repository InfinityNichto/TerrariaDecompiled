using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class SafeMsQuicStreamHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeMsQuicStreamHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	public SafeMsQuicStreamHandle(IntPtr streamHandle)
		: this()
	{
		SetHandle(streamHandle);
	}

	protected override bool ReleaseHandle()
	{
		MsQuicApi.Api.StreamCloseDelegate(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}
}
