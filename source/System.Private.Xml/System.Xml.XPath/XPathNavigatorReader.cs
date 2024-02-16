using System.Collections.Generic;
using System.Xml.Schema;

namespace System.Xml.XPath;

internal class XPathNavigatorReader : XmlReader, IXmlNamespaceResolver
{
	private enum State
	{
		Initial,
		Content,
		EndElement,
		Attribute,
		AttrVal,
		InReadBinary,
		EOF,
		Closed,
		Error
	}

	private XPathNavigator _nav;

	private readonly XPathNavigator _navToRead;

	private int _depth;

	private State _state;

	private XmlNodeType _nodeType;

	private int _attrCount;

	private bool _readEntireDocument;

	protected IXmlLineInfo lineInfo;

	protected IXmlSchemaInfo schemaInfo;

	private ReadContentAsBinaryHelper _readBinaryHelper;

	private State _savedState;

	internal static XmlNodeType[] convertFromXPathNodeType = new XmlNodeType[10]
	{
		XmlNodeType.Document,
		XmlNodeType.Element,
		XmlNodeType.Attribute,
		XmlNodeType.Attribute,
		XmlNodeType.Text,
		XmlNodeType.SignificantWhitespace,
		XmlNodeType.Whitespace,
		XmlNodeType.ProcessingInstruction,
		XmlNodeType.Comment,
		XmlNodeType.None
	};

	internal object UnderlyingObject => _nav.UnderlyingObject;

	protected bool IsReading
	{
		get
		{
			if (_state > State.Initial)
			{
				return _state < State.EOF;
			}
			return false;
		}
	}

	internal override XmlNamespaceManager NamespaceManager => XPathNavigator.GetNamespaces(this);

	public override XmlNameTable NameTable => _navToRead.NameTable;

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.NameTable = NameTable;
			xmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
			xmlReaderSettings.CheckCharacters = false;
			xmlReaderSettings.ReadOnly = true;
			return xmlReaderSettings;
		}
	}

	public override IXmlSchemaInfo SchemaInfo
	{
		get
		{
			if (_nodeType == XmlNodeType.Text)
			{
				return null;
			}
			return _nav.SchemaInfo;
		}
	}

	public override Type ValueType => _nav.ValueType;

	public override XmlNodeType NodeType => _nodeType;

	public override string NamespaceURI
	{
		get
		{
			if (_nav.NodeType == XPathNodeType.Namespace)
			{
				return NameTable.Add("http://www.w3.org/2000/xmlns/");
			}
			if (NodeType == XmlNodeType.Text)
			{
				return string.Empty;
			}
			return _nav.NamespaceURI;
		}
	}

	public override string LocalName
	{
		get
		{
			if (_nav.NodeType == XPathNodeType.Namespace && _nav.LocalName.Length == 0)
			{
				return NameTable.Add("xmlns");
			}
			if (NodeType == XmlNodeType.Text)
			{
				return string.Empty;
			}
			return _nav.LocalName;
		}
	}

	public override string Prefix
	{
		get
		{
			if (_nav.NodeType == XPathNodeType.Namespace && _nav.LocalName.Length != 0)
			{
				return NameTable.Add("xmlns");
			}
			if (NodeType == XmlNodeType.Text)
			{
				return string.Empty;
			}
			return _nav.Prefix;
		}
	}

	public override string BaseURI
	{
		get
		{
			if (_state == State.Initial)
			{
				return _navToRead.BaseURI;
			}
			return _nav.BaseURI;
		}
	}

	public override bool IsEmptyElement => _nav.IsEmptyElement;

	public override XmlSpace XmlSpace
	{
		get
		{
			XPathNavigator xPathNavigator = _nav.Clone();
			do
			{
				if (xPathNavigator.MoveToAttribute("space", "http://www.w3.org/XML/1998/namespace"))
				{
					string text = XmlConvert.TrimString(xPathNavigator.Value);
					if (text == "default")
					{
						return XmlSpace.Default;
					}
					if (text == "preserve")
					{
						return XmlSpace.Preserve;
					}
					xPathNavigator.MoveToParent();
				}
			}
			while (xPathNavigator.MoveToParent());
			return XmlSpace.None;
		}
	}

	public override string XmlLang => _nav.XmlLang;

	public override bool HasValue
	{
		get
		{
			if (_nodeType != XmlNodeType.Element && _nodeType != XmlNodeType.Document && _nodeType != XmlNodeType.EndElement && _nodeType != 0)
			{
				return true;
			}
			return false;
		}
	}

	public override string Value
	{
		get
		{
			if (_nodeType != XmlNodeType.Element && _nodeType != XmlNodeType.Document && _nodeType != XmlNodeType.EndElement && _nodeType != 0)
			{
				return _nav.Value;
			}
			return string.Empty;
		}
	}

	public override int AttributeCount
	{
		get
		{
			if (_attrCount < 0)
			{
				XPathNavigator elemNav = GetElemNav();
				int num = 0;
				if (elemNav != null)
				{
					if (elemNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
					{
						do
						{
							num++;
						}
						while (elemNav.MoveToNextNamespace(XPathNamespaceScope.Local));
						elemNav.MoveToParent();
					}
					if (elemNav.MoveToFirstAttribute())
					{
						do
						{
							num++;
						}
						while (elemNav.MoveToNextAttribute());
					}
				}
				_attrCount = num;
			}
			return _attrCount;
		}
	}

	public override bool EOF => _state == State.EOF;

	public override ReadState ReadState
	{
		get
		{
			switch (_state)
			{
			case State.Initial:
				return ReadState.Initial;
			case State.Content:
			case State.EndElement:
			case State.Attribute:
			case State.AttrVal:
			case State.InReadBinary:
				return ReadState.Interactive;
			case State.EOF:
				return ReadState.EndOfFile;
			case State.Closed:
				return ReadState.Closed;
			default:
				return ReadState.Error;
			}
		}
	}

	public override bool CanReadBinaryContent => true;

	public override int Depth => _depth;

	internal static XmlNodeType ToXmlNodeType(XPathNodeType typ)
	{
		return convertFromXPathNodeType[(int)typ];
	}

	public static XPathNavigatorReader Create(XPathNavigator navToRead)
	{
		XPathNavigator xPathNavigator = navToRead.Clone();
		IXmlLineInfo xli = xPathNavigator as IXmlLineInfo;
		IXmlSchemaInfo xmlSchemaInfo = xPathNavigator as IXmlSchemaInfo;
		if (xmlSchemaInfo == null)
		{
			return new XPathNavigatorReader(xPathNavigator, xli, xmlSchemaInfo);
		}
		return new XPathNavigatorReaderWithSI(xPathNavigator, xli, xmlSchemaInfo);
	}

	protected XPathNavigatorReader(XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi)
	{
		_navToRead = navToRead;
		lineInfo = xli;
		schemaInfo = xsi;
		_nav = XmlEmptyNavigator.Singleton;
		_state = State.Initial;
		_depth = 0;
		_nodeType = ToXmlNodeType(_nav.NodeType);
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _nav.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		return _nav.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return _nav.LookupPrefix(namespaceName);
	}

	private XPathNavigator GetElemNav()
	{
		switch (_state)
		{
		case State.Content:
			return _nav.Clone();
		case State.Attribute:
		case State.AttrVal:
		{
			XPathNavigator xPathNavigator = _nav.Clone();
			if (xPathNavigator.MoveToParent())
			{
				return xPathNavigator;
			}
			break;
		}
		case State.InReadBinary:
		{
			_state = _savedState;
			XPathNavigator elemNav = GetElemNav();
			_state = State.InReadBinary;
			return elemNav;
		}
		}
		return null;
	}

	private XPathNavigator GetElemNav(out int depth)
	{
		XPathNavigator xPathNavigator = null;
		switch (_state)
		{
		case State.Content:
			if (_nodeType == XmlNodeType.Element)
			{
				xPathNavigator = _nav.Clone();
			}
			depth = _depth;
			break;
		case State.Attribute:
			xPathNavigator = _nav.Clone();
			xPathNavigator.MoveToParent();
			depth = _depth - 1;
			break;
		case State.AttrVal:
			xPathNavigator = _nav.Clone();
			xPathNavigator.MoveToParent();
			depth = _depth - 2;
			break;
		case State.InReadBinary:
			_state = _savedState;
			xPathNavigator = GetElemNav(out depth);
			_state = State.InReadBinary;
			break;
		default:
			depth = _depth;
			break;
		}
		return xPathNavigator;
	}

	private void MoveToAttr(XPathNavigator nav, int depth)
	{
		_nav.MoveTo(nav);
		_depth = depth;
		_nodeType = XmlNodeType.Attribute;
		_state = State.Attribute;
	}

	public override string GetAttribute(string name)
	{
		XPathNavigator xPathNavigator = _nav;
		switch (xPathNavigator.NodeType)
		{
		case XPathNodeType.Attribute:
			xPathNavigator = xPathNavigator.Clone();
			if (!xPathNavigator.MoveToParent())
			{
				return null;
			}
			break;
		default:
			return null;
		case XPathNodeType.Element:
			break;
		}
		ValidateNames.SplitQName(name, out var prefix, out var lname);
		if (prefix.Length == 0)
		{
			if (lname == "xmlns")
			{
				return xPathNavigator.GetNamespace(string.Empty);
			}
			if (xPathNavigator == _nav)
			{
				xPathNavigator = xPathNavigator.Clone();
			}
			if (xPathNavigator.MoveToAttribute(lname, string.Empty))
			{
				return xPathNavigator.Value;
			}
		}
		else
		{
			if (prefix == "xmlns")
			{
				return xPathNavigator.GetNamespace(lname);
			}
			if (xPathNavigator == _nav)
			{
				xPathNavigator = xPathNavigator.Clone();
			}
			if (xPathNavigator.MoveToFirstAttribute())
			{
				do
				{
					if (xPathNavigator.LocalName == lname && xPathNavigator.Prefix == prefix)
					{
						return xPathNavigator.Value;
					}
				}
				while (xPathNavigator.MoveToNextAttribute());
			}
		}
		return null;
	}

	public override string GetAttribute(string localName, string namespaceURI)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		XPathNavigator xPathNavigator = _nav;
		switch (xPathNavigator.NodeType)
		{
		case XPathNodeType.Attribute:
			xPathNavigator = xPathNavigator.Clone();
			if (!xPathNavigator.MoveToParent())
			{
				return null;
			}
			break;
		default:
			return null;
		case XPathNodeType.Element:
			break;
		}
		if (namespaceURI == "http://www.w3.org/2000/xmlns/")
		{
			if (localName == "xmlns")
			{
				localName = string.Empty;
			}
			return xPathNavigator.GetNamespace(localName);
		}
		if (namespaceURI == null)
		{
			namespaceURI = string.Empty;
		}
		if (xPathNavigator == _nav)
		{
			xPathNavigator = xPathNavigator.Clone();
		}
		if (xPathNavigator.MoveToAttribute(localName, namespaceURI))
		{
			return xPathNavigator.Value;
		}
		return null;
	}

	private static string GetNamespaceByIndex(XPathNavigator nav, int index, out int count)
	{
		string value = nav.Value;
		string result = null;
		if (nav.MoveToNextNamespace(XPathNamespaceScope.Local))
		{
			result = GetNamespaceByIndex(nav, index, out count);
		}
		else
		{
			count = 0;
		}
		if (count == index)
		{
			result = value;
		}
		count++;
		return result;
	}

	public override string GetAttribute(int index)
	{
		if (index >= 0)
		{
			XPathNavigator elemNav = GetElemNav();
			if (elemNav != null)
			{
				if (elemNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
				{
					int count;
					string namespaceByIndex = GetNamespaceByIndex(elemNav, index, out count);
					if (namespaceByIndex != null)
					{
						return namespaceByIndex;
					}
					index -= count;
					elemNav.MoveToParent();
				}
				if (elemNav.MoveToFirstAttribute())
				{
					do
					{
						if (index == 0)
						{
							return elemNav.Value;
						}
						index--;
					}
					while (elemNav.MoveToNextAttribute());
				}
			}
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override bool MoveToAttribute(string localName, string namespaceName)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		int depth = _depth;
		XPathNavigator elemNav = GetElemNav(out depth);
		if (elemNav != null)
		{
			if (namespaceName == "http://www.w3.org/2000/xmlns/")
			{
				if (localName == "xmlns")
				{
					localName = string.Empty;
				}
				if (!elemNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
				{
					goto IL_0078;
				}
				while (!(elemNav.LocalName == localName))
				{
					if (elemNav.MoveToNextNamespace(XPathNamespaceScope.Local))
					{
						continue;
					}
					goto IL_0078;
				}
			}
			else
			{
				if (namespaceName == null)
				{
					namespaceName = string.Empty;
				}
				if (!elemNav.MoveToAttribute(localName, namespaceName))
				{
					goto IL_0078;
				}
			}
			if (_state == State.InReadBinary)
			{
				_readBinaryHelper.Finish();
				_state = _savedState;
			}
			MoveToAttr(elemNav, depth + 1);
			return true;
		}
		goto IL_0078;
		IL_0078:
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		int depth;
		XPathNavigator elemNav = GetElemNav(out depth);
		if (elemNav != null)
		{
			if (elemNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
			{
				while (elemNav.MoveToNextNamespace(XPathNamespaceScope.Local))
				{
				}
			}
			else if (!elemNav.MoveToFirstAttribute())
			{
				goto IL_0028;
			}
			if (_state == State.InReadBinary)
			{
				_readBinaryHelper.Finish();
				_state = _savedState;
			}
			MoveToAttr(elemNav, depth + 1);
			return true;
		}
		goto IL_0028;
		IL_0028:
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		switch (_state)
		{
		case State.Content:
			return MoveToFirstAttribute();
		case State.Attribute:
		{
			if (XPathNodeType.Attribute == _nav.NodeType)
			{
				return _nav.MoveToNextAttribute();
			}
			XPathNavigator xPathNavigator = _nav.Clone();
			if (!xPathNavigator.MoveToParent())
			{
				return false;
			}
			if (!xPathNavigator.MoveToFirstNamespace(XPathNamespaceScope.Local))
			{
				return false;
			}
			if (xPathNavigator.IsSamePosition(_nav))
			{
				xPathNavigator.MoveToParent();
				if (!xPathNavigator.MoveToFirstAttribute())
				{
					return false;
				}
				_nav.MoveTo(xPathNavigator);
				return true;
			}
			XPathNavigator xPathNavigator2 = xPathNavigator.Clone();
			while (true)
			{
				if (!xPathNavigator.MoveToNextNamespace(XPathNamespaceScope.Local))
				{
					return false;
				}
				if (xPathNavigator.IsSamePosition(_nav))
				{
					break;
				}
				xPathNavigator2.MoveTo(xPathNavigator);
			}
			_nav.MoveTo(xPathNavigator2);
			return true;
		}
		case State.AttrVal:
			_depth--;
			_state = State.Attribute;
			if (!MoveToNextAttribute())
			{
				_depth++;
				_state = State.AttrVal;
				return false;
			}
			_nodeType = XmlNodeType.Attribute;
			return true;
		case State.InReadBinary:
			_state = _savedState;
			if (!MoveToNextAttribute())
			{
				_state = State.InReadBinary;
				return false;
			}
			_readBinaryHelper.Finish();
			return true;
		default:
			return false;
		}
	}

	public override bool MoveToAttribute(string name)
	{
		int depth;
		XPathNavigator elemNav = GetElemNav(out depth);
		if (elemNav == null)
		{
			return false;
		}
		ValidateNames.SplitQName(name, out var prefix, out var lname);
		bool flag = false;
		if ((flag = prefix.Length == 0 && lname == "xmlns") || prefix == "xmlns")
		{
			if (flag)
			{
				lname = string.Empty;
			}
			if (!elemNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
			{
				goto IL_00b3;
			}
			while (!(elemNav.LocalName == lname))
			{
				if (elemNav.MoveToNextNamespace(XPathNamespaceScope.Local))
				{
					continue;
				}
				goto IL_00b3;
			}
		}
		else if (prefix.Length == 0)
		{
			if (!elemNav.MoveToAttribute(lname, string.Empty))
			{
				goto IL_00b3;
			}
		}
		else
		{
			if (!elemNav.MoveToFirstAttribute())
			{
				goto IL_00b3;
			}
			while (!(elemNav.LocalName == lname) || !(elemNav.Prefix == prefix))
			{
				if (elemNav.MoveToNextAttribute())
				{
					continue;
				}
				goto IL_00b3;
			}
		}
		if (_state == State.InReadBinary)
		{
			_readBinaryHelper.Finish();
			_state = _savedState;
		}
		MoveToAttr(elemNav, depth + 1);
		return true;
		IL_00b3:
		return false;
	}

	public override bool MoveToElement()
	{
		switch (_state)
		{
		case State.Attribute:
		case State.AttrVal:
			if (!_nav.MoveToParent())
			{
				return false;
			}
			_depth--;
			if (_state == State.AttrVal)
			{
				_depth--;
			}
			_state = State.Content;
			_nodeType = XmlNodeType.Element;
			return true;
		case State.InReadBinary:
			_state = _savedState;
			if (!MoveToElement())
			{
				_state = State.InReadBinary;
				return false;
			}
			_readBinaryHelper.Finish();
			break;
		}
		return false;
	}

	public override void ResolveEntity()
	{
		throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
	}

	public override bool ReadAttributeValue()
	{
		if (_state == State.InReadBinary)
		{
			_readBinaryHelper.Finish();
			_state = _savedState;
		}
		if (_state == State.Attribute)
		{
			_state = State.AttrVal;
			_nodeType = XmlNodeType.Text;
			_depth++;
			return true;
		}
		return false;
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _state;
		}
		_state = _savedState;
		int result = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);
		_savedState = _state;
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _state;
		}
		_state = _savedState;
		int result = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);
		_savedState = _state;
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _state;
		}
		_state = _savedState;
		int result = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);
		_savedState = _state;
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _state;
		}
		_state = _savedState;
		int result = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);
		_savedState = _state;
		_state = State.InReadBinary;
		return result;
	}

	public override string LookupNamespace(string prefix)
	{
		return _nav.LookupNamespace(prefix);
	}

	public override bool Read()
	{
		_attrCount = -1;
		switch (_state)
		{
		case State.EOF:
		case State.Closed:
		case State.Error:
			return false;
		case State.Initial:
			_nav = _navToRead;
			_state = State.Content;
			if (_nav.NodeType == XPathNodeType.Root)
			{
				if (!_nav.MoveToFirstChild())
				{
					SetEOF();
					return false;
				}
				_readEntireDocument = true;
			}
			else if (XPathNodeType.Attribute == _nav.NodeType)
			{
				_state = State.Attribute;
			}
			_nodeType = ToXmlNodeType(_nav.NodeType);
			break;
		case State.Content:
			if (_nav.MoveToFirstChild())
			{
				_nodeType = ToXmlNodeType(_nav.NodeType);
				_depth++;
				_state = State.Content;
				break;
			}
			if (_nodeType == XmlNodeType.Element && !_nav.IsEmptyElement)
			{
				_nodeType = XmlNodeType.EndElement;
				_state = State.EndElement;
				break;
			}
			goto case State.EndElement;
		case State.EndElement:
			if (_depth == 0 && !_readEntireDocument)
			{
				SetEOF();
				return false;
			}
			if (_nav.MoveToNext())
			{
				_nodeType = ToXmlNodeType(_nav.NodeType);
				_state = State.Content;
				break;
			}
			if (_depth > 0 && _nav.MoveToParent())
			{
				_nodeType = XmlNodeType.EndElement;
				_state = State.EndElement;
				_depth--;
				break;
			}
			SetEOF();
			return false;
		case State.Attribute:
		case State.AttrVal:
			if (!_nav.MoveToParent())
			{
				SetEOF();
				return false;
			}
			_nodeType = ToXmlNodeType(_nav.NodeType);
			_depth--;
			if (_state == State.AttrVal)
			{
				_depth--;
			}
			goto case State.Content;
		case State.InReadBinary:
			_state = _savedState;
			_readBinaryHelper.Finish();
			return Read();
		}
		return true;
	}

	public override void Close()
	{
		_nav = XmlEmptyNavigator.Singleton;
		_nodeType = XmlNodeType.None;
		_state = State.Closed;
		_depth = 0;
	}

	private void SetEOF()
	{
		_nav = XmlEmptyNavigator.Singleton;
		_nodeType = XmlNodeType.None;
		_state = State.EOF;
		_depth = 0;
	}
}
