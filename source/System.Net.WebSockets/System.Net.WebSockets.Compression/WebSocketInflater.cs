using System.Buffers;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace System.Net.WebSockets.Compression;

internal sealed class WebSocketInflater : IDisposable
{
	private readonly int _windowBits;

	private ZLibNative.ZLibStreamHandle _stream;

	private readonly bool _persisted;

	private byte? _remainingByte;

	private bool _endOfMessage;

	private byte[] _buffer;

	private int _position;

	private int _available;

	internal static ReadOnlySpan<byte> FlushMarker => new byte[4] { 0, 0, 255, 255 };

	public Memory<byte> Memory => _buffer.AsMemory(_position + _available);

	public Span<byte> Span => _buffer.AsSpan(_position + _available);

	internal WebSocketInflater(int windowBits, bool persisted)
	{
		_windowBits = -windowBits;
		_persisted = persisted;
	}

	public void Dispose()
	{
		if (_stream != null)
		{
			_stream.Dispose();
			_stream = null;
		}
		ReleaseBuffer();
	}

	public void Prepare(long payloadLength, int userBufferLength)
	{
		if (_buffer != null)
		{
			_buffer.AsSpan(_position, _available).CopyTo(_buffer);
			_position = 0;
		}
		else
		{
			_buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(userBufferLength, payloadLength));
		}
	}

	public void AddBytes(int totalBytesReceived, bool endOfMessage)
	{
		_available += totalBytesReceived;
		_endOfMessage = endOfMessage;
		if (!endOfMessage)
		{
			return;
		}
		if (_buffer == null)
		{
			_buffer = ArrayPool<byte>.Shared.Rent(4);
			_available = 4;
			FlushMarker.CopyTo(_buffer);
			return;
		}
		if (_buffer.Length < _available + 4)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(_available + 4);
			_buffer.AsSpan(0, _available).CopyTo(array);
			byte[] buffer = _buffer;
			_buffer = array;
			ArrayPool<byte>.Shared.Return(buffer);
		}
		FlushMarker.CopyTo(_buffer.AsSpan(_available));
		_available += 4;
	}

	public unsafe bool Inflate(Span<byte> output, out int written)
	{
		if (_stream == null)
		{
			_stream = CreateInflater();
		}
		if (_available > 0 && output.Length > 0)
		{
			int num;
			fixed (byte* ptr = _buffer)
			{
				_stream.NextIn = (IntPtr)(ptr + _position);
				_stream.AvailIn = (uint)_available;
				written = Inflate(_stream, output, ZLibNative.FlushCode.NoFlush);
				num = _available - (int)_stream.AvailIn;
			}
			_position += num;
			_available -= num;
		}
		else
		{
			written = 0;
		}
		if (_available == 0)
		{
			ReleaseBuffer();
			if (!_endOfMessage)
			{
				return true;
			}
			return Finish(output, ref written);
		}
		return false;
	}

	private bool Finish(Span<byte> output, ref int written)
	{
		byte? remainingByte = _remainingByte;
		if (remainingByte.HasValue)
		{
			if (output.Length == written)
			{
				return false;
			}
			output[written] = _remainingByte.GetValueOrDefault();
			_remainingByte = null;
			written++;
		}
		if (output.Length > written)
		{
			int num = written;
			ZLibNative.ZLibStreamHandle stream = _stream;
			Span<byte> span = output;
			written = num + Inflate(stream, span[written..], ZLibNative.FlushCode.SyncFlush);
		}
		if (written < output.Length || IsFinished(_stream, out _remainingByte))
		{
			if (!_persisted)
			{
				_stream.Dispose();
				_stream = null;
			}
			return true;
		}
		return false;
	}

	private void ReleaseBuffer()
	{
		byte[] buffer = _buffer;
		if (buffer != null)
		{
			_buffer = null;
			_available = 0;
			_position = 0;
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private unsafe static bool IsFinished(ZLibNative.ZLibStreamHandle stream, out byte? remainingByte)
	{
		Unsafe.SkipInit(out byte value);
		if (Inflate(stream, new Span<byte>(&value, 1), ZLibNative.FlushCode.SyncFlush) == 0)
		{
			remainingByte = null;
			return true;
		}
		remainingByte = value;
		return false;
	}

	private unsafe static int Inflate(ZLibNative.ZLibStreamHandle stream, Span<byte> destination, ZLibNative.FlushCode flushCode)
	{
		ZLibNative.ErrorCode errorCode;
		fixed (byte* ptr = destination)
		{
			stream.NextOut = (IntPtr)ptr;
			stream.AvailOut = (uint)destination.Length;
			errorCode = stream.Inflate(flushCode);
			if (errorCode == ZLibNative.ErrorCode.Ok || errorCode == ZLibNative.ErrorCode.StreamEnd || errorCode == ZLibNative.ErrorCode.BufError)
			{
				return destination.Length - (int)stream.AvailOut;
			}
		}
		throw new WebSocketException(errorCode switch
		{
			ZLibNative.ErrorCode.MemError => System.SR.ZLibErrorNotEnoughMemory, 
			ZLibNative.ErrorCode.DataError => System.SR.ZLibUnsupportedCompression, 
			ZLibNative.ErrorCode.StreamError => System.SR.ZLibErrorInconsistentStream, 
			_ => string.Format(System.SR.ZLibErrorUnexpected, (int)errorCode), 
		});
	}

	private ZLibNative.ZLibStreamHandle CreateInflater()
	{
		ZLibNative.ErrorCode errorCode;
		ZLibNative.ZLibStreamHandle zLibStreamHandle;
		try
		{
			errorCode = ZLibNative.CreateZLibStreamForInflate(out zLibStreamHandle, _windowBits);
		}
		catch (Exception innerException)
		{
			throw new WebSocketException(System.SR.ZLibErrorDLLLoadError, innerException);
		}
		if (errorCode == ZLibNative.ErrorCode.Ok)
		{
			return zLibStreamHandle;
		}
		zLibStreamHandle.Dispose();
		string message = ((errorCode == ZLibNative.ErrorCode.MemError) ? System.SR.ZLibErrorNotEnoughMemory : string.Format(System.SR.ZLibErrorUnexpected, (int)errorCode));
		throw new WebSocketException(message);
	}
}
