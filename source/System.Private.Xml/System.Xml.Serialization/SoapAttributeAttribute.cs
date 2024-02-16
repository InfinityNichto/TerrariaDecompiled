using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class SoapAttributeAttribute : Attribute
{
	private string _attributeName;

	private string _ns;

	private string _dataType;

	public string AttributeName
	{
		get
		{
			if (_attributeName != null)
			{
				return _attributeName;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_attributeName = value;
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

	public SoapAttributeAttribute()
	{
	}

	public SoapAttributeAttribute(string attributeName)
	{
		_attributeName = attributeName;
	}
}
