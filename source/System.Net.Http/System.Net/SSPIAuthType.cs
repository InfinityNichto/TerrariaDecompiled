using System.Net.Security;

namespace System.Net;

internal sealed class SSPIAuthType : System.Net.ISSPIInterface
{
	private static volatile System.Net.SecurityPackageInfoClass[] s_securityPackages;

	public System.Net.SecurityPackageInfoClass[] SecurityPackages
	{
		get
		{
			return s_securityPackages;
		}
		set
		{
			s_securityPackages = value;
		}
	}

	public int EnumerateSecurityPackages(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer pkgArray)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "EnumerateSecurityPackages");
		}
		return System.Net.Security.SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
	}

	public int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, ref System.Net.Security.SafeSspiAuthDataHandle authdata, out System.Net.Security.SafeFreeCredentials outCredential)
	{
		return System.Net.Security.SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
	}

	public int AcquireDefaultCredential(string moduleName, global::Interop.SspiCli.CredentialUse usage, out System.Net.Security.SafeFreeCredentials outCredential)
	{
		return System.Net.Security.SafeFreeCredentials.AcquireDefaultCredential(moduleName, usage, out outCredential);
	}

	public int AcceptSecurityContext(System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, System.Net.Security.InputSecurityBuffers inputBuffers, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		return System.Net.Security.SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
	}

	public int InitializeSecurityContext(ref System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, System.Net.Security.InputSecurityBuffers inputBuffers, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		return System.Net.Security.SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
	}

	public int CompleteAuthToken(ref System.Net.Security.SafeDeleteSslContext refContext, in System.Net.Security.SecurityBuffer inputBuffer)
	{
		return System.Net.Security.SafeDeleteContext.CompleteAuthToken(ref refContext, in inputBuffer);
	}

	int System.Net.ISSPIInterface.CompleteAuthToken(ref System.Net.Security.SafeDeleteSslContext refContext, in System.Net.Security.SecurityBuffer inputBuffer)
	{
		return CompleteAuthToken(ref refContext, in inputBuffer);
	}
}
