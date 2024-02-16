using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net;

internal sealed class SSPISecureChannelType : ISSPIInterface
{
	private static volatile SecurityPackageInfoClass[] s_securityPackages;

	public SecurityPackageInfoClass[] SecurityPackages
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

	public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "EnumerateSecurityPackages");
		}
		return SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
	}

	public int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
	{
		return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
	}

	public int AcquireDefaultCredential(string moduleName, global::Interop.SspiCli.CredentialUse usage, out SafeFreeCredentials outCredential)
	{
		return SafeFreeCredentials.AcquireDefaultCredential(moduleName, usage, out outCredential);
	}

	public unsafe int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, global::Interop.SspiCli.SCHANNEL_CRED* authdata, out SafeFreeCredentials outCredential)
	{
		return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, authdata, out outCredential);
	}

	public unsafe int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, global::Interop.SspiCli.SCH_CREDENTIALS* authdata, out SafeFreeCredentials outCredential)
	{
		return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, authdata, out outCredential);
	}

	public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteSslContext context, InputSecurityBuffers inputBuffers, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, ref SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
	}

	public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteSslContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, InputSecurityBuffers inputBuffers, ref SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
	}

	public int EncryptMessage(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
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

	public unsafe int DecryptMessage(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
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

	public int MakeSignature(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public int VerifySignature(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding refHandle)
	{
		refHandle = SafeFreeContextBufferChannelBinding.CreateEmptyHandle();
		SecPkgContext_Bindings secPkgContext_Bindings = default(SecPkgContext_Bindings);
		return SafeFreeContextBufferChannelBinding.QueryContextChannelBinding(phContext, attribute, &secPkgContext_Bindings, refHandle);
	}

	public unsafe int QueryContextAttributes(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type handleType, out SafeHandle refHandle)
	{
		refHandle = null;
		if (handleType != null)
		{
			if (handleType == typeof(SafeFreeContextBuffer))
			{
				refHandle = SafeFreeContextBuffer.CreateEmptyHandle();
			}
			else
			{
				if (!(handleType == typeof(SafeFreeCertContext)))
				{
					throw new ArgumentException(System.SR.Format(System.SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
				}
				refHandle = new SafeFreeCertContext();
			}
		}
		fixed (byte* buffer2 = buffer)
		{
			return SafeFreeContextBuffer.QueryContextAttributes(phContext, attribute, buffer2, refHandle);
		}
	}

	public int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken)
	{
		throw new NotSupportedException();
	}

	public int CompleteAuthToken(ref SafeDeleteSslContext refContext, in SecurityBuffer inputBuffer)
	{
		throw new NotSupportedException();
	}

	public int ApplyControlToken(ref SafeDeleteContext refContext, in SecurityBuffer inputBuffer)
	{
		return SafeDeleteContext.ApplyControlToken(ref refContext, in inputBuffer);
	}

	int ISSPIInterface.CompleteAuthToken(ref SafeDeleteSslContext refContext, in SecurityBuffer inputBuffer)
	{
		return CompleteAuthToken(ref refContext, in inputBuffer);
	}

	int ISSPIInterface.ApplyControlToken(ref SafeDeleteContext refContext, in SecurityBuffer inputBuffer)
	{
		return ApplyControlToken(ref refContext, in inputBuffer);
	}
}
