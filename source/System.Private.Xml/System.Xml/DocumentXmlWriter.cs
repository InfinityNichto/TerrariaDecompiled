using System.Collections.Generic;

namespace System.Xml;

internal sealed class DocumentXmlWriter : XmlRawWriter, IXmlNamespaceResolver
{
	private enum State
	{
		Error,
		Attribute,
		Prolog,
		Fragment,
		Content,
		Last
	}

	private enum Method
	{
		WriteXmlDeclaration,
		WriteStartDocument,
		WriteEndDocument,
		WriteDocType,
		WriteStartElement,
		WriteEndElement,
		WriteFullEndElement,
		WriteStartAttribute,
		WriteEndAttribute,
		WriteStartNamespaceDeclaration,
		WriteEndNamespaceDeclaration,
		WriteCData,
		WriteComment,
		WriteProcessingInstruction,
		WriteEntityRef,
		WriteWhitespace,
		WriteString
	}

	private readonly DocumentXmlWriterType _type;

	private readonly XmlNode _start;

	private readonly XmlDocument _document;

	private XmlNamespaceManager _namespaceManager;

	private State _state;

	private XmlNode _write;

	private readonly List<XmlNode> _fragment;

	private readonly XmlWriterSettings _settings;

	private DocumentXPathNavigator _navigator;

	private XmlNode _end;

	private static readonly State[] s_changeState = new State[85]
	{
		State.Error,
		State.Error,
		State.Prolog,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Prolog,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Prolog,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Prolog,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Prolog,
		State.Content,
		State.Content,
		State.Error,
		State.Error,
		State.Error,
		State.Content,
		State.Content
	};

	public XmlNamespaceManager NamespaceManager
	{
		set
		{
			_namespaceManager = value;
		}
	}

	public override XmlWriterSettings Settings => _settings;

	public DocumentXPathNavigator Navigator
	{
		set
		{
			_navigator = value;
		}
	}

	public XmlNode EndNode
	{
		set
		{
			_end = value;
		}
	}

	internal override bool SupportsNamespaceDeclarationInChunks => true;

	public DocumentXmlWriter(DocumentXmlWriterType type, XmlNode start, XmlDocument document)
	{
		_type = type;
		_start = start;
		_document = document;
		_state = StartState();
		_fragment = new List<XmlNode>();
		_settings = new XmlWriterSettings();
		_settings.ReadOnly = false;
		_settings.CheckCharacters = false;
		_settings.CloseOutput = false;
		_settings.ConformanceLevel = ((_state != State.Prolog) ? ConformanceLevel.Fragment : ConformanceLevel.Document);
		_settings.ReadOnly = true;
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
		VerifyState(Method.WriteXmlDeclaration);
		if (standalone != 0)
		{
			XmlNode node = _document.CreateXmlDeclaration("1.0", string.Empty, (standalone == XmlStandalone.Yes) ? "yes" : "no");
			AddChild(node, _write);
		}
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
		VerifyState(Method.WriteXmlDeclaration);
		XmlLoader.ParseXmlDeclarationValue(xmldecl, out var version, out var encoding, out var standalone);
		XmlNode node = _document.CreateXmlDeclaration(version, encoding, standalone);
		AddChild(node, _write);
	}

	public override void WriteStartDocument()
	{
		VerifyState(Method.WriteStartDocument);
	}

	public override void WriteStartDocument(bool standalone)
	{
		VerifyState(Method.WriteStartDocument);
	}

	public override void WriteEndDocument()
	{
		VerifyState(Method.WriteEndDocument);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		VerifyState(Method.WriteDocType);
		XmlNode node = _document.CreateDocumentType(name, pubid, sysid, subset);
		AddChild(node, _write);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		VerifyState(Method.WriteStartElement);
		XmlNode xmlNode = _document.CreateElement(prefix, localName, ns);
		AddChild(xmlNode, _write);
		_write = xmlNode;
	}

	public override void WriteEndElement()
	{
		VerifyState(Method.WriteEndElement);
		if (_write == null)
		{
			throw new InvalidOperationException();
		}
		_write = _write.ParentNode;
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		WriteEndElement();
	}

	public override void WriteFullEndElement()
	{
		VerifyState(Method.WriteFullEndElement);
		if (!(_write is XmlElement xmlElement))
		{
			throw new InvalidOperationException();
		}
		xmlElement.IsEmpty = false;
		_write = xmlElement.ParentNode;
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		WriteFullEndElement();
	}

	internal override void StartElementContent()
	{
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		VerifyState(Method.WriteStartAttribute);
		XmlAttribute xmlAttribute = _document.CreateAttribute(prefix, localName, ns);
		AddAttribute(xmlAttribute, _write);
		_write = xmlAttribute;
	}

	public override void WriteEndAttribute()
	{
		VerifyState(Method.WriteEndAttribute);
		if (!(_write is XmlAttribute xmlAttribute))
		{
			throw new InvalidOperationException();
		}
		if (!xmlAttribute.HasChildNodes)
		{
			XmlNode node = _document.CreateTextNode(string.Empty);
			AddChild(node, xmlAttribute);
		}
		_write = xmlAttribute.OwnerElement;
	}

	internal override void WriteNamespaceDeclaration(string prefix, string ns)
	{
		WriteStartNamespaceDeclaration(prefix);
		WriteString(ns);
		WriteEndNamespaceDeclaration();
	}

	internal override void WriteStartNamespaceDeclaration(string prefix)
	{
		VerifyState(Method.WriteStartNamespaceDeclaration);
		XmlAttribute xmlAttribute = ((prefix.Length != 0) ? _document.CreateAttribute(_document.strXmlns, prefix, _document.strReservedXmlns) : _document.CreateAttribute(prefix, _document.strXmlns, _document.strReservedXmlns));
		AddAttribute(xmlAttribute, _write);
		_write = xmlAttribute;
	}

	internal override void WriteEndNamespaceDeclaration()
	{
		VerifyState(Method.WriteEndNamespaceDeclaration);
		if (!(_write is XmlAttribute xmlAttribute))
		{
			throw new InvalidOperationException();
		}
		if (!xmlAttribute.HasChildNodes)
		{
			XmlNode node = _document.CreateTextNode(string.Empty);
			AddChild(node, xmlAttribute);
		}
		_write = xmlAttribute.OwnerElement;
	}

	public override void WriteCData(string text)
	{
		VerifyState(Method.WriteCData);
		XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
		XmlNode node = _document.CreateCDataSection(text);
		AddChild(node, _write);
	}

	public override void WriteComment(string text)
	{
		VerifyState(Method.WriteComment);
		XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
		XmlNode node = _document.CreateComment(text);
		AddChild(node, _write);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		VerifyState(Method.WriteProcessingInstruction);
		XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
		XmlNode node = _document.CreateProcessingInstruction(name, text);
		AddChild(node, _write);
	}

	public override void WriteEntityRef(string name)
	{
		VerifyState(Method.WriteEntityRef);
		XmlNode node = _document.CreateEntityReference(name);
		AddChild(node, _write);
	}

	public override void WriteCharEntity(char ch)
	{
		WriteString(char.ToString(ch));
	}

	public override void WriteWhitespace(string text)
	{
		VerifyState(Method.WriteWhitespace);
		XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
		if (_document.PreserveWhitespace)
		{
			XmlNode node = _document.CreateWhitespace(text);
			AddChild(node, _write);
		}
	}

	public override void WriteString(string text)
	{
		VerifyState(Method.WriteString);
		XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
		XmlNode node = _document.CreateTextNode(text);
		AddChild(node, _write);
	}

	public override void WriteSurrogateCharEntity(char lowCh, char highCh)
	{
		Span<char> span = stackalloc char[2] { highCh, lowCh };
		ReadOnlySpan<char> value = span;
		WriteString(new string(value));
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count));
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count));
	}

	public override void WriteRaw(string data)
	{
		WriteString(data);
	}

	public override void Close()
	{
	}

	internal override void Close(WriteState currentState)
	{
		if (currentState == WriteState.Error)
		{
			return;
		}
		try
		{
			switch (_type)
			{
			case DocumentXmlWriterType.InsertSiblingAfter:
			{
				XmlNode parentNode = _start.ParentNode;
				if (parentNode == null)
				{
					throw new InvalidOperationException(System.SR.Xpn_MissingParent);
				}
				for (int num2 = _fragment.Count - 1; num2 >= 0; num2--)
				{
					parentNode.InsertAfter(_fragment[num2], _start);
				}
				break;
			}
			case DocumentXmlWriterType.InsertSiblingBefore:
			{
				XmlNode parentNode = _start.ParentNode;
				if (parentNode == null)
				{
					throw new InvalidOperationException(System.SR.Xpn_MissingParent);
				}
				for (int j = 0; j < _fragment.Count; j++)
				{
					parentNode.InsertBefore(_fragment[j], _start);
				}
				break;
			}
			case DocumentXmlWriterType.PrependChild:
			{
				for (int num = _fragment.Count - 1; num >= 0; num--)
				{
					_start.PrependChild(_fragment[num]);
				}
				break;
			}
			case DocumentXmlWriterType.AppendChild:
			{
				for (int i = 0; i < _fragment.Count; i++)
				{
					_start.AppendChild(_fragment[i]);
				}
				break;
			}
			case DocumentXmlWriterType.AppendAttribute:
				CloseWithAppendAttribute();
				break;
			case DocumentXmlWriterType.ReplaceToFollowingSibling:
				if (_fragment.Count == 0)
				{
					throw new InvalidOperationException(System.SR.Xpn_NoContent);
				}
				CloseWithReplaceToFollowingSibling();
				break;
			}
		}
		finally
		{
			_fragment.Clear();
		}
	}

	private void CloseWithAppendAttribute()
	{
		XmlElement xmlElement = _start as XmlElement;
		XmlAttributeCollection attributes = xmlElement.Attributes;
		for (int i = 0; i < _fragment.Count; i++)
		{
			XmlAttribute xmlAttribute = _fragment[i] as XmlAttribute;
			int num = attributes.FindNodeOffsetNS(xmlAttribute);
			if (num != -1 && ((XmlAttribute)attributes.nodes[num]).Specified)
			{
				throw new XmlException(System.SR.Xml_DupAttributeName, (xmlAttribute.Prefix.Length == 0) ? xmlAttribute.LocalName : (xmlAttribute.Prefix + ":" + xmlAttribute.LocalName));
			}
		}
		for (int j = 0; j < _fragment.Count; j++)
		{
			XmlAttribute node = _fragment[j] as XmlAttribute;
			attributes.Append(node);
		}
	}

	private void CloseWithReplaceToFollowingSibling()
	{
		XmlNode parentNode = _start.ParentNode;
		if (parentNode == null)
		{
			throw new InvalidOperationException(System.SR.Xpn_MissingParent);
		}
		if (_start != _end)
		{
			if (!DocumentXPathNavigator.IsFollowingSibling(_start, _end))
			{
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			}
			if (_start.IsReadOnly)
			{
				throw new InvalidOperationException(System.SR.Xdom_Node_Modify_ReadOnly);
			}
			DocumentXPathNavigator.DeleteToFollowingSibling(_start.NextSibling, _end);
		}
		XmlNode xmlNode = _fragment[0];
		parentNode.ReplaceChild(xmlNode, _start);
		for (int num = _fragment.Count - 1; num >= 1; num--)
		{
			parentNode.InsertAfter(_fragment[num], xmlNode);
		}
		_navigator.ResetPosition(xmlNode);
	}

	public override void Flush()
	{
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _namespaceManager.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		return _namespaceManager.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return _namespaceManager.LookupPrefix(namespaceName);
	}

	private void AddAttribute(XmlAttribute attr, XmlNode parent)
	{
		if (parent == null)
		{
			_fragment.Add(attr);
			return;
		}
		if (!(parent is XmlElement xmlElement))
		{
			throw new InvalidOperationException();
		}
		xmlElement.Attributes.Append(attr);
	}

	private void AddChild(XmlNode node, XmlNode parent)
	{
		if (parent == null)
		{
			_fragment.Add(node);
		}
		else
		{
			parent.AppendChild(node);
		}
	}

	private State StartState()
	{
		XmlNodeType xmlNodeType = XmlNodeType.None;
		switch (_type)
		{
		case DocumentXmlWriterType.InsertSiblingAfter:
		case DocumentXmlWriterType.InsertSiblingBefore:
		{
			XmlNode parentNode = _start.ParentNode;
			if (parentNode != null)
			{
				xmlNodeType = parentNode.NodeType;
			}
			switch (xmlNodeType)
			{
			case XmlNodeType.Document:
				return State.Prolog;
			case XmlNodeType.DocumentFragment:
				return State.Fragment;
			}
			break;
		}
		case DocumentXmlWriterType.PrependChild:
		case DocumentXmlWriterType.AppendChild:
			switch (_start.NodeType)
			{
			case XmlNodeType.Document:
				return State.Prolog;
			case XmlNodeType.DocumentFragment:
				return State.Fragment;
			}
			break;
		case DocumentXmlWriterType.AppendAttribute:
			return State.Attribute;
		}
		return State.Content;
	}

	private void VerifyState(Method method)
	{
		_state = s_changeState[(int)((int)method * 5 + _state)];
		if (_state == State.Error)
		{
			throw new InvalidOperationException(System.SR.Xml_ClosedOrError);
		}
	}
}
