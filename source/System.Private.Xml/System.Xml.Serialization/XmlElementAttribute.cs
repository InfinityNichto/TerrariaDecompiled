using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public class XmlElementAttribute : Attribute
{
	private string _elementName;

	private Type _type;

	private string _ns;

	private string _dataType;

	private bool _nullable;

	private bool _nullableSpecified;

	private XmlSchemaForm _form;

	private int _order = -1;

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

	public XmlSchemaForm Form
	{
		get
		{
			return _form;
		}
		set
		{
			_form = value;
		}
	}

	public int Order
	{
		get
		{
			return _order;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentException(System.SR.XmlDisallowNegativeValues, "Order");
			}
			_order = value;
		}
	}

	public XmlElementAttribute()
	{
	}

	public XmlElementAttribute(string? elementName)
	{
		_elementName = elementName;
	}

	public XmlElementAttribute(Type? type)
	{
		_type = type;
	}

	public XmlElementAttribute(string? elementName, Type? type)
	{
		_elementName = elementName;
		_type = type;
	}

	internal bool GetIsNullableSpecified()
	{
		return IsNullableSpecified;
	}
}
