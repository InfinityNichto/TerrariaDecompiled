using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafePasswordHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	[CompilerGenerated]
	private int _003CLength_003Ek__BackingField;

	private int Length
	{
		[CompilerGenerated]
		set
		{
			_003CLength_003Ek__BackingField = value;
		}
	}

	public static SafePasswordHandle InvalidHandle => Microsoft.Win32.SafeHandles.SafeHandleCache<SafePasswordHandle>.GetInvalidHandle(() => new SafePasswordHandle((string)null)
	{
		handle = (IntPtr)(-1)
	});

	public SafePasswordHandle(string password)
		: base(ownsHandle: true)
	{
		if (password != null)
		{
			handle = Marshal.StringToHGlobalUni(password);
			Length = password.Length;
		}
	}

	public unsafe SafePasswordHandle(ReadOnlySpan<char> password)
		: base(ownsHandle: true)
	{
		checked
		{
			if (password != default(ReadOnlySpan<char>))
			{
				int num = password.Length + 1;
				handle = Marshal.AllocHGlobal(num * 2);
				Span<char> destination = new Span<char>((void*)handle, num);
				password.CopyTo(destination);
				destination[password.Length] = '\0';
				Length = password.Length;
			}
		}
	}

	public SafePasswordHandle(SecureString password)
		: base(ownsHandle: true)
	{
		if (password != null)
		{
			handle = Marshal.SecureStringToGlobalAllocUnicode(password);
			Length = password.Length;
		}
	}

	protected override bool ReleaseHandle()
	{
		Marshal.ZeroFreeGlobalAllocUnicode(handle);
		SetHandle((IntPtr)(-1));
		Length = 0;
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (!disposing || !Microsoft.Win32.SafeHandles.SafeHandleCache<SafePasswordHandle>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}
}
