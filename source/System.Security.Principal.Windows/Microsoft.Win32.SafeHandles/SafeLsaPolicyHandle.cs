namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeLsaPolicyHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Advapi32.LsaClose(handle) == 0;
	}
}
