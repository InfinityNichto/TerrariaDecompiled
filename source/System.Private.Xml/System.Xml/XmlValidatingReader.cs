using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Schema;

namespace System.Xml;

[Obsolete("XmlValidatingReader has been deprecated. Use XmlReader created by XmlReader.Create() method using appropriate XmlReaderSettings instead.")]
public class XmlValidatingReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
{
	private readonly XmlValidatingReaderImpl _impl;

	public override XmlNodeType NodeType => _impl.NodeType;

	public override string Name => _impl.Name;

	public override string LocalName => _impl.LocalName;

	public override string NamespaceURI => _impl.NamespaceURI;

	public override string Prefix => _impl.Prefix;

	public override bool HasValue => _impl.HasValue;

	public override string Value => _impl.Value;

	public override int Depth => _impl.Depth;

	public override string BaseURI => _impl.BaseURI;

	public override bool IsEmptyElement => _impl.IsEmptyElement;

	public override bool IsDefault => _impl.IsDefault;

	public override char QuoteChar => _impl.QuoteChar;

	public override XmlSpace XmlSpace => _impl.XmlSpace;

	public override string XmlLang => _impl.XmlLang;

	public override int AttributeCount => _impl.AttributeCount;

	public override bool EOF => _impl.EOF;

	public override ReadState ReadState => _impl.ReadState;

	public override XmlNameTable NameTable => _impl.NameTable;

	public override bool CanResolveEntity => true;

	public override bool CanReadBinaryContent => true;

	public int LineNumber => _impl.LineNumber;

	public int LinePosition => _impl.LinePosition;

	public object? SchemaType => _impl.SchemaType;

	public XmlReader Reader => _impl.Reader;

	public ValidationType ValidationType
	{
		get
		{
			return _impl.ValidationType;
		}
		set
		{
			_impl.ValidationType = value;
		}
	}

	public XmlSchemaCollection Schemas => _impl.Schemas;

	public EntityHandling EntityHandling
	{
		get
		{
			return _impl.EntityHandling;
		}
		set
		{
			_impl.EntityHandling = value;
		}
	}

	public XmlResolver XmlResolver
	{
		set
		{
			_impl.XmlResolver = value;
		}
	}

	public bool Namespaces
	{
		get
		{
			return _impl.Namespaces;
		}
		set
		{
			_impl.Namespaces = value;
		}
	}

	public Encoding? Encoding => _impl.Encoding;

	internal XmlValidatingReaderImpl Impl => _impl;

	internal override IDtdInfo? DtdInfo => _impl.DtdInfo;

	public event ValidationEventHandler ValidationEventHandler
	{
		add
		{
			_impl.ValidationEventHandler += value;
		}
		remove
		{
			_impl.ValidationEventHandler -= value;
		}
	}

	public XmlValidatingReader(XmlReader reader)
	{
		_impl = new XmlValidatingReaderImpl(reader);
		_impl.OuterReader = this;
	}

	public XmlValidatingReader(string xmlFragment, XmlNodeType fragType, XmlParserContext context)
	{
		if (xmlFragment == null)
		{
			throw new ArgumentNullException("xmlFragment");
		}
		_impl = new XmlValidatingReaderImpl(xmlFragment, fragType, context);
		_impl.OuterReader = this;
	}

	public XmlValidatingReader(Stream xmlFragment, XmlNodeType fragType, XmlParserContext context)
	{
		if (xmlFragment == null)
		{
			throw new ArgumentNullException("xmlFragment");
		}
		_impl = new XmlValidatingReaderImpl(xmlFragment, fragType, context);
		_impl.OuterReader = this;
	}

	public override string? GetAttribute(string name)
	{
		return _impl.GetAttribute(name);
	}

	public override string? GetAttribute(string localName, string? namespaceURI)
	{
		return _impl.GetAttribute(localName, namespaceURI);
	}

	public override string GetAttribute(int i)
	{
		return _impl.GetAttribute(i);
	}

	public override bool MoveToAttribute(string name)
	{
		return _impl.MoveToAttribute(name);
	}

	public override bool MoveToAttribute(string localName, string? namespaceURI)
	{
		return _impl.MoveToAttribute(localName, namespaceURI);
	}

	public override void MoveToAttribute(int i)
	{
		_impl.MoveToAttribute(i);
	}

	public override bool MoveToFirstAttribute()
	{
		return _impl.MoveToFirstAttribute();
	}

	public override bool MoveToNextAttribute()
	{
		return _impl.MoveToNextAttribute();
	}

	public override bool MoveToElement()
	{
		return _impl.MoveToElement();
	}

	public override bool ReadAttributeValue()
	{
		return _impl.ReadAttributeValue();
	}

	public override bool Read()
	{
		return _impl.Read();
	}

	public override void Close()
	{
		_impl.Close();
	}

	public override string? LookupNamespace(string prefix)
	{
		string text = _impl.LookupNamespace(prefix);
		if (text != null && text.Length == 0)
		{
			text = null;
		}
		return text;
	}

	public override void ResolveEntity()
	{
		_impl.ResolveEntity();
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		return _impl.ReadContentAsBase64(buffer, index, count);
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		return _impl.ReadElementContentAsBase64(buffer, index, count);
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		return _impl.ReadContentAsBinHex(buffer, index, count);
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		return _impl.ReadElementContentAsBinHex(buffer, index, count);
	}

	public override string ReadString()
	{
		_impl.MoveOffEntityReference();
		return base.ReadString();
	}

	public bool HasLineInfo()
	{
		return true;
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _impl.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		return _impl.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return _impl.LookupPrefix(namespaceName);
	}

	public object? ReadTypedValue()
	{
		return _impl.ReadTypedValue();
	}
}
