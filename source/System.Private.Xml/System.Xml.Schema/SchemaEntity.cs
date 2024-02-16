namespace System.Xml.Schema;

internal sealed class SchemaEntity : IDtdEntityInfo
{
	private readonly XmlQualifiedName _qname;

	private string _url;

	private string _pubid;

	private string _text;

	private XmlQualifiedName _ndata = XmlQualifiedName.Empty;

	private int _lineNumber;

	private int _linePosition;

	private readonly bool _isParameter;

	private bool _isExternal;

	private bool _parsingInProgress;

	private bool _isDeclaredInExternal;

	private string _baseURI;

	private string _declaredURI;

	string IDtdEntityInfo.Name => Name.Name;

	bool IDtdEntityInfo.IsExternal => IsExternal;

	bool IDtdEntityInfo.IsDeclaredInExternal => DeclaredInExternal;

	bool IDtdEntityInfo.IsUnparsedEntity => !NData.IsEmpty;

	bool IDtdEntityInfo.IsParameterEntity => _isParameter;

	string IDtdEntityInfo.BaseUriString => BaseURI;

	string IDtdEntityInfo.DeclaredUriString => DeclaredURI;

	string IDtdEntityInfo.SystemId => Url;

	string IDtdEntityInfo.PublicId => Pubid;

	string IDtdEntityInfo.Text => Text;

	int IDtdEntityInfo.LineNumber => Line;

	int IDtdEntityInfo.LinePosition => Pos;

	internal XmlQualifiedName Name => _qname;

	internal string Url
	{
		get
		{
			return _url;
		}
		set
		{
			_url = value;
			_isExternal = true;
		}
	}

	internal string Pubid
	{
		get
		{
			return _pubid;
		}
		set
		{
			_pubid = value;
		}
	}

	internal bool IsExternal
	{
		get
		{
			return _isExternal;
		}
		set
		{
			_isExternal = value;
		}
	}

	internal bool DeclaredInExternal
	{
		get
		{
			return _isDeclaredInExternal;
		}
		set
		{
			_isDeclaredInExternal = value;
		}
	}

	internal XmlQualifiedName NData
	{
		get
		{
			return _ndata;
		}
		set
		{
			_ndata = value;
		}
	}

	internal string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
			_isExternal = false;
		}
	}

	internal int Line
	{
		get
		{
			return _lineNumber;
		}
		set
		{
			_lineNumber = value;
		}
	}

	internal int Pos
	{
		get
		{
			return _linePosition;
		}
		set
		{
			_linePosition = value;
		}
	}

	internal string BaseURI
	{
		get
		{
			if (_baseURI != null)
			{
				return _baseURI;
			}
			return string.Empty;
		}
		set
		{
			_baseURI = value;
		}
	}

	internal bool ParsingInProgress
	{
		get
		{
			return _parsingInProgress;
		}
		set
		{
			_parsingInProgress = value;
		}
	}

	internal string DeclaredURI
	{
		get
		{
			if (_declaredURI != null)
			{
				return _declaredURI;
			}
			return string.Empty;
		}
		set
		{
			_declaredURI = value;
		}
	}

	internal SchemaEntity(XmlQualifiedName qname, bool isParameter)
	{
		_qname = qname;
		_isParameter = isParameter;
	}

	internal static bool IsPredefinedEntity(string n)
	{
		switch (n)
		{
		default:
			return n == "quot";
		case "lt":
		case "gt":
		case "amp":
		case "apos":
			return true;
		}
	}
}
