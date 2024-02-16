using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Xsl;

internal abstract class XmlQueryType : ListBase<XmlQueryType>
{
	private enum TypeFlags
	{
		None = 0,
		IsNode = 1,
		IsAtomicValue = 2,
		IsNumeric = 4
	}

	private sealed class BitMatrix
	{
		private readonly ulong[] _bits;

		public bool this[int index1, int index2]
		{
			get
			{
				return (_bits[index1] & (ulong)(1L << index2)) != 0;
			}
			set
			{
				if (value)
				{
					_bits[index1] |= (ulong)(1L << index2);
				}
				else
				{
					_bits[index1] &= (ulong)(~(1L << index2));
				}
			}
		}

		public bool this[XmlTypeCode index1, XmlTypeCode index2] => this[(int)index1, (int)index2];

		public BitMatrix(int count)
		{
			_bits = new ulong[count];
		}
	}

	private int _hashCode;

	private static readonly TypeFlags[] s_typeCodeToFlags = new TypeFlags[55]
	{
		(TypeFlags)7,
		TypeFlags.None,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsNode,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		(TypeFlags)6,
		TypeFlags.IsAtomicValue,
		TypeFlags.IsAtomicValue
	};

	private static readonly XmlTypeCode[] s_baseTypeCodes = new XmlTypeCode[55]
	{
		XmlTypeCode.None,
		XmlTypeCode.Item,
		XmlTypeCode.Item,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Node,
		XmlTypeCode.Item,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.AnyAtomicType,
		XmlTypeCode.String,
		XmlTypeCode.NormalizedString,
		XmlTypeCode.Token,
		XmlTypeCode.Token,
		XmlTypeCode.Token,
		XmlTypeCode.Name,
		XmlTypeCode.NCName,
		XmlTypeCode.NCName,
		XmlTypeCode.NCName,
		XmlTypeCode.Decimal,
		XmlTypeCode.Integer,
		XmlTypeCode.NonPositiveInteger,
		XmlTypeCode.Integer,
		XmlTypeCode.Long,
		XmlTypeCode.Int,
		XmlTypeCode.Short,
		XmlTypeCode.Integer,
		XmlTypeCode.NonNegativeInteger,
		XmlTypeCode.UnsignedLong,
		XmlTypeCode.UnsignedInt,
		XmlTypeCode.UnsignedShort,
		XmlTypeCode.NonNegativeInteger,
		XmlTypeCode.Duration,
		XmlTypeCode.Duration
	};

	private static readonly string[] s_typeNames = new string[55]
	{
		"none", "item", "node", "document", "element", "attribute", "namespace", "processing-instruction", "comment", "text",
		"xdt:anyAtomicType", "xdt:untypedAtomic", "xs:string", "xs:boolean", "xs:decimal", "xs:float", "xs:double", "xs:duration", "xs:dateTime", "xs:time",
		"xs:date", "xs:gYearMonth", "xs:gYear", "xs:gMonthDay", "xs:gDay", "xs:gMonth", "xs:hexBinary", "xs:base64Binary", "xs:anyUri", "xs:QName",
		"xs:NOTATION", "xs:normalizedString", "xs:token", "xs:language", "xs:NMTOKEN", "xs:Name", "xs:NCName", "xs:ID", "xs:IDREF", "xs:ENTITY",
		"xs:integer", "xs:nonPositiveInteger", "xs:negativeInteger", "xs:long", "xs:int", "xs:short", "xs:byte", "xs:nonNegativeInteger", "xs:unsignedLong", "xs:unsignedInt",
		"xs:unsignedShort", "xs:unsignedByte", "xs:positiveInteger", "xdt:yearMonthDuration", "xdt:dayTimeDuration"
	};

	private static readonly BitMatrix s_typeCodeDerivation = CreateTypeCodeDerivation();

	public abstract XmlTypeCode TypeCode { get; }

	public abstract XmlQualifiedNameTest NameTest { get; }

	public abstract XmlSchemaType SchemaType { get; }

	public abstract bool IsNillable { get; }

	public abstract XmlNodeKindFlags NodeKinds { get; }

	public abstract bool IsStrict { get; }

	public abstract XmlQueryCardinality Cardinality { get; }

	public abstract XmlQueryType Prime { get; }

	public abstract bool IsNotRtf { get; }

	public abstract bool IsDod { get; }

	public bool IsEmpty => Cardinality <= XmlQueryCardinality.Zero;

	public bool IsSingleton => Cardinality <= XmlQueryCardinality.One;

	public bool MaybeEmpty => XmlQueryCardinality.Zero <= Cardinality;

	public bool MaybeMany => XmlQueryCardinality.More <= Cardinality;

	public bool IsNode => (s_typeCodeToFlags[(int)TypeCode] & TypeFlags.IsNode) != 0;

	public bool IsAtomicValue => (s_typeCodeToFlags[(int)TypeCode] & TypeFlags.IsAtomicValue) != 0;

	public bool IsSubtypeOf(XmlQueryType baseType)
	{
		if (!(Cardinality <= baseType.Cardinality) || (!IsDod && baseType.IsDod))
		{
			return false;
		}
		if (!IsDod && baseType.IsDod)
		{
			return false;
		}
		XmlQueryType prime = Prime;
		XmlQueryType prime2 = baseType.Prime;
		if ((object)prime == prime2)
		{
			return true;
		}
		if (prime.Count == 1 && prime2.Count == 1)
		{
			return prime.IsSubtypeOfItemType(prime2);
		}
		foreach (XmlQueryType item in prime)
		{
			bool flag = false;
			foreach (XmlQueryType item2 in prime2)
			{
				if (item.IsSubtypeOfItemType(item2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool NeverSubtypeOf(XmlQueryType baseType)
	{
		if (Cardinality.NeverSubset(baseType.Cardinality))
		{
			return true;
		}
		if (MaybeEmpty && baseType.MaybeEmpty)
		{
			return false;
		}
		if (Count == 0)
		{
			return false;
		}
		using (IListEnumerator<XmlQueryType> listEnumerator = GetEnumerator())
		{
			while (listEnumerator.MoveNext())
			{
				XmlQueryType current = listEnumerator.Current;
				foreach (XmlQueryType item in baseType)
				{
					if (current.HasIntersectionItemType(item))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public bool Equals([NotNullWhen(true)] XmlQueryType that)
	{
		if (that == null)
		{
			return false;
		}
		if (Cardinality != that.Cardinality || IsDod != that.IsDod)
		{
			return false;
		}
		XmlQueryType prime = Prime;
		XmlQueryType prime2 = that.Prime;
		if ((object)prime == prime2)
		{
			return true;
		}
		if (prime.Count != prime2.Count)
		{
			return false;
		}
		if (prime.Count == 1)
		{
			if (prime.TypeCode == prime2.TypeCode && prime.NameTest == prime2.NameTest && prime.SchemaType == prime2.SchemaType && prime.IsStrict == prime2.IsStrict)
			{
				return prime.IsNotRtf == prime2.IsNotRtf;
			}
			return false;
		}
		using (IListEnumerator<XmlQueryType> listEnumerator = GetEnumerator())
		{
			while (listEnumerator.MoveNext())
			{
				XmlQueryType current = listEnumerator.Current;
				bool flag = false;
				foreach (XmlQueryType item in that)
				{
					if (current.TypeCode == item.TypeCode && current.NameTest == item.NameTest && current.SchemaType == item.SchemaType && current.IsStrict == item.IsStrict && current.IsNotRtf == item.IsNotRtf)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool operator ==(XmlQueryType left, XmlQueryType right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(XmlQueryType left, XmlQueryType right)
	{
		if ((object)left == null)
		{
			return (object)right != null;
		}
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		XmlQueryType xmlQueryType = obj as XmlQueryType;
		if (xmlQueryType == null)
		{
			return false;
		}
		return Equals(xmlQueryType);
	}

	public override int GetHashCode()
	{
		if (_hashCode == 0)
		{
			int num = (int)TypeCode;
			XmlSchemaType schemaType = SchemaType;
			if (schemaType != null)
			{
				num += (num << 7) ^ schemaType.GetHashCode();
			}
			num += (int)((uint)(num << 7) ^ (uint)NodeKinds);
			num += (num << 7) ^ Cardinality.GetHashCode();
			num += (num << 7) ^ (IsStrict ? 1 : 0);
			num -= num >> 17;
			num -= num >> 11;
			num -= num >> 5;
			_hashCode = ((num == 0) ? 1 : num);
		}
		return _hashCode;
	}

	public override string ToString()
	{
		return ToString("G");
	}

	public string ToString(string format)
	{
		StringBuilder stringBuilder;
		if (format == "S")
		{
			stringBuilder = new StringBuilder();
			stringBuilder.Append(Cardinality.ToString(format));
			stringBuilder.Append(';');
			for (int i = 0; i < Count; i++)
			{
				if (i != 0)
				{
					stringBuilder.Append('|');
				}
				stringBuilder.Append(this[i].TypeCode.ToString());
			}
			stringBuilder.Append(';');
			stringBuilder.Append(IsStrict);
			return stringBuilder.ToString();
		}
		bool flag = format == "X";
		if (Cardinality == XmlQueryCardinality.None)
		{
			return "none";
		}
		if (Cardinality == XmlQueryCardinality.Zero)
		{
			return "empty";
		}
		stringBuilder = new StringBuilder();
		switch (Count)
		{
		case 0:
			stringBuilder.Append("none");
			break;
		case 1:
			stringBuilder.Append(this[0].ItemTypeToString(flag));
			break;
		default:
		{
			string[] array = new string[Count];
			for (int j = 0; j < Count; j++)
			{
				array[j] = this[j].ItemTypeToString(flag);
			}
			Array.Sort(array);
			stringBuilder = new StringBuilder();
			stringBuilder.Append('(');
			stringBuilder.Append(array[0]);
			for (int k = 1; k < array.Length; k++)
			{
				stringBuilder.Append(" | ");
				stringBuilder.Append(array[k]);
			}
			stringBuilder.Append(')');
			break;
		}
		}
		stringBuilder.Append(Cardinality.ToString());
		if (!flag && IsDod)
		{
			stringBuilder.Append('#');
		}
		return stringBuilder.ToString();
	}

	public abstract void GetObjectData(BinaryWriter writer);

	private bool IsSubtypeOfItemType(XmlQueryType baseType)
	{
		XmlSchemaType schemaType = baseType.SchemaType;
		if (TypeCode != baseType.TypeCode)
		{
			if (baseType.IsStrict)
			{
				return false;
			}
			XmlSchemaType builtInSimpleType = XmlSchemaType.GetBuiltInSimpleType(baseType.TypeCode);
			if (builtInSimpleType != null && schemaType != builtInSimpleType)
			{
				return false;
			}
			return s_typeCodeDerivation[TypeCode, baseType.TypeCode];
		}
		if (baseType.IsStrict)
		{
			if (IsStrict)
			{
				return SchemaType == schemaType;
			}
			return false;
		}
		if ((IsNotRtf || !baseType.IsNotRtf) && NameTest.IsSubsetOf(baseType.NameTest) && (schemaType == XmlSchemaComplexType.AnyType || XmlSchemaType.IsDerivedFrom(SchemaType, schemaType, XmlSchemaDerivationMethod.Empty)))
		{
			if (IsNillable)
			{
				return baseType.IsNillable;
			}
			return true;
		}
		return false;
	}

	private bool HasIntersectionItemType(XmlQueryType other)
	{
		if (TypeCode == other.TypeCode && (NodeKinds & (XmlNodeKindFlags.Document | XmlNodeKindFlags.Element | XmlNodeKindFlags.Attribute)) != 0)
		{
			if (TypeCode == XmlTypeCode.Node)
			{
				return true;
			}
			if (!NameTest.HasIntersection(other.NameTest))
			{
				return false;
			}
			if (!XmlSchemaType.IsDerivedFrom(SchemaType, other.SchemaType, XmlSchemaDerivationMethod.Empty) && !XmlSchemaType.IsDerivedFrom(other.SchemaType, SchemaType, XmlSchemaDerivationMethod.Empty))
			{
				return false;
			}
			return true;
		}
		if (IsSubtypeOf(other) || other.IsSubtypeOf(this))
		{
			return true;
		}
		return false;
	}

	private string ItemTypeToString(bool isXQ)
	{
		string text;
		if (!IsNode)
		{
			text = ((SchemaType == XmlSchemaComplexType.AnyType) ? s_typeNames[(int)TypeCode] : ((!SchemaType.QualifiedName.IsEmpty) ? QNameToString(SchemaType.QualifiedName) : ("<:" + s_typeNames[(int)TypeCode])));
		}
		else
		{
			text = s_typeNames[(int)TypeCode];
			XmlTypeCode typeCode = TypeCode;
			if (typeCode != XmlTypeCode.Document)
			{
				if ((uint)(typeCode - 4) <= 1u)
				{
					goto IL_0048;
				}
			}
			else
			{
				if (!isXQ)
				{
					goto IL_0048;
				}
				text = text + "{(element" + NameAndType(isXQ: true) + "?&text?&comment?&processing-instruction?)*}";
			}
		}
		goto IL_00b0;
		IL_00b0:
		if (!isXQ && IsStrict)
		{
			text += "=";
		}
		return text;
		IL_0048:
		text += NameAndType(isXQ);
		goto IL_00b0;
	}

	private string NameAndType(bool isXQ)
	{
		string text = NameTest.ToString();
		string text2 = "*";
		if (SchemaType.QualifiedName.IsEmpty)
		{
			text2 = "typeof(" + text + ")";
		}
		else if (isXQ || (SchemaType != XmlSchemaComplexType.AnyType && SchemaType != DatatypeImplementation.AnySimpleType))
		{
			text2 = QNameToString(SchemaType.QualifiedName);
		}
		if (IsNillable)
		{
			text2 += " nillable";
		}
		if (text == "*" && text2 == "*")
		{
			return "";
		}
		return "(" + text + ", " + text2 + ")";
	}

	private static string QNameToString(XmlQualifiedName name)
	{
		if (name.IsEmpty)
		{
			return "*";
		}
		if (name.Namespace.Length == 0)
		{
			return name.Name;
		}
		if (name.Namespace == "http://www.w3.org/2001/XMLSchema")
		{
			return "xs:" + name.Name;
		}
		if (name.Namespace == "http://www.w3.org/2003/11/xpath-datatypes")
		{
			return "xdt:" + name.Name;
		}
		return "{" + name.Namespace + "}" + name.Name;
	}

	private static BitMatrix CreateTypeCodeDerivation()
	{
		BitMatrix bitMatrix = new BitMatrix(s_baseTypeCodes.Length);
		for (int i = 0; i < s_baseTypeCodes.Length; i++)
		{
			int num = i;
			while (true)
			{
				bitMatrix[i, num] = true;
				if (s_baseTypeCodes[num] == (XmlTypeCode)num)
				{
					break;
				}
				num = (int)s_baseTypeCodes[num];
			}
		}
		return bitMatrix;
	}
}
