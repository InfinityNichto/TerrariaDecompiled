using System.Globalization;
using System.IO;
using System.Text;

namespace System.Xml;

internal sealed class XmlTextEncoder
{
	private readonly TextWriter _textWriter;

	private bool _inAttribute;

	private char _quoteChar;

	private StringBuilder _attrValue;

	private bool _cacheAttrValue;

	internal char QuoteChar
	{
		set
		{
			_quoteChar = value;
		}
	}

	internal string AttributeValue
	{
		get
		{
			if (_cacheAttrValue)
			{
				return _attrValue.ToString();
			}
			return string.Empty;
		}
	}

	internal XmlTextEncoder(TextWriter textWriter)
	{
		_textWriter = textWriter;
		_quoteChar = '"';
	}

	internal void StartAttribute(bool cacheAttrValue)
	{
		_inAttribute = true;
		_cacheAttrValue = cacheAttrValue;
		if (cacheAttrValue)
		{
			if (_attrValue == null)
			{
				_attrValue = new StringBuilder();
			}
			else
			{
				_attrValue.Length = 0;
			}
		}
	}

	internal void EndAttribute()
	{
		if (_cacheAttrValue)
		{
			_attrValue.Length = 0;
		}
		_inAttribute = false;
		_cacheAttrValue = false;
	}

	internal void WriteSurrogateChar(char lowChar, char highChar)
	{
		if (!XmlCharType.IsLowSurrogate(lowChar) || !XmlCharType.IsHighSurrogate(highChar))
		{
			throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
		}
		_textWriter.Write(highChar);
		_textWriter.Write(lowChar);
	}

	internal void Write(char[] array, int offset, int count)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (0 > offset)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (0 > count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (count > array.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_cacheAttrValue)
		{
			_attrValue.Append(array, offset, count);
		}
		int num = offset + count;
		int i = offset;
		char c = '\0';
		while (true)
		{
			int num2 = i;
			for (; i < num; i++)
			{
				if (!XmlCharType.IsAttributeValueChar(c = array[i]))
				{
					break;
				}
			}
			if (num2 < i)
			{
				_textWriter.Write(array, num2, i - num2);
			}
			if (i == num)
			{
				break;
			}
			switch (c)
			{
			case '\t':
				_textWriter.Write(c);
				break;
			case '\n':
			case '\r':
				if (_inAttribute)
				{
					WriteCharEntityImpl(c);
				}
				else
				{
					_textWriter.Write(c);
				}
				break;
			case '<':
				WriteEntityRefImpl("lt");
				break;
			case '>':
				WriteEntityRefImpl("gt");
				break;
			case '&':
				WriteEntityRefImpl("amp");
				break;
			case '\'':
				if (_inAttribute && _quoteChar == c)
				{
					WriteEntityRefImpl("apos");
				}
				else
				{
					_textWriter.Write('\'');
				}
				break;
			case '"':
				if (_inAttribute && _quoteChar == c)
				{
					WriteEntityRefImpl("quot");
				}
				else
				{
					_textWriter.Write('"');
				}
				break;
			default:
				if (XmlCharType.IsHighSurrogate(c))
				{
					if (i + 1 >= num)
					{
						throw new ArgumentException(System.SR.Xml_SurrogatePairSplit);
					}
					WriteSurrogateChar(array[++i], c);
				}
				else
				{
					if (XmlCharType.IsLowSurrogate(c))
					{
						throw XmlConvert.CreateInvalidHighSurrogateCharException(c);
					}
					WriteCharEntityImpl(c);
				}
				break;
			}
			i++;
		}
	}

	internal void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		if (!XmlCharType.IsLowSurrogate(lowChar) || !XmlCharType.IsHighSurrogate(highChar))
		{
			throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
		}
		int num = XmlCharType.CombineSurrogateChar(lowChar, highChar);
		if (_cacheAttrValue)
		{
			_attrValue.Append(highChar);
			_attrValue.Append(lowChar);
		}
		_textWriter.Write("&#x");
		_textWriter.Write(num.ToString("X", NumberFormatInfo.InvariantInfo));
		_textWriter.Write(';');
	}

	internal void Write(string text)
	{
		if (text == null)
		{
			return;
		}
		if (_cacheAttrValue)
		{
			_attrValue.Append(text);
		}
		int length = text.Length;
		int i = 0;
		int num = 0;
		char c = '\0';
		while (true)
		{
			if (i < length && XmlCharType.IsAttributeValueChar(c = text[i]))
			{
				i++;
				continue;
			}
			if (i == length)
			{
				_textWriter.Write(text);
				return;
			}
			if (_inAttribute)
			{
				if (c != '\t')
				{
					break;
				}
				i++;
			}
			else
			{
				if (c != '\t' && c != '\n' && c != '\r' && c != '"' && c != '\'')
				{
					break;
				}
				i++;
			}
		}
		char[] helperBuffer = new char[256];
		while (true)
		{
			if (num < i)
			{
				WriteStringFragment(text, num, i - num, helperBuffer);
			}
			if (i == length)
			{
				break;
			}
			switch (c)
			{
			case '\t':
				_textWriter.Write(c);
				break;
			case '\n':
			case '\r':
				if (_inAttribute)
				{
					WriteCharEntityImpl(c);
				}
				else
				{
					_textWriter.Write(c);
				}
				break;
			case '<':
				WriteEntityRefImpl("lt");
				break;
			case '>':
				WriteEntityRefImpl("gt");
				break;
			case '&':
				WriteEntityRefImpl("amp");
				break;
			case '\'':
				if (_inAttribute && _quoteChar == c)
				{
					WriteEntityRefImpl("apos");
				}
				else
				{
					_textWriter.Write('\'');
				}
				break;
			case '"':
				if (_inAttribute && _quoteChar == c)
				{
					WriteEntityRefImpl("quot");
				}
				else
				{
					_textWriter.Write('"');
				}
				break;
			default:
				if (XmlCharType.IsHighSurrogate(c))
				{
					if (i + 1 >= length)
					{
						throw XmlConvert.CreateInvalidSurrogatePairException(text[i], c);
					}
					WriteSurrogateChar(text[++i], c);
				}
				else
				{
					if (XmlCharType.IsLowSurrogate(c))
					{
						throw XmlConvert.CreateInvalidHighSurrogateCharException(c);
					}
					WriteCharEntityImpl(c);
				}
				break;
			}
			i++;
			num = i;
			for (; i < length; i++)
			{
				if (!XmlCharType.IsAttributeValueChar(c = text[i]))
				{
					break;
				}
			}
		}
	}

	internal void WriteRawWithSurrogateChecking(string text)
	{
		if (text == null)
		{
			return;
		}
		if (_cacheAttrValue)
		{
			_attrValue.Append(text);
		}
		int length = text.Length;
		int num = 0;
		char c = '\0';
		while (true)
		{
			if (num < length && (XmlCharType.IsCharData(c = text[num]) || c < ' '))
			{
				num++;
				continue;
			}
			if (num == length)
			{
				break;
			}
			if (XmlCharType.IsHighSurrogate(c))
			{
				if (num + 1 >= length)
				{
					throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
				}
				char c2 = text[num + 1];
				if (!XmlCharType.IsLowSurrogate(c2))
				{
					throw XmlConvert.CreateInvalidSurrogatePairException(c2, c);
				}
				num += 2;
			}
			else
			{
				if (XmlCharType.IsLowSurrogate(c))
				{
					throw XmlConvert.CreateInvalidHighSurrogateCharException(c);
				}
				num++;
			}
		}
		_textWriter.Write(text);
	}

	internal void WriteRaw(char[] array, int offset, int count)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (0 > count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (0 > offset)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count > array.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_cacheAttrValue)
		{
			_attrValue.Append(array, offset, count);
		}
		_textWriter.Write(array, offset, count);
	}

	internal void WriteCharEntity(char ch)
	{
		if (XmlCharType.IsSurrogate(ch))
		{
			throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
		}
		int num = ch;
		string text = num.ToString("X", NumberFormatInfo.InvariantInfo);
		if (_cacheAttrValue)
		{
			_attrValue.Append("&#x");
			_attrValue.Append(text);
			_attrValue.Append(';');
		}
		WriteCharEntityImpl(text);
	}

	internal void WriteEntityRef(string name)
	{
		if (_cacheAttrValue)
		{
			_attrValue.Append('&');
			_attrValue.Append(name);
			_attrValue.Append(';');
		}
		WriteEntityRefImpl(name);
	}

	private void WriteStringFragment(string str, int offset, int count, char[] helperBuffer)
	{
		int num = helperBuffer.Length;
		while (count > 0)
		{
			int num2 = count;
			if (num2 > num)
			{
				num2 = num;
			}
			str.CopyTo(offset, helperBuffer, 0, num2);
			_textWriter.Write(helperBuffer, 0, num2);
			offset += num2;
			count -= num2;
		}
	}

	private void WriteCharEntityImpl(char ch)
	{
		int num = ch;
		WriteCharEntityImpl(num.ToString("X", NumberFormatInfo.InvariantInfo));
	}

	private void WriteCharEntityImpl(string strVal)
	{
		_textWriter.Write("&#x");
		_textWriter.Write(strVal);
		_textWriter.Write(';');
	}

	private void WriteEntityRefImpl(string name)
	{
		_textWriter.Write('&');
		_textWriter.Write(name);
		_textWriter.Write(';');
	}
}
