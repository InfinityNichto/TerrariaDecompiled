using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

public sealed class HttpListenerContext
{
	internal HttpListener _listener;

	private HttpListenerResponse _response;

	private IPrincipal _user;

	private string _mutualAuthentication;

	public HttpListenerRequest Request { get; }

	public IPrincipal? User => _user;

	internal AuthenticationSchemes AuthenticationSchemes { get; set; }

	public HttpListenerResponse Response
	{
		get
		{
			if (_response == null)
			{
				_response = new HttpListenerResponse(this);
			}
			return _response;
		}
	}

	internal HttpListenerSession ListenerSession { get; private set; }

	internal ExtendedProtectionPolicy ExtendedProtectionPolicy { get; set; }

	internal string? MutualAuthentication => _mutualAuthentication;

	internal HttpListener? Listener => _listener;

	internal SafeHandle RequestQueueHandle => ListenerSession.RequestQueueHandle;

	internal ThreadPoolBoundHandle RequestQueueBoundHandle => ListenerSession.RequestQueueBoundHandle;

	internal ulong RequestId => Request.RequestId;

	public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol)
	{
		return AcceptWebSocketAsync(subProtocol, 16384, WebSocket.DefaultKeepAliveInterval);
	}

	public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, TimeSpan keepAliveInterval)
	{
		return AcceptWebSocketAsync(subProtocol, 16384, keepAliveInterval);
	}

	internal unsafe HttpListenerContext(HttpListenerSession session, RequestContextBase memoryBlob)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"httpListener {session.Listener} requestBlob={(IntPtr)memoryBlob.RequestBlob}", ".ctor");
		}
		_listener = session.Listener;
		ListenerSession = session;
		Request = new HttpListenerRequest(this, memoryBlob);
		AuthenticationSchemes = _listener.AuthenticationSchemes;
		ExtendedProtectionPolicy = _listener.ExtendedProtectionPolicy;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"HttpListener: {_listener} HttpListenerRequest: {Request}", ".ctor");
		}
	}

	internal void SetIdentity(IPrincipal principal, string mutualAuthentication)
	{
		_mutualAuthentication = mutualAuthentication;
		_user = principal;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("mutual: {0}, Principal: {1}", (mutualAuthentication == null) ? "<null>" : mutualAuthentication, principal), "SetIdentity");
		}
	}

	public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
	{
		HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, 16, keepAliveInterval);
		ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, 16, isServerBuffer: true);
		return AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
	{
		return HttpWebSocket.AcceptWebSocketAsync(this, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
	}

	internal void Close()
	{
		try
		{
			_response?.Close();
		}
		finally
		{
			try
			{
				Request.Close();
			}
			finally
			{
				IDisposable disposable = ((_user == null) ? null : (_user.Identity as IDisposable));
				if (disposable != null && _user.Identity.AuthenticationType != "NTLM" && !_listener.UnsafeConnectionNtlmAuthentication)
				{
					disposable.Dispose();
				}
			}
		}
	}

	internal void Abort()
	{
		ForceCancelRequest(RequestQueueHandle, Request.RequestId);
		try
		{
			Request.Close();
		}
		finally
		{
			(_user?.Identity as IDisposable)?.Dispose();
		}
	}

	internal global::Interop.HttpApi.HTTP_VERB GetKnownMethod()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("Visited {0}()", "GetKnownMethod"), "GetKnownMethod");
		}
		return global::Interop.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
	}

	internal static void CancelRequest(SafeHandle requestQueueHandle, ulong requestId)
	{
		global::Interop.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId, IntPtr.Zero);
	}

	internal void ForceCancelRequest(SafeHandle requestQueueHandle, ulong requestId)
	{
		uint num = global::Interop.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId, IntPtr.Zero);
		if (num == 1229)
		{
			_response.CancelLastWrite(requestQueueHandle);
		}
	}

	internal void SetAuthenticationHeaders()
	{
		Listener.SetAuthenticationHeaders(this);
	}
}
