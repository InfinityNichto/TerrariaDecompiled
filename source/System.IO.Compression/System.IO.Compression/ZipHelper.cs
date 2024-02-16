namespace System.IO.Compression;

internal static class ZipHelper
{
	private static readonly DateTime s_invalidDateIndicator = new DateTime(1980, 1, 1, 0, 0, 0);

	internal static bool RequiresUnicode(string test)
	{
		foreach (char c in test)
		{
			if (c > '~' || c < ' ')
			{
				return true;
			}
		}
		return false;
	}

	internal static void ReadBytes(Stream stream, byte[] buffer, int bytesToRead)
	{
		int num = bytesToRead;
		int num2 = 0;
		while (num > 0)
		{
			int num3 = stream.Read(buffer, num2, num);
			if (num3 == 0)
			{
				throw new IOException(System.SR.UnexpectedEndOfStream);
			}
			num2 += num3;
			num -= num3;
		}
	}

	internal static DateTime DosTimeToDateTime(uint dateTime)
	{
		if (dateTime == 0)
		{
			return s_invalidDateIndicator;
		}
		int year = (int)(1980 + (dateTime >> 25));
		int month = (int)((dateTime >> 21) & 0xF);
		int day = (int)((dateTime >> 16) & 0x1F);
		int hour = (int)((dateTime >> 11) & 0x1F);
		int minute = (int)((dateTime >> 5) & 0x3F);
		int second = (int)((dateTime & 0x1F) * 2);
		try
		{
			return new DateTime(year, month, day, hour, minute, second, 0);
		}
		catch (ArgumentOutOfRangeException)
		{
			return s_invalidDateIndicator;
		}
		catch (ArgumentException)
		{
			return s_invalidDateIndicator;
		}
	}

	internal static uint DateTimeToDosTime(DateTime dateTime)
	{
		int num = (dateTime.Year - 1980) & 0x7F;
		num = (num << 4) + dateTime.Month;
		num = (num << 5) + dateTime.Day;
		num = (num << 5) + dateTime.Hour;
		num = (num << 6) + dateTime.Minute;
		return (uint)((num << 5) + dateTime.Second / 2);
	}

	internal static bool SeekBackwardsToSignature(Stream stream, uint signatureToFind, int maxBytesToRead)
	{
		int bufferPointer = 0;
		uint num = 0u;
		byte[] array = new byte[32];
		bool flag = false;
		bool flag2 = false;
		int num2 = 0;
		while (!flag2 && !flag && num2 <= maxBytesToRead)
		{
			flag = SeekBackwardsAndRead(stream, array, out bufferPointer);
			while (bufferPointer >= 0 && !flag2)
			{
				num = (num << 8) | array[bufferPointer];
				if (num == signatureToFind)
				{
					flag2 = true;
				}
				else
				{
					bufferPointer--;
				}
			}
			num2 += array.Length;
		}
		if (!flag2)
		{
			return false;
		}
		stream.Seek(bufferPointer, SeekOrigin.Current);
		return true;
	}

	internal static void AdvanceToPosition(this Stream stream, long position)
	{
		long num = position - stream.Position;
		while (num != 0L)
		{
			int count = (int)((num > 64) ? 64 : num);
			int num2 = stream.Read(new byte[64], 0, count);
			if (num2 == 0)
			{
				throw new IOException(System.SR.UnexpectedEndOfStream);
			}
			num -= num2;
		}
	}

	private static bool SeekBackwardsAndRead(Stream stream, byte[] buffer, out int bufferPointer)
	{
		if (stream.Position >= buffer.Length)
		{
			stream.Seek(-buffer.Length, SeekOrigin.Current);
			ReadBytes(stream, buffer, buffer.Length);
			stream.Seek(-buffer.Length, SeekOrigin.Current);
			bufferPointer = buffer.Length - 1;
			return false;
		}
		int num = (int)stream.Position;
		stream.Seek(0L, SeekOrigin.Begin);
		ReadBytes(stream, buffer, num);
		stream.Seek(0L, SeekOrigin.Begin);
		bufferPointer = num - 1;
		return true;
	}
}
