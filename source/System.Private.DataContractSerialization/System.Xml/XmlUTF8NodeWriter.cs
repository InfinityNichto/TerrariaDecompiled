using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal class XmlUTF8NodeWriter : XmlStreamNodeWriter
{
	private byte[] _entityChars;

	private readonly bool[] _isEscapedAttributeChar;

	private readonly bool[] _isEscapedElementChar;

	private bool _inAttribute;

	private const int bufferLength = 512;

	private const int maxEntityLength = 32;

	private Encoding _encoding;

	private char[] _chars;

	private static readonly byte[] s_startDecl = new byte[30]
	{
		60, 63, 120, 109, 108, 32, 118, 101, 114, 115,
		105, 111, 110, 61, 34, 49, 46, 48, 34, 32,
		101, 110, 99, 111, 100, 105, 110, 103, 61, 34
	};

	private static readonly byte[] s_endDecl = new byte[3] { 34, 63, 62 };

	private static readonly byte[] s_utf8Decl = new byte[38]
	{
		60, 63, 120, 109, 108, 32, 118, 101, 114, 115,
		105, 111, 110, 61, 34, 49, 46, 48, 34, 32,
		101, 110, 99, 111, 100, 105, 110, 103, 61, 34,
		117, 116, 102, 45, 56, 34, 63, 62
	};

	private static readonly byte[] s_digits = new byte[16]
	{
		48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
		65, 66, 67, 68, 69, 70
	};

	private static readonly bool[] s_defaultIsEscapedAttributeChar = new bool[64]
	{
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, false, false, true, false, false, false, true, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		true, false, true, false
	};

	private static readonly bool[] s_defaultIsEscapedElementChar = new bool[64]
	{
		true, true, true, true, true, true, true, true, true, false,
		false, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, false, false, false, false, false, false, true, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		true, false, true, false
	};

	public XmlUTF8NodeWriter()
		: this(s_defaultIsEscapedAttributeChar, s_defaultIsEscapedElementChar)
	{
	}

	public XmlUTF8NodeWriter(bool[] isEscapedAttributeChar, bool[] isEscapedElementChar)
	{
		_isEscapedAttributeChar = isEscapedAttributeChar;
		_isEscapedElementChar = isEscapedElementChar;
		_inAttribute = false;
	}

	public new void SetOutput(Stream stream, bool ownsStream, Encoding encoding)
	{
		Encoding encoding2 = null;
		if (encoding != null && encoding.CodePage == Encoding.UTF8.CodePage)
		{
			encoding2 = encoding;
			encoding = null;
		}
		base.SetOutput(stream, ownsStream, encoding2);
		_encoding = encoding;
		_inAttribute = false;
	}

	private byte[] GetCharEntityBuffer()
	{
		if (_entityChars == null)
		{
			_entityChars = new byte[32];
		}
		return _entityChars;
	}

	private char[] GetCharBuffer(int charCount)
	{
		if (charCount >= 256)
		{
			return new char[charCount];
		}
		if (_chars == null || _chars.Length < charCount)
		{
			_chars = new char[charCount];
		}
		return _chars;
	}

	public override void WriteDeclaration()
	{
		if (_encoding == null)
		{
			WriteUTF8Chars(s_utf8Decl, 0, s_utf8Decl.Length);
			return;
		}
		WriteUTF8Chars(s_startDecl, 0, s_startDecl.Length);
		if (_encoding.WebName == Encoding.BigEndianUnicode.WebName)
		{
			WriteUTF8Chars("utf-16BE");
		}
		else
		{
			WriteUTF8Chars(_encoding.WebName);
		}
		WriteUTF8Chars(s_endDecl, 0, s_endDecl.Length);
	}

	public override void WriteCData(string text)
	{
		byte[] buffer = GetBuffer(9, out var offset);
		buffer[offset] = 60;
		buffer[offset + 1] = 33;
		buffer[offset + 2] = 91;
		buffer[offset + 3] = 67;
		buffer[offset + 4] = 68;
		buffer[offset + 5] = 65;
		buffer[offset + 6] = 84;
		buffer[offset + 7] = 65;
		buffer[offset + 8] = 91;
		Advance(9);
		WriteUTF8Chars(text);
		buffer = GetBuffer(3, out offset);
		buffer[offset] = 93;
		buffer[offset + 1] = 93;
		buffer[offset + 2] = 62;
		Advance(3);
	}

	private void WriteStartComment()
	{
		int offset;
		byte[] buffer = GetBuffer(4, out offset);
		buffer[offset] = 60;
		buffer[offset + 1] = 33;
		buffer[offset + 2] = 45;
		buffer[offset + 3] = 45;
		Advance(4);
	}

	private void WriteEndComment()
	{
		int offset;
		byte[] buffer = GetBuffer(3, out offset);
		buffer[offset] = 45;
		buffer[offset + 1] = 45;
		buffer[offset + 2] = 62;
		Advance(3);
	}

	public override void WriteComment(string text)
	{
		WriteStartComment();
		WriteUTF8Chars(text);
		WriteEndComment();
	}

	public override void WriteStartElement(string prefix, string localName)
	{
		WriteByte('<');
		if (!string.IsNullOrEmpty(prefix))
		{
			WritePrefix(prefix);
			WriteByte(':');
		}
		WriteLocalName(localName);
	}

	public override async Task WriteStartElementAsync(string prefix, string localName)
	{
		await WriteByteAsync('<').ConfigureAwait(continueOnCapturedContext: false);
		if (!string.IsNullOrEmpty(prefix))
		{
			WritePrefix(prefix);
			await WriteByteAsync(':').ConfigureAwait(continueOnCapturedContext: false);
		}
		WriteLocalName(localName);
	}

	public override void WriteStartElement(string prefix, XmlDictionaryString localName)
	{
		WriteStartElement(prefix, localName.Value);
	}

	public override void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
	{
		WriteByte('<');
		if (prefixLength != 0)
		{
			WritePrefix(prefixBuffer, prefixOffset, prefixLength);
			WriteByte(':');
		}
		WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
	}

	public override void WriteEndStartElement(bool isEmpty)
	{
		if (!isEmpty)
		{
			WriteByte('>');
		}
		else
		{
			WriteBytes('/', '>');
		}
	}

	public override async Task WriteEndStartElementAsync(bool isEmpty)
	{
		if (!isEmpty)
		{
			await WriteByteAsync('>').ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await WriteBytesAsync('/', '>').ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public override void WriteEndElement(string prefix, string localName)
	{
		WriteBytes('<', '/');
		if (!string.IsNullOrEmpty(prefix))
		{
			WritePrefix(prefix);
			WriteByte(':');
		}
		WriteLocalName(localName);
		WriteByte('>');
	}

	public override async Task WriteEndElementAsync(string prefix, string localName)
	{
		await WriteBytesAsync('<', '/').ConfigureAwait(continueOnCapturedContext: false);
		if (!string.IsNullOrEmpty(prefix))
		{
			WritePrefix(prefix);
			await WriteByteAsync(':').ConfigureAwait(continueOnCapturedContext: false);
		}
		WriteLocalName(localName);
		await WriteByteAsync('>').ConfigureAwait(continueOnCapturedContext: false);
	}

	public override void WriteEndElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
	{
		WriteBytes('<', '/');
		if (prefixLength != 0)
		{
			WritePrefix(prefixBuffer, prefixOffset, prefixLength);
			WriteByte(':');
		}
		WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
		WriteByte('>');
	}

	private void WriteStartXmlnsAttribute()
	{
		int offset;
		byte[] buffer = GetBuffer(6, out offset);
		buffer[offset] = 32;
		buffer[offset + 1] = 120;
		buffer[offset + 2] = 109;
		buffer[offset + 3] = 108;
		buffer[offset + 4] = 110;
		buffer[offset + 5] = 115;
		Advance(6);
		_inAttribute = true;
	}

	public override void WriteXmlnsAttribute(string prefix, string ns)
	{
		WriteStartXmlnsAttribute();
		if (!string.IsNullOrEmpty(prefix))
		{
			WriteByte(':');
			WritePrefix(prefix);
		}
		WriteBytes('=', '"');
		WriteEscapedText(ns);
		WriteEndAttribute();
	}

	public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
	{
		WriteXmlnsAttribute(prefix, ns.Value);
	}

	public override void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength)
	{
		WriteStartXmlnsAttribute();
		if (prefixLength != 0)
		{
			WriteByte(':');
			WritePrefix(prefixBuffer, prefixOffset, prefixLength);
		}
		WriteBytes('=', '"');
		WriteEscapedText(nsBuffer, nsOffset, nsLength);
		WriteEndAttribute();
	}

	public override void WriteStartAttribute(string prefix, string localName)
	{
		WriteByte(' ');
		if (prefix.Length != 0)
		{
			WritePrefix(prefix);
			WriteByte(':');
		}
		WriteLocalName(localName);
		WriteBytes('=', '"');
		_inAttribute = true;
	}

	public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
	{
		WriteStartAttribute(prefix, localName.Value);
	}

	public override void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
	{
		WriteByte(' ');
		if (prefixLength != 0)
		{
			WritePrefix(prefixBuffer, prefixOffset, prefixLength);
			WriteByte(':');
		}
		WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
		WriteBytes('=', '"');
		_inAttribute = true;
	}

	public override void WriteEndAttribute()
	{
		WriteByte('"');
		_inAttribute = false;
	}

	public override async Task WriteEndAttributeAsync()
	{
		await WriteByteAsync('"').ConfigureAwait(continueOnCapturedContext: false);
		_inAttribute = false;
	}

	private void WritePrefix(string prefix)
	{
		if (prefix.Length == 1)
		{
			WriteUTF8Char(prefix[0]);
		}
		else
		{
			WriteUTF8Chars(prefix);
		}
	}

	private void WritePrefix(byte[] prefixBuffer, int prefixOffset, int prefixLength)
	{
		if (prefixLength == 1)
		{
			WriteUTF8Char(prefixBuffer[prefixOffset]);
		}
		else
		{
			WriteUTF8Chars(prefixBuffer, prefixOffset, prefixLength);
		}
	}

	private void WriteLocalName(string localName)
	{
		WriteUTF8Chars(localName);
	}

	private void WriteLocalName(byte[] localNameBuffer, int localNameOffset, int localNameLength)
	{
		WriteUTF8Chars(localNameBuffer, localNameOffset, localNameLength);
	}

	public override void WriteEscapedText(XmlDictionaryString s)
	{
		WriteEscapedText(s.Value);
	}

	public unsafe override void WriteEscapedText(string s)
	{
		int length = s.Length;
		if (length > 0)
		{
			fixed (char* chars = s)
			{
				UnsafeWriteEscapedText(chars, length);
			}
		}
	}

	public unsafe override void WriteEscapedText(char[] s, int offset, int count)
	{
		if (count > 0)
		{
			fixed (char* chars = &s[offset])
			{
				UnsafeWriteEscapedText(chars, count);
			}
		}
	}

	private unsafe void UnsafeWriteEscapedText(char* chars, int count)
	{
		bool[] array = (_inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar);
		int num = array.Length;
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			char c = chars[i];
			if ((c < num && array[(uint)c]) || c >= '\ufffe')
			{
				UnsafeWriteUTF8Chars(chars + num2, i - num2);
				WriteCharEntity(c);
				num2 = i + 1;
			}
		}
		UnsafeWriteUTF8Chars(chars + num2, count - num2);
	}

	public override void WriteEscapedText(byte[] chars, int offset, int count)
	{
		bool[] array = (_inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar);
		int num = array.Length;
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			byte b = chars[offset + i];
			if (b < num && array[b])
			{
				WriteUTF8Chars(chars, offset + num2, i - num2);
				WriteCharEntity(b);
				num2 = i + 1;
			}
			else if (b == 239 && offset + i + 2 < count)
			{
				byte b2 = chars[offset + i + 1];
				byte b3 = chars[offset + i + 2];
				if (b2 == 191 && (b3 == 190 || b3 == 191))
				{
					WriteUTF8Chars(chars, offset + num2, i - num2);
					WriteCharEntity((b3 == 190) ? 65534 : 65535);
					num2 = i + 3;
				}
			}
		}
		WriteUTF8Chars(chars, offset + num2, count - num2);
	}

	public void WriteText(int ch)
	{
		WriteUTF8Char(ch);
	}

	public override void WriteText(byte[] chars, int offset, int count)
	{
		WriteUTF8Chars(chars, offset, count);
	}

	public unsafe override void WriteText(char[] chars, int offset, int count)
	{
		if (count > 0)
		{
			fixed (char* chars2 = &chars[offset])
			{
				UnsafeWriteUTF8Chars(chars2, count);
			}
		}
	}

	public override void WriteText(string value)
	{
		WriteUTF8Chars(value);
	}

	public override void WriteText(XmlDictionaryString value)
	{
		WriteUTF8Chars(value.Value);
	}

	public void WriteLessThanCharEntity()
	{
		int offset;
		byte[] buffer = GetBuffer(4, out offset);
		buffer[offset] = 38;
		buffer[offset + 1] = 108;
		buffer[offset + 2] = 116;
		buffer[offset + 3] = 59;
		Advance(4);
	}

	public void WriteGreaterThanCharEntity()
	{
		int offset;
		byte[] buffer = GetBuffer(4, out offset);
		buffer[offset] = 38;
		buffer[offset + 1] = 103;
		buffer[offset + 2] = 116;
		buffer[offset + 3] = 59;
		Advance(4);
	}

	public void WriteAmpersandCharEntity()
	{
		int offset;
		byte[] buffer = GetBuffer(5, out offset);
		buffer[offset] = 38;
		buffer[offset + 1] = 97;
		buffer[offset + 2] = 109;
		buffer[offset + 3] = 112;
		buffer[offset + 4] = 59;
		Advance(5);
	}

	public void WriteApostropheCharEntity()
	{
		int offset;
		byte[] buffer = GetBuffer(6, out offset);
		buffer[offset] = 38;
		buffer[offset + 1] = 97;
		buffer[offset + 2] = 112;
		buffer[offset + 3] = 111;
		buffer[offset + 4] = 115;
		buffer[offset + 5] = 59;
		Advance(6);
	}

	public void WriteQuoteCharEntity()
	{
		int offset;
		byte[] buffer = GetBuffer(6, out offset);
		buffer[offset] = 38;
		buffer[offset + 1] = 113;
		buffer[offset + 2] = 117;
		buffer[offset + 3] = 111;
		buffer[offset + 4] = 116;
		buffer[offset + 5] = 59;
		Advance(6);
	}

	private void WriteHexCharEntity(int ch)
	{
		byte[] charEntityBuffer = GetCharEntityBuffer();
		int num = 32;
		charEntityBuffer[--num] = 59;
		num -= ToBase16(charEntityBuffer, num, (uint)ch);
		charEntityBuffer[--num] = 120;
		charEntityBuffer[--num] = 35;
		charEntityBuffer[--num] = 38;
		WriteUTF8Chars(charEntityBuffer, num, 32 - num);
	}

	public override void WriteCharEntity(int ch)
	{
		switch (ch)
		{
		case 60:
			WriteLessThanCharEntity();
			break;
		case 62:
			WriteGreaterThanCharEntity();
			break;
		case 38:
			WriteAmpersandCharEntity();
			break;
		case 39:
			WriteApostropheCharEntity();
			break;
		case 34:
			WriteQuoteCharEntity();
			break;
		default:
			WriteHexCharEntity(ch);
			break;
		}
	}

	private int ToBase16(byte[] chars, int offset, uint value)
	{
		int num = 0;
		do
		{
			num++;
			chars[--offset] = s_digits[value & 0xF];
			value /= 16;
		}
		while (value != 0);
		return num;
	}

	public override void WriteBoolText(bool value)
	{
		int offset;
		byte[] buffer = GetBuffer(5, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteDecimalText(decimal value)
	{
		int offset;
		byte[] buffer = GetBuffer(40, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteDoubleText(double value)
	{
		int offset;
		byte[] buffer = GetBuffer(32, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteFloatText(float value)
	{
		int offset;
		byte[] buffer = GetBuffer(16, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteDateTimeText(DateTime value)
	{
		int offset;
		byte[] buffer = GetBuffer(64, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteUniqueIdText(UniqueId value)
	{
		if (value.IsGuid)
		{
			int charArrayLength = value.CharArrayLength;
			char[] charBuffer = GetCharBuffer(charArrayLength);
			value.ToCharArray(charBuffer, 0);
			WriteText(charBuffer, 0, charArrayLength);
		}
		else
		{
			WriteEscapedText(value.ToString());
		}
	}

	public override void WriteInt32Text(int value)
	{
		int offset;
		byte[] buffer = GetBuffer(16, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteInt64Text(long value)
	{
		int offset;
		byte[] buffer = GetBuffer(32, out offset);
		Advance(XmlConverter.ToChars(value, buffer, offset));
	}

	public override void WriteUInt64Text(ulong value)
	{
		int offset;
		byte[] buffer = GetBuffer(32, out offset);
		Advance(XmlConverter.ToChars((double)value, buffer, offset));
	}

	public override void WriteGuidText(Guid value)
	{
		WriteText(value.ToString());
	}

	public override void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
	{
		if (trailByteCount > 0)
		{
			InternalWriteBase64Text(trailBytes, 0, trailByteCount);
		}
		InternalWriteBase64Text(buffer, offset, count);
	}

	public override async Task WriteBase64TextAsync(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
	{
		if (trailByteCount > 0)
		{
			await InternalWriteBase64TextAsync(trailBytes, 0, trailByteCount).ConfigureAwait(continueOnCapturedContext: false);
		}
		await InternalWriteBase64TextAsync(buffer, offset, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	private void InternalWriteBase64Text(byte[] buffer, int offset, int count)
	{
		Base64Encoding base64Encoding = XmlConverter.Base64Encoding;
		while (count >= 3)
		{
			int num = Math.Min(384, count - count % 3);
			int count2 = num / 3 * 4;
			int offset2;
			byte[] buffer2 = GetBuffer(count2, out offset2);
			Advance(base64Encoding.GetChars(buffer, offset, num, buffer2, offset2));
			offset += num;
			count -= num;
		}
		if (count > 0)
		{
			int offset3;
			byte[] buffer3 = GetBuffer(4, out offset3);
			Advance(base64Encoding.GetChars(buffer, offset, count, buffer3, offset3));
		}
	}

	private async Task InternalWriteBase64TextAsync(byte[] buffer, int offset, int count)
	{
		Base64Encoding encoding = XmlConverter.Base64Encoding;
		while (count >= 3)
		{
			int byteCount = Math.Min(384, count - count % 3);
			int count2 = byteCount / 3 * 4;
			BytesWithOffset bytesWithOffset = await GetBufferAsync(count2).ConfigureAwait(continueOnCapturedContext: false);
			byte[] bytes = bytesWithOffset.Bytes;
			int offset2 = bytesWithOffset.Offset;
			Advance(encoding.GetChars(buffer, offset, byteCount, bytes, offset2));
			offset += byteCount;
			count -= byteCount;
		}
		if (count > 0)
		{
			BytesWithOffset bytesWithOffset2 = await GetBufferAsync(4).ConfigureAwait(continueOnCapturedContext: false);
			byte[] bytes2 = bytesWithOffset2.Bytes;
			int offset3 = bytesWithOffset2.Offset;
			Advance(encoding.GetChars(buffer, offset, count, bytes2, offset3));
		}
	}

	public override void WriteTimeSpanText(TimeSpan value)
	{
		WriteText(XmlConvert.ToString(value));
	}

	public override void WriteStartListText()
	{
	}

	public override void WriteListSeparator()
	{
		WriteByte(' ');
	}

	public override void WriteEndListText()
	{
	}

	public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
	{
		if (prefix.Length != 0)
		{
			WritePrefix(prefix);
			WriteByte(':');
		}
		WriteText(localName);
	}
}
