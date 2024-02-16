using System.Runtime.InteropServices;

namespace System.Net.WebSockets;

internal static class WebSocketProtocolComponent
{
	internal enum Action
	{
		NoAction,
		SendToNetwork,
		IndicateSendComplete,
		ReceiveFromNetwork,
		IndicateReceiveComplete
	}

	internal enum BufferType : uint
	{
		None = 0u,
		UTF8Message = 2147483648u,
		UTF8Fragment = 2147483649u,
		BinaryMessage = 2147483650u,
		BinaryFragment = 2147483651u,
		Close = 2147483652u,
		PingPong = 2147483653u,
		UnsolicitedPong = 2147483654u
	}

	internal enum PropertyType
	{
		ReceiveBufferSize,
		SendBufferSize,
		DisableMasking,
		AllocatedBuffer,
		DisableUtf8Verification,
		KeepAliveInterval
	}

	internal enum ActionQueue
	{
		Send = 1,
		Receive
	}

	private static readonly string s_dummyWebsocketKeyBase64;

	private static readonly IntPtr s_webSocketDllHandle;

	private static readonly string s_supportedVersion;

	private static readonly global::Interop.WebSocket.HttpHeader[] s_initialClientRequestHeaders;

	private static readonly global::Interop.WebSocket.HttpHeader[] s_ServerFakeRequestHeaders;

	internal static string SupportedVersion
	{
		get
		{
			if (!IsSupported)
			{
				HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
			}
			return s_supportedVersion;
		}
	}

	internal static bool IsSupported => s_webSocketDllHandle != IntPtr.Zero;

	static WebSocketProtocolComponent()
	{
		s_dummyWebsocketKeyBase64 = Convert.ToBase64String(new byte[16]);
		s_initialClientRequestHeaders = new global::Interop.WebSocket.HttpHeader[2]
		{
			new global::Interop.WebSocket.HttpHeader
			{
				Name = "Connection",
				NameLength = (uint)"Connection".Length,
				Value = "Upgrade",
				ValueLength = (uint)"Upgrade".Length
			},
			new global::Interop.WebSocket.HttpHeader
			{
				Name = "Upgrade",
				NameLength = (uint)"Upgrade".Length,
				Value = "websocket",
				ValueLength = (uint)"websocket".Length
			}
		};
		s_webSocketDllHandle = global::Interop.Kernel32.LoadLibraryEx("websocket.dll", IntPtr.Zero, 0);
		if (!(s_webSocketDllHandle == IntPtr.Zero))
		{
			s_supportedVersion = GetSupportedVersion();
			s_ServerFakeRequestHeaders = new global::Interop.WebSocket.HttpHeader[5]
			{
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Connection",
					NameLength = (uint)"Connection".Length,
					Value = "Upgrade",
					ValueLength = (uint)"Upgrade".Length
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Upgrade",
					NameLength = (uint)"Upgrade".Length,
					Value = "websocket",
					ValueLength = (uint)"websocket".Length
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Host",
					NameLength = (uint)"Host".Length,
					Value = string.Empty,
					ValueLength = 0u
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Sec-WebSocket-Version",
					NameLength = (uint)"Sec-WebSocket-Version".Length,
					Value = s_supportedVersion,
					ValueLength = (uint)s_supportedVersion.Length
				},
				new global::Interop.WebSocket.HttpHeader
				{
					Name = "Sec-WebSocket-Key",
					NameLength = (uint)"Sec-WebSocket-Key".Length,
					Value = s_dummyWebsocketKeyBase64,
					ValueLength = (uint)s_dummyWebsocketKeyBase64.Length
				}
			};
		}
	}

	internal static string GetSupportedVersion()
	{
		if (!IsSupported)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		SafeWebSocketHandle webSocketHandle = null;
		try
		{
			int errorCode = global::Interop.WebSocket.WebSocketCreateClientHandle(null, 0u, out webSocketHandle);
			ThrowOnError(errorCode);
			if (webSocketHandle == null || webSocketHandle.IsInvalid)
			{
				HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
			}
			errorCode = global::Interop.WebSocket.WebSocketBeginClientHandshake(webSocketHandle, IntPtr.Zero, 0u, IntPtr.Zero, 0u, s_initialClientRequestHeaders, (uint)s_initialClientRequestHeaders.Length, out var additionalHeadersPtr, out var additionalHeaderCount);
			ThrowOnError(errorCode);
			global::Interop.WebSocket.HttpHeader[] array = MarshalHttpHeaders(additionalHeadersPtr, (int)additionalHeaderCount);
			string result = null;
			global::Interop.WebSocket.HttpHeader[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				global::Interop.WebSocket.HttpHeader httpHeader = array2[i];
				if (string.Equals(httpHeader.Name, "Sec-WebSocket-Version", StringComparison.OrdinalIgnoreCase))
				{
					result = httpHeader.Value;
					break;
				}
			}
			return result;
		}
		finally
		{
			webSocketHandle?.Dispose();
		}
	}

	internal static void WebSocketCreateServerHandle(global::Interop.WebSocket.Property[] properties, int propertyCount, out SafeWebSocketHandle webSocketHandle)
	{
		if (!IsSupported)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		int errorCode = global::Interop.WebSocket.WebSocketCreateServerHandle(properties, (uint)propertyCount, out webSocketHandle);
		ThrowOnError(errorCode);
		if (webSocketHandle == null || webSocketHandle.IsInvalid)
		{
			HttpWebSocket.ThrowPlatformNotSupportedException_WSPC();
		}
		errorCode = global::Interop.WebSocket.WebSocketBeginServerHandshake(webSocketHandle, IntPtr.Zero, IntPtr.Zero, 0u, s_ServerFakeRequestHeaders, (uint)s_ServerFakeRequestHeaders.Length, out var _, out var _);
		ThrowOnError(errorCode);
		errorCode = global::Interop.WebSocket.WebSocketEndServerHandshake(webSocketHandle);
		ThrowOnError(errorCode);
	}

	internal static void WebSocketAbortHandle(SafeHandle webSocketHandle)
	{
		global::Interop.WebSocket.WebSocketAbortHandle(webSocketHandle);
		DrainActionQueue(webSocketHandle, ActionQueue.Send);
		DrainActionQueue(webSocketHandle, ActionQueue.Receive);
	}

	internal static void WebSocketDeleteHandle(IntPtr webSocketPtr)
	{
		global::Interop.WebSocket.WebSocketDeleteHandle(webSocketPtr);
	}

	internal static void WebSocketSend(WebSocketBase webSocket, BufferType bufferType, global::Interop.WebSocket.Buffer buffer)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketSend_Raw(webSocket.SessionHandle, bufferType, ref buffer, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketSendWithoutBody(WebSocketBase webSocket, BufferType bufferType)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketSendWithoutBody_Raw(webSocket.SessionHandle, bufferType, IntPtr.Zero, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketReceive(WebSocketBase webSocket)
	{
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketReceive(webSocket.SessionHandle, IntPtr.Zero, IntPtr.Zero);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
	}

	internal static void WebSocketGetAction(WebSocketBase webSocket, ActionQueue actionQueue, global::Interop.WebSocket.Buffer[] dataBuffers, ref uint dataBufferCount, out Action action, out BufferType bufferType, out IntPtr actionContext)
	{
		action = Action.NoAction;
		bufferType = BufferType.None;
		actionContext = IntPtr.Zero;
		ThrowIfSessionHandleClosed(webSocket);
		int errorCode;
		try
		{
			errorCode = global::Interop.WebSocket.WebSocketGetAction(webSocket.SessionHandle, actionQueue, dataBuffers, ref dataBufferCount, out action, out bufferType, out var _, out actionContext);
		}
		catch (ObjectDisposedException innerException)
		{
			throw ConvertObjectDisposedException(webSocket, innerException);
		}
		ThrowOnError(errorCode);
		webSocket.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
	}

	internal static void WebSocketCompleteAction(WebSocketBase webSocket, IntPtr actionContext, int bytesTransferred)
	{
		if (webSocket.SessionHandle.IsClosed)
		{
			return;
		}
		try
		{
			global::Interop.WebSocket.WebSocketCompleteAction(webSocket.SessionHandle, actionContext, (uint)bytesTransferred);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static void DrainActionQueue(SafeHandle webSocketHandle, ActionQueue actionQueue)
	{
		while (true)
		{
			global::Interop.WebSocket.Buffer[] dataBuffers = new global::Interop.WebSocket.Buffer[1];
			uint dataBufferCount = 1u;
			Action action;
			BufferType bufferType;
			IntPtr applicationContext;
			IntPtr actionContext;
			int hr = global::Interop.WebSocket.WebSocketGetAction(webSocketHandle, actionQueue, dataBuffers, ref dataBufferCount, out action, out bufferType, out applicationContext, out actionContext);
			if (!Succeeded(hr) || action == Action.NoAction)
			{
				break;
			}
			global::Interop.WebSocket.WebSocketCompleteAction(webSocketHandle, actionContext, 0u);
		}
	}

	private static void MarshalAndVerifyHttpHeader(IntPtr httpHeaderPtr, ref global::Interop.WebSocket.HttpHeader httpHeader)
	{
		IntPtr intPtr = Marshal.ReadIntPtr(httpHeaderPtr);
		IntPtr ptr = IntPtr.Add(httpHeaderPtr, IntPtr.Size);
		int num = Marshal.ReadInt32(ptr);
		if (intPtr != IntPtr.Zero)
		{
			httpHeader.Name = Marshal.PtrToStringAnsi(intPtr, num);
		}
		if ((httpHeader.Name == null && num != 0) || (httpHeader.Name != null && num != httpHeader.Name.Length))
		{
			throw new AccessViolationException();
		}
		int offset = 2 * IntPtr.Size;
		int offset2 = 3 * IntPtr.Size;
		IntPtr ptr2 = Marshal.ReadIntPtr(IntPtr.Add(httpHeaderPtr, offset));
		ptr = IntPtr.Add(httpHeaderPtr, offset2);
		num = Marshal.ReadInt32(ptr);
		httpHeader.Value = Marshal.PtrToStringAnsi(ptr2, num);
		if ((httpHeader.Value == null && num != 0) || (httpHeader.Value != null && num != httpHeader.Value.Length))
		{
			throw new AccessViolationException();
		}
	}

	private static global::Interop.WebSocket.HttpHeader[] MarshalHttpHeaders(IntPtr nativeHeadersPtr, int nativeHeaderCount)
	{
		global::Interop.WebSocket.HttpHeader[] array = new global::Interop.WebSocket.HttpHeader[nativeHeaderCount];
		int num = 4 * IntPtr.Size;
		for (int i = 0; i < nativeHeaderCount; i++)
		{
			int offset = num * i;
			IntPtr httpHeaderPtr = IntPtr.Add(nativeHeadersPtr, offset);
			MarshalAndVerifyHttpHeader(httpHeaderPtr, ref array[i]);
		}
		return array;
	}

	public static bool Succeeded(int hr)
	{
		return hr >= 0;
	}

	private static void ThrowOnError(int errorCode)
	{
		if (Succeeded(errorCode))
		{
			return;
		}
		throw new WebSocketException(errorCode);
	}

	private static void ThrowIfSessionHandleClosed(WebSocketBase webSocket)
	{
		if (webSocket.SessionHandle.IsClosed)
		{
			throw new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State));
		}
	}

	private static WebSocketException ConvertObjectDisposedException(WebSocketBase webSocket, ObjectDisposedException innerException)
	{
		return new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State), innerException);
	}
}
