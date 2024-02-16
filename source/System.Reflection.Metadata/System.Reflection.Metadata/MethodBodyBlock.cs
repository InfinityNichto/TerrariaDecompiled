using System.Collections.Immutable;
using System.Reflection.Internal;

namespace System.Reflection.Metadata;

public sealed class MethodBodyBlock
{
	private readonly MemoryBlock _il;

	private readonly int _size;

	private readonly ushort _maxStack;

	private readonly bool _localVariablesInitialized;

	private readonly StandaloneSignatureHandle _localSignature;

	private readonly ImmutableArray<ExceptionRegion> _exceptionRegions;

	public int Size => _size;

	public int MaxStack => _maxStack;

	public bool LocalVariablesInitialized => _localVariablesInitialized;

	public StandaloneSignatureHandle LocalSignature => _localSignature;

	public ImmutableArray<ExceptionRegion> ExceptionRegions => _exceptionRegions;

	private MethodBodyBlock(bool localVariablesInitialized, ushort maxStack, StandaloneSignatureHandle localSignatureHandle, MemoryBlock il, ImmutableArray<ExceptionRegion> exceptionRegions, int size)
	{
		_localVariablesInitialized = localVariablesInitialized;
		_maxStack = maxStack;
		_localSignature = localSignatureHandle;
		_il = il;
		_exceptionRegions = exceptionRegions;
		_size = size;
	}

	public byte[]? GetILBytes()
	{
		return _il.ToArray();
	}

	public ImmutableArray<byte> GetILContent()
	{
		byte[] array = GetILBytes();
		return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);
	}

	public BlobReader GetILReader()
	{
		return new BlobReader(_il);
	}

	public static MethodBodyBlock Create(BlobReader reader)
	{
		int offset = reader.Offset;
		byte b = reader.ReadByte();
		int num;
		if ((b & 3) == 2)
		{
			num = b >> 2;
			return new MethodBodyBlock(localVariablesInitialized: false, 8, default(StandaloneSignatureHandle), reader.GetMemoryBlockAt(0, num), ImmutableArray<ExceptionRegion>.Empty, 1 + num);
		}
		if ((b & 3) != 3)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodHeader1, b));
		}
		byte b2 = reader.ReadByte();
		if (b2 >> 4 != 3)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodHeader2, b, b2));
		}
		bool localVariablesInitialized = (b & 0x10) == 16;
		bool flag = (b & 8) == 8;
		ushort maxStack = reader.ReadUInt16();
		num = reader.ReadInt32();
		int num2 = reader.ReadInt32();
		StandaloneSignatureHandle localSignatureHandle;
		if (num2 == 0)
		{
			localSignatureHandle = default(StandaloneSignatureHandle);
		}
		else
		{
			if (((ulong)num2 & 0x7F000000uL) != 285212672)
			{
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidLocalSignatureToken, (uint)num2));
			}
			localSignatureHandle = StandaloneSignatureHandle.FromRowId(num2 & 0xFFFFFF);
		}
		MemoryBlock memoryBlockAt = reader.GetMemoryBlockAt(0, num);
		reader.Offset += num;
		ImmutableArray<ExceptionRegion> exceptionRegions;
		if (flag)
		{
			reader.Align(4);
			byte b3 = reader.ReadByte();
			if ((b3 & 1) != 1)
			{
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidSehHeader, b3));
			}
			bool flag2 = (b3 & 0x40) == 64;
			int num3 = reader.ReadByte();
			if (flag2)
			{
				num3 += reader.ReadUInt16() << 8;
				exceptionRegions = ReadFatExceptionHandlers(ref reader, num3 / 24);
			}
			else
			{
				reader.Offset += 2;
				exceptionRegions = ReadSmallExceptionHandlers(ref reader, num3 / 12);
			}
		}
		else
		{
			exceptionRegions = ImmutableArray<ExceptionRegion>.Empty;
		}
		return new MethodBodyBlock(localVariablesInitialized, maxStack, localSignatureHandle, memoryBlockAt, exceptionRegions, reader.Offset - offset);
	}

	private static ImmutableArray<ExceptionRegion> ReadSmallExceptionHandlers(ref BlobReader memReader, int count)
	{
		ExceptionRegion[] array = new ExceptionRegion[count];
		for (int i = 0; i < array.Length; i++)
		{
			ExceptionRegionKind kind = (ExceptionRegionKind)memReader.ReadUInt16();
			ushort tryOffset = memReader.ReadUInt16();
			byte tryLength = memReader.ReadByte();
			ushort handlerOffset = memReader.ReadUInt16();
			byte handlerLength = memReader.ReadByte();
			int classTokenOrFilterOffset = memReader.ReadInt32();
			array[i] = new ExceptionRegion(kind, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset);
		}
		return ImmutableArray.Create(array);
	}

	private static ImmutableArray<ExceptionRegion> ReadFatExceptionHandlers(ref BlobReader memReader, int count)
	{
		ExceptionRegion[] array = new ExceptionRegion[count];
		for (int i = 0; i < array.Length; i++)
		{
			ExceptionRegionKind kind = (ExceptionRegionKind)memReader.ReadUInt32();
			int tryOffset = memReader.ReadInt32();
			int tryLength = memReader.ReadInt32();
			int handlerOffset = memReader.ReadInt32();
			int handlerLength = memReader.ReadInt32();
			int classTokenOrFilterOffset = memReader.ReadInt32();
			array[i] = new ExceptionRegion(kind, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset);
		}
		return ImmutableArray.Create(array);
	}
}
