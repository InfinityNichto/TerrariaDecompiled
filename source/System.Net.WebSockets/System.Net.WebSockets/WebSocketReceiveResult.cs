namespace System.Net.WebSockets;

public class WebSocketReceiveResult
{
	public int Count { get; }

	public bool EndOfMessage { get; }

	public WebSocketMessageType MessageType { get; }

	public WebSocketCloseStatus? CloseStatus { get; }

	public string? CloseStatusDescription { get; }

	public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
		: this(count, messageType, endOfMessage, null, null)
	{
	}

	public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Count = count;
		EndOfMessage = endOfMessage;
		MessageType = messageType;
		CloseStatus = closeStatus;
		CloseStatusDescription = closeStatusDescription;
	}
}
