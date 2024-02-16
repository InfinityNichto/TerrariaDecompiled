using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net;

internal sealed class SSPISecureChannelType : System.Net.ISSPIInterface
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

	public int EncryptMessage(System.Net.Security.SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		try
		{
			bool success = false;
			context.DangerousAddRef(ref success);
			return global::Interop.SspiCli.EncryptMessage(ref context._handle, 0u, ref inputOutput, sequenceNumber);
		}
		finally
		{
			context.DangerousRelease();
		}
	}

	public unsafe int DecryptMessage(System.Net.Security.SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		try
		{
			bool success = false;
			context.DangerousAddRef(ref success);
			return global::Interop.SspiCli.DecryptMessage(ref context._handle, ref inputOutput, sequenceNumber, null);
		}
		finally
		{
			context.DangerousRelease();
		}
	}

	public int MakeSignature(System.Net.Security.SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public int VerifySignature(System.Net.Security.SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public unsafe int QueryContextAttributes(System.Net.Security.SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type handleType, out SafeHandle refHandle)
	{
		refHandle = null;
		if (handleType != null)
		{
			if (handleType == typeof(System.Net.Security.SafeFreeContextBuffer))
			{
				refHandle = System.Net.Security.SafeFreeContextBuffer.CreateEmptyHandle();
			}
			else
			{
				if (!(handleType == typeof(System.Net.Security.SafeFreeCertContext)))
				{
					throw new ArgumentException(System.SR.Format(System.SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
				}
				refHandle = new System.Net.Security.SafeFreeCertContext();
			}
		}
		fixed (byte* buffer2 = buffer)
		{
			return System.Net.Security.SafeFreeContextBuffer.QueryContextAttributes(phContext, attribute, buffer2, refHandle);
		}
	}

	public int CompleteAuthToken(ref System.Net.Security.SafeDeleteSslContext refContext, in System.Net.Security.SecurityBuffer inputBuffer)
	{
		throw new NotSupportedException();
	}

	int System.Net.ISSPIInterface.CompleteAuthToken(ref System.Net.Security.SafeDeleteSslContext refContext, in System.Net.Security.SecurityBuffer inputBuffer)
	{
		return CompleteAuthToken(ref refContext, in inputBuffer);
	}
}
