namespace Microsoft.Win32.SafeHandles;

public sealed class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public override bool IsInvalid => base.IsInvalid;

	public SafeMemoryMappedFileHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}
}
