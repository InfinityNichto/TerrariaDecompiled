using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.WebSockets;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class WebSocketException : Win32Exception
{
	private readonly WebSocketError _webSocketErrorCode;

	public override int ErrorCode => base.NativeErrorCode;

	public WebSocketError WebSocketErrorCode => _webSocketErrorCode;

	public WebSocketException()
		: this(Marshal.GetLastPInvokeError())
	{
	}

	public WebSocketException(WebSocketError error)
		: this(error, GetErrorMessage(error))
	{
	}

	public WebSocketException(WebSocketError error, string? message)
		: base(message)
	{
		_webSocketErrorCode = error;
	}

	public WebSocketException(WebSocketError error, Exception? innerException)
		: this(error, GetErrorMessage(error), innerException)
	{
	}

	public WebSocketException(WebSocketError error, string? message, Exception? innerException)
		: base(message, innerException)
	{
		_webSocketErrorCode = error;
	}

	public WebSocketException(int nativeError)
		: base(nativeError)
	{
		_webSocketErrorCode = ((!Succeeded(nativeError)) ? WebSocketError.NativeError : WebSocketError.Success);
		SetErrorCodeOnError(nativeError);
	}

	public WebSocketException(int nativeError, string? message)
		: base(nativeError, message)
	{
		_webSocketErrorCode = ((!Succeeded(nativeError)) ? WebSocketError.NativeError : WebSocketError.Success);
		SetErrorCodeOnError(nativeError);
	}

	public WebSocketException(int nativeError, Exception? innerException)
		: base(System.SR.net_WebSockets_Generic, innerException)
	{
		_webSocketErrorCode = ((!Succeeded(nativeError)) ? WebSocketError.NativeError : WebSocketError.Success);
		SetErrorCodeOnError(nativeError);
	}

	public WebSocketException(WebSocketError error, int nativeError)
		: this(error, nativeError, GetErrorMessage(error))
	{
	}

	public WebSocketException(WebSocketError error, int nativeError, string? message)
		: base(message)
	{
		_webSocketErrorCode = error;
		SetErrorCodeOnError(nativeError);
	}

	public WebSocketException(WebSocketError error, int nativeError, Exception? innerException)
		: this(error, nativeError, GetErrorMessage(error), innerException)
	{
	}

	public WebSocketException(WebSocketError error, int nativeError, string? message, Exception? innerException)
		: base(message, innerException)
	{
		_webSocketErrorCode = error;
		SetErrorCodeOnError(nativeError);
	}

	public WebSocketException(string? message)
		: base(message)
	{
	}

	public WebSocketException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	private WebSocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("WebSocketErrorCode", _webSocketErrorCode);
	}

	private static string GetErrorMessage(WebSocketError error)
	{
		return error switch
		{
			WebSocketError.InvalidMessageType => System.SR.Format(System.SR.net_WebSockets_InvalidMessageType_Generic, "WebSocket.CloseAsync", "WebSocket.CloseOutputAsync"), 
			WebSocketError.Faulted => System.SR.net_Websockets_WebSocketBaseFaulted, 
			WebSocketError.NotAWebSocket => System.SR.net_WebSockets_NotAWebSocket_Generic, 
			WebSocketError.UnsupportedVersion => System.SR.net_WebSockets_UnsupportedWebSocketVersion_Generic, 
			WebSocketError.UnsupportedProtocol => System.SR.net_WebSockets_UnsupportedProtocol_Generic, 
			WebSocketError.HeaderError => System.SR.net_WebSockets_HeaderError_Generic, 
			WebSocketError.ConnectionClosedPrematurely => System.SR.net_WebSockets_ConnectionClosedPrematurely_Generic, 
			WebSocketError.InvalidState => System.SR.net_WebSockets_InvalidState_Generic, 
			_ => System.SR.net_WebSockets_Generic, 
		};
	}

	private void SetErrorCodeOnError(int nativeError)
	{
		if (!Succeeded(nativeError))
		{
			base.HResult = nativeError;
		}
	}

	private static bool Succeeded(int hr)
	{
		return hr >= 0;
	}
}
