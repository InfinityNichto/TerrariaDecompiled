using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
public class XmlArrayAttribute : Attribute
{
	private string _elementName;

	private string _ns;

	private bool _nullable;

	private XmlSchemaForm _form;

	private int _order = -1;

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

	public XmlArrayAttribute()
	{
	}

	public XmlArrayAttribute(string? elementName)
	{
		_elementName = elementName;
	}
}
