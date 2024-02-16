using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml;

[DebuggerDisplay("{DebuggerDisplayProxy}")]
[DebuggerDisplay("{DebuggerDisplayProxy}")]
public abstract class XmlReader : IDisposable
{
	[DebuggerDisplay("{ToString()}")]
	private readonly struct XmlReaderDebuggerDisplayProxy
	{
		private readonly XmlReader _reader;

		internal XmlReaderDebuggerDisplayProxy(XmlReader reader)
		{
			_reader = reader;
		}

		public override string ToString()
		{
			XmlNodeType nodeType = _reader.NodeType;
			string text = nodeType.ToString();
			switch (nodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.EntityReference:
			case XmlNodeType.EndElement:
			case XmlNodeType.EndEntity:
				text = text + ", Name=\"" + _reader.Name + "\"";
				break;
			case XmlNodeType.Attribute:
			case XmlNodeType.ProcessingInstruction:
				text = text + ", Name=\"" + _reader.Name + "\", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + "\"";
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Comment:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			case XmlNodeType.XmlDeclaration:
				text = text + ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + "\"";
				break;
			case XmlNodeType.DocumentType:
				text = text + ", Name=\"" + _reader.Name + "'";
				text = text + ", SYSTEM=\"" + _reader.GetAttribute("SYSTEM") + "\"";
				text = text + ", PUBLIC=\"" + _reader.GetAttribute("PUBLIC") + "\"";
				text = text + ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + "\"";
				break;
			}
			return text;
		}
	}

	public virtual XmlReaderSettings? Settings => null;

	public abstract XmlNodeType NodeType { get; }

	public virtual string Name
	{
		get
		{
			if (Prefix.Length != 0)
			{
				return NameTable.Add(Prefix + ":" + LocalName);
			}
			return LocalName;
		}
	}

	public abstract string LocalName { get; }

	public abstract string NamespaceURI { get; }

	public abstract string Prefix { get; }

	public virtual bool HasValue => HasValueInternal(NodeType);

	public abstract string Value { get; }

	public abstract int Depth { get; }

	public abstract string BaseURI { get; }

	public abstract bool IsEmptyElement { get; }

	public virtual bool IsDefault => false;

	public virtual char QuoteChar => '"';

	public virtual XmlSpace XmlSpace => XmlSpace.None;

	public virtual string XmlLang => string.Empty;

	public virtual IXmlSchemaInfo? SchemaInfo => this as IXmlSchemaInfo;

	public virtual Type ValueType => typeof(string);

	public abstract int AttributeCount { get; }

	public virtual string this[int i] => GetAttribute(i);

	public virtual string? this[string name] => GetAttribute(name);

	public virtual string? this[string name, string? namespaceURI] => GetAttribute(name, namespaceURI);

	public abstract bool EOF { get; }

	public abstract ReadState ReadState { get; }

	public abstract XmlNameTable NameTable { get; }

	public virtual bool CanResolveEntity => false;

	public virtual bool CanReadBinaryContent => false;

	public virtual bool CanReadValueChunk => false;

	public virtual bool HasAttributes => AttributeCount > 0;

	internal virtual XmlNamespaceManager? NamespaceManager => null;

	internal bool IsDefaultInternal
	{
		get
		{
			if (IsDefault)
			{
				return true;
			}
			IXmlSchemaInfo schemaInfo = SchemaInfo;
			if (schemaInfo != null && schemaInfo.IsDefault)
			{
				return true;
			}
			return false;
		}
	}

	internal virtual IDtdInfo? DtdInfo => null;

	private object DebuggerDisplayProxy => new XmlReaderDebuggerDisplayProxy(this);

	public virtual object ReadContentAsObject()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsObject");
		}
		return InternalReadContentAsString();
	}

	public virtual bool ReadContentAsBoolean()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsBoolean");
		}
		try
		{
			return XmlConvert.ToBoolean(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException, this as IXmlLineInfo);
		}
	}

	public virtual DateTime ReadContentAsDateTime()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsDateTime");
		}
		try
		{
			return XmlConvert.ToDateTime(InternalReadContentAsString(), XmlDateTimeSerializationMode.RoundtripKind);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException, this as IXmlLineInfo);
		}
	}

	public virtual DateTimeOffset ReadContentAsDateTimeOffset()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsDateTimeOffset");
		}
		try
		{
			return XmlConvert.ToDateTimeOffset(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTimeOffset", innerException, this as IXmlLineInfo);
		}
	}

	public virtual double ReadContentAsDouble()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsDouble");
		}
		try
		{
			return XmlConvert.ToDouble(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException, this as IXmlLineInfo);
		}
	}

	public virtual float ReadContentAsFloat()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsFloat");
		}
		try
		{
			return XmlConvert.ToSingle(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException, this as IXmlLineInfo);
		}
	}

	public virtual decimal ReadContentAsDecimal()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsDecimal");
		}
		try
		{
			return XmlConvert.ToDecimal(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException, this as IXmlLineInfo);
		}
	}

	public virtual int ReadContentAsInt()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsInt");
		}
		try
		{
			return XmlConvert.ToInt32(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException, this as IXmlLineInfo);
		}
	}

	public virtual long ReadContentAsLong()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsLong");
		}
		try
		{
			return XmlConvert.ToInt64(InternalReadContentAsString());
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException, this as IXmlLineInfo);
		}
	}

	public virtual string ReadContentAsString()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsString");
		}
		return InternalReadContentAsString();
	}

	public virtual object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver)
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAs");
		}
		string text = InternalReadContentAsString();
		if (returnType == typeof(string))
		{
			return text;
		}
		try
		{
			return XmlUntypedStringConverter.Instance.FromString(text, returnType, namespaceResolver ?? (this as IXmlNamespaceResolver));
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this as IXmlLineInfo);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this as IXmlLineInfo);
		}
	}

	public virtual object ReadElementContentAsObject()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsObject"))
		{
			object result = ReadContentAsObject();
			FinishReadElementContentAsXxx();
			return result;
		}
		return string.Empty;
	}

	public virtual object ReadElementContentAsObject(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsObject();
	}

	public virtual bool ReadElementContentAsBoolean()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsBoolean"))
		{
			bool result = ReadContentAsBoolean();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToBoolean(string.Empty);
	}

	public virtual bool ReadElementContentAsBoolean(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsBoolean();
	}

	public virtual DateTime ReadElementContentAsDateTime()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsDateTime"))
		{
			DateTime result = ReadContentAsDateTime();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToDateTime(string.Empty, XmlDateTimeSerializationMode.RoundtripKind);
	}

	public virtual DateTime ReadElementContentAsDateTime(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsDateTime();
	}

	public virtual double ReadElementContentAsDouble()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsDouble"))
		{
			double result = ReadContentAsDouble();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToDouble(string.Empty);
	}

	public virtual double ReadElementContentAsDouble(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsDouble();
	}

	public virtual float ReadElementContentAsFloat()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsFloat"))
		{
			float result = ReadContentAsFloat();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToSingle(string.Empty);
	}

	public virtual float ReadElementContentAsFloat(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsFloat();
	}

	public virtual decimal ReadElementContentAsDecimal()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsDecimal"))
		{
			decimal result = ReadContentAsDecimal();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToDecimal(string.Empty);
	}

	public virtual decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsDecimal();
	}

	public virtual int ReadElementContentAsInt()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsInt"))
		{
			int result = ReadContentAsInt();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToInt32(string.Empty);
	}

	public virtual int ReadElementContentAsInt(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsInt();
	}

	public virtual long ReadElementContentAsLong()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsLong"))
		{
			long result = ReadContentAsLong();
			FinishReadElementContentAsXxx();
			return result;
		}
		return XmlConvert.ToInt64(string.Empty);
	}

	public virtual long ReadElementContentAsLong(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsLong();
	}

	public virtual string ReadElementContentAsString()
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAsString"))
		{
			string result = ReadContentAsString();
			FinishReadElementContentAsXxx();
			return result;
		}
		return string.Empty;
	}

	public virtual string ReadElementContentAsString(string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAsString();
	}

	public virtual object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (SetupReadElementContentAsXxx("ReadElementContentAs"))
		{
			object result = ReadContentAs(returnType, namespaceResolver);
			FinishReadElementContentAsXxx();
			return result;
		}
		if (!(returnType == typeof(string)))
		{
			return XmlUntypedStringConverter.Instance.FromString(string.Empty, returnType, namespaceResolver);
		}
		return string.Empty;
	}

	public virtual object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
	{
		CheckElement(localName, namespaceURI);
		return ReadElementContentAs(returnType, namespaceResolver);
	}

	public abstract string? GetAttribute(string name);

	public abstract string? GetAttribute(string name, string? namespaceURI);

	public abstract string GetAttribute(int i);

	public abstract bool MoveToAttribute(string name);

	public abstract bool MoveToAttribute(string name, string? ns);

	public virtual void MoveToAttribute(int i)
	{
		if (i < 0 || i >= AttributeCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		MoveToElement();
		MoveToFirstAttribute();
		for (int j = 0; j < i; j++)
		{
			MoveToNextAttribute();
		}
	}

	public abstract bool MoveToFirstAttribute();

	public abstract bool MoveToNextAttribute();

	public abstract bool MoveToElement();

	public abstract bool ReadAttributeValue();

	public abstract bool Read();

	public virtual void Close()
	{
	}

	public virtual void Skip()
	{
		if (ReadState == ReadState.Interactive)
		{
			SkipSubtree();
		}
	}

	public abstract string? LookupNamespace(string prefix);

	public abstract void ResolveEntity();

	public virtual int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBase64"));
	}

	public virtual int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBase64"));
	}

	public virtual int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBinHex"));
	}

	public virtual int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBinHex"));
	}

	public virtual int ReadValueChunk(char[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Xml_ReadValueChunkNotSupported);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual string ReadString()
	{
		if (ReadState != ReadState.Interactive)
		{
			return string.Empty;
		}
		MoveToElement();
		if (NodeType == XmlNodeType.Element)
		{
			if (IsEmptyElement)
			{
				return string.Empty;
			}
			if (!Read())
			{
				throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
			}
			if (NodeType == XmlNodeType.EndElement)
			{
				return string.Empty;
			}
		}
		string text = string.Empty;
		while (IsTextualNode(NodeType))
		{
			text += Value;
			if (!Read())
			{
				break;
			}
		}
		return text;
	}

	public virtual XmlNodeType MoveToContent()
	{
		do
		{
			switch (NodeType)
			{
			case XmlNodeType.Attribute:
				MoveToElement();
				break;
			case XmlNodeType.Element:
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.EntityReference:
			case XmlNodeType.EndElement:
			case XmlNodeType.EndEntity:
				break;
			default:
				continue;
			}
			return NodeType;
		}
		while (Read());
		return NodeType;
	}

	public virtual void ReadStartElement()
	{
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		Read();
	}

	public virtual void ReadStartElement(string name)
	{
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (Name == name)
		{
			Read();
			return;
		}
		throw new XmlException(System.SR.Xml_ElementNotFound, name, this as IXmlLineInfo);
	}

	public virtual void ReadStartElement(string localname, string ns)
	{
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (LocalName == localname && NamespaceURI == ns)
		{
			Read();
			return;
		}
		throw new XmlException(System.SR.Xml_ElementNotFoundNs, new string[2] { localname, ns }, this as IXmlLineInfo);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual string ReadElementString()
	{
		string result = string.Empty;
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (!IsEmptyElement)
		{
			Read();
			result = ReadString();
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_UnexpectedNodeInSimpleContent, new string[2]
				{
					NodeType.ToString(),
					"ReadElementString"
				}, this as IXmlLineInfo);
			}
			Read();
		}
		else
		{
			Read();
		}
		return result;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual string ReadElementString(string name)
	{
		string result = string.Empty;
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (Name != name)
		{
			throw new XmlException(System.SR.Xml_ElementNotFound, name, this as IXmlLineInfo);
		}
		if (!IsEmptyElement)
		{
			result = ReadString();
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
			}
			Read();
		}
		else
		{
			Read();
		}
		return result;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual string ReadElementString(string localname, string ns)
	{
		string result = string.Empty;
		if (MoveToContent() != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (LocalName != localname || NamespaceURI != ns)
		{
			throw new XmlException(System.SR.Xml_ElementNotFoundNs, new string[2] { localname, ns }, this as IXmlLineInfo);
		}
		if (!IsEmptyElement)
		{
			result = ReadString();
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
			}
			Read();
		}
		else
		{
			Read();
		}
		return result;
	}

	public virtual void ReadEndElement()
	{
		if (MoveToContent() != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		Read();
	}

	public virtual bool IsStartElement()
	{
		return MoveToContent() == XmlNodeType.Element;
	}

	public virtual bool IsStartElement(string name)
	{
		if (MoveToContent() == XmlNodeType.Element)
		{
			return Name == name;
		}
		return false;
	}

	public virtual bool IsStartElement(string localname, string ns)
	{
		if (MoveToContent() == XmlNodeType.Element)
		{
			if (LocalName == localname)
			{
				return NamespaceURI == ns;
			}
			return false;
		}
		return false;
	}

	public virtual bool ReadToFollowing(string name)
	{
		if (name == null || name.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(name, "name");
		}
		name = NameTable.Add(name);
		while (Read())
		{
			if (NodeType == XmlNodeType.Element && Ref.Equal(name, Name))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool ReadToFollowing(string localName, string namespaceURI)
	{
		if (localName == null || localName.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(localName, "localName");
		}
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		localName = NameTable.Add(localName);
		namespaceURI = NameTable.Add(namespaceURI);
		while (Read())
		{
			if (NodeType == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool ReadToDescendant(string name)
	{
		if (name == null || name.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(name, "name");
		}
		int num = Depth;
		if (NodeType != XmlNodeType.Element)
		{
			if (ReadState != 0)
			{
				return false;
			}
			num--;
		}
		else if (IsEmptyElement)
		{
			return false;
		}
		name = NameTable.Add(name);
		while (Read() && Depth > num)
		{
			if (NodeType == XmlNodeType.Element && Ref.Equal(name, Name))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool ReadToDescendant(string localName, string namespaceURI)
	{
		if (localName == null || localName.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(localName, "localName");
		}
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		int num = Depth;
		if (NodeType != XmlNodeType.Element)
		{
			if (ReadState != 0)
			{
				return false;
			}
			num--;
		}
		else if (IsEmptyElement)
		{
			return false;
		}
		localName = NameTable.Add(localName);
		namespaceURI = NameTable.Add(namespaceURI);
		while (Read() && Depth > num)
		{
			if (NodeType == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool ReadToNextSibling(string name)
	{
		if (name == null || name.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(name, "name");
		}
		name = NameTable.Add(name);
		while (SkipSubtree())
		{
			XmlNodeType nodeType = NodeType;
			if (nodeType == XmlNodeType.Element && Ref.Equal(name, Name))
			{
				return true;
			}
			if (nodeType == XmlNodeType.EndElement || EOF)
			{
				break;
			}
		}
		return false;
	}

	public virtual bool ReadToNextSibling(string localName, string namespaceURI)
	{
		if (localName == null || localName.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(localName, "localName");
		}
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		localName = NameTable.Add(localName);
		namespaceURI = NameTable.Add(namespaceURI);
		while (SkipSubtree())
		{
			XmlNodeType nodeType = NodeType;
			if (nodeType == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
			{
				return true;
			}
			if (nodeType == XmlNodeType.EndElement || EOF)
			{
				break;
			}
		}
		return false;
	}

	public static bool IsName(string str)
	{
		if (str == null)
		{
			throw new NullReferenceException();
		}
		return ValidateNames.IsNameNoNamespaces(str);
	}

	public static bool IsNameToken(string str)
	{
		if (str == null)
		{
			throw new NullReferenceException();
		}
		return ValidateNames.IsNmtokenNoNamespaces(str);
	}

	public virtual string ReadInnerXml()
	{
		if (ReadState != ReadState.Interactive)
		{
			return string.Empty;
		}
		if (NodeType != XmlNodeType.Attribute && NodeType != XmlNodeType.Element)
		{
			Read();
			return string.Empty;
		}
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriter xmlWriter = CreateWriterForInnerOuterXml(stringWriter);
		try
		{
			if (NodeType == XmlNodeType.Attribute)
			{
				((XmlTextWriter)xmlWriter).QuoteChar = QuoteChar;
				WriteAttributeValue(xmlWriter);
			}
			if (NodeType == XmlNodeType.Element)
			{
				WriteNode(xmlWriter, defattr: false);
			}
		}
		finally
		{
			xmlWriter.Close();
		}
		return stringWriter.ToString();
	}

	private void WriteNode(XmlWriter xtw, bool defattr)
	{
		int num = ((NodeType == XmlNodeType.None) ? (-1) : Depth);
		while (Read() && num < Depth)
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
				xtw.WriteStartElement(Prefix, LocalName, NamespaceURI);
				((XmlTextWriter)xtw).QuoteChar = QuoteChar;
				xtw.WriteAttributes(this, defattr);
				if (IsEmptyElement)
				{
					xtw.WriteEndElement();
				}
				break;
			case XmlNodeType.Text:
				xtw.WriteString(Value);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				xtw.WriteWhitespace(Value);
				break;
			case XmlNodeType.CDATA:
				xtw.WriteCData(Value);
				break;
			case XmlNodeType.EntityReference:
				xtw.WriteEntityRef(Name);
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.XmlDeclaration:
				xtw.WriteProcessingInstruction(Name, Value);
				break;
			case XmlNodeType.DocumentType:
				xtw.WriteDocType(Name, GetAttribute("PUBLIC"), GetAttribute("SYSTEM"), Value);
				break;
			case XmlNodeType.Comment:
				xtw.WriteComment(Value);
				break;
			case XmlNodeType.EndElement:
				xtw.WriteFullEndElement();
				break;
			}
		}
		if (num == Depth && NodeType == XmlNodeType.EndElement)
		{
			Read();
		}
	}

	private void WriteAttributeValue(XmlWriter xtw)
	{
		string name = Name;
		while (ReadAttributeValue())
		{
			if (NodeType == XmlNodeType.EntityReference)
			{
				xtw.WriteEntityRef(Name);
			}
			else
			{
				xtw.WriteString(Value);
			}
		}
		MoveToAttribute(name);
	}

	public virtual string ReadOuterXml()
	{
		if (ReadState != ReadState.Interactive)
		{
			return string.Empty;
		}
		if (NodeType != XmlNodeType.Attribute && NodeType != XmlNodeType.Element)
		{
			Read();
			return string.Empty;
		}
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriter xmlWriter = CreateWriterForInnerOuterXml(stringWriter);
		try
		{
			if (NodeType == XmlNodeType.Attribute)
			{
				xmlWriter.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
				WriteAttributeValue(xmlWriter);
				xmlWriter.WriteEndAttribute();
			}
			else
			{
				xmlWriter.WriteNode(this, defattr: false);
			}
		}
		finally
		{
			xmlWriter.Close();
		}
		return stringWriter.ToString();
	}

	private XmlWriter CreateWriterForInnerOuterXml(StringWriter sw)
	{
		XmlTextWriter xmlTextWriter = new XmlTextWriter(sw);
		SetNamespacesFlag(xmlTextWriter);
		return xmlTextWriter;
	}

	private void SetNamespacesFlag(XmlTextWriter xtw)
	{
		if (this is XmlTextReader xmlTextReader)
		{
			xtw.Namespaces = xmlTextReader.Namespaces;
		}
		else if (this is XmlValidatingReader xmlValidatingReader)
		{
			xtw.Namespaces = xmlValidatingReader.Namespaces;
		}
	}

	public virtual XmlReader ReadSubtree()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw new InvalidOperationException(System.SR.Xml_ReadSubtreeNotOnElement);
		}
		return new XmlSubtreeReader(this);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && ReadState != ReadState.Closed)
		{
			Close();
		}
	}

	internal static bool IsTextualNode(XmlNodeType nodeType)
	{
		return (0x6018uL & (ulong)(1 << (int)nodeType)) != 0;
	}

	internal static bool CanReadContentAs(XmlNodeType nodeType)
	{
		return (0x1E1BCuL & (ulong)(1 << (int)nodeType)) != 0;
	}

	internal static bool HasValueInternal(XmlNodeType nodeType)
	{
		return (0x2659CuL & (ulong)(1 << (int)nodeType)) != 0;
	}

	private bool SkipSubtree()
	{
		MoveToElement();
		if (NodeType == XmlNodeType.Element && !IsEmptyElement)
		{
			int depth = Depth;
			while (Read() && depth < Depth)
			{
			}
			if (NodeType == XmlNodeType.EndElement)
			{
				return Read();
			}
			return false;
		}
		return Read();
	}

	internal void CheckElement(string localName, string namespaceURI)
	{
		if (localName == null || localName.Length == 0)
		{
			throw XmlConvert.CreateInvalidNameArgumentException(localName, "localName");
		}
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		if (NodeType != XmlNodeType.Element)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString(), this as IXmlLineInfo);
		}
		if (LocalName != localName || NamespaceURI != namespaceURI)
		{
			throw new XmlException(System.SR.Xml_ElementNotFoundNs, new string[2] { localName, namespaceURI }, this as IXmlLineInfo);
		}
	}

	internal Exception CreateReadContentAsException(string methodName)
	{
		return CreateReadContentAsException(methodName, NodeType, this as IXmlLineInfo);
	}

	internal Exception CreateReadElementContentAsException(string methodName)
	{
		return CreateReadElementContentAsException(methodName, NodeType, this as IXmlLineInfo);
	}

	internal bool CanReadContentAs()
	{
		return CanReadContentAs(NodeType);
	}

	internal static Exception CreateReadContentAsException(string methodName, XmlNodeType nodeType, IXmlLineInfo lineInfo)
	{
		return new InvalidOperationException(AddLineInfo(System.SR.Format(System.SR.Xml_InvalidReadContentAs, methodName, nodeType), lineInfo));
	}

	internal static Exception CreateReadElementContentAsException(string methodName, XmlNodeType nodeType, IXmlLineInfo lineInfo)
	{
		return new InvalidOperationException(AddLineInfo(System.SR.Format(System.SR.Xml_InvalidReadElementContentAs, methodName, nodeType), lineInfo));
	}

	private static string AddLineInfo(string message, IXmlLineInfo lineInfo)
	{
		if (lineInfo != null)
		{
			object[] args = new object[2]
			{
				lineInfo.LineNumber.ToString(CultureInfo.InvariantCulture),
				lineInfo.LinePosition.ToString(CultureInfo.InvariantCulture)
			};
			message = message + " " + System.SR.Format(System.SR.Xml_ErrorPosition, args);
		}
		return message;
	}

	internal string InternalReadContentAsString()
	{
		string text = string.Empty;
		StringBuilder stringBuilder = null;
		bool num;
		do
		{
			switch (NodeType)
			{
			case XmlNodeType.Attribute:
				return Value;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (text.Length == 0)
				{
					text = Value;
				}
				else
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder();
						stringBuilder.Append(text);
					}
					stringBuilder.Append(Value);
				}
				goto case XmlNodeType.ProcessingInstruction;
			case XmlNodeType.EntityReference:
				if (!CanResolveEntity)
				{
					break;
				}
				ResolveEntity();
				goto case XmlNodeType.ProcessingInstruction;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.EndEntity:
				num = ((AttributeCount != 0) ? ReadAttributeValue() : Read());
				continue;
			}
			break;
		}
		while (num);
		if (stringBuilder != null)
		{
			return stringBuilder.ToString();
		}
		return text;
	}

	private bool SetupReadElementContentAsXxx(string methodName)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException(methodName);
		}
		bool isEmptyElement = IsEmptyElement;
		Read();
		if (isEmptyElement)
		{
			return false;
		}
		switch (NodeType)
		{
		case XmlNodeType.EndElement:
			Read();
			return false;
		case XmlNodeType.Element:
			throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
		default:
			return true;
		}
	}

	private void FinishReadElementContentAsXxx()
	{
		if (NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString());
		}
		Read();
	}

	internal static ConformanceLevel GetV1ConformanceLevel(XmlReader reader)
	{
		return GetXmlTextReaderImpl(reader)?.V1ComformanceLevel ?? ConformanceLevel.Document;
	}

	private static XmlTextReaderImpl GetXmlTextReaderImpl(XmlReader reader)
	{
		if (reader is XmlTextReaderImpl result)
		{
			return result;
		}
		if (reader is XmlTextReader xmlTextReader)
		{
			return xmlTextReader.Impl;
		}
		if (reader is XmlValidatingReaderImpl xmlValidatingReaderImpl)
		{
			return xmlValidatingReaderImpl.ReaderImpl;
		}
		if (reader is XmlValidatingReader xmlValidatingReader)
		{
			return xmlValidatingReader.Impl.ReaderImpl;
		}
		return null;
	}

	public static XmlReader Create(string inputUri)
	{
		if (inputUri == null)
		{
			throw new ArgumentNullException("inputUri");
		}
		if (inputUri.Length == 0)
		{
			throw new ArgumentException(System.SR.XmlConvert_BadUri, "inputUri");
		}
		return new XmlTextReaderImpl(inputUri, XmlReaderSettings.s_defaultReaderSettings, null, new XmlUrlResolver());
	}

	public static XmlReader Create(string inputUri, XmlReaderSettings? settings)
	{
		return Create(inputUri, settings, null);
	}

	public static XmlReader Create(string inputUri, XmlReaderSettings? settings, XmlParserContext? inputContext)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(inputUri, inputContext);
	}

	public static XmlReader Create(Stream input)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		return new XmlTextReaderImpl(input, null, 0, XmlReaderSettings.s_defaultReaderSettings, null, string.Empty, null, closeInput: false);
	}

	public static XmlReader Create(Stream input, XmlReaderSettings? settings)
	{
		return Create(input, settings, string.Empty);
	}

	public static XmlReader Create(Stream input, XmlReaderSettings? settings, string? baseUri)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(input, null, baseUri, null);
	}

	public static XmlReader Create(Stream input, XmlReaderSettings? settings, XmlParserContext? inputContext)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(input, null, string.Empty, inputContext);
	}

	public static XmlReader Create(TextReader input)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		return new XmlTextReaderImpl(input, XmlReaderSettings.s_defaultReaderSettings, string.Empty, null);
	}

	public static XmlReader Create(TextReader input, XmlReaderSettings? settings)
	{
		return Create(input, settings, string.Empty);
	}

	public static XmlReader Create(TextReader input, XmlReaderSettings? settings, string? baseUri)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(input, baseUri, null);
	}

	public static XmlReader Create(TextReader input, XmlReaderSettings? settings, XmlParserContext? inputContext)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(input, string.Empty, inputContext);
	}

	public static XmlReader Create(XmlReader reader, XmlReaderSettings? settings)
	{
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		return settings.CreateReader(reader);
	}

	internal static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (settings == null)
		{
			settings = XmlReaderSettings.s_defaultReaderSettings;
		}
		byte[] array = new byte[CalcBufferSize(input)];
		int num = 0;
		int num2;
		do
		{
			num2 = input.Read(array, num, array.Length - num);
			num += num2;
		}
		while (num2 > 0 && num < 2);
		XmlReader xmlReader;
		if (num >= 2 && array[0] == 223 && array[1] == byte.MaxValue)
		{
			if (inputContext != null)
			{
				throw new ArgumentException(System.SR.XmlBinary_NoParserContext, "inputContext");
			}
			xmlReader = new XmlSqlBinaryReader(input, array, num, string.Empty, settings.CloseInput, settings);
		}
		else
		{
			xmlReader = new XmlTextReaderImpl(input, array, num, settings, null, string.Empty, inputContext, settings.CloseInput);
		}
		if (settings.ValidationType != 0)
		{
			xmlReader = settings.AddValidation(xmlReader);
		}
		if (settings.Async)
		{
			xmlReader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(xmlReader);
		}
		return xmlReader;
	}

	internal static int CalcBufferSize(Stream input)
	{
		int num = 4096;
		if (input.CanSeek)
		{
			long length = input.Length;
			if (length < num)
			{
				num = checked((int)length);
			}
			else if (length > 65536)
			{
				num = 8192;
			}
		}
		return num;
	}

	public virtual Task<string> GetValueAsync()
	{
		throw new NotImplementedException();
	}

	public virtual async Task<object> ReadContentAsObjectAsync()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsObject");
		}
		return await InternalReadContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public virtual Task<string> ReadContentAsStringAsync()
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAsString");
		}
		return InternalReadContentAsStringAsync();
	}

	public virtual async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver? namespaceResolver)
	{
		if (!CanReadContentAs())
		{
			throw CreateReadContentAsException("ReadContentAs");
		}
		string text = await InternalReadContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (returnType == typeof(string))
		{
			return text;
		}
		try
		{
			return XmlUntypedConverter.Untyped.ChangeType(text, returnType, namespaceResolver ?? (this as IXmlNamespaceResolver));
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this as IXmlLineInfo);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this as IXmlLineInfo);
		}
	}

	public virtual async Task<object> ReadElementContentAsObjectAsync()
	{
		if (await SetupReadElementContentAsXxxAsync("ReadElementContentAsObject").ConfigureAwait(continueOnCapturedContext: false))
		{
			object value = await ReadContentAsObjectAsync().ConfigureAwait(continueOnCapturedContext: false);
			await FinishReadElementContentAsXxxAsync().ConfigureAwait(continueOnCapturedContext: false);
			return value;
		}
		return string.Empty;
	}

	public virtual async Task<string> ReadElementContentAsStringAsync()
	{
		if (await SetupReadElementContentAsXxxAsync("ReadElementContentAsString").ConfigureAwait(continueOnCapturedContext: false))
		{
			string value = await ReadContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
			await FinishReadElementContentAsXxxAsync().ConfigureAwait(continueOnCapturedContext: false);
			return value;
		}
		return string.Empty;
	}

	public virtual async Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (await SetupReadElementContentAsXxxAsync("ReadElementContentAs").ConfigureAwait(continueOnCapturedContext: false))
		{
			object value = await ReadContentAsAsync(returnType, namespaceResolver).ConfigureAwait(continueOnCapturedContext: false);
			await FinishReadElementContentAsXxxAsync().ConfigureAwait(continueOnCapturedContext: false);
			return value;
		}
		return (returnType == typeof(string)) ? string.Empty : XmlUntypedConverter.Untyped.ChangeType(string.Empty, returnType, namespaceResolver);
	}

	public virtual Task<bool> ReadAsync()
	{
		throw new NotImplementedException();
	}

	public virtual Task SkipAsync()
	{
		if (ReadState != ReadState.Interactive)
		{
			return Task.CompletedTask;
		}
		return SkipSubtreeAsync();
	}

	public virtual Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBase64"));
	}

	public virtual Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBase64"));
	}

	public virtual Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBinHex"));
	}

	public virtual Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBinHex"));
	}

	public virtual Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
	{
		throw new NotSupportedException(System.SR.Xml_ReadValueChunkNotSupported);
	}

	public virtual async Task<XmlNodeType> MoveToContentAsync()
	{
		do
		{
			switch (NodeType)
			{
			case XmlNodeType.Attribute:
				MoveToElement();
				break;
			case XmlNodeType.Element:
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.EntityReference:
			case XmlNodeType.EndElement:
			case XmlNodeType.EndEntity:
				break;
			default:
				continue;
			}
			return NodeType;
		}
		while (await ReadAsync().ConfigureAwait(continueOnCapturedContext: false));
		return NodeType;
	}

	public virtual async Task<string> ReadInnerXmlAsync()
	{
		if (ReadState != ReadState.Interactive)
		{
			return string.Empty;
		}
		if (NodeType != XmlNodeType.Attribute && NodeType != XmlNodeType.Element)
		{
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return string.Empty;
		}
		StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriter xtw = CreateWriterForInnerOuterXml(sw);
		try
		{
			if (NodeType == XmlNodeType.Attribute)
			{
				((XmlTextWriter)xtw).QuoteChar = QuoteChar;
				WriteAttributeValue(xtw);
			}
			if (NodeType == XmlNodeType.Element)
			{
				await WriteNodeAsync(xtw, defattr: false).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			xtw.Close();
		}
		return sw.ToString();
	}

	private async Task WriteNodeAsync(XmlWriter xtw, bool defattr)
	{
		int d = ((NodeType == XmlNodeType.None) ? (-1) : Depth);
		while (await ReadAsync().ConfigureAwait(continueOnCapturedContext: false) && d < Depth)
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
				xtw.WriteStartElement(Prefix, LocalName, NamespaceURI);
				((XmlTextWriter)xtw).QuoteChar = QuoteChar;
				xtw.WriteAttributes(this, defattr);
				if (IsEmptyElement)
				{
					xtw.WriteEndElement();
				}
				break;
			case XmlNodeType.Text:
			{
				XmlWriter xmlWriter = xtw;
				xmlWriter.WriteString(await GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				XmlWriter xmlWriter = xtw;
				xmlWriter.WriteWhitespace(await GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.CDATA:
				xtw.WriteCData(Value);
				break;
			case XmlNodeType.EntityReference:
				xtw.WriteEntityRef(Name);
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.XmlDeclaration:
				xtw.WriteProcessingInstruction(Name, Value);
				break;
			case XmlNodeType.DocumentType:
				xtw.WriteDocType(Name, GetAttribute("PUBLIC"), GetAttribute("SYSTEM"), Value);
				break;
			case XmlNodeType.Comment:
				xtw.WriteComment(Value);
				break;
			case XmlNodeType.EndElement:
				xtw.WriteFullEndElement();
				break;
			}
		}
		if (d == Depth && NodeType == XmlNodeType.EndElement)
		{
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public virtual async Task<string> ReadOuterXmlAsync()
	{
		if (ReadState != ReadState.Interactive)
		{
			return string.Empty;
		}
		if (NodeType != XmlNodeType.Attribute && NodeType != XmlNodeType.Element)
		{
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return string.Empty;
		}
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriter xmlWriter = CreateWriterForInnerOuterXml(stringWriter);
		try
		{
			if (NodeType == XmlNodeType.Attribute)
			{
				xmlWriter.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
				WriteAttributeValue(xmlWriter);
				xmlWriter.WriteEndAttribute();
			}
			else
			{
				xmlWriter.WriteNode(this, defattr: false);
			}
		}
		finally
		{
			xmlWriter.Close();
		}
		return stringWriter.ToString();
	}

	private async Task<bool> SkipSubtreeAsync()
	{
		MoveToElement();
		if (NodeType == XmlNodeType.Element && !IsEmptyElement)
		{
			int depth = Depth;
			while (await ReadAsync().ConfigureAwait(continueOnCapturedContext: false) && depth < Depth)
			{
			}
			if (NodeType == XmlNodeType.EndElement)
			{
				return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			return false;
		}
		return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	internal async Task<string> InternalReadContentAsStringAsync()
	{
		string value = string.Empty;
		StringBuilder sb = null;
		bool flag;
		do
		{
			switch (NodeType)
			{
			case XmlNodeType.Attribute:
				return Value;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (value.Length == 0)
				{
					value = await GetValueAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					if (sb == null)
					{
						sb = new StringBuilder();
						sb.Append(value);
					}
					StringBuilder stringBuilder = sb;
					stringBuilder.Append(await GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				}
				goto case XmlNodeType.ProcessingInstruction;
			case XmlNodeType.EntityReference:
				if (!CanResolveEntity)
				{
					break;
				}
				ResolveEntity();
				goto case XmlNodeType.ProcessingInstruction;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.EndEntity:
				flag = ((AttributeCount == 0) ? (await ReadAsync().ConfigureAwait(continueOnCapturedContext: false)) : ReadAttributeValue());
				continue;
			}
			break;
		}
		while (flag);
		return (sb == null) ? value : sb.ToString();
	}

	private async Task<bool> SetupReadElementContentAsXxxAsync(string methodName)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException(methodName);
		}
		bool isEmptyElement = IsEmptyElement;
		await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (isEmptyElement)
		{
			return false;
		}
		switch (NodeType)
		{
		case XmlNodeType.EndElement:
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return false;
		case XmlNodeType.Element:
			throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
		default:
			return true;
		}
	}

	private Task FinishReadElementContentAsXxxAsync()
	{
		if (NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, NodeType.ToString());
		}
		return ReadAsync();
	}
}
