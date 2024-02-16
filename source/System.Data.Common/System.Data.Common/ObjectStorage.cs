using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class ObjectStorage : DataStorage
{
	private enum Families
	{
		DATETIME,
		NUMBER,
		STRING,
		BOOLEAN,
		ARRAY
	}

	private sealed class TempAssemblyComparer : IEqualityComparer<KeyValuePair<Type, XmlRootAttribute>>
	{
		internal static readonly IEqualityComparer<KeyValuePair<Type, XmlRootAttribute>> s_default = new TempAssemblyComparer();

		private TempAssemblyComparer()
		{
		}

		public bool Equals(KeyValuePair<Type, XmlRootAttribute> x, KeyValuePair<Type, XmlRootAttribute> y)
		{
			if (x.Key == y.Key)
			{
				if (x.Value != null || y.Value != null)
				{
					if (x.Value != null && y.Value != null && x.Value.ElementName == y.Value.ElementName && x.Value.Namespace == y.Value.Namespace && x.Value.DataType == y.Value.DataType)
					{
						return x.Value.IsNullable == y.Value.IsNullable;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public int GetHashCode(KeyValuePair<Type, XmlRootAttribute> obj)
		{
			return obj.Key.GetHashCode() + obj.Value.ElementName.GetHashCode();
		}
	}

	private object[] _values;

	private readonly bool _implementsIXmlSerializable;

	private static readonly object s_tempAssemblyCacheLock = new object();

	private static Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> s_tempAssemblyCache;

	private static readonly XmlSerializerFactory s_serializerFactory = new XmlSerializerFactory();

	internal ObjectStorage(DataColumn column, Type type)
		: base(column, type, null, DBNull.Value, typeof(ICloneable).IsAssignableFrom(type), DataStorage.GetStorageType(type))
	{
		_implementsIXmlSerializable = typeof(IXmlSerializable).IsAssignableFrom(type);
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		object obj = _values[recordNo1];
		object obj2 = _values[recordNo2];
		if (obj == obj2)
		{
			return 0;
		}
		if (obj == null)
		{
			return -1;
		}
		if (obj2 == null)
		{
			return 1;
		}
		if (obj is IComparable comparable)
		{
			try
			{
				return comparable.CompareTo(obj2);
			}
			catch (ArgumentException e)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			}
		}
		return CompareWithFamilies(obj, obj2);
	}

	public override int CompareValueTo(int recordNo1, object value)
	{
		object obj = Get(recordNo1);
		if (obj is IComparable && value.GetType() == obj.GetType())
		{
			return ((IComparable)obj).CompareTo(value);
		}
		if (obj == value)
		{
			return 0;
		}
		if (obj == null)
		{
			if (_nullValue == value)
			{
				return 0;
			}
			return -1;
		}
		if (_nullValue == value || value == null)
		{
			return 1;
		}
		return CompareWithFamilies(obj, value);
	}

	private int CompareTo(object valueNo1, object valueNo2)
	{
		if (valueNo1 == null)
		{
			return -1;
		}
		if (valueNo2 == null)
		{
			return 1;
		}
		if (valueNo1 == valueNo2)
		{
			return 0;
		}
		if (valueNo1 == _nullValue)
		{
			return -1;
		}
		if (valueNo2 == _nullValue)
		{
			return 1;
		}
		if (valueNo1 is IComparable)
		{
			try
			{
				return ((IComparable)valueNo1).CompareTo(valueNo2);
			}
			catch (ArgumentException e)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			}
		}
		return CompareWithFamilies(valueNo1, valueNo2);
	}

	private int CompareWithFamilies(object valueNo1, object valueNo2)
	{
		Families family = GetFamily(valueNo1.GetType());
		Families family2 = GetFamily(valueNo2.GetType());
		if (family < family2)
		{
			return -1;
		}
		if (family > family2)
		{
			return 1;
		}
		switch (family)
		{
		case Families.BOOLEAN:
			valueNo1 = Convert.ToBoolean(valueNo1, base.FormatProvider);
			valueNo2 = Convert.ToBoolean(valueNo2, base.FormatProvider);
			break;
		case Families.DATETIME:
			valueNo1 = Convert.ToDateTime(valueNo1, base.FormatProvider);
			valueNo2 = Convert.ToDateTime(valueNo1, base.FormatProvider);
			break;
		case Families.NUMBER:
			valueNo1 = Convert.ToDouble(valueNo1, base.FormatProvider);
			valueNo2 = Convert.ToDouble(valueNo2, base.FormatProvider);
			break;
		case Families.ARRAY:
		{
			Array array = (Array)valueNo1;
			Array array2 = (Array)valueNo2;
			if (array.Length > array2.Length)
			{
				return 1;
			}
			if (array.Length < array2.Length)
			{
				return -1;
			}
			for (int i = 0; i < array.Length; i++)
			{
				int num = CompareTo(array.GetValue(i), array2.GetValue(i));
				if (num != 0)
				{
					return num;
				}
			}
			return 0;
		}
		default:
			valueNo1 = valueNo1.ToString();
			valueNo2 = valueNo2.ToString();
			break;
		}
		return ((IComparable)valueNo1).CompareTo(valueNo2);
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int recordNo)
	{
		object obj = _values[recordNo];
		if (obj != null)
		{
			return obj;
		}
		return _nullValue;
	}

	private Families GetFamily(Type dataType)
	{
		switch (Type.GetTypeCode(dataType))
		{
		case TypeCode.Boolean:
			return Families.BOOLEAN;
		case TypeCode.Char:
			return Families.STRING;
		case TypeCode.SByte:
			return Families.STRING;
		case TypeCode.Byte:
			return Families.STRING;
		case TypeCode.Int16:
			return Families.NUMBER;
		case TypeCode.UInt16:
			return Families.NUMBER;
		case TypeCode.Int32:
			return Families.NUMBER;
		case TypeCode.UInt32:
			return Families.NUMBER;
		case TypeCode.Int64:
			return Families.NUMBER;
		case TypeCode.UInt64:
			return Families.NUMBER;
		case TypeCode.Single:
			return Families.NUMBER;
		case TypeCode.Double:
			return Families.NUMBER;
		case TypeCode.Decimal:
			return Families.NUMBER;
		case TypeCode.DateTime:
			return Families.DATETIME;
		case TypeCode.String:
			return Families.STRING;
		default:
			if (typeof(TimeSpan) == dataType)
			{
				return Families.DATETIME;
			}
			if (dataType.IsArray)
			{
				return Families.ARRAY;
			}
			return Families.STRING;
		}
	}

	public override bool IsNull(int record)
	{
		return _values[record] == null;
	}

	public override void Set(int recordNo, object value)
	{
		if (_nullValue == value)
		{
			_values[recordNo] = null;
			return;
		}
		if (_dataType == typeof(object) || _dataType.IsInstanceOfType(value))
		{
			_values[recordNo] = value;
			return;
		}
		Type type = value.GetType();
		if (_dataType == typeof(Guid) && type == typeof(string))
		{
			_values[recordNo] = new Guid((string)value);
			return;
		}
		if (_dataType == typeof(byte[]))
		{
			if (type == typeof(bool))
			{
				_values[recordNo] = BitConverter.GetBytes((bool)value);
				return;
			}
			if (type == typeof(char))
			{
				_values[recordNo] = BitConverter.GetBytes((char)value);
				return;
			}
			if (type == typeof(short))
			{
				_values[recordNo] = BitConverter.GetBytes((short)value);
				return;
			}
			if (type == typeof(int))
			{
				_values[recordNo] = BitConverter.GetBytes((int)value);
				return;
			}
			if (type == typeof(long))
			{
				_values[recordNo] = BitConverter.GetBytes((long)value);
				return;
			}
			if (type == typeof(ushort))
			{
				_values[recordNo] = BitConverter.GetBytes((ushort)value);
				return;
			}
			if (type == typeof(uint))
			{
				_values[recordNo] = BitConverter.GetBytes((uint)value);
				return;
			}
			if (type == typeof(ulong))
			{
				_values[recordNo] = BitConverter.GetBytes((ulong)value);
				return;
			}
			if (type == typeof(float))
			{
				_values[recordNo] = BitConverter.GetBytes((float)value);
				return;
			}
			if (type == typeof(double))
			{
				_values[recordNo] = BitConverter.GetBytes((double)value);
				return;
			}
			throw ExceptionBuilder.StorageSetFailed();
		}
		throw ExceptionBuilder.StorageSetFailed();
	}

	public override void SetCapacity(int capacity)
	{
		object[] array = new object[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		Type dataType = _dataType;
		if (dataType == typeof(byte[]))
		{
			return Convert.FromBase64String(s);
		}
		if (dataType == typeof(Type))
		{
			return Type.GetType(s);
		}
		if (dataType == typeof(Guid))
		{
			return new Guid(s);
		}
		if (dataType == typeof(Uri))
		{
			return new Uri(s);
		}
		if (_implementsIXmlSerializable)
		{
			object obj = Activator.CreateInstance(_dataType, nonPublic: true);
			StringReader input = new StringReader(s);
			using XmlTextReader reader = new XmlTextReader(input);
			((IXmlSerializable)obj).ReadXml(reader);
			return obj;
		}
		StringReader textReader = new StringReader(s);
		XmlSerializer xmlSerializer = GetXmlSerializer(dataType);
		return xmlSerializer.Deserialize(textReader);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
	{
		object obj = null;
		bool flag = false;
		bool flag2 = false;
		if (xmlAttrib == null)
		{
			Type type = null;
			string attribute = xmlReader.GetAttribute("InstanceType", "urn:schemas-microsoft-com:xml-msdata");
			if (attribute == null || attribute.Length == 0)
			{
				string text = xmlReader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
				if (text != null && text.Length > 0)
				{
					string[] array = text.Split(':');
					if (array.Length == 2 && xmlReader.LookupNamespace(array[0]) == "http://www.w3.org/2001/XMLSchema")
					{
						text = array[1];
					}
					type = XSDSchema.XsdtoClr(text);
					flag = true;
				}
				else if (_dataType == typeof(object))
				{
					flag2 = true;
				}
			}
			if (flag2)
			{
				obj = xmlReader.ReadString();
			}
			else if (attribute == "Type")
			{
				obj = Type.GetType(xmlReader.ReadString());
				xmlReader.Read();
			}
			else
			{
				if (null == type)
				{
					type = ((attribute == null) ? _dataType : DataStorage.GetType(attribute));
				}
				if (type == typeof(char) || type == typeof(Guid))
				{
					flag = true;
				}
				if (type == typeof(object))
				{
					throw ExceptionBuilder.CanNotDeserializeObjectType();
				}
				TypeLimiter.EnsureTypeIsAllowed(type);
				if (!flag)
				{
					obj = Activator.CreateInstance(type, nonPublic: true);
					((IXmlSerializable)obj).ReadXml(xmlReader);
				}
				else
				{
					if (type == typeof(string) && xmlReader.NodeType == XmlNodeType.Element && xmlReader.IsEmptyElement)
					{
						obj = string.Empty;
					}
					else
					{
						obj = xmlReader.ReadString();
						obj = ((!(type != typeof(byte[]))) ? Convert.FromBase64String(obj.ToString()) : SqlConvert.ChangeTypeForXML(obj, type));
					}
					xmlReader.Read();
				}
			}
		}
		else
		{
			XmlSerializer xmlSerializer = GetXmlSerializer(_dataType, xmlAttrib);
			obj = xmlSerializer.Deserialize(xmlReader);
		}
		return obj;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		if (value == null || value == _nullValue)
		{
			return string.Empty;
		}
		Type dataType = _dataType;
		if (dataType == typeof(byte[]) || (dataType == typeof(object) && value is byte[]))
		{
			return Convert.ToBase64String((byte[])value);
		}
		if (dataType == typeof(Type) || (dataType == typeof(object) && value is Type))
		{
			return ((Type)value).AssemblyQualifiedName;
		}
		if (!DataStorage.IsTypeCustomType(value.GetType()))
		{
			return (string)SqlConvert.ChangeTypeForXML(value, typeof(string));
		}
		if (Type.GetTypeCode(value.GetType()) != TypeCode.Object)
		{
			return value.ToString();
		}
		StringWriter stringWriter = new StringWriter(base.FormatProvider);
		if (_implementsIXmlSerializable)
		{
			using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
			{
				((IXmlSerializable)value).WriteXml(writer);
			}
			return stringWriter.ToString();
		}
		XmlSerializer xmlSerializer = GetXmlSerializer(value.GetType());
		xmlSerializer.Serialize(stringWriter, value);
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
		XmlSerializer xmlSerializer = GetXmlSerializer(value.GetType(), xmlAttrib);
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
		bool flag = IsNull(record);
		nullbits.Set(storeIndex, flag);
		if (!flag && array[storeIndex] is DateTime { Kind: DateTimeKind.Local } dateTime)
		{
			array[storeIndex] = DateTime.SpecifyKind(dateTime.ToUniversalTime(), DateTimeKind.Local);
		}
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (object[])store;
		for (int i = 0; i < _values.Length; i++)
		{
			if (_values[i] is DateTime { Kind: DateTimeKind.Local } value)
			{
				_values[i] = DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
			}
		}
	}

	internal static void VerifyIDynamicMetaObjectProvider(Type type)
	{
		if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type) && !typeof(IXmlSerializable).IsAssignableFrom(type))
		{
			throw ADP.InvalidOperation(System.SR.Xml_DynamicWithoutXmlSerializable);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal static XmlSerializer GetXmlSerializer(Type type)
	{
		VerifyIDynamicMetaObjectProvider(type);
		return s_serializerFactory.CreateSerializer(type);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal static XmlSerializer GetXmlSerializer(Type type, XmlRootAttribute attribute)
	{
		XmlSerializer value = null;
		KeyValuePair<Type, XmlRootAttribute> key = new KeyValuePair<Type, XmlRootAttribute>(type, attribute);
		Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> dictionary = s_tempAssemblyCache;
		if (dictionary == null || !dictionary.TryGetValue(key, out value))
		{
			lock (s_tempAssemblyCacheLock)
			{
				dictionary = s_tempAssemblyCache;
				if (dictionary == null || !dictionary.TryGetValue(key, out value))
				{
					VerifyIDynamicMetaObjectProvider(type);
					if (dictionary != null)
					{
						Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> dictionary2 = new Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer>(1 + dictionary.Count, TempAssemblyComparer.s_default);
						foreach (KeyValuePair<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> item in dictionary)
						{
							dictionary2.Add(item.Key, item.Value);
						}
						dictionary = dictionary2;
					}
					else
					{
						dictionary = new Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer>(TempAssemblyComparer.s_default);
					}
					key = new KeyValuePair<Type, XmlRootAttribute>(type, new XmlRootAttribute());
					key.Value.ElementName = attribute.ElementName;
					key.Value.Namespace = attribute.Namespace;
					key.Value.DataType = attribute.DataType;
					key.Value.IsNullable = attribute.IsNullable;
					value = s_serializerFactory.CreateSerializer(type, attribute);
					dictionary.Add(key, value);
					s_tempAssemblyCache = dictionary;
				}
			}
		}
		return value;
	}
}
