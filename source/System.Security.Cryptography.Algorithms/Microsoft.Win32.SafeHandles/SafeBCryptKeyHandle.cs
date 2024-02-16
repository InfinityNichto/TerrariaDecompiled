namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeBCryptKeyHandle : SafeBCryptHandle
{
	protected sealed override bool ReleaseHandle()
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDestroyKey(handle);
		return nTSTATUS == global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS;
	}
}
