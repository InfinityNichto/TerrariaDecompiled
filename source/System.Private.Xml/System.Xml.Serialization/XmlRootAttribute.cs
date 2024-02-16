using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.ReturnValue)]
public class XmlRootAttribute : Attribute
{
	private string _elementName;

	private string _ns;

	private string _dataType;

	private bool _nullable = true;

	private bool _nullableSpecified;

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
		set
		{
			_elementName = value;
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

	public bool IsNullable
	{
		get
		{
			return _nullable;
		}
		set
		{
			_nullable = value;
			_nullableSpecified = true;
		}
	}

	internal bool IsNullableSpecified => _nullableSpecified;

	internal string Key => ((_ns == null) ? string.Empty : _ns) + ":" + ElementName + ":" + _nullable;

	public XmlRootAttribute()
	{
	}

	public XmlRootAttribute(string elementName)
	{
		_elementName = elementName;
	}

	internal bool GetIsNullableSpecified()
	{
		return IsNullableSpecified;
	}

	internal string GetKey()
	{
		return Key;
	}
}
