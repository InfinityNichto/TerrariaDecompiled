using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal abstract class Accessor
{
	private string _name;

	private object _defaultValue;

	private string _ns;

	private TypeMapping _mapping;

	private bool _any;

	private string _anyNs;

	private bool _topLevelInSchema;

	private bool _isFixed;

	private bool _isOptional;

	private XmlSchemaForm _form;

	internal TypeMapping Mapping
	{
		get
		{
			return _mapping;
		}
		set
		{
			_mapping = value;
		}
	}

	internal object Default
	{
		get
		{
			return _defaultValue;
		}
		set
		{
			_defaultValue = value;
		}
	}

	internal bool HasDefault
	{
		get
		{
			if (_defaultValue != null)
			{
				return _defaultValue != DBNull.Value;
			}
			return false;
		}
	}

	internal virtual string Name
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

	internal bool Any
	{
		get
		{
			return _any;
		}
		set
		{
			_any = value;
		}
	}

	internal string AnyNamespaces
	{
		get
		{
			return _anyNs;
		}
		set
		{
			_anyNs = value;
		}
	}

	internal string Namespace
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

	internal XmlSchemaForm Form
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

	internal bool IsFixed
	{
		set
		{
			_isFixed = value;
		}
	}

	internal bool IsOptional
	{
		set
		{
			_isOptional = value;
		}
	}

	internal bool IsTopLevelInSchema
	{
		get
		{
			return _topLevelInSchema;
		}
		set
		{
			_topLevelInSchema = value;
		}
	}

	internal Accessor()
	{
	}

	[return: NotNullIfNotNull("name")]
	internal static string EscapeName(string name)
	{
		if (name == null || name.Length == 0)
		{
			return name;
		}
		return XmlConvert.EncodeLocalName(name);
	}

	[return: NotNullIfNotNull("name")]
	internal static string EscapeQName(string name)
	{
		if (name == null || name.Length == 0)
		{
			return name;
		}
		int num = name.LastIndexOf(':');
		if (num < 0)
		{
			return XmlConvert.EncodeLocalName(name);
		}
		if (num == 0 || num == name.Length - 1)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xml_InvalidNameChars, name), "name");
		}
		return new XmlQualifiedName(XmlConvert.EncodeLocalName(name.Substring(num + 1)), XmlConvert.EncodeLocalName(name.Substring(0, num))).ToString();
	}

	[return: NotNullIfNotNull("name")]
	internal static string UnescapeName(string name)
	{
		return XmlConvert.DecodeName(name);
	}

	internal string ToString(string defaultNs)
	{
		if (Any)
		{
			return ((Namespace == null) ? "##any" : Namespace) + ":" + Name;
		}
		if (!(Namespace == defaultNs))
		{
			return Namespace + ":" + Name;
		}
		return Name;
	}
}
