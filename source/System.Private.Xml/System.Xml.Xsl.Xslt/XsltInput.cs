using System.Collections.Generic;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Xslt;

internal sealed class XsltInput : IErrorHelper
{
	public struct DelayedQName
	{
		private readonly string _prefix;

		private readonly string _localName;

		public DelayedQName(ref Record rec)
		{
			_prefix = rec.prefix;
			_localName = rec.localName;
		}

		public static implicit operator string(DelayedQName qn)
		{
			if (qn._prefix.Length != 0)
			{
				return qn._prefix + ":" + qn._localName;
			}
			return qn._localName;
		}
	}

	public struct XsltAttribute
	{
		public string name;

		public int flags;

		public XsltAttribute(string name, int flags)
		{
			this.name = name;
			this.flags = flags;
		}
	}

	internal sealed class ContextInfo
	{
		internal sealed class EmptyElementEndTag : ISourceLineInfo
		{
			private readonly ISourceLineInfo _elementTagLi;

			public string Uri => _elementTagLi.Uri;

			public bool IsNoSource => _elementTagLi.IsNoSource;

			public Location Start => new Location(_elementTagLi.End.Line, _elementTagLi.End.Pos - 2);

			public Location End => _elementTagLi.End;

			public EmptyElementEndTag(ISourceLineInfo elementTagLi)
			{
				_elementTagLi = elementTagLi;
			}
		}

		public NsDecl nsList;

		public ISourceLineInfo lineInfo;

		public ISourceLineInfo elemNameLi;

		public ISourceLineInfo endTagLi;

		private readonly int _elemNameLength;

		internal ContextInfo(ISourceLineInfo lineinfo)
		{
			elemNameLi = lineinfo;
			endTagLi = lineinfo;
			lineInfo = lineinfo;
		}

		public ContextInfo(XsltInput input)
		{
			_elemNameLength = input.QualifiedName.Length;
		}

		public void AddNamespace(string prefix, string nsUri)
		{
			nsList = new NsDecl(nsList, prefix, nsUri);
		}

		public void SaveExtendedLineInfo(XsltInput input)
		{
			if (lineInfo.Start.Line == 0)
			{
				elemNameLi = (endTagLi = null);
				return;
			}
			elemNameLi = new SourceLineInfo(lineInfo.Uri, lineInfo.Start.Line, lineInfo.Start.Pos + 1, lineInfo.Start.Line, lineInfo.Start.Pos + 1 + _elemNameLength);
			if (!input.IsEmptyElement)
			{
				endTagLi = input.BuildLineInfo();
			}
			else
			{
				endTagLi = new EmptyElementEndTag(lineInfo);
			}
		}
	}

	internal struct Record
	{
		public string localName;

		public string nsUri;

		public string prefix;

		public string value;

		public string baseUri;

		public Location start;

		public Location valueStart;

		public Location end;

		public string QualifiedName
		{
			get
			{
				if (prefix.Length != 0)
				{
					return prefix + ":" + localName;
				}
				return localName;
			}
		}
	}

	private readonly XmlReader _reader;

	private readonly IXmlLineInfo _readerLineInfo;

	private readonly bool _topLevelReader;

	private readonly CompilerScopeManager<VarPar> _scopeManager;

	private readonly KeywordsTable _atoms;

	private readonly Compiler _compiler;

	private readonly bool _reatomize;

	private XmlNodeType _nodeType;

	private Record[] _records = new Record[22];

	private int _currentRecord;

	private bool _isEmptyElement;

	private int _lastTextNode;

	private int _numAttributes;

	private ContextInfo _ctxInfo;

	private bool _attributesRead;

	private StringConcat _strConcat;

	private XsltAttribute[] _attributes;

	private readonly int[] _xsltAttributeNumber = new int[21];

	public XmlNodeType NodeType
	{
		get
		{
			if (_nodeType != XmlNodeType.Element || 0 >= _currentRecord)
			{
				return _nodeType;
			}
			return XmlNodeType.Attribute;
		}
	}

	public string LocalName => _records[_currentRecord].localName;

	public string NamespaceUri => _records[_currentRecord].nsUri;

	public string Prefix => _records[_currentRecord].prefix;

	public string Value => _records[_currentRecord].value;

	public string BaseUri => _records[_currentRecord].baseUri;

	public string QualifiedName => _records[_currentRecord].QualifiedName;

	public bool IsEmptyElement => _isEmptyElement;

	public string Uri => _records[_currentRecord].baseUri;

	public Location Start => _records[_currentRecord].start;

	public Location End => _records[_currentRecord].end;

	public DelayedQName ElementName => new DelayedQName(ref _records[0]);

	public bool CanHaveApplyImports
	{
		get
		{
			return _scopeManager.CanHaveApplyImports;
		}
		set
		{
			_scopeManager.CanHaveApplyImports = value;
		}
	}

	public bool ForwardCompatibility => _scopeManager.ForwardCompatibility;

	public bool BackwardCompatibility => _scopeManager.BackwardCompatibility;

	public XslVersion XslVersion
	{
		get
		{
			if (!_scopeManager.ForwardCompatibility)
			{
				return XslVersion.Version10;
			}
			return XslVersion.ForwardsCompatible;
		}
	}

	public XsltInput(XmlReader reader, Compiler compiler, KeywordsTable atoms)
	{
		EnsureExpandEntities(reader);
		IXmlLineInfo xmlLineInfo = reader as IXmlLineInfo;
		_atoms = atoms;
		_reader = reader;
		_reatomize = reader.NameTable != atoms.NameTable;
		_readerLineInfo = ((xmlLineInfo != null && xmlLineInfo.HasLineInfo()) ? xmlLineInfo : null);
		_topLevelReader = reader.ReadState == ReadState.Initial;
		_scopeManager = new CompilerScopeManager<VarPar>(atoms);
		_compiler = compiler;
		_nodeType = XmlNodeType.Document;
	}

	private static void EnsureExpandEntities(XmlReader reader)
	{
		if (reader is XmlTextReader { EntityHandling: not EntityHandling.ExpandEntities } xmlTextReader)
		{
			xmlTextReader.EntityHandling = EntityHandling.ExpandEntities;
		}
	}

	private void ExtendRecordBuffer(int position)
	{
		if (_records.Length <= position)
		{
			int num = _records.Length * 2;
			if (num <= position)
			{
				num = position + 1;
			}
			Record[] array = new Record[num];
			Array.Copy(_records, array, _records.Length);
			_records = array;
		}
	}

	public bool FindStylesheetElement()
	{
		if (!_topLevelReader && _reader.ReadState != ReadState.Interactive)
		{
			return false;
		}
		IDictionary<string, string> dictionary = null;
		if (_reader.ReadState == ReadState.Interactive && _reader is IXmlNamespaceResolver xmlNamespaceResolver)
		{
			dictionary = xmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
		}
		while (MoveToNextSibling() && _nodeType == XmlNodeType.Whitespace)
		{
		}
		if (_nodeType == XmlNodeType.Element)
		{
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					if (_scopeManager.LookupNamespace(item.Key) == null)
					{
						string nsUri = _atoms.NameTable.Add(item.Value);
						_scopeManager.AddNsDeclaration(item.Key, nsUri);
						_ctxInfo.AddNamespace(item.Key, nsUri);
					}
				}
			}
			return true;
		}
		return false;
	}

	public void Finish()
	{
		if (_topLevelReader)
		{
			while (_reader.ReadState == ReadState.Interactive)
			{
				_reader.Skip();
			}
		}
	}

	private void FillupRecord(ref Record rec)
	{
		rec.localName = _reader.LocalName;
		rec.nsUri = _reader.NamespaceURI;
		rec.prefix = _reader.Prefix;
		rec.value = _reader.Value;
		rec.baseUri = _reader.BaseURI;
		if (_reatomize)
		{
			rec.localName = _atoms.NameTable.Add(rec.localName);
			rec.nsUri = _atoms.NameTable.Add(rec.nsUri);
			rec.prefix = _atoms.NameTable.Add(rec.prefix);
		}
		if (_readerLineInfo != null)
		{
			rec.start = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - PositionAdjustment(_reader.NodeType));
		}
	}

	private void SetRecordEnd(ref Record rec)
	{
		if (_readerLineInfo != null)
		{
			rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - PositionAdjustment(_reader.NodeType));
			if (_reader.BaseURI != rec.baseUri || rec.end.LessOrEqual(rec.start))
			{
				rec.end = new Location(rec.start.Line, int.MaxValue);
			}
		}
	}

	private void FillupTextRecord(ref Record rec)
	{
		rec.localName = string.Empty;
		rec.nsUri = string.Empty;
		rec.prefix = string.Empty;
		rec.value = _reader.Value;
		rec.baseUri = _reader.BaseURI;
		if (_readerLineInfo == null)
		{
			return;
		}
		bool flag = _reader.NodeType == XmlNodeType.CDATA;
		int num = _readerLineInfo.LineNumber;
		int num2 = _readerLineInfo.LinePosition;
		rec.start = new Location(num, num2 - (flag ? 9 : 0));
		char c = ' ';
		string value = rec.value;
		char c2;
		for (int i = 0; i < value.Length; c = c2, i++)
		{
			c2 = value[i];
			if (c2 != '\n')
			{
				if (c2 != '\r')
				{
					num2++;
					continue;
				}
			}
			else if (c == '\r')
			{
				continue;
			}
			num++;
			num2 = 1;
		}
		rec.end = new Location(num, num2 + (flag ? 3 : 0));
	}

	private void FillupCharacterEntityRecord(ref Record rec)
	{
		string localName = _reader.LocalName;
		rec.localName = string.Empty;
		rec.nsUri = string.Empty;
		rec.prefix = string.Empty;
		rec.baseUri = _reader.BaseURI;
		if (_readerLineInfo != null)
		{
			rec.start = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition - 1);
		}
		_reader.ResolveEntity();
		_reader.Read();
		rec.value = _reader.Value;
		_reader.Read();
		if (_readerLineInfo != null)
		{
			rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + 1);
		}
	}

	private bool ReadAttribute(ref Record rec)
	{
		FillupRecord(ref rec);
		if (Ref.Equal(rec.prefix, _atoms.Xmlns))
		{
			string nsUri = _atoms.NameTable.Add(_reader.Value);
			if (!Ref.Equal(rec.localName, _atoms.Xml))
			{
				_scopeManager.AddNsDeclaration(rec.localName, nsUri);
				_ctxInfo.AddNamespace(rec.localName, nsUri);
			}
			return false;
		}
		if (rec.prefix.Length == 0 && Ref.Equal(rec.localName, _atoms.Xmlns))
		{
			string nsUri2 = _atoms.NameTable.Add(_reader.Value);
			_scopeManager.AddNsDeclaration(string.Empty, nsUri2);
			_ctxInfo.AddNamespace(string.Empty, nsUri2);
			return false;
		}
		if (!_reader.ReadAttributeValue())
		{
			rec.value = string.Empty;
			SetRecordEnd(ref rec);
			return true;
		}
		if (_readerLineInfo != null)
		{
			int num = ((_reader.NodeType == XmlNodeType.EntityReference) ? (-2) : (-1));
			rec.valueStart = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + num);
			if (_reader.BaseURI != rec.baseUri || rec.valueStart.LessOrEqual(rec.start))
			{
				int num2 = ((rec.prefix.Length != 0) ? (rec.prefix.Length + 1) : 0) + rec.localName.Length;
				rec.end = new Location(rec.start.Line, rec.start.Pos + num2 + 1);
			}
		}
		string text = string.Empty;
		_strConcat.Clear();
		do
		{
			switch (_reader.NodeType)
			{
			case XmlNodeType.EntityReference:
				_reader.ResolveEntity();
				break;
			default:
				text = _reader.Value;
				_strConcat.Concat(text);
				break;
			case XmlNodeType.EndEntity:
				break;
			}
		}
		while (_reader.ReadAttributeValue());
		rec.value = _strConcat.GetResult();
		if (_readerLineInfo != null)
		{
			int num3 = ((_reader.NodeType == XmlNodeType.EndEntity) ? 1 : text.Length) + 1;
			rec.end = new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition + num3);
			if (_reader.BaseURI != rec.baseUri || rec.end.LessOrEqual(rec.valueStart))
			{
				rec.end = new Location(rec.start.Line, int.MaxValue);
			}
		}
		return true;
	}

	public bool MoveToFirstChild()
	{
		if (IsEmptyElement)
		{
			return false;
		}
		return ReadNextSibling();
	}

	public bool MoveToNextSibling()
	{
		if (_nodeType == XmlNodeType.Element || _nodeType == XmlNodeType.EndElement)
		{
			_scopeManager.ExitScope();
		}
		return ReadNextSibling();
	}

	public void SkipNode()
	{
		if (_nodeType == XmlNodeType.Element && MoveToFirstChild())
		{
			do
			{
				SkipNode();
			}
			while (MoveToNextSibling());
		}
	}

	private int ReadTextNodes()
	{
		bool flag = _reader.XmlSpace == XmlSpace.Preserve;
		bool flag2 = true;
		int num = 0;
		while (true)
		{
			XmlNodeType nodeType = _reader.NodeType;
			if (nodeType <= XmlNodeType.EntityReference)
			{
				if ((uint)(nodeType - 3) > 1u)
				{
					if (nodeType != XmlNodeType.EntityReference)
					{
						break;
					}
					string localName = _reader.LocalName;
					if (localName.Length > 0)
					{
						if (localName[0] != '#')
						{
							switch (localName)
							{
							case "lt":
							case "gt":
							case "quot":
							case "apos":
								break;
							default:
								goto IL_0121;
							}
						}
						ExtendRecordBuffer(num);
						FillupCharacterEntityRecord(ref _records[num]);
						if (flag2 && !XmlCharType.IsOnlyWhitespace(_records[num].value))
						{
							flag2 = false;
						}
						num++;
						continue;
					}
					goto IL_0121;
				}
				if (flag2 && !XmlCharType.IsOnlyWhitespace(_reader.Value))
				{
					flag2 = false;
				}
			}
			else if ((uint)(nodeType - 13) > 1u)
			{
				if (nodeType != XmlNodeType.EndEntity)
				{
					break;
				}
				_reader.Read();
				continue;
			}
			ExtendRecordBuffer(num);
			FillupTextRecord(ref _records[num]);
			_reader.Read();
			num++;
			continue;
			IL_0121:
			_reader.ResolveEntity();
			_reader.Read();
		}
		_nodeType = ((!flag2) ? XmlNodeType.Text : (flag ? XmlNodeType.SignificantWhitespace : XmlNodeType.Whitespace));
		return num;
	}

	private bool ReadNextSibling()
	{
		if (_currentRecord < _lastTextNode)
		{
			_currentRecord++;
			if (_currentRecord == _lastTextNode)
			{
				_lastTextNode = 0;
			}
			return true;
		}
		_currentRecord = 0;
		while (!_reader.EOF)
		{
			switch (_reader.NodeType)
			{
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.EntityReference:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				int num = ReadTextNodes();
				if (num != 0)
				{
					_lastTextNode = num - 1;
					return true;
				}
				break;
			}
			case XmlNodeType.Element:
				_scopeManager.EnterScope();
				_numAttributes = ReadElement();
				return true;
			case XmlNodeType.EndElement:
				_nodeType = XmlNodeType.EndElement;
				_isEmptyElement = false;
				FillupRecord(ref _records[0]);
				_reader.Read();
				SetRecordEnd(ref _records[0]);
				return false;
			default:
				_reader.Read();
				break;
			}
		}
		return false;
	}

	private int ReadElement()
	{
		_attributesRead = false;
		FillupRecord(ref _records[0]);
		_nodeType = XmlNodeType.Element;
		_isEmptyElement = _reader.IsEmptyElement;
		_ctxInfo = new ContextInfo(this);
		int num = 1;
		if (_reader.MoveToFirstAttribute())
		{
			do
			{
				ExtendRecordBuffer(num);
				if (ReadAttribute(ref _records[num]))
				{
					num++;
				}
			}
			while (_reader.MoveToNextAttribute());
			_reader.MoveToElement();
		}
		_reader.Read();
		SetRecordEnd(ref _records[0]);
		_ctxInfo.lineInfo = BuildLineInfo();
		_attributes = null;
		return num - 1;
	}

	public void MoveToElement()
	{
		_currentRecord = 0;
	}

	private bool MoveToAttributeBase(int attNum)
	{
		if (0 < attNum && attNum <= _numAttributes)
		{
			_currentRecord = attNum;
			return true;
		}
		_currentRecord = 0;
		return false;
	}

	public bool MoveToLiteralAttribute(int attNum)
	{
		if (0 < attNum && attNum <= _numAttributes)
		{
			_currentRecord = attNum;
			return true;
		}
		_currentRecord = 0;
		return false;
	}

	public bool MoveToXsltAttribute(int attNum, string attName)
	{
		_currentRecord = _xsltAttributeNumber[attNum];
		return _currentRecord != 0;
	}

	public bool IsRequiredAttribute(int attNum)
	{
		return (_attributes[attNum].flags & ((_compiler.Version == 2) ? XsltLoader.V2Req : XsltLoader.V1Req)) != 0;
	}

	public bool AttributeExists(int attNum, string attName)
	{
		return _xsltAttributeNumber[attNum] != 0;
	}

	public bool IsNs(string ns)
	{
		return Ref.Equal(ns, NamespaceUri);
	}

	public bool IsKeyword(string kwd)
	{
		return Ref.Equal(kwd, LocalName);
	}

	public bool IsXsltNamespace()
	{
		return IsNs(_atoms.UriXsl);
	}

	public bool IsNullNamespace()
	{
		return IsNs(string.Empty);
	}

	public bool IsXsltKeyword(string kwd)
	{
		if (IsKeyword(kwd))
		{
			return IsXsltNamespace();
		}
		return false;
	}

	public bool IsExtensionNamespace(string uri)
	{
		return _scopeManager.IsExNamespace(uri);
	}

	private void SetVersion(int attVersion)
	{
		MoveToLiteralAttribute(attVersion);
		double num = XPathConvert.StringToDouble(Value);
		if (double.IsNaN(num))
		{
			ReportError(System.SR.Xslt_InvalidAttrValue, _atoms.Version, Value);
			num = 1.0;
		}
		SetVersion(num);
	}

	private void SetVersion(double version)
	{
		if (_compiler.Version == 0)
		{
			_compiler.Version = 1;
		}
		if (_compiler.Version == 1)
		{
			_scopeManager.BackwardCompatibility = false;
			_scopeManager.ForwardCompatibility = version != 1.0;
		}
		else
		{
			_scopeManager.BackwardCompatibility = version < 2.0;
			_scopeManager.ForwardCompatibility = 2.0 < version;
		}
	}

	public ContextInfo GetAttributes()
	{
		return GetAttributes(Array.Empty<XsltAttribute>());
	}

	public ContextInfo GetAttributes(XsltAttribute[] attributes)
	{
		_attributes = attributes;
		_records[0].value = null;
		int attExPrefixes = 0;
		int attExPrefixes2 = 0;
		int xPathDefaultNamespace = 0;
		int defaultCollation = 0;
		int num = 0;
		bool flag = IsXsltNamespace() && IsKeyword(_atoms.Output);
		bool flag2 = IsXsltNamespace() && (IsKeyword(_atoms.Stylesheet) || IsKeyword(_atoms.Transform));
		bool flag3 = _compiler.Version == 2;
		for (int i = 0; i < attributes.Length; i++)
		{
			_xsltAttributeNumber[i] = 0;
		}
		_compiler.EnterForwardsCompatible();
		if (flag2 || (flag3 && !flag))
		{
			for (int j = 1; MoveToAttributeBase(j); j++)
			{
				if (IsNullNamespace() && IsKeyword(_atoms.Version))
				{
					SetVersion(j);
					break;
				}
			}
		}
		if (_compiler.Version == 0)
		{
			SetVersion(1.0);
		}
		flag3 = _compiler.Version == 2;
		int num2 = (flag3 ? (XsltLoader.V2Opt | XsltLoader.V2Req) : (XsltLoader.V1Opt | XsltLoader.V1Req));
		for (int k = 1; MoveToAttributeBase(k); k++)
		{
			if (IsNullNamespace())
			{
				string localName = LocalName;
				int l;
				for (l = 0; l < attributes.Length; l++)
				{
					if (Ref.Equal(localName, attributes[l].name) && (attributes[l].flags & num2) != 0)
					{
						_xsltAttributeNumber[l] = k;
						break;
					}
				}
				if (l == attributes.Length)
				{
					if (Ref.Equal(localName, _atoms.ExcludeResultPrefixes) && (flag2 || flag3))
					{
						attExPrefixes2 = k;
						continue;
					}
					if (Ref.Equal(localName, _atoms.ExtensionElementPrefixes) && (flag2 || flag3))
					{
						attExPrefixes = k;
						continue;
					}
					if (Ref.Equal(localName, _atoms.XPathDefaultNamespace) && flag3)
					{
						xPathDefaultNamespace = k;
						continue;
					}
					if (Ref.Equal(localName, _atoms.DefaultCollation) && flag3)
					{
						defaultCollation = k;
						continue;
					}
					if (Ref.Equal(localName, _atoms.UseWhen) && flag3)
					{
						num = k;
						continue;
					}
					ReportError(System.SR.Xslt_InvalidAttribute, QualifiedName, _records[0].QualifiedName);
				}
			}
			else if (IsXsltNamespace())
			{
				ReportError(System.SR.Xslt_InvalidAttribute, QualifiedName, _records[0].QualifiedName);
			}
		}
		_attributesRead = true;
		_compiler.ExitForwardsCompatible(ForwardCompatibility);
		InsertExNamespaces(attExPrefixes, _ctxInfo, extensions: true);
		InsertExNamespaces(attExPrefixes2, _ctxInfo, extensions: false);
		SetXPathDefaultNamespace(xPathDefaultNamespace);
		SetDefaultCollation(defaultCollation);
		if (num != 0)
		{
			ReportNYI(_atoms.UseWhen);
		}
		MoveToElement();
		for (int m = 0; m < attributes.Length; m++)
		{
			if (_xsltAttributeNumber[m] == 0)
			{
				int flags = attributes[m].flags;
				if ((_compiler.Version == 2 && (flags & XsltLoader.V2Req) != 0) || (_compiler.Version == 1 && (flags & XsltLoader.V1Req) != 0 && (!ForwardCompatibility || (flags & XsltLoader.V2Req) != 0)))
				{
					ReportError(System.SR.Xslt_MissingAttribute, attributes[m].name);
				}
			}
		}
		return _ctxInfo;
	}

	public ContextInfo GetLiteralAttributes(bool asStylesheet)
	{
		int num = 0;
		int attExPrefixes = 0;
		int attExPrefixes2 = 0;
		int xPathDefaultNamespace = 0;
		int defaultCollation = 0;
		int num2 = 0;
		for (int i = 1; MoveToLiteralAttribute(i); i++)
		{
			if (IsXsltNamespace())
			{
				string localName = LocalName;
				if (Ref.Equal(localName, _atoms.Version))
				{
					num = i;
				}
				else if (Ref.Equal(localName, _atoms.ExtensionElementPrefixes))
				{
					attExPrefixes = i;
				}
				else if (Ref.Equal(localName, _atoms.ExcludeResultPrefixes))
				{
					attExPrefixes2 = i;
				}
				else if (Ref.Equal(localName, _atoms.XPathDefaultNamespace))
				{
					xPathDefaultNamespace = i;
				}
				else if (Ref.Equal(localName, _atoms.DefaultCollation))
				{
					defaultCollation = i;
				}
				else if (Ref.Equal(localName, _atoms.UseWhen))
				{
					num2 = i;
				}
			}
		}
		_attributesRead = true;
		MoveToElement();
		if (num != 0)
		{
			SetVersion(num);
		}
		else if (asStylesheet)
		{
			ReportError((Ref.Equal(NamespaceUri, _atoms.UriWdXsl) && Ref.Equal(LocalName, _atoms.Stylesheet)) ? System.SR.Xslt_WdXslNamespace : System.SR.Xslt_WrongStylesheetElement);
			SetVersion(1.0);
		}
		InsertExNamespaces(attExPrefixes, _ctxInfo, extensions: true);
		if (!IsExtensionNamespace(_records[0].nsUri))
		{
			if (_compiler.Version == 2)
			{
				SetXPathDefaultNamespace(xPathDefaultNamespace);
				SetDefaultCollation(defaultCollation);
				if (num2 != 0)
				{
					ReportNYI(_atoms.UseWhen);
				}
			}
			InsertExNamespaces(attExPrefixes2, _ctxInfo, extensions: false);
		}
		return _ctxInfo;
	}

	public void GetVersionAttribute()
	{
		if (_compiler.Version == 2)
		{
			for (int i = 1; MoveToAttributeBase(i); i++)
			{
				if (IsNullNamespace() && IsKeyword(_atoms.Version))
				{
					SetVersion(i);
					break;
				}
			}
		}
		_attributesRead = true;
	}

	private void InsertExNamespaces(int attExPrefixes, ContextInfo ctxInfo, bool extensions)
	{
		if (!MoveToLiteralAttribute(attExPrefixes))
		{
			return;
		}
		string value = Value;
		if (value.Length == 0)
		{
			return;
		}
		if (!extensions && _compiler.Version != 1 && value == "#all")
		{
			ctxInfo.nsList = new NsDecl(ctxInfo.nsList, null, null);
			return;
		}
		_compiler.EnterForwardsCompatible();
		string[] array = XmlConvert.SplitString(value);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == "#default")
			{
				array[i] = LookupXmlNamespace(string.Empty);
				if (array[i].Length == 0 && _compiler.Version != 1 && !BackwardCompatibility)
				{
					ReportError(System.SR.Xslt_ExcludeDefault);
				}
			}
			else
			{
				array[i] = LookupXmlNamespace(array[i]);
			}
		}
		if (!_compiler.ExitForwardsCompatible(ForwardCompatibility))
		{
			return;
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j] != null)
			{
				ctxInfo.nsList = new NsDecl(ctxInfo.nsList, null, array[j]);
				if (extensions)
				{
					_scopeManager.AddExNamespace(array[j]);
				}
			}
		}
	}

	private void SetXPathDefaultNamespace(int attNamespace)
	{
		if (MoveToLiteralAttribute(attNamespace) && Value.Length != 0)
		{
			ReportNYI(_atoms.XPathDefaultNamespace);
		}
	}

	private void SetDefaultCollation(int attCollation)
	{
		if (MoveToLiteralAttribute(attCollation))
		{
			string[] array = XmlConvert.SplitString(Value);
			int i;
			for (i = 0; i < array.Length && XmlCollation.Create(array[i], throwOnError: false) == null; i++)
			{
			}
			if (i == array.Length)
			{
				ReportErrorFC(System.SR.Xslt_CollationSyntax);
			}
			else if (array[i] != "http://www.w3.org/2004/10/xpath-functions/collation/codepoint")
			{
				ReportNYI(_atoms.DefaultCollation);
			}
		}
	}

	private static int PositionAdjustment(XmlNodeType nt)
	{
		return nt switch
		{
			XmlNodeType.Element => 1, 
			XmlNodeType.CDATA => 9, 
			XmlNodeType.ProcessingInstruction => 2, 
			XmlNodeType.Comment => 4, 
			XmlNodeType.EndElement => 2, 
			XmlNodeType.EntityReference => 1, 
			_ => 0, 
		};
	}

	public ISourceLineInfo BuildLineInfo()
	{
		return new SourceLineInfo(Uri, Start, End);
	}

	public ISourceLineInfo BuildNameLineInfo()
	{
		if (_readerLineInfo == null)
		{
			return BuildLineInfo();
		}
		if (LocalName == null)
		{
			FillupRecord(ref _records[_currentRecord]);
		}
		Location start = Start;
		int line = start.Line;
		int num = start.Pos + PositionAdjustment(NodeType);
		return new SourceLineInfo(Uri, new Location(line, num), new Location(line, num + QualifiedName.Length));
	}

	public ISourceLineInfo BuildReaderLineInfo()
	{
		Location location = ((_readerLineInfo == null) ? new Location(0, 0) : new Location(_readerLineInfo.LineNumber, _readerLineInfo.LinePosition));
		return new SourceLineInfo(_reader.BaseURI, location, location);
	}

	public string LookupXmlNamespace(string prefix)
	{
		string text = _scopeManager.LookupNamespace(prefix);
		if (text != null)
		{
			return text;
		}
		if (prefix.Length == 0)
		{
			return string.Empty;
		}
		ReportError(System.SR.Xslt_InvalidPrefix, prefix);
		return null;
	}

	public void ReportError(string res, params string[] args)
	{
		_compiler.ReportError(BuildNameLineInfo(), res, args);
	}

	public void ReportErrorFC(string res, params string[] args)
	{
		if (!ForwardCompatibility)
		{
			_compiler.ReportError(BuildNameLineInfo(), res, args);
		}
	}

	private void ReportNYI(string arg)
	{
		ReportErrorFC(System.SR.Xslt_NotYetImplemented, arg);
	}
}
