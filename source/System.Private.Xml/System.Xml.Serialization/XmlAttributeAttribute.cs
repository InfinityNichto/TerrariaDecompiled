using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class XmlAttributeAttribute : Attribute
{
	private string _attributeName;

	private Type _type;

	private string _ns;

	private string _dataType;

	private XmlSchemaForm _form;

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

	public XmlAttributeAttribute()
	{
	}

	public XmlAttributeAttribute(string? attributeName)
	{
		_attributeName = attributeName;
	}

	public XmlAttributeAttribute(Type? type)
	{
		_type = type;
	}

	public XmlAttributeAttribute(string? attributeName, Type? type)
	{
		_attributeName = attributeName;
		_type = type;
	}
}
