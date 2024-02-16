using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private int _keySpec;

	private bool _fPublicOnly;

	private SafeProvHandle _parent;

	internal int KeySpec
	{
		set
		{
			_keySpec = value;
		}
	}

	internal bool PublicOnly
	{
		get
		{
			return _fPublicOnly;
		}
		set
		{
			_fPublicOnly = value;
		}
	}

	internal static SafeKeyHandle InvalidHandle => SafeHandleCache<SafeKeyHandle>.GetInvalidHandle(() => new SafeKeyHandle());

	public SafeKeyHandle()
		: base(ownsHandle: true)
	{
		SetHandle(IntPtr.Zero);
		_keySpec = 0;
		_fPublicOnly = false;
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
		if (!SafeHandleCache<SafeKeyHandle>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}

	protected override bool ReleaseHandle()
	{
		bool result = global::Interop.Advapi32.CryptDestroyKey(handle);
		SafeProvHandle parent = _parent;
		_parent = null;
		parent?.DangerousRelease();
		return result;
	}
}
