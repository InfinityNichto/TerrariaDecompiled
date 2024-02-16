using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

[UnsupportedOSPlatform("tvos")]
public class NegotiateStream : AuthenticatedStream
{
	private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException("NegotiateStream", (string?)null));

	private static readonly byte[] s_emptyMessage = new byte[0];

	private readonly byte[] _readHeader;

	private IIdentity _remoteIdentity;

	private byte[] _readBuffer;

	private int _readBufferOffset;

	private int _readBufferCount;

	private byte[] _writeBuffer;

	private volatile int _writeInProgress;

	private volatile int _readInProgress;

	private volatile int _authInProgress;

	private ExceptionDispatchInfo _exception;

	private StreamFramer _framer;

	private NTAuthentication _context;

	private bool _canRetryAuthentication;

	private ProtectionLevel _expectedProtectionLevel;

	private TokenImpersonationLevel _expectedImpersonationLevel;

	private uint _writeSequenceNumber;

	private uint _readSequenceNumber;

	private ExtendedProtectionPolicy _extendedProtectionPolicy;

	private bool _remoteOk;

	public override bool IsAuthenticated => IsAuthenticatedCore;

	[MemberNotNullWhen(true, "_context")]
	private bool IsAuthenticatedCore
	{
		[MemberNotNullWhen(true, "_context")]
		get
		{
			if (_context != null && HandshakeComplete && _exception == null)
			{
				return _remoteOk;
			}
			return false;
		}
	}

	public override bool IsMutuallyAuthenticated
	{
		get
		{
			if (IsAuthenticatedCore && !_context.IsNTLM)
			{
				return _context.IsMutualAuthFlag;
			}
			return false;
		}
	}

	public override bool IsEncrypted
	{
		get
		{
			if (IsAuthenticatedCore)
			{
				return _context.IsConfidentialityFlag;
			}
			return false;
		}
	}

	public override bool IsSigned
	{
		get
		{
			if (IsAuthenticatedCore)
			{
				if (!_context.IsIntegrityFlag)
				{
					return _context.IsConfidentialityFlag;
				}
				return true;
			}
			return false;
		}
	}

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

	public virtual TokenImpersonationLevel ImpersonationLevel
	{
		get
		{
			ThrowIfFailed(authSuccessCheck: true);
			return PrivateImpersonationLevel;
		}
	}

	private TokenImpersonationLevel PrivateImpersonationLevel
	{
		get
		{
			if (!_context.IsDelegationFlag || !(_context.ProtocolName != "NTLM"))
			{
				if (!_context.IsIdentifyFlag)
				{
					return TokenImpersonationLevel.Impersonation;
				}
				return TokenImpersonationLevel.Identification;
			}
			return TokenImpersonationLevel.Delegation;
		}
	}

	private bool HandshakeComplete
	{
		get
		{
			if (_context.IsCompleted)
			{
				return _context.IsValidContext;
			}
			return false;
		}
	}

	private bool CanGetSecureStream
	{
		get
		{
			if (!_context.IsConfidentialityFlag)
			{
				return _context.IsIntegrityFlag;
			}
			return true;
		}
	}

	public virtual IIdentity RemoteIdentity
	{
		get
		{
			IIdentity identity = _remoteIdentity;
			if (identity == null)
			{
				ThrowIfFailed(authSuccessCheck: true);
				identity = (_remoteIdentity = NegotiateStreamPal.GetIdentity(_context));
			}
			return identity;
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
			if (IsAuthenticated)
			{
				return base.InnerStream.CanWrite;
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

	public NegotiateStream(Stream innerStream)
		: this(innerStream, leaveInnerStreamOpen: false)
	{
	}

	public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen)
		: base(innerStream, leaveInnerStreamOpen)
	{
		_readHeader = new byte[4];
		_readBuffer = Array.Empty<byte>();
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			_exception = s_disposedSentinel;
			_context?.CloseContext();
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
			_exception = s_disposedSentinel;
			_context?.CloseContext();
		}
		finally
		{
			await base.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AuthenticateAsClientAsync(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public virtual void AuthenticateAsServer()
	{
		AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsServer(ExtendedProtectionPolicy? policy)
	{
		AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		AuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel);
	}

	public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
		AuthenticateAsync(new SyncReadWriteAdapter(base.InnerStream), "AuthenticateAsServer").GetAwaiter().GetResult();
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(ExtendedProtectionPolicy? policy, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return BeginAuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel, asyncCallback, asyncState);
	}

	public virtual IAsyncResult BeginAuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AuthenticateAsServerAsync(credential, policy, requiredProtectionLevel, requiredImpersonationLevel), asyncCallback, asyncState);
	}

	public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public virtual void AuthenticateAsClient()
	{
		AuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName)
	{
		AuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName)
	{
		AuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		AuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);
	}

	public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
		AuthenticateAsync(new SyncReadWriteAdapter(base.InnerStream), "AuthenticateAsClient").GetAwaiter().GetResult();
	}

	public virtual Task AuthenticateAsClientAsync()
	{
		return AuthenticateAsClientAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName)
	{
		return AuthenticateAsClientAsync(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		return AuthenticateAsClientAsync(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName)
	{
		return AuthenticateAsClientAsync(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
		return AuthenticateAsync(new AsyncReadWriteAdapter(base.InnerStream, default(CancellationToken)), "AuthenticateAsClientAsync");
	}

	public virtual Task AuthenticateAsServerAsync()
	{
		return AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy? policy)
	{
		return AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
	}

	public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		return AuthenticateAsServerAsync(credential, null, requiredProtectionLevel, requiredImpersonationLevel);
	}

	public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
	{
		ValidateCreateContext("Negotiate", credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
		return AuthenticateAsync(new AsyncReadWriteAdapter(base.InnerStream, default(CancellationToken)), "AuthenticateAsServerAsync");
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

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.Read(buffer, offset, count);
		}
		return ReadAsync(new SyncReadWriteAdapter(base.InnerStream), new Memory<byte>(buffer, offset, count), "Read").GetAwaiter().GetResult();
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
		}
		return ReadAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), new Memory<byte>(buffer, offset, count), "ReadAsync").AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.ReadAsync(buffer, cancellationToken);
		}
		return ReadAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), buffer, "ReadAsync");
	}

	private async ValueTask<int> ReadAsync<TAdapter>(TAdapter adapter, Memory<byte> buffer, [CallerMemberName] string callerName = null) where TAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _readInProgress, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, callerName, "read"));
		}
		try
		{
			if (_readBufferCount != 0)
			{
				int num = Math.Min(_readBufferCount, buffer.Length);
				if (num != 0)
				{
					_readBuffer.AsMemory(_readBufferOffset, num).CopyTo(buffer);
					_readBufferOffset += num;
					_readBufferCount -= num;
				}
				return num;
			}
			int num2;
			do
			{
				if (await ReadAllAsync(adapter, _readHeader, allowZeroRead: true).ConfigureAwait(continueOnCapturedContext: false) == 0)
				{
					return 0;
				}
				num2 = BitConverter.ToInt32(_readHeader, 0);
				if (num2 <= 4 || num2 > 65536)
				{
					throw new IOException(System.SR.net_frame_read_size);
				}
				_readBufferCount = num2;
				_readBufferOffset = 0;
				if (_readBuffer.Length < num2)
				{
					_readBuffer = new byte[num2];
				}
				num2 = await ReadAllAsync(adapter, new Memory<byte>(_readBuffer, 0, num2), allowZeroRead: false).ConfigureAwait(continueOnCapturedContext: false);
				num2 = (_readBufferCount = DecryptData(_readBuffer, 0, num2, out _readBufferOffset));
			}
			while (num2 == 0 && buffer.Length != 0);
			if (num2 > buffer.Length)
			{
				num2 = buffer.Length;
			}
			_readBuffer.AsMemory(_readBufferOffset, num2).CopyTo(buffer);
			_readBufferOffset += num2;
			_readBufferCount -= num2;
			return num2;
		}
		catch (Exception ex) when (!(ex is IOException) && !(ex is OperationCanceledException))
		{
			throw new IOException(System.SR.net_io_read, ex);
		}
		finally
		{
			_readInProgress = 0;
		}
		static async ValueTask<int> ReadAllAsync(TAdapter adapter, Memory<byte> buffer, bool allowZeroRead)
		{
			int read = 0;
			do
			{
				int num3 = await adapter.ReadAsync(buffer).ConfigureAwait(continueOnCapturedContext: false);
				if (num3 == 0)
				{
					if (read == 0 && allowZeroRead)
					{
						break;
					}
					throw new IOException(System.SR.net_io_eof);
				}
				buffer = buffer.Slice(num3);
				read += num3;
			}
			while (!buffer.IsEmpty);
			return read;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			base.InnerStream.Write(buffer, offset, count);
		}
		else
		{
			WriteAsync(new SyncReadWriteAdapter(base.InnerStream), new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
		}
		return WriteAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), new ReadOnlyMemory<byte>(buffer, offset, count));
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfFailed(authSuccessCheck: true);
		if (!CanGetSecureStream)
		{
			return base.InnerStream.WriteAsync(buffer, cancellationToken);
		}
		return new ValueTask(WriteAsync(new AsyncReadWriteAdapter(base.InnerStream, cancellationToken), buffer));
	}

	private async Task WriteAsync<TAdapter>(TAdapter adapter, ReadOnlyMemory<byte> buffer) where TAdapter : IReadWriteAdapter
	{
		if (Interlocked.Exchange(ref _writeInProgress, 1) == 1)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_io_invalidnestedcall, "Write", "write"));
		}
		try
		{
			while (!buffer.IsEmpty)
			{
				int chunkBytes = Math.Min(buffer.Length, 64512);
				int count;
				try
				{
					count = EncryptData(buffer.Slice(0, chunkBytes).Span, ref _writeBuffer);
				}
				catch (Exception innerException)
				{
					throw new IOException(System.SR.net_io_encrypt, innerException);
				}
				await adapter.WriteAsync(_writeBuffer, 0, count).ConfigureAwait(continueOnCapturedContext: false);
				buffer = buffer.Slice(chunkBytes);
			}
		}
		catch (Exception ex) when (!(ex is IOException) && !(ex is OperationCanceledException))
		{
			throw new IOException(System.SR.net_io_write, ex);
		}
		finally
		{
			_writeInProgress = 0;
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count), asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
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
				throw new ObjectDisposedException("NegotiateStream");
			}
			e.Throw();
		}
	}

	private void ValidateCreateContext(string package, NetworkCredential credential, string servicePrincipalName, ExtendedProtectionPolicy policy, ProtectionLevel protectionLevel, TokenImpersonationLevel impersonationLevel)
	{
		if (policy != null)
		{
			if (policy.CustomChannelBinding == null && policy.CustomServiceNames == null)
			{
				throw new ArgumentException(System.SR.net_auth_must_specify_extended_protection_scheme, "policy");
			}
			_extendedProtectionPolicy = policy;
		}
		else
		{
			_extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
		}
		ValidateCreateContext(package, isServer: true, credential, servicePrincipalName, _extendedProtectionPolicy.CustomChannelBinding, protectionLevel, impersonationLevel);
	}

	private void ValidateCreateContext(string package, bool isServer, NetworkCredential credential, string servicePrincipalName, ChannelBinding channelBinding, ProtectionLevel protectionLevel, TokenImpersonationLevel impersonationLevel)
	{
		if (!_canRetryAuthentication)
		{
			ThrowIfExceptional();
		}
		if (_context != null && _context.IsValidContext)
		{
			throw new InvalidOperationException(System.SR.net_auth_reauth);
		}
		if (credential == null)
		{
			throw new ArgumentNullException("credential");
		}
		if (servicePrincipalName == null)
		{
			throw new ArgumentNullException("servicePrincipalName");
		}
		NegotiateStreamPal.ValidateImpersonationLevel(impersonationLevel);
		if (_context != null && IsServer != isServer)
		{
			throw new InvalidOperationException(System.SR.net_auth_client_server);
		}
		_exception = null;
		_remoteOk = false;
		_framer = new StreamFramer();
		_framer.WriteHeader.MessageId = 22;
		_expectedProtectionLevel = protectionLevel;
		_expectedImpersonationLevel = (isServer ? impersonationLevel : TokenImpersonationLevel.None);
		_writeSequenceNumber = 0u;
		_readSequenceNumber = 0u;
		ContextFlagsPal contextFlagsPal = ContextFlagsPal.Connection;
		if (protectionLevel == ProtectionLevel.None && !isServer)
		{
			package = "NTLM";
		}
		else
		{
			switch (protectionLevel)
			{
			case ProtectionLevel.EncryptAndSign:
				contextFlagsPal |= ContextFlagsPal.Confidentiality;
				break;
			case ProtectionLevel.Sign:
				contextFlagsPal |= ContextFlagsPal.ReplayDetect | ContextFlagsPal.SequenceDetect | ContextFlagsPal.AcceptStream;
				break;
			}
		}
		if (isServer)
		{
			if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
			{
				contextFlagsPal |= ContextFlagsPal.AllowMissingBindings;
			}
			if (_extendedProtectionPolicy.PolicyEnforcement != 0 && _extendedProtectionPolicy.ProtectionScenario == ProtectionScenario.TrustedProxy)
			{
				contextFlagsPal |= ContextFlagsPal.ProxyBindings;
			}
		}
		else
		{
			if (protectionLevel != 0)
			{
				contextFlagsPal |= ContextFlagsPal.MutualAuth;
			}
			if (impersonationLevel == TokenImpersonationLevel.Identification)
			{
				contextFlagsPal |= ContextFlagsPal.AcceptIntegrity;
			}
			if (impersonationLevel == TokenImpersonationLevel.Delegation)
			{
				contextFlagsPal |= ContextFlagsPal.Delegate;
			}
		}
		_canRetryAuthentication = false;
		try
		{
			_context = new NTAuthentication(isServer, package, credential, servicePrincipalName, contextFlagsPal, channelBinding);
		}
		catch (Win32Exception innerException)
		{
			throw new AuthenticationException(System.SR.net_auth_SSPI, innerException);
		}
	}

	private void SetFailed(Exception e)
	{
		if (_exception == null || !(_exception.SourceException is ObjectDisposedException))
		{
			_exception = ExceptionDispatchInfo.Capture(e);
		}
		_context?.CloseContext();
	}

	private void ThrowIfFailed(bool authSuccessCheck)
	{
		ThrowIfExceptional();
		if (authSuccessCheck && !IsAuthenticatedCore)
		{
			throw new InvalidOperationException(System.SR.net_auth_noauth);
		}
	}

	private async Task AuthenticateAsync<TAdapter>(TAdapter adapter, [CallerMemberName] string callerName = null) where TAdapter : IReadWriteAdapter
	{
		ThrowIfFailed(authSuccessCheck: false);
		if (Interlocked.Exchange(ref _authInProgress, 1) == 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidnestedcall, callerName, "authenticate"));
		}
		try
		{
			await (_context.IsServer ? ReceiveBlobAsync(adapter) : SendBlobAsync(adapter, null)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception failed)
		{
			SetFailed(failed);
			throw;
		}
		finally
		{
			_authInProgress = 0;
		}
	}

	private bool CheckSpn()
	{
		if (_context.IsKerberos || _extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Never || _extendedProtectionPolicy.CustomServiceNames == null)
		{
			return true;
		}
		string clientSpecifiedSpn = _context.ClientSpecifiedSpn;
		if (string.IsNullOrEmpty(clientSpecifiedSpn))
		{
			return _extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported;
		}
		return _extendedProtectionPolicy.CustomServiceNames.Contains(clientSpecifiedSpn);
	}

	private async Task SendBlobAsync<TAdapter>(TAdapter adapter, byte[] message) where TAdapter : IReadWriteAdapter
	{
		Exception e = null;
		if (message != s_emptyMessage)
		{
			message = GetOutgoingBlob(message, ref e);
		}
		if (e != null)
		{
			await SendAuthResetSignalAndThrowAsync(adapter, message, e).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (HandshakeComplete)
		{
			if (_context.IsServer && !CheckSpn())
			{
				e = new AuthenticationException(System.SR.net_auth_bad_client_creds_or_target_mismatch);
				int num = 1790;
				message = new byte[8];
				for (int num2 = message.Length - 1; num2 >= 0; num2--)
				{
					message[num2] = (byte)((uint)num & 0xFFu);
					num >>>= 8;
				}
				await SendAuthResetSignalAndThrowAsync(adapter, message, e).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (PrivateImpersonationLevel < _expectedImpersonationLevel)
			{
				e = new AuthenticationException(System.SR.Format(System.SR.net_auth_context_expectation, _expectedImpersonationLevel.ToString(), PrivateImpersonationLevel.ToString()));
				int num3 = 1790;
				message = new byte[8];
				for (int num4 = message.Length - 1; num4 >= 0; num4--)
				{
					message[num4] = (byte)((uint)num3 & 0xFFu);
					num3 >>>= 8;
				}
				await SendAuthResetSignalAndThrowAsync(adapter, message, e).ConfigureAwait(continueOnCapturedContext: false);
			}
			ProtectionLevel protectionLevel = (_context.IsConfidentialityFlag ? ProtectionLevel.EncryptAndSign : (_context.IsIntegrityFlag ? ProtectionLevel.Sign : ProtectionLevel.None));
			if (protectionLevel < _expectedProtectionLevel)
			{
				e = new AuthenticationException(System.SR.Format(System.SR.net_auth_context_expectation, protectionLevel.ToString(), _expectedProtectionLevel.ToString()));
				int num5 = 1790;
				message = new byte[8];
				for (int num6 = message.Length - 1; num6 >= 0; num6--)
				{
					message[num6] = (byte)((uint)num5 & 0xFFu);
					num5 >>>= 8;
				}
				await SendAuthResetSignalAndThrowAsync(adapter, message, e).ConfigureAwait(continueOnCapturedContext: false);
			}
			_framer.WriteHeader.MessageId = 20;
			if (_context.IsServer)
			{
				_remoteOk = true;
				if (message == null)
				{
					message = s_emptyMessage;
				}
			}
		}
		else if (message == null || message == s_emptyMessage)
		{
			throw new InternalException();
		}
		if (message != null)
		{
			await _framer.WriteMessageAsync(adapter, message).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!HandshakeComplete || !_remoteOk)
		{
			await ReceiveBlobAsync(adapter).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task ReceiveBlobAsync<TAdapter>(TAdapter adapter) where TAdapter : IReadWriteAdapter
	{
		byte[] array = await _framer.ReadMessageAsync(adapter).ConfigureAwait(continueOnCapturedContext: false);
		if (array == null)
		{
			throw new AuthenticationException(System.SR.net_auth_eof);
		}
		if (_framer.ReadHeader.MessageId == 21)
		{
			if (array.Length >= 8)
			{
				long num = 0L;
				for (int i = 0; i < 8; i++)
				{
					num = (num << 8) + array[i];
				}
				ThrowCredentialException(num);
			}
			throw new AuthenticationException(System.SR.net_auth_alert);
		}
		if (_framer.ReadHeader.MessageId == 20)
		{
			_remoteOk = true;
		}
		else if (_framer.ReadHeader.MessageId != 22)
		{
			throw new AuthenticationException(System.SR.Format(System.SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, 22));
		}
		if (HandshakeComplete)
		{
			if (!_remoteOk)
			{
				throw new AuthenticationException(System.SR.Format(System.SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, 20));
			}
		}
		else
		{
			await SendBlobAsync(adapter, array).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task SendAuthResetSignalAndThrowAsync<TAdapter>(TAdapter adapter, byte[] message, Exception exception) where TAdapter : IReadWriteAdapter
	{
		_framer.WriteHeader.MessageId = 21;
		if (IsLogonDeniedException(exception))
		{
			exception = new InvalidCredentialException(IsServer ? System.SR.net_auth_bad_client_creds : System.SR.net_auth_bad_client_creds_or_target_mismatch, exception);
		}
		if (!(exception is AuthenticationException))
		{
			exception = new AuthenticationException(System.SR.net_auth_SSPI, exception);
		}
		await _framer.WriteMessageAsync(adapter, message).ConfigureAwait(continueOnCapturedContext: false);
		_canRetryAuthentication = true;
		ExceptionDispatchInfo.Throw(exception);
	}

	private static bool IsError(SecurityStatusPal status)
	{
		return status.ErrorCode >= SecurityStatusPalErrorCode.OutOfMemory;
	}

	private byte[] GetOutgoingBlob(byte[] incomingBlob, ref Exception e)
	{
		SecurityStatusPal statusCode;
		byte[] array = _context.GetOutgoingBlob(incomingBlob, throwOnError: false, out statusCode);
		if (IsError(statusCode))
		{
			e = NegotiateStreamPal.CreateExceptionFromError(statusCode);
			uint num = (uint)e.HResult;
			array = new byte[8];
			for (int num2 = array.Length - 1; num2 >= 0; num2--)
			{
				array[num2] = (byte)(num & 0xFFu);
				num >>= 8;
			}
		}
		if (array != null && array.Length == 0)
		{
			array = s_emptyMessage;
		}
		return array;
	}

	private int EncryptData(ReadOnlySpan<byte> buffer, [NotNull] ref byte[] outBuffer)
	{
		ThrowIfFailed(authSuccessCheck: true);
		_writeSequenceNumber++;
		return _context.Encrypt(buffer, ref outBuffer, _writeSequenceNumber);
	}

	private int DecryptData(byte[] buffer, int offset, int count, out int newOffset)
	{
		ThrowIfFailed(authSuccessCheck: true);
		_readSequenceNumber++;
		return _context.Decrypt(buffer, offset, count, out newOffset, _readSequenceNumber);
	}

	private static void ThrowCredentialException(long error)
	{
		Win32Exception ex = new Win32Exception((int)error);
		throw ex.NativeErrorCode switch
		{
			21 => new InvalidCredentialException(System.SR.net_auth_bad_client_creds, ex), 
			1790 => new AuthenticationException(System.SR.net_auth_context_expectation_remote, ex), 
			_ => new AuthenticationException(System.SR.net_auth_alert, ex), 
		};
	}

	private static bool IsLogonDeniedException(Exception exception)
	{
		if (exception is Win32Exception ex)
		{
			return ex.NativeErrorCode == 21;
		}
		return false;
	}
}
