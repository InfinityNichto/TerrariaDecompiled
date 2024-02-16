using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema;

internal abstract class SchemaDeclBase
{
	internal enum Use
	{
		Default,
		Required,
		Implied,
		Fixed,
		RequiredFixed
	}

	protected XmlQualifiedName name = XmlQualifiedName.Empty;

	protected string prefix;

	protected bool isDeclaredInExternal;

	protected Use presence;

	protected XmlSchemaType schemaType;

	protected XmlSchemaDatatype datatype;

	protected string defaultValueRaw;

	protected object defaultValueTyped;

	protected long maxLength;

	protected long minLength;

	protected List<string> values;

	internal XmlQualifiedName Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	internal string Prefix
	{
		get
		{
			if (prefix != null)
			{
				return prefix;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			prefix = value;
		}
	}

	internal bool IsDeclaredInExternal
	{
		get
		{
			return isDeclaredInExternal;
		}
		set
		{
			isDeclaredInExternal = value;
		}
	}

	internal Use Presence
	{
		get
		{
			return presence;
		}
		set
		{
			presence = value;
		}
	}

	internal long MaxLength
	{
		get
		{
			return maxLength;
		}
		set
		{
			maxLength = value;
		}
	}

	internal long MinLength
	{
		get
		{
			return minLength;
		}
		set
		{
			minLength = value;
		}
	}

	internal XmlSchemaType SchemaType
	{
		get
		{
			return schemaType;
		}
		set
		{
			schemaType = value;
		}
	}

	internal XmlSchemaDatatype Datatype
	{
		get
		{
			return datatype;
		}
		set
		{
			datatype = value;
		}
	}

	internal List<string> Values
	{
		get
		{
			return values;
		}
		set
		{
			values = value;
		}
	}

	internal string DefaultValueRaw
	{
		get
		{
			if (defaultValueRaw == null)
			{
				return string.Empty;
			}
			return defaultValueRaw;
		}
		set
		{
			defaultValueRaw = value;
		}
	}

	internal object DefaultValueTyped
	{
		get
		{
			return defaultValueTyped;
		}
		set
		{
			defaultValueTyped = value;
		}
	}

	protected SchemaDeclBase(XmlQualifiedName name, string prefix)
	{
		this.name = name;
		this.prefix = prefix;
		maxLength = -1L;
		minLength = -1L;
	}

	protected SchemaDeclBase()
	{
	}

	internal void AddValue(string value)
	{
		if (values == null)
		{
			values = new List<string>();
		}
		values.Add(value);
	}

	internal bool CheckEnumeration(object pVal)
	{
		if (datatype.TokenizedType == XmlTokenizedType.NOTATION || datatype.TokenizedType == XmlTokenizedType.ENUMERATION)
		{
			return values.Contains(pVal.ToString());
		}
		return true;
	}

	internal bool CheckValue(object pVal)
	{
		if (presence == Use.Fixed || presence == Use.RequiredFixed)
		{
			if (defaultValueTyped != null)
			{
				return datatype.IsEqual(pVal, defaultValueTyped);
			}
			return false;
		}
		return true;
	}
}
