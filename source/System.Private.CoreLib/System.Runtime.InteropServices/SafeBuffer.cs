using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices;

public abstract class SafeBuffer : SafeHandleZeroOrMinusOneIsInvalid
{
	private nuint _numBytes;

	private static nuint Uninitialized => UIntPtr.MaxValue;

	[CLSCompliant(false)]
	public ulong ByteLength
	{
		get
		{
			if (_numBytes == Uninitialized)
			{
				throw NotInitialized();
			}
			return _numBytes;
		}
	}

	protected SafeBuffer(bool ownsHandle)
		: base(ownsHandle)
	{
		_numBytes = Uninitialized;
	}

	[CLSCompliant(false)]
	public void Initialize(ulong numBytes)
	{
		if (IntPtr.Size == 4)
		{
		}
		if (numBytes >= Uninitialized)
		{
			throw new ArgumentOutOfRangeException("numBytes", SR.ArgumentOutOfRange_UIntPtrMax);
		}
		_numBytes = (nuint)numBytes;
	}

	[CLSCompliant(false)]
	public void Initialize(uint numElements, uint sizeOfEachElement)
	{
		Initialize((ulong)numElements * (ulong)sizeOfEachElement);
	}

	[CLSCompliant(false)]
	public void Initialize<T>(uint numElements) where T : struct
	{
		Initialize(numElements, AlignedSizeOf<T>());
	}

	[CLSCompliant(false)]
	public unsafe void AcquirePointer(ref byte* pointer)
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		pointer = null;
		bool success = false;
		DangerousAddRef(ref success);
		pointer = (byte*)(void*)handle;
	}

	public void ReleasePointer()
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		DangerousRelease();
	}

	[CLSCompliant(false)]
	public unsafe T Read<T>(ulong byteOffset) where T : struct
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		uint num = SizeOf<T>();
		byte* ptr = (byte*)(void*)handle + byteOffset;
		SpaceCheck(ptr, num);
		T source = default(T);
		bool success = false;
		try
		{
			DangerousAddRef(ref success);
			Buffer.Memmove(ref Unsafe.As<T, byte>(ref source), ref *ptr, num);
			return source;
		}
		finally
		{
			if (success)
			{
				DangerousRelease();
			}
		}
	}

	[CLSCompliant(false)]
	public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		ReadSpan(byteOffset, new Span<T>(array, index, count));
	}

	[CLSCompliant(false)]
	public unsafe void ReadSpan<T>(ulong byteOffset, Span<T> buffer) where T : struct
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		uint num = AlignedSizeOf<T>();
		byte* ptr = (byte*)(void*)handle + byteOffset;
		SpaceCheck(ptr, checked((nuint)(num * buffer.Length)));
		bool success = false;
		try
		{
			DangerousAddRef(ref success);
			ref T reference = ref MemoryMarshal.GetReference(buffer);
			for (int i = 0; i < buffer.Length; i++)
			{
				Buffer.Memmove(ref Unsafe.Add(ref reference, i), ref Unsafe.AsRef<T>(ptr + num * i), 1u);
			}
		}
		finally
		{
			if (success)
			{
				DangerousRelease();
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe void Write<T>(ulong byteOffset, T value) where T : struct
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		uint num = SizeOf<T>();
		byte* ptr = (byte*)(void*)handle + byteOffset;
		SpaceCheck(ptr, num);
		bool success = false;
		try
		{
			DangerousAddRef(ref success);
			Buffer.Memmove(ref *ptr, ref Unsafe.As<T, byte>(ref value), num);
		}
		finally
		{
			if (success)
			{
				DangerousRelease();
			}
		}
	}

	[CLSCompliant(false)]
	public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		WriteSpan(byteOffset, new ReadOnlySpan<T>(array, index, count));
	}

	[CLSCompliant(false)]
	public unsafe void WriteSpan<T>(ulong byteOffset, ReadOnlySpan<T> data) where T : struct
	{
		if (_numBytes == Uninitialized)
		{
			throw NotInitialized();
		}
		uint num = AlignedSizeOf<T>();
		byte* ptr = (byte*)(void*)handle + byteOffset;
		SpaceCheck(ptr, checked((nuint)(num * data.Length)));
		bool success = false;
		try
		{
			DangerousAddRef(ref success);
			ref T reference = ref MemoryMarshal.GetReference(data);
			for (int i = 0; i < data.Length; i++)
			{
				Buffer.Memmove(ref Unsafe.AsRef<T>(ptr + num * i), ref Unsafe.Add(ref reference, i), 1u);
			}
		}
		finally
		{
			if (success)
			{
				DangerousRelease();
			}
		}
	}

	private unsafe void SpaceCheck(byte* ptr, nuint sizeInBytes)
	{
		if (_numBytes < sizeInBytes)
		{
			NotEnoughRoom();
		}
		if ((ulong)(ptr - (byte*)(void*)handle) > (ulong)(_numBytes - sizeInBytes))
		{
			NotEnoughRoom();
		}
	}

	private static void NotEnoughRoom()
	{
		throw new ArgumentException(SR.Arg_BufferTooSmall);
	}

	private static InvalidOperationException NotInitialized()
	{
		return new InvalidOperationException(SR.InvalidOperation_MustCallInitialize);
	}

	internal static uint AlignedSizeOf<T>() where T : struct
	{
		uint num = SizeOf<T>();
		if (num == 1 || num == 2)
		{
			return num;
		}
		return (uint)((num + 3) & -4);
	}

	internal static uint SizeOf<T>() where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			throw new ArgumentException(SR.Argument_NeedStructWithNoRefs);
		}
		return (uint)Unsafe.SizeOf<T>();
	}
}
