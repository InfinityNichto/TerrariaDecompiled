using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonMetadataServices
{
	private static JsonConverter<bool> s_booleanConverter;

	private static JsonConverter<byte[]> s_byteArrayConverter;

	private static JsonConverter<byte> s_byteConverter;

	private static JsonConverter<char> s_charConverter;

	private static JsonConverter<DateTime> s_dateTimeConverter;

	private static JsonConverter<DateTimeOffset> s_dateTimeOffsetConverter;

	private static JsonConverter<decimal> s_decimalConverter;

	private static JsonConverter<double> s_doubleConverter;

	private static JsonConverter<Guid> s_guidConverter;

	private static JsonConverter<short> s_int16Converter;

	private static JsonConverter<int> s_int32Converter;

	private static JsonConverter<long> s_int64Converter;

	private static JsonConverter<JsonArray> s_jsonArrayConverter;

	private static JsonConverter<JsonElement> s_jsonElementConverter;

	private static JsonConverter<JsonNode> s_jsonNodeConverter;

	private static JsonConverter<JsonObject> s_jsonObjectConverter;

	private static JsonConverter<JsonValue> s_jsonValueConverter;

	private static JsonConverter<object> s_objectConverter;

	private static JsonConverter<float> s_singleConverter;

	private static JsonConverter<sbyte> s_sbyteConverter;

	private static JsonConverter<string> s_stringConverter;

	private static JsonConverter<TimeSpan> s_timeSpanConverter;

	private static JsonConverter<ushort> s_uint16Converter;

	private static JsonConverter<uint> s_uint32Converter;

	private static JsonConverter<ulong> s_uint64Converter;

	private static JsonConverter<Uri> s_uriConverter;

	private static JsonConverter<Version> s_versionConverter;

	public static JsonConverter<bool> BooleanConverter => s_booleanConverter ?? (s_booleanConverter = new BooleanConverter());

	public static JsonConverter<byte[]> ByteArrayConverter => s_byteArrayConverter ?? (s_byteArrayConverter = new ByteArrayConverter());

	public static JsonConverter<byte> ByteConverter => s_byteConverter ?? (s_byteConverter = new ByteConverter());

	public static JsonConverter<char> CharConverter => s_charConverter ?? (s_charConverter = new CharConverter());

	public static JsonConverter<DateTime> DateTimeConverter => s_dateTimeConverter ?? (s_dateTimeConverter = new DateTimeConverter());

	public static JsonConverter<DateTimeOffset> DateTimeOffsetConverter => s_dateTimeOffsetConverter ?? (s_dateTimeOffsetConverter = new DateTimeOffsetConverter());

	public static JsonConverter<decimal> DecimalConverter => s_decimalConverter ?? (s_decimalConverter = new DecimalConverter());

	public static JsonConverter<double> DoubleConverter => s_doubleConverter ?? (s_doubleConverter = new DoubleConverter());

	public static JsonConverter<Guid> GuidConverter => s_guidConverter ?? (s_guidConverter = new GuidConverter());

	public static JsonConverter<short> Int16Converter => s_int16Converter ?? (s_int16Converter = new Int16Converter());

	public static JsonConverter<int> Int32Converter => s_int32Converter ?? (s_int32Converter = new Int32Converter());

	public static JsonConverter<long> Int64Converter => s_int64Converter ?? (s_int64Converter = new Int64Converter());

	public static JsonConverter<JsonArray> JsonArrayConverter => s_jsonArrayConverter ?? (s_jsonArrayConverter = new JsonArrayConverter());

	public static JsonConverter<JsonElement> JsonElementConverter => s_jsonElementConverter ?? (s_jsonElementConverter = new JsonElementConverter());

	public static JsonConverter<JsonNode> JsonNodeConverter => s_jsonNodeConverter ?? (s_jsonNodeConverter = new JsonNodeConverter());

	public static JsonConverter<JsonObject> JsonObjectConverter => s_jsonObjectConverter ?? (s_jsonObjectConverter = new JsonObjectConverter());

	public static JsonConverter<JsonValue> JsonValueConverter => s_jsonValueConverter ?? (s_jsonValueConverter = new JsonValueConverter());

	public static JsonConverter<object?> ObjectConverter => s_objectConverter ?? (s_objectConverter = new ObjectConverter());

	public static JsonConverter<float> SingleConverter => s_singleConverter ?? (s_singleConverter = new SingleConverter());

	[CLSCompliant(false)]
	public static JsonConverter<sbyte> SByteConverter => s_sbyteConverter ?? (s_sbyteConverter = new SByteConverter());

	public static JsonConverter<string> StringConverter => s_stringConverter ?? (s_stringConverter = new StringConverter());

	public static JsonConverter<TimeSpan> TimeSpanConverter => s_timeSpanConverter ?? (s_timeSpanConverter = new TimeSpanConverter());

	[CLSCompliant(false)]
	public static JsonConverter<ushort> UInt16Converter => s_uint16Converter ?? (s_uint16Converter = new UInt16Converter());

	[CLSCompliant(false)]
	public static JsonConverter<uint> UInt32Converter => s_uint32Converter ?? (s_uint32Converter = new UInt32Converter());

	[CLSCompliant(false)]
	public static JsonConverter<ulong> UInt64Converter => s_uint64Converter ?? (s_uint64Converter = new UInt64Converter());

	public static JsonConverter<Uri> UriConverter => s_uriConverter ?? (s_uriConverter = new UriConverter());

	public static JsonConverter<Version> VersionConverter => s_versionConverter ?? (s_versionConverter = new VersionConverter());

	public static JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TElement[]> collectionInfo)
	{
		return new JsonTypeInfoInternal<TElement[]>(options, collectionInfo, () => new ArrayConverter<TElement[], TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : List<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ListOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Dictionary<TKey, TValue> where TKey : notnull
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateImmutableDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> createRangeFunc) where TCollection : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ImmutableDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(), createRangeFunc ?? throw new ArgumentNullException("createRangeFunc"));
	}

	public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IDictionary<TKey, TValue> where TKey : notnull
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateIReadOnlyDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IReadOnlyDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateImmutableEnumerableInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Func<IEnumerable<TElement>, TCollection> createRangeFunc) where TCollection : IEnumerable<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ImmutableEnumerableOfTConverter<TCollection, TElement>(), createRangeFunc ?? throw new ArgumentNullException("createRangeFunc"));
	}

	public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IList
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IListConverter<TCollection>());
	}

	public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IList<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IListOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateISetInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ISet<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ISetOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateICollectionInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ICollection<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ICollectionOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Stack<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new StackOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Queue<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new QueueOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateConcurrentStackInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ConcurrentStack<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ConcurrentStackOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateConcurrentQueueInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ConcurrentQueue<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new ConcurrentQueueOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IEnumerable<TElement>
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IEnumerableOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IDictionary
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IDictionaryConverter<TCollection>());
	}

	public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object?> addFunc) where TCollection : IEnumerable
	{
		return CreateStackOrQueueInfo(options, collectionInfo, addFunc);
	}

	public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object?> addFunc) where TCollection : IEnumerable
	{
		return CreateStackOrQueueInfo(options, collectionInfo, addFunc);
	}

	private static JsonTypeInfo<TCollection> CreateStackOrQueueInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object> addFunc) where TCollection : IEnumerable
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new StackOrQueueConverter<TCollection>(), null, addFunc ?? throw new ArgumentNullException("addFunc"));
	}

	public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IEnumerable
	{
		return new JsonTypeInfoInternal<TCollection>(options, collectionInfo, () => new IEnumerableConverter<TCollection>());
	}

	public static JsonConverter<T> GetUnsupportedTypeConverter<T>()
	{
		return new UnsupportedTypeConverter<T>();
	}

	public static JsonConverter<T> GetEnumConverter<T>(JsonSerializerOptions options) where T : struct, Enum
	{
		return new EnumConverter<T>(EnumConverterOptions.AllowNumbers, options ?? throw new ArgumentNullException("options"));
	}

	public static JsonConverter<T?> GetNullableConverter<T>(JsonTypeInfo<T> underlyingTypeInfo) where T : struct
	{
		if (underlyingTypeInfo == null)
		{
			throw new ArgumentNullException("underlyingTypeInfo");
		}
		JsonConverter<T> jsonConverter = underlyingTypeInfo.PropertyInfoForTypeInfo?.ConverterBase as JsonConverter<T>;
		if (jsonConverter == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterNotCompatible, jsonConverter, typeof(T)));
		}
		return new NullableConverter<T>(jsonConverter);
	}

	public static JsonPropertyInfo CreatePropertyInfo<T>(JsonSerializerOptions options, JsonPropertyInfoValues<T> propertyInfo)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if (propertyInfo == null)
		{
			throw new ArgumentNullException("propertyInfo");
		}
		Type declaringType = propertyInfo.DeclaringType;
		if (declaringType == null)
		{
			throw new ArgumentException("DeclaringType");
		}
		JsonTypeInfo propertyTypeInfo = propertyInfo.PropertyTypeInfo;
		if (propertyTypeInfo == null)
		{
			throw new ArgumentException("PropertyTypeInfo");
		}
		string propertyName = propertyInfo.PropertyName;
		if (propertyName == null)
		{
			throw new ArgumentException("PropertyName");
		}
		JsonConverter converter = propertyInfo.Converter;
		if (converter == null)
		{
			converter = propertyTypeInfo.PropertyInfoForTypeInfo.ConverterBase as JsonConverter<T>;
			if (converter == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ConverterForPropertyMustBeValid, declaringType, propertyName, typeof(T)));
			}
		}
		if (!propertyInfo.IsProperty && propertyInfo.IsVirtual)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.FieldCannotBeVirtual, "IsProperty", "IsVirtual"));
		}
		JsonPropertyInfo<T> jsonPropertyInfo = new JsonPropertyInfo<T>();
		jsonPropertyInfo.InitializeForSourceGen(options, propertyInfo);
		return jsonPropertyInfo;
	}

	public static JsonTypeInfo<T> CreateObjectInfo<T>(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo) where T : notnull
	{
		return new JsonTypeInfoInternal<T>(options ?? throw new ArgumentNullException("options"), objectInfo ?? throw new ArgumentNullException("objectInfo"));
	}

	public static JsonTypeInfo<T> CreateValueInfo<T>(JsonSerializerOptions options, JsonConverter converter)
	{
		JsonTypeInfo<T> jsonTypeInfo = new JsonTypeInfoInternal<T>(options);
		jsonTypeInfo.PropertyInfoForTypeInfo = CreateJsonPropertyInfoForClassInfo(typeof(T), jsonTypeInfo, converter, options);
		return jsonTypeInfo;
	}

	internal static JsonPropertyInfo CreateJsonPropertyInfoForClassInfo(Type type, JsonTypeInfo typeInfo, JsonConverter converter, JsonSerializerOptions options)
	{
		JsonPropertyInfo jsonPropertyInfo = converter.CreateJsonPropertyInfo();
		jsonPropertyInfo.InitializeForTypeInfo(type, typeInfo, converter, options);
		return jsonPropertyInfo;
	}
}
