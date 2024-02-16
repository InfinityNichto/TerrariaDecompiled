using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

public sealed class JsonSerializerOptions
{
	internal static class TrackedOptionsInstances
	{
		public static ConditionalWeakTable<JsonSerializerOptions, object> All { get; } = new ConditionalWeakTable<JsonSerializerOptions, object>();

	}

	private static Dictionary<Type, JsonConverter> s_defaultSimpleConverters;

	private static JsonConverter[] s_defaultFactoryConverters;

	private readonly ConcurrentDictionary<Type, JsonConverter> _converters = new ConcurrentDictionary<Type, JsonConverter>();

	internal static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

	private readonly ConcurrentDictionary<Type, JsonTypeInfo> _classes = new ConcurrentDictionary<Type, JsonTypeInfo>();

	internal JsonSerializerContext _context;

	private Func<Type, JsonSerializerOptions, JsonTypeInfo> _typeInfoCreationFunc;

	private MemberAccessor _memberAccessorStrategy;

	private JsonNamingPolicy _dictionaryKeyPolicy;

	private JsonNamingPolicy _jsonPropertyNamingPolicy;

	private JsonCommentHandling _readCommentHandling;

	private ReferenceHandler _referenceHandler;

	private JavaScriptEncoder _encoder;

	private JsonIgnoreCondition _defaultIgnoreCondition;

	private JsonNumberHandling _numberHandling;

	private JsonUnknownTypeHandling _unknownTypeHandling;

	private int _defaultBufferSize = 16384;

	private int _maxDepth;

	private bool _allowTrailingCommas;

	private bool _haveTypesBeenCreated;

	private bool _ignoreNullValues;

	private bool _ignoreReadOnlyProperties;

	private bool _ignoreReadonlyFields;

	private bool _includeFields;

	private bool _propertyNameCaseInsensitive;

	private bool _writeIndented;

	internal ReferenceHandlingStrategy ReferenceHandlingStrategy;

	public IList<JsonConverter> Converters { get; }

	private JsonTypeInfo? _lastClass { get; set; }

	public bool AllowTrailingCommas
	{
		get
		{
			return _allowTrailingCommas;
		}
		set
		{
			VerifyMutable();
			_allowTrailingCommas = value;
		}
	}

	public int DefaultBufferSize
	{
		get
		{
			return _defaultBufferSize;
		}
		set
		{
			VerifyMutable();
			if (value < 1)
			{
				throw new ArgumentException(System.SR.SerializationInvalidBufferSize);
			}
			_defaultBufferSize = value;
		}
	}

	public JavaScriptEncoder? Encoder
	{
		get
		{
			return _encoder;
		}
		set
		{
			VerifyMutable();
			_encoder = value;
		}
	}

	public JsonNamingPolicy? DictionaryKeyPolicy
	{
		get
		{
			return _dictionaryKeyPolicy;
		}
		set
		{
			VerifyMutable();
			_dictionaryKeyPolicy = value;
		}
	}

	[Obsolete("JsonSerializerOptions.IgnoreNullValues is obsolete. To ignore null values when serializing, set DefaultIgnoreCondition to JsonIgnoreCondition.WhenWritingNull.", DiagnosticId = "SYSLIB0020", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool IgnoreNullValues
	{
		get
		{
			return _ignoreNullValues;
		}
		set
		{
			VerifyMutable();
			if (value && _defaultIgnoreCondition != 0)
			{
				throw new InvalidOperationException(System.SR.DefaultIgnoreConditionAlreadySpecified);
			}
			_ignoreNullValues = value;
		}
	}

	public JsonIgnoreCondition DefaultIgnoreCondition
	{
		get
		{
			return _defaultIgnoreCondition;
		}
		set
		{
			VerifyMutable();
			switch (value)
			{
			case JsonIgnoreCondition.Always:
				throw new ArgumentException(System.SR.DefaultIgnoreConditionInvalid);
			default:
				if (_ignoreNullValues)
				{
					throw new InvalidOperationException(System.SR.DefaultIgnoreConditionAlreadySpecified);
				}
				break;
			case JsonIgnoreCondition.Never:
				break;
			}
			_defaultIgnoreCondition = value;
		}
	}

	public JsonNumberHandling NumberHandling
	{
		get
		{
			return _numberHandling;
		}
		set
		{
			VerifyMutable();
			if (!JsonSerializer.IsValidNumberHandlingValue(value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_numberHandling = value;
		}
	}

	public bool IgnoreReadOnlyProperties
	{
		get
		{
			return _ignoreReadOnlyProperties;
		}
		set
		{
			VerifyMutable();
			_ignoreReadOnlyProperties = value;
		}
	}

	public bool IgnoreReadOnlyFields
	{
		get
		{
			return _ignoreReadonlyFields;
		}
		set
		{
			VerifyMutable();
			_ignoreReadonlyFields = value;
		}
	}

	public bool IncludeFields
	{
		get
		{
			return _includeFields;
		}
		set
		{
			VerifyMutable();
			_includeFields = value;
		}
	}

	public int MaxDepth
	{
		get
		{
			return _maxDepth;
		}
		set
		{
			VerifyMutable();
			if (value < 0)
			{
				throw ThrowHelper.GetArgumentOutOfRangeException_MaxDepthMustBePositive("value");
			}
			_maxDepth = value;
			EffectiveMaxDepth = ((value == 0) ? 64 : value);
		}
	}

	internal int EffectiveMaxDepth { get; private set; } = 64;


	public JsonNamingPolicy? PropertyNamingPolicy
	{
		get
		{
			return _jsonPropertyNamingPolicy;
		}
		set
		{
			VerifyMutable();
			_jsonPropertyNamingPolicy = value;
		}
	}

	public bool PropertyNameCaseInsensitive
	{
		get
		{
			return _propertyNameCaseInsensitive;
		}
		set
		{
			VerifyMutable();
			_propertyNameCaseInsensitive = value;
		}
	}

	public JsonCommentHandling ReadCommentHandling
	{
		get
		{
			return _readCommentHandling;
		}
		set
		{
			VerifyMutable();
			if ((int)value > 1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.JsonSerializerDoesNotSupportComments);
			}
			_readCommentHandling = value;
		}
	}

	public JsonUnknownTypeHandling UnknownTypeHandling
	{
		get
		{
			return _unknownTypeHandling;
		}
		set
		{
			VerifyMutable();
			_unknownTypeHandling = value;
		}
	}

	public bool WriteIndented
	{
		get
		{
			return _writeIndented;
		}
		set
		{
			VerifyMutable();
			_writeIndented = value;
		}
	}

	public ReferenceHandler? ReferenceHandler
	{
		get
		{
			return _referenceHandler;
		}
		set
		{
			VerifyMutable();
			_referenceHandler = value;
			ReferenceHandlingStrategy = value?.HandlingStrategy ?? ReferenceHandlingStrategy.None;
		}
	}

	internal MemberAccessor MemberAccessorStrategy
	{
		get
		{
			if (_memberAccessorStrategy == null)
			{
				_memberAccessorStrategy = (RuntimeFeature.IsDynamicCodeSupported ? ((MemberAccessor)new ReflectionEmitMemberAccessor()) : ((MemberAccessor)new ReflectionMemberAccessor()));
			}
			return _memberAccessorStrategy;
		}
	}

	internal bool IsInitializedForReflectionSerializer { get; set; }

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private void RootBuiltInConverters()
	{
		s_defaultSimpleConverters = GetDefaultSimpleConverters();
		s_defaultFactoryConverters = new JsonConverter[8]
		{
			new UnsupportedTypeConverterFactory(),
			new NullableConverterFactory(),
			new EnumConverterFactory(),
			new JsonNodeConverterFactory(),
			new FSharpTypeConverterFactory(),
			new IAsyncEnumerableConverterFactory(),
			new IEnumerableConverterFactory(),
			new ObjectConverterFactory()
		};
	}

	private static Dictionary<Type, JsonConverter> GetDefaultSimpleConverters()
	{
		Dictionary<Type, JsonConverter> converters = new Dictionary<Type, JsonConverter>(24);
		Add(JsonMetadataServices.BooleanConverter);
		Add(JsonMetadataServices.ByteConverter);
		Add(JsonMetadataServices.ByteArrayConverter);
		Add(JsonMetadataServices.CharConverter);
		Add(JsonMetadataServices.DateTimeConverter);
		Add(JsonMetadataServices.DateTimeOffsetConverter);
		Add(JsonMetadataServices.DoubleConverter);
		Add(JsonMetadataServices.DecimalConverter);
		Add(JsonMetadataServices.GuidConverter);
		Add(JsonMetadataServices.Int16Converter);
		Add(JsonMetadataServices.Int32Converter);
		Add(JsonMetadataServices.Int64Converter);
		Add(new JsonElementConverter());
		Add(new JsonDocumentConverter());
		Add(JsonMetadataServices.ObjectConverter);
		Add(JsonMetadataServices.SByteConverter);
		Add(JsonMetadataServices.SingleConverter);
		Add(JsonMetadataServices.StringConverter);
		Add(JsonMetadataServices.TimeSpanConverter);
		Add(JsonMetadataServices.UInt16Converter);
		Add(JsonMetadataServices.UInt32Converter);
		Add(JsonMetadataServices.UInt64Converter);
		Add(JsonMetadataServices.UriConverter);
		Add(JsonMetadataServices.VersionConverter);
		return converters;
		void Add(JsonConverter converter)
		{
			converters.Add(converter.TypeToConvert, converter);
		}
	}

	internal JsonConverter DetermineConverter(Type parentClassType, Type runtimePropertyType, MemberInfo memberInfo)
	{
		JsonConverter jsonConverter = null;
		if (memberInfo != null)
		{
			JsonConverterAttribute jsonConverterAttribute = (JsonConverterAttribute)GetAttributeThatCanHaveMultiple(parentClassType, typeof(JsonConverterAttribute), memberInfo);
			if (jsonConverterAttribute != null)
			{
				jsonConverter = GetConverterFromAttribute(jsonConverterAttribute, runtimePropertyType, parentClassType, memberInfo);
			}
		}
		if (jsonConverter == null)
		{
			jsonConverter = GetConverterInternal(runtimePropertyType);
		}
		if (jsonConverter is JsonConverterFactory jsonConverterFactory)
		{
			jsonConverter = jsonConverterFactory.GetConverterInternal(runtimePropertyType, this);
		}
		if (runtimePropertyType.IsValueType && jsonConverter.IsValueType && (runtimePropertyType.IsNullableOfT() ^ jsonConverter.TypeToConvert.IsNullableOfT()))
		{
			ThrowHelper.ThrowInvalidOperationException_ConverterCanConvertMultipleTypes(runtimePropertyType, jsonConverter);
		}
		return jsonConverter;
	}

	[RequiresUnreferencedCode("Getting a converter for a type may require reflection which depends on unreferenced code.")]
	public JsonConverter GetConverter(Type typeToConvert)
	{
		if (typeToConvert == null)
		{
			throw new ArgumentNullException("typeToConvert");
		}
		RootBuiltInConverters();
		return GetConverterInternal(typeToConvert);
	}

	internal JsonConverter GetConverterInternal(Type typeToConvert)
	{
		if (_converters.TryGetValue(typeToConvert, out var value))
		{
			return value;
		}
		value = _context?.GetTypeInfo(typeToConvert)?.PropertyInfoForTypeInfo?.ConverterBase;
		foreach (JsonConverter converter in Converters)
		{
			if (converter.CanConvert(typeToConvert))
			{
				value = converter;
				break;
			}
		}
		if (value == null)
		{
			JsonConverterAttribute jsonConverterAttribute = (JsonConverterAttribute)GetAttributeThatCanHaveMultiple(typeToConvert, typeof(JsonConverterAttribute));
			if (jsonConverterAttribute != null)
			{
				value = GetConverterFromAttribute(jsonConverterAttribute, typeToConvert, typeToConvert, null);
			}
		}
		if (value == null)
		{
			if (s_defaultSimpleConverters == null || s_defaultFactoryConverters == null)
			{
				ThrowHelper.ThrowNotSupportedException_BuiltInConvertersNotRooted(typeToConvert);
				return null;
			}
			if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out var value2))
			{
				value = value2;
			}
			else
			{
				JsonConverter[] array = s_defaultFactoryConverters;
				foreach (JsonConverter jsonConverter in array)
				{
					if (jsonConverter.CanConvert(typeToConvert))
					{
						value = jsonConverter;
						break;
					}
				}
			}
		}
		if (value is JsonConverterFactory jsonConverterFactory)
		{
			value = jsonConverterFactory.GetConverterInternal(typeToConvert, this);
		}
		Type typeToConvert2 = value.TypeToConvert;
		if (!typeToConvert2.IsAssignableFromInternal(typeToConvert) && !typeToConvert.IsAssignableFromInternal(typeToConvert2))
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationConverterNotCompatible(value.GetType(), typeToConvert);
		}
		if (_haveTypesBeenCreated)
		{
			_converters.TryAdd(typeToConvert, value);
		}
		return value;
	}

	private JsonConverter GetConverterFromAttribute(JsonConverterAttribute converterAttribute, Type typeToConvert, Type classTypeAttributeIsOn, MemberInfo memberInfo)
	{
		Type converterType = converterAttribute.ConverterType;
		JsonConverter jsonConverter;
		if (converterType == null)
		{
			jsonConverter = converterAttribute.CreateConverter(typeToConvert);
			if (jsonConverter == null)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
			}
		}
		else
		{
			ConstructorInfo constructor = converterType.GetConstructor(Type.EmptyTypes);
			if (!typeof(JsonConverter).IsAssignableFrom(converterType) || constructor == null || !constructor.IsPublic)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(classTypeAttributeIsOn, memberInfo);
			}
			jsonConverter = (JsonConverter)Activator.CreateInstance(converterType);
		}
		if (!jsonConverter.CanConvert(typeToConvert))
		{
			Type underlyingType = Nullable.GetUnderlyingType(typeToConvert);
			if (underlyingType != null && jsonConverter.CanConvert(underlyingType))
			{
				if (jsonConverter is JsonConverterFactory jsonConverterFactory)
				{
					jsonConverter = jsonConverterFactory.GetConverterInternal(underlyingType, this);
				}
				return NullableConverterFactory.CreateValueConverter(underlyingType, jsonConverter);
			}
			ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
		}
		return jsonConverter;
	}

	internal bool TryGetDefaultSimpleConverter(Type typeToConvert, [NotNullWhen(true)] out JsonConverter converter)
	{
		if (_context == null && s_defaultSimpleConverters != null && s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter))
		{
			return true;
		}
		converter = null;
		return false;
	}

	private static Attribute GetAttributeThatCanHaveMultiple(Type classType, Type attributeType, MemberInfo memberInfo)
	{
		object[] customAttributes = memberInfo.GetCustomAttributes(attributeType, inherit: false);
		return GetAttributeThatCanHaveMultiple(attributeType, classType, memberInfo, customAttributes);
	}

	internal static Attribute GetAttributeThatCanHaveMultiple(Type classType, Type attributeType)
	{
		object[] customAttributes = classType.GetCustomAttributes(attributeType, inherit: false);
		return GetAttributeThatCanHaveMultiple(attributeType, classType, null, customAttributes);
	}

	private static Attribute GetAttributeThatCanHaveMultiple(Type attributeType, Type classType, MemberInfo memberInfo, object[] attributes)
	{
		if (attributes.Length == 0)
		{
			return null;
		}
		if (attributes.Length == 1)
		{
			return (Attribute)attributes[0];
		}
		ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateAttribute(attributeType, classType, memberInfo);
		return null;
	}

	public JsonSerializerOptions()
	{
		Converters = new ConverterList(this);
		TrackOptionsInstance(this);
	}

	public JsonSerializerOptions(JsonSerializerOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		_memberAccessorStrategy = options._memberAccessorStrategy;
		_dictionaryKeyPolicy = options._dictionaryKeyPolicy;
		_jsonPropertyNamingPolicy = options._jsonPropertyNamingPolicy;
		_readCommentHandling = options._readCommentHandling;
		_referenceHandler = options._referenceHandler;
		_encoder = options._encoder;
		_defaultIgnoreCondition = options._defaultIgnoreCondition;
		_numberHandling = options._numberHandling;
		_unknownTypeHandling = options._unknownTypeHandling;
		_defaultBufferSize = options._defaultBufferSize;
		_maxDepth = options._maxDepth;
		_allowTrailingCommas = options._allowTrailingCommas;
		_ignoreNullValues = options._ignoreNullValues;
		_ignoreReadOnlyProperties = options._ignoreReadOnlyProperties;
		_ignoreReadonlyFields = options._ignoreReadonlyFields;
		_includeFields = options._includeFields;
		_propertyNameCaseInsensitive = options._propertyNameCaseInsensitive;
		_writeIndented = options._writeIndented;
		Converters = new ConverterList(this, (ConverterList)options.Converters);
		EffectiveMaxDepth = options.EffectiveMaxDepth;
		ReferenceHandlingStrategy = options.ReferenceHandlingStrategy;
		TrackOptionsInstance(this);
	}

	private static void TrackOptionsInstance(JsonSerializerOptions options)
	{
		TrackedOptionsInstances.All.Add(options, null);
	}

	public JsonSerializerOptions(JsonSerializerDefaults defaults)
		: this()
	{
		switch (defaults)
		{
		case JsonSerializerDefaults.Web:
			_propertyNameCaseInsensitive = true;
			_jsonPropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			_numberHandling = JsonNumberHandling.AllowReadingFromString;
			break;
		default:
			throw new ArgumentOutOfRangeException("defaults");
		case JsonSerializerDefaults.General:
			break;
		}
	}

	public void AddContext<TContext>() where TContext : JsonSerializerContext, new()
	{
		if (_context != null)
		{
			ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext();
		}
		(_context = new TContext())._options = this;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	internal void InitializeForReflectionSerializer()
	{
		RootBuiltInConverters();
		_typeInfoCreationFunc = CreateJsonTypeInfo;
		IsInitializedForReflectionSerializer = true;
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
		static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
		{
			return new JsonTypeInfo(type, options);
		}
	}

	internal JsonTypeInfo GetOrAddClass(Type type)
	{
		_haveTypesBeenCreated = true;
		if (!TryGetClass(type, out var jsonTypeInfo))
		{
			return _classes.GetOrAdd(type, GetClassFromContextOrCreate(type));
		}
		return jsonTypeInfo;
	}

	internal JsonTypeInfo GetClassFromContextOrCreate(Type type)
	{
		JsonTypeInfo jsonTypeInfo = _context?.GetTypeInfo(type);
		if (jsonTypeInfo != null)
		{
			return jsonTypeInfo;
		}
		if (_typeInfoCreationFunc == null)
		{
			ThrowHelper.ThrowNotSupportedException_NoMetadataForType(type);
			return null;
		}
		return _typeInfoCreationFunc(type, this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal JsonTypeInfo GetOrAddClassForRootType(Type type)
	{
		JsonTypeInfo jsonTypeInfo = _lastClass;
		if (jsonTypeInfo?.Type != type)
		{
			jsonTypeInfo = (_lastClass = GetOrAddClass(type));
		}
		return jsonTypeInfo;
	}

	internal bool TryGetClass(Type type, [NotNullWhen(true)] out JsonTypeInfo jsonTypeInfo)
	{
		if (!_classes.TryGetValue(type, out var value))
		{
			jsonTypeInfo = null;
			return false;
		}
		jsonTypeInfo = value;
		return true;
	}

	internal bool TypeIsCached(Type type)
	{
		return _classes.ContainsKey(type);
	}

	internal void ClearClasses()
	{
		_classes.Clear();
		_lastClass = null;
	}

	internal JsonDocumentOptions GetDocumentOptions()
	{
		JsonDocumentOptions result = default(JsonDocumentOptions);
		result.AllowTrailingCommas = AllowTrailingCommas;
		result.CommentHandling = ReadCommentHandling;
		result.MaxDepth = MaxDepth;
		return result;
	}

	internal JsonNodeOptions GetNodeOptions()
	{
		JsonNodeOptions result = default(JsonNodeOptions);
		result.PropertyNameCaseInsensitive = PropertyNameCaseInsensitive;
		return result;
	}

	internal JsonReaderOptions GetReaderOptions()
	{
		JsonReaderOptions result = default(JsonReaderOptions);
		result.AllowTrailingCommas = AllowTrailingCommas;
		result.CommentHandling = ReadCommentHandling;
		result.MaxDepth = MaxDepth;
		return result;
	}

	internal JsonWriterOptions GetWriterOptions()
	{
		JsonWriterOptions result = default(JsonWriterOptions);
		result.Encoder = Encoder;
		result.Indented = WriteIndented;
		result.SkipValidation = true;
		return result;
	}

	internal void VerifyMutable()
	{
		if (_haveTypesBeenCreated || _context != null)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerOptionsImmutable(_context);
		}
	}
}
