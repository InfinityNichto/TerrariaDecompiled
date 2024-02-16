using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class XsltConvert
{
	internal static readonly Type BooleanType = typeof(bool);

	internal static readonly Type ByteArrayType = typeof(byte[]);

	internal static readonly Type ByteType = typeof(byte);

	internal static readonly Type DateTimeType = typeof(DateTime);

	internal static readonly Type DecimalType = typeof(decimal);

	internal static readonly Type DoubleType = typeof(double);

	internal static readonly Type ICollectionType = typeof(ICollection);

	internal static readonly Type IEnumerableType = typeof(IEnumerable);

	internal static readonly Type IListType = typeof(IList);

	internal static readonly Type Int16Type = typeof(short);

	internal static readonly Type Int32Type = typeof(int);

	internal static readonly Type Int64Type = typeof(long);

	internal static readonly Type IXPathNavigableType = typeof(IXPathNavigable);

	internal static readonly Type ObjectType = typeof(object);

	internal static readonly Type SByteType = typeof(sbyte);

	internal static readonly Type SingleType = typeof(float);

	internal static readonly Type StringType = typeof(string);

	internal static readonly Type TimeSpanType = typeof(TimeSpan);

	internal static readonly Type UInt16Type = typeof(ushort);

	internal static readonly Type UInt32Type = typeof(uint);

	internal static readonly Type UInt64Type = typeof(ulong);

	internal static readonly Type UriType = typeof(Uri);

	internal static readonly Type VoidType = typeof(void);

	internal static readonly Type XmlAtomicValueType = typeof(XmlAtomicValue);

	internal static readonly Type XmlQualifiedNameType = typeof(XmlQualifiedName);

	internal static readonly Type XPathItemType = typeof(XPathItem);

	internal static readonly Type XPathNavigatorArrayType = typeof(XPathNavigator[]);

	internal static readonly Type XPathNavigatorType = typeof(XPathNavigator);

	internal static readonly Type XPathNodeIteratorType = typeof(XPathNodeIterator);

	public static bool ToBoolean(XPathItem item)
	{
		if (item.IsNode)
		{
			return true;
		}
		Type valueType = item.ValueType;
		if (valueType == StringType)
		{
			return item.Value.Length != 0;
		}
		if (valueType == DoubleType)
		{
			double valueAsDouble = item.ValueAsDouble;
			if (!(valueAsDouble < 0.0))
			{
				return 0.0 < valueAsDouble;
			}
			return true;
		}
		return item.ValueAsBoolean;
	}

	public static bool ToBoolean(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return false;
		}
		return ToBoolean(listItems[0]);
	}

	public static double ToDouble(string value)
	{
		return XPathConvert.StringToDouble(value);
	}

	public static double ToDouble(XPathItem item)
	{
		if (item.IsNode)
		{
			return XPathConvert.StringToDouble(item.Value);
		}
		Type valueType = item.ValueType;
		if (valueType == StringType)
		{
			return XPathConvert.StringToDouble(item.Value);
		}
		if (valueType == DoubleType)
		{
			return item.ValueAsDouble;
		}
		if (!item.ValueAsBoolean)
		{
			return 0.0;
		}
		return 1.0;
	}

	public static double ToDouble(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return double.NaN;
		}
		return ToDouble(listItems[0]);
	}

	public static XPathNavigator ToNode(XPathItem item)
	{
		if (!item.IsNode)
		{
			XPathDocument xPathDocument = new XPathDocument();
			XmlRawWriter xmlRawWriter = xPathDocument.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, string.Empty);
			xmlRawWriter.WriteString(ToString(item));
			xmlRawWriter.Close();
			return xPathDocument.CreateNavigator();
		}
		if (item is RtfNavigator rtfNavigator)
		{
			return rtfNavigator.ToNavigator();
		}
		return (XPathNavigator)item;
	}

	public static XPathNavigator ToNode(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			return ToNode(listItems[0]);
		}
		throw new XslTransformException(System.SR.Xslt_NodeSetNotNode, string.Empty);
	}

	public static IList<XPathNavigator> ToNodeSet(XPathItem item)
	{
		return new XmlQueryNodeSequence(ToNode(item));
	}

	public static IList<XPathNavigator> ToNodeSet(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			return new XmlQueryNodeSequence(ToNode(listItems[0]));
		}
		return XmlILStorageConverter.ItemsToNavigators(listItems);
	}

	public static string ToString(double value)
	{
		return XPathConvert.DoubleToString(value);
	}

	public static string ToString(XPathItem item)
	{
		if (!item.IsNode && item.ValueType == DoubleType)
		{
			return XPathConvert.DoubleToString(item.ValueAsDouble);
		}
		return item.Value;
	}

	public static string ToString(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return string.Empty;
		}
		return ToString(listItems[0]);
	}

	public static string ToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime).ToString();
	}

	public static double ToDouble(decimal value)
	{
		return (double)value;
	}

	public static double ToDouble(int value)
	{
		return value;
	}

	public static double ToDouble(long value)
	{
		return value;
	}

	public static decimal ToDecimal(double value)
	{
		return (decimal)value;
	}

	public static int ToInt(double value)
	{
		return checked((int)value);
	}

	public static long ToLong(double value)
	{
		return checked((long)value);
	}

	public static DateTime ToDateTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}

	internal static XmlAtomicValue ConvertToType(XmlAtomicValue value, XmlQueryType destinationType)
	{
		switch (destinationType.TypeCode)
		{
		case XmlTypeCode.Boolean:
		{
			XmlTypeCode typeCode = value.XmlType.TypeCode;
			if ((uint)(typeCode - 12) <= 1u || typeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToBoolean(value));
			}
			break;
		}
		case XmlTypeCode.DateTime:
			if (value.XmlType.TypeCode == XmlTypeCode.String)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToDateTime(value.Value));
			}
			break;
		case XmlTypeCode.Decimal:
			if (value.XmlType.TypeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToDecimal(value.ValueAsDouble));
			}
			break;
		case XmlTypeCode.Double:
			switch (value.XmlType.TypeCode)
			{
			case XmlTypeCode.String:
			case XmlTypeCode.Boolean:
			case XmlTypeCode.Double:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value));
			case XmlTypeCode.Decimal:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble((decimal)value.ValueAs(DecimalType, null)));
			case XmlTypeCode.Long:
			case XmlTypeCode.Int:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value.ValueAsLong));
			}
			break;
		case XmlTypeCode.Long:
		case XmlTypeCode.Int:
			if (value.XmlType.TypeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToLong(value.ValueAsDouble));
			}
			break;
		case XmlTypeCode.String:
			switch (value.XmlType.TypeCode)
			{
			case XmlTypeCode.String:
			case XmlTypeCode.Boolean:
			case XmlTypeCode.Double:
				return new XmlAtomicValue(destinationType.SchemaType, ToString(value));
			case XmlTypeCode.DateTime:
				return new XmlAtomicValue(destinationType.SchemaType, ToString(value.ValueAsDateTime));
			}
			break;
		}
		return value;
	}

	public static IList<XPathNavigator> EnsureNodeSet(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			XPathItem xPathItem = listItems[0];
			if (!xPathItem.IsNode)
			{
				throw new XslTransformException(System.SR.XPath_NodeSetExpected, string.Empty);
			}
			if (xPathItem is RtfNavigator)
			{
				throw new XslTransformException(System.SR.XPath_RtfInPathExpr, string.Empty);
			}
		}
		return XmlILStorageConverter.ItemsToNavigators(listItems);
	}

	internal static XmlQueryType InferXsltType(Type clrType)
	{
		if (clrType == BooleanType)
		{
			return XmlQueryTypeFactory.BooleanX;
		}
		if (clrType == ByteType)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == DecimalType)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == DateTimeType)
		{
			return XmlQueryTypeFactory.StringX;
		}
		if (clrType == DoubleType)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == Int16Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == Int32Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == Int64Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == IXPathNavigableType)
		{
			return XmlQueryTypeFactory.NodeNotRtf;
		}
		if (clrType == SByteType)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == SingleType)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == StringType)
		{
			return XmlQueryTypeFactory.StringX;
		}
		if (clrType == UInt16Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == UInt32Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == UInt64Type)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == XPathNavigatorArrayType)
		{
			return XmlQueryTypeFactory.NodeSDod;
		}
		if (clrType == XPathNavigatorType)
		{
			return XmlQueryTypeFactory.NodeNotRtf;
		}
		if (clrType == XPathNodeIteratorType)
		{
			return XmlQueryTypeFactory.NodeSDod;
		}
		if (clrType.IsEnum)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == VoidType)
		{
			return XmlQueryTypeFactory.Empty;
		}
		return XmlQueryTypeFactory.ItemS;
	}
}
