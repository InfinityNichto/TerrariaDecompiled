using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class SafeMsQuicRegistrationHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeMsQuicRegistrationHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		MsQuicApi.Api.RegistrationCloseDelegate(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}
}
