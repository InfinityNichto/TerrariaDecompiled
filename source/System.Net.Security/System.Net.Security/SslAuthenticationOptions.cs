using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

internal sealed class SslAuthenticationOptions
{
	[CompilerGenerated]
	private CipherSuitesPolicy _003CCipherSuitesPolicy_003Ek__BackingField;

	internal bool AllowRenegotiation { get; set; }

	internal string TargetHost { get; set; }

	internal X509CertificateCollection ClientCertificates { get; set; }

	internal List<SslApplicationProtocol> ApplicationProtocols { get; set; }

	internal bool IsServer { get; set; }

	internal SslStreamCertificateContext CertificateContext { get; set; }

	internal SslProtocols EnabledSslProtocols { get; set; }

	internal X509RevocationMode CertificateRevocationCheckMode { get; set; }

	internal EncryptionPolicy EncryptionPolicy { get; set; }

	internal bool RemoteCertRequired { get; set; }

	internal bool CheckCertName { get; set; }

	internal RemoteCertificateValidationCallback CertValidationDelegate { get; set; }

	internal LocalCertSelectionCallback CertSelectionDelegate { get; set; }

	internal ServerCertSelectionCallback ServerCertSelectionDelegate { get; set; }

	internal CipherSuitesPolicy CipherSuitesPolicy
	{
		[CompilerGenerated]
		set
		{
			_003CCipherSuitesPolicy_003Ek__BackingField = value;
		}
	}

	internal object UserState { get; }

	internal ServerOptionsSelectionCallback ServerOptionDelegate { get; }

	internal SslAuthenticationOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertificateValidationCallback remoteCallback, LocalCertSelectionCallback localCallback)
	{
		AllowRenegotiation = sslClientAuthenticationOptions.AllowRenegotiation;
		ApplicationProtocols = sslClientAuthenticationOptions.ApplicationProtocols;
		CertValidationDelegate = remoteCallback;
		CheckCertName = true;
		EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslClientAuthenticationOptions.EnabledSslProtocols);
		EncryptionPolicy = sslClientAuthenticationOptions.EncryptionPolicy;
		IsServer = false;
		RemoteCertRequired = true;
		TargetHost = sslClientAuthenticationOptions.TargetHost;
		CertSelectionDelegate = localCallback;
		CertificateRevocationCheckMode = sslClientAuthenticationOptions.CertificateRevocationCheckMode;
		ClientCertificates = sslClientAuthenticationOptions.ClientCertificates;
		CipherSuitesPolicy = sslClientAuthenticationOptions.CipherSuitesPolicy;
	}

	internal SslAuthenticationOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
	{
		AllowRenegotiation = sslServerAuthenticationOptions.AllowRenegotiation;
		ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols;
		CheckCertName = false;
		EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslServerAuthenticationOptions.EnabledSslProtocols);
		EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
		IsServer = true;
		RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Server RemoteCertRequired: {RemoteCertRequired}.", ".ctor");
		}
		TargetHost = string.Empty;
		CipherSuitesPolicy = sslServerAuthenticationOptions.CipherSuitesPolicy;
		CertificateRevocationCheckMode = sslServerAuthenticationOptions.CertificateRevocationCheckMode;
		if (sslServerAuthenticationOptions.ServerCertificateContext != null)
		{
			CertificateContext = sslServerAuthenticationOptions.ServerCertificateContext;
		}
		else if (sslServerAuthenticationOptions.ServerCertificate != null)
		{
			if (sslServerAuthenticationOptions.ServerCertificate is X509Certificate2 { HasPrivateKey: not false } x509Certificate)
			{
				CertificateContext = SslStreamCertificateContext.Create(x509Certificate, null);
			}
			else
			{
				X509Certificate2 x509Certificate2 = SecureChannel.FindCertificateWithPrivateKey(this, isServer: true, sslServerAuthenticationOptions.ServerCertificate);
				if (x509Certificate2 == null)
				{
					throw new AuthenticationException(System.SR.net_ssl_io_no_server_cert);
				}
				CertificateContext = SslStreamCertificateContext.Create(x509Certificate2);
			}
		}
		if (sslServerAuthenticationOptions.RemoteCertificateValidationCallback != null)
		{
			CertValidationDelegate = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
		}
	}

	internal SslAuthenticationOptions(ServerOptionsSelectionCallback optionCallback, object state, RemoteCertificateValidationCallback remoteCallback)
	{
		CheckCertName = false;
		TargetHost = string.Empty;
		IsServer = true;
		UserState = state;
		ServerOptionDelegate = optionCallback;
		CertValidationDelegate = remoteCallback;
	}

	internal void UpdateOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
	{
		AllowRenegotiation = sslServerAuthenticationOptions.AllowRenegotiation;
		ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols;
		EnabledSslProtocols = FilterOutIncompatibleSslProtocols(sslServerAuthenticationOptions.EnabledSslProtocols);
		EncryptionPolicy = sslServerAuthenticationOptions.EncryptionPolicy;
		RemoteCertRequired = sslServerAuthenticationOptions.ClientCertificateRequired;
		CipherSuitesPolicy = sslServerAuthenticationOptions.CipherSuitesPolicy;
		CertificateRevocationCheckMode = sslServerAuthenticationOptions.CertificateRevocationCheckMode;
		if (sslServerAuthenticationOptions.ServerCertificateContext != null)
		{
			CertificateContext = sslServerAuthenticationOptions.ServerCertificateContext;
		}
		else if (sslServerAuthenticationOptions.ServerCertificate is X509Certificate2 { HasPrivateKey: not false } x509Certificate)
		{
			CertificateContext = SslStreamCertificateContext.Create(x509Certificate);
		}
		if (sslServerAuthenticationOptions.RemoteCertificateValidationCallback != null)
		{
			CertValidationDelegate = sslServerAuthenticationOptions.RemoteCertificateValidationCallback;
		}
	}

	private static SslProtocols FilterOutIncompatibleSslProtocols(SslProtocols protocols)
	{
		if (protocols.HasFlag(SslProtocols.Tls12) || protocols.HasFlag(SslProtocols.Tls13))
		{
			protocols &= ~SslProtocols.Ssl2;
		}
		return protocols;
	}
}
