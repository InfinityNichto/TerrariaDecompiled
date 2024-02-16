using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

[UnsupportedOSPlatform("tvos")]
internal sealed class NTAuthentication
{
	private bool _isServer;

	private System.Net.Security.SafeFreeCredentials _credentialsHandle;

	private System.Net.Security.SafeDeleteContext _securityContext;

	private string _spn;

	private int _tokenSize;

	private System.Net.ContextFlagsPal _requestedContextFlags;

	private System.Net.ContextFlagsPal _contextFlags;

	private bool _isCompleted;

	private string _package;

	private ChannelBinding _channelBinding;

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

	internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, System.Net.ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
	{
		Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
	}

	[MemberNotNull("_package")]
	private void Initialize(bool isServer, string package, NetworkCredential credential, string spn, System.Net.ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"package={package}, spn={spn}, requestedContextFlags={requestedContextFlags}", "Initialize");
		}
		_tokenSize = System.Net.Security.NegotiateStreamPal.QueryMaxTokenSize(package);
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
			_credentialsHandle = System.Net.Security.NegotiateStreamPal.AcquireDefaultCredential(package, _isServer);
		}
		else
		{
			_credentialsHandle = System.Net.Security.NegotiateStreamPal.AcquireCredentialsHandle(package, _isServer, credential);
		}
	}

	internal void CloseContext()
	{
		if (_securityContext != null && !_securityContext.IsClosed)
		{
			_securityContext.Dispose();
		}
	}

	internal string GetOutgoingBlob(string incomingBlob)
	{
		byte[] array = null;
		if (incomingBlob != null && incomingBlob.Length > 0)
		{
			array = Convert.FromBase64String(incomingBlob);
		}
		byte[] array2 = null;
		if ((IsValidContext || IsCompleted) && array == null)
		{
			_isCompleted = true;
		}
		else
		{
			array2 = GetOutgoingBlob(array, throwOnError: true, out var _);
		}
		string result = null;
		if (array2 != null && array2.Length != 0)
		{
			result = Convert.ToBase64String(array2);
		}
		if (IsCompleted)
		{
			CloseContext();
		}
		return result;
	}

	internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError, out System.Net.SecurityStatusPal statusCode)
	{
		byte[] resultBlob = new byte[_tokenSize];
		bool flag = _securityContext == null;
		try
		{
			if (!_isServer)
			{
				statusCode = System.Net.Security.NegotiateStreamPal.InitializeSecurityContext(ref _credentialsHandle, ref _securityContext, _spn, _requestedContextFlags, incomingBlob, _channelBinding, ref resultBlob, ref _contextFlags);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode})", "GetOutgoingBlob");
				}
				if (statusCode.ErrorCode == System.Net.SecurityStatusPalErrorCode.CompleteNeeded)
				{
					statusCode = System.Net.Security.NegotiateStreamPal.CompleteAuthToken(ref _securityContext, resultBlob);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode})", "GetOutgoingBlob");
					}
					resultBlob = null;
				}
			}
			else
			{
				statusCode = System.Net.Security.NegotiateStreamPal.AcceptSecurityContext(_credentialsHandle, ref _securityContext, _requestedContextFlags, incomingBlob, _channelBinding, ref resultBlob, ref _contextFlags);
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
		if (statusCode.ErrorCode >= System.Net.SecurityStatusPalErrorCode.OutOfMemory)
		{
			CloseContext();
			_isCompleted = true;
			if (throwOnError)
			{
				throw System.Net.Security.NegotiateStreamPal.CreateExceptionFromError(statusCode);
			}
			return null;
		}
		if (flag && _credentialsHandle != null)
		{
			System.Net.Security.SSPIHandleCache.CacheCredential(_credentialsHandle);
		}
		if (statusCode.ErrorCode == System.Net.SecurityStatusPalErrorCode.OK || (_isServer && statusCode.ErrorCode == System.Net.SecurityStatusPalErrorCode.CompleteNeeded))
		{
			_isCompleted = true;
		}
		else if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"need continue statusCode:0x{(int)statusCode.ErrorCode:x8} ({statusCode}) _securityContext:{_securityContext}", "GetOutgoingBlob");
		}
		return resultBlob;
	}
}
