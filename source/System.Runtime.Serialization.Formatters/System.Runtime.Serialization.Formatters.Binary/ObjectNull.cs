namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ObjectNull : IStreamable
{
	internal int _nullCount;

	internal ObjectNull()
	{
	}

	internal void SetNullCount(int nullCount)
	{
		_nullCount = nullCount;
	}

	public void Write(BinaryFormatterWriter output)
	{
		if (_nullCount == 1)
		{
			output.WriteByte(10);
		}
		else if (_nullCount < 256)
		{
			output.WriteByte(13);
			output.WriteByte((byte)_nullCount);
		}
		else
		{
			output.WriteByte(14);
			output.WriteInt32(_nullCount);
		}
	}

	public void Read(BinaryParser input, BinaryHeaderEnum binaryHeaderEnum)
	{
		switch (binaryHeaderEnum)
		{
		case BinaryHeaderEnum.ObjectNull:
			_nullCount = 1;
			break;
		case BinaryHeaderEnum.ObjectNullMultiple256:
			_nullCount = input.ReadByte();
			break;
		case BinaryHeaderEnum.ObjectNullMultiple:
			_nullCount = input.ReadInt32();
			break;
		case BinaryHeaderEnum.MessageEnd:
		case BinaryHeaderEnum.Assembly:
			break;
		}
	}
}
