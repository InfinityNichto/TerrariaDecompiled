using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public struct BlobReader
{
	internal const int InvalidCompressedInteger = int.MaxValue;

	private readonly MemoryBlock _block;

	private unsafe readonly byte* _endPointer;

	private unsafe byte* _currentPointer;

	private static readonly uint[] s_corEncodeTokenArray = new uint[4] { 33554432u, 16777216u, 452984832u, 0u };

	public unsafe byte* StartPointer => _block.Pointer;

	public unsafe byte* CurrentPointer => _currentPointer;

	public int Length => _block.Length;

	public unsafe int Offset
	{
		get
		{
			return (int)(_currentPointer - _block.Pointer);
		}
		set
		{
			if ((uint)value > (uint)_block.Length)
			{
				Throw.OutOfBounds();
			}
			_currentPointer = _block.Pointer + value;
		}
	}

	public unsafe int RemainingBytes => (int)(_endPointer - _currentPointer);

	public unsafe BlobReader(byte* buffer, int length)
		: this(MemoryBlock.CreateChecked(buffer, length))
	{
	}

	internal unsafe BlobReader(MemoryBlock block)
	{
		_block = block;
		_currentPointer = block.Pointer;
		_endPointer = block.Pointer + block.Length;
	}

	internal unsafe string GetDebuggerDisplay()
	{
		if (_block.Pointer == null)
		{
			return "<null>";
		}
		int displayedBytes;
		string debuggerDisplay = _block.GetDebuggerDisplay(out displayedBytes);
		if (Offset < displayedBytes)
		{
			return debuggerDisplay.Insert(Offset * 3, "*");
		}
		if (displayedBytes == _block.Length)
		{
			return debuggerDisplay + "*";
		}
		return debuggerDisplay + "*...";
	}

	public unsafe void Reset()
	{
		_currentPointer = _block.Pointer;
	}

	public void Align(byte alignment)
	{
		if (!TryAlign(alignment))
		{
			Throw.OutOfBounds();
		}
	}

	internal unsafe bool TryAlign(byte alignment)
	{
		int num = Offset & (alignment - 1);
		if (num != 0)
		{
			int num2 = alignment - num;
			if (num2 > RemainingBytes)
			{
				return false;
			}
			_currentPointer += num2;
		}
		return true;
	}

	internal unsafe MemoryBlock GetMemoryBlockAt(int offset, int length)
	{
		CheckBounds(offset, length);
		return new MemoryBlock(_currentPointer + offset, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void CheckBounds(int offset, int byteCount)
	{
		if ((ulong)((long)(uint)offset + (long)(uint)byteCount) > (ulong)(_endPointer - _currentPointer))
		{
			Throw.OutOfBounds();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe byte* GetCurrentPointerAndAdvance(int length)
	{
		byte* currentPointer = _currentPointer;
		if ((uint)length > (uint)(_endPointer - currentPointer))
		{
			Throw.OutOfBounds();
		}
		_currentPointer = currentPointer + length;
		return currentPointer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe byte* GetCurrentPointerAndAdvance1()
	{
		byte* currentPointer = _currentPointer;
		if (currentPointer == _endPointer)
		{
			Throw.OutOfBounds();
		}
		_currentPointer = currentPointer + 1;
		return currentPointer;
	}

	public bool ReadBoolean()
	{
		return ReadByte() != 0;
	}

	public unsafe sbyte ReadSByte()
	{
		return (sbyte)(*GetCurrentPointerAndAdvance1());
	}

	public unsafe byte ReadByte()
	{
		return *GetCurrentPointerAndAdvance1();
	}

	public unsafe char ReadChar()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(2);
		return (char)(*currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8));
	}

	public unsafe short ReadInt16()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(2);
		return (short)(*currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8));
	}

	public unsafe ushort ReadUInt16()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(2);
		return (ushort)(*currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8));
	}

	public unsafe int ReadInt32()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(4);
		return *currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8) + (currentPointerAndAdvance[2] << 16) + (currentPointerAndAdvance[3] << 24);
	}

	public unsafe uint ReadUInt32()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(4);
		return (uint)(*currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8) + (currentPointerAndAdvance[2] << 16) + (currentPointerAndAdvance[3] << 24));
	}

	public unsafe long ReadInt64()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(8);
		uint num = (uint)(*currentPointerAndAdvance + (currentPointerAndAdvance[1] << 8) + (currentPointerAndAdvance[2] << 16) + (currentPointerAndAdvance[3] << 24));
		uint num2 = (uint)(currentPointerAndAdvance[4] + (currentPointerAndAdvance[5] << 8) + (currentPointerAndAdvance[6] << 16) + (currentPointerAndAdvance[7] << 24));
		return (long)(num + ((ulong)num2 << 32));
	}

	public ulong ReadUInt64()
	{
		return (ulong)ReadInt64();
	}

	public unsafe float ReadSingle()
	{
		int num = ReadInt32();
		return *(float*)(&num);
	}

	public unsafe double ReadDouble()
	{
		long num = ReadInt64();
		return *(double*)(&num);
	}

	public unsafe Guid ReadGuid()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(16);
		if (BitConverter.IsLittleEndian)
		{
			return *(Guid*)currentPointerAndAdvance;
		}
		return new Guid(*currentPointerAndAdvance | (currentPointerAndAdvance[1] << 8) | (currentPointerAndAdvance[2] << 16) | (currentPointerAndAdvance[3] << 24), (short)(currentPointerAndAdvance[4] | (currentPointerAndAdvance[5] << 8)), (short)(currentPointerAndAdvance[6] | (currentPointerAndAdvance[7] << 8)), currentPointerAndAdvance[8], currentPointerAndAdvance[9], currentPointerAndAdvance[10], currentPointerAndAdvance[11], currentPointerAndAdvance[12], currentPointerAndAdvance[13], currentPointerAndAdvance[14], currentPointerAndAdvance[15]);
	}

	public unsafe decimal ReadDecimal()
	{
		byte* currentPointerAndAdvance = GetCurrentPointerAndAdvance(13);
		byte b = (byte)(*currentPointerAndAdvance & 0x7Fu);
		if (b > 28)
		{
			throw new BadImageFormatException(System.SR.ValueTooLarge);
		}
		return new decimal(currentPointerAndAdvance[1] | (currentPointerAndAdvance[2] << 8) | (currentPointerAndAdvance[3] << 16) | (currentPointerAndAdvance[4] << 24), currentPointerAndAdvance[5] | (currentPointerAndAdvance[6] << 8) | (currentPointerAndAdvance[7] << 16) | (currentPointerAndAdvance[8] << 24), currentPointerAndAdvance[9] | (currentPointerAndAdvance[10] << 8) | (currentPointerAndAdvance[11] << 16) | (currentPointerAndAdvance[12] << 24), (*currentPointerAndAdvance & 0x80) != 0, b);
	}

	public DateTime ReadDateTime()
	{
		return new DateTime(ReadInt64());
	}

	public SignatureHeader ReadSignatureHeader()
	{
		return new SignatureHeader(ReadByte());
	}

	public int IndexOf(byte value)
	{
		int offset = Offset;
		int num = _block.IndexOfUnchecked(value, offset);
		if (num < 0)
		{
			return -1;
		}
		return num - offset;
	}

	public unsafe string ReadUTF8(int byteCount)
	{
		string result = _block.PeekUtf8(Offset, byteCount);
		_currentPointer += byteCount;
		return result;
	}

	public unsafe string ReadUTF16(int byteCount)
	{
		string result = _block.PeekUtf16(Offset, byteCount);
		_currentPointer += byteCount;
		return result;
	}

	public unsafe byte[] ReadBytes(int byteCount)
	{
		byte[] result = _block.PeekBytes(Offset, byteCount);
		_currentPointer += byteCount;
		return result;
	}

	public unsafe void ReadBytes(int byteCount, byte[] buffer, int bufferOffset)
	{
		Marshal.Copy((IntPtr)GetCurrentPointerAndAdvance(byteCount), buffer, bufferOffset, byteCount);
	}

	internal unsafe string ReadUtf8NullTerminated()
	{
		int numberOfBytesRead;
		string result = _block.PeekUtf8NullTerminated(Offset, null, MetadataStringDecoder.DefaultUTF8, out numberOfBytesRead);
		_currentPointer += numberOfBytesRead;
		return result;
	}

	private unsafe int ReadCompressedIntegerOrInvalid()
	{
		int numberOfBytesRead;
		int result = _block.PeekCompressedInteger(Offset, out numberOfBytesRead);
		_currentPointer += numberOfBytesRead;
		return result;
	}

	public bool TryReadCompressedInteger(out int value)
	{
		value = ReadCompressedIntegerOrInvalid();
		return value != int.MaxValue;
	}

	public int ReadCompressedInteger()
	{
		if (!TryReadCompressedInteger(out var value))
		{
			Throw.InvalidCompressedInteger();
		}
		return value;
	}

	public unsafe bool TryReadCompressedSignedInteger(out int value)
	{
		value = _block.PeekCompressedInteger(Offset, out var numberOfBytesRead);
		if (value == int.MaxValue)
		{
			return false;
		}
		bool flag = (value & 1) != 0;
		value >>= 1;
		if (flag)
		{
			switch (numberOfBytesRead)
			{
			case 1:
				value |= -64;
				break;
			case 2:
				value |= -8192;
				break;
			default:
				value |= -268435456;
				break;
			}
		}
		_currentPointer += numberOfBytesRead;
		return true;
	}

	public int ReadCompressedSignedInteger()
	{
		if (!TryReadCompressedSignedInteger(out var value))
		{
			Throw.InvalidCompressedInteger();
		}
		return value;
	}

	public SerializationTypeCode ReadSerializationTypeCode()
	{
		int num = ReadCompressedIntegerOrInvalid();
		if (num > 255)
		{
			return SerializationTypeCode.Invalid;
		}
		return (SerializationTypeCode)num;
	}

	public SignatureTypeCode ReadSignatureTypeCode()
	{
		int num = ReadCompressedIntegerOrInvalid();
		if ((uint)(num - 17) <= 1u)
		{
			return SignatureTypeCode.TypeHandle;
		}
		if (num > 255)
		{
			return SignatureTypeCode.Invalid;
		}
		return (SignatureTypeCode)num;
	}

	public string? ReadSerializedString()
	{
		if (TryReadCompressedInteger(out var value))
		{
			return ReadUTF8(value);
		}
		if (ReadByte() != byte.MaxValue)
		{
			Throw.InvalidSerializedString();
		}
		return null;
	}

	public EntityHandle ReadTypeHandle()
	{
		uint num = (uint)ReadCompressedIntegerOrInvalid();
		uint num2 = s_corEncodeTokenArray[num & 3];
		if (num == int.MaxValue || num2 == 0)
		{
			return default(EntityHandle);
		}
		return new EntityHandle(num2 | (num >> 2));
	}

	public BlobHandle ReadBlobHandle()
	{
		return BlobHandle.FromOffset(ReadCompressedInteger());
	}

	public object? ReadConstant(ConstantTypeCode typeCode)
	{
		switch (typeCode)
		{
		case ConstantTypeCode.Boolean:
			return ReadBoolean();
		case ConstantTypeCode.Char:
			return ReadChar();
		case ConstantTypeCode.SByte:
			return ReadSByte();
		case ConstantTypeCode.Int16:
			return ReadInt16();
		case ConstantTypeCode.Int32:
			return ReadInt32();
		case ConstantTypeCode.Int64:
			return ReadInt64();
		case ConstantTypeCode.Byte:
			return ReadByte();
		case ConstantTypeCode.UInt16:
			return ReadUInt16();
		case ConstantTypeCode.UInt32:
			return ReadUInt32();
		case ConstantTypeCode.UInt64:
			return ReadUInt64();
		case ConstantTypeCode.Single:
			return ReadSingle();
		case ConstantTypeCode.Double:
			return ReadDouble();
		case ConstantTypeCode.String:
			return ReadUTF16(RemainingBytes);
		case ConstantTypeCode.NullReference:
			if (ReadUInt32() != 0)
			{
				throw new BadImageFormatException(System.SR.InvalidConstantValue);
			}
			return null;
		default:
			throw new ArgumentOutOfRangeException("typeCode");
		}
	}
}
