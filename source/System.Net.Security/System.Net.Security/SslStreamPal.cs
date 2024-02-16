using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal static class SslStreamPal
{
	private static readonly bool UseNewCryptoApi = Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 18836;

	private static readonly byte[] s_schannelShutdownBytes = BitConverter.GetBytes(1);

	public static Exception GetException(SecurityStatusPal status)
	{
		int interopFromSecurityStatusPal = (int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status);
		return new Win32Exception(interopFromSecurityStatusPal);
	}

	public static void VerifyPackageInfo()
	{
		SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", throwIfMissing: true);
	}

	public static byte[] ConvertAlpnProtocolListToByteArray(List<SslApplicationProtocol> protocols)
	{
		return global::Interop.Sec_Application_Protocols.ToByteArray(protocols);
	}

	public static SecurityStatusPal AcceptSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, ReadOnlySpan<byte> inputBuffer, ref byte[] outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
	{
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(default(ReadOnlySpan<byte>), SecurityBufferType.SECBUFFER_EMPTY));
		if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
		{
			byte[] array = ConvertAlpnProtocolListToByteArray(sslAuthenticationOptions.ApplicationProtocols);
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(new ReadOnlySpan<byte>(array), SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
		}
		SecurityBuffer outputBuffer2 = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);
		int win32SecurityStatus = SSPIWrapper.AcceptSecurityContext(GlobalSSPI.SSPISecureChannel, credentialsHandle, ref context, global::Interop.SspiCli.ContextFlags.ReplayDetect | global::Interop.SspiCli.ContextFlags.SequenceDetect | global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AllocateMemory | global::Interop.SspiCli.ContextFlags.AcceptExtendedError | global::Interop.SspiCli.ContextFlags.AcceptStream | (sslAuthenticationOptions.RemoteCertRequired ? global::Interop.SspiCli.ContextFlags.MutualAuth : global::Interop.SspiCli.ContextFlags.Zero), global::Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP, inputBuffers, ref outputBuffer2, ref outFlags);
		outputBuffer = outputBuffer2.token;
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(win32SecurityStatus);
	}

	public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, string targetName, ReadOnlySpan<byte> inputBuffer, ref byte[] outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
	{
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
		inputBuffers.SetNextBuffer(new InputSecurityBuffer(default(ReadOnlySpan<byte>), SecurityBufferType.SECBUFFER_EMPTY));
		if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
		{
			byte[] array = ConvertAlpnProtocolListToByteArray(sslAuthenticationOptions.ApplicationProtocols);
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(new ReadOnlySpan<byte>(array), SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
		}
		SecurityBuffer outputBuffer2 = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);
		int win32SecurityStatus = SSPIWrapper.InitializeSecurityContext(GlobalSSPI.SSPISecureChannel, ref credentialsHandle, ref context, targetName, global::Interop.SspiCli.ContextFlags.ReplayDetect | global::Interop.SspiCli.ContextFlags.SequenceDetect | global::Interop.SspiCli.ContextFlags.Confidentiality | global::Interop.SspiCli.ContextFlags.AllocateMemory | global::Interop.SspiCli.ContextFlags.InitManualCredValidation, global::Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP, inputBuffers, ref outputBuffer2, ref outFlags);
		outputBuffer = outputBuffer2.token;
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(win32SecurityStatus);
	}

	public static SecurityStatusPal Renegotiate(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteSslContext context, SslAuthenticationOptions sslAuthenticationOptions, out byte[] outputBuffer)
	{
		byte[] outputBuffer2 = Array.Empty<byte>();
		SecurityStatusPal result = AcceptSecurityContext(ref credentialsHandle, ref context, Span<byte>.Empty, ref outputBuffer2, sslAuthenticationOptions);
		outputBuffer = outputBuffer2;
		return result;
	}

	public static SafeFreeCredentials AcquireCredentialsHandle(SslStreamCertificateContext certificateContext, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
	{
		SafeFreeCredentials safeFreeCredentials = ((!UseNewCryptoApi || policy == EncryptionPolicy.NoEncryption) ? AcquireCredentialsHandleSchannelCred(certificateContext?.Certificate, protocols, policy, isServer) : AcquireCredentialsHandleSchCredentials(certificateContext?.Certificate, protocols, policy, isServer));
		if (certificateContext != null && certificateContext.Trust != null && certificateContext.Trust._sendTrustInHandshake)
		{
			AttachCertificateStore(safeFreeCredentials, certificateContext.Trust._store);
		}
		return safeFreeCredentials;
	}

	private unsafe static void AttachCertificateStore(SafeFreeCredentials cred, X509Store store)
	{
		global::Interop.SspiCli.SecPkgCred_ClientCertPolicy pBuffer = default(global::Interop.SspiCli.SecPkgCred_ClientCertPolicy);
		fixed (char* pwszSslCtlStoreName = store.Name)
		{
			pBuffer.pwszSslCtlStoreName = pwszSslCtlStoreName;
			global::Interop.SECURITY_STATUS sECURITY_STATUS = global::Interop.SspiCli.SetCredentialsAttributesW(ref cred._handle, 96L, ref pBuffer, sizeof(global::Interop.SspiCli.SecPkgCred_ClientCertPolicy));
			if (sECURITY_STATUS != 0)
			{
				throw new Win32Exception((int)sECURITY_STATUS);
			}
		}
	}

	public unsafe static SafeFreeCredentials AcquireCredentialsHandleSchannelCred(X509Certificate2 certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
	{
		int protocolFlagsFromSslProtocols = GetProtocolFlagsFromSslProtocols(protocols, isServer);
		global::Interop.SspiCli.CredentialUse credUsage;
		global::Interop.SspiCli.SCHANNEL_CRED.Flags flags;
		if (!isServer)
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
			flags = global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_MANUAL_CRED_VALIDATION | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_NO_DEFAULT_CREDS | global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
			if ((protocolFlagsFromSslProtocols == 0 || ((uint)protocolFlagsFromSslProtocols & 0xFFFFFFC3u) != 0) && policy != EncryptionPolicy.AllowNoEncryption && policy != EncryptionPolicy.NoEncryption)
			{
				flags |= global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_USE_STRONG_CRYPTO;
			}
		}
		else
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
			flags = global::Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlagsFromSslProtocols}), EncryptionPolicy={policy}", null, "AcquireCredentialsHandleSchannelCred");
		}
		global::Interop.SspiCli.SCHANNEL_CRED sCHANNEL_CRED = CreateSecureCredential(flags, protocolFlagsFromSslProtocols, policy);
		global::Interop.Crypt32.CERT_CONTEXT* ptr = null;
		if (certificate != null)
		{
			sCHANNEL_CRED.cCreds = 1;
			ptr = (global::Interop.Crypt32.CERT_CONTEXT*)(void*)certificate.Handle;
			sCHANNEL_CRED.paCred = &ptr;
		}
		return AcquireCredentialsHandle(credUsage, &sCHANNEL_CRED);
	}

	public unsafe static SafeFreeCredentials AcquireCredentialsHandleSchCredentials(X509Certificate2 certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
	{
		int protocolFlagsFromSslProtocols = GetProtocolFlagsFromSslProtocols(protocols, isServer);
		global::Interop.SspiCli.CredentialUse credUsage;
		global::Interop.SspiCli.SCH_CREDENTIALS.Flags flags;
		if (isServer)
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
			flags = global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;
		}
		else
		{
			credUsage = global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
			flags = global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_MANUAL_CRED_VALIDATION | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_NO_DEFAULT_CREDS | global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;
		}
		switch (policy)
		{
		case EncryptionPolicy.RequireEncryption:
			if (!isServer && (protocolFlagsFromSslProtocols & 0x30) == 0)
			{
				flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_USE_STRONG_CRYPTO;
			}
			break;
		case EncryptionPolicy.AllowNoEncryption:
			flags |= global::Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_ALLOW_NULL_ENCRYPTION;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "EncryptionPolicy"), "policy");
		}
		global::Interop.SspiCli.SCH_CREDENTIALS sCH_CREDENTIALS = default(global::Interop.SspiCli.SCH_CREDENTIALS);
		sCH_CREDENTIALS.dwVersion = 5;
		sCH_CREDENTIALS.dwFlags = flags;
		global::Interop.Crypt32.CERT_CONTEXT* ptr = null;
		if (certificate != null)
		{
			sCH_CREDENTIALS.cCreds = 1;
			ptr = (global::Interop.Crypt32.CERT_CONTEXT*)(void*)certificate.Handle;
			sCH_CREDENTIALS.paCred = &ptr;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlagsFromSslProtocols}), EncryptionPolicy={policy}", null, "AcquireCredentialsHandleSchCredentials");
		}
		if (protocolFlagsFromSslProtocols != 0)
		{
			global::Interop.SspiCli.TLS_PARAMETERS tLS_PARAMETERS = default(global::Interop.SspiCli.TLS_PARAMETERS);
			tLS_PARAMETERS.grbitDisabledProtocols = (uint)protocolFlagsFromSslProtocols ^ 0xFFFFFFFFu;
			sCH_CREDENTIALS.cTlsParameters = 1;
			sCH_CREDENTIALS.pTlsParameters = &tLS_PARAMETERS;
		}
		return AcquireCredentialsHandle(credUsage, &sCH_CREDENTIALS);
	}

	internal static byte[] GetNegotiatedApplicationProtocol(SafeDeleteContext context)
	{
		global::Interop.SecPkgContext_ApplicationProtocol attribute = default(global::Interop.SecPkgContext_ApplicationProtocol);
		if (SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, context, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_APPLICATION_PROTOCOL, ref attribute) && attribute.ProtoNegoExt == global::Interop.ApplicationProtocolNegotiationExt.ALPN && attribute.ProtoNegoStatus == global::Interop.ApplicationProtocolNegotiationStatus.Success)
		{
			return attribute.Protocol;
		}
		return null;
	}

	public unsafe static SecurityStatusPal EncryptMessage(SafeDeleteSslContext securityContext, ReadOnlyMemory<byte> input, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
	{
		checked
		{
			int num = input.Length + headerSize + trailerSize;
			if (output == null || output.Length < num)
			{
				output = new byte[num];
			}
			input.Span.CopyTo(new Span<byte>(output, headerSize, input.Length));
			global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[4];
			global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(4);
			secBufferDesc.pBuffers = ptr;
			global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
			fixed (byte* ptr3 = output)
			{
				global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
				ptr2->BufferType = SecurityBufferType.SECBUFFER_STREAM_HEADER;
				ptr2->pvBuffer = (IntPtr)ptr3;
				ptr2->cbBuffer = headerSize;
				global::Interop.SspiCli.SecBuffer* ptr4 = ptr + 1;
				ptr4->BufferType = SecurityBufferType.SECBUFFER_DATA;
				ptr4->pvBuffer = (IntPtr)(ptr3 + headerSize);
				ptr4->cbBuffer = input.Length;
				global::Interop.SspiCli.SecBuffer* ptr5 = ptr + 2;
				ptr5->BufferType = SecurityBufferType.SECBUFFER_STREAM_TRAILER;
				ptr5->pvBuffer = (IntPtr)(ptr3 + headerSize + input.Length);
				ptr5->cbBuffer = trailerSize;
				global::Interop.SspiCli.SecBuffer* ptr6 = ptr + 3;
				ptr6->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
				ptr6->cbBuffer = 0;
				ptr6->pvBuffer = IntPtr.Zero;
				int num2 = GlobalSSPI.SSPISecureChannel.EncryptMessage(securityContext, ref inputOutput, 0u);
				if (num2 != 0)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(securityContext, $"Encrypt ERROR {num2:X}", "EncryptMessage");
					}
					resultSize = 0;
					return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(num2);
				}
				resultSize = ptr2->cbBuffer + ptr4->cbBuffer + ptr5->cbBuffer;
				return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
			}
		}
	}

	public unsafe static SecurityStatusPal DecryptMessage(SafeDeleteSslContext securityContext, Span<byte> buffer, out int offset, out int count)
	{
		fixed (byte* ptr3 = buffer)
		{
			global::Interop.SspiCli.SecBuffer* ptr = stackalloc global::Interop.SspiCli.SecBuffer[4];
			global::Interop.SspiCli.SecBuffer* ptr2 = ptr;
			ptr2->BufferType = SecurityBufferType.SECBUFFER_DATA;
			ptr2->pvBuffer = (IntPtr)ptr3;
			ptr2->cbBuffer = buffer.Length;
			for (int i = 1; i < 4; i++)
			{
				global::Interop.SspiCli.SecBuffer* ptr4 = ptr + i;
				ptr4->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
				ptr4->pvBuffer = IntPtr.Zero;
				ptr4->cbBuffer = 0;
			}
			global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(4);
			secBufferDesc.pBuffers = ptr;
			global::Interop.SspiCli.SecBufferDesc inputOutput = secBufferDesc;
			global::Interop.SECURITY_STATUS sECURITY_STATUS = (global::Interop.SECURITY_STATUS)GlobalSSPI.SSPISecureChannel.DecryptMessage(securityContext, ref inputOutput, 0u);
			count = 0;
			offset = 0;
			for (int j = 0; j < 4; j++)
			{
				if ((sECURITY_STATUS == global::Interop.SECURITY_STATUS.OK && ptr[j].BufferType == SecurityBufferType.SECBUFFER_DATA) || (sECURITY_STATUS != 0 && ptr[j].BufferType == SecurityBufferType.SECBUFFER_EXTRA))
				{
					offset = (int)((byte*)(void*)ptr[j].pvBuffer - ptr3);
					count = ptr[j].cbBuffer;
					break;
				}
			}
			return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(sECURITY_STATUS);
		}
	}

	public static SecurityStatusPal ApplyAlertToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
	{
		global::Interop.SChannel.SCHANNEL_ALERT_TOKEN sCHANNEL_ALERT_TOKEN = default(global::Interop.SChannel.SCHANNEL_ALERT_TOKEN);
		sCHANNEL_ALERT_TOKEN.dwTokenType = 2u;
		sCHANNEL_ALERT_TOKEN.dwAlertType = (uint)alertType;
		sCHANNEL_ALERT_TOKEN.dwAlertNumber = (uint)alertMessage;
		global::Interop.SChannel.SCHANNEL_ALERT_TOKEN reference = sCHANNEL_ALERT_TOKEN;
		byte[] data = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref reference, 1)).ToArray();
		SecurityBuffer inputBuffer = new SecurityBuffer(data, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(GlobalSSPI.SSPISecureChannel, ref securityContext, in inputBuffer);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus, attachException: true);
	}

	public static SecurityStatusPal ApplyShutdownToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext)
	{
		SecurityBuffer inputBuffer = new SecurityBuffer(s_schannelShutdownBytes, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(GlobalSSPI.SSPISecureChannel, ref securityContext, in inputBuffer);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus, attachException: true);
	}

	public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
	{
		return SSPIWrapper.QueryContextChannelBinding(GlobalSSPI.SSPISecureChannel, securityContext, (global::Interop.SspiCli.ContextAttribute)attribute);
	}

	public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
	{
		SecPkgContext_StreamSizes attribute = default(SecPkgContext_StreamSizes);
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES, ref attribute);
		streamSizes = new StreamSizes(attribute);
	}

	public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
	{
		SecPkgContext_ConnectionInfo attribute = default(SecPkgContext_ConnectionInfo);
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO, ref attribute);
		TlsCipherSuite cipherSuite = TlsCipherSuite.TLS_NULL_WITH_NULL_NULL;
		SecPkgContext_CipherInfo attribute2 = default(SecPkgContext_CipherInfo);
		if (SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CIPHER_INFO, ref attribute2))
		{
			cipherSuite = (TlsCipherSuite)attribute2.dwCipherSuite;
		}
		connectionInfo = new SslConnectionInfo(attribute, cipherSuite);
	}

	private static int GetProtocolFlagsFromSslProtocols(SslProtocols protocols, bool isServer)
	{
		int num = (int)protocols;
		if (isServer)
		{
			return num & 0x1554;
		}
		return num & 0x2AA8;
	}

	private unsafe static global::Interop.SspiCli.SCHANNEL_CRED CreateSecureCredential(global::Interop.SspiCli.SCHANNEL_CRED.Flags flags, int protocols, EncryptionPolicy policy)
	{
		global::Interop.SspiCli.SCHANNEL_CRED sCHANNEL_CRED = default(global::Interop.SspiCli.SCHANNEL_CRED);
		sCHANNEL_CRED.hRootStore = IntPtr.Zero;
		sCHANNEL_CRED.aphMappers = IntPtr.Zero;
		sCHANNEL_CRED.palgSupportedAlgs = IntPtr.Zero;
		sCHANNEL_CRED.paCred = null;
		sCHANNEL_CRED.cCreds = 0;
		sCHANNEL_CRED.cMappers = 0;
		sCHANNEL_CRED.cSupportedAlgs = 0;
		sCHANNEL_CRED.dwSessionLifespan = 0;
		sCHANNEL_CRED.reserved = 0;
		sCHANNEL_CRED.dwVersion = 4;
		global::Interop.SspiCli.SCHANNEL_CRED result = sCHANNEL_CRED;
		switch (policy)
		{
		case EncryptionPolicy.RequireEncryption:
			result.dwMinimumCipherStrength = 0;
			result.dwMaximumCipherStrength = 0;
			break;
		case EncryptionPolicy.AllowNoEncryption:
			result.dwMinimumCipherStrength = -1;
			result.dwMaximumCipherStrength = 0;
			break;
		case EncryptionPolicy.NoEncryption:
			result.dwMinimumCipherStrength = -1;
			result.dwMaximumCipherStrength = -1;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "EncryptionPolicy"), "policy");
		}
		result.dwFlags = flags;
		result.grbitEnabledProtocols = protocols;
		return result;
	}

	private unsafe static SafeFreeCredentials AcquireCredentialsHandle(global::Interop.SspiCli.CredentialUse credUsage, global::Interop.SspiCli.SCHANNEL_CRED* secureCredential)
	{
		try
		{
			return WindowsIdentity.RunImpersonated(SafeAccessTokenHandle.InvalidHandle, () => SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential));
		}
		catch
		{
			return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential);
		}
	}

	private unsafe static SafeFreeCredentials AcquireCredentialsHandle(global::Interop.SspiCli.CredentialUse credUsage, global::Interop.SspiCli.SCH_CREDENTIALS* secureCredential)
	{
		try
		{
			return WindowsIdentity.RunImpersonated(SafeAccessTokenHandle.InvalidHandle, () => SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential));
		}
		catch
		{
			return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, "Microsoft Unified Security Protocol Provider", credUsage, secureCredential);
		}
	}
}
