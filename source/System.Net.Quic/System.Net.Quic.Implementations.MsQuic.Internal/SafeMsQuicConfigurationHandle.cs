using System.Buffers;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class SafeMsQuicConfigurationHandle : SafeHandle
{
	private static readonly FieldInfo _contextCertificate = typeof(SslStreamCertificateContext).GetField("Certificate", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo _contextChain = typeof(SslStreamCertificateContext).GetField("IntermediateCertificates", BindingFlags.Instance | BindingFlags.NonPublic);

	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeMsQuicConfigurationHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		MsQuicApi.Api.ConfigurationCloseDelegate(handle);
		SetHandle(IntPtr.Zero);
		return true;
	}

	public static SafeMsQuicConfigurationHandle Create(QuicClientConnectionOptions options)
	{
		X509Certificate certificate = null;
		if (options.ClientAuthenticationOptions != null)
		{
			if (options.ClientAuthenticationOptions.CipherSuitesPolicy != null)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.net_quic_ssl_option, "CipherSuitesPolicy"));
			}
			if (options.ClientAuthenticationOptions.EncryptionPolicy == EncryptionPolicy.NoEncryption)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.net_quic_ssl_option, "EncryptionPolicy"));
			}
			if (options.ClientAuthenticationOptions.ClientCertificates != null)
			{
				foreach (X509Certificate clientCertificate in options.ClientAuthenticationOptions.ClientCertificates)
				{
					try
					{
						if (((X509Certificate2)clientCertificate).HasPrivateKey)
						{
							certificate = clientCertificate;
							break;
						}
					}
					catch
					{
					}
				}
			}
		}
		return Create(options, QUIC_CREDENTIAL_FLAGS.CLIENT, certificate, null, options.ClientAuthenticationOptions?.ApplicationProtocols);
	}

	public static SafeMsQuicConfigurationHandle Create(QuicOptions options, SslServerAuthenticationOptions serverAuthenticationOptions, string targetHost = null)
	{
		QUIC_CREDENTIAL_FLAGS qUIC_CREDENTIAL_FLAGS = QUIC_CREDENTIAL_FLAGS.NONE;
		X509Certificate x509Certificate = serverAuthenticationOptions?.ServerCertificate;
		if (serverAuthenticationOptions != null)
		{
			if (serverAuthenticationOptions.CipherSuitesPolicy != null)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.net_quic_ssl_option, "CipherSuitesPolicy"));
			}
			if (serverAuthenticationOptions.EncryptionPolicy == EncryptionPolicy.NoEncryption)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.net_quic_ssl_option, "EncryptionPolicy"));
			}
			if (serverAuthenticationOptions.ClientCertificateRequired)
			{
				qUIC_CREDENTIAL_FLAGS |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION | QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED | QUIC_CREDENTIAL_FLAGS.REQUIRE_CLIENT_AUTHENTICATION;
			}
			if (x509Certificate == null && serverAuthenticationOptions != null && serverAuthenticationOptions.ServerCertificateSelectionCallback != null && targetHost != null)
			{
				x509Certificate = serverAuthenticationOptions.ServerCertificateSelectionCallback(options, targetHost);
			}
		}
		return Create(options, qUIC_CREDENTIAL_FLAGS, x509Certificate, serverAuthenticationOptions?.ServerCertificateContext, serverAuthenticationOptions?.ApplicationProtocols);
	}

	private unsafe static SafeMsQuicConfigurationHandle Create(QuicOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate certificate, SslStreamCertificateContext certificateContext, List<SslApplicationProtocol> alpnProtocols)
	{
		if (alpnProtocols == null || alpnProtocols.Count == 0)
		{
			throw new Exception("At least one SslApplicationProtocol value must be present in SslClientAuthenticationOptions or SslServerAuthenticationOptions.");
		}
		if (options.MaxBidirectionalStreams > 65535)
		{
			throw new Exception("MaxBidirectionalStreams overflow.");
		}
		if (options.MaxBidirectionalStreams > 65535)
		{
			throw new Exception("MaxBidirectionalStreams overflow.");
		}
		if ((flags & QUIC_CREDENTIAL_FLAGS.CLIENT) == 0)
		{
			if (certificate == null && certificateContext == null)
			{
				throw new Exception("Server must provide certificate");
			}
		}
		else
		{
			flags |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION | QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED;
		}
		if (!OperatingSystem.IsWindows())
		{
			flags |= QUIC_CREDENTIAL_FLAGS.USE_PORTABLE_CERTIFICATES;
		}
		MsQuicNativeMethods.QuicSettings quicSettings = default(MsQuicNativeMethods.QuicSettings);
		quicSettings.IsSetFlags = MsQuicNativeMethods.QuicSettingsIsSetFlags.PeerBidiStreamCount | MsQuicNativeMethods.QuicSettingsIsSetFlags.PeerUnidiStreamCount;
		quicSettings.PeerBidiStreamCount = (ushort)options.MaxBidirectionalStreams;
		quicSettings.PeerUnidiStreamCount = (ushort)options.MaxUnidirectionalStreams;
		MsQuicNativeMethods.QuicSettings settings = quicSettings;
		if (options.IdleTimeout != Timeout.InfiniteTimeSpan)
		{
			if (options.IdleTimeout <= TimeSpan.Zero)
			{
				throw new Exception("IdleTimeout must not be negative.");
			}
			ulong num = (ulong)options.IdleTimeout.Ticks / 10000uL;
			if (num > 4611686018427387903L)
			{
				throw new Exception("IdleTimeout is too large (max 2^62-1 milliseconds)");
			}
			settings.IsSetFlags |= MsQuicNativeMethods.QuicSettingsIsSetFlags.IdleTimeoutMs;
			settings.IdleTimeoutMs = (ulong)options.IdleTimeout.TotalMilliseconds;
		}
		X509Certificate2[] array = null;
		MemoryHandle[] handles = null;
		MsQuicNativeMethods.QuicBuffer[] buffers = null;
		uint status;
		SafeMsQuicConfigurationHandle configuration;
		try
		{
			MsQuicAlpnHelper.Prepare(alpnProtocols, out handles, out buffers);
			status = MsQuicApi.Api.ConfigurationOpenDelegate(MsQuicApi.Api.Registration, (MsQuicNativeMethods.QuicBuffer*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(buffers, 0), (uint)alpnProtocols.Count, ref settings, (uint)sizeof(MsQuicNativeMethods.QuicSettings), IntPtr.Zero, out configuration);
		}
		finally
		{
			MsQuicAlpnHelper.Return(ref handles, ref buffers);
		}
		QuicExceptionHelpers.ThrowIfFailed(status, "ConfigurationOpen failed.");
		try
		{
			MsQuicNativeMethods.CredentialConfig credConfig = default(MsQuicNativeMethods.CredentialConfig);
			credConfig.Flags = flags;
			if (certificateContext != null)
			{
				certificate = (X509Certificate2)_contextCertificate.GetValue(certificateContext);
				array = (X509Certificate2[])_contextChain.GetValue(certificateContext);
				if (certificate == null || array == null)
				{
					throw new ArgumentException("certificateContext");
				}
			}
			if (certificate != null)
			{
				if (OperatingSystem.IsWindows())
				{
					credConfig.Type = QUIC_CREDENTIAL_TYPE.CONTEXT;
					credConfig.Certificate = certificate.Handle;
					status = MsQuicApi.Api.ConfigurationLoadCredentialDelegate(configuration, ref credConfig);
				}
				else
				{
					byte[] array2;
					if (array != null && array.Length != 0)
					{
						X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
						x509Certificate2Collection.Add(certificate);
						for (int i = 0; i < array?.Length; i++)
						{
							x509Certificate2Collection.Add(array[i]);
						}
						array2 = x509Certificate2Collection.Export(X509ContentType.Pfx);
					}
					else
					{
						array2 = certificate.Export(X509ContentType.Pfx);
					}
					fixed (byte* ptr = array2)
					{
						void* ptr2 = ptr;
						Unsafe.SkipInit(out MsQuicNativeMethods.CredentialConfigCertificatePkcs12 credentialConfigCertificatePkcs);
						credentialConfigCertificatePkcs.Asn1Blob = (IntPtr)ptr2;
						credentialConfigCertificatePkcs.Asn1BlobLength = (uint)array2.Length;
						credentialConfigCertificatePkcs.PrivateKeyPassword = IntPtr.Zero;
						credConfig.Type = QUIC_CREDENTIAL_TYPE.PKCS12;
						credConfig.Certificate = (IntPtr)(&credentialConfigCertificatePkcs);
						status = MsQuicApi.Api.ConfigurationLoadCredentialDelegate(configuration, ref credConfig);
					}
				}
			}
			else
			{
				credConfig.Type = QUIC_CREDENTIAL_TYPE.NONE;
				status = MsQuicApi.Api.ConfigurationLoadCredentialDelegate(configuration, ref credConfig);
			}
			QuicExceptionHelpers.ThrowIfFailed(status, "ConfigurationLoadCredential failed.");
			return configuration;
		}
		catch
		{
			configuration.Dispose();
			throw;
		}
	}
}
