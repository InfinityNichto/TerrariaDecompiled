using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal abstract class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
{
	protected SafeFreeContextBuffer()
		: base(ownsHandle: true)
	{
	}

	internal void Set(IntPtr value)
	{
		handle = value;
	}

	internal static int EnumeratePackages(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer pkgArray)
	{
		int num = -1;
		System.Net.Security.SafeFreeContextBuffer_SECURITY safeFreeContextBuffer_SECURITY = null;
		num = global::Interop.SspiCli.EnumerateSecurityPackagesW(out pkgnum, out safeFreeContextBuffer_SECURITY);
		pkgArray = safeFreeContextBuffer_SECURITY;
		if (num != 0)
		{
			pkgArray?.SetHandleAsInvalid();
		}
		return num;
	}

	internal static System.Net.Security.SafeFreeContextBuffer CreateEmptyHandle()
	{
		return new System.Net.Security.SafeFreeContextBuffer_SECURITY();
	}

	public unsafe static int QueryContextAttributes(System.Net.Security.SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
	{
		int num = -2146893055;
		try
		{
			bool success = false;
			phContext.DangerousAddRef(ref success);
			num = global::Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
		}
		finally
		{
			phContext.DangerousRelease();
		}
		if (num == 0 && refHandle != null)
		{
			if (refHandle is System.Net.Security.SafeFreeContextBuffer)
			{
				((System.Net.Security.SafeFreeContextBuffer)refHandle).Set(*(IntPtr*)buffer);
			}
			else
			{
				((System.Net.Security.SafeFreeCertContext)refHandle).Set(*(IntPtr*)buffer);
			}
		}
		if (num != 0)
		{
			refHandle?.SetHandleAsInvalid();
		}
		return num;
	}
}
