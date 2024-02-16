using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class SoapElementAttribute : Attribute
{
	private string _elementName;

	private string _dataType;

	private bool _nullable;

	public string ElementName
	{
		get
		{
			if (_elementName != null)
			{
				return _elementName;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_elementName = value;
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

	public bool IsNullable
	{
		get
		{
			return _nullable;
		}
		set
		{
			_nullable = value;
		}
	}

	public SoapElementAttribute()
	{
	}

	public SoapElementAttribute(string? elementName)
	{
		_elementName = elementName;
	}
}
