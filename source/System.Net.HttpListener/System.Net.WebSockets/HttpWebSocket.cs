using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

internal static class HttpWebSocket
{
	private static string SupportedVersion => WebSocketProtocolComponent.SupportedVersion;

	private static bool WebSocketsSupported { get; } = Environment.OSVersion.Version >= new Version(6, 2);


	internal static string GetSecWebSocketAcceptString(string secWebSocketKey)
	{
		string s = secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		byte[] inArray = SHA1.HashData(bytes);
		return Convert.ToBase64String(inArray);
	}

	internal static bool ProcessWebSocketProtocolHeader(string clientSecWebSocketProtocol, string subProtocol, out string acceptProtocol)
	{
		acceptProtocol = string.Empty;
		if (string.IsNullOrEmpty(clientSecWebSocketProtocol))
		{
			if (subProtocol != null)
			{
				throw new WebSocketException(WebSocketError.UnsupportedProtocol, System.SR.Format(System.SR.net_WebSockets_ClientAcceptingNoProtocols, subProtocol));
			}
			return false;
		}
		if (subProtocol == null)
		{
			return true;
		}
		string[] array = clientSecWebSocketProtocol.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		acceptProtocol = subProtocol;
		foreach (string b in array)
		{
			if (string.Equals(acceptProtocol, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		throw new WebSocketException(WebSocketError.UnsupportedProtocol, System.SR.Format(System.SR.net_WebSockets_AcceptUnsupportedProtocol, clientSecWebSocketProtocol, subProtocol));
	}

	internal static void ValidateOptions(string subProtocol, int receiveBufferSize, int sendBufferSize, TimeSpan keepAliveInterval)
	{
		if (subProtocol != null)
		{
			System.Net.WebSockets.WebSocketValidate.ValidateSubprotocol(subProtocol);
		}
		if (receiveBufferSize < 256)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 256));
		}
		if (sendBufferSize < 16)
		{
			throw new ArgumentOutOfRangeException("sendBufferSize", sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 16));
		}
		if (receiveBufferSize > 65536)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooBig, "receiveBufferSize", receiveBufferSize, 65536));
		}
		if (sendBufferSize > 65536)
		{
			throw new ArgumentOutOfRangeException("sendBufferSize", sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooBig, "sendBufferSize", sendBufferSize, 65536));
		}
		if (keepAliveInterval < Timeout.InfiniteTimeSpan)
		{
			throw new ArgumentOutOfRangeException("keepAliveInterval", keepAliveInterval, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, Timeout.InfiniteTimeSpan.ToString()));
		}
	}

	private static void ValidateWebSocketHeaders(HttpListenerContext context)
	{
		if (!WebSocketsSupported)
		{
			throw new PlatformNotSupportedException(System.SR.net_WebSockets_UnsupportedPlatform);
		}
		if (!context.Request.IsWebSocketRequest)
		{
			throw new WebSocketException(WebSocketError.NotAWebSocket, System.SR.Format(System.SR.net_WebSockets_AcceptNotAWebSocket, "ValidateWebSocketHeaders", "Connection", "Upgrade", "websocket", context.Request.Headers["Upgrade"]));
		}
		string text = context.Request.Headers["Sec-WebSocket-Version"];
		if (string.IsNullOrEmpty(text))
		{
			throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_AcceptHeaderNotFound, "ValidateWebSocketHeaders", "Sec-WebSocket-Version"));
		}
		if (!string.Equals(text, SupportedVersion, StringComparison.OrdinalIgnoreCase))
		{
			throw new WebSocketException(WebSocketError.UnsupportedVersion, System.SR.Format(System.SR.net_WebSockets_AcceptUnsupportedWebSocketVersion, "ValidateWebSocketHeaders", text, SupportedVersion));
		}
		string text2 = context.Request.Headers["Sec-WebSocket-Key"];
		bool flag = string.IsNullOrWhiteSpace(text2);
		if (!flag)
		{
			try
			{
				flag = Convert.FromBase64String(text2).Length != 16;
			}
			catch
			{
				flag = true;
			}
		}
		if (flag)
		{
			throw new WebSocketException(WebSocketError.HeaderError, System.SR.Format(System.SR.net_WebSockets_AcceptHeaderNotFound, "ValidateWebSocketHeaders", "Sec-WebSocket-Key"));
		}
	}

	internal static Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(HttpListenerContext context, string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
	{
		ValidateOptions(subProtocol, receiveBufferSize, 16, keepAliveInterval);
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(internalBuffer, "internalBuffer");
		WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, 16, isServerBuffer: true);
		return AcceptWebSocketAsyncCore(context, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
	}

	private static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context, string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval, ArraySegment<byte> internalBuffer)
	{
		try
		{
			HttpListenerResponse response = context.Response;
			HttpListenerRequest request = context.Request;
			ValidateWebSocketHeaders(context);
			string secWebSocketVersion = request.Headers["Sec-WebSocket-Version"];
			string origin = request.Headers["Origin"];
			List<string> secWebSocketProtocols = new List<string>();
			if (ProcessWebSocketProtocolHeader(request.Headers["Sec-WebSocket-Protocol"], subProtocol, out var acceptProtocol))
			{
				secWebSocketProtocols.Add(acceptProtocol);
				response.Headers.Add("Sec-WebSocket-Protocol", acceptProtocol);
			}
			string secWebSocketKey = request.Headers["Sec-WebSocket-Key"];
			string secWebSocketAcceptString = GetSecWebSocketAcceptString(secWebSocketKey);
			response.Headers.Add("Connection", "Upgrade");
			response.Headers.Add("Upgrade", "websocket");
			response.Headers.Add("Sec-WebSocket-Accept", secWebSocketAcceptString);
			response.StatusCode = 101;
			response.ComputeCoreHeaders();
			ulong num = SendWebSocketHeaders(response);
			if (num != 0L)
			{
				throw new WebSocketException((int)num, System.SR.Format(System.SR.net_WebSockets_NativeSendResponseHeaders, "AcceptWebSocketAsync", num));
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Origin", origin), "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Sec-WebSocket-Version", secWebSocketVersion), "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Sec-WebSocket-Key", secWebSocketKey), "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Sec-WebSocket-Accept", secWebSocketAcceptString), "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Sec-WebSocket-Protocol", request.Headers["Sec-WebSocket-Protocol"]), "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Info(null, FormattableStringFactory.Create("{0} = {1}", "Sec-WebSocket-Protocol", acceptProtocol), "AcceptWebSocketAsyncCore");
			}
			await response.OutputStream.FlushAsync().SuppressContextFlow();
			HttpResponseStream outputStream = response.OutputStream as HttpResponseStream;
			((HttpResponseStream)response.OutputStream).SwitchToOpaqueMode();
			HttpRequestStream httpRequestStream = new HttpRequestStream(context);
			httpRequestStream.SwitchToOpaqueMode();
			WebSocketHttpListenerDuplexStream innerStream = new WebSocketHttpListenerDuplexStream(httpRequestStream, outputStream, context);
			WebSocket webSocket = ServerWebSocket.Create(innerStream, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
			HttpListenerWebSocketContext httpListenerWebSocketContext = new HttpListenerWebSocketContext(request.Url, request.Headers, request.Cookies, context.User, request.IsAuthenticated, request.IsLocal, request.IsSecureConnection, origin, secWebSocketProtocols.AsReadOnly(), secWebSocketVersion, secWebSocketKey, webSocket);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Associate(context, httpListenerWebSocketContext, "AcceptWebSocketAsyncCore");
				System.Net.NetEventSource.Associate(httpListenerWebSocketContext, webSocket, "AcceptWebSocketAsyncCore");
			}
			return httpListenerWebSocketContext;
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(context, message, "AcceptWebSocketAsyncCore");
			}
			throw;
		}
	}

	internal static ConfiguredTaskAwaitable SuppressContextFlow(this Task task)
	{
		return task.ConfigureAwait(continueOnCapturedContext: false);
	}

	internal static ConfiguredTaskAwaitable<T> SuppressContextFlow<T>(this Task<T> task)
	{
		return task.ConfigureAwait(continueOnCapturedContext: false);
	}

	private unsafe static ulong SendWebSocketHeaders(HttpListenerResponse response)
	{
		return response.SendHeaders(null, null, global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA | global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA | global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE, isWebSocketHandshake: true);
	}

	internal static void ValidateInnerStream(Stream innerStream)
	{
		if (innerStream == null)
		{
			throw new ArgumentNullException("innerStream");
		}
		if (!innerStream.CanRead)
		{
			throw new ArgumentException(System.SR.net_writeonlystream, "innerStream");
		}
		if (!innerStream.CanWrite)
		{
			throw new ArgumentException(System.SR.net_readonlystream, "innerStream");
		}
	}

	internal static void ThrowIfConnectionAborted(Stream connection, bool read)
	{
		if ((!read && !connection.CanWrite) || (read && !connection.CanRead))
		{
			throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
		}
	}

	[DoesNotReturn]
	internal static void ThrowPlatformNotSupportedException_WSPC()
	{
		throw new PlatformNotSupportedException(System.SR.net_WebSockets_UnsupportedPlatform);
	}
}
