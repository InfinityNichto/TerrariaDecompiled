using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security;

internal static class NegotiateStreamPal
{
	internal static IIdentity GetIdentity(NTAuthentication context)
	{
		IIdentity identity = null;
		string name = (context.IsServer ? context.AssociatedName : context.Spn);
		string protocolName = context.ProtocolName;
		if (context.IsServer)
		{
			SecurityContextTokenHandle token = null;
			try
			{
				SecurityStatusPal status;
				SafeDeleteContext context2 = context.GetContext(out status);
				if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
				{
					throw new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status));
				}
				global::Interop.SECURITY_STATUS sECURITY_STATUS = (global::Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(GlobalSSPI.SSPIAuth, context2, out token);
				if (sECURITY_STATUS != 0)
				{
					throw new Win32Exception((int)sECURITY_STATUS);
				}
				string protocolName2 = context.ProtocolName;
				return new WindowsIdentity(token.DangerousGetHandle(), protocolName2);
			}
			catch (SecurityException)
			{
			}
			finally
			{
				token?.Dispose();
			}
		}
		return new GenericIdentity(name, protocolName);
	}

	internal static string QueryContextAssociatedName(SafeDeleteContext securityContext)
	{
		return SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES);
	}

	internal static void ValidateImpersonationLevel(TokenImpersonationLevel impersonationLevel)
	{
		if (impersonationLevel != TokenImpersonationLevel.Identification && impersonationLevel != TokenImpersonationLevel.Impersonation && impersonationLevel != TokenImpersonationLevel.Delegation)
		{
			throw new ArgumentOutOfRangeException("impersonationLevel", impersonationLevel.ToString(), System.SR.net_auth_supported_impl_levels);
		}
	}

	internal static int Encrypt(SafeDeleteContext securityContext, ReadOnlySpan<byte> buffer, bool isConfidential, bool isNtlm, [NotNull] ref byte[] output, uint sequenceNumber)
	{
		SecPkgContext_Sizes attribute = default(SecPkgContext_Sizes);
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref attribute);
		int num = checked(2147483643 - attribute.cbBlockSize - attribute.cbSecurityTrailer);
		if (buffer.Length > num)
		{
			throw new ArgumentOutOfRangeException("Length", System.SR.Format(System.SR.net_io_out_range, num));
		}
		int num2 = buffer.Length + attribute.cbSecurityTrailer + attribute.cbBlockSize;
		if (output == null || output.Length < num2 + 4)
		{
			output = new byte[num2 + 4];
		}
		buffer.CopyTo(output.AsSpan(4 + attribute.cbSecurityTrailer));
		ThreeSecurityBuffers threeSecurityBuffers = default(ThreeSecurityBuffers);
		Span<SecurityBuffer> input = MemoryMarshal.CreateSpan(ref threeSecurityBuffers._item0, 3);
		input[0] = new SecurityBuffer(output, 4, attribute.cbSecurityTrailer, SecurityBufferType.SECBUFFER_TOKEN);
		input[1] = new SecurityBuffer(output, 4 + attribute.cbSecurityTrailer, buffer.Length, SecurityBufferType.SECBUFFER_DATA);
		input[2] = new SecurityBuffer(output, 4 + attribute.cbSecurityTrailer + buffer.Length, attribute.cbBlockSize, SecurityBufferType.SECBUFFER_PADDING);
		int num3;
		if (isConfidential)
		{
			num3 = SSPIWrapper.EncryptMessage(GlobalSSPI.SSPIAuth, securityContext, input, sequenceNumber);
		}
		else
		{
			if (isNtlm)
			{
				input[1].type |= SecurityBufferType.SECBUFFER_READONLY;
			}
			num3 = SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, securityContext, input, 0u);
		}
		if (num3 != 0)
		{
			Exception ex = new Win32Exception(num3);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, ex, "Encrypt");
			}
			throw ex;
		}
		num2 = input[0].size;
		bool flag2 = false;
		if (num2 != attribute.cbSecurityTrailer)
		{
			flag2 = true;
			Buffer.BlockCopy(output, input[1].offset, output, 4 + num2, input[1].size);
		}
		num2 += input[1].size;
		if (input[2].size != 0 && (flag2 || num2 != buffer.Length + attribute.cbSecurityTrailer))
		{
			Buffer.BlockCopy(output, input[2].offset, output, 4 + num2, input[2].size);
		}
		num2 += input[2].size;
		output[0] = (byte)((uint)num2 & 0xFFu);
		output[1] = (byte)((uint)(num2 >> 8) & 0xFFu);
		output[2] = (byte)((uint)(num2 >> 16) & 0xFFu);
		output[3] = (byte)((uint)(num2 >> 24) & 0xFFu);
		return num2 + 4;
	}

	internal static int Decrypt(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, bool isConfidential, bool isNtlm, out int newOffset, uint sequenceNumber)
	{
		if (offset < 0 || offset > ((buffer != null) ? buffer.Length : 0))
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > ((buffer != null) ? (buffer.Length - offset) : 0))
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (isNtlm)
		{
			return DecryptNtlm(securityContext, buffer, offset, count, isConfidential, out newOffset, sequenceNumber);
		}
		TwoSecurityBuffers twoSecurityBuffers = default(TwoSecurityBuffers);
		Span<SecurityBuffer> input = MemoryMarshal.CreateSpan(ref twoSecurityBuffers._item0, 2);
		input[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.SECBUFFER_STREAM);
		input[1] = new SecurityBuffer(0, SecurityBufferType.SECBUFFER_DATA);
		int num = ((!isConfidential) ? SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, securityContext, input, sequenceNumber) : SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, securityContext, input, sequenceNumber));
		if (num != 0)
		{
			Exception ex = new Win32Exception(num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, ex, "Decrypt");
			}
			throw ex;
		}
		if (input[1].type != SecurityBufferType.SECBUFFER_DATA)
		{
			throw new InternalException(input[1].type);
		}
		newOffset = input[1].offset;
		return input[1].size;
	}

	private static int DecryptNtlm(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, bool isConfidential, out int newOffset, uint sequenceNumber)
	{
		if (count < 16)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		TwoSecurityBuffers twoSecurityBuffers = default(TwoSecurityBuffers);
		Span<SecurityBuffer> input = MemoryMarshal.CreateSpan(ref twoSecurityBuffers._item0, 2);
		input[0] = new SecurityBuffer(buffer, offset, 16, SecurityBufferType.SECBUFFER_TOKEN);
		input[1] = new SecurityBuffer(buffer, offset + 16, count - 16, SecurityBufferType.SECBUFFER_DATA);
		SecurityBufferType securityBufferType = SecurityBufferType.SECBUFFER_DATA;
		int num;
		if (isConfidential)
		{
			num = SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, securityContext, input, sequenceNumber);
		}
		else
		{
			securityBufferType |= SecurityBufferType.SECBUFFER_READONLY;
			input[1].type = securityBufferType;
			num = SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, securityContext, input, sequenceNumber);
		}
		if (num != 0)
		{
			Exception message = new Win32Exception(num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, message, "DecryptNtlm");
			}
			throw new Win32Exception(num);
		}
		if (input[1].type != securityBufferType)
		{
			throw new InternalException(input[1].type);
		}
		newOffset = input[1].offset;
		return input[1].size;
	}

	internal static int QueryMaxTokenSize(string package)
	{
		return SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, package, throwIfMissing: true).MaxToken;
	}

	internal static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
	{
		return SSPIWrapper.AcquireDefaultCredential(GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND);
	}

	internal static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
	{
		SafeSspiAuthDataHandle authData = null;
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
			return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPIAuth, package, isServer ? global::Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : global::Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND, ref authData);
		}
		finally
		{
			authData?.Dispose();
		}
	}

	internal static string QueryContextClientSpecifiedSpn(SafeDeleteContext securityContext)
	{
		return SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);
	}

	internal static string QueryContextAuthenticationPackage(SafeDeleteContext securityContext)
	{
		SecPkgContext_NegotiationInfoW attribute = default(SecPkgContext_NegotiationInfoW);
		SafeHandle sspiHandle;
		bool flag = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO, typeof(SafeFreeContextBuffer), out sspiHandle, ref attribute);
		using (sspiHandle)
		{
			return flag ? NegotiationInfoClass.GetAuthenticationPackageName(sspiHandle, (int)attribute.NegotiationState) : null;
		}
	}

	internal static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteContext securityContext, string spn, ContextFlagsPal requestedContextFlags, byte[] incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, ref ContextFlagsPal contextFlags)
	{
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		if (incomingBlob != null)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
		}
		if (channelBinding != null)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
		}
		SecurityBuffer outputBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.InitializeSecurityContext(GlobalSSPI.SSPIAuth, ref credentialsHandle, ref context, spn, ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags), global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref outFlags);
		securityContext = context;
		resultBlob = outputBuffer.token;
		contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outFlags);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static SecurityStatusPal CompleteAuthToken(ref SafeDeleteContext securityContext, byte[] incomingBlob)
	{
		SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
		SecurityBuffer inputBuffer = new SecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.CompleteAuthToken(GlobalSSPI.SSPIAuth, ref context, in inputBuffer);
		securityContext = context;
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static SecurityStatusPal AcceptSecurityContext(SafeFreeCredentials credentialsHandle, ref SafeDeleteContext securityContext, ContextFlagsPal requestedContextFlags, byte[] incomingBlob, ChannelBinding channelBinding, ref byte[] resultBlob, ref ContextFlagsPal contextFlags)
	{
		InputSecurityBuffers inputBuffers = default(InputSecurityBuffers);
		if (incomingBlob != null)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
		}
		if (channelBinding != null)
		{
			inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
		}
		SecurityBuffer outputBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);
		global::Interop.SspiCli.ContextFlags outFlags = global::Interop.SspiCli.ContextFlags.Zero;
		SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
		global::Interop.SECURITY_STATUS win32SecurityStatus = (global::Interop.SECURITY_STATUS)SSPIWrapper.AcceptSecurityContext(GlobalSSPI.SSPIAuth, credentialsHandle, ref context, ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags), global::Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP, inputBuffers, ref outputBuffer, ref outFlags);
		resultBlob = outputBuffer.token;
		securityContext = context;
		contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outFlags);
		return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(win32SecurityStatus);
	}

	internal static Win32Exception CreateExceptionFromError(SecurityStatusPal statusCode)
	{
		return new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(statusCode));
	}
}
