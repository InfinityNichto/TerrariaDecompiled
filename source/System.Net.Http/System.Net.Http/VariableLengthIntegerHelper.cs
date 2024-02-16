using System.Buffers.Binary;

namespace System.Net.Http;

internal static class VariableLengthIntegerHelper
{
	public static bool TryRead(ReadOnlySpan<byte> buffer, out long value, out int bytesRead)
	{
		if (buffer.Length != 0)
		{
			byte b = buffer[0];
			switch (b & 0xC0)
			{
			case 0:
				value = b;
				bytesRead = 1;
				return true;
			case 64:
			{
				if (BinaryPrimitives.TryReadUInt16BigEndian(buffer, out var value3))
				{
					value = (uint)(value3 - 16384);
					bytesRead = 2;
					return true;
				}
				break;
			}
			case 128:
			{
				if (BinaryPrimitives.TryReadUInt32BigEndian(buffer, out var value4))
				{
					value = (uint)((int)value4 - int.MinValue);
					bytesRead = 4;
					return true;
				}
				break;
			}
			default:
			{
				if (BinaryPrimitives.TryReadUInt64BigEndian(buffer, out var value2))
				{
					value = (long)value2 - -4611686018427387904L;
					bytesRead = 8;
					return true;
				}
				break;
			}
			}
		}
		value = 0L;
		bytesRead = 0;
		return false;
	}

	public static bool TryWrite(Span<byte> buffer, long longToEncode, out int bytesWritten)
	{
		if (longToEncode < 63)
		{
			if (buffer.Length != 0)
			{
				buffer[0] = (byte)longToEncode;
				bytesWritten = 1;
				return true;
			}
		}
		else if (longToEncode < 16383)
		{
			if (BinaryPrimitives.TryWriteUInt16BigEndian(buffer, (ushort)((uint)(int)longToEncode | 0x4000u)))
			{
				bytesWritten = 2;
				return true;
			}
		}
		else if (longToEncode < 1073741823)
		{
			if (BinaryPrimitives.TryWriteUInt32BigEndian(buffer, (uint)(int)longToEncode | 0x80000000u))
			{
				bytesWritten = 4;
				return true;
			}
		}
		else if (BinaryPrimitives.TryWriteUInt64BigEndian(buffer, (ulong)longToEncode | 0xC000000000000000uL))
		{
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static int WriteInteger(Span<byte> buffer, long longToEncode)
	{
		int bytesWritten;
		bool flag = TryWrite(buffer, longToEncode, out bytesWritten);
		return bytesWritten;
	}

	public static int GetByteCount(long value)
	{
		if (value >= 63)
		{
			if (value >= 16383)
			{
				if (value >= 1073741823)
				{
					return 8;
				}
				return 4;
			}
			return 2;
		}
		return 1;
	}
}
