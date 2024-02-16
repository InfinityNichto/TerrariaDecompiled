using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("tvos")]
internal sealed class NTAuthentication
{
	private bool _isServer;

	private SafeFreeCredentials _credentialsHandle;

	private SafeDeleteContext _securityContext;

	private string _spn;

	private int _tokenSize;

	private ContextFlagsPal _requestedContextFlags;

	private ContextFlagsPal _contextFlags;

	private bool _isCompleted;

	private string _package;

	private string _lastProtocolName;

	private string _protocolName;

	private string _clientSpecifiedSpn;

	private ChannelBinding _channelBinding;

	internal string AssociatedName
	{
		get
		{
			if (!IsValidContext || !IsCompleted)
			{
				throw new Win32Exception(10);
			}
			string text = NegotiateStreamPal.QueryContextAssociatedName(_securityContext);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"NTAuthentication: The context is associated with [{text}]", "AssociatedName");
			}
			return text;
		}
	}

	internal bool IsConfidentialityFlag => (_contextFlags & ContextFlagsPal.Confidentiality) != 0;

	internal bool IsIntegrityFlag => ((uint)_contextFlags & (uint)(_isServer ? 131072 : 65536)) != 0;

	internal bool IsMutualAuthFlag => (_contextFlags & ContextFlagsPal.MutualAuth) != 0;

	internal bool IsDelegationFlag => (_contextFlags & ContextFlagsPal.Delegate) != 0;

	internal bool IsIdentifyFlag => ((uint)_contextFlags & (uint)(_isServer ? 524288 : 131072)) != 0;

	internal string Spn => _spn;

	internal bool IsNTLM
	{
		get
		{
			if (_lastProtocolName == null)
			{
				_lastProtocolName = ProtocolName;
			}
			return (object)_lastProtocolName == "NTLM";
		}
	}

	internal bool IsCompleted => _isCompleted;

	internal bool IsValidContext
	{
		get
		{
			if (_securityContext != null)
			{
				return !_securityContext.IsInvalid;
			}
			return false;
		}
	}

	internal bool IsServer => _isServer;

	internal string ClientSpecifiedSpn
	{
		get
		{
			if (_clientSpecifiedSpn == null)
			{
				_clientSpecifiedSpn = GetClientSpecifiedSpn();
			}
			return _clientSpecifiedSpn;
		}
	}

	internal string ProtocolName
	{
		get
		{
			if (_protocolName == null)
			{
				string text = null;
				if (IsValidContext)
				{
					text = NegotiateStreamPal.QueryContextAuthenticationPackage(_securityContext);
					if (IsCompleted)
					{
						_protocolName = text;
					}
				}
				return text ?? string.Empty;
			}
			return _protocolName;
		}
	}

	internal bool IsKerberos
	{
		get
		{
			if (_lastProtocolName == null)
			{
				_lastProtocolName = ProtocolName;
			}
			return (object)_lastProtocolName == "Kerberos";
		}
	}

	internal int Encrypt(ReadOnlySpan<byte> buffer, [NotNull] ref byte[] output, uint sequenceNumber)
	{
		return NegotiateStreamPal.Encrypt(_securityContext, buffer, IsConfidentialityFlag, IsNTLM, ref output, sequenceNumber);
	}

	internal int Decrypt(byte[] payload, int offset, int count, out int newOffset, uint expectedSeqNumber)
	{
		return NegotiateStreamPal.Decrypt(_securityContext, payload, offset, count, IsConfidentialityFlag, IsNTLM, out newOffset, expectedSeqNumber);
	}

	internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
	{
		Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
	}

	[MemberNotNull("_package")]
	private void Initialize(bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"package={package}, spn={spn}, requestedContextFlags={requestedContextFlags}", "Initialize");
		}
		_tokenSize = NegotiateStreamPal.QueryMaxTokenSize(package);
		_isServer = isServer;
		_spn = spn;
		_securityContext = null;
		_requestedContextFlags = requestedContextFlags;
		_package = package;
		_channelBinding = channelBinding;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Peer SPN-> '{_spn}'", "Initialize");
		}
		if (credential == CredentialCache.DefaultCredentials)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "using DefaultCredentials", "Initialize");
			}
			_credentialsHandle = NegotiateStreamPal.AcquireDefaultCredential(package, _isServer);
		}
		else
		{
			_credentialsHandle = NegotiateStreamPal.AcquireCredentialsHandle(package, _isServer, credential);
		}
	}

	internal SafeDeleteContext GetContext(out SecurityStatusPal status)
	{
		status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
		if (!IsValidContext)
		{
			status = new SecurityStatusPal(SecurityStatusPalErrorCode.InvalidHandle);
			return null;
		}
		return _securityContext;
	}

	internal void CloseContext()
	{
		if (_securityContext != null && !_securityContext.IsClosed)
		{
			_securityContext.Dispose();
		}
	}

	internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
	{
		byte[] resultBlob = new byte[_tokenSize];
		bool flag = _securityContext == null;
		try
		{
			if (!_isServer)
			{
				statusCode = NegotiateStreamPal.InitializeSecurityContext(ref _credentialsHandle, ref _securityContext, _spn, _requestedContextFlags, incomingBlob, _channelBinding, ref resultBlob, ref _contextFlags);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode})", "GetOutgoingBlob");
				}
				if (statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
				{
					statusCode = NegotiateStreamPal.CompleteAuthToken(ref _securityContext, resultBlob);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode})", "GetOutgoingBlob");
					}
					resultBlob = null;
				}
			}
			else
			{
				statusCode = NegotiateStreamPal.AcceptSecurityContext(_credentialsHandle, ref _securityContext, _requestedContextFlags, incomingBlob, _channelBinding, ref resultBlob, ref _contextFlags);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"SSPIWrapper.AcceptSecurityContext() returns statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode})", "GetOutgoingBlob");
				}
			}
		}
		finally
		{
			if (flag)
			{
				_credentialsHandle?.Dispose();
			}
		}
		if (statusCode.ErrorCode >= SecurityStatusPalErrorCode.OutOfMemory)
		{
			CloseContext();
			_isCompleted = true;
			if (throwOnError)
			{
				throw NegotiateStreamPal.CreateExceptionFromError(statusCode);
			}
			return null;
		}
		if (flag && _credentialsHandle != null)
		{
			SSPIHandleCache.CacheCredential(_credentialsHandle);
		}
		if (statusCode.ErrorCode == SecurityStatusPalErrorCode.OK || (_isServer && statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded))
		{
			_isCompleted = true;
		}
		else if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"need continue statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode}) _securityContext:{_securityContext}", "GetOutgoingBlob");
		}
		return resultBlob;
	}

	private string GetClientSpecifiedSpn()
	{
		string text = NegotiateStreamPal.QueryContextClientSpecifiedSpn(_securityContext);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"The client specified SPN is [{text}]", "GetClientSpecifiedSpn");
		}
		return text;
	}
}
