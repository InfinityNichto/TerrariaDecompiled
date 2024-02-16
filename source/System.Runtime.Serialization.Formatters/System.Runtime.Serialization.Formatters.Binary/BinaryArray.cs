namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryArray : IStreamable
{
	internal int _objectId;

	internal int _rank;

	internal int[] _lengthA;

	internal int[] _lowerBoundA;

	internal BinaryTypeEnum _binaryTypeEnum;

	internal object _typeInformation;

	internal int _assemId;

	private BinaryHeaderEnum _binaryHeaderEnum;

	internal BinaryArrayTypeEnum _binaryArrayTypeEnum;

	internal BinaryArray()
	{
	}

	internal BinaryArray(BinaryHeaderEnum binaryHeaderEnum)
	{
		_binaryHeaderEnum = binaryHeaderEnum;
	}

	internal void Set(int objectId, int rank, int[] lengthA, int[] lowerBoundA, BinaryTypeEnum binaryTypeEnum, object typeInformation, BinaryArrayTypeEnum binaryArrayTypeEnum, int assemId)
	{
		_objectId = objectId;
		_binaryArrayTypeEnum = binaryArrayTypeEnum;
		_rank = rank;
		_lengthA = lengthA;
		_lowerBoundA = lowerBoundA;
		_binaryTypeEnum = binaryTypeEnum;
		_typeInformation = typeInformation;
		_assemId = assemId;
		_binaryHeaderEnum = BinaryHeaderEnum.Array;
		if (binaryArrayTypeEnum == BinaryArrayTypeEnum.Single)
		{
			switch (binaryTypeEnum)
			{
			case BinaryTypeEnum.Primitive:
				_binaryHeaderEnum = BinaryHeaderEnum.ArraySinglePrimitive;
				break;
			case BinaryTypeEnum.String:
				_binaryHeaderEnum = BinaryHeaderEnum.ArraySingleString;
				break;
			case BinaryTypeEnum.Object:
				_binaryHeaderEnum = BinaryHeaderEnum.ArraySingleObject;
				break;
			}
		}
	}

	public void Write(BinaryFormatterWriter output)
	{
		switch (_binaryHeaderEnum)
		{
		case BinaryHeaderEnum.ArraySinglePrimitive:
			output.WriteByte((byte)_binaryHeaderEnum);
			output.WriteInt32(_objectId);
			output.WriteInt32(_lengthA[0]);
			output.WriteByte((byte)(InternalPrimitiveTypeE)_typeInformation);
			return;
		case BinaryHeaderEnum.ArraySingleString:
			output.WriteByte((byte)_binaryHeaderEnum);
			output.WriteInt32(_objectId);
			output.WriteInt32(_lengthA[0]);
			return;
		case BinaryHeaderEnum.ArraySingleObject:
			output.WriteByte((byte)_binaryHeaderEnum);
			output.WriteInt32(_objectId);
			output.WriteInt32(_lengthA[0]);
			return;
		}
		output.WriteByte((byte)_binaryHeaderEnum);
		output.WriteInt32(_objectId);
		output.WriteByte((byte)_binaryArrayTypeEnum);
		output.WriteInt32(_rank);
		for (int i = 0; i < _rank; i++)
		{
			output.WriteInt32(_lengthA[i]);
		}
		if (_binaryArrayTypeEnum == BinaryArrayTypeEnum.SingleOffset || _binaryArrayTypeEnum == BinaryArrayTypeEnum.JaggedOffset || _binaryArrayTypeEnum == BinaryArrayTypeEnum.RectangularOffset)
		{
			for (int j = 0; j < _rank; j++)
			{
				output.WriteInt32(_lowerBoundA[j]);
			}
		}
		output.WriteByte((byte)_binaryTypeEnum);
		BinaryTypeConverter.WriteTypeInfo(_binaryTypeEnum, _typeInformation, _assemId, output);
	}

	public void Read(BinaryParser input)
	{
		switch (_binaryHeaderEnum)
		{
		case BinaryHeaderEnum.ArraySinglePrimitive:
			_objectId = input.ReadInt32();
			_lengthA = new int[1];
			_lengthA[0] = input.ReadInt32();
			_binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
			_rank = 1;
			_lowerBoundA = new int[_rank];
			_binaryTypeEnum = BinaryTypeEnum.Primitive;
			_typeInformation = (InternalPrimitiveTypeE)input.ReadByte();
			return;
		case BinaryHeaderEnum.ArraySingleString:
			_objectId = input.ReadInt32();
			_lengthA = new int[1];
			_lengthA[0] = input.ReadInt32();
			_binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
			_rank = 1;
			_lowerBoundA = new int[_rank];
			_binaryTypeEnum = BinaryTypeEnum.String;
			_typeInformation = null;
			return;
		case BinaryHeaderEnum.ArraySingleObject:
			_objectId = input.ReadInt32();
			_lengthA = new int[1];
			_lengthA[0] = input.ReadInt32();
			_binaryArrayTypeEnum = BinaryArrayTypeEnum.Single;
			_rank = 1;
			_lowerBoundA = new int[_rank];
			_binaryTypeEnum = BinaryTypeEnum.Object;
			_typeInformation = null;
			return;
		}
		_objectId = input.ReadInt32();
		_binaryArrayTypeEnum = (BinaryArrayTypeEnum)input.ReadByte();
		_rank = input.ReadInt32();
		_lengthA = new int[_rank];
		_lowerBoundA = new int[_rank];
		for (int i = 0; i < _rank; i++)
		{
			_lengthA[i] = input.ReadInt32();
		}
		if (_binaryArrayTypeEnum == BinaryArrayTypeEnum.SingleOffset || _binaryArrayTypeEnum == BinaryArrayTypeEnum.JaggedOffset || _binaryArrayTypeEnum == BinaryArrayTypeEnum.RectangularOffset)
		{
			for (int j = 0; j < _rank; j++)
			{
				_lowerBoundA[j] = input.ReadInt32();
			}
		}
		_binaryTypeEnum = (BinaryTypeEnum)input.ReadByte();
		_typeInformation = BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnum, input, out _assemId);
	}
}
