using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security;

internal static class NegotiateStreamPal
{
	internal static int QueryMaxTokenSize(string package)
	{
		return System.Net.SSPIWrapper.GetVerifyPackageInfo(System.Net.GlobalSSPI.SSPIAuth, package, throwIfMissing: true).MaxToken;
	}

	internal static System.Net.Security.SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
	{
		return System.Net.SSPIWrapper.AcquireDefaultCredential(System.Net.GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND);
	}

	internal static System.Net.Security.SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
	{
		System.Net.Security.SafeSspiAuthDataHandle authData = null;
		try
		{
			global::Interop.SECURITY_STATUS sECURITY_STATUS = global::Interop.SspiCli.SspiEncodeStringsAsAuthIdentity(credential.UserName, credential.Domain, credential.Password, out authData);
			if (sECURITY_STATUS != 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_log_operation_failed_with_error, "SspiEncodeStringsAsAuthIdentity", $"0x{sECURITY_STATUS:X}"), "AcquireCredentialsHandle");
				}
				throw new Win32Exception((int)sECURITY_STATUS);
			}
			return System.Net.SSPIWrapper.AcquireCredentialsHandle(System.Net.GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND, ref authData);
		}
		finally
		{
			authData?.Dispose();
		}
	}

	internal static string QueryContextClientSpecifiedSpn(System.Net.Security.SafeDeleteContext securityContext)
	{
		return System.Net.SSPIWrapper.QueryStringContextAttributes(System.Net.GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);
	}

	internal static string QueryContextAuthenticationPackage(System.Net.Security.SafeDeleteContext securityContext)
	{
		System.Net.SecPkgContext_NegotiationInfoW attribute = default(System.Net.SecPkgContext_NegotiationInfoW);
		SafeHandle sspiHandle;
		bool flag = System.Net.SSPIWrapper.QueryBlittableContextAttributes(System.Net.GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO, typeof(System.Net.Security.SafeFreeContextBuffer), out sspiHandle, ref attribute);
		using (sspiHandle)
		{
			return flag ? System.Net.NegotiationInfoClass.GetAuthenticationPackageName(sspiHandle, (int)attribute.NegotiationState) : null;
		}
	}

	internal static System.Net.SecurityStatusPal InitializeSecurityContext(ref System.Net.Security.SafeFreeCredentials credentialsHandle, ref System.Net.Security.SafeDeleteContext securityContext, string spn, System.Net.ContextFlagsPal requestedContextFlags, byte[] incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, ref System.Net.ContextFlagsPal contextFlags)
	{
		System.Net.Security.InputSecurityBuffers inputBuffers = default(System.Net.Security.InputSecurityBuffers);
		if (incomingBlob != null)
		{
			inputBuffers.SetNextBuffer(new System.Net.Security.InputSecurityBuffer(incomingBlob, System.Net.Security.SecurityBufferType.SECBUFFER_TOKEN));
		}
		if (channelBinding != null)
		{
			inputBuffers.SetNextBuffer(new System.Net.Security.InputSecurityBuffer(channelBinding));
		}
		System.Net.Security.SecurityBuffer outputBuffer = new System.Net.Security.SecurityBuffer(resultBlob, System.Net.Security.SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		System.Net.Security.SafeDeleteSslContext context = (System.Net.Security.SafeDeleteSslContext)securityContext;
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)System.Net.SSPIWrapper.InitializeSecurityContext(System.Net.GlobalSSPI.SSPIAuth, ref credentialsHandle, ref context, spn, System.Net.ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags), global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref outFlags);
		securityContext = context;
		resultBlob = outputBuffer.token;
		contextFlags = System.Net.ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outFlags);
		return System.Net.SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static System.Net.SecurityStatusPal CompleteAuthToken(ref System.Net.Security.SafeDeleteContext securityContext, byte[] incomingBlob)
	{
		System.Net.Security.SafeDeleteSslContext context = (System.Net.Security.SafeDeleteSslContext)securityContext;
		System.Net.Security.SecurityBuffer inputBuffer = new System.Net.Security.SecurityBuffer(incomingBlob, System.Net.Security.SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)System.Net.SSPIWrapper.CompleteAuthToken(System.Net.GlobalSSPI.SSPIAuth, ref context, in inputBuffer);
		securityContext = context;
		return System.Net.SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static System.Net.SecurityStatusPal AcceptSecurityContext(System.Net.Security.SafeFreeCredentials credentialsHandle, ref System.Net.Security.SafeDeleteContext securityContext, System.Net.ContextFlagsPal requestedContextFlags, byte[] incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, ref System.Net.ContextFlagsPal contextFlags)
	{
		System.Net.Security.InputSecurityBuffers inputBuffers = default(System.Net.Security.InputSecurityBuffers);
		if (incomingBlob != null)
		{
			inputBuffers.SetNextBuffer(new System.Net.Security.InputSecurityBuffer(incomingBlob, System.Net.Security.SecurityBufferType.SECBUFFER_TOKEN));
		}
		if (channelBinding != null)
		{
			inputBuffers.SetNextBuffer(new System.Net.Security.InputSecurityBuffer(channelBinding));
		}
		System.Net.Security.SecurityBuffer outputBuffer = new System.Net.Security.SecurityBuffer(resultBlob, System.Net.Security.SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		System.Net.Security.SafeDeleteSslContext context = (System.Net.Security.SafeDeleteSslContext)securityContext;
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)System.Net.SSPIWrapper.AcceptSecurityContext(System.Net.GlobalSSPI.SSPIAuth, credentialsHandle, ref context, System.Net.ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags), global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref outFlags);
		resultBlob = outputBuffer.token;
		securityContext = context;
		contextFlags = System.Net.ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outFlags);
		return System.Net.SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static Win32Exception CreateExceptionFromError(System.Net.SecurityStatusPal statusCode)
	{
		return new Win32Exception((int)System.Net.SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(statusCode));
	}
}
