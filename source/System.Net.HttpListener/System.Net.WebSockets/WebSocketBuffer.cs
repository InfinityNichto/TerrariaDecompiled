using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Net.WebSockets;

internal sealed class WebSocketBuffer : IDisposable
{
	private sealed class PayloadReceiveResult
	{
		public int Count { get; set; }

		public bool EndOfMessage { get; }

		public WebSocketMessageType MessageType { get; }

		public PayloadReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			Count = count;
			EndOfMessage = endOfMessage;
			MessageType = messageType;
		}
	}

	private static readonly int s_PropertyBufferSize = 12 + IntPtr.Size;

	private readonly int _receiveBufferSize;

	private readonly long _startAddress;

	private readonly long _endAddress;

	private GCHandle _gcHandle;

	private readonly ArraySegment<byte> _internalBuffer;

	private readonly ArraySegment<byte> _nativeBuffer;

	private readonly ArraySegment<byte> _payloadBuffer;

	private readonly ArraySegment<byte> _propertyBuffer;

	private readonly int _sendBufferSize;

	private volatile int _payloadOffset;

	private volatile PayloadReceiveResult _bufferedPayloadReceiveResult;

	private long _pinnedSendBufferStartAddress;

	private long _pinnedSendBufferEndAddress;

	private ArraySegment<byte> _pinnedSendBuffer;

	private GCHandle _pinnedSendBufferHandle;

	private int _stateWhenDisposing = int.MinValue;

	private int _sendBufferState;

	public int ReceiveBufferSize => _receiveBufferSize;

	public int SendBufferSize => _sendBufferSize;

	private WebSocketBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize, int sendBufferSize)
	{
		_receiveBufferSize = receiveBufferSize;
		_sendBufferSize = sendBufferSize;
		_internalBuffer = internalBuffer;
		_gcHandle = GCHandle.Alloc(internalBuffer.Array, GCHandleType.Pinned);
		int num = _receiveBufferSize + _sendBufferSize + 144;
		_startAddress = Marshal.UnsafeAddrOfPinnedArrayElement(internalBuffer.Array, internalBuffer.Offset).ToInt64();
		_endAddress = _startAddress + num;
		_nativeBuffer = new ArraySegment<byte>(internalBuffer.Array, internalBuffer.Offset, num);
		_payloadBuffer = new ArraySegment<byte>(internalBuffer.Array, _nativeBuffer.Offset + _nativeBuffer.Count, _receiveBufferSize);
		_propertyBuffer = new ArraySegment<byte>(internalBuffer.Array, _payloadBuffer.Offset + _payloadBuffer.Count, s_PropertyBufferSize);
		_sendBufferState = 0;
	}

	internal static WebSocketBuffer CreateServerBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize)
	{
		int nativeSendBufferSize = GetNativeSendBufferSize(16, isServerBuffer: true);
		return new WebSocketBuffer(internalBuffer, receiveBufferSize, nativeSendBufferSize);
	}

	public void Dispose(WebSocketState webSocketState)
	{
		if (Interlocked.CompareExchange(ref _stateWhenDisposing, (int)webSocketState, int.MinValue) == int.MinValue)
		{
			CleanUp();
		}
	}

	public void Dispose()
	{
		Dispose(WebSocketState.None);
	}

	internal global::Interop.WebSocket.Property[] CreateProperties(bool useZeroMaskingKey)
	{
		ThrowIfDisposed();
		IntPtr intPtr = _gcHandle.AddrOfPinnedObject();
		int offset = _propertyBuffer.Offset;
		Marshal.WriteInt32(intPtr, offset, _receiveBufferSize);
		offset += 4;
		Marshal.WriteInt32(intPtr, offset, _sendBufferSize);
		offset += 4;
		Marshal.WriteIntPtr(intPtr, offset, intPtr + _internalBuffer.Offset);
		offset += IntPtr.Size;
		Marshal.WriteInt32(intPtr, offset, useZeroMaskingKey ? 1 : 0);
		int num = (useZeroMaskingKey ? 4 : 3);
		global::Interop.WebSocket.Property[] array = new global::Interop.WebSocket.Property[num];
		offset = _propertyBuffer.Offset;
		array[0] = new global::Interop.WebSocket.Property
		{
			Type = WebSocketProtocolComponent.PropertyType.ReceiveBufferSize,
			PropertySize = 4u,
			PropertyData = IntPtr.Add(intPtr, offset)
		};
		offset += 4;
		array[1] = new global::Interop.WebSocket.Property
		{
			Type = WebSocketProtocolComponent.PropertyType.SendBufferSize,
			PropertySize = 4u,
			PropertyData = IntPtr.Add(intPtr, offset)
		};
		offset += 4;
		array[2] = new global::Interop.WebSocket.Property
		{
			Type = WebSocketProtocolComponent.PropertyType.AllocatedBuffer,
			PropertySize = (uint)_nativeBuffer.Count,
			PropertyData = IntPtr.Add(intPtr, offset)
		};
		offset += IntPtr.Size;
		if (useZeroMaskingKey)
		{
			array[3] = new global::Interop.WebSocket.Property
			{
				Type = WebSocketProtocolComponent.PropertyType.DisableMasking,
				PropertySize = 4u,
				PropertyData = IntPtr.Add(intPtr, offset)
			};
		}
		return array;
	}

	internal void PinSendBuffer(ArraySegment<byte> payload, out bool bufferHasBeenPinned)
	{
		bufferHasBeenPinned = false;
		System.Net.WebSockets.WebSocketValidate.ValidateBuffer(payload.Array, payload.Offset, payload.Count);
		if (Interlocked.Exchange(ref _sendBufferState, 1) != 0)
		{
			throw new AccessViolationException();
		}
		_pinnedSendBuffer = payload;
		_pinnedSendBufferHandle = GCHandle.Alloc(_pinnedSendBuffer.Array, GCHandleType.Pinned);
		bufferHasBeenPinned = true;
		_pinnedSendBufferStartAddress = Marshal.UnsafeAddrOfPinnedArrayElement(_pinnedSendBuffer.Array, _pinnedSendBuffer.Offset).ToInt64();
		_pinnedSendBufferEndAddress = _pinnedSendBufferStartAddress + _pinnedSendBuffer.Count;
	}

	internal IntPtr ConvertPinnedSendPayloadToNative(ArraySegment<byte> payload)
	{
		return ConvertPinnedSendPayloadToNative(payload.Array, payload.Offset, payload.Count);
	}

	internal IntPtr ConvertPinnedSendPayloadToNative(byte[] buffer, int offset, int count)
	{
		if (!IsPinnedSendPayloadBuffer(buffer, offset, count))
		{
			throw new AccessViolationException();
		}
		return new IntPtr(_pinnedSendBufferStartAddress + offset - _pinnedSendBuffer.Offset);
	}

	internal ArraySegment<byte> ConvertPinnedSendPayloadFromNative(global::Interop.WebSocket.Buffer buffer, WebSocketProtocolComponent.BufferType bufferType)
	{
		if (!IsPinnedSendPayloadBuffer(buffer, bufferType))
		{
			throw new AccessViolationException();
		}
		UnwrapWebSocketBuffer(buffer, bufferType, out var bufferData, out var bufferLength);
		int num = (int)(bufferData.ToInt64() - _pinnedSendBufferStartAddress);
		return new ArraySegment<byte>(_pinnedSendBuffer.Array, _pinnedSendBuffer.Offset + num, (int)bufferLength);
	}

	private bool IsPinnedSendPayloadBuffer(byte[] buffer, int offset, int count)
	{
		if (_sendBufferState != 1)
		{
			return false;
		}
		if (buffer == _pinnedSendBuffer.Array && offset >= _pinnedSendBuffer.Offset)
		{
			return offset + count <= _pinnedSendBuffer.Offset + _pinnedSendBuffer.Count;
		}
		return false;
	}

	internal bool IsPinnedSendPayloadBuffer(global::Interop.WebSocket.Buffer buffer, WebSocketProtocolComponent.BufferType bufferType)
	{
		if (_sendBufferState != 1)
		{
			return false;
		}
		UnwrapWebSocketBuffer(buffer, bufferType, out var bufferData, out var bufferLength);
		long num = bufferData.ToInt64();
		long num2 = num + bufferLength;
		if (num >= _pinnedSendBufferStartAddress && num2 >= _pinnedSendBufferStartAddress && num <= _pinnedSendBufferEndAddress)
		{
			return num2 <= _pinnedSendBufferEndAddress;
		}
		return false;
	}

	internal void ReleasePinnedSendBuffer()
	{
		int num = Interlocked.Exchange(ref _sendBufferState, 0);
		if (num == 1)
		{
			if (_pinnedSendBufferHandle.IsAllocated)
			{
				_pinnedSendBufferHandle.Free();
			}
			_pinnedSendBuffer = ArraySegment<byte>.Empty;
		}
	}

	internal void BufferPayload(ArraySegment<byte> payload, int unconsumedDataOffset, WebSocketMessageType messageType, bool endOfMessage)
	{
		ThrowIfDisposed();
		int count = payload.Count - unconsumedDataOffset;
		Buffer.BlockCopy(payload.Array, payload.Offset + unconsumedDataOffset, _payloadBuffer.Array, _payloadBuffer.Offset, count);
		_bufferedPayloadReceiveResult = new PayloadReceiveResult(count, messageType, endOfMessage);
	}

	internal bool ReceiveFromBufferedPayload(ArraySegment<byte> buffer, out WebSocketReceiveResult receiveResult)
	{
		ThrowIfDisposed();
		int num = Math.Min(buffer.Count, _bufferedPayloadReceiveResult.Count);
		_bufferedPayloadReceiveResult.Count -= num;
		receiveResult = new WebSocketReceiveResult(num, _bufferedPayloadReceiveResult.MessageType, _bufferedPayloadReceiveResult.Count == 0 && _bufferedPayloadReceiveResult.EndOfMessage);
		Buffer.BlockCopy(_payloadBuffer.Array, _payloadBuffer.Offset + _payloadOffset, buffer.Array, buffer.Offset, num);
		if (_bufferedPayloadReceiveResult.Count == 0)
		{
			_payloadOffset = 0;
			_bufferedPayloadReceiveResult = null;
			return false;
		}
		_payloadOffset += num;
		return true;
	}

	internal ArraySegment<byte> ConvertNativeBuffer(WebSocketProtocolComponent.Action action, global::Interop.WebSocket.Buffer buffer, WebSocketProtocolComponent.BufferType bufferType)
	{
		ThrowIfDisposed();
		UnwrapWebSocketBuffer(buffer, bufferType, out var bufferData, out var bufferLength);
		if (bufferData == IntPtr.Zero)
		{
			return ArraySegment<byte>.Empty;
		}
		if (IsNativeBuffer(bufferData, bufferLength))
		{
			return new ArraySegment<byte>(_internalBuffer.Array, GetOffset(bufferData), (int)bufferLength);
		}
		throw new AccessViolationException();
	}

	internal void ConvertCloseBuffer(WebSocketProtocolComponent.Action action, global::Interop.WebSocket.Buffer buffer, out WebSocketCloseStatus closeStatus, out string reason)
	{
		ThrowIfDisposed();
		closeStatus = (WebSocketCloseStatus)buffer.CloseStatus.CloseStatus;
		UnwrapWebSocketBuffer(buffer, WebSocketProtocolComponent.BufferType.Close, out var bufferData, out var bufferLength);
		if (bufferData == IntPtr.Zero)
		{
			reason = null;
			return;
		}
		if (IsNativeBuffer(bufferData, bufferLength))
		{
			ArraySegment<byte> arraySegment = new ArraySegment<byte>(_internalBuffer.Array, GetOffset(bufferData), (int)bufferLength);
			reason = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
			return;
		}
		throw new AccessViolationException();
	}

	internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action, WebSocketProtocolComponent.BufferType bufferType, global::Interop.WebSocket.Buffer[] dataBuffers, uint dataBufferCount)
	{
		ThrowIfDisposed();
		if (dataBufferCount > dataBuffers.Length)
		{
			throw new AccessViolationException();
		}
		int num = dataBuffers.Length;
		bool flag = action == WebSocketProtocolComponent.Action.IndicateSendComplete || action == WebSocketProtocolComponent.Action.SendToNetwork;
		if (flag)
		{
			num = (int)dataBufferCount;
		}
		bool flag2 = false;
		for (int i = 0; i < num; i++)
		{
			global::Interop.WebSocket.Buffer buffer = dataBuffers[i];
			UnwrapWebSocketBuffer(buffer, bufferType, out var bufferData, out var bufferLength);
			if (!(bufferData == IntPtr.Zero))
			{
				flag2 = true;
				bool flag3 = IsPinnedSendPayloadBuffer(buffer, bufferType);
				if (bufferLength > GetMaxBufferSize() && (!flag || !flag3))
				{
					throw new AccessViolationException();
				}
				if (!flag3 && !IsNativeBuffer(bufferData, bufferLength))
				{
					throw new AccessViolationException();
				}
			}
		}
		if (!flag2 && action != 0 && action != WebSocketProtocolComponent.Action.IndicateReceiveComplete)
		{
			_ = 2;
		}
	}

	private static int GetNativeSendBufferSize(int sendBufferSize, bool isServerBuffer)
	{
		if (!isServerBuffer)
		{
			return sendBufferSize;
		}
		return 16;
	}

	internal static void UnwrapWebSocketBuffer(global::Interop.WebSocket.Buffer buffer, WebSocketProtocolComponent.BufferType bufferType, out IntPtr bufferData, out uint bufferLength)
	{
		bufferData = IntPtr.Zero;
		bufferLength = 0u;
		switch (bufferType)
		{
		case WebSocketProtocolComponent.BufferType.Close:
			bufferData = buffer.CloseStatus.ReasonData;
			bufferLength = buffer.CloseStatus.ReasonLength;
			break;
		case WebSocketProtocolComponent.BufferType.UTF8Message:
		case WebSocketProtocolComponent.BufferType.UTF8Fragment:
		case WebSocketProtocolComponent.BufferType.BinaryMessage:
		case WebSocketProtocolComponent.BufferType.BinaryFragment:
		case WebSocketProtocolComponent.BufferType.PingPong:
		case WebSocketProtocolComponent.BufferType.UnsolicitedPong:
		case WebSocketProtocolComponent.BufferType.None:
			bufferData = buffer.Data.BufferData;
			bufferLength = buffer.Data.BufferLength;
			break;
		}
	}

	private void ThrowIfDisposed()
	{
		switch (_stateWhenDisposing)
		{
		case int.MinValue:
			break;
		case 5:
		case 6:
			throw new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, typeof(WebSocketBase), _stateWhenDisposing));
		default:
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	private int GetOffset(IntPtr pBuffer)
	{
		return (int)(pBuffer.ToInt64() - _startAddress + _internalBuffer.Offset);
	}

	private int GetMaxBufferSize()
	{
		return Math.Max(_receiveBufferSize, _sendBufferSize);
	}

	internal bool IsInternalBuffer(byte[] buffer, int offset, int count)
	{
		if (buffer == _nativeBuffer.Array && offset >= _nativeBuffer.Offset)
		{
			return offset + count <= _nativeBuffer.Offset + _nativeBuffer.Count;
		}
		return false;
	}

	internal IntPtr ToIntPtr(int offset)
	{
		return new IntPtr(_startAddress + offset - _internalBuffer.Offset);
	}

	private bool IsNativeBuffer(IntPtr pBuffer, uint bufferSize)
	{
		long num = pBuffer.ToInt64();
		long num2 = bufferSize + num;
		if (num >= _startAddress && num <= _endAddress && num2 >= _startAddress && num2 <= _endAddress)
		{
			return true;
		}
		return false;
	}

	private void CleanUp()
	{
		if (_gcHandle.IsAllocated)
		{
			_gcHandle.Free();
		}
		ReleasePinnedSendBuffer();
	}

	internal static ArraySegment<byte> CreateInternalBufferArraySegment(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
	{
		int internalBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
		return new ArraySegment<byte>(new byte[internalBufferSize]);
	}

	internal static void Validate(int count, int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
	{
		int internalBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
		if (count < internalBufferSize)
		{
			throw new ArgumentOutOfRangeException("internalBuffer", System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_InternalBuffer, internalBufferSize));
		}
	}

	private static int GetInternalBufferSize(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
	{
		int nativeSendBufferSize = GetNativeSendBufferSize(sendBufferSize, isServerBuffer);
		return 2 * receiveBufferSize + nativeSendBufferSize + 144 + s_PropertyBufferSize;
	}
}
