using System.Buffers.Binary;
using System.IO;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class SerializationHeaderRecord : IStreamable
{
	internal BinaryHeaderEnum _binaryHeaderEnum;

	internal int _topId;

	internal int _headerId;

	internal int _majorVersion;

	internal int _minorVersion;

	internal SerializationHeaderRecord()
	{
	}

	internal SerializationHeaderRecord(BinaryHeaderEnum binaryHeaderEnum, int topId, int headerId, int majorVersion, int minorVersion)
	{
		_binaryHeaderEnum = binaryHeaderEnum;
		_topId = topId;
		_headerId = headerId;
		_majorVersion = majorVersion;
		_minorVersion = minorVersion;
	}

	public void Write(BinaryFormatterWriter output)
	{
		_majorVersion = 1;
		_minorVersion = 0;
		output.WriteByte((byte)_binaryHeaderEnum);
		output.WriteInt32(_topId);
		output.WriteInt32(_headerId);
		output.WriteInt32(1);
		output.WriteInt32(0);
	}

	private static int GetInt32(byte[] buffer, int index)
	{
		return BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(index));
	}

	public void Read(BinaryParser input)
	{
		byte[] array = input.ReadBytes(17);
		if (array.Length < 17)
		{
			throw new EndOfStreamException(System.SR.IO_EOF_ReadBeyondEOF);
		}
		_majorVersion = GetInt32(array, 9);
		if (_majorVersion > 1)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_InvalidFormat, BitConverter.ToString(array)));
		}
		_binaryHeaderEnum = (BinaryHeaderEnum)array[0];
		_topId = GetInt32(array, 1);
		_headerId = GetInt32(array, 5);
		_minorVersion = GetInt32(array, 13);
	}
}
