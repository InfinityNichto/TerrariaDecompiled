using System.Runtime.CompilerServices;

namespace System.Net.WebSockets;

[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public enum WebSocketError
{
	Success,
	InvalidMessageType,
	Faulted,
	NativeError,
	NotAWebSocket,
	UnsupportedVersion,
	UnsupportedProtocol,
	HeaderError,
	ConnectionClosedPrematurely,
	InvalidState
}
