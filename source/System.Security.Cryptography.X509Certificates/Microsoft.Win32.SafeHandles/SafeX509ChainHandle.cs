using Internal.Cryptography.Pal;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeX509ChainHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	internal static SafeX509ChainHandle InvalidHandle => Microsoft.Win32.SafeHandles.SafeHandleCache<SafeX509ChainHandle>.GetInvalidHandle(() => new SafeX509ChainHandle());

	public SafeX509ChainHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return ChainPal.ReleaseSafeX509ChainHandle(handle);
	}

	protected override void Dispose(bool disposing)
	{
		if (!Microsoft.Win32.SafeHandles.SafeHandleCache<SafeX509ChainHandle>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}
}
