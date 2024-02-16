using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

internal sealed class SecureChannel
{
	private SafeFreeCredentials _credentialsHandle;

	private SafeDeleteSslContext _securityContext;

	private SslConnectionInfo _connectionInfo;

	private X509Certificate _selectedClientCertificate;

	private X509Certificate2 _remoteCertificate;

	private bool _remoteCertificateExposed;

	private int _headerSize = 5;

	private int _trailerSize = 16;

	private int _maxDataSize = 16354;

	private bool _refreshCredentialNeeded;

	private readonly SslAuthenticationOptions _sslAuthenticationOptions;

	private SslApplicationProtocol _negotiatedApplicationProtocol;

	private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");

	private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");

	private SslStream _ssl;

	internal X509Certificate LocalServerCertificate => _sslAuthenticationOptions.CertificateContext?.Certificate;

	internal X509Certificate LocalClientCertificate => _selectedClientCertificate;

	internal bool IsRemoteCertificateAvailable => _remoteCertificate != null;

	internal X509Certificate RemoteCertificate
	{
		get
		{
			_remoteCertificateExposed = true;
			return _remoteCertificate;
		}
	}

	internal X509RevocationMode CheckCertRevocationStatus => _sslAuthenticationOptions.CertificateRevocationCheckMode;

	internal int MaxDataSize => _maxDataSize;

	internal SslConnectionInfo ConnectionInfo => _connectionInfo;

	internal bool IsValidContext
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (_securityContext != null)
			{
				return !_securityContext.IsInvalid;
			}
			return false;
		}
	}

	internal bool IsServer => _sslAuthenticationOptions.IsServer;

	internal bool RemoteCertRequired => _sslAuthenticationOptions.RemoteCertRequired;

	internal SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedApplicationProtocol;

	internal SecureChannel(SslAuthenticationOptions sslAuthenticationOptions, SslStream sslStream)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SecureChannelCtor(this, sslStream, sslAuthenticationOptions.TargetHost, sslAuthenticationOptions.ClientCertificates, sslAuthenticationOptions.EncryptionPolicy);
		}
		SslStreamPal.VerifyPackageInfo();
		_securityContext = null;
		_refreshCredentialNeeded = true;
		_sslAuthenticationOptions = sslAuthenticationOptions;
		_ssl = sslStream;
	}

	internal ChannelBinding GetChannelBinding(ChannelBindingKind kind)
	{
		ChannelBinding result = null;
		if (_securityContext != null)
		{
			result = SslStreamPal.QueryContextChannelBinding(_securityContext, kind);
		}
		return result;
	}

	internal void SetRefreshCredentialNeeded()
	{
		_refreshCredentialNeeded = true;
	}

	internal void Close()
	{
		if (!_remoteCertificateExposed)
		{
			_remoteCertificate?.Dispose();
			_remoteCertificate = null;
		}
		_securityContext?.Dispose();
		_credentialsHandle?.Dispose();
		_ssl = null;
		GC.SuppressFinalize(this);
	}

	internal static X509Certificate2 FindCertificateWithPrivateKey(object instance, bool isServer, X509Certificate certificate)
	{
		if (certificate == null)
		{
			return null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.LocatingPrivateKey(certificate, instance);
		}
		try
		{
			X509Certificate2 x509Certificate = MakeEx(certificate);
			if (x509Certificate != null)
			{
				if (x509Certificate.HasPrivateKey)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Log.CertIsType2(instance);
					}
					return x509Certificate;
				}
				if (certificate != x509Certificate)
				{
					x509Certificate.Dispose();
				}
			}
			string thumbprint = x509Certificate.Thumbprint;
			X509Store x509Store = CertificateValidationPal.EnsureStoreOpened(isServer);
			if (x509Store != null)
			{
				X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
				if (x509Certificate2Collection.Count > 0 && x509Certificate2Collection[0].HasPrivateKey)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Log.FoundCertInStore(isServer, instance);
					}
					return x509Certificate2Collection[0];
				}
			}
			x509Store = CertificateValidationPal.EnsureStoreOpened(!isServer);
			if (x509Store != null)
			{
				X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
				if (x509Certificate2Collection.Count > 0 && x509Certificate2Collection[0].HasPrivateKey)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Log.FoundCertInStore(!isServer, instance);
					}
					return x509Certificate2Collection[0];
				}
			}
		}
		catch (CryptographicException)
		{
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.NotFoundCertInStore(instance);
		}
		return null;
	}

	private static X509Certificate2 MakeEx(X509Certificate certificate)
	{
		if (certificate.GetType() == typeof(X509Certificate2))
		{
			return (X509Certificate2)certificate;
		}
		X509Certificate2 result = null;
		try
		{
			if (certificate.Handle != IntPtr.Zero)
			{
				result = new X509Certificate2(certificate);
			}
		}
		catch (SecurityException)
		{
		}
		catch (CryptographicException)
		{
		}
		return result;
	}

	private string[] GetRequestCertificateAuthorities()
	{
		string[] result = Array.Empty<string>();
		if (IsValidContext)
		{
			result = CertificateValidationPal.GetRequestCertificateAuthorities(_securityContext);
		}
		return result;
	}

	private bool AcquireClientCredentials(ref byte[] thumbPrint)
	{
		X509Certificate x509Certificate = null;
		List<X509Certificate> list = null;
		bool flag = false;
		if (_sslAuthenticationOptions.CertSelectionDelegate != null)
		{
			string[] requestCertificateAuthorities = GetRequestCertificateAuthorities();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Calling CertificateSelectionCallback", "AcquireClientCredentials");
			}
			X509Certificate2 x509Certificate2 = null;
			try
			{
				x509Certificate2 = CertificateValidationPal.GetRemoteCertificate(_securityContext);
				if (_sslAuthenticationOptions.ClientCertificates == null)
				{
					_sslAuthenticationOptions.ClientCertificates = new X509CertificateCollection();
				}
				x509Certificate = _sslAuthenticationOptions.CertSelectionDelegate(_sslAuthenticationOptions.TargetHost, _sslAuthenticationOptions.ClientCertificates, x509Certificate2, requestCertificateAuthorities);
			}
			finally
			{
				x509Certificate2?.Dispose();
			}
			if (x509Certificate != null)
			{
				if (_credentialsHandle == null)
				{
					flag = true;
				}
				EnsureInitialized(ref list).Add(x509Certificate);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Log.CertificateFromDelegate(this);
				}
			}
			else if (_sslAuthenticationOptions.ClientCertificates == null || _sslAuthenticationOptions.ClientCertificates.Count == 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Log.NoDelegateNoClientCert(this);
				}
				flag = true;
			}
			else if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Log.NoDelegateButClientCert(this);
			}
		}
		else if (_credentialsHandle == null && _sslAuthenticationOptions.ClientCertificates != null && _sslAuthenticationOptions.ClientCertificates.Count > 0)
		{
			x509Certificate = _sslAuthenticationOptions.ClientCertificates[0];
			flag = true;
			if (x509Certificate != null)
			{
				EnsureInitialized(ref list).Add(x509Certificate);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Log.AttemptingRestartUsingCert(x509Certificate, this);
			}
		}
		else if (_sslAuthenticationOptions.ClientCertificates != null && _sslAuthenticationOptions.ClientCertificates.Count > 0)
		{
			string[] requestCertificateAuthorities = GetRequestCertificateAuthorities();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				if (requestCertificateAuthorities == null || requestCertificateAuthorities.Length == 0)
				{
					System.Net.NetEventSource.Log.NoIssuersTryAllCerts(this);
				}
				else
				{
					System.Net.NetEventSource.Log.LookForMatchingCerts(requestCertificateAuthorities.Length, this);
				}
			}
			for (int i = 0; i < _sslAuthenticationOptions.ClientCertificates.Count; i++)
			{
				if (requestCertificateAuthorities != null && requestCertificateAuthorities.Length != 0)
				{
					X509Certificate2 x509Certificate3 = null;
					X509Chain x509Chain = null;
					try
					{
						x509Certificate3 = MakeEx(_sslAuthenticationOptions.ClientCertificates[i]);
						if (x509Certificate3 == null)
						{
							continue;
						}
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"Root cert: {x509Certificate3}", "AcquireClientCredentials");
						}
						x509Chain = new X509Chain();
						x509Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
						x509Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidName;
						x509Chain.Build(x509Certificate3);
						bool flag2 = false;
						if (x509Chain.ChainElements.Count > 0)
						{
							int count = x509Chain.ChainElements.Count;
							for (int j = 0; j < count; j++)
							{
								string issuer = x509Chain.ChainElements[j].Certificate.Issuer;
								flag2 = Array.IndexOf(requestCertificateAuthorities, issuer) != -1;
								if (flag2)
								{
									if (System.Net.NetEventSource.Log.IsEnabled())
									{
										System.Net.NetEventSource.Info(this, $"Matched {issuer}", "AcquireClientCredentials");
									}
									break;
								}
								if (System.Net.NetEventSource.Log.IsEnabled())
								{
									System.Net.NetEventSource.Info(this, $"No match: {issuer}", "AcquireClientCredentials");
								}
							}
						}
						if (!flag2)
						{
							continue;
						}
						goto IL_03c0;
					}
					finally
					{
						if (x509Chain != null)
						{
							x509Chain.Dispose();
							int count2 = x509Chain.ChainElements.Count;
							for (int k = 0; k < count2; k++)
							{
								x509Chain.ChainElements[k].Certificate.Dispose();
							}
						}
						if (x509Certificate3 != null && x509Certificate3 != _sslAuthenticationOptions.ClientCertificates[i])
						{
							x509Certificate3.Dispose();
						}
					}
				}
				goto IL_03c0;
				IL_03c0:
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Log.SelectedCert(_sslAuthenticationOptions.ClientCertificates[i], this);
				}
				EnsureInitialized(ref list).Add(_sslAuthenticationOptions.ClientCertificates[i]);
			}
		}
		bool result = false;
		X509Certificate2 x509Certificate4 = null;
		x509Certificate = null;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			if (list != null && list.Count != 0)
			{
				System.Net.NetEventSource.Log.CertsAfterFiltering(list.Count, this);
				System.Net.NetEventSource.Log.FindingMatchingCerts(this);
			}
			else
			{
				System.Net.NetEventSource.Log.CertsAfterFiltering(0, this);
				System.Net.NetEventSource.Info(this, "No client certificate to choose from", "AcquireClientCredentials");
			}
		}
		if (list != null)
		{
			for (int l = 0; l < list.Count; l++)
			{
				x509Certificate = list[l];
				if ((x509Certificate4 = FindCertificateWithPrivateKey(this, _sslAuthenticationOptions.IsServer, x509Certificate)) != null)
				{
					break;
				}
				x509Certificate = null;
				x509Certificate4 = null;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Selected cert = {x509Certificate4}", "AcquireClientCredentials");
		}
		try
		{
			byte[] array = x509Certificate4?.GetCertHash();
			SafeFreeCredentials safeFreeCredentials = SslSessionsCache.TryCachedCredential(array, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);
			if (flag && safeFreeCredentials == null && x509Certificate4 != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Reset to anonymous session.", "AcquireClientCredentials");
				}
				if (x509Certificate != x509Certificate4)
				{
					x509Certificate4.Dispose();
				}
				array = null;
				x509Certificate4 = null;
				x509Certificate = null;
			}
			if (safeFreeCredentials != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Log.UsingCachedCredential(this);
				}
				_credentialsHandle = safeFreeCredentials;
				_selectedClientCertificate = x509Certificate;
				result = true;
				if (x509Certificate4 != null)
				{
					_sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(x509Certificate4);
				}
			}
			else
			{
				if (x509Certificate4 != null)
				{
					_sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(x509Certificate4);
				}
				_credentialsHandle = SslStreamPal.AcquireCredentialsHandle(_sslAuthenticationOptions.CertificateContext, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.EncryptionPolicy, _sslAuthenticationOptions.IsServer);
				thumbPrint = array;
				_selectedClientCertificate = x509Certificate;
			}
		}
		finally
		{
			if (x509Certificate4 != null && _sslAuthenticationOptions.CertificateContext != null)
			{
				_sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(x509Certificate4);
			}
		}
		return result;
	}

	private static List<T> EnsureInitialized<T>(ref List<T> list)
	{
		return list ?? (list = new List<T>());
	}

	private bool AcquireServerCredentials(ref byte[] thumbPrint)
	{
		X509Certificate x509Certificate = null;
		X509Certificate2 x509Certificate2 = null;
		bool result = false;
		if (_sslAuthenticationOptions.ServerCertSelectionDelegate != null)
		{
			x509Certificate = _sslAuthenticationOptions.ServerCertSelectionDelegate(_sslAuthenticationOptions.TargetHost);
			if (x509Certificate == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"ServerCertSelectionDelegate returned no certificaete for '{_sslAuthenticationOptions.TargetHost}'.", "AcquireServerCredentials");
				}
				throw new AuthenticationException(System.SR.net_ssl_io_no_server_cert);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "ServerCertSelectionDelegate selected Cert", "AcquireServerCredentials");
			}
		}
		else if (_sslAuthenticationOptions.CertSelectionDelegate != null)
		{
			X509CertificateCollection x509CertificateCollection = new X509CertificateCollection();
			x509CertificateCollection.Add(_sslAuthenticationOptions.CertificateContext.Certificate);
			x509Certificate = _sslAuthenticationOptions.CertSelectionDelegate(string.Empty, x509CertificateCollection, null, Array.Empty<string>());
			if (x509Certificate == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"CertSelectionDelegate returned no certificaete for '{_sslAuthenticationOptions.TargetHost}'.", "AcquireServerCredentials");
				}
				throw new NotSupportedException(System.SR.net_ssl_io_no_server_cert);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "CertSelectionDelegate selected Cert", "AcquireServerCredentials");
			}
		}
		else if (_sslAuthenticationOptions.CertificateContext != null)
		{
			x509Certificate2 = _sslAuthenticationOptions.CertificateContext.Certificate;
		}
		if (x509Certificate2 == null)
		{
			if (x509Certificate == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, "Certiticate callback returned no certificaete.", "AcquireServerCredentials");
				}
				throw new NotSupportedException(System.SR.net_ssl_io_no_server_cert);
			}
			x509Certificate2 = FindCertificateWithPrivateKey(this, _sslAuthenticationOptions.IsServer, x509Certificate);
			if (x509Certificate2 == null)
			{
				throw new NotSupportedException(System.SR.net_ssl_io_no_server_cert);
			}
			_sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(x509Certificate2);
		}
		byte[] certHash = x509Certificate2.GetCertHash();
		SafeFreeCredentials safeFreeCredentials = SslSessionsCache.TryCachedCredential(certHash, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);
		if (safeFreeCredentials != null)
		{
			_credentialsHandle = safeFreeCredentials;
			result = true;
		}
		else
		{
			_credentialsHandle = SslStreamPal.AcquireCredentialsHandle(_sslAuthenticationOptions.CertificateContext, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.EncryptionPolicy, _sslAuthenticationOptions.IsServer);
			thumbPrint = certHash;
		}
		return result;
	}

	internal ProtocolToken NextMessage(ReadOnlySpan<byte> incomingBuffer)
	{
		byte[] output = null;
		SecurityStatusPal securityStatusPal = GenerateToken(incomingBuffer, ref output);
		if (!_sslAuthenticationOptions.IsServer && securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "NextMessage() returned SecurityStatusPal.CredentialsNeeded", "NextMessage");
			}
			SetRefreshCredentialNeeded();
			securityStatusPal = GenerateToken(incomingBuffer, ref output);
		}
		ProtocolToken protocolToken = new ProtocolToken(output, securityStatusPal);
		if (System.Net.NetEventSource.Log.IsEnabled() && protocolToken.Failed)
		{
			System.Net.NetEventSource.Error(this, $"Authentication failed. Status: {securityStatusPal}, Exception message: {protocolToken.GetException().Message}", "NextMessage");
		}
		return protocolToken;
	}

	private SecurityStatusPal GenerateToken(ReadOnlySpan<byte> inputBuffer, ref byte[] output)
	{
		byte[] outputBuffer = Array.Empty<byte>();
		SecurityStatusPal result = default(SecurityStatusPal);
		bool flag = false;
		byte[] thumbPrint = null;
		try
		{
			do
			{
				thumbPrint = null;
				if (_refreshCredentialNeeded)
				{
					flag = (_sslAuthenticationOptions.IsServer ? AcquireServerCredentials(ref thumbPrint) : AcquireClientCredentials(ref thumbPrint));
				}
				result = ((!_sslAuthenticationOptions.IsServer) ? SslStreamPal.InitializeSecurityContext(ref _credentialsHandle, ref _securityContext, _sslAuthenticationOptions.TargetHost, inputBuffer, ref outputBuffer, _sslAuthenticationOptions) : SslStreamPal.AcceptSecurityContext(ref _credentialsHandle, ref _securityContext, inputBuffer, ref outputBuffer, _sslAuthenticationOptions));
			}
			while (flag && _credentialsHandle == null);
		}
		finally
		{
			if (_refreshCredentialNeeded)
			{
				_refreshCredentialNeeded = false;
				_credentialsHandle?.Dispose();
				if (!flag && _securityContext != null && !_securityContext.IsInvalid && _credentialsHandle != null && !_credentialsHandle.IsInvalid)
				{
					SslSessionsCache.CacheCredential(_credentialsHandle, thumbPrint, _sslAuthenticationOptions.EnabledSslProtocols, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.EncryptionPolicy);
				}
			}
		}
		output = outputBuffer;
		return result;
	}

	internal SecurityStatusPal Renegotiate(out byte[] output)
	{
		return SslStreamPal.Renegotiate(ref _credentialsHandle, ref _securityContext, _sslAuthenticationOptions, out output);
	}

	internal void ProcessHandshakeSuccess()
	{
		if (_negotiatedApplicationProtocol == default(SslApplicationProtocol))
		{
			byte[] negotiatedApplicationProtocol = SslStreamPal.GetNegotiatedApplicationProtocol(_securityContext);
			_negotiatedApplicationProtocol = ((negotiatedApplicationProtocol == null) ? default(SslApplicationProtocol) : new SslApplicationProtocol(negotiatedApplicationProtocol, copy: false));
		}
		SslStreamPal.QueryContextStreamSizes(_securityContext, out var streamSizes);
		_headerSize = streamSizes.Header;
		_trailerSize = streamSizes.Trailer;
		_maxDataSize = checked(streamSizes.MaximumMessage - (_headerSize + _trailerSize));
		SslStreamPal.QueryContextConnectionInfo(_securityContext, out _connectionInfo);
	}

	internal SecurityStatusPal Encrypt(ReadOnlyMemory<byte> buffer, ref byte[] output, out int resultSize)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer.Span, "Encrypt");
		}
		byte[] output2 = output;
		SecurityStatusPal securityStatusPal = SslStreamPal.EncryptMessage(_securityContext, buffer, _headerSize, _trailerSize, ref output2, out resultSize);
		if (securityStatusPal.ErrorCode != SecurityStatusPalErrorCode.OK)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"ERROR {securityStatusPal}", "Encrypt");
			}
		}
		else
		{
			output = output2;
		}
		return securityStatusPal;
	}

	internal SecurityStatusPal Decrypt(Span<byte> buffer, out int outputOffset, out int outputCount)
	{
		SecurityStatusPal result = SslStreamPal.DecryptMessage(_securityContext, buffer, out outputOffset, out outputCount);
		if (System.Net.NetEventSource.Log.IsEnabled() && result.ErrorCode == SecurityStatusPalErrorCode.OK)
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer.Slice(outputOffset, outputCount), "Decrypt");
		}
		return result;
	}

	internal bool VerifyRemoteCertificate(RemoteCertificateValidationCallback remoteCertValidationCallback, SslCertificateTrust trust, ref ProtocolToken alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus)
	{
		sslPolicyErrors = SslPolicyErrors.None;
		chainStatus = X509ChainStatusFlags.NoError;
		bool flag = false;
		X509Chain x509Chain = null;
		X509Certificate2Collection remoteCertificateCollection = null;
		try
		{
			X509Certificate2 remoteCertificate = CertificateValidationPal.GetRemoteCertificate(_securityContext, out remoteCertificateCollection);
			if (_remoteCertificate != null && remoteCertificate != null && remoteCertificate.RawData.AsSpan().SequenceEqual(_remoteCertificate.RawData))
			{
				return true;
			}
			_remoteCertificate = remoteCertificate;
			if (_remoteCertificate == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled() && RemoteCertRequired)
				{
					System.Net.NetEventSource.Error(this, $"Remote certificate required, but no remote certificate received", "VerifyRemoteCertificate");
				}
				sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
			}
			else
			{
				x509Chain = new X509Chain();
				x509Chain.ChainPolicy.RevocationMode = _sslAuthenticationOptions.CertificateRevocationCheckMode;
				x509Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
				x509Chain.ChainPolicy.ApplicationPolicy.Add(_sslAuthenticationOptions.IsServer ? s_clientAuthOid : s_serverAuthOid);
				if (remoteCertificateCollection != null)
				{
					x509Chain.ChainPolicy.ExtraStore.AddRange(remoteCertificateCollection);
				}
				if (trust != null)
				{
					x509Chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
					if (trust._store != null)
					{
						x509Chain.ChainPolicy.CustomTrustStore.AddRange(trust._store.Certificates);
					}
					if (trust._trustList != null)
					{
						x509Chain.ChainPolicy.CustomTrustStore.AddRange(trust._trustList);
					}
				}
				sslPolicyErrors |= CertificateValidationPal.VerifyCertificateProperties(_securityContext, x509Chain, _remoteCertificate, _sslAuthenticationOptions.CheckCertName, _sslAuthenticationOptions.IsServer, _sslAuthenticationOptions.TargetHost);
			}
			if (remoteCertValidationCallback != null)
			{
				object ssl = _ssl;
				if (ssl == null)
				{
					throw new ObjectDisposedException("SslStream");
				}
				flag = remoteCertValidationCallback(ssl, _remoteCertificate, x509Chain, sslPolicyErrors);
			}
			else
			{
				if (!RemoteCertRequired)
				{
					sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
				}
				flag = sslPolicyErrors == SslPolicyErrors.None;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				LogCertificateValidation(remoteCertValidationCallback, sslPolicyErrors, flag, x509Chain);
				System.Net.NetEventSource.Info(this, $"Cert validation, remote cert = {_remoteCertificate}", "VerifyRemoteCertificate");
			}
			if (!flag)
			{
				alertToken = CreateFatalHandshakeAlertToken(sslPolicyErrors, x509Chain);
				if (x509Chain != null)
				{
					X509ChainStatus[] chainStatus2 = x509Chain.ChainStatus;
					foreach (X509ChainStatus x509ChainStatus in chainStatus2)
					{
						chainStatus |= x509ChainStatus.Status;
					}
				}
			}
		}
		finally
		{
			if (x509Chain != null)
			{
				int count = x509Chain.ChainElements.Count;
				for (int j = 0; j < count; j++)
				{
					x509Chain.ChainElements[j].Certificate.Dispose();
				}
				x509Chain.Dispose();
			}
			if (remoteCertificateCollection != null)
			{
				int count2 = remoteCertificateCollection.Count;
				for (int k = 0; k < count2; k++)
				{
					remoteCertificateCollection[k].Dispose();
				}
			}
		}
		return flag;
	}

	public ProtocolToken CreateFatalHandshakeAlertToken(SslPolicyErrors sslPolicyErrors, X509Chain chain)
	{
		TlsAlertMessage tlsAlertMessage = sslPolicyErrors switch
		{
			SslPolicyErrors.RemoteCertificateChainErrors => GetAlertMessageFromChain(chain), 
			SslPolicyErrors.RemoteCertificateNameMismatch => TlsAlertMessage.BadCertificate, 
			_ => TlsAlertMessage.CertificateUnknown, 
		};
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"alertMessage:{tlsAlertMessage}", "CreateFatalHandshakeAlertToken");
		}
		SecurityStatusPal securityStatusPal = SslStreamPal.ApplyAlertToken(ref _credentialsHandle, _securityContext, TlsAlertType.Fatal, tlsAlertMessage);
		if (securityStatusPal.ErrorCode != SecurityStatusPalErrorCode.OK)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"ApplyAlertToken() returned {securityStatusPal.ErrorCode}", "CreateFatalHandshakeAlertToken");
			}
			if (securityStatusPal.Exception != null)
			{
				ExceptionDispatchInfo.Throw(securityStatusPal.Exception);
			}
			return null;
		}
		return GenerateAlertToken();
	}

	public ProtocolToken CreateShutdownToken()
	{
		SecurityStatusPal securityStatusPal = SslStreamPal.ApplyShutdownToken(ref _credentialsHandle, _securityContext);
		if (securityStatusPal.ErrorCode != SecurityStatusPalErrorCode.OK)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"ApplyAlertToken() returned {securityStatusPal.ErrorCode}", "CreateShutdownToken");
			}
			if (securityStatusPal.Exception != null)
			{
				ExceptionDispatchInfo.Throw(securityStatusPal.Exception);
			}
			return null;
		}
		return GenerateAlertToken();
	}

	private ProtocolToken GenerateAlertToken()
	{
		byte[] output = null;
		SecurityStatusPal status = GenerateToken(default(ReadOnlySpan<byte>), ref output);
		return new ProtocolToken(output, status);
	}

	private static TlsAlertMessage GetAlertMessageFromChain(X509Chain chain)
	{
		X509ChainStatus[] chainStatus = chain.ChainStatus;
		for (int i = 0; i < chainStatus.Length; i++)
		{
			X509ChainStatus x509ChainStatus = chainStatus[i];
			if (x509ChainStatus.Status != 0)
			{
				if ((x509ChainStatus.Status & (X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.Cyclic | X509ChainStatusFlags.PartialChain)) != 0)
				{
					return TlsAlertMessage.UnknownCA;
				}
				if ((x509ChainStatus.Status & (X509ChainStatusFlags.Revoked | X509ChainStatusFlags.OfflineRevocation)) != 0)
				{
					return TlsAlertMessage.CertificateRevoked;
				}
				if ((x509ChainStatus.Status & (X509ChainStatusFlags.NotTimeValid | X509ChainStatusFlags.NotTimeNested | X509ChainStatusFlags.CtlNotTimeValid)) != 0)
				{
					return TlsAlertMessage.CertificateExpired;
				}
				if ((x509ChainStatus.Status & X509ChainStatusFlags.CtlNotValidForUsage) != 0)
				{
					return TlsAlertMessage.UnsupportedCert;
				}
				if (((x509ChainStatus.Status & (X509ChainStatusFlags.NotSignatureValid | X509ChainStatusFlags.InvalidExtension | X509ChainStatusFlags.InvalidPolicyConstraints | X509ChainStatusFlags.CtlNotSignatureValid)) | X509ChainStatusFlags.NoIssuanceChainPolicy | X509ChainStatusFlags.NotValidForUsage) != 0)
				{
					return TlsAlertMessage.BadCertificate;
				}
				return TlsAlertMessage.CertificateUnknown;
			}
		}
		return TlsAlertMessage.BadCertificate;
	}

	private void LogCertificateValidation(RemoteCertificateValidationCallback remoteCertValidationCallback, SslPolicyErrors sslPolicyErrors, bool success, X509Chain chain)
	{
		if (!System.Net.NetEventSource.Log.IsEnabled())
		{
			return;
		}
		if (sslPolicyErrors != 0)
		{
			System.Net.NetEventSource.Log.RemoteCertificateError(this, System.SR.net_log_remote_cert_has_errors);
			if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
			{
				System.Net.NetEventSource.Log.RemoteCertificateError(this, System.SR.net_log_remote_cert_not_available);
			}
			if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
			{
				System.Net.NetEventSource.Log.RemoteCertificateError(this, System.SR.net_log_remote_cert_name_mismatch);
			}
			if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
			{
				string text = "ChainStatus: ";
				X509ChainStatus[] chainStatus = chain.ChainStatus;
				foreach (X509ChainStatus x509ChainStatus in chainStatus)
				{
					text = text + "\t" + x509ChainStatus.StatusInformation;
				}
				System.Net.NetEventSource.Log.RemoteCertificateError(this, text);
			}
		}
		if (success)
		{
			if (remoteCertValidationCallback != null)
			{
				System.Net.NetEventSource.Log.RemoteCertDeclaredValid(this);
			}
			else
			{
				System.Net.NetEventSource.Log.RemoteCertHasNoErrors(this);
			}
		}
		else if (remoteCertValidationCallback != null)
		{
			System.Net.NetEventSource.Log.RemoteCertUserDeclaredInvalid(this);
		}
	}
}
