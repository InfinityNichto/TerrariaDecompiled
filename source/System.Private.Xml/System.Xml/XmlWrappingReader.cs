using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml;

internal class XmlWrappingReader : XmlReader, IXmlLineInfo
{
	protected XmlReader reader;

	protected IXmlLineInfo readerAsIXmlLineInfo;

	public override XmlReaderSettings Settings => reader.Settings;

	public override XmlNodeType NodeType => reader.NodeType;

	public override string Name => reader.Name;

	public override string LocalName => reader.LocalName;

	public override string NamespaceURI => reader.NamespaceURI;

	public override string Prefix => reader.Prefix;

	public override bool HasValue => reader.HasValue;

	public override string Value => reader.Value;

	public override int Depth => reader.Depth;

	public override string BaseURI => reader.BaseURI;

	public override bool IsEmptyElement => reader.IsEmptyElement;

	public override bool IsDefault => reader.IsDefault;

	public override XmlSpace XmlSpace => reader.XmlSpace;

	public override string XmlLang => reader.XmlLang;

	public override Type ValueType => reader.ValueType;

	public override int AttributeCount => reader.AttributeCount;

	public override bool EOF => reader.EOF;

	public override ReadState ReadState => reader.ReadState;

	public override bool HasAttributes => reader.HasAttributes;

	public override XmlNameTable NameTable => reader.NameTable;

	public override bool CanResolveEntity => reader.CanResolveEntity;

	public override IXmlSchemaInfo SchemaInfo => reader.SchemaInfo;

	public override char QuoteChar => reader.QuoteChar;

	public virtual int LineNumber
	{
		get
		{
			if (readerAsIXmlLineInfo != null)
			{
				return readerAsIXmlLineInfo.LineNumber;
			}
			return 0;
		}
	}

	public virtual int LinePosition
	{
		get
		{
			if (readerAsIXmlLineInfo != null)
			{
				return readerAsIXmlLineInfo.LinePosition;
			}
			return 0;
		}
	}

	internal override IDtdInfo DtdInfo => reader.DtdInfo;

	internal XmlWrappingReader(XmlReader baseReader)
	{
		reader = baseReader;
		readerAsIXmlLineInfo = baseReader as IXmlLineInfo;
	}

	public override string GetAttribute(string name)
	{
		return reader.GetAttribute(name);
	}

	public override string GetAttribute(string name, string namespaceURI)
	{
		return reader.GetAttribute(name, namespaceURI);
	}

	public override string GetAttribute(int i)
	{
		return reader.GetAttribute(i);
	}

	public override bool MoveToAttribute(string name)
	{
		return reader.MoveToAttribute(name);
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		return reader.MoveToAttribute(name, ns);
	}

	public override void MoveToAttribute(int i)
	{
		reader.MoveToAttribute(i);
	}

	public override bool MoveToFirstAttribute()
	{
		return reader.MoveToFirstAttribute();
	}

	public override bool MoveToNextAttribute()
	{
		return reader.MoveToNextAttribute();
	}

	public override bool MoveToElement()
	{
		return reader.MoveToElement();
	}

	public override bool Read()
	{
		return reader.Read();
	}

	public override void Close()
	{
		reader.Close();
	}

	public override void Skip()
	{
		reader.Skip();
	}

	public override string LookupNamespace(string prefix)
	{
		return reader.LookupNamespace(prefix);
	}

	public override void ResolveEntity()
	{
		reader.ResolveEntity();
	}

	public override bool ReadAttributeValue()
	{
		return reader.ReadAttributeValue();
	}

	public virtual bool HasLineInfo()
	{
		if (readerAsIXmlLineInfo != null)
		{
			return readerAsIXmlLineInfo.HasLineInfo();
		}
		return false;
	}

	public override Task<string> GetValueAsync()
	{
		return reader.GetValueAsync();
	}

	public override Task<bool> ReadAsync()
	{
		return reader.ReadAsync();
	}

	public override Task SkipAsync()
	{
		return reader.SkipAsync();
	}
}
