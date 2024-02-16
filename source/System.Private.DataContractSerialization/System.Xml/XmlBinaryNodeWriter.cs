using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal sealed class XmlBinaryNodeWriter : XmlStreamNodeWriter
{
	private struct AttributeValue
	{
		private string _captureText;

		private XmlDictionaryString _captureXText;

		private MemoryStream _captureStream;

		public void Clear()
		{
			_captureText = null;
			_captureXText = null;
			_captureStream = null;
		}

		public void WriteText(string s)
		{
			if (_captureStream != null)
			{
				ArraySegment<byte> buffer;
				bool flag = _captureStream.TryGetBuffer(out buffer);
				_captureText = XmlConverter.Base64Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
				_captureStream = null;
			}
			if (_captureXText != null)
			{
				_captureText = _captureXText.Value;
				_captureXText = null;
			}
			if (_captureText == null || _captureText.Length == 0)
			{
				_captureText = s;
			}
			else
			{
				_captureText += s;
			}
		}

		public void WriteText(XmlDictionaryString s)
		{
			if (_captureText != null || _captureStream != null)
			{
				WriteText(s.Value);
			}
			else
			{
				_captureXText = s;
			}
		}

		public void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
		{
			if (_captureText != null || _captureXText != null)
			{
				if (trailByteCount > 0)
				{
					WriteText(XmlConverter.Base64Encoding.GetString(trailBytes, 0, trailByteCount));
				}
				WriteText(XmlConverter.Base64Encoding.GetString(buffer, offset, count));
				return;
			}
			if (_captureStream == null)
			{
				_captureStream = new MemoryStream();
			}
			if (trailByteCount > 0)
			{
				_captureStream.Write(trailBytes, 0, trailByteCount);
			}
			_captureStream.Write(buffer, offset, count);
		}

		public void WriteTo(XmlBinaryNodeWriter writer)
		{
			if (_captureText != null)
			{
				writer.WriteText(_captureText);
				_captureText = null;
			}
			else if (_captureXText != null)
			{
				writer.WriteText(_captureXText);
				_captureXText = null;
			}
			else if (_captureStream != null)
			{
				ArraySegment<byte> buffer;
				bool flag = _captureStream.TryGetBuffer(out buffer);
				writer.WriteBase64Text(null, 0, buffer.Array, buffer.Offset, buffer.Count);
				_captureStream = null;
			}
			else
			{
				writer.WriteEmptyText();
			}
		}
	}

	private IXmlDictionary _dictionary;

	private XmlBinaryWriterSession _session;

	private bool _inAttribute;

	private bool _inList;

	private bool _wroteAttributeValue;

	private AttributeValue _attributeValue;

	private const int maxBytesPerChar = 3;

	private int _textNodeOffset;

	public void SetOutput(Stream stream, IXmlDictionary dictionary, XmlBinaryWriterSession session, bool ownsStream)
	{
		_dictionary = dictionary;
		_session = session;
		_inAttribute = false;
		_inList = false;
		_attributeValue.Clear();
		_textNodeOffset = -1;
		SetOutput(stream, ownsStream, null);
	}

	private void WriteNode(XmlBinaryNodeType nodeType)
	{
		WriteByte((byte)nodeType);
		_textNodeOffset = -1;
	}

	private void WroteAttributeValue()
	{
		if (_wroteAttributeValue && !_inList)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlOnlySingleValue));
		}
		_wroteAttributeValue = true;
	}

	private void WriteTextNode(XmlBinaryNodeType nodeType)
	{
		if (_inAttribute)
		{
			WroteAttributeValue();
		}
		WriteByte((byte)nodeType);
		_textNodeOffset = base.BufferOffset - 1;
	}

	private byte[] GetTextNodeBuffer(int size, out int offset)
	{
		if (_inAttribute)
		{
			WroteAttributeValue();
		}
		byte[] buffer = GetBuffer(size, out offset);
		_textNodeOffset = offset;
		return buffer;
	}

	private void WriteTextNodeWithLength(XmlBinaryNodeType nodeType, int length)
	{
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(5, out offset);
		if (length < 256)
		{
			textNodeBuffer[offset] = (byte)nodeType;
			textNodeBuffer[offset + 1] = (byte)length;
			Advance(2);
		}
		else if (length < 65536)
		{
			textNodeBuffer[offset] = (byte)(nodeType + 2);
			textNodeBuffer[offset + 1] = (byte)length;
			length >>= 8;
			textNodeBuffer[offset + 2] = (byte)length;
			Advance(3);
		}
		else
		{
			textNodeBuffer[offset] = (byte)(nodeType + 4);
			textNodeBuffer[offset + 1] = (byte)length;
			length >>= 8;
			textNodeBuffer[offset + 2] = (byte)length;
			length >>= 8;
			textNodeBuffer[offset + 3] = (byte)length;
			length >>= 8;
			textNodeBuffer[offset + 4] = (byte)length;
			Advance(5);
		}
	}

	private void WriteTextNodeWithInt64(XmlBinaryNodeType nodeType, long value)
	{
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(9, out offset);
		textNodeBuffer[offset] = (byte)nodeType;
		textNodeBuffer[offset + 1] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 2] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 3] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 4] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 5] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 6] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 7] = (byte)value;
		value >>= 8;
		textNodeBuffer[offset + 8] = (byte)value;
		Advance(9);
	}

	public override void WriteDeclaration()
	{
	}

	public override void WriteStartElement(string prefix, string localName)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.MinElement);
			WriteName(localName);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && c >= 'a' && c <= 'z')
		{
			WritePrefixNode(XmlBinaryNodeType.PrefixElementA, c - 97);
			WriteName(localName);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.Element);
			WriteName(prefix);
			WriteName(localName);
		}
	}

	private void WritePrefixNode(XmlBinaryNodeType nodeType, int ch)
	{
		WriteNode(nodeType + ch);
	}

	public override void WriteStartElement(string prefix, XmlDictionaryString localName)
	{
		if (!TryGetKey(localName, out var key))
		{
			WriteStartElement(prefix, localName.Value);
			return;
		}
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryElement);
			WriteDictionaryString(localName, key);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && c >= 'a' && c <= 'z')
		{
			WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryElementA, c - 97);
			WriteDictionaryString(localName, key);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.DictionaryElement);
			WriteName(prefix);
			WriteDictionaryString(localName, key);
		}
	}

	public override void WriteEndStartElement(bool isEmpty)
	{
		if (isEmpty)
		{
			WriteEndElement();
		}
	}

	public override void WriteEndElement(string prefix, string localName)
	{
		WriteEndElement();
	}

	private void WriteEndElement()
	{
		if (_textNodeOffset != -1)
		{
			byte[] streamBuffer = base.StreamBuffer;
			XmlBinaryNodeType xmlBinaryNodeType = (XmlBinaryNodeType)streamBuffer[_textNodeOffset];
			streamBuffer[_textNodeOffset] = (byte)(xmlBinaryNodeType + 1);
			_textNodeOffset = -1;
		}
		else
		{
			WriteNode(XmlBinaryNodeType.EndElement);
		}
	}

	public override void WriteStartAttribute(string prefix, string localName)
	{
		if (prefix.Length == 0)
		{
			WriteNode(XmlBinaryNodeType.MinAttribute);
			WriteName(localName);
		}
		else
		{
			char c = prefix[0];
			if (prefix.Length == 1 && c >= 'a' && c <= 'z')
			{
				WritePrefixNode(XmlBinaryNodeType.PrefixAttributeA, c - 97);
				WriteName(localName);
			}
			else
			{
				WriteNode(XmlBinaryNodeType.Attribute);
				WriteName(prefix);
				WriteName(localName);
			}
		}
		_inAttribute = true;
		_wroteAttributeValue = false;
	}

	public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
	{
		if (!TryGetKey(localName, out var key))
		{
			WriteStartAttribute(prefix, localName.Value);
			return;
		}
		if (prefix.Length == 0)
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryAttribute);
			WriteDictionaryString(localName, key);
		}
		else
		{
			char c = prefix[0];
			if (prefix.Length == 1 && c >= 'a' && c <= 'z')
			{
				WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryAttributeA, c - 97);
				WriteDictionaryString(localName, key);
			}
			else
			{
				WriteNode(XmlBinaryNodeType.DictionaryAttribute);
				WriteName(prefix);
				WriteDictionaryString(localName, key);
			}
		}
		_inAttribute = true;
		_wroteAttributeValue = false;
	}

	public override void WriteEndAttribute()
	{
		_inAttribute = false;
		if (!_wroteAttributeValue)
		{
			_attributeValue.WriteTo(this);
		}
		_textNodeOffset = -1;
	}

	public override void WriteXmlnsAttribute(string prefix, string ns)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortXmlnsAttribute);
			WriteName(ns);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.XmlnsAttribute);
			WriteName(prefix);
			WriteName(ns);
		}
	}

	public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
	{
		if (!TryGetKey(ns, out var key))
		{
			WriteXmlnsAttribute(prefix, ns.Value);
		}
		else if (string.IsNullOrEmpty(prefix))
		{
			WriteNode(XmlBinaryNodeType.ShortDictionaryXmlnsAttribute);
			WriteDictionaryString(ns, key);
		}
		else
		{
			WriteNode(XmlBinaryNodeType.DictionaryXmlnsAttribute);
			WriteName(prefix);
			WriteDictionaryString(ns, key);
		}
	}

	private bool TryGetKey(XmlDictionaryString s, out int key)
	{
		key = -1;
		if (s.Dictionary == _dictionary)
		{
			key = s.Key * 2;
			return true;
		}
		if (_dictionary != null && _dictionary.TryLookup(s, out XmlDictionaryString result))
		{
			key = result.Key * 2;
			return true;
		}
		if (_session == null)
		{
			return false;
		}
		if (!_session.TryLookup(s, out var key2) && !_session.TryAdd(s, out key2))
		{
			return false;
		}
		key = key2 * 2 + 1;
		return true;
	}

	private void WriteDictionaryString(XmlDictionaryString s, int key)
	{
		WriteMultiByteInt32(key);
	}

	private unsafe void WriteName(string s)
	{
		int length = s.Length;
		if (length == 0)
		{
			WriteByte(0);
			return;
		}
		fixed (char* chars = s)
		{
			UnsafeWriteName(chars, length);
		}
	}

	private unsafe void UnsafeWriteName(char* chars, int charCount)
	{
		if (charCount < 42)
		{
			int offset;
			byte[] buffer = GetBuffer(1 + charCount * 3, out offset);
			int num = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 1);
			buffer[offset] = (byte)num;
			Advance(1 + num);
		}
		else
		{
			int i = UnsafeGetUTF8Length(chars, charCount);
			WriteMultiByteInt32(i);
			UnsafeWriteUTF8Chars(chars, charCount);
		}
	}

	private void WriteMultiByteInt32(int i)
	{
		int offset;
		byte[] buffer = GetBuffer(5, out offset);
		int num = offset;
		while ((i & 0xFFFFFF80u) != 0L)
		{
			buffer[offset++] = (byte)(((uint)i & 0x7Fu) | 0x80u);
			i >>= 7;
		}
		buffer[offset++] = (byte)i;
		Advance(offset - num);
	}

	public override void WriteComment(string value)
	{
		WriteNode(XmlBinaryNodeType.Comment);
		WriteName(value);
	}

	public override void WriteCData(string value)
	{
		WriteText(value);
	}

	private void WriteEmptyText()
	{
		WriteTextNode(XmlBinaryNodeType.EmptyText);
	}

	public override void WriteBoolText(bool value)
	{
		if (value)
		{
			WriteTextNode(XmlBinaryNodeType.TrueText);
		}
		else
		{
			WriteTextNode(XmlBinaryNodeType.FalseText);
		}
	}

	public override void WriteInt32Text(int value)
	{
		if (value >= -128 && value < 128)
		{
			switch (value)
			{
			case 0:
				WriteTextNode(XmlBinaryNodeType.MinText);
				return;
			case 1:
				WriteTextNode(XmlBinaryNodeType.OneText);
				return;
			}
			int offset;
			byte[] textNodeBuffer = GetTextNodeBuffer(2, out offset);
			textNodeBuffer[offset] = 136;
			textNodeBuffer[offset + 1] = (byte)value;
			Advance(2);
		}
		else if (value >= -32768 && value < 32768)
		{
			int offset2;
			byte[] textNodeBuffer2 = GetTextNodeBuffer(3, out offset2);
			textNodeBuffer2[offset2] = 138;
			textNodeBuffer2[offset2 + 1] = (byte)value;
			value >>= 8;
			textNodeBuffer2[offset2 + 2] = (byte)value;
			Advance(3);
		}
		else
		{
			int offset3;
			byte[] textNodeBuffer3 = GetTextNodeBuffer(5, out offset3);
			textNodeBuffer3[offset3] = 140;
			textNodeBuffer3[offset3 + 1] = (byte)value;
			value >>= 8;
			textNodeBuffer3[offset3 + 2] = (byte)value;
			value >>= 8;
			textNodeBuffer3[offset3 + 3] = (byte)value;
			value >>= 8;
			textNodeBuffer3[offset3 + 4] = (byte)value;
			Advance(5);
		}
	}

	public override void WriteInt64Text(long value)
	{
		if (value >= int.MinValue && value <= int.MaxValue)
		{
			WriteInt32Text((int)value);
		}
		else
		{
			WriteTextNodeWithInt64(XmlBinaryNodeType.Int64Text, value);
		}
	}

	public override void WriteUInt64Text(ulong value)
	{
		if (value <= long.MaxValue)
		{
			WriteInt64Text((long)value);
		}
		else
		{
			WriteTextNodeWithInt64(XmlBinaryNodeType.UInt64Text, (long)value);
		}
	}

	private void WriteInt64(long value)
	{
		int offset;
		byte[] buffer = GetBuffer(8, out offset);
		buffer[offset] = (byte)value;
		value >>= 8;
		buffer[offset + 1] = (byte)value;
		value >>= 8;
		buffer[offset + 2] = (byte)value;
		value >>= 8;
		buffer[offset + 3] = (byte)value;
		value >>= 8;
		buffer[offset + 4] = (byte)value;
		value >>= 8;
		buffer[offset + 5] = (byte)value;
		value >>= 8;
		buffer[offset + 6] = (byte)value;
		value >>= 8;
		buffer[offset + 7] = (byte)value;
		Advance(8);
	}

	public override void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] base64Buffer, int base64Offset, int base64Count)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteBase64Text(trailBytes, trailByteCount, base64Buffer, base64Offset, base64Count);
			return;
		}
		int num = trailByteCount + base64Count;
		if (num > 0)
		{
			WriteTextNodeWithLength(XmlBinaryNodeType.Bytes8Text, num);
			if (trailByteCount > 0)
			{
				int offset;
				byte[] buffer = GetBuffer(trailByteCount, out offset);
				for (int i = 0; i < trailByteCount; i++)
				{
					buffer[offset + i] = trailBytes[i];
				}
				Advance(trailByteCount);
			}
			if (base64Count > 0)
			{
				WriteBytes(base64Buffer, base64Offset, base64Count);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	public override void WriteText(XmlDictionaryString value)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(value);
			return;
		}
		if (!TryGetKey(value, out var key))
		{
			WriteText(value.Value);
			return;
		}
		WriteTextNode(XmlBinaryNodeType.DictionaryText);
		WriteDictionaryString(value, key);
	}

	public unsafe override void WriteText(string value)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(value);
		}
		else if (value.Length > 0)
		{
			fixed (char* chars = value)
			{
				UnsafeWriteText(chars, value.Length);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	public unsafe override void WriteText(char[] chars, int offset, int count)
	{
		if (_inAttribute)
		{
			_attributeValue.WriteText(new string(chars, offset, count));
		}
		else if (count > 0)
		{
			fixed (char* chars2 = &chars[offset])
			{
				UnsafeWriteText(chars2, count);
			}
		}
		else
		{
			WriteEmptyText();
		}
	}

	public override void WriteText(byte[] chars, int charOffset, int charCount)
	{
		WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, charCount);
		WriteBytes(chars, charOffset, charCount);
	}

	private unsafe void UnsafeWriteText(char* chars, int charCount)
	{
		if (charCount == 1)
		{
			switch (*chars)
			{
			case '0':
				WriteTextNode(XmlBinaryNodeType.MinText);
				return;
			case '1':
				WriteTextNode(XmlBinaryNodeType.OneText);
				return;
			}
		}
		if (charCount <= 85)
		{
			int offset;
			byte[] buffer = GetBuffer(2 + charCount * 3, out offset);
			int num = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 2);
			if (num / 2 <= charCount)
			{
				buffer[offset] = 152;
			}
			else
			{
				buffer[offset] = 182;
				num = UnsafeGetUnicodeChars(chars, charCount, buffer, offset + 2);
			}
			_textNodeOffset = offset;
			buffer[offset + 1] = (byte)num;
			Advance(2 + num);
		}
		else
		{
			int num2 = UnsafeGetUTF8Length(chars, charCount);
			if (num2 / 2 > charCount)
			{
				WriteTextNodeWithLength(XmlBinaryNodeType.UnicodeChars8Text, charCount * 2);
				UnsafeWriteUnicodeChars(chars, charCount);
			}
			else
			{
				WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, num2);
				UnsafeWriteUTF8Chars(chars, charCount);
			}
		}
	}

	public override void WriteEscapedText(string value)
	{
		WriteText(value);
	}

	public override void WriteEscapedText(XmlDictionaryString value)
	{
		WriteText(value);
	}

	public override void WriteEscapedText(char[] chars, int offset, int count)
	{
		WriteText(chars, offset, count);
	}

	public override void WriteEscapedText(byte[] chars, int offset, int count)
	{
		WriteText(chars, offset, count);
	}

	public override void WriteCharEntity(int ch)
	{
		if (ch > 65535)
		{
			SurrogateChar surrogateChar = new SurrogateChar(ch);
			char[] chars = new char[2] { surrogateChar.HighChar, surrogateChar.LowChar };
			WriteText(chars, 0, 2);
		}
		else
		{
			char[] chars2 = new char[1] { (char)ch };
			WriteText(chars2, 0, 1);
		}
	}

	public unsafe override void WriteFloatText(float f)
	{
		long value;
		if (f >= -9.223372E+18f && f <= 9.223372E+18f && (float)(value = (long)f) == f)
		{
			WriteInt64Text(value);
			return;
		}
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(5, out offset);
		byte* ptr = (byte*)(&f);
		textNodeBuffer[offset] = 144;
		textNodeBuffer[offset + 1] = *ptr;
		textNodeBuffer[offset + 2] = ptr[1];
		textNodeBuffer[offset + 3] = ptr[2];
		textNodeBuffer[offset + 4] = ptr[3];
		Advance(5);
	}

	public unsafe override void WriteDoubleText(double d)
	{
		float value;
		if (d >= -3.4028234663852886E+38 && d <= 3.4028234663852886E+38 && (double)(value = (float)d) == d)
		{
			WriteFloatText(value);
			return;
		}
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(9, out offset);
		byte* ptr = (byte*)(&d);
		textNodeBuffer[offset] = 146;
		textNodeBuffer[offset + 1] = *ptr;
		textNodeBuffer[offset + 2] = ptr[1];
		textNodeBuffer[offset + 3] = ptr[2];
		textNodeBuffer[offset + 4] = ptr[3];
		textNodeBuffer[offset + 5] = ptr[4];
		textNodeBuffer[offset + 6] = ptr[5];
		textNodeBuffer[offset + 7] = ptr[6];
		textNodeBuffer[offset + 8] = ptr[7];
		Advance(9);
	}

	public unsafe override void WriteDecimalText(decimal d)
	{
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(17, out offset);
		byte* ptr = (byte*)(&d);
		textNodeBuffer[offset++] = 148;
		for (int i = 0; i < 16; i++)
		{
			textNodeBuffer[offset++] = ptr[i];
		}
		Advance(17);
	}

	public override void WriteDateTimeText(DateTime dt)
	{
		WriteTextNodeWithInt64(XmlBinaryNodeType.DateTimeText, ToBinary(dt));
	}

	private static long ToBinary(DateTime dt)
	{
		long num = 0L;
		switch (dt.Kind)
		{
		case DateTimeKind.Local:
			num |= long.MinValue;
			num |= dt.ToUniversalTime().Ticks;
			break;
		case DateTimeKind.Utc:
			num |= 0x4000000000000000L;
			num |= dt.Ticks;
			break;
		case DateTimeKind.Unspecified:
			num = dt.Ticks;
			break;
		}
		return num;
	}

	public override void WriteUniqueIdText(UniqueId value)
	{
		if (value.IsGuid)
		{
			int offset;
			byte[] textNodeBuffer = GetTextNodeBuffer(17, out offset);
			textNodeBuffer[offset] = 172;
			value.TryGetGuid(textNodeBuffer, offset + 1);
			Advance(17);
		}
		else
		{
			WriteText(value.ToString());
		}
	}

	public override void WriteGuidText(Guid guid)
	{
		int offset;
		byte[] textNodeBuffer = GetTextNodeBuffer(17, out offset);
		textNodeBuffer[offset] = 176;
		Buffer.BlockCopy(guid.ToByteArray(), 0, textNodeBuffer, offset + 1, 16);
		Advance(17);
	}

	public override void WriteTimeSpanText(TimeSpan value)
	{
		WriteTextNodeWithInt64(XmlBinaryNodeType.TimeSpanText, value.Ticks);
	}

	public override void WriteStartListText()
	{
		_inList = true;
		WriteNode(XmlBinaryNodeType.StartListText);
	}

	public override void WriteListSeparator()
	{
	}

	public override void WriteEndListText()
	{
		_inList = false;
		_wroteAttributeValue = true;
		WriteNode(XmlBinaryNodeType.EndListText);
	}

	public void WriteArrayNode()
	{
		WriteNode(XmlBinaryNodeType.Array);
	}

	private void WriteArrayInfo(XmlBinaryNodeType nodeType, int count)
	{
		WriteNode(nodeType);
		WriteMultiByteInt32(count);
	}

	public unsafe void UnsafeWriteArray(XmlBinaryNodeType nodeType, int count, byte* array, byte* arrayMax)
	{
		WriteArrayInfo(nodeType, count);
		UnsafeWriteArray(array, (int)(arrayMax - array));
	}

	private unsafe void UnsafeWriteArray(byte* array, int byteCount)
	{
		UnsafeWriteBytes(array, byteCount);
	}

	public void WriteDateTimeArray(DateTime[] array, int offset, int count)
	{
		WriteArrayInfo(XmlBinaryNodeType.DateTimeTextWithEndElement, count);
		for (int i = 0; i < count; i++)
		{
			WriteInt64(ToBinary(array[offset + i]));
		}
	}

	public void WriteGuidArray(Guid[] array, int offset, int count)
	{
		WriteArrayInfo(XmlBinaryNodeType.GuidTextWithEndElement, count);
		for (int i = 0; i < count; i++)
		{
			byte[] byteBuffer = array[offset + i].ToByteArray();
			WriteBytes(byteBuffer, 0, 16);
		}
	}

	public void WriteTimeSpanArray(TimeSpan[] array, int offset, int count)
	{
		WriteArrayInfo(XmlBinaryNodeType.TimeSpanTextWithEndElement, count);
		for (int i = 0; i < count; i++)
		{
			WriteInt64(array[offset + i].Ticks);
		}
	}

	public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
	{
		if (prefix.Length == 0)
		{
			WriteText(localName);
			return;
		}
		char c = prefix[0];
		if (prefix.Length == 1 && c >= 'a' && c <= 'z' && TryGetKey(localName, out var key))
		{
			WriteTextNode(XmlBinaryNodeType.QNameDictionaryText);
			WriteByte((byte)(c - 97));
			WriteDictionaryString(localName, key);
		}
		else
		{
			WriteText(prefix);
			WriteText(":");
			WriteText(localName);
		}
	}

	protected override void FlushBuffer()
	{
		base.FlushBuffer();
		_textNodeOffset = -1;
	}

	public override void Close()
	{
		base.Close();
		_attributeValue.Clear();
	}
}
