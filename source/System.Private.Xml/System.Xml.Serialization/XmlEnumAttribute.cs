namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Field)]
public class XmlEnumAttribute : Attribute
{
	private string _name;

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public XmlEnumAttribute()
	{
	}

	public XmlEnumAttribute(string? name)
	{
		_name = name;
	}
}
