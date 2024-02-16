using System.Collections;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlUdtStorage : DataStorage
{
	private object[] _values;

	private readonly bool _implementsIXmlSerializable;

	private readonly bool _implementsIComparable;

	private static readonly ConcurrentDictionary<Type, object> s_typeToNull = new ConcurrentDictionary<Type, object>();

	public SqlUdtStorage(DataColumn column, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		: this(column, type, GetStaticNullForUdtType(type))
	{
	}

	private SqlUdtStorage(DataColumn column, Type type, object nullValue)
		: base(column, type, nullValue, nullValue, typeof(ICloneable).IsAssignableFrom(type), DataStorage.GetStorageType(type))
	{
		_implementsIXmlSerializable = typeof(IXmlSerializable).IsAssignableFrom(type);
		_implementsIComparable = typeof(IComparable).IsAssignableFrom(type);
	}

	internal static object GetStaticNullForUdtType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
	{
		return s_typeToNull.GetOrAdd(type, (Type t) => GetStaticNullForUdtTypeCore(type));
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The only callsite is marked with DynamicallyAccessedMembers. Workaround for https://github.com/mono/linker/issues/1981")]
	private static object GetStaticNullForUdtTypeCore(Type type)
	{
		PropertyInfo property = type.GetProperty("Null", BindingFlags.Static | BindingFlags.Public);
		if (property != null)
		{
			return property.GetValue(null, null);
		}
		FieldInfo field = type.GetField("Null", BindingFlags.Static | BindingFlags.Public);
		if (field != null)
		{
			return field.GetValue(null);
		}
		throw ExceptionBuilder.INullableUDTwithoutStaticNull(type.AssemblyQualifiedName);
	}

	public override bool IsNull(int record)
	{
		return ((INullable)_values[record]).IsNull;
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return CompareValueTo(recordNo1, _values[recordNo2]);
	}

	public override int CompareValueTo(int recordNo1, object value)
	{
		if (DBNull.Value == value)
		{
			value = _nullValue;
		}
		if (_implementsIComparable)
		{
			IComparable comparable = (IComparable)_values[recordNo1];
			return comparable.CompareTo(value);
		}
		if (_nullValue == value)
		{
			INullable nullable = (INullable)_values[recordNo1];
			if (!nullable.IsNull)
			{
				return 1;
			}
			return 0;
		}
		throw ExceptionBuilder.IComparableNotImplemented(_dataType.AssemblyQualifiedName);
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		CopyBits(recordNo1, recordNo2);
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int recordNo)
	{
		return _values[recordNo];
	}

	public override void Set(int recordNo, object value)
	{
		if (DBNull.Value == value)
		{
			_values[recordNo] = _nullValue;
			SetNullBit(recordNo, flag: true);
		}
		else if (value == null)
		{
			if (_isValueType)
			{
				throw ExceptionBuilder.StorageSetFailed();
			}
			_values[recordNo] = _nullValue;
			SetNullBit(recordNo, flag: true);
		}
		else
		{
			if (!_dataType.IsInstanceOfType(value))
			{
				throw ExceptionBuilder.StorageSetFailed();
			}
			_values[recordNo] = value;
			SetNullBit(recordNo, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		object[] array = new object[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
		base.SetCapacity(capacity);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		if (_implementsIXmlSerializable)
		{
			object obj = Activator.CreateInstance(_dataType, nonPublic: true);
			string s2 = "<col>" + s + "</col>";
			StringReader input = new StringReader(s2);
			using XmlTextReader reader = new XmlTextReader(input);
			((IXmlSerializable)obj).ReadXml(reader);
			return obj;
		}
		StringReader textReader = new StringReader(s);
		XmlSerializer xmlSerializer = ObjectStorage.GetXmlSerializer(_dataType);
		return xmlSerializer.Deserialize(textReader);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
	{
		if (xmlAttrib == null)
		{
			string text = xmlReader.GetAttribute("InstanceType", "urn:schemas-microsoft-com:xml-msdata");
			if (text == null)
			{
				string attribute = xmlReader.GetAttribute("InstanceType", "http://www.w3.org/2001/XMLSchema-instance");
				if (attribute != null)
				{
					text = XSDSchema.XsdtoClr(attribute).FullName;
				}
			}
			Type type = ((text == null) ? _dataType : Type.GetType(text));
			object obj = Activator.CreateInstance(type, nonPublic: true);
			((IXmlSerializable)obj).ReadXml(xmlReader);
			return obj;
		}
		XmlSerializer xmlSerializer = ObjectStorage.GetXmlSerializer(_dataType, xmlAttrib);
		return xmlSerializer.Deserialize(xmlReader);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		StringWriter stringWriter = new StringWriter(base.FormatProvider);
		if (_implementsIXmlSerializable)
		{
			using XmlTextWriter writer = new XmlTextWriter(stringWriter);
			((IXmlSerializable)value).WriteXml(writer);
		}
		else
		{
			XmlSerializer xmlSerializer = ObjectStorage.GetXmlSerializer(value.GetType());
			xmlSerializer.Serialize(stringWriter, value);
		}
		return stringWriter.ToString();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib)
	{
		if (xmlAttrib == null)
		{
			((IXmlSerializable)value).WriteXml(xmlWriter);
			return;
		}
		XmlSerializer xmlSerializer = ObjectStorage.GetXmlSerializer(_dataType, xmlAttrib);
		xmlSerializer.Serialize(xmlWriter, value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new object[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		object[] array = (object[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (object[])store;
	}
}
