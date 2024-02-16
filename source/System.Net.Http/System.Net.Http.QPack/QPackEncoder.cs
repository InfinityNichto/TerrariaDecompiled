using System.Net.Http.HPack;
using System.Text;

namespace System.Net.Http.QPack;

internal static class QPackEncoder
{
	public static bool EncodeStaticIndexedHeaderField(int index, Span<byte> destination, out int bytesWritten)
	{
		if (!destination.IsEmpty)
		{
			destination[0] = 192;
			return IntegerEncoder.Encode(index, 6, destination, out bytesWritten);
		}
		bytesWritten = 0;
		return false;
	}

	public static byte[] EncodeStaticIndexedHeaderFieldToArray(int index)
	{
		Span<byte> destination = stackalloc byte[6];
		int bytesWritten;
		bool flag = EncodeStaticIndexedHeaderField(index, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	public static bool EncodeLiteralHeaderFieldWithStaticNameReference(int index, string value, Span<byte> destination, out int bytesWritten)
	{
		return EncodeLiteralHeaderFieldWithStaticNameReference(index, value, null, destination, out bytesWritten);
	}

	public static bool EncodeLiteralHeaderFieldWithStaticNameReference(int index, string value, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			destination[0] = 80;
			if (IntegerEncoder.Encode(index, 4, destination, out var bytesWritten2))
			{
				destination = destination.Slice(bytesWritten2);
				if (EncodeValueString(value, valueEncoding, destination, out var length))
				{
					bytesWritten = bytesWritten2 + length;
					return true;
				}
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static byte[] EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(int index)
	{
		Span<byte> destination = stackalloc byte[6];
		destination[0] = 112;
		int bytesWritten;
		bool flag = IntegerEncoder.Encode(index, 4, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	public static byte[] EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(int index, string value)
	{
		Span<byte> span = ((value.Length >= 256) ? ((Span<byte>)new byte[value.Length + 12]) : stackalloc byte[268]);
		Span<byte> destination = span;
		int bytesWritten;
		bool flag = EncodeLiteralHeaderFieldWithStaticNameReference(index, value, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, string value, Span<byte> destination, out int bytesWritten)
	{
		return EncodeLiteralHeaderFieldWithoutNameReference(name, value, null, destination, out bytesWritten);
	}

	public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, string value, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if (EncodeNameString(name, destination, out var length) && EncodeValueString(value, valueEncoding, destination.Slice(length), out var length2))
		{
			bytesWritten = length + length2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, ReadOnlySpan<string> values, string valueSeparator, Encoding valueEncoding, Span<byte> destination, out int bytesWritten)
	{
		if (EncodeNameString(name, destination, out var length) && EncodeValueString(values, valueSeparator, valueEncoding, destination.Slice(length), out var length2))
		{
			bytesWritten = length + length2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static byte[] EncodeLiteralHeaderFieldWithoutNameReferenceToArray(string name)
	{
		Span<byte> span = ((name.Length >= 256) ? ((Span<byte>)new byte[name.Length + 6]) : stackalloc byte[262]);
		Span<byte> buffer = span;
		int length;
		bool flag = EncodeNameString(name, buffer, out length);
		return buffer.Slice(0, length).ToArray();
	}

	public static byte[] EncodeLiteralHeaderFieldWithoutNameReferenceToArray(string name, string value)
	{
		Span<byte> span = ((name.Length + value.Length >= 256) ? ((Span<byte>)new byte[name.Length + value.Length + 12]) : stackalloc byte[268]);
		Span<byte> destination = span;
		int bytesWritten;
		bool flag = EncodeLiteralHeaderFieldWithoutNameReference(name, value, destination, out bytesWritten);
		return destination.Slice(0, bytesWritten).ToArray();
	}

	private static bool EncodeValueString(string s, Encoding valueEncoding, Span<byte> buffer, out int length)
	{
		if (buffer.Length != 0)
		{
			buffer[0] = 0;
			int num = ((valueEncoding == null || valueEncoding == Encoding.Latin1) ? s.Length : valueEncoding.GetByteCount(s));
			if (IntegerEncoder.Encode(num, 7, buffer, out var bytesWritten))
			{
				buffer = buffer.Slice(bytesWritten);
				if (buffer.Length >= num)
				{
					if (valueEncoding == null)
					{
						EncodeValueStringPart(s, buffer);
					}
					else
					{
						int bytes = valueEncoding.GetBytes(s, buffer);
					}
					length = bytesWritten + num;
					return true;
				}
			}
		}
		length = 0;
		return false;
	}

	public static bool EncodeValueString(ReadOnlySpan<string> values, string separator, Encoding valueEncoding, Span<byte> buffer, out int length)
	{
		if (values.Length == 1)
		{
			return EncodeValueString(values[0], valueEncoding, buffer, out length);
		}
		if (values.Length == 0)
		{
			return EncodeValueString(string.Empty, null, buffer, out length);
		}
		if (buffer.Length > 0)
		{
			int num;
			if (valueEncoding == null || valueEncoding == Encoding.Latin1)
			{
				num = separator.Length * (values.Length - 1);
				ReadOnlySpan<string> readOnlySpan = values;
				for (int i = 0; i < readOnlySpan.Length; i++)
				{
					string text = readOnlySpan[i];
					num += text.Length;
				}
			}
			else
			{
				num = valueEncoding.GetByteCount(separator) * (values.Length - 1);
				ReadOnlySpan<string> readOnlySpan2 = values;
				for (int j = 0; j < readOnlySpan2.Length; j++)
				{
					string s = readOnlySpan2[j];
					num += valueEncoding.GetByteCount(s);
				}
			}
			buffer[0] = 0;
			if (IntegerEncoder.Encode(num, 7, buffer, out var bytesWritten))
			{
				buffer = buffer.Slice(bytesWritten);
				if (buffer.Length >= num)
				{
					if (valueEncoding == null)
					{
						string text2 = values[0];
						EncodeValueStringPart(text2, buffer);
						buffer = buffer.Slice(text2.Length);
						for (int k = 1; k < values.Length; k++)
						{
							EncodeValueStringPart(separator, buffer);
							buffer = buffer.Slice(separator.Length);
							text2 = values[k];
							EncodeValueStringPart(text2, buffer);
							buffer = buffer.Slice(text2.Length);
						}
					}
					else
					{
						int bytes = valueEncoding.GetBytes(values[0], buffer);
						buffer = buffer.Slice(bytes);
						for (int l = 1; l < values.Length; l++)
						{
							bytes = valueEncoding.GetBytes(separator, buffer);
							buffer = buffer.Slice(bytes);
							bytes = valueEncoding.GetBytes(values[l], buffer);
							buffer = buffer.Slice(bytes);
						}
					}
					length = bytesWritten + num;
					return true;
				}
			}
		}
		length = 0;
		return false;
	}

	private static void EncodeValueStringPart(string s, Span<byte> buffer)
	{
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c > '\u007f')
			{
				throw new QPackEncodingException(System.SR.net_http_request_invalid_char_encoding);
			}
			buffer[i] = (byte)c;
		}
	}

	private static bool EncodeNameString(string s, Span<byte> buffer, out int length)
	{
		if (buffer.Length != 0)
		{
			buffer[0] = 48;
			if (IntegerEncoder.Encode(s.Length, 3, buffer, out var bytesWritten))
			{
				buffer = buffer.Slice(bytesWritten);
				if (buffer.Length >= s.Length)
				{
					for (int i = 0; i < s.Length; i++)
					{
						int num = s[i];
						if ((uint)(num - 65) <= 25u)
						{
							num |= 0x20;
						}
						buffer[i] = (byte)num;
					}
					length = bytesWritten + s.Length;
					return true;
				}
			}
		}
		length = 0;
		return false;
	}
}
