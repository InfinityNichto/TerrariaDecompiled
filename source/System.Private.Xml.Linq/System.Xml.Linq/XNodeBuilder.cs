using System.Collections.Generic;

namespace System.Xml.Linq;

internal sealed class XNodeBuilder : XmlWriter
{
	private List<object> _content;

	private XContainer _parent;

	private XName _attrName;

	private string _attrValue;

	private readonly XContainer _root;

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Auto;
			return xmlWriterSettings;
		}
	}

	public override WriteState WriteState
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public XNodeBuilder(XContainer container)
	{
		_root = container;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Close();
		}
	}

	public override void Close()
	{
		_root.Add(_content);
	}

	public override void Flush()
	{
	}

	public override string LookupPrefix(string namespaceName)
	{
		throw new NotSupportedException();
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.NotSupported_WriteBase64);
	}

	public override void WriteCData(string text)
	{
		AddNode(new XCData(text));
	}

	public override void WriteCharEntity(char ch)
	{
		AddString(char.ToString(ch));
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		AddString(new string(buffer, index, count));
	}

	public override void WriteComment(string text)
	{
		AddNode(new XComment(text));
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		AddNode(new XDocumentType(name, pubid, sysid, subset));
	}

	public override void WriteEndAttribute()
	{
		XAttribute xAttribute = new XAttribute(_attrName, _attrValue);
		_attrName = null;
		_attrValue = null;
		if (_parent != null)
		{
			_parent.Add(xAttribute);
		}
		else
		{
			Add(xAttribute);
		}
	}

	public override void WriteEndDocument()
	{
	}

	public override void WriteEndElement()
	{
		_parent = ((XElement)_parent).parent;
	}

	public override void WriteEntityRef(string name)
	{
		switch (name)
		{
		case "amp":
			AddString("&");
			break;
		case "apos":
			AddString("'");
			break;
		case "gt":
			AddString(">");
			break;
		case "lt":
			AddString("<");
			break;
		case "quot":
			AddString("\"");
			break;
		default:
			throw new NotSupportedException(System.SR.NotSupported_WriteEntityRef);
		}
	}

	public override void WriteFullEndElement()
	{
		XElement xElement = (XElement)_parent;
		if (xElement.IsEmpty)
		{
			xElement.Add(string.Empty);
		}
		_parent = xElement.parent;
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (!(name == "xml"))
		{
			AddNode(new XProcessingInstruction(name, text));
		}
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		AddString(new string(buffer, index, count));
	}

	public override void WriteRaw(string data)
	{
		AddString(data);
	}

	public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
	{
		if (prefix == null)
		{
			throw new ArgumentNullException("prefix");
		}
		_attrName = XNamespace.Get((prefix.Length == 0) ? string.Empty : namespaceName).GetName(localName);
		_attrValue = string.Empty;
	}

	public override void WriteStartDocument()
	{
	}

	public override void WriteStartDocument(bool standalone)
	{
	}

	public override void WriteStartElement(string prefix, string localName, string namespaceName)
	{
		AddNode(new XElement(XNamespace.Get(namespaceName).GetName(localName)));
	}

	public override void WriteString(string text)
	{
		AddString(text);
	}

	public override void WriteSurrogateCharEntity(char lowCh, char highCh)
	{
		Span<char> span = stackalloc char[2] { highCh, lowCh };
		ReadOnlySpan<char> value = span;
		AddString(new string(value));
	}

	public override void WriteValue(DateTimeOffset value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	public override void WriteWhitespace(string ws)
	{
		AddString(ws);
	}

	private void Add(object o)
	{
		if (_content == null)
		{
			_content = new List<object>();
		}
		_content.Add(o);
	}

	private void AddNode(XNode n)
	{
		if (_parent != null)
		{
			_parent.Add(n);
		}
		else
		{
			Add(n);
		}
		if (n is XContainer parent)
		{
			_parent = parent;
		}
	}

	private void AddString(string s)
	{
		if (s != null)
		{
			if (_attrValue != null)
			{
				_attrValue += s;
			}
			else if (_parent != null)
			{
				_parent.Add(s);
			}
			else
			{
				Add(s);
			}
		}
	}
}
