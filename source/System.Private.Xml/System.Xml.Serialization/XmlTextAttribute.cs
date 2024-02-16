using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class XmlTextAttribute : Attribute
{
	private Type _type;

	private string _dataType;

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

	public string DataType
	{
		get
		{
			if (_dataType != null)
			{
				return _dataType;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_dataType = value;
		}
	}

	public XmlTextAttribute()
	{
	}

	public XmlTextAttribute(Type? type)
	{
		_type = type;
	}
}
