namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeFindHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return Interop.Kernel32.FindClose(handle);
	}
}
