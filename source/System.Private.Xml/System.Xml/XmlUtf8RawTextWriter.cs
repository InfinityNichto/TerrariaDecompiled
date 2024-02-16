using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal class XmlUtf8RawTextWriter : XmlRawWriter
{
	private readonly bool _useAsync;

	protected byte[] _bufBytes;

	protected Stream _stream;

	protected Encoding _encoding;

	protected int _bufPos = 1;

	protected int _textPos = 1;

	protected int _contentPos;

	protected int _cdataPos;

	protected int _attrEndPos;

	protected int _bufLen = 6144;

	protected bool _writeToNull;

	protected bool _hadDoubleBracket;

	protected bool _inAttributeValue;

	protected NewLineHandling _newLineHandling;

	protected bool _closeOutput;

	protected bool _omitXmlDeclaration;

	protected string _newLineChars;

	protected bool _checkCharacters;

	protected XmlStandalone _standalone;

	protected XmlOutputMethod _outputMethod;

	protected bool _autoXmlDeclaration;

	protected bool _mergeCDataSections;

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.Encoding = _encoding;
			xmlWriterSettings.OmitXmlDeclaration = _omitXmlDeclaration;
			xmlWriterSettings.NewLineHandling = _newLineHandling;
			xmlWriterSettings.NewLineChars = _newLineChars;
			xmlWriterSettings.CloseOutput = _closeOutput;
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Auto;
			xmlWriterSettings.CheckCharacters = _checkCharacters;
			xmlWriterSettings.AutoXmlDeclaration = _autoXmlDeclaration;
			xmlWriterSettings.Standalone = _standalone;
			xmlWriterSettings.OutputMethod = _outputMethod;
			xmlWriterSettings.ReadOnly = true;
			return xmlWriterSettings;
		}
	}

	internal override bool SupportsNamespaceDeclarationInChunks => true;

	protected XmlUtf8RawTextWriter(XmlWriterSettings settings)
	{
		_useAsync = settings.Async;
		_newLineHandling = settings.NewLineHandling;
		_omitXmlDeclaration = settings.OmitXmlDeclaration;
		_newLineChars = settings.NewLineChars;
		_checkCharacters = settings.CheckCharacters;
		_closeOutput = settings.CloseOutput;
		_standalone = settings.Standalone;
		_outputMethod = settings.OutputMethod;
		_mergeCDataSections = settings.MergeCDataSections;
		if (_checkCharacters && _newLineHandling == NewLineHandling.Replace)
		{
			ValidateContentChars(_newLineChars, "NewLineChars", allowOnlyWhitespace: false);
		}
	}

	public XmlUtf8RawTextWriter(Stream stream, XmlWriterSettings settings)
		: this(settings)
	{
		_stream = stream;
		_encoding = settings.Encoding;
		if (settings.Async)
		{
			_bufLen = 65536;
		}
		_bufBytes = new byte[_bufLen + 32];
		if (!stream.CanSeek || stream.Position == 0L)
		{
			ReadOnlySpan<byte> preamble = _encoding.Preamble;
			if (preamble.Length != 0)
			{
				preamble.CopyTo(new Span<byte>(_bufBytes).Slice(1));
				_bufPos += preamble.Length;
				_textPos += preamble.Length;
			}
		}
		if (settings.AutoXmlDeclaration)
		{
			WriteXmlDeclaration(_standalone);
			_autoXmlDeclaration = true;
		}
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
		if (!_omitXmlDeclaration && !_autoXmlDeclaration)
		{
			RawText("<?xml version=\"");
			RawText("1.0");
			if (_encoding != null)
			{
				RawText("\" encoding=\"");
				RawText(_encoding.WebName);
			}
			if (standalone != 0)
			{
				RawText("\" standalone=\"");
				RawText((standalone == XmlStandalone.Yes) ? "yes" : "no");
			}
			RawText("\"?>");
		}
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
		if (!_omitXmlDeclaration && !_autoXmlDeclaration)
		{
			WriteProcessingInstruction("xml", xmldecl);
		}
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		RawText("<!DOCTYPE ");
		RawText(name);
		if (pubid != null)
		{
			RawText(" PUBLIC \"");
			RawText(pubid);
			RawText("\" \"");
			if (sysid != null)
			{
				RawText(sysid);
			}
			_bufBytes[_bufPos++] = 34;
		}
		else if (sysid != null)
		{
			RawText(" SYSTEM \"");
			RawText(sysid);
			_bufBytes[_bufPos++] = 34;
		}
		else
		{
			_bufBytes[_bufPos++] = 32;
		}
		if (subset != null)
		{
			_bufBytes[_bufPos++] = 91;
			RawText(subset);
			_bufBytes[_bufPos++] = 93;
		}
		_bufBytes[_bufPos++] = 62;
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		_bufBytes[_bufPos++] = 60;
		if (prefix != null && prefix.Length != 0)
		{
			RawText(prefix);
			_bufBytes[_bufPos++] = 58;
		}
		RawText(localName);
		_attrEndPos = _bufPos;
	}

	internal override void StartElementContent()
	{
		_bufBytes[_bufPos++] = 62;
		_contentPos = _bufPos;
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		if (_contentPos != _bufPos)
		{
			_bufBytes[_bufPos++] = 60;
			_bufBytes[_bufPos++] = 47;
			if (prefix != null && prefix.Length != 0)
			{
				RawText(prefix);
				_bufBytes[_bufPos++] = 58;
			}
			RawText(localName);
			_bufBytes[_bufPos++] = 62;
		}
		else
		{
			_bufPos--;
			_bufBytes[_bufPos++] = 32;
			_bufBytes[_bufPos++] = 47;
			_bufBytes[_bufPos++] = 62;
		}
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 47;
		if (prefix != null && prefix.Length != 0)
		{
			RawText(prefix);
			_bufBytes[_bufPos++] = 58;
		}
		RawText(localName);
		_bufBytes[_bufPos++] = 62;
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (_attrEndPos == _bufPos)
		{
			_bufBytes[_bufPos++] = 32;
		}
		if (prefix != null && prefix.Length > 0)
		{
			RawText(prefix);
			_bufBytes[_bufPos++] = 58;
		}
		RawText(localName);
		_bufBytes[_bufPos++] = 61;
		_bufBytes[_bufPos++] = 34;
		_inAttributeValue = true;
	}

	public override void WriteEndAttribute()
	{
		_bufBytes[_bufPos++] = 34;
		_inAttributeValue = false;
		_attrEndPos = _bufPos;
	}

	internal override void WriteNamespaceDeclaration(string prefix, string namespaceName)
	{
		WriteStartNamespaceDeclaration(prefix);
		WriteString(namespaceName);
		WriteEndNamespaceDeclaration();
	}

	internal override void WriteStartNamespaceDeclaration(string prefix)
	{
		if (prefix.Length == 0)
		{
			RawText(" xmlns=\"");
		}
		else
		{
			RawText(" xmlns:");
			RawText(prefix);
			_bufBytes[_bufPos++] = 61;
			_bufBytes[_bufPos++] = 34;
		}
		_inAttributeValue = true;
	}

	internal override void WriteEndNamespaceDeclaration()
	{
		_inAttributeValue = false;
		_bufBytes[_bufPos++] = 34;
		_attrEndPos = _bufPos;
	}

	public override void WriteCData(string text)
	{
		if (_mergeCDataSections && _bufPos == _cdataPos)
		{
			_bufPos -= 3;
		}
		else
		{
			_bufBytes[_bufPos++] = 60;
			_bufBytes[_bufPos++] = 33;
			_bufBytes[_bufPos++] = 91;
			_bufBytes[_bufPos++] = 67;
			_bufBytes[_bufPos++] = 68;
			_bufBytes[_bufPos++] = 65;
			_bufBytes[_bufPos++] = 84;
			_bufBytes[_bufPos++] = 65;
			_bufBytes[_bufPos++] = 91;
		}
		WriteCDataSection(text);
		_bufBytes[_bufPos++] = 93;
		_bufBytes[_bufPos++] = 93;
		_bufBytes[_bufPos++] = 62;
		_textPos = _bufPos;
		_cdataPos = _bufPos;
	}

	public override void WriteComment(string text)
	{
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 33;
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 45;
		WriteCommentOrPi(text, 45);
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 62;
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 63;
		RawText(name);
		if (text.Length > 0)
		{
			_bufBytes[_bufPos++] = 32;
			WriteCommentOrPi(text, 63);
		}
		_bufBytes[_bufPos++] = 63;
		_bufBytes[_bufPos++] = 62;
	}

	public override void WriteEntityRef(string name)
	{
		_bufBytes[_bufPos++] = 38;
		RawText(name);
		_bufBytes[_bufPos++] = 59;
		if (_bufPos > _bufLen)
		{
			FlushBuffer();
		}
		_textPos = _bufPos;
	}

	public override void WriteCharEntity(char ch)
	{
		int num = ch;
		string s = num.ToString("X", NumberFormatInfo.InvariantInfo);
		if (_checkCharacters && !XmlCharType.IsCharData(ch))
		{
			throw XmlConvert.CreateInvalidCharException(ch, '\0');
		}
		_bufBytes[_bufPos++] = 38;
		_bufBytes[_bufPos++] = 35;
		_bufBytes[_bufPos++] = 120;
		RawText(s);
		_bufBytes[_bufPos++] = 59;
		if (_bufPos > _bufLen)
		{
			FlushBuffer();
		}
		_textPos = _bufPos;
	}

	public unsafe override void WriteWhitespace(string ws)
	{
		fixed (char* ptr = ws)
		{
			char* pSrcEnd = ptr + ws.Length;
			if (_inAttributeValue)
			{
				WriteAttributeTextBlock(ptr, pSrcEnd);
			}
			else
			{
				WriteElementTextBlock(ptr, pSrcEnd);
			}
		}
	}

	public unsafe override void WriteString(string text)
	{
		fixed (char* ptr = text)
		{
			char* pSrcEnd = ptr + text.Length;
			if (_inAttributeValue)
			{
				WriteAttributeTextBlock(ptr, pSrcEnd);
			}
			else
			{
				WriteElementTextBlock(ptr, pSrcEnd);
			}
		}
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		int num = XmlCharType.CombineSurrogateChar(lowChar, highChar);
		_bufBytes[_bufPos++] = 38;
		_bufBytes[_bufPos++] = 35;
		_bufBytes[_bufPos++] = 120;
		RawText(num.ToString("X", NumberFormatInfo.InvariantInfo));
		_bufBytes[_bufPos++] = 59;
		_textPos = _bufPos;
	}

	public unsafe override void WriteChars(char[] buffer, int index, int count)
	{
		fixed (char* ptr = &buffer[index])
		{
			if (_inAttributeValue)
			{
				WriteAttributeTextBlock(ptr, ptr + count);
			}
			else
			{
				WriteElementTextBlock(ptr, ptr + count);
			}
		}
	}

	public unsafe override void WriteRaw(char[] buffer, int index, int count)
	{
		fixed (char* ptr = &buffer[index])
		{
			WriteRawWithCharChecking(ptr, ptr + count);
		}
		_textPos = _bufPos;
	}

	public unsafe override void WriteRaw(string data)
	{
		fixed (char* ptr = data)
		{
			WriteRawWithCharChecking(ptr, ptr + data.Length);
		}
		_textPos = _bufPos;
	}

	public override void Close()
	{
		try
		{
			FlushBuffer();
			FlushEncoder();
		}
		finally
		{
			_writeToNull = true;
			if (_stream != null)
			{
				try
				{
					_stream.Flush();
				}
				finally
				{
					try
					{
						if (_closeOutput)
						{
							_stream.Dispose();
						}
					}
					finally
					{
						_stream = null;
					}
				}
			}
		}
	}

	public override void Flush()
	{
		FlushBuffer();
		FlushEncoder();
		if (_stream != null)
		{
			_stream.Flush();
		}
	}

	protected virtual void FlushBuffer()
	{
		try
		{
			if (!_writeToNull && _bufPos - 1 > 0)
			{
				_stream.Write(_bufBytes, 1, _bufPos - 1);
			}
		}
		catch
		{
			_writeToNull = true;
			throw;
		}
		finally
		{
			_bufBytes[0] = _bufBytes[_bufPos - 1];
			if (IsSurrogateByte(_bufBytes[0]))
			{
				_bufBytes[1] = _bufBytes[_bufPos];
				_bufBytes[2] = _bufBytes[_bufPos + 1];
				_bufBytes[3] = _bufBytes[_bufPos + 2];
			}
			_textPos = ((_textPos == _bufPos) ? 1 : 0);
			_attrEndPos = ((_attrEndPos == _bufPos) ? 1 : 0);
			_contentPos = 0;
			_cdataPos = 0;
			_bufPos = 1;
		}
	}

	private void FlushEncoder()
	{
	}

	protected unsafe void WriteAttributeTextBlock(char* pSrc, char* pSrcEnd)
	{
		fixed (byte* ptr = _bufBytes)
		{
			byte* ptr2 = ptr + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)) && num <= 127)
				{
					*ptr2 = (byte)num;
					ptr2++;
					pSrc++;
				}
				if (pSrc >= pSrcEnd)
				{
					break;
				}
				if (ptr2 >= ptr3)
				{
					_bufPos = (int)(ptr2 - ptr);
					FlushBuffer();
					ptr2 = ptr + 1;
					continue;
				}
				switch (num)
				{
				case 38:
					ptr2 = AmpEntity(ptr2);
					break;
				case 60:
					ptr2 = LtEntity(ptr2);
					break;
				case 62:
					ptr2 = GtEntity(ptr2);
					break;
				case 34:
					ptr2 = QuoteEntity(ptr2);
					break;
				case 39:
					*ptr2 = (byte)num;
					ptr2++;
					break;
				case 9:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr2 = (byte)num;
						ptr2++;
					}
					else
					{
						ptr2 = TabEntity(ptr2);
					}
					break;
				case 13:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr2 = (byte)num;
						ptr2++;
					}
					else
					{
						ptr2 = CarriageReturnEntity(ptr2);
					}
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr2 = (byte)num;
						ptr2++;
					}
					else
					{
						ptr2 = LineFeedEntity(ptr2);
					}
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr2 = EncodeSurrogate(pSrc, pSrcEnd, ptr2);
						pSrc += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr2 = InvalidXmlChar(num, ptr2, entitize: true);
						pSrc++;
					}
					else
					{
						ptr2 = EncodeMultibyteUTF8(num, ptr2);
						pSrc++;
					}
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(ptr2 - ptr);
		}
	}

	protected unsafe void WriteElementTextBlock(char* pSrc, char* pSrcEnd)
	{
		fixed (byte* ptr = _bufBytes)
		{
			byte* ptr2 = ptr + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)) && num <= 127)
				{
					*ptr2 = (byte)num;
					ptr2++;
					pSrc++;
				}
				if (pSrc >= pSrcEnd)
				{
					break;
				}
				if (ptr2 >= ptr3)
				{
					_bufPos = (int)(ptr2 - ptr);
					FlushBuffer();
					ptr2 = ptr + 1;
					continue;
				}
				switch (num)
				{
				case 38:
					ptr2 = AmpEntity(ptr2);
					break;
				case 60:
					ptr2 = LtEntity(ptr2);
					break;
				case 62:
					ptr2 = GtEntity(ptr2);
					break;
				case 9:
				case 34:
				case 39:
					*ptr2 = (byte)num;
					ptr2++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						ptr2 = WriteNewLine(ptr2);
						break;
					}
					*ptr2 = (byte)num;
					ptr2++;
					break;
				case 13:
					switch (_newLineHandling)
					{
					case NewLineHandling.Replace:
						if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
						{
							pSrc++;
						}
						ptr2 = WriteNewLine(ptr2);
						break;
					case NewLineHandling.Entitize:
						ptr2 = CarriageReturnEntity(ptr2);
						break;
					case NewLineHandling.None:
						*ptr2 = (byte)num;
						ptr2++;
						break;
					}
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr2 = EncodeSurrogate(pSrc, pSrcEnd, ptr2);
						pSrc += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr2 = InvalidXmlChar(num, ptr2, entitize: true);
						pSrc++;
					}
					else
					{
						ptr2 = EncodeMultibyteUTF8(num, ptr2);
						pSrc++;
					}
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(ptr2 - ptr);
			_textPos = _bufPos;
			_contentPos = 0;
		}
	}

	protected unsafe void RawText(string s)
	{
		fixed (char* ptr = s)
		{
			RawText(ptr, ptr + s.Length);
		}
	}

	protected unsafe void RawText(char* pSrcBegin, char* pSrcEnd)
	{
		fixed (byte* ptr = _bufBytes)
		{
			byte* ptr2 = ptr + _bufPos;
			char* ptr3 = pSrcBegin;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr2 + (pSrcEnd - ptr3);
				if (ptr4 > ptr + _bufLen)
				{
					ptr4 = ptr + _bufLen;
				}
				for (; ptr2 < ptr4; ptr2++)
				{
					if ((num = *ptr3) > 127)
					{
						break;
					}
					ptr3++;
					*ptr2 = (byte)num;
				}
				if (ptr3 >= pSrcEnd)
				{
					break;
				}
				if (ptr2 >= ptr4)
				{
					_bufPos = (int)(ptr2 - ptr);
					FlushBuffer();
					ptr2 = ptr + 1;
				}
				else if (XmlCharType.IsSurrogate(num))
				{
					ptr2 = EncodeSurrogate(ptr3, pSrcEnd, ptr2);
					ptr3 += 2;
				}
				else if (num <= 127 || num >= 65534)
				{
					ptr2 = InvalidXmlChar(num, ptr2, entitize: false);
					ptr3++;
				}
				else
				{
					ptr2 = EncodeMultibyteUTF8(num, ptr2);
					ptr3++;
				}
			}
			_bufPos = (int)(ptr2 - ptr);
		}
	}

	protected unsafe void WriteRawWithCharChecking(char* pSrcBegin, char* pSrcEnd)
	{
		fixed (byte* ptr2 = _bufBytes)
		{
			char* ptr = pSrcBegin;
			byte* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr3 + (pSrcEnd - ptr);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsTextChar((char)(num = *ptr)) && num <= 127)
				{
					*ptr3 = (byte)num;
					ptr3++;
					ptr++;
				}
				if (ptr >= pSrcEnd)
				{
					break;
				}
				if (ptr3 >= ptr4)
				{
					_bufPos = (int)(ptr3 - ptr2);
					FlushBuffer();
					ptr3 = ptr2 + 1;
					continue;
				}
				switch (num)
				{
				case 9:
				case 38:
				case 60:
				case 93:
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 13:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						if (ptr + 1 < pSrcEnd && ptr[1] == '\n')
						{
							ptr++;
						}
						ptr3 = WriteNewLine(ptr3);
					}
					else
					{
						*ptr3 = (byte)num;
						ptr3++;
					}
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						ptr3 = WriteNewLine(ptr3);
						break;
					}
					*ptr3 = (byte)num;
					ptr3++;
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr3 = EncodeSurrogate(ptr, pSrcEnd, ptr3);
						ptr += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr3 = InvalidXmlChar(num, ptr3, entitize: false);
						ptr++;
					}
					else
					{
						ptr3 = EncodeMultibyteUTF8(num, ptr3);
						ptr++;
					}
					continue;
				}
				ptr++;
			}
			_bufPos = (int)(ptr3 - ptr2);
		}
	}

	protected unsafe void WriteCommentOrPi(string text, int stopChar)
	{
		if (text.Length == 0)
		{
			if (_bufPos >= _bufLen)
			{
				FlushBuffer();
			}
			return;
		}
		fixed (char* ptr2 = text)
		{
			byte[] bufBytes = _bufBytes;
			fixed (byte[] array = bufBytes)
			{
				byte* ptr = (byte*)((bufBytes != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr3 = ptr2;
				char* ptr4 = ptr2 + text.Length;
				byte* ptr5 = ptr + _bufPos;
				int num = 0;
				while (true)
				{
					byte* ptr6 = ptr5 + (ptr4 - ptr3);
					if (ptr6 > ptr + _bufLen)
					{
						ptr6 = ptr + _bufLen;
					}
					while (ptr5 < ptr6 && XmlCharType.IsTextChar((char)(num = *ptr3)) && num != stopChar && num <= 127)
					{
						*ptr5 = (byte)num;
						ptr5++;
						ptr3++;
					}
					if (ptr3 >= ptr4)
					{
						break;
					}
					if (ptr5 >= ptr6)
					{
						_bufPos = (int)(ptr5 - ptr);
						FlushBuffer();
						ptr5 = ptr + 1;
						continue;
					}
					switch (num)
					{
					case 45:
						*ptr5 = 45;
						ptr5++;
						if (num == stopChar && (ptr3 + 1 == ptr4 || ptr3[1] == '-'))
						{
							*ptr5 = 32;
							ptr5++;
						}
						break;
					case 63:
						*ptr5 = 63;
						ptr5++;
						if (num == stopChar && ptr3 + 1 < ptr4 && ptr3[1] == '>')
						{
							*ptr5 = 32;
							ptr5++;
						}
						break;
					case 93:
						*ptr5 = 93;
						ptr5++;
						break;
					case 13:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							if (ptr3 + 1 < ptr4 && ptr3[1] == '\n')
							{
								ptr3++;
							}
							ptr5 = WriteNewLine(ptr5);
						}
						else
						{
							*ptr5 = (byte)num;
							ptr5++;
						}
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							ptr5 = WriteNewLine(ptr5);
							break;
						}
						*ptr5 = (byte)num;
						ptr5++;
						break;
					case 9:
					case 38:
					case 60:
						*ptr5 = (byte)num;
						ptr5++;
						break;
					default:
						if (XmlCharType.IsSurrogate(num))
						{
							ptr5 = EncodeSurrogate(ptr3, ptr4, ptr5);
							ptr3 += 2;
						}
						else if (num <= 127 || num >= 65534)
						{
							ptr5 = InvalidXmlChar(num, ptr5, entitize: false);
							ptr3++;
						}
						else
						{
							ptr5 = EncodeMultibyteUTF8(num, ptr5);
							ptr3++;
						}
						continue;
					}
					ptr3++;
				}
				_bufPos = (int)(ptr5 - ptr);
			}
		}
	}

	protected unsafe void WriteCDataSection(string text)
	{
		if (text.Length == 0)
		{
			if (_bufPos >= _bufLen)
			{
				FlushBuffer();
			}
			return;
		}
		fixed (char* ptr2 = text)
		{
			byte[] bufBytes = _bufBytes;
			fixed (byte[] array = bufBytes)
			{
				byte* ptr = (byte*)((bufBytes != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr3 = ptr2;
				char* ptr4 = ptr2 + text.Length;
				byte* ptr5 = ptr + _bufPos;
				int num = 0;
				while (true)
				{
					byte* ptr6 = ptr5 + (ptr4 - ptr3);
					if (ptr6 > ptr + _bufLen)
					{
						ptr6 = ptr + _bufLen;
					}
					while (ptr5 < ptr6 && XmlCharType.IsAttributeValueChar((char)(num = *ptr3)) && num != 93 && num <= 127)
					{
						*ptr5 = (byte)num;
						ptr5++;
						ptr3++;
					}
					if (ptr3 >= ptr4)
					{
						break;
					}
					if (ptr5 >= ptr6)
					{
						_bufPos = (int)(ptr5 - ptr);
						FlushBuffer();
						ptr5 = ptr + 1;
						continue;
					}
					switch (num)
					{
					case 62:
						if (_hadDoubleBracket && ptr5[-1] == 93)
						{
							ptr5 = RawEndCData(ptr5);
							ptr5 = RawStartCData(ptr5);
						}
						*ptr5 = 62;
						ptr5++;
						break;
					case 93:
						if (ptr5[-1] == 93)
						{
							_hadDoubleBracket = true;
						}
						else
						{
							_hadDoubleBracket = false;
						}
						*ptr5 = 93;
						ptr5++;
						break;
					case 13:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							if (ptr3 + 1 < ptr4 && ptr3[1] == '\n')
							{
								ptr3++;
							}
							ptr5 = WriteNewLine(ptr5);
						}
						else
						{
							*ptr5 = (byte)num;
							ptr5++;
						}
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							ptr5 = WriteNewLine(ptr5);
							break;
						}
						*ptr5 = (byte)num;
						ptr5++;
						break;
					case 9:
					case 34:
					case 38:
					case 39:
					case 60:
						*ptr5 = (byte)num;
						ptr5++;
						break;
					default:
						if (XmlCharType.IsSurrogate(num))
						{
							ptr5 = EncodeSurrogate(ptr3, ptr4, ptr5);
							ptr3 += 2;
						}
						else if (num <= 127 || num >= 65534)
						{
							ptr5 = InvalidXmlChar(num, ptr5, entitize: false);
							ptr3++;
						}
						else
						{
							ptr5 = EncodeMultibyteUTF8(num, ptr5);
							ptr3++;
						}
						continue;
					}
					ptr3++;
				}
				_bufPos = (int)(ptr5 - ptr);
			}
		}
	}

	private static bool IsSurrogateByte(byte b)
	{
		return (b & 0xF8) == 240;
	}

	private unsafe static byte* EncodeSurrogate(char* pSrc, char* pSrcEnd, byte* pDst)
	{
		int num = *pSrc;
		if (num <= 56319)
		{
			if (pSrc + 1 < pSrcEnd)
			{
				int num2 = pSrc[1];
				if (num2 >= 56320 && (System.LocalAppContextSwitches.DontThrowOnInvalidSurrogatePairs || num2 <= 57343))
				{
					num = XmlCharType.CombineSurrogateChar(num2, num);
					*pDst = (byte)(0xF0u | (uint)(num >> 18));
					pDst[1] = (byte)(0x80u | ((uint)(num >> 12) & 0x3Fu));
					pDst[2] = (byte)(0x80u | ((uint)(num >> 6) & 0x3Fu));
					pDst[3] = (byte)(0x80u | ((uint)num & 0x3Fu));
					pDst += 4;
					return pDst;
				}
				throw XmlConvert.CreateInvalidSurrogatePairException((char)num2, (char)num);
			}
			throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
		}
		throw XmlConvert.CreateInvalidHighSurrogateCharException((char)num);
	}

	private unsafe byte* InvalidXmlChar(int ch, byte* pDst, bool entitize)
	{
		if (_checkCharacters)
		{
			throw XmlConvert.CreateInvalidCharException((char)ch, '\0');
		}
		if (entitize)
		{
			return CharEntity(pDst, (char)ch);
		}
		if (ch < 128)
		{
			*pDst = (byte)ch;
			pDst++;
		}
		else
		{
			pDst = EncodeMultibyteUTF8(ch, pDst);
		}
		return pDst;
	}

	internal unsafe void EncodeChar(ref char* pSrc, char* pSrcEnd, ref byte* pDst)
	{
		int num = *pSrc;
		if (XmlCharType.IsSurrogate(num))
		{
			pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
			pSrc += 2;
		}
		else if (num <= 127 || num >= 65534)
		{
			pDst = InvalidXmlChar(num, pDst, entitize: false);
			pSrc++;
		}
		else
		{
			pDst = EncodeMultibyteUTF8(num, pDst);
			pSrc++;
		}
	}

	internal unsafe static byte* EncodeMultibyteUTF8(int ch, byte* pDst)
	{
		if (ch < 2048)
		{
			*pDst = (byte)(0xFFFFFFC0u | (uint)(ch >> 6));
		}
		else
		{
			*pDst = (byte)(0xFFFFFFE0u | (uint)(ch >> 12));
			pDst++;
			*pDst = (byte)(0xFFFFFF80u | ((uint)(ch >> 6) & 0x3Fu));
		}
		pDst++;
		*pDst = (byte)(0x80u | ((uint)ch & 0x3Fu));
		return pDst + 1;
	}

	internal unsafe static void CharToUTF8(ref char* pSrc, char* pSrcEnd, ref byte* pDst)
	{
		int num = *pSrc;
		if (num <= 127)
		{
			*pDst = (byte)num;
			pDst++;
			pSrc++;
		}
		else if (XmlCharType.IsSurrogate(num))
		{
			pDst = EncodeSurrogate(pSrc, pSrcEnd, pDst);
			pSrc += 2;
		}
		else
		{
			pDst = EncodeMultibyteUTF8(num, pDst);
			pSrc++;
		}
	}

	protected unsafe byte* WriteNewLine(byte* pDst)
	{
		fixed (byte* ptr = _bufBytes)
		{
			_bufPos = (int)(pDst - ptr);
			RawText(_newLineChars);
			return ptr + _bufPos;
		}
	}

	protected unsafe static byte* LtEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 108;
		pDst[2] = 116;
		pDst[3] = 59;
		return pDst + 4;
	}

	protected unsafe static byte* GtEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 103;
		pDst[2] = 116;
		pDst[3] = 59;
		return pDst + 4;
	}

	protected unsafe static byte* AmpEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 97;
		pDst[2] = 109;
		pDst[3] = 112;
		pDst[4] = 59;
		return pDst + 5;
	}

	protected unsafe static byte* QuoteEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 113;
		pDst[2] = 117;
		pDst[3] = 111;
		pDst[4] = 116;
		pDst[5] = 59;
		return pDst + 6;
	}

	protected unsafe static byte* TabEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 35;
		pDst[2] = 120;
		pDst[3] = 57;
		pDst[4] = 59;
		return pDst + 5;
	}

	protected unsafe static byte* LineFeedEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 35;
		pDst[2] = 120;
		pDst[3] = 65;
		pDst[4] = 59;
		return pDst + 5;
	}

	protected unsafe static byte* CarriageReturnEntity(byte* pDst)
	{
		*pDst = 38;
		pDst[1] = 35;
		pDst[2] = 120;
		pDst[3] = 68;
		pDst[4] = 59;
		return pDst + 5;
	}

	private unsafe static byte* CharEntity(byte* pDst, char ch)
	{
		int num = ch;
		string text = num.ToString("X", NumberFormatInfo.InvariantInfo);
		*pDst = 38;
		pDst[1] = 35;
		pDst[2] = 120;
		pDst += 3;
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr;
			while ((*(pDst++) = (byte)(*(ptr2++))) != 0)
			{
			}
		}
		pDst[-1] = 59;
		return pDst;
	}

	protected unsafe static byte* RawStartCData(byte* pDst)
	{
		*pDst = 60;
		pDst[1] = 33;
		pDst[2] = 91;
		pDst[3] = 67;
		pDst[4] = 68;
		pDst[5] = 65;
		pDst[6] = 84;
		pDst[7] = 65;
		pDst[8] = 91;
		return pDst + 9;
	}

	protected unsafe static byte* RawEndCData(byte* pDst)
	{
		*pDst = 93;
		pDst[1] = 93;
		pDst[2] = 62;
		return pDst + 3;
	}

	protected void ValidateContentChars(string chars, string propertyName, bool allowOnlyWhitespace)
	{
		if (allowOnlyWhitespace)
		{
			if (!XmlCharType.IsOnlyWhitespace(chars))
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xml_IndentCharsNotWhitespace, propertyName));
			}
			return;
		}
		string text = null;
		int num = 0;
		object[] args;
		while (true)
		{
			if (num >= chars.Length)
			{
				return;
			}
			if (!XmlCharType.IsTextChar(chars[num]))
			{
				switch (chars[num])
				{
				case '&':
				case '<':
				case ']':
				{
					string xml_InvalidCharacter = System.SR.Xml_InvalidCharacter;
					args = XmlException.BuildCharExceptionArgs(chars, num);
					text = System.SR.Format(xml_InvalidCharacter, args);
					break;
				}
				default:
					if (XmlCharType.IsHighSurrogate(chars[num]))
					{
						if (num + 1 < chars.Length && XmlCharType.IsLowSurrogate(chars[num + 1]))
						{
							num++;
							goto IL_00f6;
						}
						text = System.SR.Xml_InvalidSurrogateMissingLowChar;
					}
					else
					{
						if (!XmlCharType.IsLowSurrogate(chars[num]))
						{
							goto IL_00f6;
						}
						text = System.SR.Format(System.SR.Xml_InvalidSurrogateHighChar, ((uint)chars[num]).ToString("X", CultureInfo.InvariantCulture));
					}
					break;
				case '\t':
				case '\n':
				case '\r':
					goto IL_00f6;
				}
				break;
			}
			goto IL_00f6;
			IL_00f6:
			num++;
		}
		string xml_InvalidCharsInIndent = System.SR.Xml_InvalidCharsInIndent;
		args = new string[2] { propertyName, text };
		throw new ArgumentException(System.SR.Format(xml_InvalidCharsInIndent, args));
	}

	protected void CheckAsyncCall()
	{
		if (!_useAsync)
		{
			throw new InvalidOperationException(System.SR.Xml_WriterAsyncNotSetException);
		}
	}

	internal override async Task WriteXmlDeclarationAsync(XmlStandalone standalone)
	{
		CheckAsyncCall();
		if (!_omitXmlDeclaration && !_autoXmlDeclaration)
		{
			await RawTextAsync("<?xml version=\"").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync("1.0").ConfigureAwait(continueOnCapturedContext: false);
			if (_encoding != null)
			{
				await RawTextAsync("\" encoding=\"").ConfigureAwait(continueOnCapturedContext: false);
				await RawTextAsync(_encoding.WebName).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (standalone != 0)
			{
				await RawTextAsync("\" standalone=\"").ConfigureAwait(continueOnCapturedContext: false);
				await RawTextAsync((standalone == XmlStandalone.Yes) ? "yes" : "no").ConfigureAwait(continueOnCapturedContext: false);
			}
			await RawTextAsync("\"?>").ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	internal override Task WriteXmlDeclarationAsync(string xmldecl)
	{
		CheckAsyncCall();
		if (!_omitXmlDeclaration && !_autoXmlDeclaration)
		{
			return WriteProcessingInstructionAsync("xml", xmldecl);
		}
		return Task.CompletedTask;
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_writeToNull = true;
			if (_stream != null)
			{
				try
				{
					await _stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					try
					{
						if (_closeOutput)
						{
							await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					finally
					{
						_stream = null;
					}
				}
			}
		}
	}

	public override async Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		CheckAsyncCall();
		await RawTextAsync("<!DOCTYPE ").ConfigureAwait(continueOnCapturedContext: false);
		await RawTextAsync(name).ConfigureAwait(continueOnCapturedContext: false);
		if (pubid != null)
		{
			await RawTextAsync(" PUBLIC \"").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync(pubid).ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync("\" \"").ConfigureAwait(continueOnCapturedContext: false);
			if (sysid != null)
			{
				await RawTextAsync(sysid).ConfigureAwait(continueOnCapturedContext: false);
			}
			_bufBytes[_bufPos++] = 34;
		}
		else if (sysid != null)
		{
			await RawTextAsync(" SYSTEM \"").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync(sysid).ConfigureAwait(continueOnCapturedContext: false);
			_bufBytes[_bufPos++] = 34;
		}
		else
		{
			_bufBytes[_bufPos++] = 32;
		}
		if (subset != null)
		{
			_bufBytes[_bufPos++] = 91;
			await RawTextAsync(subset).ConfigureAwait(continueOnCapturedContext: false);
			_bufBytes[_bufPos++] = 93;
		}
		_bufBytes[_bufPos++] = 62;
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 60;
		Task task = ((prefix == null || prefix.Length == 0) ? RawTextAsync(localName) : RawTextAsync(prefix, ":", localName));
		return task.CallVoidFuncWhenFinishAsync(delegate(XmlUtf8RawTextWriter thisRef)
		{
			thisRef.WriteStartElementAsync_SetAttEndPos();
		}, this);
	}

	private void WriteStartElementAsync_SetAttEndPos()
	{
		_attrEndPos = _bufPos;
	}

	internal override Task WriteEndElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_contentPos != _bufPos)
		{
			_bufBytes[_bufPos++] = 60;
			_bufBytes[_bufPos++] = 47;
			if (prefix != null && prefix.Length != 0)
			{
				return RawTextAsync(prefix, ":", localName, ">");
			}
			return RawTextAsync(localName, ">");
		}
		_bufPos--;
		_bufBytes[_bufPos++] = 32;
		_bufBytes[_bufPos++] = 47;
		_bufBytes[_bufPos++] = 62;
		return Task.CompletedTask;
	}

	internal override Task WriteFullEndElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 47;
		if (prefix != null && prefix.Length != 0)
		{
			return RawTextAsync(prefix, ":", localName, ">");
		}
		return RawTextAsync(localName, ">");
	}

	protected internal override Task WriteStartAttributeAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_attrEndPos == _bufPos)
		{
			_bufBytes[_bufPos++] = 32;
		}
		Task task = ((prefix == null || prefix.Length <= 0) ? RawTextAsync(localName) : RawTextAsync(prefix, ":", localName));
		return task.CallVoidFuncWhenFinishAsync(delegate(XmlUtf8RawTextWriter thisRef)
		{
			thisRef.WriteStartAttribute_SetInAttribute();
		}, this);
	}

	private void WriteStartAttribute_SetInAttribute()
	{
		_bufBytes[_bufPos++] = 61;
		_bufBytes[_bufPos++] = 34;
		_inAttributeValue = true;
	}

	protected internal override Task WriteEndAttributeAsync()
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 34;
		_inAttributeValue = false;
		_attrEndPos = _bufPos;
		return Task.CompletedTask;
	}

	internal override async Task WriteNamespaceDeclarationAsync(string prefix, string namespaceName)
	{
		CheckAsyncCall();
		await WriteStartNamespaceDeclarationAsync(prefix).ConfigureAwait(continueOnCapturedContext: false);
		await WriteStringAsync(namespaceName).ConfigureAwait(continueOnCapturedContext: false);
		await WriteEndNamespaceDeclarationAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	internal override async Task WriteStartNamespaceDeclarationAsync(string prefix)
	{
		CheckAsyncCall();
		if (prefix.Length == 0)
		{
			await RawTextAsync(" xmlns=\"").ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await RawTextAsync(" xmlns:").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync(prefix).ConfigureAwait(continueOnCapturedContext: false);
			_bufBytes[_bufPos++] = 61;
			_bufBytes[_bufPos++] = 34;
		}
		_inAttributeValue = true;
	}

	internal override Task WriteEndNamespaceDeclarationAsync()
	{
		CheckAsyncCall();
		_inAttributeValue = false;
		_bufBytes[_bufPos++] = 34;
		_attrEndPos = _bufPos;
		return Task.CompletedTask;
	}

	public override async Task WriteCDataAsync(string text)
	{
		CheckAsyncCall();
		if (_mergeCDataSections && _bufPos == _cdataPos)
		{
			_bufPos -= 3;
		}
		else
		{
			_bufBytes[_bufPos++] = 60;
			_bufBytes[_bufPos++] = 33;
			_bufBytes[_bufPos++] = 91;
			_bufBytes[_bufPos++] = 67;
			_bufBytes[_bufPos++] = 68;
			_bufBytes[_bufPos++] = 65;
			_bufBytes[_bufPos++] = 84;
			_bufBytes[_bufPos++] = 65;
			_bufBytes[_bufPos++] = 91;
		}
		await WriteCDataSectionAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		_bufBytes[_bufPos++] = 93;
		_bufBytes[_bufPos++] = 93;
		_bufBytes[_bufPos++] = 62;
		_textPos = _bufPos;
		_cdataPos = _bufPos;
	}

	public override async Task WriteCommentAsync(string text)
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 33;
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 45;
		await WriteCommentOrPiAsync(text, 45).ConfigureAwait(continueOnCapturedContext: false);
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 45;
		_bufBytes[_bufPos++] = 62;
	}

	public override async Task WriteProcessingInstructionAsync(string name, string text)
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 63;
		await RawTextAsync(name).ConfigureAwait(continueOnCapturedContext: false);
		if (text.Length > 0)
		{
			_bufBytes[_bufPos++] = 32;
			await WriteCommentOrPiAsync(text, 63).ConfigureAwait(continueOnCapturedContext: false);
		}
		_bufBytes[_bufPos++] = 63;
		_bufBytes[_bufPos++] = 62;
	}

	public override async Task WriteEntityRefAsync(string name)
	{
		CheckAsyncCall();
		_bufBytes[_bufPos++] = 38;
		await RawTextAsync(name).ConfigureAwait(continueOnCapturedContext: false);
		_bufBytes[_bufPos++] = 59;
		if (_bufPos > _bufLen)
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_textPos = _bufPos;
	}

	public override async Task WriteCharEntityAsync(char ch)
	{
		CheckAsyncCall();
		int num = ch;
		string text = num.ToString("X", NumberFormatInfo.InvariantInfo);
		if (_checkCharacters && !XmlCharType.IsCharData(ch))
		{
			throw XmlConvert.CreateInvalidCharException(ch, '\0');
		}
		_bufBytes[_bufPos++] = 38;
		_bufBytes[_bufPos++] = 35;
		_bufBytes[_bufPos++] = 120;
		await RawTextAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		_bufBytes[_bufPos++] = 59;
		if (_bufPos > _bufLen)
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_textPos = _bufPos;
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		CheckAsyncCall();
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(ws);
		}
		return WriteElementTextBlockAsync(ws);
	}

	public override Task WriteStringAsync(string text)
	{
		CheckAsyncCall();
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(text);
		}
		return WriteElementTextBlockAsync(text);
	}

	public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		CheckAsyncCall();
		int num = XmlCharType.CombineSurrogateChar(lowChar, highChar);
		_bufBytes[_bufPos++] = 38;
		_bufBytes[_bufPos++] = 35;
		_bufBytes[_bufPos++] = 120;
		await RawTextAsync(num.ToString("X", NumberFormatInfo.InvariantInfo)).ConfigureAwait(continueOnCapturedContext: false);
		_bufBytes[_bufPos++] = 59;
		_textPos = _bufPos;
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(buffer, index, count);
		}
		return WriteElementTextBlockAsync(buffer, index, count);
	}

	public override async Task WriteRawAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		await WriteRawWithCharCheckingAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_textPos = _bufPos;
	}

	public override async Task WriteRawAsync(string data)
	{
		CheckAsyncCall();
		await WriteRawWithCharCheckingAsync(data).ConfigureAwait(continueOnCapturedContext: false);
		_textPos = _bufPos;
	}

	public override async Task FlushAsync()
	{
		CheckAsyncCall();
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (_stream != null)
		{
			await _stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	protected virtual async Task FlushBufferAsync()
	{
		try
		{
			if (!_writeToNull && _bufPos - 1 > 0)
			{
				await _stream.WriteAsync(_bufBytes.AsMemory(1, _bufPos - 1)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_writeToNull = true;
			throw;
		}
		finally
		{
			_bufBytes[0] = _bufBytes[_bufPos - 1];
			if (IsSurrogateByte(_bufBytes[0]))
			{
				_bufBytes[1] = _bufBytes[_bufPos];
				_bufBytes[2] = _bufBytes[_bufPos + 1];
				_bufBytes[3] = _bufBytes[_bufPos + 2];
			}
			_textPos = ((_textPos == _bufPos) ? 1 : 0);
			_attrEndPos = ((_attrEndPos == _bufPos) ? 1 : 0);
			_contentPos = 0;
			_cdataPos = 0;
			_bufPos = 1;
		}
	}

	protected unsafe int WriteAttributeTextBlockNoFlush(char* pSrc, char* pSrcEnd)
	{
		char* ptr = pSrc;
		fixed (byte* ptr2 = _bufBytes)
		{
			byte* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr3 + (pSrcEnd - pSrc);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)) && num <= 127)
				{
					*ptr3 = (byte)num;
					ptr3++;
					pSrc++;
				}
				if (pSrc >= pSrcEnd)
				{
					break;
				}
				if (ptr3 >= ptr4)
				{
					_bufPos = (int)(ptr3 - ptr2);
					return (int)(pSrc - ptr);
				}
				switch (num)
				{
				case 38:
					ptr3 = AmpEntity(ptr3);
					break;
				case 60:
					ptr3 = LtEntity(ptr3);
					break;
				case 62:
					ptr3 = GtEntity(ptr3);
					break;
				case 34:
					ptr3 = QuoteEntity(ptr3);
					break;
				case 39:
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 9:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr3 = (byte)num;
						ptr3++;
					}
					else
					{
						ptr3 = TabEntity(ptr3);
					}
					break;
				case 13:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr3 = (byte)num;
						ptr3++;
					}
					else
					{
						ptr3 = CarriageReturnEntity(ptr3);
					}
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr3 = (byte)num;
						ptr3++;
					}
					else
					{
						ptr3 = LineFeedEntity(ptr3);
					}
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr3 = EncodeSurrogate(pSrc, pSrcEnd, ptr3);
						pSrc += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr3 = InvalidXmlChar(num, ptr3, entitize: true);
						pSrc++;
					}
					else
					{
						ptr3 = EncodeMultibyteUTF8(num, ptr3);
						pSrc++;
					}
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(ptr3 - ptr2);
		}
		return -1;
	}

	protected unsafe int WriteAttributeTextBlockNoFlush(char[] chars, int index, int count)
	{
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = &chars[index])
		{
			char* ptr2 = ptr;
			char* pSrcEnd = ptr2 + count;
			return WriteAttributeTextBlockNoFlush(ptr2, pSrcEnd);
		}
	}

	protected unsafe int WriteAttributeTextBlockNoFlush(string text, int index, int count)
	{
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			char* pSrcEnd = ptr2 + count;
			return WriteAttributeTextBlockNoFlush(ptr2, pSrcEnd);
		}
	}

	protected async Task WriteAttributeTextBlockAsync(char[] chars, int index, int count)
	{
		int curIndex = index;
		int leftCount = count;
		int writeLen;
		do
		{
			writeLen = WriteAttributeTextBlockNoFlush(chars, curIndex, leftCount);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0);
	}

	protected Task WriteAttributeTextBlockAsync(string text)
	{
		int num = 0;
		int num2 = 0;
		int length = text.Length;
		num = WriteAttributeTextBlockNoFlush(text, num2, length);
		num2 += num;
		length -= num;
		if (num >= 0)
		{
			return _WriteAttributeTextBlockAsync(text, num2, length);
		}
		return Task.CompletedTask;
	}

	private async Task _WriteAttributeTextBlockAsync(string text, int curIndex, int leftCount)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		int writeLen;
		do
		{
			writeLen = WriteAttributeTextBlockNoFlush(text, curIndex, leftCount);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0);
	}

	protected unsafe int WriteElementTextBlockNoFlush(char* pSrc, char* pSrcEnd, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		char* ptr = pSrc;
		fixed (byte* ptr2 = _bufBytes)
		{
			byte* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr3 + (pSrcEnd - pSrc);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)) && num <= 127)
				{
					*ptr3 = (byte)num;
					ptr3++;
					pSrc++;
				}
				if (pSrc >= pSrcEnd)
				{
					break;
				}
				if (ptr3 >= ptr4)
				{
					_bufPos = (int)(ptr3 - ptr2);
					return (int)(pSrc - ptr);
				}
				switch (num)
				{
				case 38:
					ptr3 = AmpEntity(ptr3);
					break;
				case 60:
					ptr3 = LtEntity(ptr3);
					break;
				case 62:
					ptr3 = GtEntity(ptr3);
					break;
				case 9:
				case 34:
				case 39:
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(pSrc - ptr);
					}
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 13:
					switch (_newLineHandling)
					{
					case NewLineHandling.Replace:
						if (pSrc + 1 < pSrcEnd && pSrc[1] == '\n')
						{
							pSrc++;
						}
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(pSrc - ptr);
					case NewLineHandling.Entitize:
						ptr3 = CarriageReturnEntity(ptr3);
						break;
					case NewLineHandling.None:
						*ptr3 = (byte)num;
						ptr3++;
						break;
					}
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr3 = EncodeSurrogate(pSrc, pSrcEnd, ptr3);
						pSrc += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr3 = InvalidXmlChar(num, ptr3, entitize: true);
						pSrc++;
					}
					else
					{
						ptr3 = EncodeMultibyteUTF8(num, ptr3);
						pSrc++;
					}
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(ptr3 - ptr2);
			_textPos = _bufPos;
			_contentPos = 0;
		}
		return -1;
	}

	protected unsafe int WriteElementTextBlockNoFlush(char[] chars, int index, int count, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			_contentPos = 0;
			return -1;
		}
		fixed (char* ptr = &chars[index])
		{
			char* ptr2 = ptr;
			char* pSrcEnd = ptr2 + count;
			return WriteElementTextBlockNoFlush(ptr2, pSrcEnd, out needWriteNewLine);
		}
	}

	protected unsafe int WriteElementTextBlockNoFlush(string text, int index, int count, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			_contentPos = 0;
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			char* pSrcEnd = ptr2 + count;
			return WriteElementTextBlockNoFlush(ptr2, pSrcEnd, out needWriteNewLine);
		}
	}

	protected async Task WriteElementTextBlockAsync(char[] chars, int index, int count)
	{
		int curIndex = index;
		int leftCount = count;
		bool needWriteNewLine = false;
		int writeLen;
		do
		{
			writeLen = WriteElementTextBlockNoFlush(chars, curIndex, leftCount, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}

	protected Task WriteElementTextBlockAsync(string text)
	{
		int num = 0;
		int num2 = 0;
		int length = text.Length;
		bool needWriteNewLine = false;
		num = WriteElementTextBlockNoFlush(text, num2, length, out needWriteNewLine);
		num2 += num;
		length -= num;
		if (needWriteNewLine)
		{
			return _WriteElementTextBlockAsync(newLine: true, text, num2, length);
		}
		if (num >= 0)
		{
			return _WriteElementTextBlockAsync(newLine: false, text, num2, length);
		}
		return Task.CompletedTask;
	}

	private async Task _WriteElementTextBlockAsync(bool newLine, string text, int curIndex, int leftCount)
	{
		bool needWriteNewLine = false;
		if (!newLine)
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
			curIndex++;
			leftCount--;
		}
		int writeLen;
		do
		{
			writeLen = WriteElementTextBlockNoFlush(text, curIndex, leftCount, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}

	protected unsafe int RawTextNoFlush(char* pSrcBegin, char* pSrcEnd)
	{
		fixed (byte* ptr = _bufBytes)
		{
			byte* ptr2 = ptr + _bufPos;
			char* ptr3 = pSrcBegin;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr2 + (pSrcEnd - ptr3);
				if (ptr4 > ptr + _bufLen)
				{
					ptr4 = ptr + _bufLen;
				}
				for (; ptr2 < ptr4; ptr2++)
				{
					if ((num = *ptr3) > 127)
					{
						break;
					}
					ptr3++;
					*ptr2 = (byte)num;
				}
				if (ptr3 >= pSrcEnd)
				{
					break;
				}
				if (ptr2 >= ptr4)
				{
					_bufPos = (int)(ptr2 - ptr);
					return (int)(ptr3 - pSrcBegin);
				}
				if (XmlCharType.IsSurrogate(num))
				{
					ptr2 = EncodeSurrogate(ptr3, pSrcEnd, ptr2);
					ptr3 += 2;
				}
				else if (num <= 127 || num >= 65534)
				{
					ptr2 = InvalidXmlChar(num, ptr2, entitize: false);
					ptr3++;
				}
				else
				{
					ptr2 = EncodeMultibyteUTF8(num, ptr2);
					ptr3++;
				}
			}
			_bufPos = (int)(ptr2 - ptr);
		}
		return -1;
	}

	protected unsafe int RawTextNoFlush(string text, int index, int count)
	{
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			char* pSrcEnd = ptr2 + count;
			return RawTextNoFlush(ptr2, pSrcEnd);
		}
	}

	protected Task RawTextAsync(string text)
	{
		int num = RawTextNoFlush(text, 0, text.Length);
		if (num < 0)
		{
			return Task.CompletedTask;
		}
		return _RawTextAsync(text, num, text.Length - num);
	}

	protected Task RawTextAsync(string text1, string text2 = null, string text3 = null, string text4 = null)
	{
		int num = RawTextNoFlush(text1, 0, text1.Length);
		if (num >= 0)
		{
			return _RawTextAsync(text1, num, text1.Length - num, text2, text3, text4);
		}
		if (text2 != null)
		{
			num = RawTextNoFlush(text2, 0, text2.Length);
			if (num >= 0)
			{
				return _RawTextAsync(text2, num, text2.Length - num, text3, text4);
			}
		}
		if (text3 != null)
		{
			num = RawTextNoFlush(text3, 0, text3.Length);
			if (num >= 0)
			{
				return _RawTextAsync(text3, num, text3.Length - num, text4);
			}
		}
		if (text4 != null)
		{
			num = RawTextNoFlush(text4, 0, text4.Length);
			if (num >= 0)
			{
				return _RawTextAsync(text4, num, text4.Length - num);
			}
		}
		return Task.CompletedTask;
	}

	private async Task _RawTextAsync(string text1, int curIndex1, int leftCount1, string text2 = null, string text3 = null, string text4 = null)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		int writeLen;
		do
		{
			writeLen = RawTextNoFlush(text1, curIndex1, leftCount1);
			curIndex1 += writeLen;
			leftCount1 -= writeLen;
			if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0);
		if (text2 != null)
		{
			await RawTextAsync(text2, text3, text4).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	protected unsafe int WriteRawWithCharCheckingNoFlush(char* pSrcBegin, char* pSrcEnd, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		fixed (byte* ptr2 = _bufBytes)
		{
			char* ptr = pSrcBegin;
			byte* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				byte* ptr4 = ptr3 + (pSrcEnd - ptr);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsTextChar((char)(num = *ptr)) && num <= 127)
				{
					*ptr3 = (byte)num;
					ptr3++;
					ptr++;
				}
				if (ptr >= pSrcEnd)
				{
					break;
				}
				if (ptr3 >= ptr4)
				{
					_bufPos = (int)(ptr3 - ptr2);
					return (int)(ptr - pSrcBegin);
				}
				switch (num)
				{
				case 9:
				case 38:
				case 60:
				case 93:
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 13:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						if (ptr + 1 < pSrcEnd && ptr[1] == '\n')
						{
							ptr++;
						}
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(ptr - pSrcBegin);
					}
					*ptr3 = (byte)num;
					ptr3++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(ptr - pSrcBegin);
					}
					*ptr3 = (byte)num;
					ptr3++;
					break;
				default:
					if (XmlCharType.IsSurrogate(num))
					{
						ptr3 = EncodeSurrogate(ptr, pSrcEnd, ptr3);
						ptr += 2;
					}
					else if (num <= 127 || num >= 65534)
					{
						ptr3 = InvalidXmlChar(num, ptr3, entitize: false);
						ptr++;
					}
					else
					{
						ptr3 = EncodeMultibyteUTF8(num, ptr3);
						ptr++;
					}
					continue;
				}
				ptr++;
			}
			_bufPos = (int)(ptr3 - ptr2);
		}
		return -1;
	}

	protected unsafe int WriteRawWithCharCheckingNoFlush(char[] chars, int index, int count, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = &chars[index])
		{
			char* ptr2 = ptr;
			char* pSrcEnd = ptr2 + count;
			return WriteRawWithCharCheckingNoFlush(ptr2, pSrcEnd, out needWriteNewLine);
		}
	}

	protected unsafe int WriteRawWithCharCheckingNoFlush(string text, int index, int count, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			char* pSrcEnd = ptr2 + count;
			return WriteRawWithCharCheckingNoFlush(ptr2, pSrcEnd, out needWriteNewLine);
		}
	}

	protected async Task WriteRawWithCharCheckingAsync(char[] chars, int index, int count)
	{
		int curIndex = index;
		int leftCount = count;
		bool needWriteNewLine = false;
		int writeLen;
		do
		{
			writeLen = WriteRawWithCharCheckingNoFlush(chars, curIndex, leftCount, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}

	protected async Task WriteRawWithCharCheckingAsync(string text)
	{
		int curIndex = 0;
		int leftCount = text.Length;
		bool needWriteNewLine = false;
		int writeLen;
		do
		{
			writeLen = WriteRawWithCharCheckingNoFlush(text, curIndex, leftCount, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}

	protected unsafe int WriteCommentOrPiNoFlush(string text, int index, int count, int stopChar, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			byte[] bufBytes = _bufBytes;
			fixed (byte[] array = bufBytes)
			{
				byte* ptr3 = (byte*)((bufBytes != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr4 = ptr2;
				char* ptr5 = ptr4;
				char* ptr6 = ptr2 + count;
				byte* ptr7 = ptr3 + _bufPos;
				int num = 0;
				while (true)
				{
					byte* ptr8 = ptr7 + (ptr6 - ptr4);
					if (ptr8 > ptr3 + _bufLen)
					{
						ptr8 = ptr3 + _bufLen;
					}
					while (ptr7 < ptr8 && XmlCharType.IsTextChar((char)(num = *ptr4)) && num != stopChar && num <= 127)
					{
						*ptr7 = (byte)num;
						ptr7++;
						ptr4++;
					}
					if (ptr4 >= ptr6)
					{
						break;
					}
					if (ptr7 >= ptr8)
					{
						_bufPos = (int)(ptr7 - ptr3);
						return (int)(ptr4 - ptr5);
					}
					switch (num)
					{
					case 45:
						*ptr7 = 45;
						ptr7++;
						if (num == stopChar && (ptr4 + 1 == ptr6 || ptr4[1] == '-'))
						{
							*ptr7 = 32;
							ptr7++;
						}
						break;
					case 63:
						*ptr7 = 63;
						ptr7++;
						if (num == stopChar && ptr4 + 1 < ptr6 && ptr4[1] == '>')
						{
							*ptr7 = 32;
							ptr7++;
						}
						break;
					case 93:
						*ptr7 = 93;
						ptr7++;
						break;
					case 13:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							if (ptr4 + 1 < ptr6 && ptr4[1] == '\n')
							{
								ptr4++;
							}
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr5);
						}
						*ptr7 = (byte)num;
						ptr7++;
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr5);
						}
						*ptr7 = (byte)num;
						ptr7++;
						break;
					case 9:
					case 38:
					case 60:
						*ptr7 = (byte)num;
						ptr7++;
						break;
					default:
						if (XmlCharType.IsSurrogate(num))
						{
							ptr7 = EncodeSurrogate(ptr4, ptr6, ptr7);
							ptr4 += 2;
						}
						else if (num <= 127 || num >= 65534)
						{
							ptr7 = InvalidXmlChar(num, ptr7, entitize: false);
							ptr4++;
						}
						else
						{
							ptr7 = EncodeMultibyteUTF8(num, ptr7);
							ptr4++;
						}
						continue;
					}
					ptr4++;
				}
				_bufPos = (int)(ptr7 - ptr3);
			}
			return -1;
		}
	}

	protected async Task WriteCommentOrPiAsync(string text, int stopChar)
	{
		if (text.Length == 0)
		{
			if (_bufPos >= _bufLen)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			return;
		}
		int curIndex = 0;
		int leftCount = text.Length;
		bool needWriteNewLine = false;
		int writeLen;
		do
		{
			writeLen = WriteCommentOrPiNoFlush(text, curIndex, leftCount, stopChar, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}

	protected unsafe int WriteCDataSectionNoFlush(string text, int index, int count, out bool needWriteNewLine)
	{
		needWriteNewLine = false;
		if (count == 0)
		{
			return -1;
		}
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr + index;
			byte[] bufBytes = _bufBytes;
			fixed (byte[] array = bufBytes)
			{
				byte* ptr3 = (byte*)((bufBytes != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr4 = ptr2;
				char* ptr5 = ptr2 + count;
				char* ptr6 = ptr4;
				byte* ptr7 = ptr3 + _bufPos;
				int num = 0;
				while (true)
				{
					byte* ptr8 = ptr7 + (ptr5 - ptr4);
					if (ptr8 > ptr3 + _bufLen)
					{
						ptr8 = ptr3 + _bufLen;
					}
					while (ptr7 < ptr8 && XmlCharType.IsAttributeValueChar((char)(num = *ptr4)) && num != 93 && num <= 127)
					{
						*ptr7 = (byte)num;
						ptr7++;
						ptr4++;
					}
					if (ptr4 >= ptr5)
					{
						break;
					}
					if (ptr7 >= ptr8)
					{
						_bufPos = (int)(ptr7 - ptr3);
						return (int)(ptr4 - ptr6);
					}
					switch (num)
					{
					case 62:
						if (_hadDoubleBracket && ptr7[-1] == 93)
						{
							ptr7 = RawEndCData(ptr7);
							ptr7 = RawStartCData(ptr7);
						}
						*ptr7 = 62;
						ptr7++;
						break;
					case 93:
						if (ptr7[-1] == 93)
						{
							_hadDoubleBracket = true;
						}
						else
						{
							_hadDoubleBracket = false;
						}
						*ptr7 = 93;
						ptr7++;
						break;
					case 13:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							if (ptr4 + 1 < ptr5 && ptr4[1] == '\n')
							{
								ptr4++;
							}
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr6);
						}
						*ptr7 = (byte)num;
						ptr7++;
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr6);
						}
						*ptr7 = (byte)num;
						ptr7++;
						break;
					case 9:
					case 34:
					case 38:
					case 39:
					case 60:
						*ptr7 = (byte)num;
						ptr7++;
						break;
					default:
						if (XmlCharType.IsSurrogate(num))
						{
							ptr7 = EncodeSurrogate(ptr4, ptr5, ptr7);
							ptr4 += 2;
						}
						else if (num <= 127 || num >= 65534)
						{
							ptr7 = InvalidXmlChar(num, ptr7, entitize: false);
							ptr4++;
						}
						else
						{
							ptr7 = EncodeMultibyteUTF8(num, ptr7);
							ptr4++;
						}
						continue;
					}
					ptr4++;
				}
				_bufPos = (int)(ptr7 - ptr3);
			}
			return -1;
		}
	}

	protected async Task WriteCDataSectionAsync(string text)
	{
		if (text.Length == 0)
		{
			if (_bufPos >= _bufLen)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			return;
		}
		int curIndex = 0;
		int leftCount = text.Length;
		bool needWriteNewLine = false;
		int writeLen;
		do
		{
			writeLen = WriteCDataSectionNoFlush(text, curIndex, leftCount, out needWriteNewLine);
			curIndex += writeLen;
			leftCount -= writeLen;
			if (needWriteNewLine)
			{
				await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
				curIndex++;
				leftCount--;
			}
			else if (writeLen >= 0)
			{
				await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (writeLen >= 0 || needWriteNewLine);
	}
}
