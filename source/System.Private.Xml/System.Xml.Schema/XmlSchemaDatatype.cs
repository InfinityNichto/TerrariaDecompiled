using System.Collections;
using System.Globalization;
using System.Text;

namespace System.Xml.Schema;

public abstract class XmlSchemaDatatype
{
	public abstract Type ValueType { get; }

	public abstract XmlTokenizedType TokenizedType { get; }

	public virtual XmlSchemaDatatypeVariety Variety => XmlSchemaDatatypeVariety.Atomic;

	public virtual XmlTypeCode TypeCode => XmlTypeCode.None;

	internal abstract bool HasLexicalFacets { get; }

	internal abstract bool HasValueFacets { get; }

	internal abstract XmlValueConverter ValueConverter { get; }

	internal abstract RestrictionFacets? Restriction { get; }

	internal abstract FacetsChecker FacetsChecker { get; }

	internal abstract XmlSchemaWhiteSpace BuiltInWhitespaceFacet { get; }

	internal string TypeCodeString
	{
		get
		{
			string result = string.Empty;
			XmlTypeCode typeCode = TypeCode;
			switch (Variety)
			{
			case XmlSchemaDatatypeVariety.List:
				result = ((typeCode != XmlTypeCode.AnyAtomicType) ? ("List of " + TypeCodeToString(typeCode)) : "List of Union");
				break;
			case XmlSchemaDatatypeVariety.Union:
				result = "Union";
				break;
			case XmlSchemaDatatypeVariety.Atomic:
				result = ((typeCode != XmlTypeCode.AnyAtomicType) ? TypeCodeToString(typeCode) : "anySimpleType");
				break;
			}
			return result;
		}
	}

	public abstract object ParseValue(string s, XmlNameTable? nameTable, IXmlNamespaceResolver? nsmgr);

	internal XmlSchemaDatatype()
	{
	}

	public virtual object ChangeType(object value, Type targetType)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		return ValueConverter.ChangeType(value, targetType);
	}

	public virtual object ChangeType(object value, Type targetType, IXmlNamespaceResolver namespaceResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		if (namespaceResolver == null)
		{
			throw new ArgumentNullException("namespaceResolver");
		}
		return ValueConverter.ChangeType(value, targetType, namespaceResolver);
	}

	public virtual bool IsDerivedFrom(XmlSchemaDatatype datatype)
	{
		return false;
	}

	internal abstract int Compare(object value1, object value2);

	internal abstract object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, bool createAtomicValue);

	internal abstract Exception TryParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, out object typedValue);

	internal abstract Exception TryParseValue(object value, XmlNameTable nameTable, IXmlNamespaceResolver namespaceResolver, out object typedValue);

	internal abstract XmlSchemaDatatype DeriveByRestriction(XmlSchemaObjectCollection facets, XmlNameTable nameTable, XmlSchemaType schemaType);

	internal abstract XmlSchemaDatatype DeriveByList(XmlSchemaType schemaType);

	internal abstract void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller);

	internal abstract bool IsEqual(object o1, object o2);

	internal abstract bool IsComparable(XmlSchemaDatatype dtype);

	internal string TypeCodeToString(XmlTypeCode typeCode)
	{
		return typeCode switch
		{
			XmlTypeCode.None => "None", 
			XmlTypeCode.Item => "AnyType", 
			XmlTypeCode.AnyAtomicType => "AnyAtomicType", 
			XmlTypeCode.String => "String", 
			XmlTypeCode.Boolean => "Boolean", 
			XmlTypeCode.Decimal => "Decimal", 
			XmlTypeCode.Float => "Float", 
			XmlTypeCode.Double => "Double", 
			XmlTypeCode.Duration => "Duration", 
			XmlTypeCode.DateTime => "DateTime", 
			XmlTypeCode.Time => "Time", 
			XmlTypeCode.Date => "Date", 
			XmlTypeCode.GYearMonth => "GYearMonth", 
			XmlTypeCode.GYear => "GYear", 
			XmlTypeCode.GMonthDay => "GMonthDay", 
			XmlTypeCode.GDay => "GDay", 
			XmlTypeCode.GMonth => "GMonth", 
			XmlTypeCode.HexBinary => "HexBinary", 
			XmlTypeCode.Base64Binary => "Base64Binary", 
			XmlTypeCode.AnyUri => "AnyUri", 
			XmlTypeCode.QName => "QName", 
			XmlTypeCode.Notation => "Notation", 
			XmlTypeCode.NormalizedString => "NormalizedString", 
			XmlTypeCode.Token => "Token", 
			XmlTypeCode.Language => "Language", 
			XmlTypeCode.NmToken => "NmToken", 
			XmlTypeCode.Name => "Name", 
			XmlTypeCode.NCName => "NCName", 
			XmlTypeCode.Id => "Id", 
			XmlTypeCode.Idref => "Idref", 
			XmlTypeCode.Entity => "Entity", 
			XmlTypeCode.Integer => "Integer", 
			XmlTypeCode.NonPositiveInteger => "NonPositiveInteger", 
			XmlTypeCode.NegativeInteger => "NegativeInteger", 
			XmlTypeCode.Long => "Long", 
			XmlTypeCode.Int => "Int", 
			XmlTypeCode.Short => "Short", 
			XmlTypeCode.Byte => "Byte", 
			XmlTypeCode.NonNegativeInteger => "NonNegativeInteger", 
			XmlTypeCode.UnsignedLong => "UnsignedLong", 
			XmlTypeCode.UnsignedInt => "UnsignedInt", 
			XmlTypeCode.UnsignedShort => "UnsignedShort", 
			XmlTypeCode.UnsignedByte => "UnsignedByte", 
			XmlTypeCode.PositiveInteger => "PositiveInteger", 
			_ => typeCode.ToString(), 
		};
	}

	internal static string ConcatenatedToString(object value)
	{
		Type type = value.GetType();
		string result = string.Empty;
		if (!(type == typeof(IEnumerable)) || !(type != typeof(string)))
		{
			result = ((!(value is IFormattable)) ? value.ToString() : ((IFormattable)value).ToString("", CultureInfo.InvariantCulture));
		}
		else
		{
			StringBuilder stringBuilder = new StringBuilder();
			IEnumerator enumerator = (value as IEnumerable).GetEnumerator();
			if (enumerator.MoveNext())
			{
				stringBuilder.Append('{');
				object current = enumerator.Current;
				if (current is IFormattable)
				{
					stringBuilder.Append(((IFormattable)current).ToString("", CultureInfo.InvariantCulture));
				}
				else
				{
					stringBuilder.Append(current.ToString());
				}
				while (enumerator.MoveNext())
				{
					stringBuilder.Append(" , ");
					current = enumerator.Current;
					if (current is IFormattable)
					{
						stringBuilder.Append(((IFormattable)current).ToString("", CultureInfo.InvariantCulture));
					}
					else
					{
						stringBuilder.Append(current.ToString());
					}
				}
				stringBuilder.Append('}');
				result = stringBuilder.ToString();
			}
		}
		return result;
	}

	internal static XmlSchemaDatatype FromXmlTokenizedType(XmlTokenizedType token)
	{
		return DatatypeImplementation.FromXmlTokenizedType(token);
	}

	internal static XmlSchemaDatatype FromXmlTokenizedTypeXsd(XmlTokenizedType token)
	{
		return DatatypeImplementation.FromXmlTokenizedTypeXsd(token);
	}

	internal static XmlSchemaDatatype FromXdrName(string name)
	{
		return DatatypeImplementation.FromXdrName(name);
	}

	internal static XmlSchemaDatatype DeriveByUnion(XmlSchemaSimpleType[] types, XmlSchemaType schemaType)
	{
		return DatatypeImplementation.DeriveByUnion(types, schemaType);
	}

	internal static string XdrCanonizeUri(string uri, XmlNameTable nameTable, SchemaNames schemaNames)
	{
		int num = 5;
		bool flag = false;
		if (uri.Length > 5 && uri.StartsWith("uuid:", StringComparison.Ordinal))
		{
			flag = true;
		}
		else if (uri.Length > 9 && uri.StartsWith("urn:uuid:", StringComparison.Ordinal))
		{
			flag = true;
			num = 9;
		}
		string text = ((!flag) ? uri : nameTable.Add(string.Concat(uri.AsSpan(0, num), uri.Substring(num, uri.Length - num).ToUpperInvariant())));
		if (Ref.Equal(schemaNames.NsDataTypeAlias, text) || Ref.Equal(schemaNames.NsDataTypeOld, text))
		{
			text = schemaNames.NsDataType;
		}
		else if (Ref.Equal(schemaNames.NsXdrAlias, text))
		{
			text = schemaNames.NsXdr;
		}
		return text;
	}
}
