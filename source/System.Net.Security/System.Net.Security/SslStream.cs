using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace System.Net.Security;

public class SslStream : AuthenticatedStream
{
	private enum Framing
	{
		Unknown,
		BeforeSSL3,
		SinceSSL3,
		Unified,
		Invalid
	}

	private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException("SslStream", (string?)null));

	internal RemoteCertificateValidationCallback _userCertificateValidationCallback;

	internal LocalCertificateSelectionCallback _userCertificateSelectionCallback;

	internal ServerCertificateSelectionCallback _userServerCertificateSelectionCallback;

	internal LocalCertSelectionCallback _certSelectionDelegate;

	internal EncryptionPolicy _encryptionPolicy;

	private readonly Stream _innerStream;

	private SecureChannel _context;

	private ExceptionDispatchInfo _exception;

	private bool _shutdown;

	private bool _handshakeCompleted;

	internal byte[] _internalBuffer;

	internal int _internalOffset;

	internal int _internalBufferCount;

	internal int _decryptedBytesOffset;

	internal int _decryptedBytesCount;

	private int _nestedWrite;

	private int _nestedRead;

	private SslAuthenticationOptions _sslAuthenticationOptions;

	private int _nestedAuth;

	private bool _isRenego;

	private Framing _framing;

	private TlsFrameHelper.TlsFrameInfo _lastFrame;

	private volatile TaskCompletionSource<bool> _handshakeWaiter;

	private ArrayBuffer _handshakeBuffer;

	private bool _receivedEOF;

	private int _connectionOpenedStatus;

	public SslApplicationProtocol NegotiatedApplicationProtocol
	{
		get
		{
			if (_context == null)
			{
				return default(SslApplicationProtocol);
			}
			return _context.NegotiatedApplicationProtocol;
		}
	}

	public TransportContext TransportContext => new SslStreamContext(this);

	public override bool IsAuthenticated
	{
		get
		{
			if (_context != null && _context.IsValidContext && _exception == null)
			{
				return _handshakeCompleted;
			}
			return false;
		}
	}

	public override bool IsMutuallyAuthenticated
	{
		get
		{
			if (IsAuthenticated && (_context.IsServer ? _context.LocalServerCertificate : _context.LocalClientCertificate) != null)
			{
				return _context.IsRemoteCertificateAvailable;
			}
			return false;
		}
	}

	public override bool IsEncrypted => IsAuthenticated;

	public override bool IsSigned => IsAuthenticated;

	public override bool IsServer
	{
		get
		{
			if (_context != null)
			{
				return _context.IsServer;
			}
			return false;
		}
	}

	public virtual SslProtocols SslProtocol
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return GetSslProtocolInternal();
		}
	}

	public virtual bool CheckCertRevocationStatus
	{
		get
		{
			if (_context != null)
			{
				return _context.CheckCertRevocationStatus != X509RevocationMode.NoCheck;
			}
			return false;
		}
	}

	public virtual X509Certificate? LocalCertificate
	{
		get
		{
			ThrowIfExceptionalOrNotAuthenticated();
			if (!_context.IsServer)
			{
				return _context.LocalClientCertificate;
			}
			return _context.LocalServerCertificate;
		}
	}

	public virtual X509Certificate? RemoteCertificate
	{
		get
		{
			ThrowIfExceptionalOrNotAuthenticated();
			return _context?.RemoteCertificate;
		}
	}

	[CLSCompliant(false)]
	public virtual TlsCipherSuite NegotiatedCipherSuite
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return _context.ConnectionInfo?.TlsCipherSuite ?? TlsCipherSuite.TLS_NULL_WITH_NULL_NULL;
		}
	}

	public virtual CipherAlgorithmType CipherAlgorithm
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return (CipherAlgorithmType)(_context.ConnectionInfo?.DataCipherAlg ?? 0);
		}
	}

	public virtual int CipherStrength
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return _context.ConnectionInfo?.DataKeySize ?? 0;
		}
	}

	public virtual HashAlgorithmType HashAlgorithm
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return (HashAlgorithmType)(_context.ConnectionInfo?.DataHashAlg ?? 0);
		}
	}

	public virtual int HashStrength
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return _context.ConnectionInfo?.DataHashKeySize ?? 0;
		}
	}

	public virtual ExchangeAlgorithmType KeyExchangeAlgorithm
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return (ExchangeAlgorithmType)(_context.ConnectionInfo?.KeyExchangeAlg ?? 0);
		}
	}

	public virtual int KeyExchangeStrength
	{
		get
		{
			ThrowIfExceptionalOrNotHandshake();
			return _context.ConnectionInfo?.KeyExchKeySize ?? 0;
		}
	}

	public string TargetHostName
	{
		get
		{
			if (_sslAuthenticationOptions == null)
			{
				return string.Empty;
			}
			return _sslAuthenticationOptions.TargetHost;
		}
	}

	public override bool CanSeek => false;

	public override bool CanRead
	{
		get
		{
			if (IsAuthenticated)
			{
				return base.InnerStream.CanRead;
			}
			return false;
		}
	}

	public override bool CanTimeout => base.InnerStream.CanTimeout;

	public override bool CanWrite
	{
		get
		{
			if (IsAuthenticated && base.InnerStream.CanWrite)
			{
				return !_shutdown;
			}
			return false;
		}
	}

	public override int ReadTimeout
	{
		get
		{
			return base.InnerStream.ReadTimeout;
		}
		set
		{
			base.InnerStream.ReadTimeout = value;
		}
	}

	public override int WriteTimeout
	{
		get
		{
			return base.InnerStream.WriteTimeout;
		}
		set
		{
			base.InnerStream.WriteTimeout = value;
		}
	}

	public override long Length => base.InnerStream.Length;

	public override long Position
	{
		get
		{
			return base.InnerStream.Position;
		}
		set
		{
			throw new NotSupportedException(System.SR.net_noseek);
		}
	}

	private object _handshakeLock => _sslAuthenticationOptions;

	private int MaxDataSize => _context.MaxDataSize;

	public SslStream(Stream innerStream)
		: this(innerStream, leaveInnerStreamOpen: false, null, null)
	{
	}

	public SslStream(Stream innerStream, bool leaveInnerStreamOpen)
		: this(innerStream, leaveInnerStreamOpen, null, null, EncryptionPolicy.RequireEncryption)
	{
	}

	public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback)
		: this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, null, EncryptionPolicy.RequireEncryption)
	{
	}

	public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback, LocalCertificateSelectionCallback? userCertificateSelectionCallback)
		: this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback, EncryptionPolicy.RequireEncryption)
	{
	}

	public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback, LocalCertificateSelectionCallback? userCertificateSelectionCallback, EncryptionPolicy encryptionPolicy)
		: base(innerStream, leaveInnerStreamOpen)
	{
		if (encryptionPolicy != 0 && encryptionPolicy != EncryptionPolicy.AllowNoEncryption && encryptionPolicy != EncryptionPolicy.NoEncryption)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "EncryptionPolicy"), "encryptionPolicy");
		}
		_userCertificateValidationCallback = userCertificateValidationCallback;
		_userCertificateSelectionCallback = userCertificateSelectionCallback;
		_encryptionPolicy = encryptionPolicy;
		_certSelectionDelegate = ((userCertificateSelectionCallback == null) ? null : new LocalCertSelectionCallback(UserCertSelectionCallbackWrapper));
		_innerStream = innerStream;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SslStreamCtor(this, innerStream);
		}
	}

	private void SetAndVerifyValidationCallback(RemoteCertificateValidationCallback callback)
	{
		if (_userCertificateValidationCallback == null)
		{
			_userCertificateValidationCallback = callback;
		}
		else if (callback != null && _userCertificateValidationCallback != callback)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_conflicting_options, "RemoteCertificateValidationCallback"));
		}
	}

	private void SetAndVerifySelectionCallback(LocalCertificateSelectionCallback callback)
	{
		if (_userCertificateSelectionCallback == null)
		{
			_userCertificateSelectionCallback = callback;
			_certSelectionDelegate = ((_userCertificateSelectionCallback == null) ? null : new LocalCertSelectionCallback(UserCertSelectionCallbackWrapper));
		}
		else if (callback != null && _userCertificateSelectionCallback != callback)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_conflicting_options, "LocalCertificateSelectionCallback"));
		}
	}

	private X509Certificate UserCertSelectionCallbackWrapper(string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
	{
		return _userCertificateSelectionCallback(this, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
	}

	private X509Certificate ServerCertSelectionCallbackWrapper(string targetHost)
	{
		return _userServerCertificateSelectionCallback(this, targetHost);
	}

	private SslAuthenticationOptions CreateAuthenticationOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
	{
		if (sslServerAuthenticationOptions.ServerCertificate == null && sslServerAuthenticationOptions.ServerCertificateContext == null && sslServerAuthenticationOptions.ServerCertificateSelectionCallback == null && _certSelectionDelegate == null)
		{
			throw new ArgumentNullException("ServerCertificate");
		}
		if ((sslServerAuthenticationOptions.ServerCertificate != null || sslServerAuthenticationOptions.ServerCertificateContext != null || _certSelectionDelegate != null) && sslServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_conflicting_options, "ServerCertificateSelectionCallback"));
		}
		SslAuthenticationOptions sslAuthenticationOptions = new SslAuthenticationOptions(sslServerAuthenticationOptions);
		_userServerCertificateSelectionCallback = sslServerAuthenticationOptions.ServerCertificateSelectionCallback;
		sslAuthenticationOptions.ServerCertSelectionDelegate = ((_userServerCertificateSelectionCallback == null) ? null : new ServerCertSelectionCallback(ServerCertSelectionCallbackWrapper));
		sslAuthenticationOptions.CertValidationDelegate = _userCertificateValidationCallback;
		sslAuthenticationOptions.CertSelectionDelegate = _certSelectionDelegate;
		return sslAuthenticationOptions;
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(targetHost, null, SslProtocols.None, checkCertificateRevocation: false, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(targetHost, clientCertificates, SslProtocols.None, checkCertificateRevocation, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
	{
		SslClientAuthenticationOptions sslClientAuthenticationOptions = new SslClientAuthenticationOptions
		{
			TargetHost = targetHost,
			ClientCertificates = clientCertificates,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		return BeginAuthenticateAsClient(sslClientAuthenticationOptions, CancellationToken.None, asyncCallback, asyncState);
	}

	internal IAsyncResult BeginAuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback asyncCallback, object asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AuthenticateAsClientApm(sslClientAuthenticationOptions, cancellationToken), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired: false, SslProtocols.None, checkCertificateRevocation: false, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, SslProtocols.None, checkCertificateRevocation, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
	{
		SslServerAuthenticationOptions sslServerAuthenticationOptions = new SslServerAuthenticationOptions
		{
			ServerCertificate = serverCertificate,
			ClientCertificateRequired = clientCertificateRequired,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		return BeginAuthenticateAsServer(sslServerAuthenticationOptions, CancellationToken.None, asyncCallback, asyncState);
	}

	private IAsyncResult BeginAuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback asyncCallback, object asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AuthenticateAsServerApm(sslServerAuthenticationOptions, cancellationToken), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	internal ChannelBinding GetChannelBinding(ChannelBindingKind kind)
	{
		return _context?.GetChannelBinding(kind);
	}

	public virtual void AuthenticateAsClient(string targetHost)
	{
		AuthenticateAsClient(targetHost, null, SslProtocols.None, checkCertificateRevocation: false);
	}

	public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, bool checkCertificateRevocation)
	{
		AuthenticateAsClient(targetHost, clientCertificates, SslProtocols.None, checkCertificateRevocation);
	}

	public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
	{
		SslClientAuthenticationOptions sslClientAuthenticationOptions = new SslClientAuthenticationOptions
		{
			TargetHost = targetHost,
			ClientCertificates = clientCertificates,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		AuthenticateAsClient(sslClientAuthenticationOptions);
	}

	public void AuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions)
	{
		if (sslClientAuthenticationOptions == null)
		{
			throw new ArgumentNullException("sslClientAuthenticationOptions");
		}
		SetAndVerifyValidationCallback(sslClientAuthenticationOptions.RemoteCertificateValidationCallback);
		SetAndVerifySelectionCallback(sslClientAuthenticationOptions.LocalCertificateSelectionCallback);
		ValidateCreateContext(sslClientAuthenticationOptions, _userCertificateValidationCallback, _certSelectionDelegate);
		ProcessAuthenticationAsync().GetAwaiter().GetResult();
	}

	public virtual void AuthenticateAsServer(X509Certificate serverCertificate)
	{
		AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, SslProtocols.None, checkCertificateRevocation: false);
	}

	public virtual void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
	{
		AuthenticateAsServer(serverCertificate, clientCertificateRequired, SslProtocols.None, checkCertificateRevocation);
	}

	public virtual void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
	{
		SslServerAuthenticationOptions sslServerAuthenticationOptions = new SslServerAuthenticationOptions
		{
			ServerCertificate = serverCertificate,
			ClientCertificateRequired = clientCertificateRequired,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		AuthenticateAsServer(sslServerAuthenticationOptions);
	}

	public void AuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions)
	{
		if (sslServerAuthenticationOptions == null)
		{
			throw new ArgumentNullException("sslServerAuthenticationOptions");
		}
		SetAndVerifyValidationCallback(sslServerAuthenticationOptions.RemoteCertificateValidationCallback);
		ValidateCreateContext(CreateAuthenticationOptions(sslServerAuthenticationOptions));
		ProcessAuthenticationAsync().GetAwaiter().GetResult();
	}

	public virtual Task AuthenticateAsClientAsync(string targetHost)
	{
		return AuthenticateAsClientAsync(targetHost, null, checkCertificateRevocation: false);
	}

	public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection? clientCertificates, bool checkCertificateRevocation)
	{
		return AuthenticateAsClientAsync(targetHost, clientCertificates, SslProtocols.None, checkCertificateRevocation);
	}

	public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection? clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
	{
		SslClientAuthenticationOptions sslClientAuthenticationOptions = new SslClientAuthenticationOptions
		{
			TargetHost = targetHost,
			ClientCertificates = clientCertificates,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		return AuthenticateAsClientAsync(sslClientAuthenticationOptions);
	}

	public Task AuthenticateAsClientAsync(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (sslClientAuthenticationOptions == null)
		{
			throw new ArgumentNullException("sslClientAuthenticationOptions");
		}
		SetAndVerifyValidationCallback(sslClientAuthenticationOptions.RemoteCertificateValidationCallback);
		SetAndVerifySelectionCallback(sslClientAuthenticationOptions.LocalCertificateSelectionCallback);
		ValidateCreateContext(sslClientAuthenticationOptions, _userCertificateValidationCallback, _certSelectionDelegate);
		return ProcessAuthenticationAsync(isAsync: true, isApm: false, cancellationToken);
	}

	private Task AuthenticateAsClientApm(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken = default(CancellationToken))
	{
		SetAndVerifyValidationCallback(sslClientAuthenticationOptions.RemoteCertificateValidationCallback);
		SetAndVerifySelectionCallback(sslClientAuthenticationOptions.LocalCertificateSelectionCallback);
		ValidateCreateContext(sslClientAuthenticationOptions, _userCertificateValidationCallback, _certSelectionDelegate);
		return ProcessAuthenticationAsync(isAsync: true, isApm: true, cancellationToken);
	}

	public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate)
	{
		return AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, SslProtocols.None, checkCertificateRevocation: false);
	}

	public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
	{
		SslServerAuthenticationOptions sslServerAuthenticationOptions = new SslServerAuthenticationOptions
		{
			ServerCertificate = serverCertificate,
			ClientCertificateRequired = clientCertificateRequired,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		return AuthenticateAsServerAsync(sslServerAuthenticationOptions);
	}

	public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
	{
		SslServerAuthenticationOptions sslServerAuthenticationOptions = new SslServerAuthenticationOptions
		{
			ServerCertificate = serverCertificate,
			ClientCertificateRequired = clientCertificateRequired,
			EnabledSslProtocols = enabledSslProtocols,
			CertificateRevocationCheckMode = (checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck),
			EncryptionPolicy = _encryptionPolicy
		};
		return AuthenticateAsServerAsync(sslServerAuthenticationOptions);
	}

	public Task AuthenticateAsServerAsync(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (sslServerAuthenticationOptions == null)
		{
			throw new ArgumentNullException("sslServerAuthenticationOptions");
		}
		SetAndVerifyValidationCallback(sslServerAuthenticationOptions.RemoteCertificateValidationCallback);
		ValidateCreateContext(CreateAuthenticationOptions(sslServerAuthenticationOptions));
		return ProcessAuthenticationAsync(isAsync: true, isApm: false, cancellationToken);
	}

	private Task AuthenticateAsServerApm(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken = default(CancellationToken))
	{
		SetAndVerifyValidationCallback(sslServerAuthenticationOptions.RemoteCertificateValidationCallback);
		ValidateCreateContext(CreateAuthenticationOptions(sslServerAuthenticationOptions));
		return ProcessAuthenticationAsync(isAsync: true, isApm: true, cancellationToken);
	}

	public Task AuthenticateAsServerAsync(ServerOptionsSelectionCallback optionsCallback, object? state, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateCreateContext(new SslAuthenticationOptions(optionsCallback, state, _userCertificateValidationCallback));
		return ProcessAuthenticationAsync(isAsync: true, isApm: false, cancellationToken);
	}

	public virtual Task ShutdownAsync()
	{
		ThrowIfExceptionalOrNotAuthenticatedOrShutdown();
		ProtocolToken protocolToken = _context.CreateShutdownToken();
		_shutdown = true;
		return base.InnerStream.WriteAsync(protocolToken.Payload).AsTask();
	}

	private SslProtocols GetSslProtocolInternal()
	{
		SslConnectionInfo connectionInfo = _context.ConnectionInfo;
		if (connectionInfo == null)
		{
			return SslProtocols.None;
		}
		SslProtocols protocol = (SslProtocols)connectionInfo.Protocol;
		SslProtocols sslProtocols = SslProtocols.None;
		if ((protocol & SslProtocols.Ssl2) != 0)
		{
			sslProtocols |= SslProtocols.Ssl2;
		}
		if ((protocol & SslProtocols.Ssl3) != 0)
		{
			sslProtocols |= SslProtocols.Ssl3;
		}
		if ((protocol & SslProtocols.Tls) != 0)
		{
			sslProtocols |= SslProtocols.Tls;
		}
		if ((protocol & SslProtocols.Tls11) != 0)
		{
			sslProtocols |= SslProtocols.Tls11;
		}
		if ((protocol & SslProtocols.Tls12) != 0)
		{
			sslProtocols |= SslProtocols.Tls12;
		}
		if ((protocol & SslProtocols.Tls13) != 0)
		{
			sslProtocols |= SslProtocols.Tls13;
		}
		return sslProtocols;
	}

	public override void SetLength(long value)
	{
		base.InnerStream.SetLength(value);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public override void Flush()
	{
		base.InnerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return base.InnerStream.FlushAsync(cancellationToken);
	}

	public virtual Task NegotiateClientCertificateAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfExceptionalOrNotAuthenticated();
		if (RemoteCertificate != null)
		{
			throw new InvalidOperationException(System.SR.net_ssl_certificate_exist);
		}
		return RenegotiateAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken));
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			CloseInternal();
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override async ValueTask DisposeAsync()
	{
		try
		{
			CloseInternal();
		}
		finally
		{
			await base.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override int ReadByte()
	{
		ThrowIfExceptionalOrNotAuthenticated();
		if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "ReadByte", "read"));
		}
		try
		{
			if (_decryptedBytesCount > 0)
			{
				int result = _internalBuffer[_decryptedBytesOffset++];
				_decryptedBytesCount--;
				ReturnReadBufferIfEmpty();
				return result;
			}
		}
		finally
		{
			_nestedRead = 0;
		}
		byte[] array = new byte[1];
		int num = Read(array, 0, 1);
		if (num != 1)
		{
			return -1;
		}
		return array[0];
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsyncInternal(new SyncReadWriteAdapter(base.InnerStream), new Memory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
	}

	public void Write(byte[] buffer)
	{
		Write(buffer, 0, buffer.Length);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteAsyncInternal(new SyncReadWriteAdapter(base.InnerStream), new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfExceptionalOrNotAuthenticated();
		return WriteAsyncInternal(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsyncInternal(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), new Memory<byte>(buffer, offset, count)).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfExceptionalOrNotAuthenticated();
		return ReadAsyncInternal(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), buffer);
	}

	private void ThrowIfExceptional()
	{
		ExceptionDispatchInfo exception = _exception;
		if (exception != null)
		{
			ThrowExceptional(exception);
		}
		static void ThrowExceptional(ExceptionDispatchInfo e)
		{
			if (e == s_disposedSentinel)
			{
				throw new ObjectDisposedException("SslStream");
			}
			e.Throw();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ThrowIfExceptionalOrNotAuthenticated()
	{
		ThrowIfExceptional();
		if (!IsAuthenticated)
		{
			ThrowNotAuthenticated();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ThrowIfExceptionalOrNotHandshake()
	{
		ThrowIfExceptional();
		if (!IsAuthenticated && _context?.ConnectionInfo == null)
		{
			ThrowNotAuthenticated();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ThrowIfExceptionalOrNotAuthenticatedOrShutdown()
	{
		ThrowIfExceptional();
		if (!IsAuthenticated)
		{
			ThrowNotAuthenticated();
		}
		if (_shutdown)
		{
			ThrowAlreadyShutdown();
		}
		static void ThrowAlreadyShutdown()
		{
			throw new InvalidOperationException(System.SR.net_ssl_io_already_shutdown);
		}
	}

	private static void ThrowNotAuthenticated()
	{
		throw new InvalidOperationException(System.SR.net_auth_noauth);
	}

	private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertificateValidationCallback remoteCallback, LocalCertSelectionCallback localCallback)
	{
		ThrowIfExceptional();
		if (_context != null && _context.IsValidContext)
		{
			throw new InvalidOperationException(System.SR.net_auth_reauth);
		}
		if (_context != null && IsServer)
		{
			throw new InvalidOperationException(System.SR.net_auth_client_server);
		}
		if (sslClientAuthenticationOptions.TargetHost == null)
		{
			throw new ArgumentNullException("TargetHost");
		}
		_exception = null;
		try
		{
			_sslAuthenticationOptions = new SslAuthenticationOptions(sslClientAuthenticationOptions, remoteCallback, localCallback);
			_context = new SecureChannel(_sslAuthenticationOptions, this);
		}
		catch (Win32Exception innerException)
		{
			throw new AuthenticationException(System.SR.net_auth_SSPI, innerException);
		}
	}

	private void ValidateCreateContext(SslAuthenticationOptions sslAuthenticationOptions)
	{
		ThrowIfExceptional();
		if (_context != null && _context.IsValidContext)
		{
			throw new InvalidOperationException(System.SR.net_auth_reauth);
		}
		if (_context != null && !IsServer)
		{
			throw new InvalidOperationException(System.SR.net_auth_client_server);
		}
		_exception = null;
		_sslAuthenticationOptions = sslAuthenticationOptions;
		try
		{
			_context = new SecureChannel(_sslAuthenticationOptions, this);
		}
		catch (Win32Exception innerException)
		{
			throw new AuthenticationException(System.SR.net_auth_SSPI, innerException);
		}
	}

	private void SetException(Exception e)
	{
		if (_exception == null)
		{
			_exception = ExceptionDispatchInfo.Capture(e);
		}
		_context?.Close();
	}

	private void CloseInternal()
	{
		_exception = s_disposedSentinel;
		_context?.Close();
		if (Interlocked.CompareExchange(ref _nestedRead, 1, 0) == 0)
		{
			byte[] internalBuffer = _internalBuffer;
			if (internalBuffer != null)
			{
				_internalBuffer = null;
				_internalBufferCount = 0;
				_internalOffset = 0;
				ArrayPool<byte>.Shared.Return(internalBuffer);
			}
		}
		if (_internalBuffer == null)
		{
			GC.SuppressFinalize(this);
		}
		if (NetSecurityTelemetry.Log.IsEnabled() && Interlocked.Exchange(ref _connectionOpenedStatus, 2) == 1)
		{
			NetSecurityTelemetry.Log.ConnectionClosed(GetSslProtocolInternal());
		}
	}

	private SecurityStatusPal EncryptData(ReadOnlyMemory<byte> buffer, ref byte[] outBuffer, out int outSize)
	{
		ThrowIfExceptionalOrNotAuthenticated();
		lock (_handshakeLock)
		{
			if (_handshakeWaiter != null)
			{
				outSize = 0;
				return new SecurityStatusPal(SecurityStatusPalErrorCode.TryAgain);
			}
			return _context.Encrypt(buffer, ref outBuffer, out outSize);
		}
	}

	private Task ProcessAuthenticationAsync(bool isAsync = false, bool isApm = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfExceptional();
		if (NetSecurityTelemetry.Log.IsEnabled())
		{
			return ProcessAuthenticationWithTelemetryAsync(isAsync, isApm, cancellationToken);
		}
		if (!isAsync)
		{
			return ForceAuthenticationAsync(new SyncReadWriteAdapter(base.InnerStream), _context.IsServer, null);
		}
		return ForceAuthenticationAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), _context.IsServer, null, isApm);
	}

	private async Task ProcessAuthenticationWithTelemetryAsync(bool isAsync, bool isApm, CancellationToken cancellationToken)
	{
		NetSecurityTelemetry.Log.HandshakeStart(_context.IsServer, _sslAuthenticationOptions.TargetHost);
		ValueStopwatch stopwatch = ValueStopwatch.StartNew();
		try
		{
			Task task = (isAsync ? ForceAuthenticationAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), _context.IsServer, null, isApm) : ForceAuthenticationAsync(new SyncReadWriteAdapter(base.InnerStream), _context.IsServer, null));
			await task.ConfigureAwait(continueOnCapturedContext: false);
			bool connectionOpen = Interlocked.CompareExchange(ref _connectionOpenedStatus, 1, 0) == 0;
			NetSecurityTelemetry.Log.HandshakeCompleted(GetSslProtocolInternal(), stopwatch, connectionOpen);
		}
		catch (Exception ex)
		{
			NetSecurityTelemetry.Log.HandshakeFailed(_context.IsServer, stopwatch, ex.Message);
			throw;
		}
	}

	private async Task ReplyOnReAuthenticationAsync<TIOAdapter>(TIOAdapter adapter, byte[] buffer) where TIOAdapter : IReadWriteAdapter
	{
		try
		{
			await ForceAuthenticationAsync(adapter, receiveFirst: false, buffer).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_handshakeWaiter.SetResult(result: true);
			_handshakeWaiter = null;
		}
	}

	private async Task RenegotiateAsync<TIOAdapter>(TIOAdapter adapter) where TIOAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, "NegotiateClientCertificateAsync", "renegotiate"));
		}
		if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "ReadAsync", "read"));
		}
		if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
		{
			_nestedRead = 0;
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "WriteAsync", "write"));
		}
		try
		{
			if (_decryptedBytesCount != 0)
			{
				throw new InvalidOperationException(System.SR.net_ssl_renegotiate_buffer);
			}
			_sslAuthenticationOptions.RemoteCertRequired = true;
			_isRenego = true;
			byte[] output;
			SecurityStatusPal status = _context.Renegotiate(out output);
			if (output != null && output.Length != 0)
			{
				await adapter.WriteAsync(output, 0, output.Length).ConfigureAwait(continueOnCapturedContext: false);
				await adapter.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
			{
				if (status.ErrorCode != SecurityStatusPalErrorCode.NoRenegotiation)
				{
					throw SslStreamPal.GetException(status);
				}
				return;
			}
			_handshakeBuffer = new ArrayBuffer(4160);
			ProtocolToken message;
			do
			{
				message = await ReceiveBlobAsync(adapter).ConfigureAwait(continueOnCapturedContext: false);
				if (message.Size > 0)
				{
					await adapter.WriteAsync(message.Payload, 0, message.Size).ConfigureAwait(continueOnCapturedContext: false);
					await adapter.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			while (message.Status.ErrorCode == SecurityStatusPalErrorCode.ContinueNeeded);
			if (_handshakeBuffer.ActiveLength > 0)
			{
				ResetReadBuffer();
				_handshakeBuffer.ActiveSpan.CopyTo(_internalBuffer);
				_internalBufferCount = _handshakeBuffer.ActiveLength;
			}
			CompleteHandshake(_sslAuthenticationOptions);
		}
		finally
		{
			_nestedRead = 0;
			_nestedWrite = 0;
			_isRenego = false;
		}
	}

	private async Task ForceAuthenticationAsync<TIOAdapter>(TIOAdapter adapter, bool receiveFirst, byte[] reAuthenticationData, bool isApm = false) where TIOAdapter : IReadWriteAdapter
	{
		bool handshakeCompleted = false;
		if (reAuthenticationData == null && Interlocked.Exchange(ref _nestedAuth, 1) == 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, isApm ? "BeginAuthenticate" : "Authenticate", "authenticate"));
		}
		try
		{
			if (!receiveFirst)
			{
				ProtocolToken message = _context.NextMessage(reAuthenticationData);
				if (message.Size > 0)
				{
					await adapter.WriteAsync(message.Payload, 0, message.Size).ConfigureAwait(continueOnCapturedContext: false);
					await adapter.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Log.SentFrame(this, message.Payload);
					}
				}
				if (message.Failed)
				{
					throw new AuthenticationException(System.SR.net_auth_SSPI, message.GetException());
				}
				if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
				{
					handshakeCompleted = true;
				}
			}
			if (!handshakeCompleted)
			{
				_handshakeBuffer = new ArrayBuffer(4160);
			}
			while (!handshakeCompleted)
			{
				ProtocolToken message = await ReceiveBlobAsync(adapter).ConfigureAwait(continueOnCapturedContext: false);
				byte[] payload = null;
				int num = 0;
				if (message.Size > 0)
				{
					payload = message.Payload;
					num = message.Size;
				}
				else if (message.Failed && (_lastFrame.Header.Type == TlsContentType.Handshake || _lastFrame.Header.Type == TlsContentType.ChangeCipherSpec))
				{
					payload = TlsFrameHelper.CreateAlertFrame(_lastFrame.Header.Version, TlsAlertDescription.ProtocolVersion);
					num = payload.Length;
				}
				if (payload != null && num > 0)
				{
					await adapter.WriteAsync(payload, 0, num).ConfigureAwait(continueOnCapturedContext: false);
					await adapter.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Log.SentFrame(this, payload);
					}
				}
				if (message.Failed)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, message.Status, "ForceAuthenticationAsync");
					}
					if (_lastFrame.Header.Type == TlsContentType.Alert && _lastFrame.AlertDescription != 0 && message.Status.ErrorCode == SecurityStatusPalErrorCode.IllegalMessage)
					{
						throw new AuthenticationException(System.SR.Format(System.SR.net_auth_tls_alert, _lastFrame.AlertDescription.ToString()), message.GetException());
					}
					throw new AuthenticationException(System.SR.net_auth_SSPI, message.GetException());
				}
				if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
				{
					handshakeCompleted = true;
				}
			}
			if (_handshakeBuffer.ActiveLength > 0)
			{
				ResetReadBuffer();
				_handshakeBuffer.ActiveSpan.CopyTo(_internalBuffer);
				_internalBufferCount = _handshakeBuffer.ActiveLength;
			}
			CompleteHandshake(_sslAuthenticationOptions);
		}
		finally
		{
			_handshakeBuffer.Dispose();
			if (reAuthenticationData == null)
			{
				_nestedAuth = 0;
				_isRenego = false;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SspiSelectedCipherSuite("ForceAuthenticationAsync", SslProtocol, CipherAlgorithm, CipherStrength, HashAlgorithm, HashStrength, KeyExchangeAlgorithm, KeyExchangeStrength);
		}
	}

	private async ValueTask<ProtocolToken> ReceiveBlobAsync<TIOAdapter>(TIOAdapter adapter) where TIOAdapter : IReadWriteAdapter
	{
		if (await FillHandshakeBufferAsync(adapter, 5).ConfigureAwait(continueOnCapturedContext: false) == 0)
		{
			throw new IOException(System.SR.net_io_eof);
		}
		if (_framing == Framing.Unified || _framing == Framing.Unknown)
		{
			_framing = DetectFraming(_handshakeBuffer.ActiveReadOnlySpan);
		}
		if (_framing != Framing.SinceSSL3)
		{
			_lastFrame.Header.Version = SslProtocols.Ssl2;
			_lastFrame.Header.Length = GetFrameSize(_handshakeBuffer.ActiveReadOnlySpan) - 5;
		}
		else
		{
			TlsFrameHelper.TryGetFrameHeader(_handshakeBuffer.ActiveReadOnlySpan, ref _lastFrame.Header);
		}
		if (_lastFrame.Header.Length < 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, "invalid TLS frame size", "ReceiveBlobAsync");
			}
			throw new IOException(System.SR.net_frame_read_size);
		}
		int frameSize = _lastFrame.Header.Length + 5;
		if (_handshakeBuffer.ActiveLength < frameSize)
		{
			await FillHandshakeBufferAsync(adapter, frameSize).ConfigureAwait(continueOnCapturedContext: false);
		}
		switch (_lastFrame.Header.Type)
		{
		case TlsContentType.Alert:
			if (TlsFrameHelper.TryGetFrameInfo(_handshakeBuffer.ActiveReadOnlySpan, ref _lastFrame) && System.Net.NetEventSource.Log.IsEnabled() && _lastFrame.AlertDescription != 0)
			{
				System.Net.NetEventSource.Error(this, $"Received TLS alert {_lastFrame.AlertDescription}", "ReceiveBlobAsync");
			}
			break;
		case TlsContentType.Handshake:
		{
			if (_isRenego || _handshakeBuffer.ActiveReadOnlySpan[5] != 1 || (_sslAuthenticationOptions.ServerCertSelectionDelegate == null && _sslAuthenticationOptions.ServerOptionDelegate == null))
			{
				break;
			}
			TlsFrameHelper.ProcessingOptions options = ((!System.Net.NetEventSource.Log.IsEnabled()) ? TlsFrameHelper.ProcessingOptions.ServerName : TlsFrameHelper.ProcessingOptions.All);
			if (!TlsFrameHelper.TryGetFrameInfo(_handshakeBuffer.ActiveReadOnlySpan, ref _lastFrame, options) && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"Failed to parse TLS hello.", "ReceiveBlobAsync");
			}
			if (_lastFrame.HandshakeType == TlsHandshakeType.ClientHello)
			{
				if (_lastFrame.TargetName != null)
				{
					_sslAuthenticationOptions.TargetHost = _lastFrame.TargetName;
				}
				if (_sslAuthenticationOptions.ServerOptionDelegate != null)
				{
					SslServerAuthenticationOptions sslServerAuthenticationOptions = await _sslAuthenticationOptions.ServerOptionDelegate(this, new SslClientHelloInfo(_sslAuthenticationOptions.TargetHost, _lastFrame.SupportedVersions), _sslAuthenticationOptions.UserState, adapter.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					_sslAuthenticationOptions.UpdateOptions(sslServerAuthenticationOptions);
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Log.ReceivedFrame(this, _lastFrame);
			}
			break;
		}
		case TlsContentType.AppData:
			if (_isRenego && SslProtocol != SslProtocols.Tls13)
			{
				throw new InvalidOperationException(System.SR.net_ssl_renegotiate_data);
			}
			break;
		}
		return ProcessBlob(frameSize);
	}

	private ProtocolToken ProcessBlob(int frameSize)
	{
		int num = frameSize;
		ReadOnlySpan<byte> activeReadOnlySpan = _handshakeBuffer.ActiveReadOnlySpan;
		_handshakeBuffer.Discard(frameSize);
		if (_framing == Framing.SinceSSL3)
		{
			while (_handshakeBuffer.ActiveLength > 5)
			{
				TlsFrameHeader header = default(TlsFrameHeader);
				if (!TlsFrameHelper.TryGetFrameHeader(_handshakeBuffer.ActiveReadOnlySpan, ref header))
				{
					break;
				}
				frameSize = header.Length + 5;
				if ((header.Type != TlsContentType.Handshake && header.Type != TlsContentType.ChangeCipherSpec) || frameSize > _handshakeBuffer.ActiveLength)
				{
					break;
				}
				num += frameSize;
				_handshakeBuffer.Discard(frameSize);
			}
		}
		return _context.NextMessage(activeReadOnlySpan.Slice(0, num));
	}

	private void SendAuthResetSignal(ProtocolToken message, ExceptionDispatchInfo exception)
	{
		SetException(exception.SourceException);
		if (message == null || message.Size == 0)
		{
			exception.Throw();
		}
		base.InnerStream.Write(message.Payload, 0, message.Size);
		exception.Throw();
	}

	private bool CompleteHandshake(ref ProtocolToken alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus)
	{
		_context.ProcessHandshakeSuccess();
		if (_nestedAuth != 1)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"Ignoring unsolicited renegotiated certificate.", "CompleteHandshake");
			}
			sslPolicyErrors = SslPolicyErrors.None;
			chainStatus = X509ChainStatusFlags.NoError;
			return true;
		}
		if (!_context.VerifyRemoteCertificate(_sslAuthenticationOptions.CertValidationDelegate, _sslAuthenticationOptions.CertificateContext?.Trust, ref alertToken, out sslPolicyErrors, out chainStatus))
		{
			_handshakeCompleted = false;
			return false;
		}
		_handshakeCompleted = true;
		return true;
	}

	private void CompleteHandshake(SslAuthenticationOptions sslAuthenticationOptions)
	{
		ProtocolToken alertToken = null;
		if (!CompleteHandshake(ref alertToken, out var sslPolicyErrors, out var chainStatus))
		{
			if (sslAuthenticationOptions.CertValidationDelegate != null)
			{
				SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(System.SR.net_ssl_io_cert_custom_validation, null)));
			}
			else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && chainStatus != 0)
			{
				SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(System.SR.Format(System.SR.net_ssl_io_cert_chain_validation, chainStatus), null)));
			}
			else
			{
				SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(System.SR.Format(System.SR.net_ssl_io_cert_validation, sslPolicyErrors), null)));
			}
		}
	}

	private async ValueTask WriteAsyncChunked<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer) where TIOAdapter : struct, IReadWriteAdapter
	{
		do
		{
			int chunkBytes = Math.Min(buffer.Length, MaxDataSize);
			await WriteSingleChunk(writeAdapter, buffer.Slice(0, chunkBytes)).ConfigureAwait(continueOnCapturedContext: false);
			buffer = buffer.Slice(chunkBytes);
		}
		while (buffer.Length != 0);
	}

	private ValueTask WriteSingleChunk<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer) where TIOAdapter : struct, IReadWriteAdapter
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length + 64);
		byte[] outBuffer = array;
		SecurityStatusPal status;
		int outSize;
		while (true)
		{
			status = EncryptData(buffer, ref outBuffer, out outSize);
			if (status.ErrorCode != SecurityStatusPalErrorCode.TryAgain)
			{
				break;
			}
			TaskCompletionSource<bool> handshakeWaiter = _handshakeWaiter;
			if (handshakeWaiter != null)
			{
				Task task = writeAdapter.WaitAsync(handshakeWaiter);
				if (!task.IsCompletedSuccessfully)
				{
					return WaitAndWriteAsync(writeAdapter, buffer, task, array);
				}
			}
		}
		if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
		{
			ArrayPool<byte>.Shared.Return(array);
			return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(System.SR.net_io_encrypt, SslStreamPal.GetException(status))));
		}
		ValueTask valueTask = writeAdapter.WriteAsync(outBuffer, 0, outSize);
		if (valueTask.IsCompletedSuccessfully)
		{
			ArrayPool<byte>.Shared.Return(array);
			return valueTask;
		}
		return CompleteWriteAsync(valueTask, array);
		static async ValueTask CompleteWriteAsync(ValueTask writeTask, byte[] bufferToReturn)
		{
			try
			{
				await writeTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(bufferToReturn);
			}
		}
		async ValueTask WaitAndWriteAsync(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer, Task waitTask, byte[] rentedBuffer)
		{
			byte[] bufferToReturn2 = rentedBuffer;
			byte[] outBuffer2 = rentedBuffer;
			try
			{
				await waitTask.ConfigureAwait(continueOnCapturedContext: false);
				int outSize2;
				SecurityStatusPal status2 = EncryptData(buffer, ref outBuffer2, out outSize2);
				if (status2.ErrorCode == SecurityStatusPalErrorCode.TryAgain)
				{
					byte[] array2 = bufferToReturn2;
					bufferToReturn2 = null;
					ArrayPool<byte>.Shared.Return(array2);
					await WriteSingleChunk(writeAdapter, buffer).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					if (status2.ErrorCode != SecurityStatusPalErrorCode.OK)
					{
						throw new IOException(System.SR.net_io_encrypt, SslStreamPal.GetException(status2));
					}
					await writeAdapter.WriteAsync(outBuffer2, 0, outSize2).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			finally
			{
				if (bufferToReturn2 != null)
				{
					ArrayPool<byte>.Shared.Return(bufferToReturn2);
				}
			}
		}
	}

	~SslStream()
	{
		Dispose(disposing: false);
	}

	private void ReturnReadBufferIfEmpty()
	{
		byte[] internalBuffer = _internalBuffer;
		if (internalBuffer != null && _decryptedBytesCount == 0 && _internalBufferCount == 0)
		{
			_internalBuffer = null;
			_internalOffset = 0;
			_decryptedBytesOffset = 0;
			ArrayPool<byte>.Shared.Return(internalBuffer);
		}
		else if (_decryptedBytesCount == 0)
		{
			_decryptedBytesOffset = 0;
		}
	}

	private bool HaveFullTlsFrame(out int frameSize)
	{
		if (_internalBufferCount < 5)
		{
			frameSize = int.MaxValue;
			return false;
		}
		frameSize = GetFrameSize(_internalBuffer.AsSpan(_internalOffset));
		return _internalBufferCount >= frameSize;
	}

	private async ValueTask<int> EnsureFullTlsFrameAsync<TIOAdapter>(TIOAdapter adapter) where TIOAdapter : IReadWriteAdapter
	{
		if (HaveFullTlsFrame(out var frameSize))
		{
			return frameSize;
		}
		ResetReadBuffer();
		while (_internalBufferCount < frameSize)
		{
			int num = await adapter.ReadAsync(_internalBuffer.AsMemory(_internalBufferCount)).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				if (_internalBufferCount != 0)
				{
					throw new IOException(System.SR.net_io_eof);
				}
				return 0;
			}
			_internalBufferCount += num;
			if (frameSize == int.MaxValue && _internalBufferCount > 5)
			{
				frameSize = GetFrameSize(_internalBuffer.AsSpan(_internalOffset));
			}
		}
		return frameSize;
	}

	private SecurityStatusPal DecryptData(int frameSize)
	{
		_decryptedBytesOffset = _internalOffset;
		_decryptedBytesCount = frameSize;
		SecurityStatusPal result;
		lock (_handshakeLock)
		{
			ThrowIfExceptionalOrNotAuthenticated();
			result = _context.Decrypt(new Span<byte>(_internalBuffer, _internalOffset, frameSize), out var outputOffset, out var outputCount);
			_decryptedBytesCount = outputCount;
			if (outputCount > 0)
			{
				_decryptedBytesOffset = _internalOffset + outputOffset;
			}
			if (result.ErrorCode == SecurityStatusPalErrorCode.Renegotiate && (_sslAuthenticationOptions.AllowRenegotiation || SslProtocol == SslProtocols.Tls13 || _nestedAuth != 0))
			{
				_handshakeWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}
		ConsumeBufferedBytes(frameSize);
		return result;
	}

	private async ValueTask<int> ReadAsyncInternal<TIOAdapter>(TIOAdapter adapter, Memory<byte> buffer) where TIOAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "ReadAsync", "read"));
		}
		ThrowIfExceptionalOrNotAuthenticated();
		int processedLength = 0;
		int frameSize = 0;
		try
		{
			if (_decryptedBytesCount != 0)
			{
				processedLength = CopyDecryptedData(buffer);
				if (processedLength == buffer.Length || !HaveFullTlsFrame(out frameSize))
				{
					return processedLength;
				}
				buffer = buffer.Slice(processedLength);
			}
			if (_receivedEOF)
			{
				return 0;
			}
			if (buffer.Length == 0 && _internalBuffer == null)
			{
				await adapter.ReadAsync(Memory<byte>.Empty).ConfigureAwait(continueOnCapturedContext: false);
			}
			while (true)
			{
				frameSize = await EnsureFullTlsFrameAsync(adapter).ConfigureAwait(continueOnCapturedContext: false);
				if (frameSize == 0)
				{
					_receivedEOF = true;
					break;
				}
				SecurityStatusPal securityStatusPal = DecryptData(frameSize);
				if (securityStatusPal.ErrorCode != SecurityStatusPalErrorCode.OK)
				{
					byte[] array = null;
					if (_decryptedBytesCount != 0)
					{
						array = new byte[_decryptedBytesCount];
						Buffer.BlockCopy(_internalBuffer, _decryptedBytesOffset, array, 0, _decryptedBytesCount);
						_decryptedBytesCount = 0;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(null, $"***Processing an error Status = {securityStatusPal}", "ReadAsyncInternal");
					}
					if (securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
					{
						if (_handshakeWaiter == null)
						{
							throw new IOException(System.SR.net_ssl_io_renego);
						}
						await ReplyOnReAuthenticationAsync(adapter, array).ConfigureAwait(continueOnCapturedContext: false);
						continue;
					}
					if (securityStatusPal.ErrorCode == SecurityStatusPalErrorCode.ContextExpired)
					{
						_receivedEOF = true;
						break;
					}
					throw new IOException(System.SR.net_io_decrypt, SslStreamPal.GetException(securityStatusPal));
				}
				if (_decryptedBytesCount > 0)
				{
					int num = CopyDecryptedData(buffer);
					processedLength += num;
					if (num == buffer.Length)
					{
						break;
					}
					buffer = buffer.Slice(num);
				}
				if (processedLength != 0)
				{
					if (!HaveFullTlsFrame(out frameSize))
					{
						break;
					}
					TlsFrameHelper.TryGetFrameHeader(_internalBuffer.AsSpan(_internalOffset), ref _lastFrame.Header);
					if (_lastFrame.Header.Type != TlsContentType.AppData)
					{
						break;
					}
				}
			}
			return processedLength;
		}
		catch (Exception ex)
		{
			if (ex is IOException || (ex is OperationCanceledException && adapter.CancellationToken.IsCancellationRequested))
			{
				throw;
			}
			throw new IOException(System.SR.net_io_read, ex);
		}
		finally
		{
			ReturnReadBufferIfEmpty();
			_nestedRead = 0;
		}
	}

	private ValueTask<int> FillHandshakeBufferAsync<TIOAdapter>(TIOAdapter adapter, int minSize) where TIOAdapter : IReadWriteAdapter
	{
		if (_handshakeBuffer.ActiveLength >= minSize)
		{
			return new ValueTask<int>(minSize);
		}
		int byteCount = minSize - _handshakeBuffer.ActiveLength;
		_handshakeBuffer.EnsureAvailableSpace(byteCount);
		while (_handshakeBuffer.ActiveLength < minSize)
		{
			ValueTask<int> task2 = adapter.ReadAsync(_handshakeBuffer.AvailableMemory);
			if (!task2.IsCompletedSuccessfully)
			{
				return InternalFillHandshakeBufferAsync(adapter, task2, minSize);
			}
			int result = task2.Result;
			if (result == 0)
			{
				return new ValueTask<int>(0);
			}
			_handshakeBuffer.Commit(result);
		}
		return new ValueTask<int>(minSize);
		async ValueTask<int> InternalFillHandshakeBufferAsync(TIOAdapter adap, ValueTask<int> task, int minSize)
		{
			while (true)
			{
				int num = await task.ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					throw new IOException(System.SR.net_io_eof);
				}
				_handshakeBuffer.Commit(num);
				if (_handshakeBuffer.ActiveLength >= minSize)
				{
					break;
				}
				task = adap.ReadAsync(_handshakeBuffer.AvailableMemory);
			}
			return minSize;
		}
	}

	private async ValueTask WriteAsyncInternal<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer) where TIOAdapter : struct, IReadWriteAdapter
	{
		ThrowIfExceptionalOrNotAuthenticatedOrShutdown();
		_ = buffer.Length;
		if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "WriteAsync", "write"));
		}
		try
		{
			await ((buffer.Length < MaxDataSize) ? WriteSingleChunk(writeAdapter, buffer) : WriteAsyncChunked(writeAdapter, buffer)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			if (ex is IOException || (ex is OperationCanceledException && writeAdapter.CancellationToken.IsCancellationRequested))
			{
				throw;
			}
			throw new IOException(System.SR.net_io_write, ex);
		}
		finally
		{
			_nestedWrite = 0;
		}
	}

	private void ConsumeBufferedBytes(int byteCount)
	{
		_internalOffset += byteCount;
		_internalBufferCount -= byteCount;
		if (_internalBufferCount == 0)
		{
			_internalOffset = 0;
		}
	}

	private int CopyDecryptedData(Memory<byte> buffer)
	{
		int num = Math.Min(_decryptedBytesCount, buffer.Length);
		if (num != 0)
		{
			new ReadOnlySpan<byte>(_internalBuffer, _decryptedBytesOffset, num).CopyTo(buffer.Span);
			_decryptedBytesOffset += num;
			_decryptedBytesCount -= num;
		}
		if (_decryptedBytesCount == 0)
		{
			_decryptedBytesOffset = 0;
		}
		return num;
	}

	private void ResetReadBuffer()
	{
		if (_internalBuffer == null)
		{
			_internalBuffer = ArrayPool<byte>.Shared.Rent(16448);
		}
		else if (_internalOffset > 0)
		{
			Buffer.BlockCopy(_internalBuffer, _internalOffset, _internalBuffer, 0, _internalBufferCount);
			_internalOffset = 0;
		}
	}

	private Framing DetectFraming(ReadOnlySpan<byte> bytes)
	{
		int num = -1;
		if (bytes[0] == 22 || bytes[0] == 23 || bytes[0] == 21)
		{
			if (bytes.Length < 3)
			{
				return Framing.Invalid;
			}
			num = (bytes[1] << 8) | bytes[2];
			if (num < 768 || num >= 1280)
			{
				return Framing.Invalid;
			}
			return Framing.SinceSSL3;
		}
		if (bytes.Length < 3)
		{
			return Framing.Invalid;
		}
		if (bytes[2] > 8)
		{
			return Framing.Invalid;
		}
		if (bytes[2] == 1)
		{
			if (bytes.Length >= 5)
			{
				num = (bytes[3] << 8) | bytes[4];
			}
		}
		else if (bytes[2] == 4 && bytes.Length >= 7)
		{
			num = (bytes[5] << 8) | bytes[6];
		}
		if (num != -1)
		{
			if (_framing == Framing.Unknown)
			{
				if (num != 2 && (num < 512 || num >= 1280))
				{
					return Framing.Invalid;
				}
			}
			else if (num != 2)
			{
				return Framing.Invalid;
			}
		}
		if (!_context.IsServer || _framing == Framing.Unified)
		{
			return Framing.BeforeSSL3;
		}
		return Framing.Unified;
	}

	private int GetFrameSize(ReadOnlySpan<byte> buffer)
	{
		int num = -1;
		switch (_framing)
		{
		case Framing.BeforeSSL3:
		case Framing.Unified:
			if (buffer.Length < 2)
			{
				throw new IOException(System.SR.net_ssl_io_frame);
			}
			if ((buffer[0] & 0x80u) != 0)
			{
				return (((buffer[0] & 0x7F) << 8) | buffer[1]) + 2;
			}
			return (((buffer[0] & 0x3F) << 8) | buffer[1]) + 3;
		case Framing.SinceSSL3:
			if (buffer.Length < 5)
			{
				throw new IOException(System.SR.net_ssl_io_frame);
			}
			return ((buffer[3] << 8) | buffer[4]) + 5;
		default:
			throw new IOException(System.SR.net_frame_read_size);
		}
	}
}
