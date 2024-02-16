using System.IO;

namespace System.Xml;

internal class HtmlUtf8RawTextWriter : XmlUtf8RawTextWriter
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

	public HtmlUtf8RawTextWriter(Stream stream, XmlWriterSettings settings)
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
		_elementScope.Push((byte)_currentElementProperties);
		if (ns.Length == 0)
		{
			_currentElementProperties = (ElementProperties)_elementPropertySearch.FindCaseInsensitiveString(localName);
			_bufBytes[_bufPos++] = 60;
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
		_bufBytes[_bufPos++] = 62;
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
			if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
			{
				_bufBytes[_bufPos++] = 60;
				_bufBytes[_bufPos++] = 47;
				RawText(localName);
				_bufBytes[_bufPos++] = 62;
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
			if ((_currentElementProperties & ElementProperties.EMPTY) == 0)
			{
				_bufBytes[_bufPos++] = 60;
				_bufBytes[_bufPos++] = 47;
				RawText(localName);
				_bufBytes[_bufPos++] = 62;
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
			if (_attrEndPos == _bufPos)
			{
				_bufBytes[_bufPos++] = 32;
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
			_bufBytes[_bufPos++] = 61;
			_bufBytes[_bufPos++] = 34;
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
			_bufBytes[_bufPos++] = 34;
		}
		_inAttributeValue = false;
		_attrEndPos = _bufPos;
	}

	public override void WriteProcessingInstruction(string target, string text)
	{
		_bufBytes[_bufPos++] = 60;
		_bufBytes[_bufPos++] = 63;
		RawText(target);
		_bufBytes[_bufPos++] = 32;
		WriteCommentOrPi(text, 63);
		_bufBytes[_bufPos++] = 62;
		if (_bufPos > _bufLen)
		{
			FlushBuffer();
		}
	}

	public unsafe override void WriteString(string text)
	{
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
		fixed (byte* ptr = _bufBytes)
		{
			byte* pDst = ptr + _bufPos;
			char c = '\0';
			while (true)
			{
				byte* ptr2 = pDst + (pSrcEnd - pSrc);
				if (ptr2 > ptr + _bufLen)
				{
					ptr2 = ptr + _bufLen;
				}
				while (pDst < ptr2 && XmlCharType.IsAttributeValueChar(c = *pSrc) && c <= '\u007f')
				{
					*(pDst++) = (byte)c;
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
						pDst = XmlUtf8RawTextWriter.AmpEntity(pDst);
						break;
					}
					*(pDst++) = (byte)c;
					break;
				case '"':
					pDst = XmlUtf8RawTextWriter.QuoteEntity(pDst);
					break;
				case '\t':
				case '\'':
				case '<':
				case '>':
					*(pDst++) = (byte)c;
					break;
				case '\r':
					pDst = XmlUtf8RawTextWriter.CarriageReturnEntity(pDst);
					break;
				case '\n':
					pDst = XmlUtf8RawTextWriter.LineFeedEntity(pDst);
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
		fixed (byte* ptr = _bufBytes)
		{
			byte* ptr2 = ptr + _bufPos;
			char c = '\0';
			while (true)
			{
				byte* ptr3 = ptr2 + (pSrcEnd - pSrc);
				if (ptr3 > ptr + _bufLen)
				{
					ptr3 = ptr + _bufLen;
				}
				while (ptr2 < ptr3 && XmlCharType.IsAttributeValueChar(c = *pSrc) && c < '\u0080')
				{
					*(ptr2++) = (byte)c;
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
						ptr2 = XmlUtf8RawTextWriter.AmpEntity(ptr2);
						break;
					}
					*(ptr2++) = (byte)c;
					break;
				case '"':
					ptr2 = XmlUtf8RawTextWriter.QuoteEntity(ptr2);
					break;
				case '\t':
				case '\'':
				case '<':
				case '>':
					*(ptr2++) = (byte)c;
					break;
				case '\r':
					ptr2 = XmlUtf8RawTextWriter.CarriageReturnEntity(ptr2);
					break;
				case '\n':
					ptr2 = XmlUtf8RawTextWriter.LineFeedEntity(ptr2);
					break;
				default:
					fixed (byte* ptr4 = _uriEscapingBuffer)
					{
						byte* ptr5 = ptr4;
						byte* pDst = ptr5;
						XmlUtf8RawTextWriter.CharToUTF8(ref pSrc, pSrcEnd, ref pDst);
						for (; ptr5 < pDst; ptr5++)
						{
							*(ptr2++) = 37;
							*(ptr2++) = (byte)System.HexConverter.ToCharUpper(*ptr5 >> 4);
							*(ptr2++) = (byte)System.HexConverter.ToCharUpper(*ptr5);
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
		_bufBytes[_bufPos++] = 97;
		_bufBytes[_bufPos++] = 109;
		_bufBytes[_bufPos++] = 112;
		_bufBytes[_bufPos++] = 59;
	}
}
