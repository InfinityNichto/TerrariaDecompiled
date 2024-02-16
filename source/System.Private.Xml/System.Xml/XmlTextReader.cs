using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace System.Xml;

[EditorBrowsable(EditorBrowsableState.Never)]
public class XmlTextReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
{
	private readonly XmlTextReaderImpl _impl;

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

	public override bool CanReadValueChunk => false;

	public int LineNumber => _impl.LineNumber;

	public int LinePosition => _impl.LinePosition;

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

	public bool Normalization
	{
		get
		{
			return _impl.Normalization;
		}
		set
		{
			_impl.Normalization = value;
		}
	}

	public Encoding? Encoding => _impl.Encoding;

	public WhitespaceHandling WhitespaceHandling
	{
		get
		{
			return _impl.WhitespaceHandling;
		}
		set
		{
			_impl.WhitespaceHandling = value;
		}
	}

	[Obsolete("XmlTextReader.ProhibitDtd has been deprecated. Use DtdProcessing instead.")]
	public bool ProhibitDtd
	{
		get
		{
			return _impl.DtdProcessing == DtdProcessing.Prohibit;
		}
		set
		{
			_impl.DtdProcessing = ((!value) ? DtdProcessing.Parse : DtdProcessing.Prohibit);
		}
	}

	public DtdProcessing DtdProcessing
	{
		get
		{
			return _impl.DtdProcessing;
		}
		set
		{
			_impl.DtdProcessing = value;
		}
	}

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

	public XmlResolver? XmlResolver
	{
		set
		{
			_impl.XmlResolver = value;
		}
	}

	internal XmlTextReaderImpl Impl => _impl;

	internal override XmlNamespaceManager? NamespaceManager => _impl.NamespaceManager;

	internal bool XmlValidatingReaderCompatibilityMode
	{
		set
		{
			_impl.XmlValidatingReaderCompatibilityMode = value;
		}
	}

	internal override IDtdInfo? DtdInfo => _impl.DtdInfo;

	protected XmlTextReader()
	{
		_impl = new XmlTextReaderImpl();
		_impl.OuterReader = this;
	}

	protected XmlTextReader(XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(nt);
		_impl.OuterReader = this;
	}

	public XmlTextReader(Stream input)
	{
		_impl = new XmlTextReaderImpl(input);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url, Stream input)
	{
		_impl = new XmlTextReaderImpl(url, input);
		_impl.OuterReader = this;
	}

	public XmlTextReader(Stream input, XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(input, nt);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url, Stream input, XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(url, input, nt);
		_impl.OuterReader = this;
	}

	public XmlTextReader(TextReader input)
	{
		_impl = new XmlTextReaderImpl(input);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url, TextReader input)
	{
		_impl = new XmlTextReaderImpl(url, input);
		_impl.OuterReader = this;
	}

	public XmlTextReader(TextReader input, XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(input, nt);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url, TextReader input, XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(url, input, nt);
		_impl.OuterReader = this;
	}

	public XmlTextReader(Stream xmlFragment, XmlNodeType fragType, XmlParserContext? context)
	{
		_impl = new XmlTextReaderImpl(xmlFragment, fragType, context);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string xmlFragment, XmlNodeType fragType, XmlParserContext? context)
	{
		_impl = new XmlTextReaderImpl(xmlFragment, fragType, context);
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url)
	{
		_impl = new XmlTextReaderImpl(url, new NameTable());
		_impl.OuterReader = this;
	}

	public XmlTextReader(string url, XmlNameTable nt)
	{
		_impl = new XmlTextReaderImpl(url, nt);
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

	public override void Skip()
	{
		_impl.Skip();
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

	public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _impl.GetNamespacesInScope(scope);
	}

	public void ResetState()
	{
		_impl.ResetState();
	}

	public TextReader GetRemainder()
	{
		return _impl.GetRemainder();
	}

	public int ReadChars(char[] buffer, int index, int count)
	{
		return _impl.ReadChars(buffer, index, count);
	}

	public int ReadBase64(byte[] array, int offset, int len)
	{
		return _impl.ReadBase64(array, offset, len);
	}

	public int ReadBinHex(byte[] array, int offset, int len)
	{
		return _impl.ReadBinHex(array, offset, len);
	}
}
