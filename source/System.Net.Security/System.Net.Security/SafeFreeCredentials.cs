using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Security;

internal abstract class SafeFreeCredentials : SafeHandle
{
	internal global::Interop.SspiCli.CredHandle _handle;

	public override bool IsInvalid
	{
		get
		{
			if (!base.IsClosed)
			{
				return _handle.IsZero;
			}
			return true;
		}
	}

	protected SafeFreeCredentials()
		: base(IntPtr.Zero, ownsHandle: true)
	{
		_handle = default(global::Interop.SspiCli.CredHandle);
	}

	public unsafe static int AcquireDefaultCredential(string package, global::Interop.SspiCli.CredentialUse intent, out SafeFreeCredentials outCredential)
	{
		int num = -1;
		outCredential = new SafeFreeCredential_SECURITY();
		num = global::Interop.SspiCli.AcquireCredentialsHandleW(null, package, (int)intent, null, IntPtr.Zero, null, null, ref outCredential._handle, out var _);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Verbose(null, FormattableStringFactory.Create("{0} returns 0x{1:x}, handle = {2}", "AcquireCredentialsHandleW", num, outCredential), "AcquireDefaultCredential");
		}
		if (num != 0)
		{
			outCredential.SetHandleAsInvalid();
		}
		return num;
	}

	public unsafe static int AcquireCredentialsHandle(string package, global::Interop.SspiCli.CredentialUse intent, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
	{
		int num = -1;
		outCredential = new SafeFreeCredential_SECURITY();
		num = global::Interop.SspiCli.AcquireCredentialsHandleW(null, package, (int)intent, null, authdata, null, null, ref outCredential._handle, out var _);
		if (num != 0)
		{
			outCredential.SetHandleAsInvalid();
		}
		return num;
	}

	public unsafe static int AcquireCredentialsHandle(string package, global::Interop.SspiCli.CredentialUse intent, global::Interop.SspiCli.SCHANNEL_CRED* authdata, out SafeFreeCredentials outCredential)
	{
		int num = -1;
		outCredential = new SafeFreeCredential_SECURITY();
		num = global::Interop.SspiCli.AcquireCredentialsHandleW(null, package, (int)intent, null, authdata, null, null, ref outCredential._handle, out var _);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Verbose(null, FormattableStringFactory.Create("{0} returns 0x{1:x}, handle = {2}", "AcquireCredentialsHandleW", num, outCredential), "AcquireCredentialsHandle");
		}
		if (num != 0)
		{
			outCredential.SetHandleAsInvalid();
		}
		return num;
	}

	public unsafe static int AcquireCredentialsHandle(string package, global::Interop.SspiCli.CredentialUse intent, global::Interop.SspiCli.SCH_CREDENTIALS* authdata, out SafeFreeCredentials outCredential)
	{
		outCredential = new SafeFreeCredential_SECURITY();
		long timeStamp;
		int num = global::Interop.SspiCli.AcquireCredentialsHandleW(null, package, (int)intent, null, authdata, null, null, ref outCredential._handle, out timeStamp);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Verbose(null, FormattableStringFactory.Create("{0} returns 0x{1:x}, handle = {2}", "AcquireCredentialsHandleW", num, outCredential), "AcquireCredentialsHandle");
		}
		if (num != 0)
		{
			outCredential.SetHandleAsInvalid();
		}
		return num;
	}
}
