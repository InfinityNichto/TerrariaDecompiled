using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json;

internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
{
	private byte[] _rentedBuffer;

	private int _index;

	public ReadOnlyMemory<byte> WrittenMemory => _rentedBuffer.AsMemory(0, _index);

	public int Capacity => _rentedBuffer.Length;

	public PooledByteBufferWriter(int initialCapacity)
	{
		_rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
		_index = 0;
	}

	public void Clear()
	{
		ClearHelper();
	}

	private void ClearHelper()
	{
		_rentedBuffer.AsSpan(0, _index).Clear();
		_index = 0;
	}

	public void Dispose()
	{
		if (_rentedBuffer != null)
		{
			ClearHelper();
			byte[] rentedBuffer = _rentedBuffer;
			_rentedBuffer = null;
			ArrayPool<byte>.Shared.Return(rentedBuffer);
		}
	}

	public void Advance(int count)
	{
		_index += count;
	}

	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _rentedBuffer.AsMemory(_index);
	}

	public Span<byte> GetSpan(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _rentedBuffer.AsSpan(_index);
	}

	internal ValueTask WriteToStreamAsync(Stream destination, CancellationToken cancellationToken)
	{
		return destination.WriteAsync(WrittenMemory, cancellationToken);
	}

	internal void WriteToStream(Stream destination)
	{
		destination.Write(WrittenMemory.Span);
	}

	private void CheckAndResizeBuffer(int sizeHint)
	{
		if (sizeHint == 0)
		{
			sizeHint = 256;
		}
		int num = _rentedBuffer.Length - _index;
		if (sizeHint <= num)
		{
			return;
		}
		int num2 = _rentedBuffer.Length;
		int num3 = Math.Max(sizeHint, num2);
		int num4 = num2 + num3;
		if ((uint)num4 > 2147483647u)
		{
			num4 = num2 + sizeHint;
			if ((uint)num4 > 2147483647u)
			{
				ThrowHelper.ThrowOutOfMemoryException_BufferMaximumSizeExceeded((uint)num4);
			}
		}
		byte[] rentedBuffer = _rentedBuffer;
		_rentedBuffer = ArrayPool<byte>.Shared.Rent(num4);
		Span<byte> span = rentedBuffer.AsSpan(0, _index);
		span.CopyTo(_rentedBuffer);
		span.Clear();
		ArrayPool<byte>.Shared.Return(rentedBuffer);
	}
}
