using System.Reflection.Emit;

namespace System.Runtime.Serialization;

internal sealed class BitFlagsGenerator
{
	private readonly int _bitCount;

	private readonly CodeGenerator _ilg;

	private readonly LocalBuilder[] _locals;

	public BitFlagsGenerator(int bitCount, CodeGenerator ilg, string localName)
	{
		_ilg = ilg;
		_bitCount = bitCount;
		int num = (bitCount + 7) / 8;
		_locals = new LocalBuilder[num];
		for (int i = 0; i < _locals.Length; i++)
		{
			_locals[i] = ilg.DeclareLocal(typeof(byte), localName + i, (byte)0);
		}
	}

	public static bool IsBitSet(byte[] bytes, int bitIndex)
	{
		int byteIndex = GetByteIndex(bitIndex);
		byte bitValue = GetBitValue(bitIndex);
		return (bytes[byteIndex] & bitValue) == bitValue;
	}

	public static void SetBit(byte[] bytes, int bitIndex)
	{
		int byteIndex = GetByteIndex(bitIndex);
		byte bitValue = GetBitValue(bitIndex);
		bytes[byteIndex] |= bitValue;
	}

	public int GetBitCount()
	{
		return _bitCount;
	}

	public LocalBuilder GetLocal(int i)
	{
		return _locals[i];
	}

	public int GetLocalCount()
	{
		return _locals.Length;
	}

	public void Load(int bitIndex)
	{
		LocalBuilder obj = _locals[GetByteIndex(bitIndex)];
		byte bitValue = GetBitValue(bitIndex);
		_ilg.Load(obj);
		_ilg.Load(bitValue);
		_ilg.And();
		_ilg.Load(bitValue);
		_ilg.Ceq();
	}

	public void LoadArray()
	{
		LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfByteArray, "localArray");
		_ilg.NewArray(typeof(byte), _locals.Length);
		_ilg.Store(localBuilder);
		for (int i = 0; i < _locals.Length; i++)
		{
			_ilg.StoreArrayElement(localBuilder, i, _locals[i]);
		}
		_ilg.Load(localBuilder);
	}

	public void Store(int bitIndex, bool value)
	{
		LocalBuilder localBuilder = _locals[GetByteIndex(bitIndex)];
		byte bitValue = GetBitValue(bitIndex);
		if (value)
		{
			_ilg.Load(localBuilder);
			_ilg.Load(bitValue);
			_ilg.Or();
			_ilg.Stloc(localBuilder);
		}
		else
		{
			_ilg.Load(localBuilder);
			_ilg.Load(bitValue);
			_ilg.Not();
			_ilg.And();
			_ilg.Stloc(localBuilder);
		}
	}

	private static byte GetBitValue(int bitIndex)
	{
		return (byte)(1 << (bitIndex & 7));
	}

	private static int GetByteIndex(int bitIndex)
	{
		return bitIndex >> 3;
	}
}
