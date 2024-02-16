using System.Xml.Serialization;

namespace System.Xml.Schema;

public abstract class XmlSchemaParticle : XmlSchemaAnnotated
{
	[Flags]
	private enum Occurs
	{
		None = 0,
		Min = 1,
		Max = 2
	}

	private sealed class EmptyParticle : XmlSchemaParticle
	{
		internal override bool IsEmpty => true;
	}

	private decimal _minOccurs = 1m;

	private decimal _maxOccurs = 1m;

	private Occurs _flags;

	internal static readonly XmlSchemaParticle Empty = new EmptyParticle();

	[XmlAttribute("minOccurs")]
	public string? MinOccursString
	{
		get
		{
			if ((_flags & Occurs.Min) != 0)
			{
				return XmlConvert.ToString(_minOccurs);
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				_minOccurs = 1m;
				_flags &= ~Occurs.Min;
				return;
			}
			_minOccurs = XmlConvert.ToInteger(value);
			if (_minOccurs < 0m)
			{
				throw new XmlSchemaException(System.SR.Sch_MinOccursInvalidXsd, string.Empty);
			}
			_flags |= Occurs.Min;
		}
	}

	[XmlAttribute("maxOccurs")]
	public string? MaxOccursString
	{
		get
		{
			if ((_flags & Occurs.Max) != 0)
			{
				if (!(_maxOccurs == decimal.MaxValue))
				{
					return XmlConvert.ToString(_maxOccurs);
				}
				return "unbounded";
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				_maxOccurs = 1m;
				_flags &= ~Occurs.Max;
				return;
			}
			if (value == "unbounded")
			{
				_maxOccurs = decimal.MaxValue;
			}
			else
			{
				_maxOccurs = XmlConvert.ToInteger(value);
				if (_maxOccurs < 0m)
				{
					throw new XmlSchemaException(System.SR.Sch_MaxOccursInvalidXsd, string.Empty);
				}
				if (_maxOccurs == 0m && (_flags & Occurs.Min) == 0)
				{
					_minOccurs = default(decimal);
				}
			}
			_flags |= Occurs.Max;
		}
	}

	[XmlIgnore]
	public decimal MinOccurs
	{
		get
		{
			return _minOccurs;
		}
		set
		{
			if (value < 0m || value != decimal.Truncate(value))
			{
				throw new XmlSchemaException(System.SR.Sch_MinOccursInvalidXsd, string.Empty);
			}
			_minOccurs = value;
			_flags |= Occurs.Min;
		}
	}

	[XmlIgnore]
	public decimal MaxOccurs
	{
		get
		{
			return _maxOccurs;
		}
		set
		{
			if (value < 0m || value != decimal.Truncate(value))
			{
				throw new XmlSchemaException(System.SR.Sch_MaxOccursInvalidXsd, string.Empty);
			}
			_maxOccurs = value;
			if (_maxOccurs == 0m && (_flags & Occurs.Min) == 0)
			{
				_minOccurs = default(decimal);
			}
			_flags |= Occurs.Max;
		}
	}

	internal virtual bool IsEmpty => _maxOccurs == 0m;

	internal bool IsMultipleOccurrence => _maxOccurs > 1m;

	internal virtual string NameString => string.Empty;

	internal XmlQualifiedName GetQualifiedName()
	{
		if (this is XmlSchemaElement xmlSchemaElement)
		{
			return xmlSchemaElement.QualifiedName;
		}
		if (this is XmlSchemaAny xmlSchemaAny)
		{
			string @namespace = xmlSchemaAny.Namespace;
			@namespace = ((@namespace == null) ? string.Empty : @namespace.Trim());
			return new XmlQualifiedName("*", (@namespace.Length == 0) ? "##any" : @namespace);
		}
		return XmlQualifiedName.Empty;
	}
}
