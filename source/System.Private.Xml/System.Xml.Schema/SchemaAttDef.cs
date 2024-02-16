using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema;

internal sealed class SchemaAttDef : SchemaDeclBase, IDtdDefaultAttributeInfo, IDtdAttributeInfo
{
	internal enum Reserve
	{
		None,
		XmlSpace,
		XmlLang
	}

	private string _defExpanded;

	private int _lineNum;

	private int _linePos;

	private int _valueLineNum;

	private int _valueLinePos;

	private Reserve _reserved;

	private XmlSchemaAttribute _schemaAttribute;

	public static readonly SchemaAttDef Empty = new SchemaAttDef();

	string IDtdAttributeInfo.Prefix => Prefix;

	string IDtdAttributeInfo.LocalName => Name.Name;

	int IDtdAttributeInfo.LineNumber => LineNumber;

	int IDtdAttributeInfo.LinePosition => LinePosition;

	bool IDtdAttributeInfo.IsNonCDataType => TokenizedType != XmlTokenizedType.CDATA;

	bool IDtdAttributeInfo.IsDeclaredInExternal => IsDeclaredInExternal;

	bool IDtdAttributeInfo.IsXmlAttribute => Reserved != Reserve.None;

	string IDtdDefaultAttributeInfo.DefaultValueExpanded => DefaultValueExpanded;

	object IDtdDefaultAttributeInfo.DefaultValueTyped => DefaultValueTyped;

	int IDtdDefaultAttributeInfo.ValueLineNumber => ValueLineNumber;

	int IDtdDefaultAttributeInfo.ValueLinePosition => ValueLinePosition;

	internal int LinePosition
	{
		get
		{
			return _linePos;
		}
		set
		{
			_linePos = value;
		}
	}

	internal int LineNumber
	{
		get
		{
			return _lineNum;
		}
		set
		{
			_lineNum = value;
		}
	}

	internal int ValueLinePosition
	{
		get
		{
			return _valueLinePos;
		}
		set
		{
			_valueLinePos = value;
		}
	}

	internal int ValueLineNumber
	{
		get
		{
			return _valueLineNum;
		}
		set
		{
			_valueLineNum = value;
		}
	}

	internal string DefaultValueExpanded
	{
		get
		{
			if (_defExpanded == null)
			{
				return string.Empty;
			}
			return _defExpanded;
		}
		[param: AllowNull]
		set
		{
			_defExpanded = value;
		}
	}

	internal XmlTokenizedType TokenizedType
	{
		get
		{
			return base.Datatype.TokenizedType;
		}
		set
		{
			base.Datatype = XmlSchemaDatatype.FromXmlTokenizedType(value);
		}
	}

	internal Reserve Reserved
	{
		get
		{
			return _reserved;
		}
		set
		{
			_reserved = value;
		}
	}

	internal XmlSchemaAttribute SchemaAttribute
	{
		get
		{
			return _schemaAttribute;
		}
		set
		{
			_schemaAttribute = value;
		}
	}

	public SchemaAttDef(XmlQualifiedName name, string prefix)
		: base(name, prefix)
	{
	}

	public SchemaAttDef(XmlQualifiedName name)
		: base(name, null)
	{
	}

	private SchemaAttDef()
	{
	}

	internal void CheckXmlSpace(IValidationEventHandling validationEventHandling)
	{
		if (datatype.TokenizedType == XmlTokenizedType.ENUMERATION && values != null && values.Count <= 2)
		{
			string text = values[0].ToString();
			if (values.Count == 2)
			{
				string text2 = values[1].ToString();
				if ((text == "default" || text2 == "default") && (text == "preserve" || text2 == "preserve"))
				{
					return;
				}
			}
			else if (text == "default" || text == "preserve")
			{
				return;
			}
		}
		validationEventHandling.SendEvent(new XmlSchemaException(System.SR.Sch_XmlSpace, string.Empty), XmlSeverityType.Error);
	}

	internal SchemaAttDef Clone()
	{
		return (SchemaAttDef)MemberwiseClone();
	}
}
