namespace Internal.Cryptography.Pal.Native;

internal sealed class SafeCryptMsgHandle : SafePointerHandle<SafeCryptMsgHandle>
{
	protected sealed override bool ReleaseHandle()
	{
		return global::Interop.Crypt32.CryptMsgClose(handle);
	}
}
