using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private SafeProvHandle _parent;

	internal static SafeHashHandle InvalidHandle => SafeHandleCache<SafeHashHandle>.GetInvalidHandle(() => new SafeHashHandle());

	public SafeHashHandle()
		: base(ownsHandle: true)
	{
		SetHandle(IntPtr.Zero);
	}

	internal void SetParent(SafeProvHandle parent)
	{
		if (!IsInvalid && !base.IsClosed)
		{
			_parent = parent;
			bool success = false;
			_parent.DangerousAddRef(ref success);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!SafeHandleCache<SafeHashHandle>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}

	protected override bool ReleaseHandle()
	{
		bool result = global::Interop.Advapi32.CryptDestroyHash(handle);
		SafeProvHandle parent = _parent;
		_parent = null;
		parent?.DangerousRelease();
		return result;
	}
}
