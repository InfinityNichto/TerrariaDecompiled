using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal class XmlEncodedRawTextWriter : XmlRawWriter
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

	protected int _bufBytesUsed;

	protected char[] _bufChars;

	protected Encoder _encoder;

	protected TextWriter _writer;

	protected bool _trackTextContent;

	protected bool _inTextContent;

	private int _lastMarkPos;

	private int[] _textContentMarks;

	private readonly CharEntityEncoderFallback _charEntityFallback;

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

	protected XmlEncodedRawTextWriter(XmlWriterSettings settings)
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

	public XmlEncodedRawTextWriter(TextWriter writer, XmlWriterSettings settings)
		: this(settings)
	{
		_writer = writer;
		_encoding = writer.Encoding;
		if (settings.Async)
		{
			_bufLen = 65536;
		}
		_bufChars = new char[_bufLen + 32];
		if (settings.AutoXmlDeclaration)
		{
			WriteXmlDeclaration(_standalone);
			_autoXmlDeclaration = true;
		}
	}

	public XmlEncodedRawTextWriter(Stream stream, XmlWriterSettings settings)
		: this(settings)
	{
		_stream = stream;
		_encoding = settings.Encoding;
		if (settings.Async)
		{
			_bufLen = 65536;
		}
		_bufChars = new char[_bufLen + 32];
		_bufBytes = new byte[_bufChars.Length];
		_bufBytesUsed = 0;
		_trackTextContent = true;
		_inTextContent = false;
		_lastMarkPos = 0;
		_textContentMarks = new int[64];
		_textContentMarks[0] = 1;
		_charEntityFallback = new CharEntityEncoderFallback();
		ReadOnlySpan<byte> preamble = _encoding.Preamble;
		_encoding = (Encoding)settings.Encoding.Clone();
		_encoding.EncoderFallback = _charEntityFallback;
		_encoder = _encoding.GetEncoder();
		if ((!stream.CanSeek || stream.Position == 0L) && preamble.Length != 0)
		{
			_stream.Write(preamble);
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
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
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
			_bufChars[_bufPos++] = '"';
		}
		else if (sysid != null)
		{
			RawText(" SYSTEM \"");
			RawText(sysid);
			_bufChars[_bufPos++] = '"';
		}
		else
		{
			_bufChars[_bufPos++] = ' ';
		}
		if (subset != null)
		{
			_bufChars[_bufPos++] = '[';
			RawText(subset);
			_bufChars[_bufPos++] = ']';
		}
		_bufChars[_bufPos++] = '>';
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		if (prefix != null && prefix.Length != 0)
		{
			RawText(prefix);
			_bufChars[_bufPos++] = ':';
		}
		RawText(localName);
		_attrEndPos = _bufPos;
	}

	internal override void StartElementContent()
	{
		_bufChars[_bufPos++] = '>';
		_contentPos = _bufPos;
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_contentPos != _bufPos)
		{
			_bufChars[_bufPos++] = '<';
			_bufChars[_bufPos++] = '/';
			if (prefix != null && prefix.Length != 0)
			{
				RawText(prefix);
				_bufChars[_bufPos++] = ':';
			}
			RawText(localName);
			_bufChars[_bufPos++] = '>';
		}
		else
		{
			_bufPos--;
			_bufChars[_bufPos++] = ' ';
			_bufChars[_bufPos++] = '/';
			_bufChars[_bufPos++] = '>';
		}
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '/';
		if (prefix != null && prefix.Length != 0)
		{
			RawText(prefix);
			_bufChars[_bufPos++] = ':';
		}
		RawText(localName);
		_bufChars[_bufPos++] = '>';
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_attrEndPos == _bufPos)
		{
			_bufChars[_bufPos++] = ' ';
		}
		if (prefix != null && prefix.Length > 0)
		{
			RawText(prefix);
			_bufChars[_bufPos++] = ':';
		}
		RawText(localName);
		_bufChars[_bufPos++] = '=';
		_bufChars[_bufPos++] = '"';
		_inAttributeValue = true;
	}

	public override void WriteEndAttribute()
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '"';
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (prefix.Length == 0)
		{
			RawText(" xmlns=\"");
		}
		else
		{
			RawText(" xmlns:");
			RawText(prefix);
			_bufChars[_bufPos++] = '=';
			_bufChars[_bufPos++] = '"';
		}
		_inAttributeValue = true;
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
	}

	internal override void WriteEndNamespaceDeclaration()
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_inAttributeValue = false;
		_bufChars[_bufPos++] = '"';
		_attrEndPos = _bufPos;
	}

	public override void WriteCData(string text)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_mergeCDataSections && _bufPos == _cdataPos)
		{
			_bufPos -= 3;
		}
		else
		{
			_bufChars[_bufPos++] = '<';
			_bufChars[_bufPos++] = '!';
			_bufChars[_bufPos++] = '[';
			_bufChars[_bufPos++] = 'C';
			_bufChars[_bufPos++] = 'D';
			_bufChars[_bufPos++] = 'A';
			_bufChars[_bufPos++] = 'T';
			_bufChars[_bufPos++] = 'A';
			_bufChars[_bufPos++] = '[';
		}
		WriteCDataSection(text);
		_bufChars[_bufPos++] = ']';
		_bufChars[_bufPos++] = ']';
		_bufChars[_bufPos++] = '>';
		_textPos = _bufPos;
		_cdataPos = _bufPos;
	}

	public override void WriteComment(string text)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '!';
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '-';
		WriteCommentOrPi(text, 45);
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '>';
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '?';
		RawText(name);
		if (text.Length > 0)
		{
			_bufChars[_bufPos++] = ' ';
			WriteCommentOrPi(text, 63);
		}
		_bufChars[_bufPos++] = '?';
		_bufChars[_bufPos++] = '>';
	}

	public override void WriteEntityRef(string name)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '&';
		RawText(name);
		_bufChars[_bufPos++] = ';';
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '&';
		_bufChars[_bufPos++] = '#';
		_bufChars[_bufPos++] = 'x';
		RawText(s);
		_bufChars[_bufPos++] = ';';
		if (_bufPos > _bufLen)
		{
			FlushBuffer();
		}
		_textPos = _bufPos;
	}

	public unsafe override void WriteWhitespace(string ws)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
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
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		int num = XmlCharType.CombineSurrogateChar(lowChar, highChar);
		_bufChars[_bufPos++] = '&';
		_bufChars[_bufPos++] = '#';
		_bufChars[_bufPos++] = 'x';
		RawText(num.ToString("X", NumberFormatInfo.InvariantInfo));
		_bufChars[_bufPos++] = ';';
		_textPos = _bufPos;
	}

	public unsafe override void WriteChars(char[] buffer, int index, int count)
	{
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		fixed (char* ptr = &buffer[index])
		{
			WriteRawWithCharChecking(ptr, ptr + count);
		}
		_textPos = _bufPos;
	}

	public unsafe override void WriteRaw(string data)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
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
			else if (_writer != null)
			{
				try
				{
					_writer.Flush();
				}
				finally
				{
					try
					{
						if (_closeOutput)
						{
							_writer.Dispose();
						}
					}
					finally
					{
						_writer = null;
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
		else if (_writer != null)
		{
			_writer.Flush();
		}
	}

	protected virtual void FlushBuffer()
	{
		try
		{
			if (_writeToNull)
			{
				return;
			}
			if (_stream != null)
			{
				if (_trackTextContent)
				{
					_charEntityFallback.Reset(_textContentMarks, _lastMarkPos);
					if (((uint)_lastMarkPos & (true ? 1u : 0u)) != 0)
					{
						_textContentMarks[1] = 1;
						_lastMarkPos = 1;
					}
					else
					{
						_lastMarkPos = 0;
					}
				}
				EncodeChars(1, _bufPos, writeAllToStream: true);
			}
			else if (_bufPos - 1 > 0)
			{
				_writer.Write(_bufChars, 1, _bufPos - 1);
			}
		}
		catch
		{
			_writeToNull = true;
			throw;
		}
		finally
		{
			_bufChars[0] = _bufChars[_bufPos - 1];
			_textPos = ((_textPos == _bufPos) ? 1 : 0);
			_attrEndPos = ((_attrEndPos == _bufPos) ? 1 : 0);
			_contentPos = 0;
			_cdataPos = 0;
			_bufPos = 1;
		}
	}

	private void EncodeChars(int startOffset, int endOffset, bool writeAllToStream)
	{
		while (startOffset < endOffset)
		{
			if (_charEntityFallback != null)
			{
				_charEntityFallback.StartOffset = startOffset;
			}
			_encoder.Convert(_bufChars, startOffset, endOffset - startOffset, _bufBytes, _bufBytesUsed, _bufBytes.Length - _bufBytesUsed, flush: false, out var charsUsed, out var bytesUsed, out var _);
			startOffset += charsUsed;
			_bufBytesUsed += bytesUsed;
			if (_bufBytesUsed >= _bufBytes.Length - 16)
			{
				_stream.Write(_bufBytes, 0, _bufBytesUsed);
				_bufBytesUsed = 0;
			}
		}
		if (writeAllToStream && _bufBytesUsed > 0)
		{
			_stream.Write(_bufBytes, 0, _bufBytesUsed);
			_bufBytesUsed = 0;
		}
	}

	private void FlushEncoder()
	{
		if (_stream != null)
		{
			_encoder.Convert(_bufChars, 1, 0, _bufBytes, 0, _bufBytes.Length, flush: true, out var _, out var bytesUsed, out var _);
			if (bytesUsed != 0)
			{
				_stream.Write(_bufBytes, 0, bytesUsed);
			}
		}
	}

	protected unsafe void WriteAttributeTextBlock(char* pSrc, char* pSrcEnd)
	{
		fixed (char* ptr = _bufChars)
		{
			char* ptr2 = ptr + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)))
				{
					*ptr2 = (char)num;
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
					*ptr2 = (char)num;
					ptr2++;
					break;
				case 9:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr2 = (char)num;
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
						*ptr2 = (char)num;
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
						*ptr2 = (char)num;
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
						*ptr2 = (char)num;
						ptr2++;
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
		fixed (char* ptr = _bufChars)
		{
			char* ptr2 = ptr + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)))
				{
					*ptr2 = (char)num;
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
					*ptr2 = (char)num;
					ptr2++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						ptr2 = WriteNewLine(ptr2);
						break;
					}
					*ptr2 = (char)num;
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
						*ptr2 = (char)num;
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
						*ptr2 = (char)num;
						ptr2++;
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
		fixed (char* ptr = _bufChars)
		{
			char* ptr2 = ptr + _bufPos;
			char* ptr3 = pSrcBegin;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr2 + (pSrcEnd - ptr3);
				if (ptr4 > ptr + _bufLen)
				{
					ptr4 = ptr + _bufLen;
				}
				for (; ptr2 < ptr4; ptr2++)
				{
					if ((num = *ptr3) >= 55296)
					{
						break;
					}
					ptr3++;
					*ptr2 = (char)num;
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
					*ptr2 = (char)num;
					ptr2++;
					ptr3++;
				}
			}
			_bufPos = (int)(ptr2 - ptr);
		}
	}

	protected unsafe void WriteRawWithCharChecking(char* pSrcBegin, char* pSrcEnd)
	{
		fixed (char* ptr2 = _bufChars)
		{
			char* ptr = pSrcBegin;
			char* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr3 + (pSrcEnd - ptr);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsTextChar((char)(num = *ptr)))
				{
					*ptr3 = (char)num;
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
					*ptr3 = (char)num;
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
						*ptr3 = (char)num;
						ptr3++;
					}
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						ptr3 = WriteNewLine(ptr3);
						break;
					}
					*ptr3 = (char)num;
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
						*ptr3 = (char)num;
						ptr3++;
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
			char[] bufChars = _bufChars;
			fixed (char[] array = bufChars)
			{
				char* ptr = (char*)((bufChars != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr3 = ptr2;
				char* ptr4 = ptr2 + text.Length;
				char* ptr5 = ptr + _bufPos;
				int num = 0;
				while (true)
				{
					char* ptr6 = ptr5 + (ptr4 - ptr3);
					if (ptr6 > ptr + _bufLen)
					{
						ptr6 = ptr + _bufLen;
					}
					while (ptr5 < ptr6 && XmlCharType.IsTextChar((char)(num = *ptr3)) && num != stopChar)
					{
						*ptr5 = (char)num;
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
						*ptr5 = '-';
						ptr5++;
						if (num == stopChar && (ptr3 + 1 == ptr4 || ptr3[1] == '-'))
						{
							*ptr5 = ' ';
							ptr5++;
						}
						break;
					case 63:
						*ptr5 = '?';
						ptr5++;
						if (num == stopChar && ptr3 + 1 < ptr4 && ptr3[1] == '>')
						{
							*ptr5 = ' ';
							ptr5++;
						}
						break;
					case 93:
						*ptr5 = ']';
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
							*ptr5 = (char)num;
							ptr5++;
						}
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							ptr5 = WriteNewLine(ptr5);
							break;
						}
						*ptr5 = (char)num;
						ptr5++;
						break;
					case 9:
					case 38:
					case 60:
						*ptr5 = (char)num;
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
							*ptr5 = (char)num;
							ptr5++;
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
			char[] bufChars = _bufChars;
			fixed (char[] array = bufChars)
			{
				char* ptr = (char*)((bufChars != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr3 = ptr2;
				char* ptr4 = ptr2 + text.Length;
				char* ptr5 = ptr + _bufPos;
				int num = 0;
				while (true)
				{
					char* ptr6 = ptr5 + (ptr4 - ptr3);
					if (ptr6 > ptr + _bufLen)
					{
						ptr6 = ptr + _bufLen;
					}
					while (ptr5 < ptr6 && XmlCharType.IsAttributeValueChar((char)(num = *ptr3)) && num != 93)
					{
						*ptr5 = (char)num;
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
						if (_hadDoubleBracket && ptr5[-1] == ']')
						{
							ptr5 = RawEndCData(ptr5);
							ptr5 = RawStartCData(ptr5);
						}
						*ptr5 = '>';
						ptr5++;
						break;
					case 93:
						if (ptr5[-1] == ']')
						{
							_hadDoubleBracket = true;
						}
						else
						{
							_hadDoubleBracket = false;
						}
						*ptr5 = ']';
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
							*ptr5 = (char)num;
							ptr5++;
						}
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							ptr5 = WriteNewLine(ptr5);
							break;
						}
						*ptr5 = (char)num;
						ptr5++;
						break;
					case 9:
					case 34:
					case 38:
					case 39:
					case 60:
						*ptr5 = (char)num;
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
							*ptr5 = (char)num;
							ptr5++;
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

	private unsafe static char* EncodeSurrogate(char* pSrc, char* pSrcEnd, char* pDst)
	{
		int num = *pSrc;
		if (num <= 56319)
		{
			if (pSrc + 1 < pSrcEnd)
			{
				int num2 = pSrc[1];
				if (num2 >= 56320 && (System.LocalAppContextSwitches.DontThrowOnInvalidSurrogatePairs || num2 <= 57343))
				{
					*pDst = (char)num;
					pDst[1] = (char)num2;
					pDst += 2;
					return pDst;
				}
				throw XmlConvert.CreateInvalidSurrogatePairException((char)num2, (char)num);
			}
			throw new ArgumentException(System.SR.Xml_InvalidSurrogateMissingLowChar);
		}
		throw XmlConvert.CreateInvalidHighSurrogateCharException((char)num);
	}

	private unsafe char* InvalidXmlChar(int ch, char* pDst, bool entitize)
	{
		if (_checkCharacters)
		{
			throw XmlConvert.CreateInvalidCharException((char)ch, '\0');
		}
		if (entitize)
		{
			return CharEntity(pDst, (char)ch);
		}
		*pDst = (char)ch;
		pDst++;
		return pDst;
	}

	internal unsafe void EncodeChar(ref char* pSrc, char* pSrcEnd, ref char* pDst)
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
			*pDst = (char)num;
			pDst++;
			pSrc++;
		}
	}

	protected void ChangeTextContentMark(bool value)
	{
		_inTextContent = value;
		if (_lastMarkPos + 1 == _textContentMarks.Length)
		{
			GrowTextContentMarks();
		}
		_textContentMarks[++_lastMarkPos] = _bufPos;
	}

	private void GrowTextContentMarks()
	{
		int[] array = new int[_textContentMarks.Length * 2];
		Array.Copy(_textContentMarks, array, _textContentMarks.Length);
		_textContentMarks = array;
	}

	protected unsafe char* WriteNewLine(char* pDst)
	{
		fixed (char* ptr = _bufChars)
		{
			_bufPos = (int)(pDst - ptr);
			RawText(_newLineChars);
			return ptr + _bufPos;
		}
	}

	protected unsafe static char* LtEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = 'l';
		pDst[2] = 't';
		pDst[3] = ';';
		return pDst + 4;
	}

	protected unsafe static char* GtEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = 'g';
		pDst[2] = 't';
		pDst[3] = ';';
		return pDst + 4;
	}

	protected unsafe static char* AmpEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = 'a';
		pDst[2] = 'm';
		pDst[3] = 'p';
		pDst[4] = ';';
		return pDst + 5;
	}

	protected unsafe static char* QuoteEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = 'q';
		pDst[2] = 'u';
		pDst[3] = 'o';
		pDst[4] = 't';
		pDst[5] = ';';
		return pDst + 6;
	}

	protected unsafe static char* TabEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = '#';
		pDst[2] = 'x';
		pDst[3] = '9';
		pDst[4] = ';';
		return pDst + 5;
	}

	protected unsafe static char* LineFeedEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = '#';
		pDst[2] = 'x';
		pDst[3] = 'A';
		pDst[4] = ';';
		return pDst + 5;
	}

	protected unsafe static char* CarriageReturnEntity(char* pDst)
	{
		*pDst = '&';
		pDst[1] = '#';
		pDst[2] = 'x';
		pDst[3] = 'D';
		pDst[4] = ';';
		return pDst + 5;
	}

	private unsafe static char* CharEntity(char* pDst, char ch)
	{
		int num = ch;
		string text = num.ToString("X", NumberFormatInfo.InvariantInfo);
		*pDst = '&';
		pDst[1] = '#';
		pDst[2] = 'x';
		pDst += 3;
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr;
			while ((*(pDst++) = *(ptr2++)) != 0)
			{
			}
		}
		pDst[-1] = ';';
		return pDst;
	}

	protected unsafe static char* RawStartCData(char* pDst)
	{
		*pDst = '<';
		pDst[1] = '!';
		pDst[2] = '[';
		pDst[3] = 'C';
		pDst[4] = 'D';
		pDst[5] = 'A';
		pDst[6] = 'T';
		pDst[7] = 'A';
		pDst[8] = '[';
		return pDst + 9;
	}

	protected unsafe static char* RawEndCData(char* pDst)
	{
		*pDst = ']';
		pDst[1] = ']';
		pDst[2] = '>';
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
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
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
			else if (_writer != null)
			{
				try
				{
					await _writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					try
					{
						if (_closeOutput)
						{
							await _writer.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					finally
					{
						_writer = null;
					}
				}
			}
		}
	}

	public override async Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
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
			_bufChars[_bufPos++] = '"';
		}
		else if (sysid != null)
		{
			await RawTextAsync(" SYSTEM \"").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync(sysid).ConfigureAwait(continueOnCapturedContext: false);
			_bufChars[_bufPos++] = '"';
		}
		else
		{
			_bufChars[_bufPos++] = ' ';
		}
		if (subset != null)
		{
			_bufChars[_bufPos++] = '[';
			await RawTextAsync(subset).ConfigureAwait(continueOnCapturedContext: false);
			_bufChars[_bufPos++] = ']';
		}
		_bufChars[_bufPos++] = '>';
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		Task task = ((prefix == null || prefix.Length == 0) ? RawTextAsync(localName) : RawTextAsync(prefix, ":", localName));
		return task.CallVoidFuncWhenFinishAsync(delegate(XmlEncodedRawTextWriter thisRef)
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_contentPos != _bufPos)
		{
			_bufChars[_bufPos++] = '<';
			_bufChars[_bufPos++] = '/';
			if (prefix != null && prefix.Length != 0)
			{
				return RawTextAsync(prefix, ":", localName, ">");
			}
			return RawTextAsync(localName, ">");
		}
		_bufPos--;
		_bufChars[_bufPos++] = ' ';
		_bufChars[_bufPos++] = '/';
		_bufChars[_bufPos++] = '>';
		return Task.CompletedTask;
	}

	internal override Task WriteFullEndElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '/';
		if (prefix != null && prefix.Length != 0)
		{
			return RawTextAsync(prefix, ":", localName, ">");
		}
		return RawTextAsync(localName, ">");
	}

	protected internal override Task WriteStartAttributeAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_attrEndPos == _bufPos)
		{
			_bufChars[_bufPos++] = ' ';
		}
		Task task = ((prefix == null || prefix.Length <= 0) ? RawTextAsync(localName) : RawTextAsync(prefix, ":", localName));
		return task.CallVoidFuncWhenFinishAsync(delegate(XmlEncodedRawTextWriter thisRef)
		{
			thisRef.WriteStartAttribute_SetInAttribute();
		}, this);
	}

	private void WriteStartAttribute_SetInAttribute()
	{
		_bufChars[_bufPos++] = '=';
		_bufChars[_bufPos++] = '"';
		_inAttributeValue = true;
	}

	protected internal override Task WriteEndAttributeAsync()
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '"';
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (prefix.Length == 0)
		{
			await RawTextAsync(" xmlns=\"").ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await RawTextAsync(" xmlns:").ConfigureAwait(continueOnCapturedContext: false);
			await RawTextAsync(prefix).ConfigureAwait(continueOnCapturedContext: false);
			_bufChars[_bufPos++] = '=';
			_bufChars[_bufPos++] = '"';
		}
		_inAttributeValue = true;
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
	}

	internal override Task WriteEndNamespaceDeclarationAsync()
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_inAttributeValue = false;
		_bufChars[_bufPos++] = '"';
		_attrEndPos = _bufPos;
		return Task.CompletedTask;
	}

	public override async Task WriteCDataAsync(string text)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_mergeCDataSections && _bufPos == _cdataPos)
		{
			_bufPos -= 3;
		}
		else
		{
			_bufChars[_bufPos++] = '<';
			_bufChars[_bufPos++] = '!';
			_bufChars[_bufPos++] = '[';
			_bufChars[_bufPos++] = 'C';
			_bufChars[_bufPos++] = 'D';
			_bufChars[_bufPos++] = 'A';
			_bufChars[_bufPos++] = 'T';
			_bufChars[_bufPos++] = 'A';
			_bufChars[_bufPos++] = '[';
		}
		await WriteCDataSectionAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		_bufChars[_bufPos++] = ']';
		_bufChars[_bufPos++] = ']';
		_bufChars[_bufPos++] = '>';
		_textPos = _bufPos;
		_cdataPos = _bufPos;
	}

	public override async Task WriteCommentAsync(string text)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '!';
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '-';
		await WriteCommentOrPiAsync(text, 45).ConfigureAwait(continueOnCapturedContext: false);
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '-';
		_bufChars[_bufPos++] = '>';
	}

	public override async Task WriteProcessingInstructionAsync(string name, string text)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '?';
		await RawTextAsync(name).ConfigureAwait(continueOnCapturedContext: false);
		if (text.Length > 0)
		{
			_bufChars[_bufPos++] = ' ';
			await WriteCommentOrPiAsync(text, 63).ConfigureAwait(continueOnCapturedContext: false);
		}
		_bufChars[_bufPos++] = '?';
		_bufChars[_bufPos++] = '>';
	}

	public override async Task WriteEntityRefAsync(string name)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '&';
		await RawTextAsync(name).ConfigureAwait(continueOnCapturedContext: false);
		_bufChars[_bufPos++] = ';';
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
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '&';
		_bufChars[_bufPos++] = '#';
		_bufChars[_bufPos++] = 'x';
		await RawTextAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		_bufChars[_bufPos++] = ';';
		if (_bufPos > _bufLen)
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_textPos = _bufPos;
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(ws);
		}
		return WriteElementTextBlockAsync(ws);
	}

	public override Task WriteStringAsync(string text)
	{
		CheckAsyncCall();
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(text);
		}
		return WriteElementTextBlockAsync(text);
	}

	public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		int num = XmlCharType.CombineSurrogateChar(lowChar, highChar);
		_bufChars[_bufPos++] = '&';
		_bufChars[_bufPos++] = '#';
		_bufChars[_bufPos++] = 'x';
		await RawTextAsync(num.ToString("X", NumberFormatInfo.InvariantInfo)).ConfigureAwait(continueOnCapturedContext: false);
		_bufChars[_bufPos++] = ';';
		_textPos = _bufPos;
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		if (_trackTextContent && !_inTextContent)
		{
			ChangeTextContentMark(value: true);
		}
		if (_inAttributeValue)
		{
			return WriteAttributeTextBlockAsync(buffer, index, count);
		}
		return WriteElementTextBlockAsync(buffer, index, count);
	}

	public override async Task WriteRawAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		await WriteRawWithCharCheckingAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_textPos = _bufPos;
	}

	public override async Task WriteRawAsync(string data)
	{
		CheckAsyncCall();
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		await WriteRawWithCharCheckingAsync(data).ConfigureAwait(continueOnCapturedContext: false);
		_textPos = _bufPos;
	}

	public override async Task FlushAsync()
	{
		CheckAsyncCall();
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		await FlushEncoderAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (_stream != null)
		{
			await _stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (_writer != null)
		{
			await _writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	protected virtual async Task FlushBufferAsync()
	{
		_ = 1;
		try
		{
			if (_writeToNull)
			{
				return;
			}
			if (_stream != null)
			{
				if (_trackTextContent)
				{
					_charEntityFallback.Reset(_textContentMarks, _lastMarkPos);
					if (((uint)_lastMarkPos & (true ? 1u : 0u)) != 0)
					{
						_textContentMarks[1] = 1;
						_lastMarkPos = 1;
					}
					else
					{
						_lastMarkPos = 0;
					}
				}
				await EncodeCharsAsync(1, _bufPos, writeAllToStream: true).ConfigureAwait(continueOnCapturedContext: false);
			}
			else if (_bufPos - 1 > 0)
			{
				await _writer.WriteAsync(_bufChars.AsMemory(1, _bufPos - 1)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			_writeToNull = true;
			throw;
		}
		finally
		{
			_bufChars[0] = _bufChars[_bufPos - 1];
			_textPos = ((_textPos == _bufPos) ? 1 : 0);
			_attrEndPos = ((_attrEndPos == _bufPos) ? 1 : 0);
			_contentPos = 0;
			_cdataPos = 0;
			_bufPos = 1;
		}
	}

	private async Task EncodeCharsAsync(int startOffset, int endOffset, bool writeAllToStream)
	{
		while (startOffset < endOffset)
		{
			if (_charEntityFallback != null)
			{
				_charEntityFallback.StartOffset = startOffset;
			}
			_encoder.Convert(_bufChars, startOffset, endOffset - startOffset, _bufBytes, _bufBytesUsed, _bufBytes.Length - _bufBytesUsed, flush: false, out var charsUsed, out var bytesUsed, out var _);
			startOffset += charsUsed;
			_bufBytesUsed += bytesUsed;
			if (_bufBytesUsed >= _bufBytes.Length - 16)
			{
				await _stream.WriteAsync(_bufBytes.AsMemory(0, _bufBytesUsed)).ConfigureAwait(continueOnCapturedContext: false);
				_bufBytesUsed = 0;
			}
		}
		if (writeAllToStream && _bufBytesUsed > 0)
		{
			await _stream.WriteAsync(_bufBytes.AsMemory(0, _bufBytesUsed)).ConfigureAwait(continueOnCapturedContext: false);
			_bufBytesUsed = 0;
		}
	}

	private Task FlushEncoderAsync()
	{
		if (_stream != null)
		{
			_encoder.Convert(_bufChars, 1, 0, _bufBytes, 0, _bufBytes.Length, flush: true, out var _, out var bytesUsed, out var _);
			if (bytesUsed != 0)
			{
				return _stream.WriteAsync(_bufBytes, 0, bytesUsed);
			}
		}
		return Task.CompletedTask;
	}

	protected unsafe int WriteAttributeTextBlockNoFlush(char* pSrc, char* pSrcEnd)
	{
		char* ptr = pSrc;
		fixed (char* ptr2 = _bufChars)
		{
			char* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr3 + (pSrcEnd - pSrc);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)))
				{
					*ptr3 = (char)num;
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
					*ptr3 = (char)num;
					ptr3++;
					break;
				case 9:
					if (_newLineHandling == NewLineHandling.None)
					{
						*ptr3 = (char)num;
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
						*ptr3 = (char)num;
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
						*ptr3 = (char)num;
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
						*ptr3 = (char)num;
						ptr3++;
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
		fixed (char* ptr2 = _bufChars)
		{
			char* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr3 + (pSrcEnd - pSrc);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsAttributeValueChar((char)(num = *pSrc)))
				{
					*ptr3 = (char)num;
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
					*ptr3 = (char)num;
					ptr3++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(pSrc - ptr);
					}
					*ptr3 = (char)num;
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
						*ptr3 = (char)num;
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
						*ptr3 = (char)num;
						ptr3++;
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
		fixed (char* ptr = _bufChars)
		{
			char* ptr2 = ptr + _bufPos;
			char* ptr3 = pSrcBegin;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr2 + (pSrcEnd - ptr3);
				if (ptr4 > ptr + _bufLen)
				{
					ptr4 = ptr + _bufLen;
				}
				for (; ptr2 < ptr4; ptr2++)
				{
					if ((num = *ptr3) >= 55296)
					{
						break;
					}
					ptr3++;
					*ptr2 = (char)num;
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
					*ptr2 = (char)num;
					ptr2++;
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
		fixed (char* ptr2 = _bufChars)
		{
			char* ptr = pSrcBegin;
			char* ptr3 = ptr2 + _bufPos;
			int num = 0;
			while (true)
			{
				char* ptr4 = ptr3 + (pSrcEnd - ptr);
				if (ptr4 > ptr2 + _bufLen)
				{
					ptr4 = ptr2 + _bufLen;
				}
				while (ptr3 < ptr4 && XmlCharType.IsTextChar((char)(num = *ptr)))
				{
					*ptr3 = (char)num;
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
					*ptr3 = (char)num;
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
					*ptr3 = (char)num;
					ptr3++;
					break;
				case 10:
					if (_newLineHandling == NewLineHandling.Replace)
					{
						_bufPos = (int)(ptr3 - ptr2);
						needWriteNewLine = true;
						return (int)(ptr - pSrcBegin);
					}
					*ptr3 = (char)num;
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
						*ptr3 = (char)num;
						ptr3++;
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
			char[] bufChars = _bufChars;
			fixed (char[] array = bufChars)
			{
				char* ptr3 = (char*)((bufChars != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr4 = ptr2;
				char* ptr5 = ptr4;
				char* ptr6 = ptr2 + count;
				char* ptr7 = ptr3 + _bufPos;
				int num = 0;
				while (true)
				{
					char* ptr8 = ptr7 + (ptr6 - ptr4);
					if (ptr8 > ptr3 + _bufLen)
					{
						ptr8 = ptr3 + _bufLen;
					}
					while (ptr7 < ptr8 && XmlCharType.IsTextChar((char)(num = *ptr4)) && num != stopChar)
					{
						*ptr7 = (char)num;
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
						*ptr7 = '-';
						ptr7++;
						if (num == stopChar && (ptr4 + 1 == ptr6 || ptr4[1] == '-'))
						{
							*ptr7 = ' ';
							ptr7++;
						}
						break;
					case 63:
						*ptr7 = '?';
						ptr7++;
						if (num == stopChar && ptr4 + 1 < ptr6 && ptr4[1] == '>')
						{
							*ptr7 = ' ';
							ptr7++;
						}
						break;
					case 93:
						*ptr7 = ']';
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
						*ptr7 = (char)num;
						ptr7++;
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr5);
						}
						*ptr7 = (char)num;
						ptr7++;
						break;
					case 9:
					case 38:
					case 60:
						*ptr7 = (char)num;
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
							*ptr7 = (char)num;
							ptr7++;
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
			char[] bufChars = _bufChars;
			fixed (char[] array = bufChars)
			{
				char* ptr3 = (char*)((bufChars != null && array.Length != 0) ? Unsafe.AsPointer(ref array[0]) : null);
				char* ptr4 = ptr2;
				char* ptr5 = ptr2 + count;
				char* ptr6 = ptr4;
				char* ptr7 = ptr3 + _bufPos;
				int num = 0;
				while (true)
				{
					char* ptr8 = ptr7 + (ptr5 - ptr4);
					if (ptr8 > ptr3 + _bufLen)
					{
						ptr8 = ptr3 + _bufLen;
					}
					while (ptr7 < ptr8 && XmlCharType.IsAttributeValueChar((char)(num = *ptr4)) && num != 93)
					{
						*ptr7 = (char)num;
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
						if (_hadDoubleBracket && ptr7[-1] == ']')
						{
							ptr7 = RawEndCData(ptr7);
							ptr7 = RawStartCData(ptr7);
						}
						*ptr7 = '>';
						ptr7++;
						break;
					case 93:
						if (ptr7[-1] == ']')
						{
							_hadDoubleBracket = true;
						}
						else
						{
							_hadDoubleBracket = false;
						}
						*ptr7 = ']';
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
						*ptr7 = (char)num;
						ptr7++;
						break;
					case 10:
						if (_newLineHandling == NewLineHandling.Replace)
						{
							_bufPos = (int)(ptr7 - ptr3);
							needWriteNewLine = true;
							return (int)(ptr4 - ptr6);
						}
						*ptr7 = (char)num;
						ptr7++;
						break;
					case 9:
					case 34:
					case 38:
					case 39:
					case 60:
						*ptr7 = (char)num;
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
							*ptr7 = (char)num;
							ptr7++;
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
