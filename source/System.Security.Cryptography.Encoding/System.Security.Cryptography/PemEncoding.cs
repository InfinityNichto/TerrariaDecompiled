using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

public static class PemEncoding
{
	public static PemFields Find(ReadOnlySpan<char> pemData)
	{
		if (!TryFind(pemData, out var fields))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_NoPemFound, "pemData");
		}
		return fields;
	}

	public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
	{
		if (pemData.Length < "-----BEGIN ".Length + "-----".Length * 2 + "-----END ".Length)
		{
			fields = default(PemFields);
			return false;
		}
		Span<char> span = stackalloc char[256];
		int num = 0;
		int num2;
		while ((num2 = pemData.IndexOfByOffset("-----BEGIN ", num)) >= 0)
		{
			int num3 = num2 + "-----BEGIN ".Length;
			if (num2 > 0 && !IsWhiteSpaceCharacter(pemData[num2 - 1]))
			{
				num = num3;
				continue;
			}
			int num4 = pemData.IndexOfByOffset("-----", num3);
			if (num4 < 0)
			{
				fields = default(PemFields);
				return false;
			}
			Range range = num3..num4;
			ReadOnlySpan<char> readOnlySpan = pemData;
			ReadOnlySpan<char> readOnlySpan2 = readOnlySpan[range];
			if (IsValidLabel(readOnlySpan2))
			{
				int num5 = num4 + "-----".Length;
				int num6 = "-----END ".Length + readOnlySpan2.Length + "-----".Length;
				Span<char> destination2 = ((num6 > 256) ? ((Span<char>)new char[num6]) : span);
				ReadOnlySpan<char> value = WritePostEB(readOnlySpan2, destination2);
				int num7 = pemData.IndexOfByOffset(value, num5);
				if (num7 >= 0)
				{
					int num8 = num7 + num6;
					if (num8 >= pemData.Length - 1 || IsWhiteSpaceCharacter(pemData[num8]))
					{
						Range range2 = num5..num7;
						readOnlySpan = pemData;
						if (TryCountBase64(readOnlySpan[range2], out var base64Start, out var base64End, out var base64DecodedSize))
						{
							Range location = num2..num8;
							Range base64data = (num5 + base64Start)..(num5 + base64End);
							fields = new PemFields(range, base64data, location, base64DecodedSize);
							return true;
						}
					}
				}
			}
			if (num4 <= num)
			{
				fields = default(PemFields);
				return false;
			}
			num = num4;
		}
		fields = default(PemFields);
		return false;
		static ReadOnlySpan<char> WritePostEB(ReadOnlySpan<char> label, Span<char> destination)
		{
			int length = "-----END ".Length + label.Length + "-----".Length;
			"-----END ".CopyTo(destination);
			label.CopyTo(destination.Slice("-----END ".Length));
			"-----".CopyTo(destination.Slice("-----END ".Length + label.Length));
			return destination.Slice(0, length);
		}
	}

	private static int IndexOfByOffset(this ReadOnlySpan<char> str, ReadOnlySpan<char> value, int startPosition)
	{
		int num = str.Slice(startPosition).IndexOf(value);
		if (num != -1)
		{
			return num + startPosition;
		}
		return -1;
	}

	private static bool IsValidLabel(ReadOnlySpan<char> data)
	{
		if (data.IsEmpty)
		{
			return true;
		}
		bool flag = false;
		for (int i = 0; i < data.Length; i++)
		{
			char c2 = data[i];
			if (IsLabelChar(c2))
			{
				flag = true;
				continue;
			}
			if ((c2 != ' ' && c2 != '-') || !flag)
			{
				return false;
			}
			flag = false;
		}
		return flag;
		static bool IsLabelChar(char c)
		{
			if ((uint)(c - 33) <= 93u)
			{
				return c != '-';
			}
			return false;
		}
	}

	private static bool TryCountBase64(ReadOnlySpan<char> str, out int base64Start, out int base64End, out int base64DecodedSize)
	{
		base64Start = 0;
		base64End = str.Length;
		if (str.IsEmpty)
		{
			base64DecodedSize = 0;
			return true;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < str.Length; i++)
		{
			char c = str[i];
			if (IsWhiteSpaceCharacter(c))
			{
				if (num == 0)
				{
					base64Start++;
				}
				else
				{
					base64End--;
				}
				continue;
			}
			base64End = str.Length;
			if (c == '=')
			{
				num2++;
				continue;
			}
			if (num2 == 0 && IsBase64Character(c))
			{
				num++;
				continue;
			}
			base64DecodedSize = 0;
			return false;
		}
		int num3 = num2 + num;
		if (num2 > 2 || ((uint)num3 & 3u) != 0)
		{
			base64DecodedSize = 0;
			return false;
		}
		base64DecodedSize = (num3 >> 2) * 3 - num2;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsBase64Character(char ch)
	{
		if (ch != '+' && ch != '/' && (uint)(ch - 48) >= 10u && (uint)(ch - 65) >= 26u)
		{
			return (uint)(ch - 97) < 26u;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsWhiteSpaceCharacter(char ch)
	{
		if (ch != ' ' && ch != '\t' && ch != '\n')
		{
			return ch == '\r';
		}
		return true;
	}

	public static int GetEncodedSize(int labelLength, int dataLength)
	{
		if (labelLength < 0)
		{
			throw new ArgumentOutOfRangeException("labelLength", System.SR.ArgumentOutOfRange_NeedPositiveNumber);
		}
		if (dataLength < 0)
		{
			throw new ArgumentOutOfRangeException("dataLength", System.SR.ArgumentOutOfRange_NeedPositiveNumber);
		}
		if (labelLength > 1073741808)
		{
			throw new ArgumentOutOfRangeException("labelLength", System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		if (dataLength > 1585834053)
		{
			throw new ArgumentOutOfRangeException("dataLength", System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		int num = "-----BEGIN ".Length + labelLength + "-----".Length;
		int num2 = "-----END ".Length + labelLength + "-----".Length;
		int num3 = num + num2 + 1;
		int num4 = (dataLength + 2) / 3 << 2;
		int result;
		int num5 = Math.DivRem(num4, 64, out result);
		if (result > 0)
		{
			num5++;
		}
		int num6 = num4 + num5;
		if (int.MaxValue - num6 < num3)
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_EncodedSizeTooLarge);
		}
		return num6 + num3;
	}

	public static bool TryWrite(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten)
	{
		if (!IsValidLabel(label))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_InvalidLabel, "label");
		}
		int encodedSize = GetEncodedSize(label.Length, data.Length);
		if (destination.Length < encodedSize)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 0;
		charsWritten += Write("-----BEGIN ", destination, charsWritten);
		charsWritten += Write(label, destination, charsWritten);
		charsWritten += Write("-----", destination, charsWritten);
		charsWritten += Write("\n", destination, charsWritten);
		ReadOnlySpan<byte> bytes2 = data;
		while (bytes2.Length >= 48)
		{
			charsWritten += WriteBase64(bytes2.Slice(0, 48), destination, charsWritten);
			charsWritten += Write("\n", destination, charsWritten);
			bytes2 = bytes2.Slice(48);
		}
		if (bytes2.Length > 0)
		{
			charsWritten += WriteBase64(bytes2, destination, charsWritten);
			charsWritten += Write("\n", destination, charsWritten);
			bytes2 = default(ReadOnlySpan<byte>);
		}
		charsWritten += Write("-----END ", destination, charsWritten);
		charsWritten += Write(label, destination, charsWritten);
		charsWritten += Write("-----", destination, charsWritten);
		return true;
		static int Write(ReadOnlySpan<char> str, Span<char> dest, int offset)
		{
			str.CopyTo(dest.Slice(offset));
			return str.Length;
		}
		static int WriteBase64(ReadOnlySpan<byte> bytes, Span<char> dest, int offset)
		{
			if (!Convert.TryToBase64Chars(bytes, dest.Slice(offset), out var charsWritten2))
			{
				throw new ArgumentException(null, "destination");
			}
			return charsWritten2;
		}
	}

	public static char[] Write(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
	{
		if (!IsValidLabel(label))
		{
			throw new ArgumentException(System.SR.Argument_PemEncoding_InvalidLabel, "label");
		}
		int encodedSize = GetEncodedSize(label.Length, data.Length);
		char[] array = new char[encodedSize];
		if (!TryWrite(label, data, array, out var _))
		{
			throw new ArgumentException(null, "data");
		}
		return array;
	}
}
