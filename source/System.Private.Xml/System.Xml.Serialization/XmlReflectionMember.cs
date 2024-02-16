namespace System.Xml.Serialization;

public class XmlReflectionMember
{
	private string _memberName;

	private Type _type;

	private XmlAttributes _xmlAttributes = new XmlAttributes();

	private SoapAttributes _soapAttributes = new SoapAttributes();

	private bool _isReturnValue;

	private bool _overrideIsNullable;

	public Type? MemberType
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
		}
	}

	public XmlAttributes XmlAttributes
	{
		get
		{
			return _xmlAttributes;
		}
		set
		{
			_xmlAttributes = value;
		}
	}

	public SoapAttributes SoapAttributes
	{
		get
		{
			return _soapAttributes;
		}
		set
		{
			_soapAttributes = value;
		}
	}

	public string MemberName
	{
		get
		{
			if (_memberName != null)
			{
				return _memberName;
			}
			return string.Empty;
		}
		set
		{
			_memberName = value;
		}
	}

	public bool IsReturnValue
	{
		get
		{
			return _isReturnValue;
		}
		set
		{
			_isReturnValue = value;
		}
	}

	public bool OverrideIsNullable
	{
		get
		{
			return _overrideIsNullable;
		}
		set
		{
			_overrideIsNullable = value;
		}
	}
}
