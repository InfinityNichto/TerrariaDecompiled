using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.IO;

public class UnmanagedMemoryAccessor : IDisposable
{
	private SafeBuffer _buffer;

	private long _offset;

	private long _capacity;

	private FileAccess _access;

	private bool _isOpen;

	private bool _canRead;

	private bool _canWrite;

	public long Capacity => _capacity;

	public bool CanRead
	{
		get
		{
			if (_isOpen)
			{
				return _canRead;
			}
			return false;
		}
	}

	public bool CanWrite
	{
		get
		{
			if (_isOpen)
			{
				return _canWrite;
			}
			return false;
		}
	}

	protected bool IsOpen => _isOpen;

	protected UnmanagedMemoryAccessor()
	{
	}

	public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity)
	{
		Initialize(buffer, offset, capacity, FileAccess.Read);
	}

	public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity, FileAccess access)
	{
		Initialize(buffer, offset, capacity, access);
	}

	protected unsafe void Initialize(SafeBuffer buffer, long offset, long capacity, FileAccess access)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.ByteLength < (ulong)(offset + capacity))
		{
			throw new ArgumentException(SR.Argument_OffsetAndCapacityOutOfBounds);
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (_isOpen)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
		}
		byte* pointer = null;
		try
		{
			buffer.AcquirePointer(ref pointer);
			if ((nuint)((long)pointer + offset + capacity) < (nuint)pointer)
			{
				throw new ArgumentException(SR.Argument_UnmanagedMemAccessorWrapAround);
			}
		}
		finally
		{
			if (pointer != null)
			{
				buffer.ReleasePointer();
			}
		}
		_offset = offset;
		_buffer = buffer;
		_capacity = capacity;
		_access = access;
		_isOpen = true;
		_canRead = (_access & FileAccess.Read) != 0;
		_canWrite = (_access & FileAccess.Write) != 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		_isOpen = false;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public bool ReadBoolean(long position)
	{
		return ReadByte(position) != 0;
	}

	public unsafe byte ReadByte(long position)
	{
		EnsureSafeToRead(position, 1);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			return (pointer + _offset)[position];
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public char ReadChar(long position)
	{
		return (char)ReadInt16(position);
	}

	public unsafe short ReadInt16(long position)
	{
		EnsureSafeToRead(position, 2);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			return Unsafe.ReadUnaligned<short>(pointer + _offset + position);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe int ReadInt32(long position)
	{
		EnsureSafeToRead(position, 4);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			return Unsafe.ReadUnaligned<int>(pointer + _offset + position);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe long ReadInt64(long position)
	{
		EnsureSafeToRead(position, 8);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			return Unsafe.ReadUnaligned<long>(pointer + _offset + position);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe decimal ReadDecimal(long position)
	{
		EnsureSafeToRead(position, 16);
		byte* pointer = null;
		int lo;
		int mid;
		int hi;
		int num;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			pointer += _offset + position;
			lo = Unsafe.ReadUnaligned<int>(pointer);
			mid = Unsafe.ReadUnaligned<int>(pointer + 4);
			hi = Unsafe.ReadUnaligned<int>(pointer + 8);
			num = Unsafe.ReadUnaligned<int>(pointer + 12);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
		if (((uint)num & 0x7F00FFFFu) != 0 || (num & 0xFF0000) > 1835008)
		{
			throw new ArgumentException(SR.Arg_BadDecimal);
		}
		bool isNegative = (num & int.MinValue) != 0;
		byte scale = (byte)(num >> 16);
		return new decimal(lo, mid, hi, isNegative, scale);
	}

	public float ReadSingle(long position)
	{
		return BitConverter.Int32BitsToSingle(ReadInt32(position));
	}

	public double ReadDouble(long position)
	{
		return BitConverter.Int64BitsToDouble(ReadInt64(position));
	}

	[CLSCompliant(false)]
	public sbyte ReadSByte(long position)
	{
		return (sbyte)ReadByte(position);
	}

	[CLSCompliant(false)]
	public ushort ReadUInt16(long position)
	{
		return (ushort)ReadInt16(position);
	}

	[CLSCompliant(false)]
	public uint ReadUInt32(long position)
	{
		return (uint)ReadInt32(position);
	}

	[CLSCompliant(false)]
	public ulong ReadUInt64(long position)
	{
		return (ulong)ReadInt64(position);
	}

	public void Read<T>(long position, out T structure) where T : struct
	{
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canRead)
		{
			throw new NotSupportedException(SR.NotSupported_Reading);
		}
		uint num = SafeBuffer.SizeOf<T>();
		if (position > _capacity - num)
		{
			if (position >= _capacity)
			{
				throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
			}
			throw new ArgumentException(SR.Format(SR.Argument_NotEnoughBytesToRead, typeof(T)), "position");
		}
		structure = _buffer.Read<T>((ulong)(_offset + position));
	}

	public int ReadArray<T>(long position, T[] array, int offset, int count) where T : struct
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - offset < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canRead)
		{
			throw new NotSupportedException(SR.NotSupported_Reading);
		}
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		uint num = SafeBuffer.AlignedSizeOf<T>();
		if (position >= _capacity)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
		}
		int num2 = count;
		long num3 = _capacity - position;
		if (num3 < 0)
		{
			num2 = 0;
		}
		else
		{
			ulong num4 = (ulong)(num * count);
			if ((ulong)num3 < num4)
			{
				num2 = (int)(num3 / num);
			}
		}
		_buffer.ReadArray((ulong)(_offset + position), array, offset, num2);
		return num2;
	}

	public void Write(long position, bool value)
	{
		Write(position, (byte)(value ? 1u : 0u));
	}

	public unsafe void Write(long position, byte value)
	{
		EnsureSafeToWrite(position, 1);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			(pointer + _offset)[position] = value;
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public void Write(long position, char value)
	{
		Write(position, (short)value);
	}

	public unsafe void Write(long position, short value)
	{
		EnsureSafeToWrite(position, 2);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			Unsafe.WriteUnaligned(pointer + _offset + position, value);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe void Write(long position, int value)
	{
		EnsureSafeToWrite(position, 4);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			Unsafe.WriteUnaligned(pointer + _offset + position, value);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe void Write(long position, long value)
	{
		EnsureSafeToWrite(position, 8);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			Unsafe.WriteUnaligned(pointer + _offset + position, value);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public unsafe void Write(long position, decimal value)
	{
		EnsureSafeToWrite(position, 16);
		Span<int> destination = stackalloc int[4];
		decimal.TryGetBits(value, destination, out var _);
		byte* pointer = null;
		try
		{
			_buffer.AcquirePointer(ref pointer);
			pointer += _offset + position;
			Unsafe.WriteUnaligned(pointer, destination[0]);
			Unsafe.WriteUnaligned(pointer + 4, destination[1]);
			Unsafe.WriteUnaligned(pointer + 8, destination[2]);
			Unsafe.WriteUnaligned(pointer + 12, destination[3]);
		}
		finally
		{
			if (pointer != null)
			{
				_buffer.ReleasePointer();
			}
		}
	}

	public void Write(long position, float value)
	{
		Write(position, BitConverter.SingleToInt32Bits(value));
	}

	public void Write(long position, double value)
	{
		Write(position, BitConverter.DoubleToInt64Bits(value));
	}

	[CLSCompliant(false)]
	public void Write(long position, sbyte value)
	{
		Write(position, (byte)value);
	}

	[CLSCompliant(false)]
	public void Write(long position, ushort value)
	{
		Write(position, (short)value);
	}

	[CLSCompliant(false)]
	public void Write(long position, uint value)
	{
		Write(position, (int)value);
	}

	[CLSCompliant(false)]
	public void Write(long position, ulong value)
	{
		Write(position, (long)value);
	}

	public void Write<T>(long position, ref T structure) where T : struct
	{
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canWrite)
		{
			throw new NotSupportedException(SR.NotSupported_Writing);
		}
		uint num = SafeBuffer.SizeOf<T>();
		if (position > _capacity - num)
		{
			if (position >= _capacity)
			{
				throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
			}
			throw new ArgumentException(SR.Format(SR.Argument_NotEnoughBytesToWrite, typeof(T)), "position");
		}
		_buffer.Write((ulong)(_offset + position), structure);
	}

	public void WriteArray<T>(long position, T[] array, int offset, int count) where T : struct
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - offset < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (position >= Capacity)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
		}
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canWrite)
		{
			throw new NotSupportedException(SR.NotSupported_Writing);
		}
		_buffer.WriteArray((ulong)(_offset + position), array, offset, count);
	}

	private void EnsureSafeToRead(long position, int sizeOfType)
	{
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canRead)
		{
			throw new NotSupportedException(SR.NotSupported_Reading);
		}
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (position > _capacity - sizeOfType)
		{
			if (position >= _capacity)
			{
				throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
			}
			throw new ArgumentException(SR.Argument_NotEnoughBytesToRead, "position");
		}
	}

	private void EnsureSafeToWrite(long position, int sizeOfType)
	{
		if (!_isOpen)
		{
			throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
		}
		if (!_canWrite)
		{
			throw new NotSupportedException(SR.NotSupported_Writing);
		}
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (position > _capacity - sizeOfType)
		{
			if (position >= _capacity)
			{
				throw new ArgumentOutOfRangeException("position", SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
			}
			throw new ArgumentException(SR.Argument_NotEnoughBytesToWrite, "position");
		}
	}
}
