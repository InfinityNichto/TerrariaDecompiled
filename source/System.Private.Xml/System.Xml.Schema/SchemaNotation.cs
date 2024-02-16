namespace System.Xml.Schema;

internal sealed class SchemaNotation
{
	private readonly XmlQualifiedName _name;

	private string _systemLiteral;

	private string _pubid;

	internal XmlQualifiedName Name => _name;

	internal string SystemLiteral
	{
		get
		{
			return _systemLiteral;
		}
		set
		{
			_systemLiteral = value;
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

	internal SchemaNotation(XmlQualifiedName name)
	{
		_name = name;
	}
}
