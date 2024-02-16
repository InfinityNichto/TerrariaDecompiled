using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private string _containerName;

	private string _providerName;

	private int _type;

	private uint _flags;

	private bool _fPersistKeyInCsp;

	internal string ContainerName
	{
		set
		{
			_containerName = value;
		}
	}

	internal string ProviderName
	{
		set
		{
			_providerName = value;
		}
	}

	internal int Types
	{
		set
		{
			_type = value;
		}
	}

	internal uint Flags
	{
		set
		{
			_flags = value;
		}
	}

	internal bool PersistKeyInCsp
	{
		get
		{
			return _fPersistKeyInCsp;
		}
		set
		{
			_fPersistKeyInCsp = value;
		}
	}

	internal static SafeProvHandle InvalidHandle => SafeHandleCache<SafeProvHandle>.GetInvalidHandle(() => new SafeProvHandle());

	public SafeProvHandle()
		: base(ownsHandle: true)
	{
		SetHandle(IntPtr.Zero);
		_containerName = null;
		_providerName = null;
		_type = 0;
		_flags = 0u;
		_fPersistKeyInCsp = true;
	}

	protected override void Dispose(bool disposing)
	{
		if (!SafeHandleCache<SafeProvHandle>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}

	protected override bool ReleaseHandle()
	{
		if (!_fPersistKeyInCsp && (_flags & 0xF0000000u) == 0)
		{
			uint dwFlags = (_flags & 0x20u) | 0x10u;
			global::Interop.Advapi32.CryptAcquireContext(out var phProv, _containerName, _providerName, _type, dwFlags);
			phProv.Dispose();
		}
		bool result = global::Interop.Advapi32.CryptReleaseContext(handle, 0);
		SetHandle(IntPtr.Zero);
		return result;
	}
}
