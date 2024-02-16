using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlSqlBinaryReader : XmlReader, IXmlNamespaceResolver
{
	private enum ScanState
	{
		Doc,
		XmlText,
		Attr,
		AttrVal,
		AttrValPseudoValue,
		Init,
		Error,
		EOF,
		Closed
	}

	internal struct QName
	{
		public string prefix;

		public string localname;

		public string namespaceUri;

		public QName(string prefix, string lname, string nsUri)
		{
			this.prefix = prefix;
			localname = lname;
			namespaceUri = nsUri;
		}

		public void Set(string prefix, string lname, string nsUri)
		{
			this.prefix = prefix;
			localname = lname;
			namespaceUri = nsUri;
		}

		public void Clear()
		{
			prefix = (localname = (namespaceUri = string.Empty));
		}

		public bool MatchNs(string lname, string nsUri)
		{
			if (lname == localname)
			{
				return nsUri == namespaceUri;
			}
			return false;
		}

		public bool MatchPrefix(string prefix, string lname)
		{
			if (lname == localname)
			{
				return prefix == this.prefix;
			}
			return false;
		}

		public void CheckPrefixNS(string prefix, string namespaceUri)
		{
			if (this.prefix == prefix && this.namespaceUri != namespaceUri)
			{
				throw new XmlException(System.SR.XmlBinary_NoRemapPrefix, new string[3] { prefix, this.namespaceUri, namespaceUri });
			}
		}

		public override int GetHashCode()
		{
			return prefix.GetHashCode() ^ localname.GetHashCode();
		}

		public int GetNSHashCode()
		{
			return HashCode.Combine(namespaceUri, localname);
		}

		public override bool Equals([NotNullWhen(true)] object other)
		{
			if (other is QName qName)
			{
				return this == qName;
			}
			return false;
		}

		public override string ToString()
		{
			if (prefix.Length == 0)
			{
				return localname;
			}
			return prefix + ":" + localname;
		}

		public static bool operator ==(QName a, QName b)
		{
			if (a.prefix == b.prefix && a.localname == b.localname)
			{
				return a.namespaceUri == b.namespaceUri;
			}
			return false;
		}

		public static bool operator !=(QName a, QName b)
		{
			return !(a == b);
		}
	}

	private struct ElemInfo
	{
		public QName name;

		public string xmlLang;

		public XmlSpace xmlSpace;

		public bool xmlspacePreserve;

		public NamespaceDecl nsdecls;

		public void Set(QName name, bool xmlspacePreserve)
		{
			this.name = name;
			xmlLang = null;
			xmlSpace = XmlSpace.None;
			this.xmlspacePreserve = xmlspacePreserve;
		}

		public NamespaceDecl Clear()
		{
			NamespaceDecl result = nsdecls;
			nsdecls = null;
			return result;
		}
	}

	private struct AttrInfo
	{
		public QName name;

		public string val;

		public int contentPos;

		public int hashCode;

		public int prevHash;

		public void Set(QName n, string v)
		{
			name = n;
			val = v;
			contentPos = 0;
			hashCode = 0;
			prevHash = 0;
		}

		public void Set(QName n, int pos)
		{
			name = n;
			val = null;
			contentPos = pos;
			hashCode = 0;
			prevHash = 0;
		}

		public void GetLocalnameAndNamespaceUri(out string localname, out string namespaceUri)
		{
			localname = name.localname;
			namespaceUri = name.namespaceUri;
		}

		public int GetLocalnameAndNamespaceUriAndHash(out string localname, out string namespaceUri)
		{
			localname = name.localname;
			namespaceUri = name.namespaceUri;
			return hashCode = name.GetNSHashCode();
		}

		public bool MatchNS(string localname, string namespaceUri)
		{
			return name.MatchNs(localname, namespaceUri);
		}

		public bool MatchHashNS(int hash, string localname, string namespaceUri)
		{
			if (hashCode == hash)
			{
				return name.MatchNs(localname, namespaceUri);
			}
			return false;
		}

		public void AdjustPosition(int adj)
		{
			if (contentPos != 0)
			{
				contentPos += adj;
			}
		}
	}

	private sealed class NamespaceDecl
	{
		public string prefix;

		public string uri;

		public NamespaceDecl scopeLink;

		public NamespaceDecl prevLink;

		public int scope;

		public bool implied;

		public NamespaceDecl(string prefix, string nsuri, NamespaceDecl nextInScope, NamespaceDecl prevDecl, int scope, bool implied)
		{
			this.prefix = prefix;
			uri = nsuri;
			scopeLink = nextInScope;
			prevLink = prevDecl;
			this.scope = scope;
			this.implied = implied;
		}
	}

	private struct SymbolTables
	{
		public string[] symtable;

		public int symCount;

		public QName[] qnametable;

		public int qnameCount;

		public void Init()
		{
			symtable = new string[64];
			qnametable = new QName[16];
			symtable[0] = string.Empty;
			symCount = 1;
			qnameCount = 1;
		}
	}

	private sealed class NestedBinXml
	{
		public SymbolTables symbolTables;

		public int docState;

		public NestedBinXml next;

		public NestedBinXml(SymbolTables symbolTables, int docState, NestedBinXml next)
		{
			this.symbolTables = symbolTables;
			this.docState = docState;
			this.next = next;
		}
	}

	internal static readonly Type TypeOfObject = typeof(object);

	internal static readonly Type TypeOfString = typeof(string);

	private static volatile Type[] s_tokenTypeMap = null;

	private static readonly ReadState[] s_scanState2ReadState = new ReadState[9]
	{
		ReadState.Interactive,
		ReadState.Interactive,
		ReadState.Interactive,
		ReadState.Interactive,
		ReadState.Interactive,
		ReadState.Initial,
		ReadState.Error,
		ReadState.EndOfFile,
		ReadState.Closed
	};

	private Stream _inStrm;

	private byte[] _data;

	private int _pos;

	private int _mark;

	private int _end;

	private bool _eof;

	private bool _sniffed;

	private bool _isEmpty;

	private int _docState;

	private SymbolTables _symbolTables;

	private readonly XmlNameTable _xnt;

	private readonly bool _xntFromSettings;

	private readonly string _xml;

	private readonly string _xmlns;

	private readonly string _nsxmlns;

	private readonly string _baseUri;

	private ScanState _state;

	private XmlNodeType _nodetype;

	private BinXmlToken _token;

	private int _attrIndex;

	private QName _qnameOther;

	private QName _qnameElement;

	private XmlNodeType _parentNodeType;

	private ElemInfo[] _elementStack;

	private int _elemDepth;

	private AttrInfo[] _attributes;

	private int[] _attrHashTbl;

	private int _attrCount;

	private int _posAfterAttrs;

	private bool _xmlspacePreserve;

	private int _tokLen;

	private int _tokDataPos;

	private bool _hasTypedValue;

	private Type _valueType;

	private string _stringValue;

	private readonly Dictionary<string, NamespaceDecl> _namespaces;

	private NestedBinXml _prevNameInfo;

	private XmlReader _textXmlReader;

	private readonly bool _closeInput;

	private readonly bool _checkCharacters;

	private readonly bool _ignoreWhitespace;

	private readonly bool _ignorePIs;

	private readonly bool _ignoreComments;

	private readonly DtdProcessing _dtdProcessing;

	private byte _version;

	private static ReadOnlySpan<byte> XsdKatmaiTimeScaleToValueLengthMap => new byte[8] { 3, 3, 3, 4, 4, 5, 5, 5 };

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			if (_xntFromSettings)
			{
				xmlReaderSettings.NameTable = _xnt;
			}
			XmlReaderSettings xmlReaderSettings2 = xmlReaderSettings;
			xmlReaderSettings2.ConformanceLevel = _docState switch
			{
				0 => ConformanceLevel.Auto, 
				9 => ConformanceLevel.Fragment, 
				_ => ConformanceLevel.Document, 
			};
			xmlReaderSettings.CheckCharacters = _checkCharacters;
			xmlReaderSettings.IgnoreWhitespace = _ignoreWhitespace;
			xmlReaderSettings.IgnoreProcessingInstructions = _ignorePIs;
			xmlReaderSettings.IgnoreComments = _ignoreComments;
			xmlReaderSettings.DtdProcessing = _dtdProcessing;
			xmlReaderSettings.CloseInput = _closeInput;
			xmlReaderSettings.ReadOnly = true;
			return xmlReaderSettings;
		}
	}

	public override XmlNodeType NodeType => _nodetype;

	public override string LocalName => _qnameOther.localname;

	public override string NamespaceURI => _qnameOther.namespaceUri;

	public override string Prefix => _qnameOther.prefix;

	public override bool HasValue
	{
		get
		{
			if (ScanState.XmlText == _state)
			{
				return _textXmlReader.HasValue;
			}
			return XmlReader.HasValueInternal(_nodetype);
		}
	}

	public override string Value
	{
		get
		{
			if (_stringValue != null)
			{
				return _stringValue;
			}
			switch (_state)
			{
			case ScanState.Doc:
				switch (_nodetype)
				{
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Comment:
				case XmlNodeType.DocumentType:
					return _stringValue = GetString(_tokDataPos, _tokLen);
				case XmlNodeType.CDATA:
					return _stringValue = CDATAValue();
				case XmlNodeType.XmlDeclaration:
					return _stringValue = XmlDeclValue();
				case XmlNodeType.Text:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					return _stringValue = ValueAsString(_token);
				}
				break;
			case ScanState.XmlText:
				return _textXmlReader.Value;
			case ScanState.Attr:
			case ScanState.AttrValPseudoValue:
				return _stringValue = GetAttributeText(_attrIndex - 1);
			case ScanState.AttrVal:
				return _stringValue = ValueAsString(_token);
			}
			return string.Empty;
		}
	}

	public override int Depth
	{
		get
		{
			int num = 0;
			switch (_state)
			{
			case ScanState.Doc:
				if (_nodetype == XmlNodeType.Element || _nodetype == XmlNodeType.EndElement)
				{
					num = -1;
				}
				break;
			case ScanState.XmlText:
				num = _textXmlReader.Depth;
				break;
			case ScanState.Attr:
				if (_parentNodeType != XmlNodeType.Element)
				{
					num = 1;
				}
				break;
			case ScanState.AttrVal:
			case ScanState.AttrValPseudoValue:
				if (_parentNodeType != XmlNodeType.Element)
				{
					num = 1;
				}
				num++;
				break;
			default:
				return 0;
			}
			return _elemDepth + num;
		}
	}

	public override string BaseURI => _baseUri;

	public override bool IsEmptyElement
	{
		get
		{
			ScanState state = _state;
			if ((uint)state <= 1u)
			{
				return _isEmpty;
			}
			return false;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			if (ScanState.XmlText != _state)
			{
				for (int num = _elemDepth; num >= 0; num--)
				{
					XmlSpace xmlSpace = _elementStack[num].xmlSpace;
					if (xmlSpace != 0)
					{
						return xmlSpace;
					}
				}
				return XmlSpace.None;
			}
			return _textXmlReader.XmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			if (ScanState.XmlText != _state)
			{
				for (int num = _elemDepth; num >= 0; num--)
				{
					string xmlLang = _elementStack[num].xmlLang;
					if (xmlLang != null)
					{
						return xmlLang;
					}
				}
				return string.Empty;
			}
			return _textXmlReader.XmlLang;
		}
	}

	public override Type ValueType => _valueType;

	public override int AttributeCount
	{
		get
		{
			switch (_state)
			{
			case ScanState.Doc:
			case ScanState.Attr:
			case ScanState.AttrVal:
			case ScanState.AttrValPseudoValue:
				return _attrCount;
			case ScanState.XmlText:
				return _textXmlReader.AttributeCount;
			default:
				return 0;
			}
		}
	}

	public override bool EOF => _state == ScanState.EOF;

	public override XmlNameTable NameTable => _xnt;

	public override ReadState ReadState => s_scanState2ReadState[(int)_state];

	public XmlSqlBinaryReader(Stream stream, byte[] data, int len, string baseUri, bool closeInput, XmlReaderSettings settings)
	{
		_xnt = settings.NameTable;
		if (_xnt == null)
		{
			_xnt = new NameTable();
			_xntFromSettings = false;
		}
		else
		{
			_xntFromSettings = true;
		}
		_xml = _xnt.Add("xml");
		_xmlns = _xnt.Add("xmlns");
		_nsxmlns = _xnt.Add("http://www.w3.org/2000/xmlns/");
		_baseUri = baseUri;
		_state = ScanState.Init;
		_nodetype = XmlNodeType.None;
		_token = BinXmlToken.Error;
		_elementStack = new ElemInfo[16];
		_attributes = new AttrInfo[8];
		_attrHashTbl = new int[8];
		_symbolTables.Init();
		_qnameOther.Clear();
		_qnameElement.Clear();
		_xmlspacePreserve = false;
		_namespaces = new Dictionary<string, NamespaceDecl>();
		AddInitNamespace(string.Empty, string.Empty);
		AddInitNamespace(_xml, _xnt.Add("http://www.w3.org/XML/1998/namespace"));
		AddInitNamespace(_xmlns, _nsxmlns);
		_valueType = TypeOfString;
		_inStrm = stream;
		if (data != null)
		{
			_data = data;
			_end = len;
			_pos = 2;
			_sniffed = true;
		}
		else
		{
			_data = new byte[4096];
			_end = stream.Read(_data, 0, 4096);
			_pos = 0;
			_sniffed = false;
		}
		_mark = -1;
		_eof = _end == 0;
		_closeInput = closeInput;
		switch (settings.ConformanceLevel)
		{
		case ConformanceLevel.Auto:
			_docState = 0;
			break;
		case ConformanceLevel.Fragment:
			_docState = 9;
			break;
		case ConformanceLevel.Document:
			_docState = 1;
			break;
		}
		_checkCharacters = settings.CheckCharacters;
		_dtdProcessing = settings.DtdProcessing;
		_ignoreWhitespace = settings.IgnoreWhitespace;
		_ignorePIs = settings.IgnoreProcessingInstructions;
		_ignoreComments = settings.IgnoreComments;
		s_tokenTypeMap = s_tokenTypeMap ?? GenerateTokenTypeMap();
	}

	public override string GetAttribute(string name, string ns)
	{
		if (ScanState.XmlText == _state)
		{
			return _textXmlReader.GetAttribute(name, ns);
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (ns == null)
		{
			ns = string.Empty;
		}
		int num = LocateAttribute(name, ns);
		if (-1 == num)
		{
			return null;
		}
		return GetAttribute(num);
	}

	public override string GetAttribute(string name)
	{
		if (ScanState.XmlText == _state)
		{
			return _textXmlReader.GetAttribute(name);
		}
		int num = LocateAttribute(name);
		if (-1 == num)
		{
			return null;
		}
		return GetAttribute(num);
	}

	public override string GetAttribute(int i)
	{
		if (ScanState.XmlText == _state)
		{
			return _textXmlReader.GetAttribute(i);
		}
		if (i < 0 || i >= _attrCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		return GetAttributeText(i);
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		if (ScanState.XmlText == _state)
		{
			return UpdateFromTextReader(_textXmlReader.MoveToAttribute(name, ns));
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (ns == null)
		{
			ns = string.Empty;
		}
		int num = LocateAttribute(name, ns);
		if (-1 != num && _state < ScanState.Init)
		{
			PositionOnAttribute(num + 1);
			return true;
		}
		return false;
	}

	public override bool MoveToAttribute(string name)
	{
		if (ScanState.XmlText == _state)
		{
			return UpdateFromTextReader(_textXmlReader.MoveToAttribute(name));
		}
		int num = LocateAttribute(name);
		if (-1 != num && _state < ScanState.Init)
		{
			PositionOnAttribute(num + 1);
			return true;
		}
		return false;
	}

	public override void MoveToAttribute(int i)
	{
		if (ScanState.XmlText == _state)
		{
			_textXmlReader.MoveToAttribute(i);
			UpdateFromTextReader(needUpdate: true);
			return;
		}
		if (i < 0 || i >= _attrCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		PositionOnAttribute(i + 1);
	}

	public override bool MoveToFirstAttribute()
	{
		if (ScanState.XmlText == _state)
		{
			return UpdateFromTextReader(_textXmlReader.MoveToFirstAttribute());
		}
		if (_attrCount == 0)
		{
			return false;
		}
		PositionOnAttribute(1);
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		switch (_state)
		{
		case ScanState.Doc:
		case ScanState.Attr:
		case ScanState.AttrVal:
		case ScanState.AttrValPseudoValue:
			if (_attrIndex >= _attrCount)
			{
				return false;
			}
			PositionOnAttribute(++_attrIndex);
			return true;
		case ScanState.XmlText:
			return UpdateFromTextReader(_textXmlReader.MoveToNextAttribute());
		default:
			return false;
		}
	}

	public override bool MoveToElement()
	{
		switch (_state)
		{
		case ScanState.Attr:
		case ScanState.AttrVal:
		case ScanState.AttrValPseudoValue:
			_attrIndex = 0;
			_qnameOther = _qnameElement;
			if (XmlNodeType.Element == _parentNodeType)
			{
				_token = BinXmlToken.Element;
			}
			else if (XmlNodeType.XmlDeclaration == _parentNodeType)
			{
				_token = BinXmlToken.XmlDecl;
			}
			else if (XmlNodeType.DocumentType == _parentNodeType)
			{
				_token = BinXmlToken.DocType;
			}
			_nodetype = _parentNodeType;
			_state = ScanState.Doc;
			_pos = _posAfterAttrs;
			_stringValue = null;
			return true;
		case ScanState.XmlText:
			return UpdateFromTextReader(_textXmlReader.MoveToElement());
		default:
			return false;
		}
	}

	public override bool ReadAttributeValue()
	{
		_stringValue = null;
		switch (_state)
		{
		case ScanState.Attr:
			if (_attributes[_attrIndex - 1].val == null)
			{
				_pos = _attributes[_attrIndex - 1].contentPos;
				BinXmlToken binXmlToken = RescanNextToken();
				if (BinXmlToken.Attr == binXmlToken || BinXmlToken.EndAttrs == binXmlToken)
				{
					return false;
				}
				_token = binXmlToken;
				ReScanOverValue(binXmlToken);
				_valueType = GetValueType(binXmlToken);
				_state = ScanState.AttrVal;
			}
			else
			{
				_token = BinXmlToken.Error;
				_valueType = TypeOfString;
				_state = ScanState.AttrValPseudoValue;
			}
			_qnameOther.Clear();
			_nodetype = XmlNodeType.Text;
			return true;
		case ScanState.AttrVal:
			return false;
		case ScanState.XmlText:
			return UpdateFromTextReader(_textXmlReader.ReadAttributeValue());
		default:
			return false;
		}
	}

	public override void Close()
	{
		_state = ScanState.Closed;
		_nodetype = XmlNodeType.None;
		_token = BinXmlToken.Error;
		_stringValue = null;
		if (_textXmlReader != null)
		{
			_textXmlReader.Close();
			_textXmlReader = null;
		}
		if (_inStrm != null && _closeInput)
		{
			_inStrm.Dispose();
		}
		_inStrm = null;
		_pos = (_end = 0);
	}

	public override string LookupNamespace(string prefix)
	{
		if (ScanState.XmlText == _state)
		{
			return _textXmlReader.LookupNamespace(prefix);
		}
		if (prefix != null && _namespaces.TryGetValue(prefix, out var value))
		{
			return value.uri;
		}
		return null;
	}

	public override void ResolveEntity()
	{
		throw new NotSupportedException();
	}

	public override bool Read()
	{
		try
		{
			switch (_state)
			{
			case ScanState.Init:
				return ReadInit(skipXmlDecl: false);
			case ScanState.Doc:
				return ReadDoc();
			case ScanState.XmlText:
				if (_textXmlReader.Read())
				{
					return UpdateFromTextReader(needUpdate: true);
				}
				_state = ScanState.Doc;
				_nodetype = XmlNodeType.None;
				_isEmpty = false;
				goto case ScanState.Doc;
			case ScanState.Attr:
			case ScanState.AttrVal:
			case ScanState.AttrValPseudoValue:
				MoveToElement();
				goto case ScanState.Doc;
			default:
				return false;
			}
		}
		catch (OverflowException ex)
		{
			_state = ScanState.Error;
			throw new XmlException(ex.Message, ex);
		}
		catch
		{
			_state = ScanState.Error;
			throw;
		}
	}

	private bool SetupContentAsXXX(string name)
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException(name);
		}
		switch (_state)
		{
		case ScanState.Doc:
			if (NodeType == XmlNodeType.EndElement)
			{
				return true;
			}
			if (NodeType == XmlNodeType.ProcessingInstruction || NodeType == XmlNodeType.Comment)
			{
				while (Read() && (NodeType == XmlNodeType.ProcessingInstruction || NodeType == XmlNodeType.Comment))
				{
				}
				if (NodeType == XmlNodeType.EndElement)
				{
					return true;
				}
			}
			if (_hasTypedValue)
			{
				return true;
			}
			break;
		case ScanState.Attr:
		{
			_pos = _attributes[_attrIndex - 1].contentPos;
			BinXmlToken binXmlToken = RescanNextToken();
			if (BinXmlToken.Attr != binXmlToken && BinXmlToken.EndAttrs != binXmlToken)
			{
				_token = binXmlToken;
				ReScanOverValue(binXmlToken);
				return true;
			}
			break;
		}
		case ScanState.AttrVal:
			return true;
		}
		return false;
	}

	private int FinishContentAsXXX(int origPos)
	{
		if (_state == ScanState.Doc)
		{
			if (NodeType != XmlNodeType.Element && NodeType != XmlNodeType.EndElement)
			{
				while (Read())
				{
					XmlNodeType nodeType = NodeType;
					if (nodeType == XmlNodeType.Element)
					{
						break;
					}
					if ((uint)(nodeType - 7) > 1u)
					{
						if (nodeType == XmlNodeType.EndElement)
						{
							break;
						}
						throw ThrowNotSupported(System.SR.XmlBinary_ListsOfValuesNotSupported);
					}
				}
			}
			return _pos;
		}
		return origPos;
	}

	public override bool ReadContentAsBoolean()
	{
		int num = _pos;
		bool flag = false;
		try
		{
			if (SetupContentAsXXX("ReadContentAsBoolean"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.XSD_BOOLEAN:
						flag = _data[_tokDataPos] != 0;
						goto IL_0165;
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Boolean"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToBoolean(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException2, null);
				}
			}
			goto end_IL_0009;
			IL_0165:
			num = FinishContentAsXXX(num);
			return flag;
			end_IL_0009:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsBoolean();
	}

	public override DateTime ReadContentAsDateTime()
	{
		int num = _pos;
		try
		{
			DateTime result;
			if (SetupContentAsXXX("ReadContentAsDateTime"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
						result = ValueAsDateTime();
						goto IL_016f;
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "DateTime"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToDateTime(string.Empty, XmlDateTimeSerializationMode.RoundtripKind);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_016f:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsDateTime();
	}

	public override double ReadContentAsDouble()
	{
		int num = _pos;
		try
		{
			double result;
			if (SetupContentAsXXX("ReadContentAsDouble"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
						result = ValueAsDouble();
						goto IL_0132;
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Double"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToDouble(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_0132:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsDouble();
	}

	public override float ReadContentAsFloat()
	{
		int num = _pos;
		try
		{
			float result;
			if (SetupContentAsXXX("ReadContentAsFloat"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
						result = (float)ValueAsDouble();
						goto IL_0133;
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Float"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToSingle(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_0133:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsFloat();
	}

	public override decimal ReadContentAsDecimal()
	{
		int num = _pos;
		try
		{
			decimal result;
			if (SetupContentAsXXX("ReadContentAsDecimal"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
						result = ValueAsDecimal();
						goto IL_016e;
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Decimal"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToDecimal(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_016e:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsDecimal();
	}

	public override int ReadContentAsInt()
	{
		int num = _pos;
		try
		{
			int result;
			if (SetupContentAsXXX("ReadContentAsInt"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
						result = checked((int)ValueAsLong());
						goto IL_016f;
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Int32"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToInt32(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int32", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int32", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int32", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_016f:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsInt();
	}

	public override long ReadContentAsLong()
	{
		int num = _pos;
		try
		{
			long result;
			if (SetupContentAsXXX("ReadContentAsLong"))
			{
				try
				{
					switch (_token)
					{
					case BinXmlToken.SQL_SMALLINT:
					case BinXmlToken.SQL_INT:
					case BinXmlToken.SQL_MONEY:
					case BinXmlToken.SQL_BIT:
					case BinXmlToken.SQL_TINYINT:
					case BinXmlToken.SQL_BIGINT:
					case BinXmlToken.SQL_DECIMAL:
					case BinXmlToken.SQL_NUMERIC:
					case BinXmlToken.SQL_SMALLMONEY:
					case BinXmlToken.XSD_DECIMAL:
					case BinXmlToken.XSD_BYTE:
					case BinXmlToken.XSD_UNSIGNEDSHORT:
					case BinXmlToken.XSD_UNSIGNEDINT:
					case BinXmlToken.XSD_UNSIGNEDLONG:
						result = ValueAsLong();
						goto IL_016e;
					case BinXmlToken.SQL_REAL:
					case BinXmlToken.SQL_FLOAT:
					case BinXmlToken.SQL_UUID:
					case BinXmlToken.SQL_BINARY:
					case BinXmlToken.SQL_VARBINARY:
					case BinXmlToken.SQL_DATETIME:
					case BinXmlToken.SQL_SMALLDATETIME:
					case BinXmlToken.SQL_IMAGE:
					case BinXmlToken.SQL_UDT:
					case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
					case BinXmlToken.XSD_KATMAI_DATEOFFSET:
					case BinXmlToken.XSD_KATMAI_TIME:
					case BinXmlToken.XSD_KATMAI_DATETIME:
					case BinXmlToken.XSD_KATMAI_DATE:
					case BinXmlToken.XSD_TIME:
					case BinXmlToken.XSD_DATETIME:
					case BinXmlToken.XSD_DATE:
					case BinXmlToken.XSD_BINHEX:
					case BinXmlToken.XSD_BASE64:
					case BinXmlToken.XSD_BOOLEAN:
					case BinXmlToken.XSD_QNAME:
						throw new InvalidCastException(System.SR.Format(System.SR.XmlBinary_CastNotSupported, _token, "Int64"));
					case BinXmlToken.EndElem:
					case BinXmlToken.Element:
						return XmlConvert.ToInt64(string.Empty);
					}
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int64", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int64", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int64", innerException3, null);
				}
			}
			goto end_IL_0007;
			IL_016e:
			num = FinishContentAsXXX(num);
			return result;
			end_IL_0007:;
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsLong();
	}

	public override object ReadContentAsObject()
	{
		int num = _pos;
		try
		{
			if (SetupContentAsXXX("ReadContentAsObject"))
			{
				object result;
				try
				{
					result = ((NodeType != XmlNodeType.Element && NodeType != XmlNodeType.EndElement) ? ValueAsObject(_token, returnInternalTypes: false) : string.Empty);
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Object", innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Object", innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Object", innerException3, null);
				}
				num = FinishContentAsXXX(num);
				return result;
			}
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAsObject();
	}

	public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		int num = _pos;
		try
		{
			if (SetupContentAsXXX("ReadContentAs"))
			{
				object result;
				try
				{
					result = ((NodeType != XmlNodeType.Element && NodeType != XmlNodeType.EndElement) ? ((!(returnType == ValueType) && !(returnType == typeof(object))) ? ValueAs(_token, returnType, namespaceResolver) : ValueAsObject(_token, returnInternalTypes: false)) : string.Empty);
				}
				catch (InvalidCastException innerException)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, null);
				}
				catch (FormatException innerException2)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, null);
				}
				catch (OverflowException innerException3)
				{
					throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException3, null);
				}
				num = FinishContentAsXXX(num);
				return result;
			}
		}
		finally
		{
			_pos = num;
		}
		return base.ReadContentAs(returnType, namespaceResolver);
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		if (ScanState.XmlText == _state)
		{
			IXmlNamespaceResolver xmlNamespaceResolver = (IXmlNamespaceResolver)_textXmlReader;
			return xmlNamespaceResolver.GetNamespacesInScope(scope);
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (XmlNamespaceScope.Local == scope)
		{
			if (_elemDepth > 0)
			{
				for (NamespaceDecl namespaceDecl = _elementStack[_elemDepth].nsdecls; namespaceDecl != null; namespaceDecl = namespaceDecl.scopeLink)
				{
					dictionary.Add(namespaceDecl.prefix, namespaceDecl.uri);
				}
			}
		}
		else
		{
			foreach (NamespaceDecl value in _namespaces.Values)
			{
				if ((value.scope != -1 || (scope == XmlNamespaceScope.All && "xml" == value.prefix)) && (value.prefix.Length > 0 || value.uri.Length > 0))
				{
					dictionary.Add(value.prefix, value.uri);
				}
			}
		}
		return dictionary;
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		if (ScanState.XmlText == _state)
		{
			IXmlNamespaceResolver xmlNamespaceResolver = (IXmlNamespaceResolver)_textXmlReader;
			return xmlNamespaceResolver.LookupPrefix(namespaceName);
		}
		if (namespaceName == null)
		{
			return null;
		}
		string text = _xnt.Get(namespaceName);
		if (text == null)
		{
			return null;
		}
		for (int num = _elemDepth; num >= 0; num--)
		{
			for (NamespaceDecl namespaceDecl = _elementStack[num].nsdecls; namespaceDecl != null; namespaceDecl = namespaceDecl.scopeLink)
			{
				if ((object)namespaceDecl.uri == text)
				{
					return namespaceDecl.prefix;
				}
			}
		}
		return null;
	}

	private void VerifyVersion(int requiredVersion, BinXmlToken token)
	{
		if (_version < requiredVersion)
		{
			throw ThrowUnexpectedToken(token);
		}
	}

	private void AddInitNamespace(string prefix, string uri)
	{
		NamespaceDecl namespaceDecl = new NamespaceDecl(prefix, uri, _elementStack[0].nsdecls, null, -1, implied: true);
		_elementStack[0].nsdecls = namespaceDecl;
		_namespaces.Add(prefix, namespaceDecl);
	}

	private void AddName()
	{
		string array = ParseText();
		int num = _symbolTables.symCount++;
		string[] array2 = _symbolTables.symtable;
		if (num == array2.Length)
		{
			string[] array3 = new string[checked(num * 2)];
			Array.Copy(array2, array3, num);
			array2 = (_symbolTables.symtable = array3);
		}
		array2[num] = _xnt.Add(array);
	}

	private void AddQName()
	{
		int num = ReadNameRef();
		int num2 = ReadNameRef();
		int num3 = ReadNameRef();
		int num4 = _symbolTables.qnameCount++;
		QName[] array = _symbolTables.qnametable;
		if (num4 == array.Length)
		{
			QName[] array2 = new QName[checked(num4 * 2)];
			Array.Copy(array, array2, num4);
			array = (_symbolTables.qnametable = array2);
		}
		string[] symtable = _symbolTables.symtable;
		string text = symtable[num2];
		string lname;
		string nsUri;
		if (num3 == 0)
		{
			if (num2 == 0 && num == 0)
			{
				return;
			}
			if (!text.StartsWith("xmlns", StringComparison.Ordinal))
			{
				goto IL_0104;
			}
			if (5 < text.Length)
			{
				if (6 == text.Length || ':' != text[5])
				{
					goto IL_0104;
				}
				lname = _xnt.Add(text.Substring(6));
				text = _xmlns;
			}
			else
			{
				lname = text;
				text = string.Empty;
			}
			nsUri = _nsxmlns;
		}
		else
		{
			lname = symtable[num3];
			nsUri = symtable[num];
		}
		array[num4].Set(text, lname, nsUri);
		return;
		IL_0104:
		throw new XmlException(System.SR.Xml_BadNamespaceDecl, (string[])null);
	}

	private void NameFlush()
	{
		_symbolTables.symCount = (_symbolTables.qnameCount = 1);
		Array.Clear(_symbolTables.symtable, 1, _symbolTables.symtable.Length - 1);
		Array.Clear(_symbolTables.qnametable);
	}

	private void SkipExtn()
	{
		int num = ParseMB32();
		checked
		{
			_pos += num;
			Fill(-1);
		}
	}

	private int ReadQNameRef()
	{
		int num = ParseMB32();
		if (num < 0 || num >= _symbolTables.qnameCount)
		{
			throw new XmlException(System.SR.XmlBin_InvalidQNameID, string.Empty);
		}
		return num;
	}

	private int ReadNameRef()
	{
		int num = ParseMB32();
		if (num < 0 || num >= _symbolTables.symCount)
		{
			throw new XmlException(System.SR.XmlBin_InvalidQNameID, string.Empty);
		}
		return num;
	}

	private bool FillAllowEOF()
	{
		if (_eof)
		{
			return false;
		}
		byte[] array = _data;
		int pos = _pos;
		int num = _mark;
		int end = _end;
		if (num == -1)
		{
			num = pos;
		}
		if (num >= 0 && num < end)
		{
			int num2 = end - num;
			if (num2 > 7 * (array.Length / 8))
			{
				byte[] array2 = new byte[checked(array.Length * 2)];
				Array.Copy(array, num, array2, 0, num2);
				array = (_data = array2);
			}
			else
			{
				Array.Copy(array, num, array, 0, num2);
			}
			pos -= num;
			end -= num;
			_tokDataPos -= num;
			for (int i = 0; i < _attrCount; i++)
			{
				_attributes[i].AdjustPosition(-num);
			}
			_pos = pos;
			_mark = 0;
		}
		else
		{
			_pos -= end;
			_mark -= end;
			_tokDataPos -= end;
			end = 0;
		}
		int count = array.Length - end;
		int num3 = _inStrm.Read(array, end, count);
		_end = end + num3;
		_eof = num3 <= 0;
		return num3 > 0;
	}

	private void Fill_(int require)
	{
		while (FillAllowEOF() && _pos + require >= _end)
		{
		}
		if (_pos + require >= _end)
		{
			throw ThrowXmlException(System.SR.Xml_UnexpectedEOF1);
		}
	}

	private void Fill(int require)
	{
		if (_pos + require >= _end)
		{
			Fill_(require);
		}
	}

	private byte ReadByte()
	{
		Fill(0);
		return _data[_pos++];
	}

	private ushort ReadUShort()
	{
		Fill(1);
		int pos = _pos;
		byte[] data = _data;
		ushort result = (ushort)(data[pos] + (data[pos + 1] << 8));
		_pos += 2;
		return result;
	}

	private int ParseMB32()
	{
		byte b = ReadByte();
		if (b > 127)
		{
			return ParseMB32_(b);
		}
		return b;
	}

	private int ParseMB32_(byte b)
	{
		uint num = b & 0x7Fu;
		b = ReadByte();
		uint num2 = b & 0x7Fu;
		num += num2 << 7;
		if (b > 127)
		{
			b = ReadByte();
			num2 = b & 0x7Fu;
			num += num2 << 14;
			if (b > 127)
			{
				b = ReadByte();
				num2 = b & 0x7Fu;
				num += num2 << 21;
				if (b > 127)
				{
					b = ReadByte();
					num2 = b & 7u;
					if (b > 7)
					{
						throw ThrowXmlException(System.SR.XmlBinary_ValueTooBig);
					}
					num += num2 << 28;
				}
			}
		}
		return (int)num;
	}

	private int ParseMB32(int pos)
	{
		byte[] data = _data;
		byte b = data[pos++];
		uint num = b & 0x7Fu;
		if (b > 127)
		{
			b = data[pos++];
			uint num2 = b & 0x7Fu;
			num += num2 << 7;
			if (b > 127)
			{
				b = data[pos++];
				num2 = b & 0x7Fu;
				num += num2 << 14;
				if (b > 127)
				{
					b = data[pos++];
					num2 = b & 0x7Fu;
					num += num2 << 21;
					if (b > 127)
					{
						b = data[pos++];
						num2 = b & 7u;
						if (b > 7)
						{
							throw ThrowXmlException(System.SR.XmlBinary_ValueTooBig);
						}
						num += num2 << 28;
					}
				}
			}
		}
		return (int)num;
	}

	private int ParseMB64()
	{
		byte b = ReadByte();
		if (b > 127)
		{
			return ParseMB32_(b);
		}
		return b;
	}

	private BinXmlToken PeekToken()
	{
		while (_pos >= _end && FillAllowEOF())
		{
		}
		if (_pos >= _end)
		{
			return BinXmlToken.EOF;
		}
		return (BinXmlToken)_data[_pos];
	}

	private BinXmlToken ReadToken()
	{
		while (_pos >= _end && FillAllowEOF())
		{
		}
		if (_pos >= _end)
		{
			return BinXmlToken.EOF;
		}
		return (BinXmlToken)_data[_pos++];
	}

	private BinXmlToken NextToken2(BinXmlToken token)
	{
		while (true)
		{
			switch (token)
			{
			case BinXmlToken.Name:
				AddName();
				break;
			case BinXmlToken.QName:
				AddQName();
				break;
			case BinXmlToken.NmFlush:
				NameFlush();
				break;
			case BinXmlToken.Extn:
				SkipExtn();
				break;
			default:
				return token;
			}
			token = ReadToken();
		}
	}

	private BinXmlToken NextToken1()
	{
		int pos = _pos;
		BinXmlToken binXmlToken;
		if (pos >= _end)
		{
			binXmlToken = ReadToken();
		}
		else
		{
			binXmlToken = (BinXmlToken)_data[pos];
			_pos = pos + 1;
		}
		if (binXmlToken >= BinXmlToken.NmFlush && binXmlToken <= BinXmlToken.Name)
		{
			return NextToken2(binXmlToken);
		}
		return binXmlToken;
	}

	private BinXmlToken NextToken()
	{
		int pos = _pos;
		if (pos < _end)
		{
			BinXmlToken binXmlToken = (BinXmlToken)_data[pos];
			if (binXmlToken < BinXmlToken.NmFlush || binXmlToken > BinXmlToken.Name)
			{
				_pos = pos + 1;
				return binXmlToken;
			}
		}
		return NextToken1();
	}

	private BinXmlToken PeekNextToken()
	{
		BinXmlToken binXmlToken = NextToken();
		if (BinXmlToken.EOF != binXmlToken)
		{
			_pos--;
		}
		return binXmlToken;
	}

	private BinXmlToken RescanNextToken()
	{
		checked
		{
			while (true)
			{
				BinXmlToken binXmlToken = ReadToken();
				switch (binXmlToken)
				{
				case BinXmlToken.NmFlush:
					break;
				case BinXmlToken.Name:
				{
					int num2 = ParseMB32();
					_pos += 2 * num2;
					break;
				}
				case BinXmlToken.QName:
					ParseMB32();
					ParseMB32();
					ParseMB32();
					break;
				case BinXmlToken.Extn:
				{
					int num = ParseMB32();
					_pos += num;
					break;
				}
				default:
					return binXmlToken;
				}
			}
		}
	}

	private string ParseText()
	{
		int mark = _mark;
		try
		{
			if (mark < 0)
			{
				_mark = _pos;
			}
			int start;
			int cch = ScanText(out start);
			return GetString(start, cch);
		}
		finally
		{
			if (mark < 0)
			{
				_mark = -1;
			}
		}
	}

	private int ScanText(out int start)
	{
		int num = ParseMB32();
		int mark = _mark;
		int pos = _pos;
		checked
		{
			_pos += num * 2;
			if (_pos > _end)
			{
				Fill(-1);
			}
		}
		start = pos - (mark - _mark);
		return num;
	}

	private string GetString(int pos, int cch)
	{
		checked
		{
			if (pos + cch * 2 > _end)
			{
				throw new XmlException(System.SR.Xml_UnexpectedEOF1, (string[])null);
			}
			if (cch == 0)
			{
				return string.Empty;
			}
			return string.Create(cch, (_data, pos), delegate(Span<char> dstChars, (byte[] _data, int pos) state)
			{
				int length = dstChars.Length;
				ReadOnlySpan<byte> readOnlySpan = state._data.AsSpan(state.pos, length * 2);
				Span<byte> destination = MemoryMarshal.AsBytes(dstChars);
				readOnlySpan.CopyTo(destination);
			});
		}
	}

	private string GetAttributeText(int i)
	{
		string val = _attributes[i].val;
		if (val != null)
		{
			return val;
		}
		int pos = _pos;
		try
		{
			_pos = _attributes[i].contentPos;
			BinXmlToken binXmlToken = RescanNextToken();
			if (BinXmlToken.Attr == binXmlToken || BinXmlToken.EndAttrs == binXmlToken)
			{
				return "";
			}
			_token = binXmlToken;
			ReScanOverValue(binXmlToken);
			return ValueAsString(binXmlToken);
		}
		finally
		{
			_pos = pos;
		}
	}

	private int LocateAttribute(string name, string ns)
	{
		for (int i = 0; i < _attrCount; i++)
		{
			if (_attributes[i].name.MatchNs(name, ns))
			{
				return i;
			}
		}
		return -1;
	}

	private int LocateAttribute(string name)
	{
		ValidateNames.SplitQName(name, out var prefix, out var lname);
		for (int i = 0; i < _attrCount; i++)
		{
			if (_attributes[i].name.MatchPrefix(prefix, lname))
			{
				return i;
			}
		}
		return -1;
	}

	private void PositionOnAttribute(int i)
	{
		_attrIndex = i;
		_qnameOther = _attributes[i - 1].name;
		if (_state == ScanState.Doc)
		{
			_parentNodeType = _nodetype;
		}
		_token = BinXmlToken.Attr;
		_nodetype = XmlNodeType.Attribute;
		_state = ScanState.Attr;
		_valueType = TypeOfObject;
		_stringValue = null;
	}

	private void GrowElements()
	{
		int num = _elementStack.Length * 2;
		ElemInfo[] array = new ElemInfo[num];
		Array.Copy(_elementStack, array, _elementStack.Length);
		_elementStack = array;
	}

	private void GrowAttributes()
	{
		int num = _attributes.Length * 2;
		AttrInfo[] array = new AttrInfo[num];
		Array.Copy(_attributes, array, _attrCount);
		_attributes = array;
	}

	private void ClearAttributes()
	{
		if (_attrCount != 0)
		{
			_attrCount = 0;
		}
	}

	private void PushNamespace(string prefix, string ns, bool implied)
	{
		if (prefix == "xml")
		{
			return;
		}
		int elemDepth = _elemDepth;
		_namespaces.TryGetValue(prefix, out var value);
		if (value != null)
		{
			if (value.uri == ns)
			{
				if (!implied && value.implied && value.scope == elemDepth)
				{
					value.implied = false;
				}
				return;
			}
			_qnameElement.CheckPrefixNS(prefix, ns);
			if (prefix.Length != 0)
			{
				for (int i = 0; i < _attrCount; i++)
				{
					if (_attributes[i].name.prefix.Length != 0)
					{
						_attributes[i].name.CheckPrefixNS(prefix, ns);
					}
				}
			}
		}
		NamespaceDecl namespaceDecl = new NamespaceDecl(prefix, ns, _elementStack[elemDepth].nsdecls, value, elemDepth, implied);
		_elementStack[elemDepth].nsdecls = namespaceDecl;
		_namespaces[prefix] = namespaceDecl;
	}

	private void PopNamespaces(NamespaceDecl firstInScopeChain)
	{
		NamespaceDecl namespaceDecl = firstInScopeChain;
		while (namespaceDecl != null)
		{
			if (namespaceDecl.prevLink == null)
			{
				_namespaces.Remove(namespaceDecl.prefix);
			}
			else
			{
				_namespaces[namespaceDecl.prefix] = namespaceDecl.prevLink;
			}
			NamespaceDecl scopeLink = namespaceDecl.scopeLink;
			namespaceDecl.prevLink = null;
			namespaceDecl.scopeLink = null;
			namespaceDecl = scopeLink;
		}
	}

	private void GenerateImpliedXmlnsAttrs()
	{
		for (NamespaceDecl namespaceDecl = _elementStack[_elemDepth].nsdecls; namespaceDecl != null; namespaceDecl = namespaceDecl.scopeLink)
		{
			if (namespaceDecl.implied)
			{
				if (_attrCount == _attributes.Length)
				{
					GrowAttributes();
				}
				QName n = ((namespaceDecl.prefix.Length != 0) ? new QName(_xmlns, _xnt.Add(namespaceDecl.prefix), _nsxmlns) : new QName(string.Empty, _xmlns, _nsxmlns));
				_attributes[_attrCount].Set(n, namespaceDecl.uri);
				_attrCount++;
			}
		}
	}

	private bool ReadInit(bool skipXmlDecl)
	{
		string text = null;
		if (!_sniffed)
		{
			ushort num = ReadUShort();
			if (num != 65503)
			{
				text = System.SR.XmlBinary_InvalidSignature;
				goto IL_01e6;
			}
		}
		_version = ReadByte();
		if (_version != 1 && _version != 2)
		{
			text = System.SR.XmlBinary_InvalidProtocolVersion;
		}
		else
		{
			if (1200 == ReadUShort())
			{
				_state = ScanState.Doc;
				if (BinXmlToken.XmlDecl == PeekToken())
				{
					_pos++;
					_attributes[0].Set(new QName(string.Empty, _xnt.Add("version"), string.Empty), ParseText());
					_attrCount = 1;
					if (BinXmlToken.Encoding == PeekToken())
					{
						_pos++;
						_attributes[1].Set(new QName(string.Empty, _xnt.Add("encoding"), string.Empty), ParseText());
						_attrCount++;
					}
					byte b = ReadByte();
					if (b != 0)
					{
						if ((uint)(b - 1) > 1u)
						{
							text = System.SR.XmlBinary_InvalidStandalone;
							goto IL_01e6;
						}
						_attributes[_attrCount].Set(new QName(string.Empty, _xnt.Add("standalone"), string.Empty), (b == 1) ? "yes" : "no");
						_attrCount++;
					}
					if (!skipXmlDecl)
					{
						QName qnameElement = new QName(string.Empty, _xnt.Add("xml"), string.Empty);
						_qnameOther = (_qnameElement = qnameElement);
						_nodetype = XmlNodeType.XmlDeclaration;
						_posAfterAttrs = _pos;
						return true;
					}
				}
				return ReadDoc();
			}
			text = System.SR.XmlBinary_UnsupportedCodePage;
		}
		goto IL_01e6;
		IL_01e6:
		_state = ScanState.Error;
		throw new XmlException(text, (string[])null);
	}

	private void ScanAttributes()
	{
		int num = -1;
		int num2 = -1;
		_mark = _pos;
		string text = null;
		bool flag = false;
		BinXmlToken binXmlToken;
		while (BinXmlToken.EndAttrs != (binXmlToken = NextToken()))
		{
			if (BinXmlToken.Attr == binXmlToken)
			{
				if (text != null)
				{
					PushNamespace(text, string.Empty, implied: false);
					text = null;
				}
				if (_attrCount == _attributes.Length)
				{
					GrowAttributes();
				}
				QName n = _symbolTables.qnametable[ReadQNameRef()];
				_attributes[_attrCount].Set(n, _pos);
				if (n.prefix == "xml")
				{
					if (n.localname == "lang")
					{
						num2 = _attrCount;
					}
					else if (n.localname == "space")
					{
						num = _attrCount;
					}
				}
				else if (Ref.Equal(n.namespaceUri, _nsxmlns))
				{
					text = n.localname;
					if (text == "xmlns")
					{
						text = string.Empty;
					}
				}
				else if (n.prefix.Length != 0)
				{
					if (n.namespaceUri.Length == 0)
					{
						throw new XmlException(System.SR.Xml_PrefixForEmptyNs, string.Empty);
					}
					PushNamespace(n.prefix, n.namespaceUri, implied: true);
				}
				else if (n.namespaceUri.Length != 0)
				{
					throw ThrowXmlException(System.SR.XmlBinary_AttrWithNsNoPrefix, n.localname, n.namespaceUri);
				}
				_attrCount++;
				flag = false;
			}
			else
			{
				ScanOverValue(binXmlToken, attr: true, checkChars: true);
				if (flag)
				{
					throw ThrowNotSupported(System.SR.XmlBinary_ListsOfValuesNotSupported);
				}
				string stringValue = _stringValue;
				if (stringValue != null)
				{
					_attributes[_attrCount - 1].val = stringValue;
					_stringValue = null;
				}
				if (text != null)
				{
					string ns = _xnt.Add(ValueAsString(binXmlToken));
					PushNamespace(text, ns, implied: false);
					text = null;
				}
				flag = true;
			}
		}
		if (num != -1)
		{
			string attributeText = GetAttributeText(num);
			XmlSpace xmlSpace = XmlSpace.None;
			if (attributeText == "preserve")
			{
				xmlSpace = XmlSpace.Preserve;
			}
			else if (attributeText == "default")
			{
				xmlSpace = XmlSpace.Default;
			}
			_elementStack[_elemDepth].xmlSpace = xmlSpace;
			_xmlspacePreserve = XmlSpace.Preserve == xmlSpace;
		}
		if (num2 != -1)
		{
			_elementStack[_elemDepth].xmlLang = GetAttributeText(num2);
		}
		if (_attrCount < 200)
		{
			SimpleCheckForDuplicateAttributes();
		}
		else
		{
			HashCheckForDuplicateAttributes();
		}
	}

	private void SimpleCheckForDuplicateAttributes()
	{
		for (int i = 0; i < _attrCount; i++)
		{
			_attributes[i].GetLocalnameAndNamespaceUri(out var localname, out var namespaceUri);
			for (int j = i + 1; j < _attrCount; j++)
			{
				if (_attributes[j].MatchNS(localname, namespaceUri))
				{
					throw new XmlException(System.SR.Xml_DupAttributeName, _attributes[i].name.ToString());
				}
			}
		}
	}

	private void HashCheckForDuplicateAttributes()
	{
		int num;
		for (num = 256; num < _attrCount; num = checked(num * 2))
		{
		}
		if (_attrHashTbl.Length < num)
		{
			_attrHashTbl = new int[num];
		}
		for (int i = 0; i < _attrCount; i++)
		{
			string localname;
			string namespaceUri;
			int localnameAndNamespaceUriAndHash = _attributes[i].GetLocalnameAndNamespaceUriAndHash(out localname, out namespaceUri);
			int num2 = localnameAndNamespaceUriAndHash & (num - 1);
			int num3 = _attrHashTbl[num2];
			_attrHashTbl[num2] = i + 1;
			_attributes[i].prevHash = num3;
			while (num3 != 0)
			{
				num3--;
				if (_attributes[num3].MatchHashNS(localnameAndNamespaceUriAndHash, localname, namespaceUri))
				{
					throw new XmlException(System.SR.Xml_DupAttributeName, _attributes[i].name.ToString());
				}
				num3 = _attributes[num3].prevHash;
			}
		}
		Array.Clear(_attrHashTbl, 0, num);
	}

	private string XmlDeclValue()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _attrCount; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(_attributes[i].name.localname);
			stringBuilder.Append("=\"");
			stringBuilder.Append(_attributes[i].val);
			stringBuilder.Append('"');
		}
		return stringBuilder.ToString();
	}

	private string CDATAValue()
	{
		string text = GetString(_tokDataPos, _tokLen);
		StringBuilder stringBuilder = null;
		while (PeekToken() == BinXmlToken.CData)
		{
			_pos++;
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder(text.Length + text.Length / 2);
				stringBuilder.Append(text);
			}
			stringBuilder.Append(ParseText());
		}
		if (stringBuilder != null)
		{
			text = stringBuilder.ToString();
		}
		_stringValue = text;
		return text;
	}

	private void FinishCDATA()
	{
		while (true)
		{
			switch (PeekToken())
			{
			case BinXmlToken.CData:
				break;
			case BinXmlToken.EndCData:
				_pos++;
				return;
			default:
				throw new XmlException(System.SR.XmlBin_MissingEndCDATA);
			}
			_pos++;
			ScanText(out var _);
		}
	}

	private void FinishEndElement()
	{
		NamespaceDecl firstInScopeChain = _elementStack[_elemDepth].Clear();
		PopNamespaces(firstInScopeChain);
		_elemDepth--;
	}

	private bool ReadDoc()
	{
		switch (_nodetype)
		{
		case XmlNodeType.CDATA:
			FinishCDATA();
			break;
		case XmlNodeType.EndElement:
			FinishEndElement();
			break;
		case XmlNodeType.Element:
			if (_isEmpty)
			{
				FinishEndElement();
				_isEmpty = false;
			}
			break;
		}
		while (true)
		{
			_nodetype = XmlNodeType.None;
			_mark = -1;
			if (_qnameOther.localname.Length != 0)
			{
				_qnameOther.Clear();
			}
			ClearAttributes();
			_attrCount = 0;
			_valueType = TypeOfString;
			_stringValue = null;
			_hasTypedValue = false;
			_token = NextToken();
			switch (_token)
			{
			case BinXmlToken.EOF:
				if (_elemDepth > 0)
				{
					throw new XmlException(System.SR.Xml_UnexpectedEOF1, (string[])null);
				}
				_state = ScanState.EOF;
				return false;
			case BinXmlToken.Element:
				ImplReadElement();
				break;
			case BinXmlToken.EndElem:
				ImplReadEndElement();
				break;
			case BinXmlToken.DocType:
				ImplReadDoctype();
				if (_dtdProcessing == DtdProcessing.Ignore || _prevNameInfo != null)
				{
					continue;
				}
				break;
			case BinXmlToken.PI:
				ImplReadPI();
				if (_ignorePIs)
				{
					continue;
				}
				break;
			case BinXmlToken.Comment:
				ImplReadComment();
				if (_ignoreComments)
				{
					continue;
				}
				break;
			case BinXmlToken.CData:
				ImplReadCDATA();
				break;
			case BinXmlToken.Nest:
				ImplReadNest();
				_sniffed = false;
				return ReadInit(skipXmlDecl: true);
			case BinXmlToken.EndNest:
				if (_prevNameInfo != null)
				{
					ImplReadEndNest();
					return ReadDoc();
				}
				goto default;
			case BinXmlToken.XmlText:
				ImplReadXmlText();
				break;
			case BinXmlToken.SQL_SMALLINT:
			case BinXmlToken.SQL_INT:
			case BinXmlToken.SQL_REAL:
			case BinXmlToken.SQL_FLOAT:
			case BinXmlToken.SQL_MONEY:
			case BinXmlToken.SQL_BIT:
			case BinXmlToken.SQL_TINYINT:
			case BinXmlToken.SQL_BIGINT:
			case BinXmlToken.SQL_UUID:
			case BinXmlToken.SQL_DECIMAL:
			case BinXmlToken.SQL_NUMERIC:
			case BinXmlToken.SQL_BINARY:
			case BinXmlToken.SQL_CHAR:
			case BinXmlToken.SQL_NCHAR:
			case BinXmlToken.SQL_VARBINARY:
			case BinXmlToken.SQL_VARCHAR:
			case BinXmlToken.SQL_NVARCHAR:
			case BinXmlToken.SQL_DATETIME:
			case BinXmlToken.SQL_SMALLDATETIME:
			case BinXmlToken.SQL_SMALLMONEY:
			case BinXmlToken.SQL_TEXT:
			case BinXmlToken.SQL_IMAGE:
			case BinXmlToken.SQL_NTEXT:
			case BinXmlToken.SQL_UDT:
			case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			case BinXmlToken.XSD_KATMAI_TIME:
			case BinXmlToken.XSD_KATMAI_DATETIME:
			case BinXmlToken.XSD_KATMAI_DATE:
			case BinXmlToken.XSD_TIME:
			case BinXmlToken.XSD_DATETIME:
			case BinXmlToken.XSD_DATE:
			case BinXmlToken.XSD_BINHEX:
			case BinXmlToken.XSD_BASE64:
			case BinXmlToken.XSD_BOOLEAN:
			case BinXmlToken.XSD_DECIMAL:
			case BinXmlToken.XSD_BYTE:
			case BinXmlToken.XSD_UNSIGNEDSHORT:
			case BinXmlToken.XSD_UNSIGNEDINT:
			case BinXmlToken.XSD_UNSIGNEDLONG:
			case BinXmlToken.XSD_QNAME:
				ImplReadData(_token);
				if (XmlNodeType.Text == _nodetype)
				{
					CheckAllowContent();
				}
				else if (_ignoreWhitespace && !_xmlspacePreserve)
				{
					continue;
				}
				return true;
			default:
				throw ThrowUnexpectedToken(_token);
			}
			break;
		}
		return true;
	}

	private void ImplReadData(BinXmlToken tokenType)
	{
		_mark = _pos;
		switch (tokenType)
		{
		case BinXmlToken.SQL_CHAR:
		case BinXmlToken.SQL_NCHAR:
		case BinXmlToken.SQL_VARCHAR:
		case BinXmlToken.SQL_NVARCHAR:
		case BinXmlToken.SQL_TEXT:
		case BinXmlToken.SQL_NTEXT:
			_valueType = TypeOfString;
			_hasTypedValue = false;
			break;
		default:
			_valueType = GetValueType(_token);
			_hasTypedValue = true;
			break;
		}
		_nodetype = ScanOverValue(_token, attr: false, checkChars: true);
		switch (PeekNextToken())
		{
		case BinXmlToken.SQL_SMALLINT:
		case BinXmlToken.SQL_INT:
		case BinXmlToken.SQL_REAL:
		case BinXmlToken.SQL_FLOAT:
		case BinXmlToken.SQL_MONEY:
		case BinXmlToken.SQL_BIT:
		case BinXmlToken.SQL_TINYINT:
		case BinXmlToken.SQL_BIGINT:
		case BinXmlToken.SQL_UUID:
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.SQL_BINARY:
		case BinXmlToken.SQL_CHAR:
		case BinXmlToken.SQL_NCHAR:
		case BinXmlToken.SQL_VARBINARY:
		case BinXmlToken.SQL_VARCHAR:
		case BinXmlToken.SQL_NVARCHAR:
		case BinXmlToken.SQL_DATETIME:
		case BinXmlToken.SQL_SMALLDATETIME:
		case BinXmlToken.SQL_SMALLMONEY:
		case BinXmlToken.SQL_TEXT:
		case BinXmlToken.SQL_IMAGE:
		case BinXmlToken.SQL_NTEXT:
		case BinXmlToken.SQL_UDT:
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
		case BinXmlToken.XSD_KATMAI_TIME:
		case BinXmlToken.XSD_KATMAI_DATETIME:
		case BinXmlToken.XSD_KATMAI_DATE:
		case BinXmlToken.XSD_TIME:
		case BinXmlToken.XSD_DATETIME:
		case BinXmlToken.XSD_DATE:
		case BinXmlToken.XSD_BINHEX:
		case BinXmlToken.XSD_BASE64:
		case BinXmlToken.XSD_BOOLEAN:
		case BinXmlToken.XSD_DECIMAL:
		case BinXmlToken.XSD_BYTE:
		case BinXmlToken.XSD_UNSIGNEDSHORT:
		case BinXmlToken.XSD_UNSIGNEDINT:
		case BinXmlToken.XSD_UNSIGNEDLONG:
		case BinXmlToken.XSD_QNAME:
			throw ThrowNotSupported(System.SR.XmlBinary_ListsOfValuesNotSupported);
		}
	}

	private void ImplReadElement()
	{
		if (3 != _docState || 9 != _docState)
		{
			switch (_docState)
			{
			case 0:
				_docState = 9;
				break;
			case 1:
			case 2:
				_docState = 3;
				break;
			case -1:
				throw ThrowUnexpectedToken(_token);
			}
		}
		_elemDepth++;
		if (_elemDepth == _elementStack.Length)
		{
			GrowElements();
		}
		QName qName = _symbolTables.qnametable[ReadQNameRef()];
		_qnameOther = (_qnameElement = qName);
		_elementStack[_elemDepth].Set(qName, _xmlspacePreserve);
		PushNamespace(qName.prefix, qName.namespaceUri, implied: true);
		BinXmlToken binXmlToken = PeekNextToken();
		if (BinXmlToken.Attr == binXmlToken)
		{
			ScanAttributes();
			binXmlToken = PeekNextToken();
		}
		GenerateImpliedXmlnsAttrs();
		if (BinXmlToken.EndElem == binXmlToken)
		{
			NextToken();
			_isEmpty = true;
		}
		else if (BinXmlToken.SQL_NVARCHAR == binXmlToken)
		{
			if (_mark < 0)
			{
				_mark = _pos;
			}
			_pos++;
			if (ReadByte() == 0)
			{
				if (247 != ReadByte())
				{
					_pos -= 3;
				}
				else
				{
					_pos--;
				}
			}
			else
			{
				_pos -= 2;
			}
		}
		_nodetype = XmlNodeType.Element;
		_valueType = TypeOfObject;
		_posAfterAttrs = _pos;
	}

	private void ImplReadEndElement()
	{
		if (_elemDepth == 0)
		{
			throw ThrowXmlException(System.SR.Xml_UnexpectedEndTag);
		}
		int elemDepth = _elemDepth;
		if (1 == elemDepth && 3 == _docState)
		{
			_docState = -1;
		}
		_qnameOther = _elementStack[elemDepth].name;
		_xmlspacePreserve = _elementStack[elemDepth].xmlspacePreserve;
		_nodetype = XmlNodeType.EndElement;
	}

	private void ImplReadDoctype()
	{
		if (_dtdProcessing == DtdProcessing.Prohibit)
		{
			throw ThrowXmlException(System.SR.Xml_DtdIsProhibited);
		}
		switch (_docState)
		{
		case 9:
			throw ThrowXmlException(System.SR.Xml_DtdNotAllowedInFragment);
		default:
			throw ThrowXmlException(System.SR.Xml_BadDTDLocation);
		case 0:
		case 1:
			_docState = 2;
			_qnameOther.localname = ParseText();
			if (BinXmlToken.System == PeekToken())
			{
				_pos++;
				_attributes[_attrCount++].Set(new QName(string.Empty, _xnt.Add("SYSTEM"), string.Empty), ParseText());
			}
			if (BinXmlToken.Public == PeekToken())
			{
				_pos++;
				_attributes[_attrCount++].Set(new QName(string.Empty, _xnt.Add("PUBLIC"), string.Empty), ParseText());
			}
			if (BinXmlToken.Subset == PeekToken())
			{
				_pos++;
				_mark = _pos;
				_tokLen = ScanText(out _tokDataPos);
			}
			else
			{
				_tokLen = (_tokDataPos = 0);
			}
			_nodetype = XmlNodeType.DocumentType;
			_posAfterAttrs = _pos;
			break;
		}
	}

	private void ImplReadPI()
	{
		_qnameOther.localname = _symbolTables.symtable[ReadNameRef()];
		_mark = _pos;
		_tokLen = ScanText(out _tokDataPos);
		_nodetype = XmlNodeType.ProcessingInstruction;
	}

	private void ImplReadComment()
	{
		_nodetype = XmlNodeType.Comment;
		_mark = _pos;
		_tokLen = ScanText(out _tokDataPos);
	}

	private void ImplReadCDATA()
	{
		CheckAllowContent();
		_nodetype = XmlNodeType.CDATA;
		_mark = _pos;
		_tokLen = ScanText(out _tokDataPos);
	}

	private void ImplReadNest()
	{
		CheckAllowContent();
		_prevNameInfo = new NestedBinXml(_symbolTables, _docState, _prevNameInfo);
		_symbolTables.Init();
		_docState = 0;
	}

	private void ImplReadEndNest()
	{
		NestedBinXml prevNameInfo = _prevNameInfo;
		_symbolTables = prevNameInfo.symbolTables;
		_docState = prevNameInfo.docState;
		_prevNameInfo = prevNameInfo.next;
	}

	private void ImplReadXmlText()
	{
		CheckAllowContent();
		string xmlFragment = ParseText();
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(_xnt);
		foreach (NamespaceDecl value in _namespaces.Values)
		{
			if (value.scope > 0)
			{
				xmlNamespaceManager.AddNamespace(value.prefix, value.uri);
			}
		}
		XmlReaderSettings settings = Settings;
		settings.ReadOnly = false;
		settings.NameTable = _xnt;
		settings.DtdProcessing = DtdProcessing.Prohibit;
		if (_elemDepth != 0)
		{
			settings.ConformanceLevel = ConformanceLevel.Fragment;
		}
		settings.ReadOnly = true;
		XmlParserContext context = new XmlParserContext(_xnt, xmlNamespaceManager, XmlLang, XmlSpace);
		_textXmlReader = new XmlTextReaderImpl(xmlFragment, context, settings);
		if (!_textXmlReader.Read() || (_textXmlReader.NodeType == XmlNodeType.XmlDeclaration && !_textXmlReader.Read()))
		{
			_state = ScanState.Doc;
			ReadDoc();
		}
		else
		{
			_state = ScanState.XmlText;
			UpdateFromTextReader();
		}
	}

	private void UpdateFromTextReader()
	{
		XmlReader textXmlReader = _textXmlReader;
		_nodetype = textXmlReader.NodeType;
		_qnameOther.prefix = textXmlReader.Prefix;
		_qnameOther.localname = textXmlReader.LocalName;
		_qnameOther.namespaceUri = textXmlReader.NamespaceURI;
		_valueType = textXmlReader.ValueType;
		_isEmpty = textXmlReader.IsEmptyElement;
	}

	private bool UpdateFromTextReader(bool needUpdate)
	{
		if (needUpdate)
		{
			UpdateFromTextReader();
		}
		return needUpdate;
	}

	private void CheckAllowContent()
	{
		switch (_docState)
		{
		case 0:
			_docState = 9;
			break;
		default:
			throw ThrowXmlException(System.SR.Xml_InvalidRootData);
		case 3:
		case 9:
			break;
		}
	}

	private Type[] GenerateTokenTypeMap()
	{
		Type[] array = new Type[256];
		array[134] = typeof(bool);
		array[7] = typeof(byte);
		array[136] = typeof(sbyte);
		array[1] = typeof(short);
		array[137] = typeof(ushort);
		array[138] = typeof(uint);
		array[3] = typeof(float);
		array[4] = typeof(double);
		array[8] = typeof(long);
		array[139] = typeof(ulong);
		array[140] = typeof(XmlQualifiedName);
		array[2] = (array[6] = typeof(int));
		array[135] = (array[11] = (array[10] = (array[5] = (array[20] = typeof(decimal)))));
		array[125] = (array[126] = (array[127] = (array[131] = (array[130] = (array[129] = (array[18] = (array[19] = typeof(DateTime))))))));
		array[122] = (array[123] = (array[124] = typeof(DateTimeOffset)));
		array[133] = (array[132] = (array[27] = (array[23] = (array[12] = (array[15] = typeof(byte[]))))));
		array[13] = TypeOfString;
		array[16] = TypeOfString;
		array[22] = TypeOfString;
		array[14] = TypeOfString;
		array[17] = TypeOfString;
		array[24] = TypeOfString;
		array[9] = TypeOfString;
		return array;
	}

	private Type GetValueType(BinXmlToken token)
	{
		Type type = s_tokenTypeMap[(int)token];
		if (type == null)
		{
			throw ThrowUnexpectedToken(token);
		}
		return type;
	}

	private void ReScanOverValue(BinXmlToken token)
	{
		ScanOverValue(token, attr: true, checkChars: false);
	}

	private XmlNodeType ScanOverValue(BinXmlToken token, bool attr, bool checkChars)
	{
		checked
		{
			if (token == BinXmlToken.SQL_NVARCHAR)
			{
				if (_mark < 0)
				{
					_mark = _pos;
				}
				_tokLen = ParseMB32();
				_tokDataPos = _pos;
				_pos += _tokLen * 2;
				Fill(-1);
				if (checkChars && _checkCharacters)
				{
					return CheckText(attr);
				}
				if (!attr)
				{
					return CheckTextIsWS();
				}
				return XmlNodeType.Text;
			}
			return ScanOverAnyValue(token, attr, checkChars);
		}
	}

	private XmlNodeType ScanOverAnyValue(BinXmlToken token, bool attr, bool checkChars)
	{
		if (_mark < 0)
		{
			_mark = _pos;
		}
		checked
		{
			switch (token)
			{
			case BinXmlToken.SQL_BIT:
			case BinXmlToken.SQL_TINYINT:
			case BinXmlToken.XSD_BOOLEAN:
			case BinXmlToken.XSD_BYTE:
				_tokDataPos = _pos;
				_tokLen = 1;
				_pos++;
				break;
			case BinXmlToken.SQL_SMALLINT:
			case BinXmlToken.XSD_UNSIGNEDSHORT:
				_tokDataPos = _pos;
				_tokLen = 2;
				_pos += 2;
				break;
			case BinXmlToken.SQL_INT:
			case BinXmlToken.SQL_REAL:
			case BinXmlToken.SQL_SMALLDATETIME:
			case BinXmlToken.SQL_SMALLMONEY:
			case BinXmlToken.XSD_UNSIGNEDINT:
				_tokDataPos = _pos;
				_tokLen = 4;
				_pos += 4;
				break;
			case BinXmlToken.SQL_FLOAT:
			case BinXmlToken.SQL_MONEY:
			case BinXmlToken.SQL_BIGINT:
			case BinXmlToken.SQL_DATETIME:
			case BinXmlToken.XSD_TIME:
			case BinXmlToken.XSD_DATETIME:
			case BinXmlToken.XSD_DATE:
			case BinXmlToken.XSD_UNSIGNEDLONG:
				_tokDataPos = _pos;
				_tokLen = 8;
				_pos += 8;
				break;
			case BinXmlToken.SQL_UUID:
				_tokDataPos = _pos;
				_tokLen = 16;
				_pos += 16;
				break;
			case BinXmlToken.SQL_DECIMAL:
			case BinXmlToken.SQL_NUMERIC:
			case BinXmlToken.XSD_DECIMAL:
				_tokDataPos = _pos;
				_tokLen = ParseMB64();
				_pos += _tokLen;
				break;
			case BinXmlToken.SQL_BINARY:
			case BinXmlToken.SQL_VARBINARY:
			case BinXmlToken.SQL_IMAGE:
			case BinXmlToken.SQL_UDT:
			case BinXmlToken.XSD_BINHEX:
			case BinXmlToken.XSD_BASE64:
				_tokLen = ParseMB64();
				_tokDataPos = _pos;
				_pos += _tokLen;
				break;
			case BinXmlToken.SQL_CHAR:
			case BinXmlToken.SQL_VARCHAR:
			case BinXmlToken.SQL_TEXT:
				_tokLen = ParseMB64();
				_tokDataPos = _pos;
				_pos += _tokLen;
				if (checkChars && _checkCharacters)
				{
					Fill(-1);
					string text = ValueAsString(token);
					XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException, ExceptionType.XmlException);
					_stringValue = text;
				}
				break;
			case BinXmlToken.SQL_NCHAR:
			case BinXmlToken.SQL_NVARCHAR:
			case BinXmlToken.SQL_NTEXT:
				return ScanOverValue(BinXmlToken.SQL_NVARCHAR, attr, checkChars);
			case BinXmlToken.XSD_QNAME:
				_tokDataPos = _pos;
				ParseMB32();
				break;
			case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			case BinXmlToken.XSD_KATMAI_TIME:
			case BinXmlToken.XSD_KATMAI_DATETIME:
			case BinXmlToken.XSD_KATMAI_DATE:
				VerifyVersion(2, token);
				_tokDataPos = _pos;
				_tokLen = GetXsdKatmaiTokenLength(token);
				_pos += _tokLen;
				break;
			default:
				throw ThrowUnexpectedToken(token);
			}
			Fill(-1);
			return XmlNodeType.Text;
		}
	}

	private XmlNodeType CheckText(bool attr)
	{
		ReadOnlySpan<byte> source = _data.AsSpan(_tokDataPos, _end - _tokDataPos);
		if (!attr)
		{
			while (true)
			{
				if (!BinaryPrimitives.TryReadUInt16LittleEndian(source, out var value))
				{
					if (!_xmlspacePreserve)
					{
						return XmlNodeType.Whitespace;
					}
					return XmlNodeType.SignificantWhitespace;
				}
				if (value > 255 || !XmlCharType.IsWhiteSpace((char)value))
				{
					break;
				}
				source = source.Slice(2);
			}
		}
		char c;
		ushort value3;
		while (true)
		{
			if (!BinaryPrimitives.TryReadUInt16LittleEndian(source, out var value2))
			{
				return XmlNodeType.Text;
			}
			source = source.Slice(2);
			c = (char)value2;
			if (!XmlCharType.IsCharData(c))
			{
				if (!XmlCharType.IsHighSurrogate(c))
				{
					throw XmlConvert.CreateInvalidCharException(c, '\0', ExceptionType.XmlException);
				}
				if (!BinaryPrimitives.TryReadUInt16LittleEndian(source, out value3))
				{
					throw ThrowXmlException(System.SR.Xml_InvalidSurrogateMissingLowChar);
				}
				if (!XmlCharType.IsLowSurrogate(value3))
				{
					break;
				}
				source = source.Slice(2);
			}
		}
		throw XmlConvert.CreateInvalidSurrogatePairException(c, (char)value3);
	}

	private XmlNodeType CheckTextIsWS()
	{
		byte[] data = _data;
		int num = _tokDataPos;
		while (true)
		{
			if (num < _pos)
			{
				if (data[num + 1] != 0)
				{
					break;
				}
				byte b = data[num];
				if ((uint)(b - 9) > 1u && b != 13 && b != 32)
				{
					break;
				}
				num += 2;
				continue;
			}
			if (_xmlspacePreserve)
			{
				return XmlNodeType.SignificantWhitespace;
			}
			return XmlNodeType.Whitespace;
		}
		return XmlNodeType.Text;
	}

	private void CheckValueTokenBounds()
	{
		if (_end - _tokDataPos < _tokLen)
		{
			throw ThrowXmlException(System.SR.Xml_UnexpectedEOF1);
		}
	}

	private int GetXsdKatmaiTokenLength(BinXmlToken token)
	{
		switch (token)
		{
		case BinXmlToken.XSD_KATMAI_DATE:
			return 3;
		case BinXmlToken.XSD_KATMAI_TIME:
		case BinXmlToken.XSD_KATMAI_DATETIME:
		{
			Fill(0);
			byte scale = _data[_pos];
			return 4 + XsdKatmaiTimeScaleToValueLength(scale);
		}
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
		{
			Fill(0);
			byte scale = _data[_pos];
			return 6 + XsdKatmaiTimeScaleToValueLength(scale);
		}
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private int XsdKatmaiTimeScaleToValueLength(byte scale)
	{
		if (scale > 7)
		{
			throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
		}
		return XsdKatmaiTimeScaleToValueLengthMap[scale];
	}

	private long ValueAsLong()
	{
		CheckValueTokenBounds();
		switch (_token)
		{
		case BinXmlToken.SQL_BIT:
		case BinXmlToken.SQL_TINYINT:
		{
			byte b2 = _data[_tokDataPos];
			return b2;
		}
		case BinXmlToken.XSD_BYTE:
		{
			sbyte b = (sbyte)_data[_tokDataPos];
			return b;
		}
		case BinXmlToken.SQL_SMALLINT:
			return GetInt16(_tokDataPos);
		case BinXmlToken.SQL_INT:
			return GetInt32(_tokDataPos);
		case BinXmlToken.SQL_BIGINT:
			return GetInt64(_tokDataPos);
		case BinXmlToken.XSD_UNSIGNEDSHORT:
			return GetUInt16(_tokDataPos);
		case BinXmlToken.XSD_UNSIGNEDINT:
			return GetUInt32(_tokDataPos);
		case BinXmlToken.XSD_UNSIGNEDLONG:
		{
			ulong uInt = GetUInt64(_tokDataPos);
			return checked((long)uInt);
		}
		case BinXmlToken.SQL_REAL:
		case BinXmlToken.SQL_FLOAT:
		{
			double num2 = ValueAsDouble();
			return (long)num2;
		}
		case BinXmlToken.SQL_MONEY:
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.SQL_SMALLMONEY:
		case BinXmlToken.XSD_DECIMAL:
		{
			decimal num = ValueAsDecimal();
			return (long)num;
		}
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private ulong ValueAsULong()
	{
		if (BinXmlToken.XSD_UNSIGNEDLONG == _token)
		{
			CheckValueTokenBounds();
			return GetUInt64(_tokDataPos);
		}
		throw ThrowUnexpectedToken(_token);
	}

	private decimal ValueAsDecimal()
	{
		CheckValueTokenBounds();
		switch (_token)
		{
		case BinXmlToken.SQL_SMALLINT:
		case BinXmlToken.SQL_INT:
		case BinXmlToken.SQL_BIT:
		case BinXmlToken.SQL_TINYINT:
		case BinXmlToken.SQL_BIGINT:
		case BinXmlToken.XSD_BYTE:
		case BinXmlToken.XSD_UNSIGNEDSHORT:
		case BinXmlToken.XSD_UNSIGNEDINT:
			return new decimal(ValueAsLong());
		case BinXmlToken.XSD_UNSIGNEDLONG:
			return new decimal(ValueAsULong());
		case BinXmlToken.SQL_REAL:
			return new decimal(GetSingle(_tokDataPos));
		case BinXmlToken.SQL_FLOAT:
			return new decimal(GetDouble(_tokDataPos));
		case BinXmlToken.SQL_SMALLMONEY:
			return new BinXmlSqlMoney(GetInt32(_tokDataPos)).ToDecimal();
		case BinXmlToken.SQL_MONEY:
			return new BinXmlSqlMoney(GetInt64(_tokDataPos)).ToDecimal();
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.XSD_DECIMAL:
			return new BinXmlSqlDecimal(_data, _tokDataPos, _token == BinXmlToken.XSD_DECIMAL).ToDecimal();
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private double ValueAsDouble()
	{
		CheckValueTokenBounds();
		switch (_token)
		{
		case BinXmlToken.SQL_SMALLINT:
		case BinXmlToken.SQL_INT:
		case BinXmlToken.SQL_BIT:
		case BinXmlToken.SQL_TINYINT:
		case BinXmlToken.SQL_BIGINT:
		case BinXmlToken.XSD_BYTE:
		case BinXmlToken.XSD_UNSIGNEDSHORT:
		case BinXmlToken.XSD_UNSIGNEDINT:
			return ValueAsLong();
		case BinXmlToken.XSD_UNSIGNEDLONG:
			return ValueAsULong();
		case BinXmlToken.SQL_REAL:
			return GetSingle(_tokDataPos);
		case BinXmlToken.SQL_FLOAT:
			return GetDouble(_tokDataPos);
		case BinXmlToken.SQL_MONEY:
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.SQL_SMALLMONEY:
		case BinXmlToken.XSD_DECIMAL:
			return (double)ValueAsDecimal();
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private DateTime ValueAsDateTime()
	{
		CheckValueTokenBounds();
		switch (_token)
		{
		case BinXmlToken.SQL_DATETIME:
		{
			int tokDataPos2 = _tokDataPos;
			int int5 = GetInt32(tokDataPos2);
			uint uInt2 = GetUInt32(tokDataPos2 + 4);
			return BinXmlDateTime.SqlDateTimeToDateTime(int5, uInt2);
		}
		case BinXmlToken.SQL_SMALLDATETIME:
		{
			int tokDataPos = _tokDataPos;
			short int4 = GetInt16(tokDataPos);
			ushort uInt = GetUInt16(tokDataPos + 2);
			return BinXmlDateTime.SqlSmallDateTimeToDateTime(int4, uInt);
		}
		case BinXmlToken.XSD_TIME:
		{
			long int3 = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdTimeToDateTime(int3);
		}
		case BinXmlToken.XSD_DATE:
		{
			long int2 = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdDateToDateTime(int2);
		}
		case BinXmlToken.XSD_DATETIME:
		{
			long @int = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdDateTimeToDateTime(@int);
		}
		case BinXmlToken.XSD_KATMAI_DATE:
			return BinXmlDateTime.XsdKatmaiDateToDateTime(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATETIME:
			return BinXmlDateTime.XsdKatmaiDateTimeToDateTime(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_TIME:
			return BinXmlDateTime.XsdKatmaiTimeToDateTime(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			return BinXmlDateTime.XsdKatmaiDateOffsetToDateTime(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
			return BinXmlDateTime.XsdKatmaiDateTimeOffsetToDateTime(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
			return BinXmlDateTime.XsdKatmaiTimeOffsetToDateTime(_data, _tokDataPos);
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private DateTimeOffset ValueAsDateTimeOffset()
	{
		CheckValueTokenBounds();
		return _token switch
		{
			BinXmlToken.XSD_KATMAI_DATEOFFSET => BinXmlDateTime.XsdKatmaiDateOffsetToDateTimeOffset(_data, _tokDataPos), 
			BinXmlToken.XSD_KATMAI_DATETIMEOFFSET => BinXmlDateTime.XsdKatmaiDateTimeOffsetToDateTimeOffset(_data, _tokDataPos), 
			BinXmlToken.XSD_KATMAI_TIMEOFFSET => BinXmlDateTime.XsdKatmaiTimeOffsetToDateTimeOffset(_data, _tokDataPos), 
			_ => throw ThrowUnexpectedToken(_token), 
		};
	}

	private string ValueAsDateTimeString()
	{
		CheckValueTokenBounds();
		switch (_token)
		{
		case BinXmlToken.SQL_DATETIME:
		{
			int tokDataPos2 = _tokDataPos;
			int int5 = GetInt32(tokDataPos2);
			uint uInt2 = GetUInt32(tokDataPos2 + 4);
			return BinXmlDateTime.SqlDateTimeToString(int5, uInt2);
		}
		case BinXmlToken.SQL_SMALLDATETIME:
		{
			int tokDataPos = _tokDataPos;
			short int4 = GetInt16(tokDataPos);
			ushort uInt = GetUInt16(tokDataPos + 2);
			return BinXmlDateTime.SqlSmallDateTimeToString(int4, uInt);
		}
		case BinXmlToken.XSD_TIME:
		{
			long int3 = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdTimeToString(int3);
		}
		case BinXmlToken.XSD_DATE:
		{
			long int2 = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdDateToString(int2);
		}
		case BinXmlToken.XSD_DATETIME:
		{
			long @int = GetInt64(_tokDataPos);
			return BinXmlDateTime.XsdDateTimeToString(@int);
		}
		case BinXmlToken.XSD_KATMAI_DATE:
			return BinXmlDateTime.XsdKatmaiDateToString(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATETIME:
			return BinXmlDateTime.XsdKatmaiDateTimeToString(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_TIME:
			return BinXmlDateTime.XsdKatmaiTimeToString(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			return BinXmlDateTime.XsdKatmaiDateOffsetToString(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
			return BinXmlDateTime.XsdKatmaiDateTimeOffsetToString(_data, _tokDataPos);
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
			return BinXmlDateTime.XsdKatmaiTimeOffsetToString(_data, _tokDataPos);
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private string ValueAsString(BinXmlToken token)
	{
		try
		{
			CheckValueTokenBounds();
			switch (token)
			{
			case BinXmlToken.SQL_NCHAR:
			case BinXmlToken.SQL_NVARCHAR:
			case BinXmlToken.SQL_NTEXT:
				return GetString(_tokDataPos, _tokLen);
			case BinXmlToken.XSD_BOOLEAN:
				if (_data[_tokDataPos] == 0)
				{
					return "false";
				}
				return "true";
			case BinXmlToken.SQL_SMALLINT:
			case BinXmlToken.SQL_INT:
			case BinXmlToken.SQL_BIT:
			case BinXmlToken.SQL_TINYINT:
			case BinXmlToken.SQL_BIGINT:
			case BinXmlToken.XSD_BYTE:
			case BinXmlToken.XSD_UNSIGNEDSHORT:
			case BinXmlToken.XSD_UNSIGNEDINT:
				return ValueAsLong().ToString(CultureInfo.InvariantCulture);
			case BinXmlToken.XSD_UNSIGNEDLONG:
				return ValueAsULong().ToString(CultureInfo.InvariantCulture);
			case BinXmlToken.SQL_REAL:
				return XmlConvert.ToString(GetSingle(_tokDataPos));
			case BinXmlToken.SQL_FLOAT:
				return XmlConvert.ToString(GetDouble(_tokDataPos));
			case BinXmlToken.SQL_UUID:
			{
				int tokDataPos2 = _tokDataPos;
				int int2 = GetInt32(tokDataPos2);
				short int3 = GetInt16(tokDataPos2 + 4);
				short int4 = GetInt16(tokDataPos2 + 6);
				return new Guid(int2, int3, int4, _data[tokDataPos2 + 8], _data[tokDataPos2 + 9], _data[tokDataPos2 + 10], _data[tokDataPos2 + 11], _data[tokDataPos2 + 12], _data[tokDataPos2 + 13], _data[tokDataPos2 + 14], _data[tokDataPos2 + 15]).ToString();
			}
			case BinXmlToken.SQL_SMALLMONEY:
				return new BinXmlSqlMoney(GetInt32(_tokDataPos)).ToString();
			case BinXmlToken.SQL_MONEY:
				return new BinXmlSqlMoney(GetInt64(_tokDataPos)).ToString();
			case BinXmlToken.SQL_DECIMAL:
			case BinXmlToken.SQL_NUMERIC:
			case BinXmlToken.XSD_DECIMAL:
				return new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL).ToString();
			case BinXmlToken.SQL_CHAR:
			case BinXmlToken.SQL_VARCHAR:
			case BinXmlToken.SQL_TEXT:
			{
				int tokDataPos = _tokDataPos;
				int @int = GetInt32(tokDataPos);
				Encoding encoding = Encoding.GetEncoding(@int);
				return encoding.GetString(_data, tokDataPos + 4, _tokLen - 4);
			}
			case BinXmlToken.SQL_BINARY:
			case BinXmlToken.SQL_VARBINARY:
			case BinXmlToken.SQL_IMAGE:
			case BinXmlToken.SQL_UDT:
			case BinXmlToken.XSD_BASE64:
				return Convert.ToBase64String(_data, _tokDataPos, _tokLen);
			case BinXmlToken.XSD_BINHEX:
				return BinHexEncoder.Encode(_data, _tokDataPos, _tokLen);
			case BinXmlToken.SQL_DATETIME:
			case BinXmlToken.SQL_SMALLDATETIME:
			case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
			case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			case BinXmlToken.XSD_KATMAI_TIME:
			case BinXmlToken.XSD_KATMAI_DATETIME:
			case BinXmlToken.XSD_KATMAI_DATE:
			case BinXmlToken.XSD_TIME:
			case BinXmlToken.XSD_DATETIME:
			case BinXmlToken.XSD_DATE:
				return ValueAsDateTimeString();
			case BinXmlToken.XSD_QNAME:
			{
				int num = ParseMB32(_tokDataPos);
				if (num < 0 || num >= _symbolTables.qnameCount)
				{
					throw new XmlException(System.SR.XmlBin_InvalidQNameID, string.Empty);
				}
				QName qName = _symbolTables.qnametable[num];
				if (qName.prefix.Length == 0)
				{
					return qName.localname;
				}
				return qName.prefix + ":" + qName.localname;
			}
			default:
				throw ThrowUnexpectedToken(_token);
			}
		}
		catch
		{
			_state = ScanState.Error;
			throw;
		}
	}

	private object ValueAsObject(BinXmlToken token, bool returnInternalTypes)
	{
		CheckValueTokenBounds();
		switch (token)
		{
		case BinXmlToken.SQL_NCHAR:
		case BinXmlToken.SQL_NVARCHAR:
		case BinXmlToken.SQL_NTEXT:
			return GetString(_tokDataPos, _tokLen);
		case BinXmlToken.XSD_BOOLEAN:
			return _data[_tokDataPos] != 0;
		case BinXmlToken.SQL_BIT:
			return (int)_data[_tokDataPos];
		case BinXmlToken.SQL_TINYINT:
			return _data[_tokDataPos];
		case BinXmlToken.SQL_SMALLINT:
			return GetInt16(_tokDataPos);
		case BinXmlToken.SQL_INT:
			return GetInt32(_tokDataPos);
		case BinXmlToken.SQL_BIGINT:
			return GetInt64(_tokDataPos);
		case BinXmlToken.XSD_BYTE:
		{
			sbyte b = (sbyte)_data[_tokDataPos];
			return b;
		}
		case BinXmlToken.XSD_UNSIGNEDSHORT:
			return GetUInt16(_tokDataPos);
		case BinXmlToken.XSD_UNSIGNEDINT:
			return GetUInt32(_tokDataPos);
		case BinXmlToken.XSD_UNSIGNEDLONG:
			return GetUInt64(_tokDataPos);
		case BinXmlToken.SQL_REAL:
			return GetSingle(_tokDataPos);
		case BinXmlToken.SQL_FLOAT:
			return GetDouble(_tokDataPos);
		case BinXmlToken.SQL_UUID:
		{
			int tokDataPos2 = _tokDataPos;
			int int2 = GetInt32(tokDataPos2);
			short int3 = GetInt16(tokDataPos2 + 4);
			short int4 = GetInt16(tokDataPos2 + 6);
			return new Guid(int2, int3, int4, _data[tokDataPos2 + 8], _data[tokDataPos2 + 9], _data[tokDataPos2 + 10], _data[tokDataPos2 + 11], _data[tokDataPos2 + 12], _data[tokDataPos2 + 13], _data[tokDataPos2 + 14], _data[tokDataPos2 + 15]).ToString();
		}
		case BinXmlToken.SQL_SMALLMONEY:
		{
			BinXmlSqlMoney binXmlSqlMoney2 = new BinXmlSqlMoney(GetInt32(_tokDataPos));
			if (returnInternalTypes)
			{
				return binXmlSqlMoney2;
			}
			return binXmlSqlMoney2.ToDecimal();
		}
		case BinXmlToken.SQL_MONEY:
		{
			BinXmlSqlMoney binXmlSqlMoney = new BinXmlSqlMoney(GetInt64(_tokDataPos));
			if (returnInternalTypes)
			{
				return binXmlSqlMoney;
			}
			return binXmlSqlMoney.ToDecimal();
		}
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.XSD_DECIMAL:
		{
			BinXmlSqlDecimal binXmlSqlDecimal = new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL);
			if (returnInternalTypes)
			{
				return binXmlSqlDecimal;
			}
			return binXmlSqlDecimal.ToDecimal();
		}
		case BinXmlToken.SQL_CHAR:
		case BinXmlToken.SQL_VARCHAR:
		case BinXmlToken.SQL_TEXT:
		{
			int tokDataPos = _tokDataPos;
			int @int = GetInt32(tokDataPos);
			Encoding encoding = Encoding.GetEncoding(@int);
			return encoding.GetString(_data, tokDataPos + 4, _tokLen - 4);
		}
		case BinXmlToken.SQL_BINARY:
		case BinXmlToken.SQL_VARBINARY:
		case BinXmlToken.SQL_IMAGE:
		case BinXmlToken.SQL_UDT:
		case BinXmlToken.XSD_BINHEX:
		case BinXmlToken.XSD_BASE64:
		{
			byte[] array = new byte[_tokLen];
			Array.Copy(_data, _tokDataPos, array, 0, _tokLen);
			return array;
		}
		case BinXmlToken.SQL_DATETIME:
		case BinXmlToken.SQL_SMALLDATETIME:
		case BinXmlToken.XSD_KATMAI_TIME:
		case BinXmlToken.XSD_KATMAI_DATETIME:
		case BinXmlToken.XSD_KATMAI_DATE:
		case BinXmlToken.XSD_TIME:
		case BinXmlToken.XSD_DATETIME:
		case BinXmlToken.XSD_DATE:
			return ValueAsDateTime();
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			return ValueAsDateTimeOffset();
		case BinXmlToken.XSD_QNAME:
		{
			int num = ParseMB32(_tokDataPos);
			if (num < 0 || num >= _symbolTables.qnameCount)
			{
				throw new XmlException(System.SR.XmlBin_InvalidQNameID, string.Empty);
			}
			QName qName = _symbolTables.qnametable[num];
			return new XmlQualifiedName(qName.localname, qName.namespaceUri);
		}
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private XmlValueConverter GetValueConverter(XmlTypeCode typeCode)
	{
		XmlSchemaSimpleType simpleTypeFromTypeCode = DatatypeImplementation.GetSimpleTypeFromTypeCode(typeCode);
		return simpleTypeFromTypeCode.ValueConverter;
	}

	private object ValueAs(BinXmlToken token, Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		CheckValueTokenBounds();
		switch (token)
		{
		case BinXmlToken.SQL_NCHAR:
		case BinXmlToken.SQL_NVARCHAR:
		case BinXmlToken.SQL_NTEXT:
			return GetValueConverter(XmlTypeCode.UntypedAtomic).ChangeType(GetString(_tokDataPos, _tokLen), returnType, namespaceResolver);
		case BinXmlToken.XSD_BOOLEAN:
			return GetValueConverter(XmlTypeCode.Boolean).ChangeType(_data[_tokDataPos] != 0, returnType, namespaceResolver);
		case BinXmlToken.SQL_BIT:
			return GetValueConverter(XmlTypeCode.NonNegativeInteger).ChangeType((int)_data[_tokDataPos], returnType, namespaceResolver);
		case BinXmlToken.SQL_TINYINT:
			return GetValueConverter(XmlTypeCode.UnsignedByte).ChangeType(_data[_tokDataPos], returnType, namespaceResolver);
		case BinXmlToken.SQL_SMALLINT:
		{
			int @int = GetInt16(_tokDataPos);
			return GetValueConverter(XmlTypeCode.Short).ChangeType(@int, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_INT:
		{
			int int2 = GetInt32(_tokDataPos);
			return GetValueConverter(XmlTypeCode.Int).ChangeType(int2, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_BIGINT:
		{
			long int4 = GetInt64(_tokDataPos);
			return GetValueConverter(XmlTypeCode.Long).ChangeType(int4, returnType, namespaceResolver);
		}
		case BinXmlToken.XSD_BYTE:
			return GetValueConverter(XmlTypeCode.Byte).ChangeType((int)(sbyte)_data[_tokDataPos], returnType, namespaceResolver);
		case BinXmlToken.XSD_UNSIGNEDSHORT:
		{
			int uInt = GetUInt16(_tokDataPos);
			return GetValueConverter(XmlTypeCode.UnsignedShort).ChangeType(uInt, returnType, namespaceResolver);
		}
		case BinXmlToken.XSD_UNSIGNEDINT:
		{
			long num3 = GetUInt32(_tokDataPos);
			return GetValueConverter(XmlTypeCode.UnsignedInt).ChangeType(num3, returnType, namespaceResolver);
		}
		case BinXmlToken.XSD_UNSIGNEDLONG:
		{
			decimal num2 = GetUInt64(_tokDataPos);
			return GetValueConverter(XmlTypeCode.UnsignedLong).ChangeType(num2, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_REAL:
		{
			float single = GetSingle(_tokDataPos);
			return GetValueConverter(XmlTypeCode.Float).ChangeType(single, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_FLOAT:
		{
			double @double = GetDouble(_tokDataPos);
			return GetValueConverter(XmlTypeCode.Double).ChangeType(@double, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_UUID:
			return GetValueConverter(XmlTypeCode.String).ChangeType(ValueAsString(token), returnType, namespaceResolver);
		case BinXmlToken.SQL_SMALLMONEY:
			return GetValueConverter(XmlTypeCode.Decimal).ChangeType(new BinXmlSqlMoney(GetInt32(_tokDataPos)).ToDecimal(), returnType, namespaceResolver);
		case BinXmlToken.SQL_MONEY:
			return GetValueConverter(XmlTypeCode.Decimal).ChangeType(new BinXmlSqlMoney(GetInt64(_tokDataPos)).ToDecimal(), returnType, namespaceResolver);
		case BinXmlToken.SQL_DECIMAL:
		case BinXmlToken.SQL_NUMERIC:
		case BinXmlToken.XSD_DECIMAL:
			return GetValueConverter(XmlTypeCode.Decimal).ChangeType(new BinXmlSqlDecimal(_data, _tokDataPos, token == BinXmlToken.XSD_DECIMAL).ToDecimal(), returnType, namespaceResolver);
		case BinXmlToken.SQL_CHAR:
		case BinXmlToken.SQL_VARCHAR:
		case BinXmlToken.SQL_TEXT:
		{
			int tokDataPos = _tokDataPos;
			int int3 = GetInt32(tokDataPos);
			Encoding encoding = Encoding.GetEncoding(int3);
			return GetValueConverter(XmlTypeCode.UntypedAtomic).ChangeType(encoding.GetString(_data, tokDataPos + 4, _tokLen - 4), returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_BINARY:
		case BinXmlToken.SQL_VARBINARY:
		case BinXmlToken.SQL_IMAGE:
		case BinXmlToken.SQL_UDT:
		case BinXmlToken.XSD_BINHEX:
		case BinXmlToken.XSD_BASE64:
		{
			byte[] array = new byte[_tokLen];
			Array.Copy(_data, _tokDataPos, array, 0, _tokLen);
			return GetValueConverter((token == BinXmlToken.XSD_BINHEX) ? XmlTypeCode.HexBinary : XmlTypeCode.Base64Binary).ChangeType(array, returnType, namespaceResolver);
		}
		case BinXmlToken.SQL_DATETIME:
		case BinXmlToken.SQL_SMALLDATETIME:
		case BinXmlToken.XSD_KATMAI_TIME:
		case BinXmlToken.XSD_KATMAI_DATETIME:
		case BinXmlToken.XSD_KATMAI_DATE:
		case BinXmlToken.XSD_DATETIME:
			return GetValueConverter(XmlTypeCode.DateTime).ChangeType(ValueAsDateTime(), returnType, namespaceResolver);
		case BinXmlToken.XSD_KATMAI_TIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATETIMEOFFSET:
		case BinXmlToken.XSD_KATMAI_DATEOFFSET:
			return GetValueConverter(XmlTypeCode.DateTime).ChangeType(ValueAsDateTimeOffset(), returnType, namespaceResolver);
		case BinXmlToken.XSD_TIME:
			return GetValueConverter(XmlTypeCode.Time).ChangeType(ValueAsDateTime(), returnType, namespaceResolver);
		case BinXmlToken.XSD_DATE:
			return GetValueConverter(XmlTypeCode.Date).ChangeType(ValueAsDateTime(), returnType, namespaceResolver);
		case BinXmlToken.XSD_QNAME:
		{
			int num = ParseMB32(_tokDataPos);
			if (num < 0 || num >= _symbolTables.qnameCount)
			{
				throw new XmlException(System.SR.XmlBin_InvalidQNameID, string.Empty);
			}
			QName qName = _symbolTables.qnametable[num];
			return GetValueConverter(XmlTypeCode.QName).ChangeType(new XmlQualifiedName(qName.localname, qName.namespaceUri), returnType, namespaceResolver);
		}
		default:
			throw ThrowUnexpectedToken(_token);
		}
	}

	private short GetInt16(int pos)
	{
		return BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(pos));
	}

	private ushort GetUInt16(int pos)
	{
		return BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(pos));
	}

	private int GetInt32(int pos)
	{
		return BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(pos));
	}

	private uint GetUInt32(int pos)
	{
		return BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(pos));
	}

	private long GetInt64(int pos)
	{
		return BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(pos));
	}

	private ulong GetUInt64(int pos)
	{
		return BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(pos));
	}

	private float GetSingle(int offset)
	{
		return BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(offset));
	}

	private double GetDouble(int offset)
	{
		return BinaryPrimitives.ReadDoubleLittleEndian(_data.AsSpan(offset));
	}

	private Exception ThrowUnexpectedToken(BinXmlToken token)
	{
		return ThrowXmlException(System.SR.XmlBinary_UnexpectedToken);
	}

	private Exception ThrowXmlException(string res)
	{
		_state = ScanState.Error;
		return new XmlException(res, (string[])null);
	}

	private Exception ThrowXmlException(string res, string arg1, string arg2)
	{
		_state = ScanState.Error;
		return new XmlException(res, new string[2] { arg1, arg2 });
	}

	private Exception ThrowNotSupported(string res)
	{
		_state = ScanState.Error;
		return new NotSupportedException(res);
	}
}
