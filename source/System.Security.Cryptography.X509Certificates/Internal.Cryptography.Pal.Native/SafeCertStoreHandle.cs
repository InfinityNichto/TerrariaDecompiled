namespace Internal.Cryptography.Pal.Native;

internal sealed class SafeCertStoreHandle : SafePointerHandle<SafeCertStoreHandle>
{
	protected sealed override bool ReleaseHandle()
	{
		return global::Interop.Crypt32.CertCloseStore(handle, 0u);
	}
}
