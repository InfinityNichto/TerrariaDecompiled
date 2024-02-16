using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal sealed class SafeSspiAuthDataHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeSspiAuthDataHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.SspiFreeAuthIdentity(handle) == global::Interop.SECURITY_STATUS.OK;
	}
}
