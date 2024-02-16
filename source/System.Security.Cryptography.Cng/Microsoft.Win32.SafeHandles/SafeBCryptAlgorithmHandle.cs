namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeBCryptAlgorithmHandle : Microsoft.Win32.SafeHandles.SafeBCryptHandle
{
	protected sealed override bool ReleaseHandle()
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCloseAlgorithmProvider(handle, 0);
		return nTSTATUS == global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS;
	}
}
