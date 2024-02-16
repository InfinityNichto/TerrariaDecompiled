using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

public abstract class WebSocket : IDisposable
{
	public abstract WebSocketCloseStatus? CloseStatus { get; }

	public abstract string? CloseStatusDescription { get; }

	public abstract string? SubProtocol { get; }

	public abstract WebSocketState State { get; }

	public static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(30.0);

	public abstract void Abort();

	public abstract Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);

	public abstract Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);

	public abstract void Dispose();

	public abstract Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

	public abstract Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);

	public virtual async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment))
		{
			WebSocketReceiveResult webSocketReceiveResult = await ReceiveAsync(segment, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new ValueWebSocketReceiveResult(webSocketReceiveResult.Count, webSocketReceiveResult.MessageType, webSocketReceiveResult.EndOfMessage);
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			WebSocketReceiveResult webSocketReceiveResult2 = await ReceiveAsync(new ArraySegment<byte>(array, 0, buffer.Length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			new Span<byte>(array, 0, webSocketReceiveResult2.Count).CopyTo(buffer.Span);
			return new ValueWebSocketReceiveResult(webSocketReceiveResult2.Count, webSocketReceiveResult2.MessageType, webSocketReceiveResult2.EndOfMessage);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		if (!MemoryMarshal.TryGetArray(buffer, out var segment))
		{
			return SendWithArrayPoolAsync(buffer, messageType, endOfMessage, cancellationToken);
		}
		return new ValueTask(SendAsync(segment, messageType, endOfMessage, cancellationToken));
	}

	public virtual ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, WebSocketMessageFlags messageFlags, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(buffer, messageType, messageFlags.HasFlag(WebSocketMessageFlags.EndOfMessage), cancellationToken);
	}

	private async ValueTask SendWithArrayPoolAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			buffer.Span.CopyTo(array);
			await SendAsync(new ArraySegment<byte>(array, 0, buffer.Length), messageType, endOfMessage, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	protected static void ThrowOnInvalidState(WebSocketState state, params WebSocketState[] validStates)
	{
		string p = string.Empty;
		if (validStates != null && validStates.Length != 0)
		{
			foreach (WebSocketState webSocketState in validStates)
			{
				if (state == webSocketState)
				{
					return;
				}
			}
			p = string.Join(", ", validStates);
		}
		throw new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState, state, p));
	}

	protected static bool IsStateTerminal(WebSocketState state)
	{
		if (state != WebSocketState.Closed)
		{
			return state == WebSocketState.Aborted;
		}
		return true;
	}

	public static ArraySegment<byte> CreateClientBuffer(int receiveBufferSize, int sendBufferSize)
	{
		if (receiveBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		if (sendBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("sendBufferSize", sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		return new ArraySegment<byte>(new byte[Math.Max(receiveBufferSize, sendBufferSize)]);
	}

	public static ArraySegment<byte> CreateServerBuffer(int receiveBufferSize)
	{
		if (receiveBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		return new ArraySegment<byte>(new byte[receiveBufferSize]);
	}

	public static WebSocket CreateFromStream(Stream stream, bool isServer, string? subProtocol, TimeSpan keepAliveInterval)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanRead || !stream.CanWrite)
		{
			throw new ArgumentException((!stream.CanRead) ? System.SR.NotReadableStream : System.SR.NotWriteableStream, "stream");
		}
		if (subProtocol != null)
		{
			WebSocketValidate.ValidateSubprotocol(subProtocol);
		}
		if (keepAliveInterval != Timeout.InfiniteTimeSpan && keepAliveInterval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("keepAliveInterval", keepAliveInterval, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 0));
		}
		return new ManagedWebSocket(stream, isServer, subProtocol, keepAliveInterval);
	}

	public static WebSocket CreateFromStream(Stream stream, WebSocketCreationOptions options)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if (!stream.CanRead || !stream.CanWrite)
		{
			throw new ArgumentException((!stream.CanRead) ? System.SR.NotReadableStream : System.SR.NotWriteableStream, "stream");
		}
		return new ManagedWebSocket(stream, options);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.")]
	public static bool IsApplicationTargeting45()
	{
		return true;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.")]
	public static void RegisterPrefixes()
	{
		throw new PlatformNotSupportedException();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static WebSocket CreateClientWebSocket(Stream innerStream, string? subProtocol, int receiveBufferSize, int sendBufferSize, TimeSpan keepAliveInterval, bool useZeroMaskingKey, ArraySegment<byte> internalBuffer)
	{
		if (innerStream == null)
		{
			throw new ArgumentNullException("innerStream");
		}
		if (!innerStream.CanRead || !innerStream.CanWrite)
		{
			throw new ArgumentException((!innerStream.CanRead) ? System.SR.NotReadableStream : System.SR.NotWriteableStream, "innerStream");
		}
		if (subProtocol != null)
		{
			WebSocketValidate.ValidateSubprotocol(subProtocol);
		}
		if (keepAliveInterval != Timeout.InfiniteTimeSpan && keepAliveInterval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("keepAliveInterval", keepAliveInterval, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 0));
		}
		if (receiveBufferSize <= 0 || sendBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException((receiveBufferSize <= 0) ? "receiveBufferSize" : "sendBufferSize", (receiveBufferSize <= 0) ? receiveBufferSize : sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 0));
		}
		return new ManagedWebSocket(innerStream, isServer: false, subProtocol, keepAliveInterval);
	}
}
