using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public class XmlAnyElementAttribute : Attribute
{
	private string _name;

	private string _ns;

	private int _order = -1;

	private bool _nsSpecified;

	public string Name
	{
		get
		{
			if (_name != null)
			{
				return _name;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_name = value;
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
			_nsSpecified = true;
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

	internal bool NamespaceSpecified => _nsSpecified;

	public XmlAnyElementAttribute()
	{
	}

	public XmlAnyElementAttribute(string? name)
	{
		_name = name;
	}

	public XmlAnyElementAttribute(string? name, string? ns)
	{
		_name = name;
		_ns = ns;
		_nsSpecified = true;
	}

	internal bool GetNamespaceSpecified()
	{
		return NamespaceSpecified;
	}
}
