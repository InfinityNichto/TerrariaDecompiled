using System.Text;

namespace System.Net.Http.HPack;

internal static class HPackEncoder
{
	public static bool EncodeIndexedHeaderField(int index, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length != 0)
		{
			destination[0] = 128;
			return IntegerEncoder.Encode(index, 7, destination, out bytesWritten);
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeLiteralHeaderFieldWithoutIndexing(int index, string value, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if ((uint)destination.Length >= 2u)
		{
			destination[0] = 0;
			if (IntegerEncoder.Encode(index, 4, destination, out var bytesWritten2) && EncodeStringLiteral(value, valueEncoding, destination.Slice(bytesWritten2), out var bytesWritten3))
			{
				bytesWritten = bytesWritten2 + bytesWritten3;
				return true;
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeLiteralHeaderFieldWithoutIndexing(int index, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length != 0)
		{
			destination[0] = 0;
			if (IntegerEncoder.Encode(index, 4, destination, out var bytesWritten2))
			{
				bytesWritten = bytesWritten2;
				return true;
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, ReadOnlySpan<string> values, string separator, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if ((uint)destination.Length >= 3u)
		{
			destination[0] = 0;
			if (EncodeLiteralHeaderName(name, destination.Slice(1), out var bytesWritten2) && EncodeStringLiterals(values, separator, valueEncoding, destination.Slice(1 + bytesWritten2), out var bytesWritten3))
			{
				bytesWritten = 1 + bytesWritten2 + bytesWritten3;
				return true;
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, Span<byte> destination, out int bytesWritten)
	{
		if ((uint)destination.Length >= 2u)
		{
			destination[0] = 0;
			if (EncodeLiteralHeaderName(name, destination.Slice(1), out var bytesWritten2))
			{
				bytesWritten = 1 + bytesWritten2;
				return true;
			}
		}
		bytesWritten = 0;
		return false;
	}

	private static bool EncodeLiteralHeaderName(string value, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length != 0)
		{
			destination[0] = 0;
			if (IntegerEncoder.Encode(value.Length, 7, destination, out var bytesWritten2))
			{
				destination = destination.Slice(bytesWritten2);
				if (value.Length <= destination.Length)
				{
					for (int i = 0; i < value.Length; i++)
					{
						char c = value[i];
						destination[i] = (byte)(((uint)(c - 65) <= 25u) ? (c | 0x20u) : c);
					}
					bytesWritten = bytesWritten2 + value.Length;
					return true;
				}
			}
		}
		bytesWritten = 0;
		return false;
	}

	private static void EncodeValueStringPart(string value, Span<byte> destination)
	{
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if ((c & 0xFF80u) != 0)
			{
				throw new HttpRequestException(System.SR.net_http_request_invalid_char_encoding);
			}
			destination[i] = (byte)c;
		}
	}

	public static bool EncodeStringLiteral(string value, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length != 0)
		{
			destination[0] = 0;
			int num = ((valueEncoding == null || valueEncoding == Encoding.Latin1) ? value.Length : valueEncoding.GetByteCount(value));
			if (IntegerEncoder.Encode(num, 7, destination, out var bytesWritten2))
			{
				destination = destination.Slice(bytesWritten2);
				if (num <= destination.Length)
				{
					if (valueEncoding == null)
					{
						EncodeValueStringPart(value, destination);
					}
					else
					{
						int bytes = valueEncoding.GetBytes(value, destination);
					}
					bytesWritten = bytesWritten2 + num;
					return true;
				}
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeStringLiterals(ReadOnlySpan<string> values, string separator, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		bytesWritten = 0;
		if (values.Length == 0)
		{
			return EncodeStringLiteral("", null, destination, out bytesWritten);
		}
		if (values.Length == 1)
		{
			return EncodeStringLiteral(values[0], valueEncoding, destination, out bytesWritten);
		}
		if (destination.Length != 0)
		{
			int num;
			checked
			{
				if (valueEncoding == null || valueEncoding == Encoding.Latin1)
				{
					num = (values.Length - 1) * separator.Length;
					ReadOnlySpan<string> readOnlySpan = values;
					for (int i = 0; i < readOnlySpan.Length; i = unchecked(i + 1))
					{
						string text = readOnlySpan[i];
						num += text.Length;
					}
				}
				else
				{
					num = (values.Length - 1) * valueEncoding.GetByteCount(separator);
					ReadOnlySpan<string> readOnlySpan2 = values;
					for (int j = 0; j < readOnlySpan2.Length; j = unchecked(j + 1))
					{
						string s = readOnlySpan2[j];
						num += valueEncoding.GetByteCount(s);
					}
				}
				destination[0] = 0;
			}
			if (IntegerEncoder.Encode(num, 7, destination, out var bytesWritten2))
			{
				destination = destination.Slice(bytesWritten2);
				if (destination.Length >= num)
				{
					if (valueEncoding == null)
					{
						string text2 = values[0];
						EncodeValueStringPart(text2, destination);
						destination = destination.Slice(text2.Length);
						for (int k = 1; k < values.Length; k++)
						{
							EncodeValueStringPart(separator, destination);
							destination = destination.Slice(separator.Length);
							text2 = values[k];
							EncodeValueStringPart(text2, destination);
							destination = destination.Slice(text2.Length);
						}
					}
					else
					{
						int bytes = valueEncoding.GetBytes(values[0], destination);
						destination = destination.Slice(bytes);
						for (int l = 1; l < values.Length; l++)
						{
							bytes = valueEncoding.GetBytes(separator, destination);
							destination = destination.Slice(bytes);
							bytes = valueEncoding.GetBytes(values[l], destination);
							destination = destination.Slice(bytes);
						}
					}
					bytesWritten = bytesWritten2 + num;
					return true;
				}
			}
		}
		return false;
	}

	public static byte[] EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(int index)
	{
		Span<byte> destination = stackalloc byte[256];
		int bytesWritten;
		bool flag = EncodeLiteralHeaderFieldWithoutIndexing(index, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	public static byte[] EncodeLiteralHeaderFieldWithoutIndexingNewNameToAllocatedArray(string name)
	{
		Span<byte> destination = stackalloc byte[256];
		int bytesWritten;
		bool flag = EncodeLiteralHeaderFieldWithoutIndexingNewName(name, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	public static byte[] EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(int index, string value)
	{
		Span<byte> destination = stackalloc byte[512];
		int bytesWritten;
		while (!EncodeLiteralHeaderFieldWithoutIndexing(index, value, null, destination, out bytesWritten))
		{
			destination = new byte[destination.Length * 2];
		}
		return destination.Slice(0, bytesWritten).ToArray();
	}
}
