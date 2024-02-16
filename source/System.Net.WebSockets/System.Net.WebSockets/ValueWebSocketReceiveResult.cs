namespace System.Net.WebSockets;

public readonly struct ValueWebSocketReceiveResult
{
	private readonly uint _countAndEndOfMessage;

	private readonly WebSocketMessageType _messageType;

	public int Count => (int)(_countAndEndOfMessage & 0x7FFFFFFF);

	public bool EndOfMessage => (_countAndEndOfMessage & 0x80000000u) == 2147483648u;

	public WebSocketMessageType MessageType => _messageType;

	public ValueWebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
	{
		if (count < 0)
		{
			ThrowCountOutOfRange();
		}
		if ((uint)messageType > 2u)
		{
			ThrowMessageTypeOutOfRange();
		}
		_countAndEndOfMessage = (uint)count | (endOfMessage ? 2147483648u : 0u);
		_messageType = messageType;
	}

	private static void ThrowCountOutOfRange()
	{
		throw new ArgumentOutOfRangeException("count");
	}

	private static void ThrowMessageTypeOutOfRange()
	{
		throw new ArgumentOutOfRangeException("messageType");
	}
}
