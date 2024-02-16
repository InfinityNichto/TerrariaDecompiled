namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeBCryptHashHandle : Microsoft.Win32.SafeHandles.SafeBCryptHandle
{
	protected sealed override bool ReleaseHandle()
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDestroyHash(handle);
		return nTSTATUS == global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS;
	}
}
