using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public abstract class XmlSchemaFacet : XmlSchemaAnnotated
{
	private string _value;

	private bool _isFixed;

	private FacetType _facetType;

	[XmlAttribute("value")]
	public string? Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	[XmlAttribute("fixed")]
	[DefaultValue(false)]
	public virtual bool IsFixed
	{
		get
		{
			return _isFixed;
		}
		set
		{
			if (!(this is XmlSchemaEnumerationFacet) && !(this is XmlSchemaPatternFacet))
			{
				_isFixed = value;
			}
		}
	}

	internal FacetType FacetType
	{
		get
		{
			return _facetType;
		}
		set
		{
			_facetType = value;
		}
	}
}
