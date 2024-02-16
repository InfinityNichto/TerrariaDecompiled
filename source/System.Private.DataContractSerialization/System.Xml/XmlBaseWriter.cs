using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal abstract class XmlBaseWriter : XmlDictionaryWriter
{
	private sealed class Element
	{
		private string _prefix;

		private string _localName;

		private int _prefixId;

		public string Prefix
		{
			get
			{
				return _prefix;
			}
			set
			{
				_prefix = value;
			}
		}

		public string LocalName
		{
			get
			{
				return _localName;
			}
			set
			{
				_localName = value;
			}
		}

		public int PrefixId
		{
			get
			{
				return _prefixId;
			}
			set
			{
				_prefixId = value;
			}
		}

		public void Clear()
		{
			_prefix = null;
			_localName = null;
			_prefixId = 0;
		}
	}

	private enum DocumentState : byte
	{
		None,
		Document,
		Epilog,
		End
	}

	private sealed class NamespaceManager
	{
		private sealed class XmlAttribute
		{
			private XmlSpace _space;

			private string _lang;

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

			public void Clear()
			{
				_lang = null;
			}
		}

		private sealed class Namespace
		{
			private string _prefix;

			private string _ns;

			private XmlDictionaryString _xNs;

			private int _depth;

			private char _prefixChar;

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

			public char PrefixChar => _prefixChar;

			public string Prefix
			{
				get
				{
					return _prefix;
				}
				[param: DisallowNull]
				set
				{
					if (value.Length == 1)
					{
						_prefixChar = value[0];
					}
					else
					{
						_prefixChar = '\0';
					}
					_prefix = value;
				}
			}

			public string Uri
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

			public XmlDictionaryString UriDictionaryString
			{
				get
				{
					return _xNs;
				}
				set
				{
					_xNs = value;
				}
			}

			public void Clear()
			{
				_prefix = null;
				_prefixChar = '\0';
				_ns = null;
				_xNs = null;
				_depth = 0;
			}
		}

		private Namespace[] _namespaces;

		private Namespace _lastNameSpace;

		private int _nsCount;

		private int _depth;

		private XmlAttribute[] _attributes;

		private int _attributeCount;

		private XmlSpace _space;

		private string _lang;

		private int _nsTop;

		private readonly Namespace _defaultNamespace;

		public string XmlLang => _lang;

		public XmlSpace XmlSpace => _space;

		public NamespaceManager()
		{
			_defaultNamespace = new Namespace();
			_defaultNamespace.Depth = 0;
			_defaultNamespace.Prefix = string.Empty;
			_defaultNamespace.Uri = string.Empty;
			_defaultNamespace.UriDictionaryString = null;
		}

		public void Clear()
		{
			if (_namespaces == null)
			{
				_namespaces = new Namespace[4];
				_namespaces[0] = _defaultNamespace;
			}
			_nsCount = 1;
			_nsTop = 0;
			_depth = 0;
			_attributeCount = 0;
			_space = XmlSpace.None;
			_lang = null;
			_lastNameSpace = null;
		}

		public void Close()
		{
			if (_depth == 0)
			{
				if (_namespaces != null && _namespaces.Length > 32)
				{
					_namespaces = null;
				}
				if (_attributes != null && _attributes.Length > 4)
				{
					_attributes = null;
				}
			}
			else
			{
				_namespaces = null;
				_attributes = null;
			}
			_lang = null;
		}

		public void DeclareNamespaces(XmlNodeWriter writer)
		{
			int i;
			for (i = _nsCount; i > 0; i--)
			{
				Namespace @namespace = _namespaces[i - 1];
				if (@namespace.Depth != _depth)
				{
					break;
				}
			}
			for (; i < _nsCount; i++)
			{
				Namespace namespace2 = _namespaces[i];
				if (namespace2.UriDictionaryString != null)
				{
					writer.WriteXmlnsAttribute(namespace2.Prefix, namespace2.UriDictionaryString);
				}
				else
				{
					writer.WriteXmlnsAttribute(namespace2.Prefix, namespace2.Uri);
				}
			}
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
				if (_lastNameSpace == @namespace)
				{
					_lastNameSpace = null;
				}
				@namespace.Clear();
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
				xmlAttribute.Clear();
				_attributeCount--;
			}
			_depth--;
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

		public string AddNamespace(string uri, XmlDictionaryString uriDictionaryString)
		{
			if (uri.Length == 0)
			{
				AddNamespaceIfNotDeclared(string.Empty, uri, uriDictionaryString);
				return string.Empty;
			}
			for (int i = 0; i < s_prefixes.Length; i++)
			{
				string text = s_prefixes[i];
				bool flag = false;
				for (int num = _nsCount - 1; num >= _nsTop; num--)
				{
					Namespace @namespace = _namespaces[num];
					if (@namespace.Prefix == text)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					AddNamespace(text, uri, uriDictionaryString);
					return text;
				}
			}
			return null;
		}

		public void AddNamespaceIfNotDeclared(string prefix, string uri, XmlDictionaryString uriDictionaryString)
		{
			if (LookupNamespace(prefix) != uri)
			{
				AddNamespace(prefix, uri, uriDictionaryString);
			}
		}

		public void AddNamespace(string prefix, string uri, XmlDictionaryString uriDictionaryString)
		{
			if (prefix.Length >= 3 && (prefix[0] & -33) == 88 && (prefix[1] & -33) == 77 && (prefix[2] & -33) == 76)
			{
				if ((!(prefix == "xml") || !(uri == "http://www.w3.org/XML/1998/namespace")) && (!(prefix == "xmlns") || !(uri == "http://www.w3.org/2000/xmlns/")))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlReservedPrefix, "prefix"));
				}
				return;
			}
			Namespace @namespace;
			for (int num = _nsCount - 1; num >= 0; num--)
			{
				@namespace = _namespaces[num];
				if (@namespace.Depth != _depth)
				{
					break;
				}
				if (@namespace.Prefix == prefix)
				{
					if (@namespace.Uri == uri)
					{
						return;
					}
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlPrefixBoundToNamespace, prefix, @namespace.Uri, uri), "prefix"));
				}
			}
			if (prefix.Length != 0 && uri.Length == 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlEmptyNamespaceRequiresNullPrefix, "prefix"));
			}
			if (uri.Length == "http://www.w3.org/2000/xmlns/".Length && uri == "http://www.w3.org/2000/xmlns/")
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlSpecificBindingNamespace, "xmlns", uri)));
			}
			if (uri.Length == "http://www.w3.org/XML/1998/namespace".Length && uri[18] == 'X' && uri == "http://www.w3.org/XML/1998/namespace")
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlSpecificBindingNamespace, "xml", uri)));
			}
			if (_namespaces.Length == _nsCount)
			{
				Namespace[] array = new Namespace[_nsCount * 2];
				Array.Copy(_namespaces, array, _nsCount);
				_namespaces = array;
			}
			@namespace = _namespaces[_nsCount];
			if (@namespace == null)
			{
				@namespace = new Namespace();
				_namespaces[_nsCount] = @namespace;
			}
			@namespace.Depth = _depth;
			@namespace.Prefix = prefix;
			@namespace.Uri = uri;
			@namespace.UriDictionaryString = uriDictionaryString;
			_nsCount++;
			_lastNameSpace = null;
		}

		public string LookupPrefix(string ns)
		{
			if (_lastNameSpace != null && _lastNameSpace.Uri == ns)
			{
				return _lastNameSpace.Prefix;
			}
			int nsCount = _nsCount;
			for (int num = nsCount - 1; num >= _nsTop; num--)
			{
				Namespace @namespace = _namespaces[num];
				if ((object)@namespace.Uri == ns)
				{
					string prefix = @namespace.Prefix;
					bool flag = false;
					for (int i = num + 1; i < nsCount; i++)
					{
						if (_namespaces[i].Prefix == prefix)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						_lastNameSpace = @namespace;
						return prefix;
					}
				}
			}
			for (int num2 = nsCount - 1; num2 >= _nsTop; num2--)
			{
				Namespace namespace2 = _namespaces[num2];
				if (namespace2.Uri == ns)
				{
					string prefix2 = namespace2.Prefix;
					bool flag2 = false;
					for (int j = num2 + 1; j < nsCount; j++)
					{
						if (_namespaces[j].Prefix == prefix2)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						_lastNameSpace = namespace2;
						return prefix2;
					}
				}
			}
			if (ns.Length == 0)
			{
				bool flag3 = true;
				for (int num3 = nsCount - 1; num3 >= _nsTop; num3--)
				{
					if (_namespaces[num3].Prefix.Length == 0)
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					return string.Empty;
				}
			}
			if (ns == "http://www.w3.org/2000/xmlns/")
			{
				return "xmlns";
			}
			if (ns == "http://www.w3.org/XML/1998/namespace")
			{
				return "xml";
			}
			return null;
		}

		public string LookupAttributePrefix(string ns)
		{
			if (_lastNameSpace != null && _lastNameSpace.Uri == ns && _lastNameSpace.Prefix.Length != 0)
			{
				return _lastNameSpace.Prefix;
			}
			int nsCount = _nsCount;
			for (int num = nsCount - 1; num >= _nsTop; num--)
			{
				Namespace @namespace = _namespaces[num];
				if ((object)@namespace.Uri == ns)
				{
					string prefix = @namespace.Prefix;
					if (prefix.Length != 0)
					{
						bool flag = false;
						for (int i = num + 1; i < nsCount; i++)
						{
							if (_namespaces[i].Prefix == prefix)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							_lastNameSpace = @namespace;
							return prefix;
						}
					}
				}
			}
			for (int num2 = nsCount - 1; num2 >= _nsTop; num2--)
			{
				Namespace namespace2 = _namespaces[num2];
				if (namespace2.Uri == ns)
				{
					string prefix2 = namespace2.Prefix;
					if (prefix2.Length != 0)
					{
						bool flag2 = false;
						for (int j = num2 + 1; j < nsCount; j++)
						{
							if (_namespaces[j].Prefix == prefix2)
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							_lastNameSpace = namespace2;
							return prefix2;
						}
					}
				}
			}
			if (ns.Length == 0)
			{
				return string.Empty;
			}
			return null;
		}

		public string LookupNamespace(string prefix)
		{
			int nsCount = _nsCount;
			if (prefix.Length == 0)
			{
				for (int num = nsCount - 1; num >= _nsTop; num--)
				{
					Namespace @namespace = _namespaces[num];
					if (@namespace.Prefix.Length == 0)
					{
						return @namespace.Uri;
					}
				}
				return string.Empty;
			}
			if (prefix.Length == 1)
			{
				char c = prefix[0];
				for (int num2 = nsCount - 1; num2 >= _nsTop; num2--)
				{
					Namespace namespace2 = _namespaces[num2];
					if (namespace2.PrefixChar == c)
					{
						return namespace2.Uri;
					}
				}
				return null;
			}
			for (int num3 = nsCount - 1; num3 >= _nsTop; num3--)
			{
				Namespace namespace3 = _namespaces[num3];
				if (namespace3.Prefix == prefix)
				{
					return namespace3.Uri;
				}
			}
			if (prefix == "xmlns")
			{
				return "http://www.w3.org/2000/xmlns/";
			}
			if (prefix == "xml")
			{
				return "http://www.w3.org/XML/1998/namespace";
			}
			return null;
		}

		public void Sign(XmlCanonicalWriter signingWriter)
		{
			int nsCount = _nsCount;
			for (int i = 1; i < nsCount; i++)
			{
				Namespace @namespace = _namespaces[i];
				bool flag = false;
				for (int j = i + 1; j < nsCount; j++)
				{
					if (flag)
					{
						break;
					}
					flag = @namespace.Prefix == _namespaces[j].Prefix;
				}
				if (!flag)
				{
					signingWriter.WriteXmlnsAttribute(@namespace.Prefix, @namespace.Uri);
				}
			}
		}
	}

	private XmlNodeWriter _writer;

	private readonly NamespaceManager _nsMgr;

	private Element[] _elements;

	private int _depth;

	private string _attributeLocalName;

	private string _attributeValue;

	private bool _isXmlAttribute;

	private bool _isXmlnsAttribute;

	private WriteState _writeState;

	private DocumentState _documentState;

	private byte[] _trailBytes;

	private int _trailByteCount;

	private XmlStreamNodeWriter _nodeWriter;

	private XmlSigningNodeWriter _signingWriter;

	private bool _inList;

	private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

	private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";

	private static BinHexEncoding _binhexEncoding;

	private static readonly string[] s_prefixes = new string[26]
	{
		"a", "b", "c", "d", "e", "f", "g", "h", "i", "j",
		"k", "l", "m", "n", "o", "p", "q", "r", "s", "t",
		"u", "v", "w", "x", "y", "z"
	};

	protected bool IsClosed => _writeState == WriteState.Closed;

	private static BinHexEncoding BinHexEncoding
	{
		get
		{
			if (_binhexEncoding == null)
			{
				_binhexEncoding = new BinHexEncoding();
			}
			return _binhexEncoding;
		}
	}

	public override string XmlLang => _nsMgr.XmlLang;

	public override XmlSpace XmlSpace => _nsMgr.XmlSpace;

	public override WriteState WriteState => _writeState;

	public override bool CanCanonicalize => true;

	protected bool Signing => _writer == _signingWriter;

	protected XmlBaseWriter()
	{
		_nsMgr = new NamespaceManager();
		_writeState = WriteState.Start;
		_documentState = DocumentState.None;
	}

	protected void SetOutput(XmlStreamNodeWriter writer)
	{
		_inList = false;
		_writer = writer;
		_nodeWriter = writer;
		_writeState = WriteState.Start;
		_documentState = DocumentState.None;
		_nsMgr.Clear();
		if (_depth != 0)
		{
			_elements = null;
			_depth = 0;
		}
		_attributeLocalName = null;
		_attributeValue = null;
	}

	public override void Flush()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		_writer.Flush();
	}

	public override Task FlushAsync()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		return _writer.FlushAsync();
	}

	public override void Close()
	{
		if (IsClosed)
		{
			return;
		}
		try
		{
			FinishDocument();
			AutoComplete(WriteState.Closed);
			_writer.Flush();
		}
		finally
		{
			_nsMgr.Close();
			if (_depth != 0)
			{
				_elements = null;
				_depth = 0;
			}
			_attributeValue = null;
			_attributeLocalName = null;
			_nodeWriter.Close();
			if (_signingWriter != null)
			{
				_signingWriter.Close();
			}
		}
	}

	[DoesNotReturn]
	protected void ThrowClosed()
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlWriterClosed));
	}

	public override void WriteXmlnsAttribute(string prefix, string ns)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (ns == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("ns");
		}
		if (_writeState != WriteState.Element)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString())));
		}
		if (prefix == null)
		{
			prefix = _nsMgr.LookupPrefix(ns);
			if (prefix == null)
			{
				GeneratePrefix(ns, null);
			}
		}
		else
		{
			_nsMgr.AddNamespaceIfNotDeclared(prefix, ns, null);
		}
	}

	public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (ns == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("ns");
		}
		if (_writeState != WriteState.Element)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteXmlnsAttribute", WriteState.ToString())));
		}
		if (prefix == null)
		{
			prefix = _nsMgr.LookupPrefix(ns.Value);
			if (prefix == null)
			{
				GeneratePrefix(ns.Value, ns);
			}
		}
		else
		{
			_nsMgr.AddNamespaceIfNotDeclared(prefix, ns.Value, ns);
		}
	}

	private void StartAttribute([AllowNull] ref string prefix, string localName, string ns, XmlDictionaryString xNs)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState == WriteState.Attribute)
		{
			WriteEndAttribute();
		}
		if (localName == null || (localName.Length == 0 && prefix != "xmlns"))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (_writeState != WriteState.Element)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteStartAttribute", WriteState.ToString())));
		}
		if (prefix == null)
		{
			if (ns == "http://www.w3.org/2000/xmlns/" && localName != "xmlns")
			{
				prefix = "xmlns";
			}
			else if (ns == "http://www.w3.org/XML/1998/namespace")
			{
				prefix = "xml";
			}
			else
			{
				prefix = string.Empty;
			}
		}
		if (prefix.Length == 0 && localName == "xmlns")
		{
			prefix = "xmlns";
			localName = string.Empty;
		}
		_isXmlnsAttribute = false;
		_isXmlAttribute = false;
		if (prefix == "xml")
		{
			if (ns != null && ns != "http://www.w3.org/XML/1998/namespace")
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlPrefixBoundToNamespace, "xml", "http://www.w3.org/XML/1998/namespace", ns), "ns"));
			}
			_isXmlAttribute = true;
			_attributeValue = string.Empty;
			_attributeLocalName = localName;
		}
		else if (prefix == "xmlns")
		{
			if (ns != null && ns != "http://www.w3.org/2000/xmlns/")
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlPrefixBoundToNamespace, "xmlns", "http://www.w3.org/2000/xmlns/", ns), "ns"));
			}
			_isXmlnsAttribute = true;
			_attributeValue = string.Empty;
			_attributeLocalName = localName;
		}
		else if (ns == null)
		{
			if (prefix.Length == 0)
			{
				ns = string.Empty;
			}
			else
			{
				ns = _nsMgr.LookupNamespace(prefix);
				if (ns == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlUndefinedPrefix, prefix), "prefix"));
				}
			}
		}
		else if (ns.Length == 0)
		{
			if (prefix.Length != 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlEmptyNamespaceRequiresNullPrefix, "prefix"));
			}
		}
		else if (prefix.Length == 0)
		{
			string text = _nsMgr.LookupAttributePrefix(ns);
			if (text == null)
			{
				if (ns.Length == "http://www.w3.org/2000/xmlns/".Length && ns == "http://www.w3.org/2000/xmlns/")
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlSpecificBindingNamespace, "xmlns", ns)));
				}
				if (ns.Length == "http://www.w3.org/XML/1998/namespace".Length && ns == "http://www.w3.org/XML/1998/namespace")
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlSpecificBindingNamespace, "xml", ns)));
				}
				text = GeneratePrefix(ns, xNs);
			}
			prefix = text;
		}
		else
		{
			_nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
		}
		_writeState = WriteState.Attribute;
	}

	public override void WriteStartAttribute(string prefix, string localName, string namespaceUri)
	{
		StartAttribute(ref prefix, localName, namespaceUri, null);
		if (!_isXmlnsAttribute)
		{
			_writer.WriteStartAttribute(prefix, localName);
		}
	}

	public override void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		StartAttribute(ref prefix, localName?.Value, namespaceUri?.Value, namespaceUri);
		if (!_isXmlnsAttribute)
		{
			_writer.WriteStartAttribute(prefix, localName);
		}
	}

	public override void WriteEndAttribute()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState != WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString())));
		}
		FlushBase64();
		try
		{
			if (_isXmlAttribute)
			{
				if (_attributeLocalName == "lang")
				{
					_nsMgr.AddLangAttribute(_attributeValue);
				}
				else if (_attributeLocalName == "space")
				{
					if (_attributeValue == "preserve")
					{
						_nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
					}
					else
					{
						if (!(_attributeValue == "default"))
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlInvalidXmlSpace, _attributeValue)));
						}
						_nsMgr.AddSpaceAttribute(XmlSpace.Default);
					}
				}
				_isXmlAttribute = false;
				_attributeLocalName = null;
				_attributeValue = null;
			}
			if (_isXmlnsAttribute)
			{
				_nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName, _attributeValue, null);
				_isXmlnsAttribute = false;
				_attributeLocalName = null;
				_attributeValue = null;
			}
			else
			{
				_writer.WriteEndAttribute();
			}
		}
		finally
		{
			_writeState = WriteState.Element;
		}
	}

	protected override Task WriteEndAttributeAsync()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState != WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteEndAttribute", WriteState.ToString())));
		}
		return WriteEndAttributeAsyncImpl();
	}

	private async Task WriteEndAttributeAsyncImpl()
	{
		await FlushBase64Async().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_isXmlAttribute)
			{
				if (_attributeLocalName == "lang")
				{
					_nsMgr.AddLangAttribute(_attributeValue);
				}
				else if (_attributeLocalName == "space")
				{
					if (_attributeValue == "preserve")
					{
						_nsMgr.AddSpaceAttribute(XmlSpace.Preserve);
					}
					else
					{
						if (!(_attributeValue == "default"))
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlInvalidXmlSpace, _attributeValue)));
						}
						_nsMgr.AddSpaceAttribute(XmlSpace.Default);
					}
				}
				_isXmlAttribute = false;
				_attributeLocalName = null;
				_attributeValue = null;
			}
			if (_isXmlnsAttribute)
			{
				_nsMgr.AddNamespaceIfNotDeclared(_attributeLocalName, _attributeValue, null);
				_isXmlnsAttribute = false;
				_attributeLocalName = null;
				_attributeValue = null;
			}
			else
			{
				await _writer.WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			_writeState = WriteState.Element;
		}
	}

	public override void WriteComment(string text)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState == WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteComment", WriteState.ToString())));
		}
		if (text == null)
		{
			text = string.Empty;
		}
		else if (text.IndexOf("--", StringComparison.Ordinal) != -1 || (text.Length > 0 && text[text.Length - 1] == '-'))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlInvalidCommentChars, "text"));
		}
		StartComment();
		FlushBase64();
		_writer.WriteComment(text);
		EndComment();
	}

	public override void WriteFullEndElement()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState == WriteState.Attribute)
		{
			WriteEndAttribute();
		}
		if (_writeState != WriteState.Element && _writeState != WriteState.Content)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteFullEndElement", WriteState.ToString())));
		}
		AutoComplete(WriteState.Content);
		WriteEndElement();
	}

	public override void WriteCData(string text)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState == WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteCData", WriteState.ToString())));
		}
		if (text == null)
		{
			text = string.Empty;
		}
		if (text.Length > 0)
		{
			StartContent();
			FlushBase64();
			_writer.WriteCData(text);
			EndContent();
		}
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(System.SR.Format(System.SR.XmlMethodNotSupported, "WriteDocType")));
	}

	private void StartElement(ref string prefix, string localName, string ns, XmlDictionaryString xNs)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_documentState == DocumentState.Epilog)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlOnlyOneRoot));
		}
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (localName.Length == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.InvalidLocalNameEmpty, "localName"));
		}
		if (_writeState == WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));
		}
		FlushBase64();
		AutoComplete(WriteState.Element);
		Element element = EnterScope();
		if (ns == null)
		{
			if (prefix == null)
			{
				prefix = string.Empty;
			}
			ns = _nsMgr.LookupNamespace(prefix);
			if (ns == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlUndefinedPrefix, prefix), "prefix"));
			}
		}
		else if (prefix == null)
		{
			prefix = _nsMgr.LookupPrefix(ns);
			if (prefix == null)
			{
				prefix = string.Empty;
				_nsMgr.AddNamespace(string.Empty, ns, xNs);
			}
		}
		else
		{
			_nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
		}
		element.Prefix = prefix;
		element.LocalName = localName;
	}

	private void PreStartElementAsyncCheck(string prefix, string localName, string ns, XmlDictionaryString xNs)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_documentState == DocumentState.Epilog)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlOnlyOneRoot));
		}
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (localName.Length == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.InvalidLocalNameEmpty, "localName"));
		}
		if (_writeState == WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));
		}
	}

	private async Task StartElementAndWriteStartElementAsync(string prefix, string localName, string namespaceUri)
	{
		prefix = await StartElementAsync(prefix, localName, namespaceUri, null).ConfigureAwait(continueOnCapturedContext: false);
		await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<string> StartElementAsync(string prefix, string localName, string ns, XmlDictionaryString xNs)
	{
		await FlushBase64Async().ConfigureAwait(continueOnCapturedContext: false);
		await AutoCompleteAsync(WriteState.Element).ConfigureAwait(continueOnCapturedContext: false);
		Element element = EnterScope();
		if (ns == null)
		{
			if (prefix == null)
			{
				prefix = string.Empty;
			}
			ns = _nsMgr.LookupNamespace(prefix);
			if (ns == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlUndefinedPrefix, prefix), "prefix"));
			}
		}
		else if (prefix == null)
		{
			prefix = _nsMgr.LookupPrefix(ns);
			if (prefix == null)
			{
				prefix = string.Empty;
				_nsMgr.AddNamespace(string.Empty, ns, xNs);
			}
		}
		else
		{
			_nsMgr.AddNamespaceIfNotDeclared(prefix, ns, xNs);
		}
		element.Prefix = prefix;
		element.LocalName = localName;
		return prefix;
	}

	public override void WriteStartElement(string prefix, string localName, string namespaceUri)
	{
		StartElement(ref prefix, localName, namespaceUri, null);
		_writer.WriteStartElement(prefix, localName);
	}

	public override Task WriteStartElementAsync(string prefix, string localName, string namespaceUri)
	{
		PreStartElementAsyncCheck(prefix, localName, namespaceUri, null);
		return StartElementAndWriteStartElementAsync(prefix, localName, namespaceUri);
	}

	public override void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		StartElement(ref prefix, localName.Value, namespaceUri?.Value, namespaceUri);
		_writer.WriteStartElement(prefix, localName);
	}

	public override void WriteEndElement()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_depth == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture))));
		}
		if (_writeState == WriteState.Attribute)
		{
			WriteEndAttribute();
		}
		FlushBase64();
		if (_writeState == WriteState.Element)
		{
			_nsMgr.DeclareNamespaces(_writer);
			_writer.WriteEndStartElement(isEmpty: true);
		}
		else
		{
			Element element = _elements[_depth];
			_writer.WriteEndElement(element.Prefix, element.LocalName);
		}
		ExitScope();
		_writeState = WriteState.Content;
	}

	public override Task WriteEndElementAsync()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_depth == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDepth, "WriteEndElement", _depth.ToString(CultureInfo.InvariantCulture))));
		}
		return WriteEndElementAsyncImpl();
	}

	private async Task WriteEndElementAsyncImpl()
	{
		if (_writeState == WriteState.Attribute)
		{
			await WriteEndAttributeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		FlushBase64();
		if (_writeState == WriteState.Element)
		{
			_nsMgr.DeclareNamespaces(_writer);
			await _writer.WriteEndStartElementAsync(isEmpty: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			Element element = _elements[_depth];
			await _writer.WriteEndElementAsync(element.Prefix, element.LocalName).ConfigureAwait(continueOnCapturedContext: false);
		}
		ExitScope();
		_writeState = WriteState.Content;
	}

	private Element EnterScope()
	{
		_nsMgr.EnterScope();
		_depth++;
		if (_elements == null)
		{
			_elements = new Element[4];
		}
		else if (_elements.Length == _depth)
		{
			Element[] array = new Element[_depth * 2];
			Array.Copy(_elements, array, _depth);
			_elements = array;
		}
		Element element = _elements[_depth];
		if (element == null)
		{
			element = new Element();
			_elements[_depth] = element;
		}
		return element;
	}

	private void ExitScope()
	{
		_elements[_depth].Clear();
		_depth--;
		if (_depth == 0 && _documentState == DocumentState.Document)
		{
			_documentState = DocumentState.Epilog;
		}
		_nsMgr.ExitScope();
	}

	protected void FlushElement()
	{
		if (_writeState == WriteState.Element)
		{
			AutoComplete(WriteState.Content);
		}
	}

	private Task FlushElementAsync()
	{
		if (_writeState != WriteState.Element)
		{
			return Task.CompletedTask;
		}
		return AutoCompleteAsync(WriteState.Content);
	}

	protected void StartComment()
	{
		FlushElement();
	}

	protected void EndComment()
	{
	}

	protected void StartContent()
	{
		FlushElement();
		if (_depth == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlIllegalOutsideRoot));
		}
	}

	protected async Task StartContentAsync()
	{
		await FlushElementAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (_depth == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlIllegalOutsideRoot));
		}
	}

	protected void StartContent(char ch)
	{
		FlushElement();
		if (_depth == 0)
		{
			VerifyWhitespace(ch);
		}
	}

	protected void StartContent(string s)
	{
		FlushElement();
		if (_depth == 0)
		{
			VerifyWhitespace(s);
		}
	}

	protected void StartContent(char[] chars, int offset, int count)
	{
		FlushElement();
		if (_depth == 0)
		{
			VerifyWhitespace(chars, offset, count);
		}
	}

	private void VerifyWhitespace(char ch)
	{
		if (!IsWhitespace(ch))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlIllegalOutsideRoot));
		}
	}

	private void VerifyWhitespace(string s)
	{
		for (int i = 0; i < s.Length; i++)
		{
			if (!IsWhitespace(s[i]))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlIllegalOutsideRoot));
			}
		}
	}

	private void VerifyWhitespace(char[] chars, int offset, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!IsWhitespace(chars[offset + i]))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlIllegalOutsideRoot));
			}
		}
	}

	private bool IsWhitespace(char ch)
	{
		if (ch != ' ' && ch != '\n' && ch != '\r')
		{
			return ch == 't';
		}
		return true;
	}

	protected void EndContent()
	{
	}

	private void AutoComplete(WriteState writeState)
	{
		if (_writeState == WriteState.Element)
		{
			EndStartElement();
		}
		_writeState = writeState;
	}

	private async Task AutoCompleteAsync(WriteState writeState)
	{
		if (_writeState == WriteState.Element)
		{
			await EndStartElementAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_writeState = writeState;
	}

	private void EndStartElement()
	{
		_nsMgr.DeclareNamespaces(_writer);
		_writer.WriteEndStartElement(isEmpty: false);
	}

	private Task EndStartElementAsync()
	{
		_nsMgr.DeclareNamespaces(_writer);
		return _writer.WriteEndStartElementAsync(isEmpty: false);
	}

	public override string LookupPrefix(string ns)
	{
		if (ns == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("ns"));
		}
		if (IsClosed)
		{
			ThrowClosed();
		}
		return _nsMgr.LookupPrefix(ns);
	}

	private string GetQualifiedNamePrefix(string namespaceUri, XmlDictionaryString xNs)
	{
		string text = _nsMgr.LookupPrefix(namespaceUri);
		if (text == null)
		{
			if (_writeState != WriteState.Attribute)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlNamespaceNotFound, namespaceUri), "namespaceUri"));
			}
			text = GeneratePrefix(namespaceUri, xNs);
		}
		return text;
	}

	public override void WriteQualifiedName(string localName, string namespaceUri)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (localName.Length == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.InvalidLocalNameEmpty, "localName"));
		}
		if (namespaceUri == null)
		{
			namespaceUri = string.Empty;
		}
		string qualifiedNamePrefix = GetQualifiedNamePrefix(namespaceUri, null);
		if (qualifiedNamePrefix.Length != 0)
		{
			WriteString(qualifiedNamePrefix);
			WriteString(":");
		}
		WriteString(localName);
	}

	public override void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (localName == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
		}
		if (localName.Value.Length == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.InvalidLocalNameEmpty, "localName"));
		}
		if (namespaceUri == null)
		{
			namespaceUri = XmlDictionaryString.Empty;
		}
		string qualifiedNamePrefix = GetQualifiedNamePrefix(namespaceUri.Value, namespaceUri);
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(qualifiedNamePrefix + ":" + namespaceUri.Value);
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteQualifiedName(qualifiedNamePrefix, localName);
			EndContent();
		}
	}

	public override void WriteStartDocument()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteStartDocument", WriteState.ToString())));
		}
		_writeState = WriteState.Prolog;
		_documentState = DocumentState.Document;
		_writer.WriteDeclaration();
	}

	public override void WriteStartDocument(bool standalone)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		WriteStartDocument();
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (name != "xml")
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlProcessingInstructionNotSupported, "name"));
		}
		if (_writeState != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlInvalidDeclaration));
		}
		_writer.WriteDeclaration();
	}

	private void FinishDocument()
	{
		if (_writeState == WriteState.Attribute)
		{
			WriteEndAttribute();
		}
		while (_depth > 0)
		{
			WriteEndElement();
		}
	}

	public override void WriteEndDocument()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (_writeState == WriteState.Start || _writeState == WriteState.Prolog)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlNoRootElement));
		}
		FinishDocument();
		_writeState = WriteState.Start;
		_documentState = DocumentState.End;
	}

	public override void WriteEntityRef(string name)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(System.SR.Format(System.SR.XmlMethodNotSupported, "WriteEntityRef")));
	}

	public override void WriteName(string name)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		WriteString(name);
	}

	public override void WriteNmToken(string name)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(System.SR.Format(System.SR.XmlMethodNotSupported, "WriteNmToken")));
	}

	public override void WriteWhitespace(string whitespace)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (whitespace == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("whitespace");
		}
		foreach (char c in whitespace)
		{
			if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlOnlyWhitespace, "whitespace"));
			}
		}
		WriteString(whitespace);
	}

	public override void WriteString(string value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			value = string.Empty;
		}
		if (value.Length > 0 || _inList)
		{
			FlushBase64();
			if (_attributeValue != null)
			{
				WriteAttributeText(value);
			}
			if (!_isXmlnsAttribute)
			{
				StartContent(value);
				_writer.WriteEscapedText(value);
				EndContent();
			}
		}
	}

	public override void WriteString(XmlDictionaryString value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
		}
		if (value.Value.Length > 0)
		{
			FlushBase64();
			if (_attributeValue != null)
			{
				WriteAttributeText(value.Value);
			}
			if (!_isXmlnsAttribute)
			{
				StartContent(value.Value);
				_writer.WriteEscapedText(value);
				EndContent();
			}
		}
	}

	public override void WriteChars(char[] chars, int offset, int count)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > chars.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));
		}
		if (count > 0)
		{
			FlushBase64();
			if (_attributeValue != null)
			{
				WriteAttributeText(new string(chars, offset, count));
			}
			if (!_isXmlnsAttribute)
			{
				StartContent(chars, offset, count);
				_writer.WriteEscapedText(chars, offset, count);
				EndContent();
			}
		}
	}

	public override void WriteRaw(string value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			value = string.Empty;
		}
		if (value.Length > 0)
		{
			FlushBase64();
			if (_attributeValue != null)
			{
				WriteAttributeText(value);
			}
			if (!_isXmlnsAttribute)
			{
				StartContent(value);
				_writer.WriteText(value);
				EndContent();
			}
		}
	}

	public override void WriteRaw(char[] chars, int offset, int count)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > chars.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - offset)));
		}
		if (count > 0)
		{
			FlushBase64();
			if (_attributeValue != null)
			{
				WriteAttributeText(new string(chars, offset, count));
			}
			if (!_isXmlnsAttribute)
			{
				StartContent(chars, offset, count);
				_writer.WriteText(chars, offset, count);
				EndContent();
			}
		}
	}

	public override void WriteCharEntity(char ch)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (ch >= '\ud800' && ch <= '\udfff')
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlMissingLowSurrogate, "ch"));
		}
		if (_attributeValue != null)
		{
			WriteAttributeText(ch.ToString());
		}
		if (!_isXmlnsAttribute)
		{
			StartContent(ch);
			FlushBase64();
			_writer.WriteCharEntity(ch);
			EndContent();
		}
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		SurrogateChar surrogateChar = new SurrogateChar(lowChar, highChar);
		if (_attributeValue != null)
		{
			Span<char> span = stackalloc char[2] { highChar, lowChar };
			WriteAttributeText(new string(span));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			FlushBase64();
			_writer.WriteCharEntity(surrogateChar.Char);
			EndContent();
		}
	}

	public override void WriteValue(object value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
		}
		if (value is object[])
		{
			WriteValue((object[])value);
		}
		else if (value is Array)
		{
			WriteValue((Array)value);
		}
		else
		{
			WritePrimitiveValue(value);
		}
	}

	protected void WritePrimitiveValue(object value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
		}
		if (value is ulong)
		{
			WriteValue((ulong)value);
			return;
		}
		if (value is string)
		{
			WriteValue((string)value);
			return;
		}
		if (value is int)
		{
			WriteValue((int)value);
			return;
		}
		if (value is long)
		{
			WriteValue((long)value);
			return;
		}
		if (value is bool)
		{
			WriteValue((bool)value);
			return;
		}
		if (value is double)
		{
			WriteValue((double)value);
			return;
		}
		if (value is DateTime)
		{
			WriteValue((DateTime)value);
			return;
		}
		if (value is float)
		{
			WriteValue((float)value);
			return;
		}
		if (value is decimal)
		{
			WriteValue((decimal)value);
			return;
		}
		if (value is XmlDictionaryString)
		{
			WriteValue((XmlDictionaryString)value);
			return;
		}
		if (value is UniqueId)
		{
			WriteValue((UniqueId)value);
			return;
		}
		if (value is Guid)
		{
			WriteValue((Guid)value);
			return;
		}
		if (value is TimeSpan)
		{
			WriteValue((TimeSpan)value);
			return;
		}
		if (value.GetType().IsArray)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlNestedArraysNotSupported, "value"));
		}
		base.WriteValue(value);
	}

	public override void WriteValue(string value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		WriteString(value);
	}

	public override void WriteValue(int value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteInt32Text(value);
			EndContent();
		}
	}

	public override void WriteValue(long value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteInt64Text(value);
			EndContent();
		}
	}

	private void WriteValue(ulong value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteUInt64Text(value);
			EndContent();
		}
	}

	public override void WriteValue(bool value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteBoolText(value);
			EndContent();
		}
	}

	public override void WriteValue(decimal value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteDecimalText(value);
			EndContent();
		}
	}

	public override void WriteValue(float value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteFloatText(value);
			EndContent();
		}
	}

	public override void WriteValue(double value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteDoubleText(value);
			EndContent();
		}
	}

	public override void WriteValue(XmlDictionaryString value)
	{
		WriteString(value);
	}

	public override void WriteValue(DateTime value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteDateTimeText(value);
			EndContent();
		}
	}

	public override void WriteValue(UniqueId value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteUniqueIdText(value);
			EndContent();
		}
	}

	public override void WriteValue(Guid value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteGuidText(value);
			EndContent();
		}
	}

	public override void WriteValue(TimeSpan value)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		FlushBase64();
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.ToString(value));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteTimeSpanText(value);
			EndContent();
		}
	}

	public override void WriteBinHex(byte[] buffer, int offset, int count)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		WriteRaw(BinHexEncoding.GetString(buffer, offset, count));
	}

	public override void WriteBase64(byte[] buffer, int offset, int count)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		if (count <= 0)
		{
			return;
		}
		if (_trailByteCount > 0)
		{
			while (_trailByteCount < 3 && count > 0)
			{
				_trailBytes[_trailByteCount++] = buffer[offset++];
				count--;
			}
		}
		int num = _trailByteCount + count;
		int num2 = num - num % 3;
		if (_trailBytes == null)
		{
			_trailBytes = new byte[3];
		}
		if (num2 >= 3)
		{
			if (_attributeValue != null)
			{
				WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
				WriteAttributeText(XmlConverter.Base64Encoding.GetString(buffer, offset, num2 - _trailByteCount));
			}
			if (!_isXmlnsAttribute)
			{
				StartContent();
				_writer.WriteBase64Text(_trailBytes, _trailByteCount, buffer, offset, num2 - _trailByteCount);
				EndContent();
			}
			_trailByteCount = num - num2;
			if (_trailByteCount > 0)
			{
				int num3 = offset + count - _trailByteCount;
				for (int i = 0; i < _trailByteCount; i++)
				{
					_trailBytes[i] = buffer[num3++];
				}
			}
		}
		else
		{
			Buffer.BlockCopy(buffer, offset, _trailBytes, _trailByteCount, count);
			_trailByteCount += count;
		}
	}

	public override Task WriteBase64Async(byte[] buffer, int offset, int count)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (buffer == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("buffer"));
		}
		if (offset < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("offset", System.SR.ValueMustBeNonNegative));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > buffer.Length - offset)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
		}
		return WriteBase64AsyncImpl(buffer, offset, count);
	}

	private async Task WriteBase64AsyncImpl(byte[] buffer, int offset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		if (_trailByteCount > 0)
		{
			while (_trailByteCount < 3 && count > 0)
			{
				_trailBytes[_trailByteCount++] = buffer[offset++];
				count--;
			}
		}
		int totalByteCount = _trailByteCount + count;
		int actualByteCount = totalByteCount - totalByteCount % 3;
		if (_trailBytes == null)
		{
			_trailBytes = new byte[3];
		}
		if (actualByteCount >= 3)
		{
			if (_attributeValue != null)
			{
				WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
				WriteAttributeText(XmlConverter.Base64Encoding.GetString(buffer, offset, actualByteCount - _trailByteCount));
			}
			if (!_isXmlnsAttribute)
			{
				await StartContentAsync().ConfigureAwait(continueOnCapturedContext: false);
				await _writer.WriteBase64TextAsync(_trailBytes, _trailByteCount, buffer, offset, actualByteCount - _trailByteCount).ConfigureAwait(continueOnCapturedContext: false);
				EndContent();
			}
			_trailByteCount = totalByteCount - actualByteCount;
			if (_trailByteCount > 0)
			{
				int num = offset + count - _trailByteCount;
				for (int i = 0; i < _trailByteCount; i++)
				{
					_trailBytes[i] = buffer[num++];
				}
			}
		}
		else
		{
			Buffer.BlockCopy(buffer, offset, _trailBytes, _trailByteCount, count);
			_trailByteCount += count;
		}
	}

	public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (Signing)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlCanonicalizationStarted));
		}
		FlushElement();
		if (_signingWriter == null)
		{
			_signingWriter = CreateSigningNodeWriter();
		}
		_signingWriter.SetOutput(_writer, stream, includeComments, inclusivePrefixes);
		_writer = _signingWriter;
		SignScope(_signingWriter.CanonicalWriter);
	}

	public override void EndCanonicalization()
	{
		if (IsClosed)
		{
			ThrowClosed();
		}
		if (!Signing)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlCanonicalizationNotStarted));
		}
		_signingWriter.Flush();
		_writer = _signingWriter.NodeWriter;
	}

	protected abstract XmlSigningNodeWriter CreateSigningNodeWriter();

	private void FlushBase64()
	{
		if (_trailByteCount > 0)
		{
			FlushTrailBytes();
		}
	}

	private Task FlushBase64Async()
	{
		if (_trailByteCount <= 0)
		{
			return Task.CompletedTask;
		}
		return FlushTrailBytesAsync();
	}

	private void FlushTrailBytes()
	{
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
		}
		if (!_isXmlnsAttribute)
		{
			StartContent();
			_writer.WriteBase64Text(_trailBytes, _trailByteCount, _trailBytes, 0, 0);
			EndContent();
		}
		_trailByteCount = 0;
	}

	private async Task FlushTrailBytesAsync()
	{
		if (_attributeValue != null)
		{
			WriteAttributeText(XmlConverter.Base64Encoding.GetString(_trailBytes, 0, _trailByteCount));
		}
		if (!_isXmlnsAttribute)
		{
			await StartContentAsync().ConfigureAwait(continueOnCapturedContext: false);
			await _writer.WriteBase64TextAsync(_trailBytes, _trailByteCount, _trailBytes, 0, 0).ConfigureAwait(continueOnCapturedContext: false);
			EndContent();
		}
		_trailByteCount = 0;
	}

	private void WriteValue(object[] array)
	{
		FlushBase64();
		StartContent();
		_writer.WriteStartListText();
		_inList = true;
		for (int i = 0; i < array.Length; i++)
		{
			if (i != 0)
			{
				_writer.WriteListSeparator();
			}
			WritePrimitiveValue(array[i]);
		}
		_inList = false;
		_writer.WriteEndListText();
		EndContent();
	}

	private void WriteValue(Array array)
	{
		FlushBase64();
		StartContent();
		_writer.WriteStartListText();
		_inList = true;
		for (int i = 0; i < array.Length; i++)
		{
			if (i != 0)
			{
				_writer.WriteListSeparator();
			}
			WritePrimitiveValue(array.GetValue(i));
		}
		_inList = false;
		_writer.WriteEndListText();
		EndContent();
	}

	protected void StartArray(int count)
	{
		FlushBase64();
		if (_documentState == DocumentState.Epilog)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlOnlyOneRoot));
		}
		if (_documentState == DocumentState.Document && count > 1 && _depth == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlOnlyOneRoot));
		}
		if (_writeState == WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidWriteState, "WriteStartElement", WriteState.ToString())));
		}
		AutoComplete(WriteState.Content);
	}

	protected void EndArray()
	{
	}

	private string GeneratePrefix(string ns, XmlDictionaryString xNs)
	{
		if (_writeState != WriteState.Element && _writeState != WriteState.Attribute)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidPrefixState, WriteState.ToString())));
		}
		string text = _nsMgr.AddNamespace(ns, xNs);
		if (text != null)
		{
			return text;
		}
		do
		{
			int value = _elements[_depth].PrefixId++;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(2, 2, invariantCulture);
			handler.AppendLiteral("d");
			handler.AppendFormatted(_depth);
			handler.AppendLiteral("p");
			handler.AppendFormatted(value);
			text = string.Create(invariantCulture, ref handler);
		}
		while (_nsMgr.LookupNamespace(text) != null);
		_nsMgr.AddNamespace(text, ns, xNs);
		return text;
	}

	protected void SignScope(XmlCanonicalWriter signingWriter)
	{
		_nsMgr.Sign(signingWriter);
	}

	private void WriteAttributeText(string value)
	{
		if (_attributeValue.Length == 0)
		{
			_attributeValue = value;
		}
		else
		{
			_attributeValue += value;
		}
	}
}
