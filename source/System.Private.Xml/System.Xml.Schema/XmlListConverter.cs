using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;

namespace System.Xml.Schema;

internal class XmlListConverter : XmlBaseConverter
{
	protected XmlValueConverter atomicConverter;

	protected XmlListConverter(XmlBaseConverter atomicConverter)
		: base(atomicConverter)
	{
		this.atomicConverter = atomicConverter;
	}

	protected XmlListConverter(XmlBaseConverter atomicConverter, Type clrTypeDefault)
		: base(atomicConverter, clrTypeDefault)
	{
		this.atomicConverter = atomicConverter;
	}

	protected XmlListConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlValueConverter atomicConverter)
	{
		if (atomicConverter == XmlUntypedConverter.Untyped)
		{
			return XmlUntypedConverter.UntypedList;
		}
		if (atomicConverter == XmlAnyConverter.Item)
		{
			return XmlAnyListConverter.ItemList;
		}
		if (atomicConverter == XmlAnyConverter.AnyAtomic)
		{
			return XmlAnyListConverter.AnyAtomicList;
		}
		return new XmlListConverter((XmlBaseConverter)atomicConverter);
	}

	public override object ChangeType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	protected override object ChangeListType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		Type type = value.GetType();
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (!(value is IEnumerable) || !IsListType(destinationType))
		{
			throw CreateInvalidClrMappingException(type, destinationType);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			if (type == XmlBaseConverter.StringType)
			{
				return value;
			}
			return ListAsString((IEnumerable)value, nsResolver);
		}
		if (type == XmlBaseConverter.StringType)
		{
			value = StringAsList((string)value);
		}
		if (destinationType.IsArray)
		{
			Type elementType = destinationType.GetElementType();
			if (elementType == XmlBaseConverter.ObjectType)
			{
				return ToArray<object>(value, nsResolver);
			}
			if (type == destinationType)
			{
				return value;
			}
			if (elementType == XmlBaseConverter.BooleanType)
			{
				return ToArray<bool>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.ByteType)
			{
				return ToArray<byte>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.ByteArrayType)
			{
				return ToArray<byte[]>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.DateTimeType)
			{
				return ToArray<DateTime>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.DateTimeOffsetType)
			{
				return ToArray<DateTimeOffset>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.DecimalType)
			{
				return ToArray<decimal>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.DoubleType)
			{
				return ToArray<double>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.Int16Type)
			{
				return ToArray<short>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.Int32Type)
			{
				return ToArray<int>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.Int64Type)
			{
				return ToArray<long>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.SByteType)
			{
				return ToArray<sbyte>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.SingleType)
			{
				return ToArray<float>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.StringType)
			{
				return ToArray<string>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.TimeSpanType)
			{
				return ToArray<TimeSpan>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.UInt16Type)
			{
				return ToArray<ushort>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.UInt32Type)
			{
				return ToArray<uint>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.UInt64Type)
			{
				return ToArray<ulong>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.UriType)
			{
				return ToArray<Uri>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.XmlAtomicValueType)
			{
				return ToArray<XmlAtomicValue>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.XmlQualifiedNameType)
			{
				return ToArray<XmlQualifiedName>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.XPathItemType)
			{
				return ToArray<XPathItem>(value, nsResolver);
			}
			if (elementType == XmlBaseConverter.XPathNavigatorType)
			{
				return ToArray<XPathNavigator>(value, nsResolver);
			}
			throw CreateInvalidClrMappingException(type, destinationType);
		}
		if (type == base.DefaultClrType && type != XmlBaseConverter.ObjectArrayType)
		{
			return value;
		}
		return ToList(value, nsResolver);
	}

	private bool IsListType(Type type)
	{
		if (type == XmlBaseConverter.IListType || type == XmlBaseConverter.ICollectionType || type == XmlBaseConverter.IEnumerableType || type == XmlBaseConverter.StringType)
		{
			return true;
		}
		return type.IsArray;
	}

	private T[] ToArray<T>(object list, IXmlNamespaceResolver nsResolver)
	{
		if (list is IList list2)
		{
			T[] array = new T[list2.Count];
			for (int i = 0; i < list2.Count; i++)
			{
				array[i] = (T)atomicConverter.ChangeType(list2[i], typeof(T), nsResolver);
			}
			return array;
		}
		IEnumerable enumerable = list as IEnumerable;
		List<T> list3 = new List<T>();
		foreach (object item in enumerable)
		{
			list3.Add((T)atomicConverter.ChangeType(item, typeof(T), nsResolver));
		}
		return list3.ToArray();
	}

	private IList ToList(object list, IXmlNamespaceResolver nsResolver)
	{
		if (list is IList list2)
		{
			object[] array = new object[list2.Count];
			for (int i = 0; i < list2.Count; i++)
			{
				array[i] = atomicConverter.ChangeType(list2[i], XmlBaseConverter.ObjectType, nsResolver);
			}
			return array;
		}
		IEnumerable enumerable = list as IEnumerable;
		List<object> list3 = new List<object>();
		foreach (object item in enumerable)
		{
			list3.Add(atomicConverter.ChangeType(item, XmlBaseConverter.ObjectType, nsResolver));
		}
		return list3;
	}

	private List<string> StringAsList(string value)
	{
		return new List<string>(XmlConvert.SplitString(value));
	}

	private string ListAsString(IEnumerable list, IXmlNamespaceResolver nsResolver)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (object item in list)
		{
			if (item != null)
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(atomicConverter.ToString(item, nsResolver));
			}
		}
		return stringBuilder.ToString();
	}

	private new Exception CreateInvalidClrMappingException(Type sourceType, Type destinationType)
	{
		if (sourceType == destinationType)
		{
			return new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeListBadMapping, base.XmlTypeName, sourceType.Name));
		}
		return new InvalidCastException(System.SR.Format(System.SR.XmlConvert_TypeListBadMapping2, base.XmlTypeName, sourceType.Name, destinationType.Name));
	}
}
