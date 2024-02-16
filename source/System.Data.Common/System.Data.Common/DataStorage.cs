using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal abstract class DataStorage
{
	private static readonly Type[] s_storageClassType = new Type[41]
	{
		null,
		typeof(object),
		typeof(DBNull),
		typeof(bool),
		typeof(char),
		typeof(sbyte),
		typeof(byte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(decimal),
		typeof(DateTime),
		typeof(TimeSpan),
		typeof(string),
		typeof(Guid),
		typeof(byte[]),
		typeof(char[]),
		typeof(Type),
		typeof(DateTimeOffset),
		typeof(BigInteger),
		typeof(Uri),
		typeof(SqlBinary),
		typeof(SqlBoolean),
		typeof(SqlByte),
		typeof(SqlBytes),
		typeof(SqlChars),
		typeof(SqlDateTime),
		typeof(SqlDecimal),
		typeof(SqlDouble),
		typeof(SqlGuid),
		typeof(SqlInt16),
		typeof(SqlInt32),
		typeof(SqlInt64),
		typeof(SqlMoney),
		typeof(SqlSingle),
		typeof(SqlString)
	};

	internal readonly DataColumn _column;

	internal readonly DataTable _table;

	internal readonly Type _dataType;

	internal readonly StorageType _storageTypeCode;

	private BitArray _dbNullBits;

	private readonly object _defaultValue;

	internal readonly object _nullValue;

	internal readonly bool _isCloneable;

	internal readonly bool _isCustomDefinedType;

	internal readonly bool _isStringType;

	internal readonly bool _isValueType;

	private static readonly Func<Type, Tuple<bool, bool, bool, bool>> s_inspectTypeForInterfaces = InspectTypeForInterfaces;

	private static readonly ConcurrentDictionary<Type, Tuple<bool, bool, bool, bool>> s_typeImplementsInterface = new ConcurrentDictionary<Type, Tuple<bool, bool, bool, bool>>();

	internal DataSetDateTime DateTimeMode => _column.DateTimeMode;

	internal IFormatProvider FormatProvider => _table.FormatProvider;

	protected DataStorage(DataColumn column, Type type, object defaultValue, StorageType storageType)
		: this(column, type, defaultValue, DBNull.Value, isICloneable: false, storageType)
	{
	}

	protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue, StorageType storageType)
		: this(column, type, defaultValue, nullValue, isICloneable: false, storageType)
	{
	}

	protected DataStorage(DataColumn column, Type type, object defaultValue, object nullValue, bool isICloneable, StorageType storageType)
	{
		_column = column;
		_table = column.Table;
		_dataType = type;
		_storageTypeCode = storageType;
		_defaultValue = defaultValue;
		_nullValue = nullValue;
		_isCloneable = isICloneable;
		_isCustomDefinedType = IsTypeCustomType(_storageTypeCode);
		_isStringType = StorageType.String == _storageTypeCode || StorageType.SqlString == _storageTypeCode;
		_isValueType = DetermineIfValueType(_storageTypeCode, type);
	}

	public virtual object Aggregate(int[] recordNos, AggregateType kind)
	{
		if (AggregateType.Count == kind)
		{
			return AggregateCount(recordNos);
		}
		return null;
	}

	public object AggregateCount(int[] recordNos)
	{
		int num = 0;
		for (int i = 0; i < recordNos.Length; i++)
		{
			if (!_dbNullBits.Get(recordNos[i]))
			{
				num++;
			}
		}
		return num;
	}

	protected int CompareBits(int recordNo1, int recordNo2)
	{
		bool flag = _dbNullBits.Get(recordNo1);
		bool flag2 = _dbNullBits.Get(recordNo2);
		if (flag ^ flag2)
		{
			if (flag)
			{
				return -1;
			}
			return 1;
		}
		return 0;
	}

	public abstract int Compare(int recordNo1, int recordNo2);

	public abstract int CompareValueTo(int recordNo1, object value);

	public virtual object ConvertValue(object value)
	{
		return value;
	}

	protected void CopyBits(int srcRecordNo, int dstRecordNo)
	{
		_dbNullBits.Set(dstRecordNo, _dbNullBits.Get(srcRecordNo));
	}

	public abstract void Copy(int recordNo1, int recordNo2);

	public abstract object Get(int recordNo);

	protected object GetBits(int recordNo)
	{
		if (_dbNullBits.Get(recordNo))
		{
			return _nullValue;
		}
		return _defaultValue;
	}

	public virtual int GetStringLength(int record)
	{
		return int.MaxValue;
	}

	protected bool HasValue(int recordNo)
	{
		return !_dbNullBits.Get(recordNo);
	}

	public virtual bool IsNull(int recordNo)
	{
		return _dbNullBits.Get(recordNo);
	}

	public abstract void Set(int recordNo, object value);

	protected void SetNullBit(int recordNo, bool flag)
	{
		_dbNullBits.Set(recordNo, flag);
	}

	public virtual void SetCapacity(int capacity)
	{
		if (_dbNullBits == null)
		{
			_dbNullBits = new BitArray(capacity);
		}
		else
		{
			_dbNullBits.Length = capacity;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public abstract object ConvertXmlToObject(string s);

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public virtual object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
	{
		return ConvertXmlToObject(xmlReader.Value);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public abstract string ConvertObjectToXml(object value);

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public virtual void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib)
	{
		xmlWriter.WriteString(ConvertObjectToXml(value));
	}

	public static DataStorage CreateStorage(DataColumn column, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type dataType, StorageType typeCode)
	{
		if (typeCode == StorageType.Empty && null != dataType)
		{
			if (typeof(INullable).IsAssignableFrom(dataType))
			{
				return new SqlUdtStorage(column, dataType);
			}
			return new ObjectStorage(column, dataType);
		}
		return typeCode switch
		{
			StorageType.Empty => throw ExceptionBuilder.InvalidStorageType(TypeCode.Empty), 
			StorageType.DBNull => throw ExceptionBuilder.InvalidStorageType(TypeCode.DBNull), 
			StorageType.Boolean => new BooleanStorage(column), 
			StorageType.Char => new CharStorage(column), 
			StorageType.SByte => new SByteStorage(column), 
			StorageType.Byte => new ByteStorage(column), 
			StorageType.Int16 => new Int16Storage(column), 
			StorageType.UInt16 => new UInt16Storage(column), 
			StorageType.Int32 => new Int32Storage(column), 
			StorageType.UInt32 => new UInt32Storage(column), 
			StorageType.Int64 => new Int64Storage(column), 
			StorageType.UInt64 => new UInt64Storage(column), 
			StorageType.Single => new SingleStorage(column), 
			StorageType.Double => new DoubleStorage(column), 
			StorageType.Decimal => new DecimalStorage(column), 
			StorageType.DateTime => new DateTimeStorage(column), 
			StorageType.TimeSpan => new TimeSpanStorage(column), 
			StorageType.String => new StringStorage(column), 
			StorageType.Guid => new ObjectStorage(column, dataType), 
			StorageType.ByteArray => new ObjectStorage(column, dataType), 
			StorageType.CharArray => new ObjectStorage(column, dataType), 
			StorageType.Type => new ObjectStorage(column, dataType), 
			StorageType.DateTimeOffset => new DateTimeOffsetStorage(column), 
			StorageType.BigInteger => new BigIntegerStorage(column), 
			StorageType.Uri => new ObjectStorage(column, dataType), 
			StorageType.SqlBinary => new SqlBinaryStorage(column), 
			StorageType.SqlBoolean => new SqlBooleanStorage(column), 
			StorageType.SqlByte => new SqlByteStorage(column), 
			StorageType.SqlBytes => new SqlBytesStorage(column), 
			StorageType.SqlChars => new SqlCharsStorage(column), 
			StorageType.SqlDateTime => new SqlDateTimeStorage(column), 
			StorageType.SqlDecimal => new SqlDecimalStorage(column), 
			StorageType.SqlDouble => new SqlDoubleStorage(column), 
			StorageType.SqlGuid => new SqlGuidStorage(column), 
			StorageType.SqlInt16 => new SqlInt16Storage(column), 
			StorageType.SqlInt32 => new SqlInt32Storage(column), 
			StorageType.SqlInt64 => new SqlInt64Storage(column), 
			StorageType.SqlMoney => new SqlMoneyStorage(column), 
			StorageType.SqlSingle => new SqlSingleStorage(column), 
			StorageType.SqlString => new SqlStringStorage(column), 
			_ => new ObjectStorage(column, dataType), 
		};
	}

	internal static StorageType GetStorageType(Type dataType)
	{
		for (int i = 0; i < s_storageClassType.Length; i++)
		{
			if (dataType == s_storageClassType[i])
			{
				return (StorageType)i;
			}
		}
		TypeCode typeCode = Type.GetTypeCode(dataType);
		if (TypeCode.Object != typeCode)
		{
			return (StorageType)typeCode;
		}
		return StorageType.Empty;
	}

	internal static Type GetTypeStorage(StorageType storageType)
	{
		return s_storageClassType[(int)storageType];
	}

	internal static bool IsTypeCustomType(Type type)
	{
		return IsTypeCustomType(GetStorageType(type));
	}

	internal static bool IsTypeCustomType(StorageType typeCode)
	{
		if (StorageType.Object != typeCode && typeCode != 0)
		{
			return StorageType.CharArray == typeCode;
		}
		return true;
	}

	internal static bool IsSqlType(StorageType storageType)
	{
		return StorageType.SqlBinary <= storageType;
	}

	public static bool IsSqlType(Type dataType)
	{
		for (int i = 26; i < s_storageClassType.Length; i++)
		{
			if (dataType == s_storageClassType[i])
			{
				return true;
			}
		}
		return false;
	}

	private static bool DetermineIfValueType(StorageType typeCode, Type dataType)
	{
		switch (typeCode)
		{
		case StorageType.Boolean:
		case StorageType.Char:
		case StorageType.SByte:
		case StorageType.Byte:
		case StorageType.Int16:
		case StorageType.UInt16:
		case StorageType.Int32:
		case StorageType.UInt32:
		case StorageType.Int64:
		case StorageType.UInt64:
		case StorageType.Single:
		case StorageType.Double:
		case StorageType.Decimal:
		case StorageType.DateTime:
		case StorageType.TimeSpan:
		case StorageType.Guid:
		case StorageType.DateTimeOffset:
		case StorageType.BigInteger:
		case StorageType.SqlBinary:
		case StorageType.SqlBoolean:
		case StorageType.SqlByte:
		case StorageType.SqlDateTime:
		case StorageType.SqlDecimal:
		case StorageType.SqlDouble:
		case StorageType.SqlGuid:
		case StorageType.SqlInt16:
		case StorageType.SqlInt32:
		case StorageType.SqlInt64:
		case StorageType.SqlMoney:
		case StorageType.SqlSingle:
		case StorageType.SqlString:
			return true;
		case StorageType.String:
		case StorageType.ByteArray:
		case StorageType.CharArray:
		case StorageType.Type:
		case StorageType.Uri:
		case StorageType.SqlBytes:
		case StorageType.SqlChars:
			return false;
		default:
			return dataType.IsValueType;
		}
	}

	internal static void ImplementsInterfaces(StorageType typeCode, Type dataType, out bool sqlType, out bool nullable, out bool xmlSerializable, out bool changeTracking, out bool revertibleChangeTracking)
	{
		if (IsSqlType(typeCode))
		{
			sqlType = true;
			nullable = true;
			changeTracking = false;
			revertibleChangeTracking = false;
			xmlSerializable = true;
		}
		else if (typeCode != 0)
		{
			sqlType = false;
			nullable = false;
			changeTracking = false;
			revertibleChangeTracking = false;
			xmlSerializable = false;
		}
		else
		{
			Tuple<bool, bool, bool, bool> orAdd = s_typeImplementsInterface.GetOrAdd(dataType, s_inspectTypeForInterfaces);
			sqlType = false;
			nullable = orAdd.Item1;
			changeTracking = orAdd.Item2;
			revertibleChangeTracking = orAdd.Item3;
			xmlSerializable = orAdd.Item4;
		}
	}

	private static Tuple<bool, bool, bool, bool> InspectTypeForInterfaces(Type dataType)
	{
		return new Tuple<bool, bool, bool, bool>(typeof(INullable).IsAssignableFrom(dataType), typeof(IChangeTracking).IsAssignableFrom(dataType), typeof(IRevertibleChangeTracking).IsAssignableFrom(dataType), typeof(IXmlSerializable).IsAssignableFrom(dataType));
	}

	internal static bool ImplementsINullableValue(StorageType typeCode, Type dataType)
	{
		if (typeCode == StorageType.Empty && dataType.IsGenericType)
		{
			return dataType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
		return false;
	}

	public static bool IsObjectNull(object value)
	{
		if (value != null && DBNull.Value != value)
		{
			return IsObjectSqlNull(value);
		}
		return true;
	}

	public static bool IsObjectSqlNull(object value)
	{
		if (value is INullable nullable)
		{
			return nullable.IsNull;
		}
		return false;
	}

	internal object GetEmptyStorageInternal(int recordCount)
	{
		return GetEmptyStorage(recordCount);
	}

	internal void CopyValueInternal(int record, object store, BitArray nullbits, int storeIndex)
	{
		CopyValue(record, store, nullbits, storeIndex);
	}

	internal void SetStorageInternal(object store, BitArray nullbits)
	{
		SetStorage(store, nullbits);
	}

	protected abstract object GetEmptyStorage(int recordCount);

	protected abstract void CopyValue(int record, object store, BitArray nullbits, int storeIndex);

	protected abstract void SetStorage(object store, BitArray nullbits);

	protected void SetNullStorage(BitArray nullbits)
	{
		_dbNullBits = nullbits;
	}

	[RequiresUnreferencedCode("Calls Type.GetType")]
	internal static Type GetType(string value)
	{
		Type type = Type.GetType(value);
		if (null == type && "System.Numerics.BigInteger" == value)
		{
			type = typeof(BigInteger);
		}
		ObjectStorage.VerifyIDynamicMetaObjectProvider(type);
		return type;
	}

	internal static string GetQualifiedName(Type type)
	{
		ObjectStorage.VerifyIDynamicMetaObjectProvider(type);
		return type.AssemblyQualifiedName;
	}
}
