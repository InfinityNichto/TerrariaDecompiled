using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal sealed class XmlUTF8TextReader : XmlBaseReader, IXmlLineInfo, IXmlTextReaderInitializer
{
	private static class CharType
	{
		public const byte None = 0;

		public const byte FirstName = 1;

		public const byte Name = 2;

		public const byte Whitespace = 4;

		public const byte Text = 8;

		public const byte AttributeText = 16;

		public const byte SpecialWhitespace = 32;

		public const byte Comment = 64;
	}

	private const int MaxTextChunk = 2048;

	private readonly PrefixHandle _prefix;

	private readonly StringHandle _localName;

	private int[] _rowOffsets;

	private OnXmlDictionaryReaderClose _onClose;

	private bool _buffered;

	private int _maxBytesPerRead;

	private static readonly byte[] s_charType = new byte[256]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 108,
		108, 0, 0, 68, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 124, 88, 72, 88, 88, 88, 64, 72,
		88, 88, 88, 88, 88, 90, 90, 88, 90, 90,
		90, 90, 90, 90, 90, 90, 90, 90, 88, 88,
		64, 88, 88, 88, 88, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 88, 88, 80, 88, 91, 88, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 88, 88, 88, 88, 88, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 3,
		91, 91, 91, 91, 91, 91, 91, 91, 91, 91,
		91, 91, 91, 91, 91, 91
	};

	public int LineNumber
	{
		get
		{
			GetPosition(out var row, out var _);
			return row;
		}
	}

	public int LinePosition
	{
		get
		{
			GetPosition(out var _, out var column);
			return column;
		}
	}

	public XmlUTF8TextReader()
	{
		_prefix = new PrefixHandle(base.BufferReader);
		_localName = new StringHandle(base.BufferReader);
	}

	public void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
	{
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > buffer.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		MoveToInitial(quotas, onClose);
		ArraySegment<byte> arraySegment = EncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
		base.BufferReader.SetBuffer(arraySegment.Array, arraySegment.Offset, arraySegment.Count, null, null);
		_buffered = true;
	}

	public void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
	{
		if (stream == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("stream");
		}
		MoveToInitial(quotas, onClose);
		stream = new EncodingStreamWrapper(stream, encoding);
		base.BufferReader.SetBuffer(stream, null, null);
		_buffered = false;
	}

	private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
	{
		MoveToInitial(quotas);
		_maxBytesPerRead = quotas.MaxBytesPerRead;
		_onClose = onClose;
	}

	public override void Close()
	{
		_rowOffsets = null;
		base.Close();
		OnXmlDictionaryReaderClose onClose = _onClose;
		_onClose = null;
		if (onClose != null)
		{
			try
			{
				onClose(this);
			}
			catch (Exception e)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(e);
			}
		}
	}

	private void SkipWhitespace()
	{
		while (!base.BufferReader.EndOfFile && (s_charType[base.BufferReader.GetByte()] & 4u) != 0)
		{
			base.BufferReader.SkipByte();
		}
	}

	private void ReadDeclaration()
	{
		if (!_buffered)
		{
			BufferElement();
		}
		byte[] buffer = base.BufferReader.GetBuffer(5, out var offset);
		if (buffer[offset] != 63 || buffer[offset + 1] != 120 || buffer[offset + 2] != 109 || buffer[offset + 3] != 108 || (s_charType[buffer[offset + 4]] & 4) == 0)
		{
			XmlExceptionHelper.ThrowProcessingInstructionNotSupported(this);
		}
		if (base.Node.ReadState != 0)
		{
			XmlExceptionHelper.ThrowDeclarationNotFirst(this);
		}
		base.BufferReader.Advance(5);
		int offset2 = offset + 1;
		int length = 3;
		int offset3 = base.BufferReader.Offset;
		SkipWhitespace();
		ReadAttributes();
		int num;
		for (num = base.BufferReader.Offset - offset3; num > 0; num--)
		{
			byte @byte = base.BufferReader.GetByte(offset3 + num - 1);
			if ((s_charType[@byte] & 4) == 0)
			{
				break;
			}
		}
		buffer = base.BufferReader.GetBuffer(2, out offset);
		if (buffer[offset] != 63 || buffer[offset + 1] != 62)
		{
			XmlExceptionHelper.ThrowTokenExpected(this, "?>", Encoding.UTF8.GetString(buffer, offset, 2));
		}
		base.BufferReader.Advance(2);
		XmlDeclarationNode xmlDeclarationNode = MoveToDeclaration();
		xmlDeclarationNode.LocalName.SetValue(offset2, length);
		xmlDeclarationNode.Value.SetValue(ValueHandleType.UTF8, offset3, num);
	}

	private void VerifyNCName(string s)
	{
		try
		{
			XmlConvert.VerifyNCName(s);
		}
		catch (XmlException exception)
		{
			XmlExceptionHelper.ThrowXmlException(this, exception);
		}
	}

	private void ReadQualifiedName(PrefixHandle prefix, StringHandle localName)
	{
		int i;
		int offsetMax;
		byte[] buffer = base.BufferReader.GetBuffer(out i, out offsetMax);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = i;
		if (i < offsetMax)
		{
			num = buffer[i];
			num3 = num;
			if ((s_charType[num] & 1) == 0)
			{
				num2 |= 0x80;
			}
			num2 |= num;
			for (i++; i < offsetMax; i++)
			{
				num = buffer[i];
				if ((s_charType[num] & 2) == 0)
				{
					break;
				}
				num2 |= num;
			}
		}
		else
		{
			num2 |= 0x80;
			num = 0;
		}
		if (num == 58)
		{
			int num5 = i - num4;
			if (num5 == 1 && num3 >= 97 && num3 <= 122)
			{
				prefix.SetValue(PrefixHandle.GetAlphaPrefix(num3 - 97));
			}
			else
			{
				prefix.SetValue(num4, num5);
			}
			i++;
			int num6 = i;
			if (i < offsetMax)
			{
				num = buffer[i];
				if ((s_charType[num] & 1) == 0)
				{
					num2 |= 0x80;
				}
				num2 |= num;
				for (i++; i < offsetMax; i++)
				{
					num = buffer[i];
					if ((s_charType[num] & 2) == 0)
					{
						break;
					}
					num2 |= num;
				}
			}
			else
			{
				num2 |= 0x80;
				num = 0;
			}
			localName.SetValue(num6, i - num6);
			if (num2 >= 128)
			{
				VerifyNCName(prefix.GetString());
				VerifyNCName(localName.GetString());
			}
		}
		else
		{
			prefix.SetValue(PrefixHandleType.Empty);
			localName.SetValue(num4, i - num4);
			if (num2 >= 128)
			{
				VerifyNCName(localName.GetString());
			}
		}
		base.BufferReader.Advance(i - num4);
	}

	private int ReadAttributeText(byte[] buffer, int offset, int offsetMax)
	{
		byte[] array = s_charType;
		int num = offset;
		while (offset < offsetMax && (array[buffer[offset]] & 0x10u) != 0)
		{
			offset++;
		}
		return offset - num;
	}

	private void ReadAttributes()
	{
		int num = 0;
		if (_buffered)
		{
			num = base.BufferReader.Offset;
		}
		while (true)
		{
			ReadQualifiedName(_prefix, _localName);
			if (base.BufferReader.GetByte() != 61)
			{
				SkipWhitespace();
				if (base.BufferReader.GetByte() != 61)
				{
					XmlExceptionHelper.ThrowTokenExpected(this, "=", (char)base.BufferReader.GetByte());
				}
			}
			base.BufferReader.SkipByte();
			byte @byte = base.BufferReader.GetByte();
			if (@byte != 34 && @byte != 39)
			{
				SkipWhitespace();
				@byte = base.BufferReader.GetByte();
				if (@byte != 34 && @byte != 39)
				{
					XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)base.BufferReader.GetByte());
				}
			}
			base.BufferReader.SkipByte();
			bool flag = false;
			int offset = base.BufferReader.Offset;
			byte byte2;
			while (true)
			{
				int offset2;
				int offsetMax;
				byte[] buffer = base.BufferReader.GetBuffer(out offset2, out offsetMax);
				int count = ReadAttributeText(buffer, offset2, offsetMax);
				base.BufferReader.Advance(count);
				byte2 = base.BufferReader.GetByte();
				if (byte2 == @byte)
				{
					break;
				}
				switch (byte2)
				{
				case 38:
					ReadCharRef();
					flag = true;
					break;
				case 34:
				case 39:
					base.BufferReader.SkipByte();
					break;
				case 9:
				case 10:
				case 13:
					base.BufferReader.SkipByte();
					flag = true;
					break;
				case 239:
					ReadNonFFFE();
					break;
				default:
				{
					char c = (char)@byte;
					XmlExceptionHelper.ThrowTokenExpected(this, c.ToString(), (char)byte2);
					break;
				}
				}
			}
			int length = base.BufferReader.Offset - offset;
			XmlAttributeNode xmlAttributeNode;
			if (_prefix.IsXmlns)
			{
				Namespace @namespace = AddNamespace();
				_localName.ToPrefixHandle(@namespace.Prefix);
				@namespace.Uri.SetValue(offset, length, flag);
				xmlAttributeNode = AddXmlnsAttribute(@namespace);
			}
			else if (_prefix.IsEmpty && _localName.IsXmlns)
			{
				Namespace namespace2 = AddNamespace();
				namespace2.Prefix.SetValue(PrefixHandleType.Empty);
				namespace2.Uri.SetValue(offset, length, flag);
				xmlAttributeNode = AddXmlnsAttribute(namespace2);
			}
			else if (_prefix.IsXml)
			{
				xmlAttributeNode = AddXmlAttribute();
				xmlAttributeNode.Prefix.SetValue(_prefix);
				xmlAttributeNode.LocalName.SetValue(_localName);
				xmlAttributeNode.Value.SetValue(flag ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8, offset, length);
				FixXmlAttribute(xmlAttributeNode);
			}
			else
			{
				xmlAttributeNode = AddAttribute();
				xmlAttributeNode.Prefix.SetValue(_prefix);
				xmlAttributeNode.LocalName.SetValue(_localName);
				xmlAttributeNode.Value.SetValue(flag ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8, offset, length);
			}
			xmlAttributeNode.QuoteChar = (char)@byte;
			base.BufferReader.SkipByte();
			byte2 = base.BufferReader.GetByte();
			bool flag2 = false;
			while ((s_charType[byte2] & 4u) != 0)
			{
				flag2 = true;
				base.BufferReader.SkipByte();
				byte2 = base.BufferReader.GetByte();
			}
			if (byte2 == 62 || byte2 == 47 || byte2 == 63)
			{
				break;
			}
			if (!flag2)
			{
				XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlSpaceBetweenAttributes));
			}
		}
		if (_buffered && base.BufferReader.Offset - num > _maxBytesPerRead)
		{
			XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);
		}
		ProcessAttributes();
	}

	private bool IsNextCharacterNonFFFE(byte[] buffer, int offset)
	{
		if (buffer[offset + 1] == 191 && (buffer[offset + 2] == 190 || buffer[offset + 2] == 191))
		{
			return false;
		}
		return true;
	}

	private void ReadNonFFFE()
	{
		int offset;
		byte[] buffer = base.BufferReader.GetBuffer(3, out offset);
		if (buffer[offset + 1] == 191 && (buffer[offset + 2] == 190 || buffer[offset + 2] == 191))
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlInvalidFFFE));
		}
		base.BufferReader.Advance(3);
	}

	private void BufferElement()
	{
		int offset = base.BufferReader.Offset;
		bool flag = false;
		byte b = 0;
		while (!flag)
		{
			int offset2;
			int offsetMax;
			byte[] buffer = base.BufferReader.GetBuffer(128, out offset2, out offsetMax);
			if (offset2 + 128 != offsetMax)
			{
				break;
			}
			for (int i = offset2; i < offsetMax; i++)
			{
				if (flag)
				{
					break;
				}
				byte b2 = buffer[i];
				if (b == 0)
				{
					if (b2 == 39 || b2 == 34)
					{
						b = b2;
					}
					if (b2 == 62)
					{
						flag = true;
					}
				}
				else if (b2 == b)
				{
					b = 0;
				}
			}
			base.BufferReader.Advance(128);
		}
		base.BufferReader.Offset = offset;
	}

	private new void ReadStartElement()
	{
		if (!_buffered)
		{
			BufferElement();
		}
		XmlElementNode xmlElementNode = EnterScope();
		xmlElementNode.NameOffset = base.BufferReader.Offset;
		ReadQualifiedName(xmlElementNode.Prefix, xmlElementNode.LocalName);
		xmlElementNode.NameLength = base.BufferReader.Offset - xmlElementNode.NameOffset;
		byte @byte = base.BufferReader.GetByte();
		while ((s_charType[@byte] & 4u) != 0)
		{
			base.BufferReader.SkipByte();
			@byte = base.BufferReader.GetByte();
		}
		if (@byte != 62 && @byte != 47)
		{
			ReadAttributes();
			@byte = base.BufferReader.GetByte();
		}
		xmlElementNode.Namespace = LookupNamespace(xmlElementNode.Prefix);
		bool flag = false;
		if (@byte == 47)
		{
			flag = true;
			base.BufferReader.SkipByte();
		}
		xmlElementNode.IsEmptyElement = flag;
		xmlElementNode.ExitScope = flag;
		if (base.BufferReader.GetByte() != 62)
		{
			XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)base.BufferReader.GetByte());
		}
		base.BufferReader.SkipByte();
		xmlElementNode.BufferOffset = base.BufferReader.Offset;
	}

	private new void ReadEndElement()
	{
		base.BufferReader.SkipByte();
		XmlElementNode elementNode = base.ElementNode;
		int nameOffset = elementNode.NameOffset;
		int nameLength = elementNode.NameLength;
		int offset;
		byte[] buffer = base.BufferReader.GetBuffer(nameLength, out offset);
		for (int i = 0; i < nameLength; i++)
		{
			if (buffer[offset + i] != buffer[nameOffset + i])
			{
				ReadQualifiedName(_prefix, _localName);
				XmlExceptionHelper.ThrowTagMismatch(this, elementNode.Prefix.GetString(), elementNode.LocalName.GetString(), _prefix.GetString(), _localName.GetString());
			}
		}
		base.BufferReader.Advance(nameLength);
		if (base.BufferReader.GetByte() != 62)
		{
			SkipWhitespace();
			if (base.BufferReader.GetByte() != 62)
			{
				XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)base.BufferReader.GetByte());
			}
		}
		base.BufferReader.SkipByte();
		MoveToEndElement();
	}

	private void ReadComment()
	{
		base.BufferReader.SkipByte();
		if (base.BufferReader.GetByte() != 45)
		{
			XmlExceptionHelper.ThrowTokenExpected(this, "--", (char)base.BufferReader.GetByte());
		}
		base.BufferReader.SkipByte();
		int offset = base.BufferReader.Offset;
		while (true)
		{
			byte @byte = base.BufferReader.GetByte();
			if (@byte != 45)
			{
				if ((s_charType[@byte] & 0x40) == 0)
				{
					if (@byte == 239)
					{
						ReadNonFFFE();
					}
					else
					{
						XmlExceptionHelper.ThrowInvalidXml(this, @byte);
					}
				}
				else
				{
					base.BufferReader.SkipByte();
				}
				continue;
			}
			int offset2;
			byte[] buffer = base.BufferReader.GetBuffer(3, out offset2);
			if (buffer[offset2] == 45 && buffer[offset2 + 1] == 45)
			{
				if (buffer[offset2 + 2] == 62)
				{
					break;
				}
				XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlInvalidCommentChars));
			}
			base.BufferReader.SkipByte();
		}
		int length = base.BufferReader.Offset - offset;
		MoveToComment().Value.SetValue(ValueHandleType.UTF8, offset, length);
		base.BufferReader.Advance(3);
	}

	private void ReadCData()
	{
		byte[] buffer = base.BufferReader.GetBuffer(7, out var offset);
		if (buffer[offset] != 91 || buffer[offset + 1] != 67 || buffer[offset + 2] != 68 || buffer[offset + 3] != 65 || buffer[offset + 4] != 84 || buffer[offset + 5] != 65 || buffer[offset + 6] != 91)
		{
			XmlExceptionHelper.ThrowTokenExpected(this, "[CDATA[", Encoding.UTF8.GetString(buffer, offset, 7));
		}
		base.BufferReader.Advance(7);
		int offset2 = base.BufferReader.Offset;
		while (true)
		{
			switch (base.BufferReader.GetByte())
			{
			case 239:
				ReadNonFFFE();
				break;
			default:
				base.BufferReader.SkipByte();
				break;
			case 93:
			{
				buffer = base.BufferReader.GetBuffer(3, out offset);
				if (buffer[offset] != 93 || buffer[offset + 1] != 93 || buffer[offset + 2] != 62)
				{
					base.BufferReader.SkipByte();
					break;
				}
				int length = base.BufferReader.Offset - offset2;
				MoveToCData().Value.SetValue(ValueHandleType.UTF8, offset2, length);
				base.BufferReader.Advance(3);
				return;
			}
			}
		}
	}

	private int ReadCharRef()
	{
		int offset = base.BufferReader.Offset;
		base.BufferReader.SkipByte();
		while (base.BufferReader.GetByte() != 59)
		{
			base.BufferReader.SkipByte();
		}
		base.BufferReader.SkipByte();
		int num = base.BufferReader.Offset - offset;
		base.BufferReader.Offset = offset;
		int charEntity = base.BufferReader.GetCharEntity(offset, num);
		base.BufferReader.Advance(num);
		return charEntity;
	}

	private void ReadWhitespace()
	{
		int offset;
		int offsetMax;
		int num;
		if (_buffered)
		{
			byte[] buffer = base.BufferReader.GetBuffer(out offset, out offsetMax);
			num = ReadWhitespace(buffer, offset, offsetMax);
		}
		else
		{
			byte[] buffer = base.BufferReader.GetBuffer(2048, out offset, out offsetMax);
			num = ReadWhitespace(buffer, offset, offsetMax);
			num = BreakText(buffer, offset, num);
		}
		base.BufferReader.Advance(num);
		MoveToWhitespaceText().Value.SetValue(ValueHandleType.UTF8, offset, num);
	}

	private int ReadWhitespace(byte[] buffer, int offset, int offsetMax)
	{
		byte[] array = s_charType;
		int num = offset;
		while (offset < offsetMax && (array[buffer[offset]] & 0x20u) != 0)
		{
			offset++;
		}
		return offset - num;
	}

	private int ReadText(byte[] buffer, int offset, int offsetMax)
	{
		byte[] array = s_charType;
		int num = offset;
		while (offset < offsetMax && (array[buffer[offset]] & 8u) != 0)
		{
			offset++;
		}
		return offset - num;
	}

	private int ReadTextAndWatchForInvalidCharacters(byte[] buffer, int offset, int offsetMax)
	{
		byte[] array = s_charType;
		int num = offset;
		while (offset < offsetMax && ((array[buffer[offset]] & 8u) != 0 || buffer[offset] == 239))
		{
			if (buffer[offset] != 239)
			{
				offset++;
				continue;
			}
			if (offset + 2 < offsetMax)
			{
				if (IsNextCharacterNonFFFE(buffer, offset))
				{
					offset += 3;
				}
				else
				{
					XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlInvalidFFFE));
				}
				continue;
			}
			if (base.BufferReader.Offset < offset)
			{
				break;
			}
			base.BufferReader.GetBuffer(3, out var _);
		}
		return offset - num;
	}

	private int BreakText(byte[] buffer, int offset, int length)
	{
		if (length > 0 && (buffer[offset + length - 1] & 0x80) == 128)
		{
			int num = length;
			do
			{
				length--;
			}
			while (length > 0 && (buffer[offset + length] & 0xC0) != 192);
			if (length == 0)
			{
				return num;
			}
			byte b = (byte)(buffer[offset + length] << 2);
			int num2 = 2;
			while ((b & 0x80) == 128)
			{
				b <<= 1;
				num2++;
				if (num2 > 4)
				{
					return num;
				}
			}
			if (length + num2 == num)
			{
				return num;
			}
		}
		return length;
	}

	private void ReadText(bool hasLeadingByteOf0xEF)
	{
		byte[] buffer;
		int offset;
		int offsetMax;
		int num;
		if (_buffered)
		{
			buffer = base.BufferReader.GetBuffer(out offset, out offsetMax);
			num = ((!hasLeadingByteOf0xEF) ? ReadText(buffer, offset, offsetMax) : ReadTextAndWatchForInvalidCharacters(buffer, offset, offsetMax));
		}
		else
		{
			buffer = base.BufferReader.GetBuffer(2048, out offset, out offsetMax);
			num = BreakText(length: (!hasLeadingByteOf0xEF) ? ReadText(buffer, offset, offsetMax) : ReadTextAndWatchForInvalidCharacters(buffer, offset, offsetMax), buffer: buffer, offset: offset);
		}
		base.BufferReader.Advance(num);
		if (offset < offsetMax - 1 - num && buffer[offset + num] == 60 && buffer[offset + num + 1] != 33)
		{
			MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num);
		}
		else
		{
			MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
		}
	}

	private void ReadEscapedText()
	{
		int num = ReadCharRef();
		if (num < 256 && (s_charType[num] & 4u) != 0)
		{
			MoveToWhitespaceText().Value.SetCharValue(num);
		}
		else
		{
			MoveToComplexText().Value.SetCharValue(num);
		}
	}

	public override bool Read()
	{
		if (base.Node.ReadState == ReadState.Closed)
		{
			return false;
		}
		if (base.Node.CanMoveToElement)
		{
			MoveToElement();
		}
		SignNode();
		if (base.Node.ExitScope)
		{
			ExitScope();
		}
		if (!_buffered)
		{
			base.BufferReader.SetWindow(base.ElementNode.BufferOffset, _maxBytesPerRead);
		}
		if (base.BufferReader.EndOfFile)
		{
			MoveToEndOfFile();
			return false;
		}
		byte @byte = base.BufferReader.GetByte();
		if (@byte == 60)
		{
			base.BufferReader.SkipByte();
			switch (base.BufferReader.GetByte())
			{
			case 47:
				ReadEndElement();
				break;
			case 33:
				base.BufferReader.SkipByte();
				@byte = base.BufferReader.GetByte();
				if (@byte == 45)
				{
					ReadComment();
					break;
				}
				if (base.OutsideRootElement)
				{
					XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlCDATAInvalidAtTopLevel));
				}
				ReadCData();
				break;
			case 63:
				ReadDeclaration();
				break;
			default:
				ReadStartElement();
				break;
			}
		}
		else if ((s_charType[@byte] & 0x20u) != 0)
		{
			ReadWhitespace();
		}
		else if (base.OutsideRootElement && @byte != 13)
		{
			XmlExceptionHelper.ThrowInvalidRootData(this);
		}
		else if ((s_charType[@byte] & 8u) != 0)
		{
			ReadText(hasLeadingByteOf0xEF: false);
		}
		else
		{
			switch (@byte)
			{
			case 38:
				ReadEscapedText();
				break;
			case 13:
				base.BufferReader.SkipByte();
				if (!base.BufferReader.EndOfFile && base.BufferReader.GetByte() == 10)
				{
					ReadWhitespace();
				}
				else
				{
					MoveToComplexText().Value.SetCharValue(10);
				}
				break;
			case 93:
			{
				int offset;
				byte[] buffer = base.BufferReader.GetBuffer(3, out offset);
				if (buffer[offset] == 93 && buffer[offset + 1] == 93 && buffer[offset + 2] == 62)
				{
					XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlCloseCData));
				}
				base.BufferReader.SkipByte();
				MoveToComplexText().Value.SetCharValue(93);
				break;
			}
			case 239:
				ReadText(hasLeadingByteOf0xEF: true);
				break;
			default:
				XmlExceptionHelper.ThrowInvalidXml(this, @byte);
				break;
			}
		}
		return true;
	}

	protected override XmlSigningNodeWriter CreateSigningNodeWriter()
	{
		return new XmlSigningNodeWriter(text: true);
	}

	public bool HasLineInfo()
	{
		return true;
	}

	private void GetPosition(out int row, out int column)
	{
		if (_rowOffsets == null)
		{
			_rowOffsets = base.BufferReader.GetRows();
		}
		int offset = base.BufferReader.Offset;
		int i;
		for (i = 0; i < _rowOffsets.Length - 1 && _rowOffsets[i + 1] < offset; i++)
		{
		}
		row = i + 1;
		column = offset - _rowOffsets[i] + 1;
	}
}
