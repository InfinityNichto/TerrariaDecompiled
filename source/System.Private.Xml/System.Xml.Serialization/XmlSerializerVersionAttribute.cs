namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class XmlSerializerVersionAttribute : Attribute
{
	private string _mvid;

	private string _serializerVersion;

	private string _ns;

	private Type _type;

	public string? ParentAssemblyId
	{
		get
		{
			return _mvid;
		}
		set
		{
			_mvid = value;
		}
	}

	public string? Version
	{
		get
		{
			return _serializerVersion;
		}
		set
		{
			_serializerVersion = value;
		}
	}

	public string? Namespace
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

	public Type? Type
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

	public XmlSerializerVersionAttribute()
	{
	}

	public XmlSerializerVersionAttribute(Type? type)
	{
		_type = type;
	}
}
