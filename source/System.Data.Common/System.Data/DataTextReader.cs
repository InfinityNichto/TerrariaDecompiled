using System.Xml;

namespace System.Data;

internal sealed class DataTextReader : XmlReader
{
	private readonly XmlReader _xmlreader;

	public override XmlReaderSettings Settings => _xmlreader.Settings;

	public override XmlNodeType NodeType => _xmlreader.NodeType;

	public override string Name => _xmlreader.Name;

	public override string LocalName => _xmlreader.LocalName;

	public override string NamespaceURI => _xmlreader.NamespaceURI;

	public override string Prefix => _xmlreader.Prefix;

	public override bool HasValue => _xmlreader.HasValue;

	public override string Value => _xmlreader.Value;

	public override int Depth => _xmlreader.Depth;

	public override string BaseURI => _xmlreader.BaseURI;

	public override bool IsEmptyElement => _xmlreader.IsEmptyElement;

	public override bool IsDefault => _xmlreader.IsDefault;

	public override char QuoteChar => _xmlreader.QuoteChar;

	public override XmlSpace XmlSpace => _xmlreader.XmlSpace;

	public override string XmlLang => _xmlreader.XmlLang;

	public override int AttributeCount => _xmlreader.AttributeCount;

	public override bool EOF => _xmlreader.EOF;

	public override ReadState ReadState => _xmlreader.ReadState;

	public override XmlNameTable NameTable => _xmlreader.NameTable;

	public override bool CanResolveEntity => _xmlreader.CanResolveEntity;

	public override bool CanReadBinaryContent => _xmlreader.CanReadBinaryContent;

	public override bool CanReadValueChunk => _xmlreader.CanReadValueChunk;

	internal static XmlReader CreateReader(XmlReader xr)
	{
		return new DataTextReader(xr);
	}

	private DataTextReader(XmlReader input)
	{
		_xmlreader = input;
	}

	public override string GetAttribute(string name)
	{
		return _xmlreader.GetAttribute(name);
	}

	public override string GetAttribute(string localName, string namespaceURI)
	{
		return _xmlreader.GetAttribute(localName, namespaceURI);
	}

	public override string GetAttribute(int i)
	{
		return _xmlreader.GetAttribute(i);
	}

	public override bool MoveToAttribute(string name)
	{
		return _xmlreader.MoveToAttribute(name);
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		return _xmlreader.MoveToAttribute(localName, namespaceURI);
	}

	public override void MoveToAttribute(int i)
	{
		_xmlreader.MoveToAttribute(i);
	}

	public override bool MoveToFirstAttribute()
	{
		return _xmlreader.MoveToFirstAttribute();
	}

	public override bool MoveToNextAttribute()
	{
		return _xmlreader.MoveToNextAttribute();
	}

	public override bool MoveToElement()
	{
		return _xmlreader.MoveToElement();
	}

	public override bool ReadAttributeValue()
	{
		return _xmlreader.ReadAttributeValue();
	}

	public override bool Read()
	{
		return _xmlreader.Read();
	}

	public override void Close()
	{
		_xmlreader.Close();
	}

	public override void Skip()
	{
		_xmlreader.Skip();
	}

	public override string LookupNamespace(string prefix)
	{
		return _xmlreader.LookupNamespace(prefix);
	}

	public override void ResolveEntity()
	{
		_xmlreader.ResolveEntity();
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		return _xmlreader.ReadContentAsBase64(buffer, index, count);
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		return _xmlreader.ReadElementContentAsBase64(buffer, index, count);
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		return _xmlreader.ReadContentAsBinHex(buffer, index, count);
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		return _xmlreader.ReadElementContentAsBinHex(buffer, index, count);
	}

	public override string ReadString()
	{
		return _xmlreader.ReadString();
	}
}
