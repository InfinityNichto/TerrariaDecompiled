using System;
using System.Security.Cryptography;

namespace Microsoft.Win32.SafeHandles;

internal sealed class DuplicateSafeNCryptKeyHandle : SafeNCryptKeyHandle
{
	private readonly SafeNCryptKeyHandle _original;

	public DuplicateSafeNCryptKeyHandle(SafeNCryptKeyHandle original)
	{
		bool success = false;
		original.DangerousAddRef(ref success);
		if (!success)
		{
			throw new CryptographicException();
		}
		SetHandle(original.DangerousGetHandle());
		_original = original;
	}

	protected override bool ReleaseHandle()
	{
		_original.DangerousRelease();
		SetHandle(IntPtr.Zero);
		return true;
	}
}
