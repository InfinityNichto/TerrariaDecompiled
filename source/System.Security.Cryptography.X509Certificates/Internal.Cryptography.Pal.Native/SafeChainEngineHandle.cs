using System;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal.Native;

internal sealed class SafeChainEngineHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public static readonly SafeChainEngineHandle MachineChainEngine = new SafeChainEngineHandle((IntPtr)1);

	public static readonly SafeChainEngineHandle UserChainEngine = new SafeChainEngineHandle((IntPtr)0);

	public SafeChainEngineHandle()
		: base(ownsHandle: true)
	{
	}

	private SafeChainEngineHandle(IntPtr handle)
		: base(ownsHandle: true)
	{
		SetHandle(handle);
	}

	protected sealed override bool ReleaseHandle()
	{
		global::Interop.crypt32.CertFreeCertificateChainEngine(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (this != UserChainEngine && this != MachineChainEngine)
		{
			base.Dispose(disposing);
		}
	}
}
