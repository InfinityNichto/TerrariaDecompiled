using System.IO;

namespace System.Xml;

internal class HtmlEncodedRawTextWriter : XmlEncodedRawTextWriter
{
	protected ByteStack _elementScope;

	protected ElementProperties _currentElementProperties;

	private AttributeProperties _currentAttributeProperties;

	private bool _endsWithAmpersand;

	private byte[] _uriEscapingBuffer;

	private string _mediaType;

	private bool _doNotEscapeUriAttributes;

	protected static TernaryTreeReadOnly _elementPropertySearch;

	protected static TernaryTreeReadOnly _attributePropertySearch;

	public HtmlEncodedRawTextWriter(TextWriter writer, XmlWriterSettings settings)
		: base(writer, settings)
	{
		Init(settings);
	}

	public HtmlEncodedRawTextWriter(Stream stream, XmlWriterSettings settings)
		: base(stream, settings)
	{
		Init(settings);
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		RawText("<!DOCTYPE ");
		if (name == "HTML")
		{
			RawText("HTML");
		}
		else
		{
			RawText("html");
		}
		if (pubid != null)
		{
			RawText(" PUBLIC \"");
			RawText(pubid);
			if (sysid != null)
			{
				RawText("\" \"");
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
		_elementScope.Push((byte)_currentElementProperties);
		if (ns.Length == 0)
		{
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
			_currentElementProperties = (ElementProperties)_elementPropertySearch.FindCaseInsensitiveString(localName);
			_bufChars[_bufPos++] = '<';
			RawText(localName);
			_attrEndPos = _bufPos;
		}
		else
		{
			_currentElementProperties = ElementProperties.HAS_NS;
			base.WriteStartElement(prefix, localName, ns);
		}
	}

	internal override void StartElementContent()
	{
		_bufChars[_bufPos++] = '>';
		_contentPos = _bufPos;
		if ((_currentElementProperties & ElementProperties.HEAD) != 0)
		{
			WriteMetaElement();
		}
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		if (ns.Length == 0)
		{
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
			if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
			{
				_bufChars[_bufPos++] = '<';
				_bufChars[_bufPos++] = '/';
				RawText(localName);
				_bufChars[_bufPos++] = '>';
			}
		}
		else
		{
			base.WriteEndElement(prefix, localName, ns);
		}
		_currentElementProperties = (ElementProperties)_elementScope.Pop();
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		if (ns.Length == 0)
		{
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
			if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
			{
				_bufChars[_bufPos++] = '<';
				_bufChars[_bufPos++] = '/';
				RawText(localName);
				_bufChars[_bufPos++] = '>';
			}
		}
		else
		{
			base.WriteFullEndElement(prefix, localName, ns);
		}
		_currentElementProperties = (ElementProperties)_elementScope.Pop();
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (ns.Length == 0)
		{
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
			if (_attrEndPos == _bufPos)
			{
				_bufChars[_bufPos++] = ' ';
			}
			RawText(localName);
			if ((_currentElementProperties & (ElementProperties)7u) != 0)
			{
				_currentAttributeProperties = (AttributeProperties)((uint)_attributePropertySearch.FindCaseInsensitiveString(localName) & (uint)_currentElementProperties);
				if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) != 0)
				{
					_inAttributeValue = true;
					return;
				}
			}
			else
			{
				_currentAttributeProperties = AttributeProperties.DEFAULT;
			}
			_bufChars[_bufPos++] = '=';
			_bufChars[_bufPos++] = '"';
		}
		else
		{
			base.WriteStartAttribute(prefix, localName, ns);
			_currentAttributeProperties = AttributeProperties.DEFAULT;
		}
		_inAttributeValue = true;
	}

	public override void WriteEndAttribute()
	{
		if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) != 0)
		{
			_attrEndPos = _bufPos;
		}
		else
		{
			if (_endsWithAmpersand)
			{
				OutputRestAmps();
				_endsWithAmpersand = false;
			}
			if (_trackTextContent && _inTextContent)
			{
				ChangeTextContentMark(value: false);
			}
			_bufChars[_bufPos++] = '"';
		}
		_inAttributeValue = false;
		_attrEndPos = _bufPos;
	}

	public override void WriteProcessingInstruction(string target, string text)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_bufChars[_bufPos++] = '<';
		_bufChars[_bufPos++] = '?';
		RawText(target);
		_bufChars[_bufPos++] = ' ';
		WriteCommentOrPi(text, 63);
		_bufChars[_bufPos++] = '>';
		if (_bufPos > _bufLen)
		{
			FlushBuffer();
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
				WriteHtmlAttributeTextBlock(ptr, pSrcEnd);
			}
			else
			{
				WriteHtmlElementTextBlock(ptr, pSrcEnd);
			}
		}
	}

	public override void WriteEntityRef(string name)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteCharEntity(char ch)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
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

	private void Init(XmlWriterSettings settings)
	{
		if (_elementPropertySearch == null)
		{
			_attributePropertySearch = new TernaryTreeReadOnly(HtmlTernaryTree.htmlAttributes);
			_elementPropertySearch = new TernaryTreeReadOnly(HtmlTernaryTree.htmlElements);
		}
		_elementScope = new ByteStack(10);
		_uriEscapingBuffer = new byte[5];
		_currentElementProperties = ElementProperties.DEFAULT;
		_mediaType = settings.MediaType;
		_doNotEscapeUriAttributes = settings.DoNotEscapeUriAttributes;
	}

	protected void WriteMetaElement()
	{
		RawText("<META http-equiv=\"Content-Type\"");
		if (_mediaType == null)
		{
			_mediaType = "text/html";
		}
		RawText(" content=\"");
		RawText(_mediaType);
		RawText("; charset=");
		RawText(_encoding.WebName);
		RawText("\">");
	}

	protected unsafe void WriteHtmlElementTextBlock(char* pSrc, char* pSrcEnd)
	{
		if ((_currentElementProperties & ElementProperties.NO_ENTITIES) != 0)
		{
			RawText(pSrc, pSrcEnd);
		}
		else
		{
			WriteElementTextBlock(pSrc, pSrcEnd);
		}
	}

	protected unsafe void WriteHtmlAttributeTextBlock(char* pSrc, char* pSrcEnd)
	{
		if ((_currentAttributeProperties & (AttributeProperties)7u) != 0)
		{
			if ((_currentAttributeProperties & AttributeProperties.BOOLEAN) == 0)
			{
				if ((_currentAttributeProperties & (AttributeProperties)5u) != 0 && !_doNotEscapeUriAttributes)
				{
					WriteUriAttributeText(pSrc, pSrcEnd);
				}
				else
				{
					WriteHtmlAttributeText(pSrc, pSrcEnd);
				}
			}
		}
		else if ((_currentElementProperties & ElementProperties.HAS_NS) != 0)
		{
			WriteAttributeTextBlock(pSrc, pSrcEnd);
		}
		else
		{
			WriteHtmlAttributeText(pSrc, pSrcEnd);
		}
	}

	private unsafe void WriteHtmlAttributeText(char* pSrc, char* pSrcEnd)
	{
		if (_endsWithAmpersand)
		{
			if (pSrcEnd - pSrc > 0 && *pSrc != '{')
			{
				OutputRestAmps();
			}
			_endsWithAmpersand = false;
		}
		fixed (char* ptr = _bufChars)
		{
			char* pDst = ptr + _bufPos;
			char c = '\0';
			while (true)
			{
				char* ptr2 = pDst + (pSrcEnd - pSrc);
				if (ptr2 > ptr + _bufLen)
				{
					ptr2 = ptr + _bufLen;
				}
				while (pDst < ptr2 && XmlCharType.IsAttributeValueChar(c = *pSrc))
				{
					*(pDst++) = c;
					pSrc++;
				}
				if (pSrc >= pSrcEnd)
				{
					break;
				}
				if (pDst >= ptr2)
				{
					_bufPos = (int)(pDst - ptr);
					FlushBuffer();
					pDst = ptr + 1;
					continue;
				}
				switch (c)
				{
				case '&':
					if (pSrc + 1 == pSrcEnd)
					{
						_endsWithAmpersand = true;
					}
					else if (pSrc[1] != '{')
					{
						pDst = XmlEncodedRawTextWriter.AmpEntity(pDst);
						break;
					}
					*(pDst++) = c;
					break;
				case '"':
					pDst = XmlEncodedRawTextWriter.QuoteEntity(pDst);
					break;
				case '\t':
				case '\'':
				case '<':
				case '>':
					*(pDst++) = c;
					break;
				case '\r':
					pDst = XmlEncodedRawTextWriter.CarriageReturnEntity(pDst);
					break;
				case '\n':
					pDst = XmlEncodedRawTextWriter.LineFeedEntity(pDst);
					break;
				default:
					EncodeChar(ref pSrc, pSrcEnd, ref pDst);
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(pDst - ptr);
		}
	}

	private unsafe void WriteUriAttributeText(char* pSrc, char* pSrcEnd)
	{
		if (_endsWithAmpersand)
		{
			if (pSrcEnd - pSrc > 0 && *pSrc != '{')
			{
				OutputRestAmps();
			}
			_endsWithAmpersand = false;
		}
		fixed (char* ptr = _bufChars)
		{
			char* ptr2 = ptr + _bufPos;
			char c = '\0';
			while (true)
			{
				char* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar(c = *pSrc) && c < '\u0080')
				{
					*(ptr2++) = c;
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
				switch (c)
				{
				case '&':
					if (pSrc + 1 == pSrcEnd)
					{
						_endsWithAmpersand = true;
					}
					else if (pSrc[1] != '{')
					{
						ptr2 = XmlEncodedRawTextWriter.AmpEntity(ptr2);
						break;
					}
					*(ptr2++) = c;
					break;
				case '"':
					ptr2 = XmlEncodedRawTextWriter.QuoteEntity(ptr2);
					break;
				case '\t':
				case '\'':
				case '<':
				case '>':
					*(ptr2++) = c;
					break;
				case '\r':
					ptr2 = XmlEncodedRawTextWriter.CarriageReturnEntity(ptr2);
					break;
				case '\n':
					ptr2 = XmlEncodedRawTextWriter.LineFeedEntity(ptr2);
					break;
				default:
					fixed (byte* ptr4 = _uriEscapingBuffer)
					{
						byte* ptr5 = ptr4;
						byte* pDst = ptr5;
						XmlUtf8RawTextWriter.CharToUTF8(ref pSrc, pSrcEnd, ref pDst);
						for (; ptr5 < pDst; ptr5++)
						{
							*(ptr2++) = '%';
							*(ptr2++) = System.HexConverter.ToCharUpper(*ptr5 >> 4);
							*(ptr2++) = System.HexConverter.ToCharUpper(*ptr5);
						}
					}
					continue;
				}
				pSrc++;
			}
			_bufPos = (int)(ptr2 - ptr);
		}
	}

	private void OutputRestAmps()
	{
		_bufChars[_bufPos++] = 'a';
		_bufChars[_bufPos++] = 'm';
		_bufChars[_bufPos++] = 'p';
		_bufChars[_bufPos++] = ';';
	}
}
