using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal abstract class XmlBaseReader : XmlDictionaryReader
{
	protected enum QNameType
	{
		Normal,
		Xmlns
	}

	protected class XmlNode
	{
		protected enum XmlNodeFlags
		{
			None = 0,
			CanGetAttribute = 1,
			CanMoveToElement = 2,
			HasValue = 4,
			AtomicValue = 8,
			SkipValue = 0x10,
			HasContent = 0x20
		}

		private XmlNodeType _nodeType;

		private readonly PrefixHandle _prefix;

		private readonly StringHandle _localName;

		private readonly ValueHandle _value;

		private Namespace _ns;

		private readonly bool _hasValue;

		private readonly bool _canGetAttribute;

		private readonly bool _canMoveToElement;

		private readonly ReadState _readState;

		private readonly XmlAttributeTextNode _attributeTextNode;

		private bool _exitScope;

		private readonly int _depthDelta;

		private bool _isAtomicValue;

		private readonly bool _skipValue;

		private QNameType _qnameType;

		private readonly bool _hasContent;

		private bool _isEmptyElement;

		private char _quoteChar;

		public bool HasValue => _hasValue;

		public ReadState ReadState => _readState;

		public StringHandle LocalName => _localName;

		public PrefixHandle Prefix => _prefix;

		public bool CanGetAttribute => _canGetAttribute;

		public bool CanMoveToElement => _canMoveToElement;

		public XmlAttributeTextNode AttributeText => _attributeTextNode;

		public bool SkipValue => _skipValue;

		public ValueHandle Value => _value;

		public int DepthDelta => _depthDelta;

		public bool HasContent => _hasContent;

		public XmlNodeType NodeType
		{
			get
			{
				return _nodeType;
			}
			set
			{
				_nodeType = value;
			}
		}

		public QNameType QNameType
		{
			get
			{
				return _qnameType;
			}
			set
			{
				_qnameType = value;
			}
		}

		public Namespace Namespace
		{
			get
			{
				return _ns;
			}
			set
			{
				_ns = value;
			}
		}

		public bool IsAtomicValue
		{
			get
			{
				return _isAtomicValue;
			}
			set
			{
				_isAtomicValue = value;
			}
		}

		public bool ExitScope
		{
			get
			{
				return _exitScope;
			}
			set
			{
				_exitScope = value;
			}
		}

		public bool IsEmptyElement
		{
			get
			{
				return _isEmptyElement;
			}
			set
			{
				_isEmptyElement = value;
			}
		}

		public char QuoteChar
		{
			get
			{
				return _quoteChar;
			}
			set
			{
				_quoteChar = value;
			}
		}

		public string ValueAsString
		{
			get
			{
				if (_qnameType == QNameType.Normal)
				{
					return Value.GetString();
				}
				return Namespace.Uri.GetString();
			}
		}

		protected XmlNode(XmlNodeType nodeType, PrefixHandle prefix, StringHandle localName, ValueHandle value, XmlNodeFlags nodeFlags, ReadState readState, XmlAttributeTextNode attributeTextNode, int depthDelta)
		{
			_nodeType = nodeType;
			_prefix = prefix;
			_localName = localName;
			_value = value;
			_ns = NamespaceManager.EmptyNamespace;
			_hasValue = (nodeFlags & XmlNodeFlags.HasValue) != 0;
			_canGetAttribute = (nodeFlags & XmlNodeFlags.CanGetAttribute) != 0;
			_canMoveToElement = (nodeFlags & XmlNodeFlags.CanMoveToElement) != 0;
			_isAtomicValue = (nodeFlags & XmlNodeFlags.AtomicValue) != 0;
			_skipValue = (nodeFlags & XmlNodeFlags.SkipValue) != 0;
			_hasContent = (nodeFlags & XmlNodeFlags.HasContent) != 0;
			_readState = readState;
			_attributeTextNode = attributeTextNode;
			_exitScope = nodeType == XmlNodeType.EndElement;
			_depthDelta = depthDelta;
			_isEmptyElement = false;
			_quoteChar = '"';
			_qnameType = QNameType.Normal;
		}

		public bool IsLocalName(string localName)
		{
			if (_qnameType == QNameType.Normal)
			{
				return LocalName == localName;
			}
			return Namespace.Prefix == localName;
		}

		public bool IsLocalName(XmlDictionaryString localName)
		{
			if (_qnameType == QNameType.Normal)
			{
				return LocalName == localName;
			}
			return Namespace.Prefix == localName;
		}

		public bool IsNamespaceUri(string ns)
		{
			if (_qnameType == QNameType.Normal)
			{
				return Namespace.IsUri(ns);
			}
			return ns == "http://www.w3.org/2000/xmlns/";
		}

		public bool IsNamespaceUri(XmlDictionaryString ns)
		{
			if (_qnameType == QNameType.Normal)
			{
				return Namespace.IsUri(ns);
			}
			return ns.Value == "http://www.w3.org/2000/xmlns/";
		}

		public bool IsLocalNameAndNamespaceUri(string localName, string ns)
		{
			if (_qnameType == QNameType.Normal)
			{
				if (LocalName == localName)
				{
					return Namespace.IsUri(ns);
				}
				return false;
			}
			if (Namespace.Prefix == localName)
			{
				return ns == "http://www.w3.org/2000/xmlns/";
			}
			return false;
		}

		public bool IsLocalNameAndNamespaceUri(XmlDictionaryString localName, XmlDictionaryString ns)
		{
			if (_qnameType == QNameType.Normal)
			{
				if (LocalName == localName)
				{
					return Namespace.IsUri(ns);
				}
				return false;
			}
			if (Namespace.Prefix == localName)
			{
				return ns.Value == "http://www.w3.org/2000/xmlns/";
			}
			return false;
		}

		public bool IsPrefixAndLocalName(string prefix, string localName)
		{
			if (_qnameType == QNameType.Normal)
			{
				if (Prefix == prefix)
				{
					return LocalName == localName;
				}
				return false;
			}
			if (prefix == "xmlns")
			{
				return Namespace.Prefix == localName;
			}
			return false;
		}

		public bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString localName)
		{
			if (_qnameType == QNameType.Normal)
			{
				return LocalName.TryGetDictionaryString(out localName);
			}
			localName = null;
			return false;
		}

		public bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString ns)
		{
			if (_qnameType == QNameType.Normal)
			{
				return Namespace.Uri.TryGetDictionaryString(out ns);
			}
			ns = null;
			return false;
		}

		public bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString value)
		{
			if (_qnameType == QNameType.Normal)
			{
				return Value.TryGetDictionaryString(out value);
			}
			value = null;
			return false;
		}
	}

	protected class XmlElementNode : XmlNode
	{
		private readonly XmlEndElementNode _endElementNode;

		private int _bufferOffset;

		public int NameOffset;

		public int NameLength;

		public XmlEndElementNode EndElement => _endElementNode;

		public int BufferOffset
		{
			get
			{
				return _bufferOffset;
			}
			set
			{
				_bufferOffset = value;
			}
		}

		public XmlElementNode(XmlBufferReader bufferReader)
			: this(new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader))
		{
		}

		private XmlElementNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
			: base(XmlNodeType.Element, prefix, localName, value, (XmlNodeFlags)33, ReadState.Interactive, null, -1)
		{
			_endElementNode = new XmlEndElementNode(prefix, localName, value);
		}
	}

	protected class XmlAttributeNode : XmlNode
	{
		public XmlAttributeNode(XmlBufferReader bufferReader)
			: this(new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader))
		{
		}

		private XmlAttributeNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
			: base(XmlNodeType.Attribute, prefix, localName, value, (XmlNodeFlags)15, ReadState.Interactive, new XmlAttributeTextNode(prefix, localName, value), 0)
		{
		}
	}

	protected class XmlEndElementNode : XmlNode
	{
		public XmlEndElementNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
			: base(XmlNodeType.EndElement, prefix, localName, value, XmlNodeFlags.HasContent, ReadState.Interactive, null, -1)
		{
		}
	}

	protected class XmlTextNode : XmlNode
	{
		protected XmlTextNode(XmlNodeType nodeType, PrefixHandle prefix, StringHandle localName, ValueHandle value, XmlNodeFlags nodeFlags, ReadState readState, XmlAttributeTextNode attributeTextNode, int depthDelta)
			: base(nodeType, prefix, localName, value, nodeFlags, readState, attributeTextNode, depthDelta)
		{
		}
	}

	protected class XmlAtomicTextNode : XmlTextNode
	{
		public XmlAtomicTextNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.Text, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)60, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlComplexTextNode : XmlTextNode
	{
		public XmlComplexTextNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.Text, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)36, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlWhitespaceTextNode : XmlTextNode
	{
		public XmlWhitespaceTextNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.Whitespace, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.HasValue, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlCDataNode : XmlTextNode
	{
		public XmlCDataNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.CDATA, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)36, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlAttributeTextNode : XmlTextNode
	{
		public XmlAttributeTextNode(PrefixHandle prefix, StringHandle localName, ValueHandle value)
			: base(XmlNodeType.Text, prefix, localName, value, (XmlNodeFlags)47, ReadState.Interactive, null, 1)
		{
		}
	}

	protected class XmlInitialNode : XmlNode
	{
		public XmlInitialNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.None, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.Initial, null, 0)
		{
		}
	}

	protected class XmlDeclarationNode : XmlNode
	{
		public XmlDeclarationNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.XmlDeclaration, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.CanGetAttribute, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlCommentNode : XmlNode
	{
		public XmlCommentNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.Comment, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.HasValue, ReadState.Interactive, null, 0)
		{
		}
	}

	protected class XmlEndOfFileNode : XmlNode
	{
		public XmlEndOfFileNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.None, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.EndOfFile, null, 0)
		{
		}
	}

	protected class XmlClosedNode : XmlNode
	{
		public XmlClosedNode(XmlBufferReader bufferReader)
			: base(XmlNodeType.None, new PrefixHandle(bufferReader), new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.Closed, null, 0)
		{
		}
	}

	private sealed class AttributeSorter : IComparer
	{
		private object[] _indeces;

		private XmlAttributeNode[] _attributeNodes;

		private int _attributeCount;

		private int _attributeIndex1;

		private int _attributeIndex2;

		public bool Sort(XmlAttributeNode[] attributeNodes, int attributeCount)
		{
			_attributeIndex1 = -1;
			_attributeIndex2 = -1;
			_attributeNodes = attributeNodes;
			_attributeCount = attributeCount;
			bool result = Sort();
			_attributeNodes = null;
			_attributeCount = 0;
			return result;
		}

		public void GetIndeces(out int attributeIndex1, out int attributeIndex2)
		{
			attributeIndex1 = _attributeIndex1;
			attributeIndex2 = _attributeIndex2;
		}

		public void Close()
		{
			if (_indeces != null && _indeces.Length > 32)
			{
				_indeces = null;
			}
		}

		private bool Sort()
		{
			if (_indeces != null && _indeces.Length == _attributeCount && IsSorted())
			{
				return true;
			}
			object[] array = new object[_attributeCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			_indeces = array;
			Array.Sort(_indeces, 0, _attributeCount, this);
			return IsSorted();
		}

		private bool IsSorted()
		{
			for (int i = 0; i < _indeces.Length - 1; i++)
			{
				if (Compare(_indeces[i], _indeces[i + 1]) >= 0)
				{
					_attributeIndex1 = (int)_indeces[i];
					_attributeIndex2 = (int)_indeces[i + 1];
					return false;
				}
			}
			return true;
		}

		public int Compare(object obj1, object obj2)
		{
			int num = (int)obj1;
			int num2 = (int)obj2;
			XmlAttributeNode xmlAttributeNode = _attributeNodes[num];
			XmlAttributeNode xmlAttributeNode2 = _attributeNodes[num2];
			int num3 = CompareQNameType(xmlAttributeNode.QNameType, xmlAttributeNode2.QNameType);
			if (num3 == 0)
			{
				if (xmlAttributeNode.QNameType == QNameType.Normal)
				{
					num3 = xmlAttributeNode.LocalName.CompareTo(xmlAttributeNode2.LocalName);
					if (num3 == 0)
					{
						num3 = xmlAttributeNode.Namespace.Uri.CompareTo(xmlAttributeNode2.Namespace.Uri);
					}
				}
				else
				{
					num3 = xmlAttributeNode.Namespace.Prefix.CompareTo(xmlAttributeNode2.Namespace.Prefix);
				}
			}
			return num3;
		}

		public int CompareQNameType(QNameType type1, QNameType type2)
		{
			return type1 - type2;
		}
	}

	private sealed class NamespaceManager
	{
		private sealed class XmlAttribute
		{
			private XmlSpace _space;

			private string _lang = string.Empty;

			private int _depth;

			public int Depth
			{
				get
				{
					return _depth;
				}
				set
				{
					_depth = value;
				}
			}

			public string XmlLang
			{
				get
				{
					return _lang;
				}
				set
				{
					_lang = value;
				}
			}

			public XmlSpace XmlSpace
			{
				get
				{
					return _space;
				}
				set
				{
					_space = value;
				}
			}
		}

		private readonly XmlBufferReader _bufferReader;

		private Namespace[] _namespaces;

		private int _nsCount;

		private int _depth;

		private readonly Namespace[] _shortPrefixUri;

		private static readonly Namespace s_emptyNamespace = new Namespace(XmlBufferReader.Empty);

		private static Namespace s_xmlNamespace;

		private XmlAttribute[] _attributes;

		private int _attributeCount;

		private XmlSpace _space;

		private string _lang;

		public static Namespace XmlNamespace
		{
			get
			{
				if (s_xmlNamespace == null)
				{
					byte[] array = new byte[39]
					{
						120, 109, 108, 104, 116, 116, 112, 58, 47, 47,
						119, 119, 119, 46, 119, 51, 46, 111, 114, 103,
						47, 88, 77, 76, 47, 49, 57, 57, 56, 47,
						110, 97, 109, 101, 115, 112, 97, 99, 101
					};
					Namespace @namespace = new Namespace(new XmlBufferReader(array));
					@namespace.Prefix.SetValue(0, 3);
					@namespace.Uri.SetValue(3, array.Length - 3);
					s_xmlNamespace = @namespace;
				}
				return s_xmlNamespace;
			}
		}

		public static Namespace EmptyNamespace => s_emptyNamespace;

		public string XmlLang => _lang;

		public XmlSpace XmlSpace => _space;

		public NamespaceManager(XmlBufferReader bufferReader)
		{
			_bufferReader = bufferReader;
			_shortPrefixUri = new Namespace[28];
			_shortPrefixUri[0] = s_emptyNamespace;
			_namespaces = null;
			_nsCount = 0;
			_attributes = null;
			_attributeCount = 0;
			_space = XmlSpace.None;
			_lang = string.Empty;
			_depth = 0;
		}

		public void Close()
		{
			if (_namespaces != null && _namespaces.Length > 32)
			{
				_namespaces = null;
			}
			if (_attributes != null && _attributes.Length > 4)
			{
				_attributes = null;
			}
			_lang = string.Empty;
		}

		public void Clear()
		{
			if (_nsCount != 0)
			{
				for (int i = 0; i < _shortPrefixUri.Length; i++)
				{
					_shortPrefixUri[i] = null;
				}
				_shortPrefixUri[0] = s_emptyNamespace;
				_nsCount = 0;
			}
			_attributeCount = 0;
			_space = XmlSpace.None;
			_lang = string.Empty;
			_depth = 0;
		}

		public void EnterScope()
		{
			_depth++;
		}

		public void ExitScope()
		{
			while (_nsCount > 0)
			{
				Namespace @namespace = _namespaces[_nsCount - 1];
				if (@namespace.Depth != _depth)
				{
					break;
				}
				if (@namespace.Prefix.TryGetShortPrefix(out var type))
				{
					_shortPrefixUri[(int)type] = @namespace.OuterUri;
				}
				_nsCount--;
			}
			while (_attributeCount > 0)
			{
				XmlAttribute xmlAttribute = _attributes[_attributeCount - 1];
				if (xmlAttribute.Depth != _depth)
				{
					break;
				}
				_space = xmlAttribute.XmlSpace;
				_lang = xmlAttribute.XmlLang;
				_attributeCount--;
			}
			_depth--;
		}

		public void Sign(XmlSigningNodeWriter writer)
		{
			for (int i = 0; i < _nsCount; i++)
			{
				PrefixHandle prefix = _namespaces[i].Prefix;
				bool flag = false;
				for (int j = i + 1; j < _nsCount; j++)
				{
					if (object.Equals(prefix, _namespaces[j].Prefix))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					int offset;
					int length;
					byte[] @string = prefix.GetString(out offset, out length);
					int offset2;
					int length2;
					byte[] string2 = _namespaces[i].Uri.GetString(out offset2, out length2);
					writer.WriteXmlnsAttribute(@string, offset, length, string2, offset2, length2);
				}
			}
		}

		public void AddLangAttribute(string lang)
		{
			AddAttribute();
			_lang = lang;
		}

		public void AddSpaceAttribute(XmlSpace space)
		{
			AddAttribute();
			_space = space;
		}

		private void AddAttribute()
		{
			if (_attributes == null)
			{
				_attributes = new XmlAttribute[1];
			}
			else if (_attributes.Length == _attributeCount)
			{
				XmlAttribute[] array = new XmlAttribute[_attributeCount * 2];
				Array.Copy(_attributes, array, _attributeCount);
				_attributes = array;
			}
			XmlAttribute xmlAttribute = _attributes[_attributeCount];
			if (xmlAttribute == null)
			{
				xmlAttribute = new XmlAttribute();
				_attributes[_attributeCount] = xmlAttribute;
			}
			xmlAttribute.XmlLang = _lang;
			xmlAttribute.XmlSpace = _space;
			xmlAttribute.Depth = _depth;
			_attributeCount++;
		}

		public void Register(Namespace nameSpace)
		{
			if (nameSpace.Prefix.TryGetShortPrefix(out var type))
			{
				nameSpace.OuterUri = _shortPrefixUri[(int)type];
				_shortPrefixUri[(int)type] = nameSpace;
			}
			else
			{
				nameSpace.OuterUri = null;
			}
		}

		public Namespace AddNamespace()
		{
			if (_namespaces == null)
			{
				_namespaces = new Namespace[4];
			}
			else if (_namespaces.Length == _nsCount)
			{
				Namespace[] array = new Namespace[_nsCount * 2];
				Array.Copy(_namespaces, array, _nsCount);
				_namespaces = array;
			}
			Namespace @namespace = _namespaces[_nsCount];
			if (@namespace == null)
			{
				@namespace = new Namespace(_bufferReader);
				_namespaces[_nsCount] = @namespace;
			}
			@namespace.Clear();
			@namespace.Depth = _depth;
			_nsCount++;
			return @namespace;
		}

		public Namespace LookupNamespace(PrefixHandleType prefix)
		{
			return _shortPrefixUri[(int)prefix];
		}

		public Namespace LookupNamespace(PrefixHandle prefix)
		{
			if (prefix.TryGetShortPrefix(out var type))
			{
				return LookupNamespace(type);
			}
			for (int num = _nsCount - 1; num >= 0; num--)
			{
				Namespace @namespace = _namespaces[num];
				if (@namespace.Prefix == prefix)
				{
					return @namespace;
				}
			}
			if (prefix.IsXml)
			{
				return XmlNamespace;
			}
			return null;
		}

		public Namespace LookupNamespace(string prefix)
		{
			if (TryGetShortPrefix(prefix, out var shortPrefix))
			{
				return LookupNamespace(shortPrefix);
			}
			for (int num = _nsCount - 1; num >= 0; num--)
			{
				Namespace @namespace = _namespaces[num];
				if (@namespace.Prefix == prefix)
				{
					return @namespace;
				}
			}
			if (prefix == "xml")
			{
				return XmlNamespace;
			}
			return null;
		}

		private bool TryGetShortPrefix(string s, out PrefixHandleType shortPrefix)
		{
			switch (s.Length)
			{
			case 0:
				shortPrefix = PrefixHandleType.Empty;
				return true;
			case 1:
			{
				char c = s[0];
				if (c >= 'a' && c <= 'z')
				{
					shortPrefix = PrefixHandle.GetAlphaPrefix(c - 97);
					return true;
				}
				break;
			}
			}
			shortPrefix = PrefixHandleType.Empty;
			return false;
		}
	}

	protected class Namespace
	{
		private readonly PrefixHandle _prefix;

		private readonly StringHandle _uri;

		private int _depth;

		private Namespace _outerUri;

		private string _uriString;

		public int Depth
		{
			get
			{
				return _depth;
			}
			set
			{
				_depth = value;
			}
		}

		public PrefixHandle Prefix => _prefix;

		public StringHandle Uri => _uri;

		public Namespace OuterUri
		{
			get
			{
				return _outerUri;
			}
			set
			{
				_outerUri = value;
			}
		}

		public Namespace(XmlBufferReader bufferReader)
		{
			_prefix = new PrefixHandle(bufferReader);
			_uri = new StringHandle(bufferReader);
			_outerUri = null;
			_uriString = null;
		}

		public void Clear()
		{
			_uriString = null;
		}

		public bool IsUri(string s)
		{
			if ((object)s == _uriString)
			{
				return true;
			}
			if (_uri == s)
			{
				_uriString = s;
				return true;
			}
			return false;
		}

		public bool IsUri(XmlDictionaryString s)
		{
			if ((object)s.Value == _uriString)
			{
				return true;
			}
			if (_uri == s)
			{
				_uriString = s.Value;
				return true;
			}
			return false;
		}
	}

	private readonly XmlBufferReader _bufferReader;

	private XmlNode _node;

	private readonly NamespaceManager _nsMgr;

	private XmlElementNode[] _elementNodes;

	private XmlAttributeNode[] _attributeNodes;

	private readonly XmlAtomicTextNode _atomicTextNode;

	private int _depth;

	private int _attributeCount;

	private int _attributeStart;

	private readonly XmlDictionaryReaderQuotas _quotas;

	private XmlNameTable _nameTable;

	private XmlDeclarationNode _declarationNode;

	private XmlComplexTextNode _complexTextNode;

	private XmlWhitespaceTextNode _whitespaceTextNode;

	private XmlCDataNode _cdataNode;

	private XmlCommentNode _commentNode;

	private readonly XmlElementNode _rootElementNode;

	private int _attributeIndex;

	private char[] _chars;

	private string _prefix;

	private string _localName;

	private string _ns;

	private string _value;

	private int _trailCharCount;

	private int _trailByteCount;

	private char[] _trailChars;

	private byte[] _trailBytes;

	private bool _rootElement;

	private bool _readingElement;

	private AttributeSorter _attributeSorter;

	private static readonly XmlInitialNode s_initialNode = new XmlInitialNode(XmlBufferReader.Empty);

	private static readonly XmlEndOfFileNode s_endOfFileNode = new XmlEndOfFileNode(XmlBufferReader.Empty);

	private static readonly XmlClosedNode s_closedNode = new XmlClosedNode(XmlBufferReader.Empty);

	private static Base64Encoding s_base64Encoding;

	private static BinHexEncoding s_binHexEncoding;

	private const string xmlns = "xmlns";

	private const string xml = "xml";

	private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

	private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";

	private XmlSigningNodeWriter _signingWriter;

	private bool _signing;

	private static Base64Encoding Base64Encoding
	{
		get
		{
			if (s_base64Encoding == null)
			{
				s_base64Encoding = new Base64Encoding();
			}
			return s_base64Encoding;
		}
	}

	private static BinHexEncoding BinHexEncoding
	{
		get
		{
			if (s_binHexEncoding == null)
			{
				s_binHexEncoding = new BinHexEncoding();
			}
			return s_binHexEncoding;
		}
	}

	protected XmlBufferReader BufferReader => _bufferReader;

	public override XmlDictionaryReaderQuotas Quotas => _quotas;

	protected XmlNode Node => _node;

	protected XmlElementNode ElementNode
	{
		get
		{
			if (_depth == 0)
			{
				return _rootElementNode;
			}
			return _elementNodes[_depth];
		}
	}

	protected bool OutsideRootElement => _depth == 0;

	public override bool CanReadBinaryContent => true;

	public override bool CanReadValueChunk => true;

	public override string BaseURI => string.Empty;

	public override bool HasValue => _node.HasValue;

	public override bool IsDefault => false;

	public override string this[int index] => GetAttribute(index);

	public override string this[string name] => GetAttribute(name);

	public override string this[string localName, string namespaceUri] => GetAttribute(localName, namespaceUri);

	public override int AttributeCount
	{
		get
		{
			if (_node.CanGetAttribute)
			{
				return _attributeCount;
			}
			return 0;
		}
	}

	public sealed override int Depth => _depth + _node.DepthDelta;

	public override bool EOF => _node.ReadState == ReadState.EndOfFile;

	public sealed override bool IsEmptyElement => _node.IsEmptyElement;

	public override string LocalName
	{
		get
		{
			if (_localName == null)
			{
				if (_node.QNameType == QNameType.Normal)
				{
					_localName = _node.LocalName.GetString(NameTable);
				}
				else if (_node.Namespace.Prefix.IsEmpty)
				{
					_localName = "xmlns";
				}
				else
				{
					_localName = _node.Namespace.Prefix.GetString(NameTable);
				}
			}
			return _localName;
		}
	}

	public override string NamespaceURI
	{
		get
		{
			if (_ns == null)
			{
				if (_node.QNameType == QNameType.Normal)
				{
					_ns = _node.Namespace.Uri.GetString(NameTable);
				}
				else
				{
					_ns = "http://www.w3.org/2000/xmlns/";
				}
			}
			return _ns;
		}
	}

	public override XmlNameTable NameTable
	{
		get
		{
			if (_nameTable == null)
			{
				_nameTable = new NameTable();
				_nameTable.Add("xml");
				_nameTable.Add("xmlns");
				_nameTable.Add("http://www.w3.org/2000/xmlns/");
				_nameTable.Add("http://www.w3.org/XML/1998/namespace");
				for (PrefixHandleType prefixHandleType = PrefixHandleType.A; prefixHandleType <= PrefixHandleType.Z; prefixHandleType++)
				{
					_nameTable.Add(PrefixHandle.GetString(prefixHandleType));
				}
			}
			return _nameTable;
		}
	}

	public sealed override XmlNodeType NodeType => _node.NodeType;

	public override string Prefix
	{
		get
		{
			if (_prefix == null)
			{
				switch (_node.QNameType)
				{
				case QNameType.Normal:
					_prefix = _node.Prefix.GetString(NameTable);
					break;
				case QNameType.Xmlns:
					if (_node.Namespace.Prefix.IsEmpty)
					{
						_prefix = string.Empty;
					}
					else
					{
						_prefix = "xmlns";
					}
					break;
				default:
					_prefix = "xml";
					break;
				}
			}
			return _prefix;
		}
	}

	public override ReadState ReadState => _node.ReadState;

	public override string Value
	{
		get
		{
			if (_value == null)
			{
				_value = _node.ValueAsString;
			}
			return _value;
		}
	}

	public override Type ValueType
	{
		get
		{
			if (_value == null && _node.QNameType == QNameType.Normal)
			{
				Type type = _node.Value.ToType();
				if (_node.IsAtomicValue)
				{
					return type;
				}
				if (type == typeof(byte[]))
				{
					return type;
				}
			}
			return typeof(string);
		}
	}

	public override string XmlLang => _nsMgr.XmlLang;

	public override XmlSpace XmlSpace => _nsMgr.XmlSpace;

	public override bool CanCanonicalize => true;

	protected bool Signing => _signing;

	protected XmlBaseReader()
	{
		_bufferReader = new XmlBufferReader(this);
		_nsMgr = new NamespaceManager(_bufferReader);
		_quotas = new XmlDictionaryReaderQuotas();
		_rootElementNode = new XmlElementNode(_bufferReader);
		_atomicTextNode = new XmlAtomicTextNode(_bufferReader);
		_node = s_closedNode;
	}

	protected void MoveToNode(XmlNode node)
	{
		_node = node;
		_ns = null;
		_localName = null;
		_prefix = null;
		_value = null;
	}

	protected void MoveToInitial(XmlDictionaryReaderQuotas quotas)
	{
		if (quotas == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("quotas");
		}
		quotas.InternalCopyTo(_quotas);
		_quotas.MakeReadOnly();
		_nsMgr.Clear();
		_depth = 0;
		_attributeCount = 0;
		_attributeStart = -1;
		_attributeIndex = -1;
		_rootElement = false;
		_readingElement = false;
		MoveToNode(s_initialNode);
	}

	protected XmlDeclarationNode MoveToDeclaration()
	{
		if (_attributeCount < 1)
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlDeclMissingVersion));
		}
		if (_attributeCount > 3)
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlMalformedDecl));
		}
		if (!CheckDeclAttribute(0, "version", "1.0", checkLower: false, System.SR.XmlInvalidVersion))
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlDeclMissingVersion));
		}
		if (_attributeCount > 1)
		{
			if (CheckDeclAttribute(1, "encoding", null, checkLower: true, System.SR.XmlInvalidEncoding_UTF8))
			{
				if (_attributeCount == 3 && !CheckStandalone(2))
				{
					XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlMalformedDecl));
				}
			}
			else if (!CheckStandalone(1) || _attributeCount > 2)
			{
				XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlMalformedDecl));
			}
		}
		if (_declarationNode == null)
		{
			_declarationNode = new XmlDeclarationNode(_bufferReader);
		}
		MoveToNode(_declarationNode);
		return _declarationNode;
	}

	private bool CheckStandalone(int attr)
	{
		XmlAttributeNode xmlAttributeNode = _attributeNodes[attr];
		if (!xmlAttributeNode.Prefix.IsEmpty)
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlMalformedDecl));
		}
		if (xmlAttributeNode.LocalName != "standalone")
		{
			return false;
		}
		if (!xmlAttributeNode.Value.Equals2("yes", checkLower: false) && !xmlAttributeNode.Value.Equals2("no", checkLower: false))
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlInvalidStandalone));
		}
		return true;
	}

	private bool CheckDeclAttribute(int index, string localName, string value, bool checkLower, string valueSR)
	{
		XmlAttributeNode xmlAttributeNode = _attributeNodes[index];
		if (!xmlAttributeNode.Prefix.IsEmpty)
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.XmlMalformedDecl));
		}
		if (xmlAttributeNode.LocalName != localName)
		{
			return false;
		}
		if (value != null && !xmlAttributeNode.Value.Equals2(value, checkLower))
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.Format(valueSR)));
		}
		return true;
	}

	protected XmlCommentNode MoveToComment()
	{
		if (_commentNode == null)
		{
			_commentNode = new XmlCommentNode(_bufferReader);
		}
		MoveToNode(_commentNode);
		return _commentNode;
	}

	protected XmlCDataNode MoveToCData()
	{
		if (_cdataNode == null)
		{
			_cdataNode = new XmlCDataNode(_bufferReader);
		}
		MoveToNode(_cdataNode);
		return _cdataNode;
	}

	protected XmlAtomicTextNode MoveToAtomicText()
	{
		XmlAtomicTextNode atomicTextNode = _atomicTextNode;
		MoveToNode(atomicTextNode);
		return atomicTextNode;
	}

	protected XmlComplexTextNode MoveToComplexText()
	{
		if (_complexTextNode == null)
		{
			_complexTextNode = new XmlComplexTextNode(_bufferReader);
		}
		MoveToNode(_complexTextNode);
		return _complexTextNode;
	}

	protected XmlTextNode MoveToWhitespaceText()
	{
		if (_whitespaceTextNode == null)
		{
			_whitespaceTextNode = new XmlWhitespaceTextNode(_bufferReader);
		}
		if (_nsMgr.XmlSpace == XmlSpace.Preserve)
		{
			_whitespaceTextNode.NodeType = XmlNodeType.SignificantWhitespace;
		}
		else
		{
			_whitespaceTextNode.NodeType = XmlNodeType.Whitespace;
		}
		MoveToNode(_whitespaceTextNode);
		return _whitespaceTextNode;
	}

	protected void MoveToEndElement()
	{
		if (_depth == 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
		}
		XmlElementNode xmlElementNode = _elementNodes[_depth];
		XmlEndElementNode endElement = xmlElementNode.EndElement;
		endElement.Namespace = xmlElementNode.Namespace;
		MoveToNode(endElement);
	}

	protected void MoveToEndOfFile()
	{
		if (_depth != 0)
		{
			XmlExceptionHelper.ThrowUnexpectedEndOfFile(this);
		}
		MoveToNode(s_endOfFileNode);
	}

	protected XmlElementNode EnterScope()
	{
		if (_depth == 0)
		{
			if (_rootElement)
			{
				XmlExceptionHelper.ThrowMultipleRootElements(this);
			}
			_rootElement = true;
		}
		_nsMgr.EnterScope();
		_depth++;
		if (_depth > _quotas.MaxDepth)
		{
			XmlExceptionHelper.ThrowMaxDepthExceeded(this, _quotas.MaxDepth);
		}
		if (_elementNodes == null)
		{
			_elementNodes = new XmlElementNode[4];
		}
		else if (_elementNodes.Length == _depth)
		{
			XmlElementNode[] array = new XmlElementNode[_depth * 2];
			Array.Copy(_elementNodes, array, _depth);
			_elementNodes = array;
		}
		XmlElementNode xmlElementNode = _elementNodes[_depth];
		if (xmlElementNode == null)
		{
			xmlElementNode = new XmlElementNode(_bufferReader);
			_elementNodes[_depth] = xmlElementNode;
		}
		_attributeCount = 0;
		_attributeStart = -1;
		_attributeIndex = -1;
		MoveToNode(xmlElementNode);
		return xmlElementNode;
	}

	protected void ExitScope()
	{
		if (_depth == 0)
		{
			XmlExceptionHelper.ThrowUnexpectedEndElement(this);
		}
		_depth--;
		_nsMgr.ExitScope();
	}

	private XmlAttributeNode AddAttribute(QNameType qnameType, bool isAtomicValue)
	{
		int attributeCount = _attributeCount;
		if (_attributeNodes == null)
		{
			_attributeNodes = new XmlAttributeNode[4];
		}
		else if (_attributeNodes.Length == attributeCount)
		{
			XmlAttributeNode[] array = new XmlAttributeNode[attributeCount * 2];
			Array.Copy(_attributeNodes, array, attributeCount);
			_attributeNodes = array;
		}
		XmlAttributeNode xmlAttributeNode = _attributeNodes[attributeCount];
		if (xmlAttributeNode == null)
		{
			xmlAttributeNode = new XmlAttributeNode(_bufferReader);
			_attributeNodes[attributeCount] = xmlAttributeNode;
		}
		xmlAttributeNode.QNameType = qnameType;
		xmlAttributeNode.IsAtomicValue = isAtomicValue;
		xmlAttributeNode.AttributeText.QNameType = qnameType;
		xmlAttributeNode.AttributeText.IsAtomicValue = isAtomicValue;
		_attributeCount++;
		return xmlAttributeNode;
	}

	protected Namespace AddNamespace()
	{
		return _nsMgr.AddNamespace();
	}

	protected XmlAttributeNode AddAttribute()
	{
		return AddAttribute(QNameType.Normal, isAtomicValue: true);
	}

	protected XmlAttributeNode AddXmlAttribute()
	{
		return AddAttribute(QNameType.Normal, isAtomicValue: true);
	}

	protected XmlAttributeNode AddXmlnsAttribute(Namespace ns)
	{
		if (!ns.Prefix.IsEmpty && ns.Uri.IsEmpty)
		{
			XmlExceptionHelper.ThrowEmptyNamespace(this);
		}
		if (ns.Prefix.IsXml && ns.Uri != "http://www.w3.org/XML/1998/namespace")
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.Format(System.SR.XmlSpecificBindingPrefix, "xml", "http://www.w3.org/XML/1998/namespace")));
		}
		else if (ns.Prefix.IsXmlns && ns.Uri != "http://www.w3.org/2000/xmlns/")
		{
			XmlExceptionHelper.ThrowXmlException(this, new XmlException(System.SR.Format(System.SR.XmlSpecificBindingPrefix, "xmlns", "http://www.w3.org/2000/xmlns/")));
		}
		_nsMgr.Register(ns);
		XmlAttributeNode xmlAttributeNode = AddAttribute(QNameType.Xmlns, isAtomicValue: false);
		xmlAttributeNode.Namespace = ns;
		xmlAttributeNode.AttributeText.Namespace = ns;
		return xmlAttributeNode;
	}

	protected void FixXmlAttribute(XmlAttributeNode attributeNode)
	{
		if (!(attributeNode.Prefix == "xml"))
		{
			return;
		}
		if (attributeNode.LocalName == "lang")
		{
			_nsMgr.AddLangAttribute(attributeNode.Value.GetString());
		}
		else if (attributeNode.LocalName == "space")
		{
			string @string = attributeNode.Value.GetString();
			if (@string == "preserve")
			{
				_nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
			}
			else if (@string == "default")
			{
				_nsMgr.AddSpaceAttribute(XmlSpace.Default);
			}
		}
	}

	public override void Close()
	{
		MoveToNode(s_closedNode);
		_nameTable = null;
		if (_attributeNodes != null && _attributeNodes.Length > 16)
		{
			_attributeNodes = null;
		}
		if (_elementNodes != null && _elementNodes.Length > 16)
		{
			_elementNodes = null;
		}
		_nsMgr.Close();
		_bufferReader.Close();
		if (_signingWriter != null)
		{
			_signingWriter.Close();
		}
		if (_attributeSorter != null)
		{
			_attributeSorter.Close();
		}
	}

	private XmlAttributeNode GetAttributeNode(int index)
	{
		if (!_node.CanGetAttribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("index", System.SR.XmlElementAttributes));
		}
		if (index < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative));
		}
		if (index >= _attributeCount)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("index", System.SR.Format(System.SR.OffsetExceedsBufferSize, _attributeCount)));
		}
		return _attributeNodes[index];
	}

	private XmlAttributeNode GetAttributeNode(string name)
	{
		if (name == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("name"));
		}
		if (!_node.CanGetAttribute)
		{
			return null;
		}
		int num = name.IndexOf(':');
		string prefix;
		string localName;
		if (num == -1)
		{
			if (name == "xmlns")
			{
				prefix = "xmlns";
				localName = string.Empty;
			}
			else
			{
				prefix = string.Empty;
				localName = name;
			}
		}
		else
		{
			prefix = name.Substring(0, num);
			localName = name.Substring(num + 1);
		}
		XmlAttributeNode[] attributeNodes = _attributeNodes;
		int attributeCount = _attributeCount;
		int num2 = _attributeStart;
		for (int i = 0; i < attributeCount; i++)
		{
			if (++num2 >= attributeCount)
			{
				num2 = 0;
			}
			XmlAttributeNode xmlAttributeNode = attributeNodes[num2];
			if (xmlAttributeNode.IsPrefixAndLocalName(prefix, localName))
			{
				_attributeStart = num2;
				return xmlAttributeNode;
			}
		}
		return null;
	}

	private XmlAttributeNode GetAttributeNode(string localName, string namespaceUri)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (namespaceUri == null)
		{
			namespaceUri = string.Empty;
		}
		if (!_node.CanGetAttribute)
		{
			return null;
		}
		XmlAttributeNode[] attributeNodes = _attributeNodes;
		int attributeCount = _attributeCount;
		int num = _attributeStart;
		for (int i = 0; i < attributeCount; i++)
		{
			if (++num >= attributeCount)
			{
				num = 0;
			}
			XmlAttributeNode xmlAttributeNode = attributeNodes[num];
			if (xmlAttributeNode.IsLocalNameAndNamespaceUri(localName, namespaceUri))
			{
				_attributeStart = num;
				return xmlAttributeNode;
			}
		}
		return null;
	}

	private XmlAttributeNode GetAttributeNode(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (namespaceUri == null)
		{
			namespaceUri = XmlDictionaryString.Empty;
		}
		if (!_node.CanGetAttribute)
		{
			return null;
		}
		XmlAttributeNode[] attributeNodes = _attributeNodes;
		int attributeCount = _attributeCount;
		int num = _attributeStart;
		for (int i = 0; i < attributeCount; i++)
		{
			if (++num >= attributeCount)
			{
				num = 0;
			}
			XmlAttributeNode xmlAttributeNode = attributeNodes[num];
			if (xmlAttributeNode.IsLocalNameAndNamespaceUri(localName, namespaceUri))
			{
				_attributeStart = num;
				return xmlAttributeNode;
			}
		}
		return null;
	}

	public override string GetAttribute(int index)
	{
		return GetAttributeNode(index).ValueAsString;
	}

	public override string GetAttribute(string name)
	{
		return GetAttributeNode(name)?.ValueAsString;
	}

	public override string GetAttribute(string localName, string namespaceUri)
	{
		return GetAttributeNode(localName, namespaceUri)?.ValueAsString;
	}

	public override string GetAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return GetAttributeNode(localName, namespaceUri)?.ValueAsString;
	}

	public override string LookupNamespace(string prefix)
	{
		Namespace @namespace = _nsMgr.LookupNamespace(prefix);
		if (@namespace != null)
		{
			return @namespace.Uri.GetString(NameTable);
		}
		if (prefix == "xmlns")
		{
			return "http://www.w3.org/2000/xmlns/";
		}
		return null;
	}

	protected Namespace LookupNamespace(PrefixHandleType prefix)
	{
		Namespace @namespace = _nsMgr.LookupNamespace(prefix);
		if (@namespace == null)
		{
			XmlExceptionHelper.ThrowUndefinedPrefix(this, PrefixHandle.GetString(prefix));
		}
		return @namespace;
	}

	protected Namespace LookupNamespace(PrefixHandle prefix)
	{
		Namespace @namespace = _nsMgr.LookupNamespace(prefix);
		if (@namespace == null)
		{
			XmlExceptionHelper.ThrowUndefinedPrefix(this, prefix.GetString());
		}
		return @namespace;
	}

	protected void ProcessAttributes()
	{
		if (_attributeCount > 0)
		{
			ProcessAttributes(_attributeNodes, _attributeCount);
		}
	}

	private void ProcessAttributes(XmlAttributeNode[] attributeNodes, int attributeCount)
	{
		for (int i = 0; i < attributeCount; i++)
		{
			XmlAttributeNode xmlAttributeNode = attributeNodes[i];
			if (xmlAttributeNode.QNameType == QNameType.Normal)
			{
				PrefixHandle prefix = xmlAttributeNode.Prefix;
				if (!prefix.IsEmpty)
				{
					xmlAttributeNode.Namespace = LookupNamespace(prefix);
				}
				else
				{
					xmlAttributeNode.Namespace = NamespaceManager.EmptyNamespace;
				}
				xmlAttributeNode.AttributeText.Namespace = xmlAttributeNode.Namespace;
			}
		}
		if (attributeCount <= 1)
		{
			return;
		}
		if (attributeCount < 12)
		{
			for (int j = 0; j < attributeCount - 1; j++)
			{
				XmlAttributeNode xmlAttributeNode2 = attributeNodes[j];
				if (xmlAttributeNode2.QNameType == QNameType.Normal)
				{
					for (int k = j + 1; k < attributeCount; k++)
					{
						XmlAttributeNode xmlAttributeNode3 = attributeNodes[k];
						if (xmlAttributeNode3.QNameType == QNameType.Normal && xmlAttributeNode2.LocalName == xmlAttributeNode3.LocalName && xmlAttributeNode2.Namespace.Uri == xmlAttributeNode3.Namespace.Uri)
						{
							XmlExceptionHelper.ThrowDuplicateAttribute(this, xmlAttributeNode2.Prefix.GetString(), xmlAttributeNode3.Prefix.GetString(), xmlAttributeNode2.LocalName.GetString(), xmlAttributeNode2.Namespace.Uri.GetString());
						}
					}
					continue;
				}
				for (int l = j + 1; l < attributeCount; l++)
				{
					XmlAttributeNode xmlAttributeNode4 = attributeNodes[l];
					if (xmlAttributeNode4.QNameType == QNameType.Xmlns && xmlAttributeNode2.Namespace.Prefix == xmlAttributeNode4.Namespace.Prefix)
					{
						XmlExceptionHelper.ThrowDuplicateAttribute(this, "xmlns", "xmlns", xmlAttributeNode2.Namespace.Prefix.GetString(), "http://www.w3.org/2000/xmlns/");
					}
				}
			}
		}
		else
		{
			CheckAttributes(attributeNodes, attributeCount);
		}
	}

	private void CheckAttributes(XmlAttributeNode[] attributeNodes, int attributeCount)
	{
		if (_attributeSorter == null)
		{
			_attributeSorter = new AttributeSorter();
		}
		if (!_attributeSorter.Sort(attributeNodes, attributeCount))
		{
			_attributeSorter.GetIndeces(out var attributeIndex, out var attributeIndex2);
			if (attributeNodes[attributeIndex].QNameType == QNameType.Xmlns)
			{
				XmlExceptionHelper.ThrowDuplicateXmlnsAttribute(this, attributeNodes[attributeIndex].Namespace.Prefix.GetString(), "http://www.w3.org/2000/xmlns/");
			}
			else
			{
				XmlExceptionHelper.ThrowDuplicateAttribute(this, attributeNodes[attributeIndex].Prefix.GetString(), attributeNodes[attributeIndex2].Prefix.GetString(), attributeNodes[attributeIndex].LocalName.GetString(), attributeNodes[attributeIndex].Namespace.Uri.GetString());
			}
		}
	}

	public override void MoveToAttribute(int index)
	{
		MoveToNode(GetAttributeNode(index));
	}

	public override bool MoveToAttribute(string name)
	{
		XmlNode attributeNode = GetAttributeNode(name);
		if (attributeNode == null)
		{
			return false;
		}
		MoveToNode(attributeNode);
		return true;
	}

	public override bool MoveToAttribute(string localName, string namespaceUri)
	{
		XmlNode attributeNode = GetAttributeNode(localName, namespaceUri);
		if (attributeNode == null)
		{
			return false;
		}
		MoveToNode(attributeNode);
		return true;
	}

	public override bool MoveToElement()
	{
		if (!_node.CanMoveToElement)
		{
			return false;
		}
		if (_depth == 0)
		{
			MoveToDeclaration();
		}
		else
		{
			MoveToNode(_elementNodes[_depth]);
		}
		_attributeIndex = -1;
		return true;
	}

	public override XmlNodeType MoveToContent()
	{
		do
		{
			if (_node.HasContent)
			{
				if ((_node.NodeType != XmlNodeType.Text && _node.NodeType != XmlNodeType.CDATA) || _trailByteCount > 0)
				{
					break;
				}
				if (_value == null)
				{
					if (!_node.Value.IsWhitespace())
					{
						break;
					}
				}
				else if (!XmlConverter.IsWhitespace(_value))
				{
					break;
				}
			}
			else if (_node.NodeType == XmlNodeType.Attribute)
			{
				MoveToElement();
				break;
			}
		}
		while (Read());
		return _node.NodeType;
	}

	public override bool MoveToFirstAttribute()
	{
		if (!_node.CanGetAttribute || _attributeCount == 0)
		{
			return false;
		}
		MoveToNode(GetAttributeNode(0));
		_attributeIndex = 0;
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (!_node.CanGetAttribute)
		{
			return false;
		}
		int num = _attributeIndex + 1;
		if (num >= _attributeCount)
		{
			return false;
		}
		MoveToNode(GetAttributeNode(num));
		_attributeIndex = num;
		return true;
	}

	public override bool IsLocalName(string localName)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		return _node.IsLocalName(localName);
	}

	public override bool IsLocalName(XmlDictionaryString localName)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		return _node.IsLocalName(localName);
	}

	public override bool IsNamespaceUri(string namespaceUri)
	{
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		return _node.IsNamespaceUri(namespaceUri);
	}

	public override bool IsNamespaceUri(XmlDictionaryString namespaceUri)
	{
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		return _node.IsNamespaceUri(namespaceUri);
	}

	public sealed override bool IsStartElement()
	{
		switch (_node.NodeType)
		{
		case XmlNodeType.Element:
			return true;
		case XmlNodeType.EndElement:
			return false;
		case XmlNodeType.None:
			Read();
			if (_node.NodeType == XmlNodeType.Element)
			{
				return true;
			}
			break;
		}
		return MoveToContent() == XmlNodeType.Element;
	}

	public override bool IsStartElement(string name)
	{
		if (name == null)
		{
			return false;
		}
		int num = name.IndexOf(':');
		string text;
		string text2;
		if (num == -1)
		{
			text = string.Empty;
			text2 = name;
		}
		else
		{
			text = name.Substring(0, num);
			text2 = name.Substring(num + 1);
		}
		if ((_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.Prefix == text)
		{
			return _node.LocalName == text2;
		}
		return false;
	}

	public override bool IsStartElement(string localName, string namespaceUri)
	{
		if (localName == null)
		{
			return false;
		}
		if (namespaceUri == null)
		{
			return false;
		}
		if ((_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.LocalName == localName)
		{
			return _node.IsNamespaceUri(namespaceUri);
		}
		return false;
	}

	public override bool IsStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("localName");
		}
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		if ((_node.NodeType == XmlNodeType.Element || IsStartElement()) && _node.LocalName == localName)
		{
			return _node.IsNamespaceUri(namespaceUri);
		}
		return false;
	}

	public override int IndexOfLocalName(string[] localNames, string namespaceUri)
	{
		if (localNames == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("localNames");
		}
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		QNameType qNameType = _node.QNameType;
		if (_node.IsNamespaceUri(namespaceUri))
		{
			if (qNameType == QNameType.Normal)
			{
				StringHandle localName = _node.LocalName;
				for (int i = 0; i < localNames.Length; i++)
				{
					string text = localNames[i];
					if (text == null)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
					}
					if (localName == text)
					{
						return i;
					}
				}
			}
			else
			{
				PrefixHandle prefix = _node.Namespace.Prefix;
				for (int j = 0; j < localNames.Length; j++)
				{
					string text2 = localNames[j];
					if (text2 == null)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{j}]");
					}
					if (prefix == text2)
					{
						return j;
					}
				}
			}
		}
		return -1;
	}

	public override int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString namespaceUri)
	{
		if (localNames == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("localNames");
		}
		if (namespaceUri == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("namespaceUri");
		}
		QNameType qNameType = _node.QNameType;
		if (_node.IsNamespaceUri(namespaceUri))
		{
			if (qNameType == QNameType.Normal)
			{
				StringHandle localName = _node.LocalName;
				for (int i = 0; i < localNames.Length; i++)
				{
					XmlDictionaryString xmlDictionaryString = localNames[i];
					if (xmlDictionaryString == null)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{i}]");
					}
					if (localName == xmlDictionaryString)
					{
						return i;
					}
				}
			}
			else
			{
				PrefixHandle prefix = _node.Namespace.Prefix;
				for (int j = 0; j < localNames.Length; j++)
				{
					XmlDictionaryString xmlDictionaryString2 = localNames[j];
					if (xmlDictionaryString2 == null)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull($"localNames[{j}]");
					}
					if (prefix == xmlDictionaryString2)
					{
						return j;
					}
				}
			}
		}
		return -1;
	}

	public override int ReadValueChunk(char[] chars, int offset, int count)
	{
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > chars.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));
		}
		if (_value == null && _node.QNameType == QNameType.Normal && _node.Value.TryReadChars(chars, offset, count, out var actual))
		{
			return actual;
		}
		string value = Value;
		actual = Math.Min(count, value.Length);
		value.CopyTo(0, chars, offset, actual);
		_value = value.Substring(actual);
		return actual;
	}

	public override int ReadValueAsBase64(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > buffer.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		if (count == 0)
		{
			return 0;
		}
		if (_value == null && _trailByteCount == 0 && _trailCharCount == 0 && _node.QNameType == QNameType.Normal && _node.Value.TryReadBase64(buffer, offset, count, out var actual))
		{
			return actual;
		}
		return ReadBytes(Base64Encoding, 3, 4, buffer, offset, Math.Min(count, 512), readContent: false);
	}

	public override string ReadElementContentAsString()
	{
		if (_node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		if (_node.IsEmptyElement)
		{
			Read();
			return string.Empty;
		}
		Read();
		string result = ReadContentAsString();
		ReadEndElement();
		return result;
	}

	public override void ReadStartElement()
	{
		if (_node.NodeType != XmlNodeType.Element)
		{
			MoveToStartElement();
		}
		Read();
	}

	public override void ReadStartElement(string name)
	{
		MoveToStartElement(name);
		Read();
	}

	public override void ReadStartElement(string localName, string namespaceUri)
	{
		MoveToStartElement(localName, namespaceUri);
		Read();
	}

	public override void ReadEndElement()
	{
		if (_node.NodeType != XmlNodeType.EndElement && MoveToContent() != XmlNodeType.EndElement)
		{
			int num = ((_node.NodeType == XmlNodeType.Element) ? (_depth - 1) : _depth);
			if (num == 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlEndElementNoOpenNodes));
			}
			XmlElementNode xmlElementNode = _elementNodes[num];
			XmlExceptionHelper.ThrowEndElementExpected(this, xmlElementNode.LocalName.GetString(), xmlElementNode.Namespace.Uri.GetString());
		}
		Read();
	}

	public override bool ReadAttributeValue()
	{
		XmlAttributeTextNode attributeText = _node.AttributeText;
		if (attributeText == null)
		{
			return false;
		}
		MoveToNode(attributeText);
		return true;
	}

	private void SkipValue(XmlNode node)
	{
		if (node.SkipValue)
		{
			Read();
		}
	}

	public override bool TryGetBase64ContentLength(out int length)
	{
		if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null)
		{
			XmlNode node = Node;
			if (node.IsAtomicValue)
			{
				return node.Value.TryGetByteArrayLength(out length);
			}
		}
		return base.TryGetBase64ContentLength(out length);
	}

	public override byte[] ReadContentAsBase64()
	{
		if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null)
		{
			XmlNode node = Node;
			if (node.IsAtomicValue)
			{
				byte[] array = node.Value.ToByteArray();
				if (array.Length > _quotas.MaxArrayLength)
				{
					XmlExceptionHelper.ThrowMaxArrayLengthExceeded(this, _quotas.MaxArrayLength);
				}
				SkipValue(node);
				return array;
			}
		}
		if (!_bufferReader.IsStreamed)
		{
			return ReadContentAsBase64(_quotas.MaxArrayLength, _bufferReader.Buffer.Length);
		}
		return ReadContentAsBase64(_quotas.MaxArrayLength, 65535);
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int offset, int count)
	{
		if (!_readingElement)
		{
			if (IsEmptyElement)
			{
				Read();
				return 0;
			}
			ReadStartElement();
			_readingElement = true;
		}
		int num = ReadContentAsBase64(buffer, offset, count);
		if (num == 0)
		{
			ReadEndElement();
			_readingElement = false;
		}
		return num;
	}

	public override int ReadContentAsBase64(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > buffer.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		if (count == 0)
		{
			return 0;
		}
		if (_trailByteCount == 0 && _trailCharCount == 0 && _value == null && _node.QNameType == QNameType.Normal)
		{
			int actual;
			while (_node.NodeType != XmlNodeType.Comment && _node.Value.TryReadBase64(buffer, offset, count, out actual))
			{
				if (actual != 0)
				{
					return actual;
				}
				Read();
			}
		}
		XmlNodeType nodeType = _node.NodeType;
		if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
		{
			return 0;
		}
		return ReadBytes(Base64Encoding, 3, 4, buffer, offset, Math.Min(count, 512), readContent: true);
	}

	public override byte[] ReadContentAsBinHex()
	{
		return ReadContentAsBinHex(_quotas.MaxArrayLength);
	}

	public override int ReadContentAsBinHex(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (offset > buffer.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.Format(System.SR.OffsetExceedsBufferSize, buffer.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		if (count == 0)
		{
			return 0;
		}
		return ReadBytes(BinHexEncoding, 1, 2, buffer, offset, Math.Min(count, 512), readContent: true);
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int offset, int count)
	{
		if (!_readingElement)
		{
			if (IsEmptyElement)
			{
				Read();
				return 0;
			}
			ReadStartElement();
			_readingElement = true;
		}
		int num = ReadContentAsBinHex(buffer, offset, count);
		if (num == 0)
		{
			ReadEndElement();
			_readingElement = false;
		}
		return num;
	}

	private int ReadBytes(Encoding encoding, int byteBlock, int charBlock, byte[] buffer, int offset, int byteCount, bool readContent)
	{
		if (_trailByteCount > 0)
		{
			int num = Math.Min(_trailByteCount, byteCount);
			Buffer.BlockCopy(_trailBytes, 0, buffer, offset, num);
			_trailByteCount -= num;
			Buffer.BlockCopy(_trailBytes, num, _trailBytes, 0, _trailByteCount);
			return num;
		}
		XmlNodeType nodeType = _node.NodeType;
		if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
		{
			return 0;
		}
		int num2 = ((byteCount >= byteBlock) ? (byteCount / byteBlock * charBlock) : charBlock);
		char[] charBuffer = GetCharBuffer(num2);
		int num3 = 0;
		while (true)
		{
			if (_trailCharCount > 0)
			{
				Array.Copy(_trailChars, 0, charBuffer, num3, _trailCharCount);
				num3 += _trailCharCount;
				_trailCharCount = 0;
			}
			while (num3 < charBlock)
			{
				int num4;
				if (readContent)
				{
					num4 = ReadContentAsChars(charBuffer, num3, num2 - num3);
					if (num4 == 1 && charBuffer[num3] == '\n')
					{
						continue;
					}
				}
				else
				{
					num4 = ReadValueChunk(charBuffer, num3, num2 - num3);
				}
				if (num4 == 0)
				{
					break;
				}
				num3 += num4;
			}
			if (num3 >= charBlock)
			{
				_trailCharCount = num3 % charBlock;
				if (_trailCharCount > 0)
				{
					if (_trailChars == null)
					{
						_trailChars = new char[4];
					}
					num3 -= _trailCharCount;
					Array.Copy(charBuffer, num3, _trailChars, 0, _trailCharCount);
				}
			}
			try
			{
				if (byteCount < byteBlock)
				{
					if (_trailBytes == null)
					{
						_trailBytes = new byte[3];
					}
					_trailByteCount = encoding.GetBytes(charBuffer, 0, num3, _trailBytes, 0);
					int num5 = Math.Min(_trailByteCount, byteCount);
					Buffer.BlockCopy(_trailBytes, 0, buffer, offset, num5);
					_trailByteCount -= num5;
					Buffer.BlockCopy(_trailBytes, num5, _trailBytes, 0, _trailByteCount);
					return num5;
				}
				return encoding.GetBytes(charBuffer, 0, num3, buffer, offset);
			}
			catch (FormatException ex)
			{
				int num6 = 0;
				int num7 = 0;
				while (true)
				{
					if (num7 < num3 && XmlConverter.IsWhitespace(charBuffer[num7]))
					{
						num7++;
						continue;
					}
					if (num7 == num3)
					{
						break;
					}
					charBuffer[num6++] = charBuffer[num7++];
				}
				if (num6 == num3)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(ex.Message, ex.InnerException));
				}
				num3 = num6;
			}
		}
	}

	public override string ReadContentAsString()
	{
		XmlNode node = Node;
		if (node.IsAtomicValue)
		{
			string text;
			if (_value != null)
			{
				text = _value;
				if (node.AttributeText == null)
				{
					_value = string.Empty;
				}
			}
			else
			{
				text = node.Value.GetString();
				SkipValue(node);
				if (text.Length > _quotas.MaxStringContentLength)
				{
					XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, _quotas.MaxStringContentLength);
				}
			}
			return text;
		}
		return ReadContentAsString(_quotas.MaxStringContentLength);
	}

	public override bool ReadContentAsBoolean()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			bool result = node.Value.ToBoolean();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToBoolean(ReadContentAsString());
	}

	public override long ReadContentAsLong()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			long result = node.Value.ToLong();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToInt64(ReadContentAsString());
	}

	public override int ReadContentAsInt()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			int result = node.Value.ToInt();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToInt32(ReadContentAsString());
	}

	public override DateTime ReadContentAsDateTime()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			DateTime result = node.Value.ToDateTime();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToDateTime(ReadContentAsString());
	}

	public override double ReadContentAsDouble()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			double result = node.Value.ToDouble();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToDouble(ReadContentAsString());
	}

	public override float ReadContentAsFloat()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			float result = node.Value.ToSingle();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToSingle(ReadContentAsString());
	}

	public override decimal ReadContentAsDecimal()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			decimal result = node.Value.ToDecimal();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToDecimal(ReadContentAsString());
	}

	public override UniqueId ReadContentAsUniqueId()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			UniqueId result = node.Value.ToUniqueId();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToUniqueId(ReadContentAsString());
	}

	public override TimeSpan ReadContentAsTimeSpan()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			TimeSpan result = node.Value.ToTimeSpan();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToTimeSpan(ReadContentAsString());
	}

	public override Guid ReadContentAsGuid()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			Guid result = node.Value.ToGuid();
			SkipValue(node);
			return result;
		}
		return XmlConverter.ToGuid(ReadContentAsString());
	}

	public override object ReadContentAsObject()
	{
		XmlNode node = Node;
		if (_value == null && node.IsAtomicValue)
		{
			object result = node.Value.ToObject();
			SkipValue(node);
			return result;
		}
		return ReadContentAsString();
	}

	public override object ReadContentAs(Type type, IXmlNamespaceResolver namespaceResolver)
	{
		if (type == typeof(ulong))
		{
			if (_value == null && _node.IsAtomicValue)
			{
				ulong num = _node.Value.ToULong();
				SkipValue(_node);
				return num;
			}
			return XmlConverter.ToUInt64(ReadContentAsString());
		}
		if (type == typeof(bool))
		{
			return ReadContentAsBoolean();
		}
		if (type == typeof(int))
		{
			return ReadContentAsInt();
		}
		if (type == typeof(long))
		{
			return ReadContentAsLong();
		}
		if (type == typeof(float))
		{
			return ReadContentAsFloat();
		}
		if (type == typeof(double))
		{
			return ReadContentAsDouble();
		}
		if (type == typeof(decimal))
		{
			return ReadContentAsDecimal();
		}
		if (type == typeof(DateTime))
		{
			return ReadContentAsDateTime();
		}
		if (type == typeof(UniqueId))
		{
			return ReadContentAsUniqueId();
		}
		if (type == typeof(Guid))
		{
			return ReadContentAsGuid();
		}
		if (type == typeof(TimeSpan))
		{
			return ReadContentAsTimeSpan();
		}
		if (type == typeof(object))
		{
			return ReadContentAsObject();
		}
		return base.ReadContentAs(type, namespaceResolver);
	}

	public override void ResolveEntity()
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlInvalidOperation));
	}

	public override void Skip()
	{
		if (_node.ReadState != ReadState.Interactive)
		{
			return;
		}
		if ((_node.NodeType == XmlNodeType.Element || MoveToElement()) && !IsEmptyElement)
		{
			int depth = Depth;
			while (Read() && depth < Depth)
			{
			}
			if (_node.NodeType == XmlNodeType.EndElement)
			{
				Read();
			}
		}
		else
		{
			Read();
		}
	}

	public override bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString localName)
	{
		return _node.TryGetLocalNameAsDictionaryString(out localName);
	}

	public override bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString localName)
	{
		return _node.TryGetNamespaceUriAsDictionaryString(out localName);
	}

	public override bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString value)
	{
		return _node.TryGetValueAsDictionaryString(out value);
	}

	public override short[] ReadInt16Array(string localName, string namespaceUri)
	{
		return Int16ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override short[] ReadInt16Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return Int16ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override int[] ReadInt32Array(string localName, string namespaceUri)
	{
		return Int32ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override int[] ReadInt32Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return Int32ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override long[] ReadInt64Array(string localName, string namespaceUri)
	{
		return Int64ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override long[] ReadInt64Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return Int64ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override float[] ReadSingleArray(string localName, string namespaceUri)
	{
		return SingleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override float[] ReadSingleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return SingleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override double[] ReadDoubleArray(string localName, string namespaceUri)
	{
		return DoubleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override double[] ReadDoubleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return DoubleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override decimal[] ReadDecimalArray(string localName, string namespaceUri)
	{
		return DecimalArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override decimal[] ReadDecimalArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return DecimalArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override DateTime[] ReadDateTimeArray(string localName, string namespaceUri)
	{
		return DateTimeArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override DateTime[] ReadDateTimeArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return DateTimeArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override Guid[] ReadGuidArray(string localName, string namespaceUri)
	{
		return GuidArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override Guid[] ReadGuidArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return GuidArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override TimeSpan[] ReadTimeSpanArray(string localName, string namespaceUri)
	{
		return TimeSpanArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public override TimeSpan[] ReadTimeSpanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		return TimeSpanArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, _quotas.MaxArrayLength);
	}

	public string GetOpenElements()
	{
		string text = string.Empty;
		for (int num = _depth; num > 0; num--)
		{
			string @string = _elementNodes[num].LocalName.GetString();
			if (num != _depth)
			{
				text += ", ";
			}
			text += @string;
		}
		return text;
	}

	private char[] GetCharBuffer(int count)
	{
		if (count > 1024)
		{
			return new char[count];
		}
		if (_chars == null || _chars.Length < count)
		{
			_chars = new char[count];
		}
		return _chars;
	}

	private void SignStartElement(XmlSigningNodeWriter writer)
	{
		int offset;
		int length;
		byte[] @string = _node.Prefix.GetString(out offset, out length);
		int offset2;
		int length2;
		byte[] string2 = _node.LocalName.GetString(out offset2, out length2);
		writer.WriteStartElement(@string, offset, length, string2, offset2, length2);
	}

	private void SignAttribute(XmlSigningNodeWriter writer, XmlAttributeNode attributeNode)
	{
		if (attributeNode.QNameType == QNameType.Normal)
		{
			int offset;
			int length;
			byte[] @string = attributeNode.Prefix.GetString(out offset, out length);
			int offset2;
			int length2;
			byte[] string2 = attributeNode.LocalName.GetString(out offset2, out length2);
			writer.WriteStartAttribute(@string, offset, length, string2, offset2, length2);
			attributeNode.Value.Sign(writer);
			writer.WriteEndAttribute();
		}
		else
		{
			int offset3;
			int length3;
			byte[] string3 = attributeNode.Namespace.Prefix.GetString(out offset3, out length3);
			int offset4;
			int length4;
			byte[] string4 = attributeNode.Namespace.Uri.GetString(out offset4, out length4);
			writer.WriteXmlnsAttribute(string3, offset3, length3, string4, offset4, length4);
		}
	}

	private void SignEndElement(XmlSigningNodeWriter writer)
	{
		int offset;
		int length;
		byte[] @string = _node.Prefix.GetString(out offset, out length);
		int offset2;
		int length2;
		byte[] string2 = _node.LocalName.GetString(out offset2, out length2);
		writer.WriteEndElement(@string, offset, length, string2, offset2, length2);
	}

	private void SignNode(XmlSigningNodeWriter writer)
	{
		switch (_node.NodeType)
		{
		case XmlNodeType.Element:
		{
			SignStartElement(writer);
			for (int i = 0; i < _attributeCount; i++)
			{
				SignAttribute(writer, _attributeNodes[i]);
			}
			writer.WriteEndStartElement(_node.IsEmptyElement);
			break;
		}
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			_node.Value.Sign(writer);
			break;
		case XmlNodeType.XmlDeclaration:
			writer.WriteDeclaration();
			break;
		case XmlNodeType.Comment:
			writer.WriteComment(_node.Value.GetString());
			break;
		case XmlNodeType.EndElement:
			SignEndElement(writer);
			break;
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
		case XmlNodeType.None:
			break;
		}
	}

	protected void SignNode()
	{
		if (_signing)
		{
			SignNode(_signingWriter);
		}
	}

	public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
	{
		if (_signing)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlCanonicalizationStarted));
		}
		if (_signingWriter == null)
		{
			_signingWriter = CreateSigningNodeWriter();
		}
		_signingWriter.SetOutput(XmlNodeWriter.Null, stream, includeComments, inclusivePrefixes);
		_nsMgr.Sign(_signingWriter);
		_signing = true;
	}

	public override void EndCanonicalization()
	{
		if (!_signing)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlCanonicalizationNotStarted));
		}
		_signingWriter.Flush();
		_signingWriter.Close();
		_signing = false;
	}

	protected abstract XmlSigningNodeWriter CreateSigningNodeWriter();
}
