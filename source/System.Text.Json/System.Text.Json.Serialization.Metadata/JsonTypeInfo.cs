using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JsonTypeInfo<T> : JsonTypeInfo
{
	private Action<Utf8JsonWriter, T> _serialize;

	public Action<Utf8JsonWriter, T>? SerializeHandler
	{
		get
		{
			return _serialize;
		}
		private protected set
		{
			_serialize = value;
			base.HasSerialize = value != null;
		}
	}

	internal JsonTypeInfo(Type type, JsonSerializerOptions options)
		: base(type, options, dummy: false)
	{
	}
}
[DebuggerDisplay("ConverterStrategy.{ConverterStrategy}, {Type.Name}")]
public class JsonTypeInfo
{
	internal delegate object ConstructorDelegate();

	internal delegate T ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>(TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

	private sealed class ParameterLookupKey
	{
		public string Name { get; }

		public Type Type { get; }

		public ParameterLookupKey(string name, Type type)
		{
			Name = name;
			Type = type;
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			ParameterLookupKey parameterLookupKey = (ParameterLookupKey)obj;
			if (Type == parameterLookupKey.Type)
			{
				return string.Equals(Name, parameterLookupKey.Name, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	private sealed class ParameterLookupValue
	{
		public string DuplicateName { get; set; }

		public JsonPropertyInfo JsonPropertyInfo { get; }

		public ParameterLookupValue(JsonPropertyInfo jsonPropertyInfo)
		{
			JsonPropertyInfo = jsonPropertyInfo;
		}
	}

	internal static readonly Type ObjectType = typeof(object);

	private const int PropertyNameKeyLength = 7;

	private const int ParameterNameCountCacheThreshold = 32;

	private const int PropertyNameCountCacheThreshold = 64;

	internal JsonPropertyDictionary<JsonParameterInfo> ParameterCache;

	internal JsonPropertyDictionary<JsonPropertyInfo> PropertyCache;

	private volatile ParameterRef[] _parameterRefsSorted;

	private volatile PropertyRef[] _propertyRefsSorted;

	internal Func<JsonSerializerContext, JsonPropertyInfo[]> PropInitFunc;

	internal Func<JsonParameterInfoValues[]> CtorParamInitFunc;

	internal const string JsonObjectTypeName = "System.Text.Json.Nodes.JsonObject";

	private JsonTypeInfo _elementTypeInfo;

	private JsonTypeInfo _keyTypeInfo;

	private GenericMethodHolder _genericMethods;

	internal int ParameterCount { get; private set; }

	internal ConstructorDelegate? CreateObject { get; set; }

	internal object? CreateObjectWithArgs { get; set; }

	internal object? AddMethodDelegate { get; set; }

	internal JsonPropertyInfo? DataExtensionProperty { get; private set; }

	internal bool HasSerialize { get; set; }

	internal JsonTypeInfo? ElementTypeInfo
	{
		get
		{
			if (_elementTypeInfo == null && ElementType != null)
			{
				_elementTypeInfo = Options.GetOrAddClass(ElementType);
			}
			return _elementTypeInfo;
		}
		set
		{
			_elementTypeInfo = value;
		}
	}

	internal Type? ElementType { get; set; }

	internal JsonTypeInfo? KeyTypeInfo
	{
		get
		{
			if (_keyTypeInfo == null && KeyType != null)
			{
				_keyTypeInfo = Options.GetOrAddClass(KeyType);
			}
			return _keyTypeInfo;
		}
		set
		{
			_keyTypeInfo = value;
		}
	}

	internal Type? KeyType { get; set; }

	internal JsonSerializerOptions Options { get; set; }

	internal Type Type { get; private set; }

	internal JsonPropertyInfo PropertyInfoForTypeInfo { get; set; }

	internal bool IsObjectWithParameterizedCtor => PropertyInfoForTypeInfo.ConverterBase.ConstructorIsParameterized;

	internal GenericMethodHolder GenericMethods
	{
		get
		{
			if (_genericMethods == null)
			{
				_genericMethods = GenericMethodHolder.CreateHolder(Type);
			}
			return _genericMethods;
		}
	}

	internal JsonNumberHandling? NumberHandling { get; set; }

	internal static JsonPropertyInfo AddProperty(MemberInfo memberInfo, Type memberType, Type parentClassType, bool isVirtual, JsonNumberHandling? parentTypeNumberHandling, JsonSerializerOptions options)
	{
		JsonIgnoreCondition? jsonIgnoreCondition = JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(memberInfo)?.Condition;
		if (jsonIgnoreCondition == JsonIgnoreCondition.Always)
		{
			return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(memberInfo, memberType, isVirtual, options);
		}
		Type runtimeType;
		JsonConverter converter = GetConverter(memberType, parentClassType, memberInfo, out runtimeType, options);
		return CreateProperty(memberType, runtimeType, memberInfo, parentClassType, isVirtual, converter, options, parentTypeNumberHandling, jsonIgnoreCondition);
	}

	internal static JsonPropertyInfo CreateProperty(Type declaredPropertyType, Type runtimePropertyType, MemberInfo memberInfo, Type parentClassType, bool isVirtual, JsonConverter converter, JsonSerializerOptions options, JsonNumberHandling? parentTypeNumberHandling = null, JsonIgnoreCondition? ignoreCondition = null)
	{
		JsonPropertyInfo jsonPropertyInfo = converter.CreateJsonPropertyInfo();
		jsonPropertyInfo.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, converter.ConverterStrategy, memberInfo, isVirtual, converter, ignoreCondition, parentTypeNumberHandling, options);
		return jsonPropertyInfo;
	}

	internal static JsonPropertyInfo CreatePropertyInfoForTypeInfo(Type declaredPropertyType, Type runtimePropertyType, JsonConverter converter, JsonNumberHandling? numberHandling, JsonSerializerOptions options)
	{
		return CreateProperty(declaredPropertyType, runtimePropertyType, null, ObjectType, isVirtual: false, converter, options, numberHandling);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal JsonPropertyInfo GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
	{
		ulong key = GetKey(propertyName);
		PropertyRef[] propertyRefsSorted = _propertyRefsSorted;
		if (propertyRefsSorted != null)
		{
			int propertyIndex = frame.PropertyIndex;
			int num = propertyRefsSorted.Length;
			int num2 = Math.Min(propertyIndex, num);
			int num3 = num2 - 1;
			while (true)
			{
				if (num2 < num)
				{
					PropertyRef propertyRef = propertyRefsSorted[num2];
					if (IsPropertyRefEqual(in propertyRef, propertyName, key))
					{
						utf8PropertyName = propertyRef.NameFromJson;
						return propertyRef.Info;
					}
					num2++;
					if (num3 >= 0)
					{
						propertyRef = propertyRefsSorted[num3];
						if (IsPropertyRefEqual(in propertyRef, propertyName, key))
						{
							utf8PropertyName = propertyRef.NameFromJson;
							return propertyRef.Info;
						}
						num3--;
					}
				}
				else
				{
					if (num3 < 0)
					{
						break;
					}
					PropertyRef propertyRef = propertyRefsSorted[num3];
					if (IsPropertyRefEqual(in propertyRef, propertyName, key))
					{
						utf8PropertyName = propertyRef.NameFromJson;
						return propertyRef.Info;
					}
					num3--;
				}
			}
		}
		if (PropertyCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out var value))
		{
			if (Options.PropertyNameCaseInsensitive)
			{
				if (propertyName.SequenceEqual(value.NameAsUtf8Bytes))
				{
					utf8PropertyName = value.NameAsUtf8Bytes;
				}
				else
				{
					utf8PropertyName = propertyName.ToArray();
				}
			}
			else
			{
				utf8PropertyName = value.NameAsUtf8Bytes;
			}
		}
		else
		{
			value = JsonPropertyInfo.s_missingProperty;
			utf8PropertyName = propertyName.ToArray();
		}
		int num4 = 0;
		if (propertyRefsSorted != null)
		{
			num4 = propertyRefsSorted.Length;
		}
		if (num4 < 64)
		{
			if (frame.PropertyRefCache != null)
			{
				num4 += frame.PropertyRefCache.Count;
			}
			if (num4 < 64)
			{
				if (frame.PropertyRefCache == null)
				{
					frame.PropertyRefCache = new List<PropertyRef>();
				}
				PropertyRef propertyRef = new PropertyRef(key, value, utf8PropertyName);
				frame.PropertyRefCache.Add(propertyRef);
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal JsonParameterInfo GetParameter(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
	{
		ulong key = GetKey(propertyName);
		ParameterRef[] parameterRefsSorted = _parameterRefsSorted;
		if (parameterRefsSorted != null)
		{
			int parameterIndex = frame.CtorArgumentState.ParameterIndex;
			int num = parameterRefsSorted.Length;
			int num2 = Math.Min(parameterIndex, num);
			int num3 = num2 - 1;
			while (true)
			{
				if (num2 < num)
				{
					ParameterRef parameterRef = parameterRefsSorted[num2];
					if (IsParameterRefEqual(in parameterRef, propertyName, key))
					{
						utf8PropertyName = parameterRef.NameFromJson;
						return parameterRef.Info;
					}
					num2++;
					if (num3 >= 0)
					{
						parameterRef = parameterRefsSorted[num3];
						if (IsParameterRefEqual(in parameterRef, propertyName, key))
						{
							utf8PropertyName = parameterRef.NameFromJson;
							return parameterRef.Info;
						}
						num3--;
					}
				}
				else
				{
					if (num3 < 0)
					{
						break;
					}
					ParameterRef parameterRef = parameterRefsSorted[num3];
					if (IsParameterRefEqual(in parameterRef, propertyName, key))
					{
						utf8PropertyName = parameterRef.NameFromJson;
						return parameterRef.Info;
					}
					num3--;
				}
			}
		}
		if (ParameterCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out var value))
		{
			if (Options.PropertyNameCaseInsensitive)
			{
				if (propertyName.SequenceEqual(value.NameAsUtf8Bytes))
				{
					utf8PropertyName = value.NameAsUtf8Bytes;
				}
				else
				{
					utf8PropertyName = propertyName.ToArray();
				}
			}
			else
			{
				utf8PropertyName = value.NameAsUtf8Bytes;
			}
		}
		else
		{
			utf8PropertyName = propertyName.ToArray();
		}
		int num4 = 0;
		if (parameterRefsSorted != null)
		{
			num4 = parameterRefsSorted.Length;
		}
		if (num4 < 32)
		{
			if (frame.CtorArgumentState.ParameterRefCache != null)
			{
				num4 += frame.CtorArgumentState.ParameterRefCache.Count;
			}
			if (num4 < 32)
			{
				if (frame.CtorArgumentState.ParameterRefCache == null)
				{
					frame.CtorArgumentState.ParameterRefCache = new List<ParameterRef>();
				}
				ParameterRef parameterRef = new ParameterRef(key, value, utf8PropertyName);
				frame.CtorArgumentState.ParameterRefCache.Add(parameterRef);
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key)
	{
		if (key == propertyRef.Key && (propertyName.Length <= 7 || propertyName.SequenceEqual(propertyRef.NameFromJson)))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsParameterRefEqual(in ParameterRef parameterRef, ReadOnlySpan<byte> parameterName, ulong key)
	{
		if (key == parameterRef.Key && (parameterName.Length <= 7 || parameterName.SequenceEqual(parameterRef.NameFromJson)))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong GetKey(ReadOnlySpan<byte> name)
	{
		ref byte reference = ref MemoryMarshal.GetReference(name);
		int length = name.Length;
		ulong num;
		if (length > 7)
		{
			num = Unsafe.ReadUnaligned<ulong>(ref reference) & 0xFFFFFFFFFFFFFFuL;
			num |= (ulong)((long)Math.Min(length, 255) << 56);
		}
		else
		{
			num = ((length > 5) ? (Unsafe.ReadUnaligned<uint>(ref reference) | ((ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32)) : ((length > 3) ? ((ulong)Unsafe.ReadUnaligned<uint>(ref reference)) : ((ulong)((length > 1) ? Unsafe.ReadUnaligned<ushort>(ref reference) : 0))));
			num |= (ulong)((long)length << 56);
			if (((uint)length & (true ? 1u : 0u)) != 0)
			{
				int num2 = length - 1;
				num |= (ulong)Unsafe.Add(ref reference, num2) << num2 * 8;
			}
		}
		return num;
	}

	internal void UpdateSortedPropertyCache(ref ReadStackFrame frame)
	{
		List<PropertyRef> propertyRefCache = frame.PropertyRefCache;
		if (_propertyRefsSorted != null)
		{
			List<PropertyRef> list = new List<PropertyRef>(_propertyRefsSorted);
			while (list.Count + propertyRefCache.Count > 64)
			{
				propertyRefCache.RemoveAt(propertyRefCache.Count - 1);
			}
			list.AddRange(propertyRefCache);
			_propertyRefsSorted = list.ToArray();
		}
		else
		{
			_propertyRefsSorted = propertyRefCache.ToArray();
		}
		frame.PropertyRefCache = null;
	}

	internal void UpdateSortedParameterCache(ref ReadStackFrame frame)
	{
		List<ParameterRef> parameterRefCache = frame.CtorArgumentState.ParameterRefCache;
		if (_parameterRefsSorted != null)
		{
			List<ParameterRef> list = new List<ParameterRef>(_parameterRefsSorted);
			while (list.Count + parameterRefCache.Count > 32)
			{
				parameterRefCache.RemoveAt(parameterRefCache.Count - 1);
			}
			list.AddRange(parameterRefCache);
			_parameterRefsSorted = list.ToArray();
		}
		else
		{
			_parameterRefsSorted = parameterRefCache.ToArray();
		}
		frame.CtorArgumentState.ParameterRefCache = null;
	}

	internal void InitializePropCache()
	{
		JsonSerializerContext context = Options._context;
		JsonPropertyInfo[] array;
		if (PropInitFunc == null || (array = PropInitFunc(context)) == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(context, Type);
			return;
		}
		Dictionary<string, JsonPropertyInfo> ignoredMembers = null;
		JsonPropertyDictionary<JsonPropertyInfo> propertyCache = new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, array.Length);
		foreach (JsonPropertyInfo jsonPropertyInfo in array)
		{
			bool srcGen_HasJsonInclude = jsonPropertyInfo.SrcGen_HasJsonInclude;
			if (!jsonPropertyInfo.SrcGen_IsPublic)
			{
				if (srcGen_HasJsonInclude)
				{
					ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(jsonPropertyInfo.ClrName, jsonPropertyInfo.DeclaringType);
				}
			}
			else if (jsonPropertyInfo.MemberType != MemberTypes.Field || srcGen_HasJsonInclude || Options.IncludeFields)
			{
				if (jsonPropertyInfo.SrcGen_IsExtensionData)
				{
					DataExtensionProperty = jsonPropertyInfo;
				}
				else
				{
					CacheMember(jsonPropertyInfo, propertyCache, ref ignoredMembers);
				}
			}
		}
		PropertyCache = propertyCache;
	}

	internal void InitializeParameterCache()
	{
		JsonSerializerContext context = Options._context;
		JsonParameterInfoValues[] jsonParameters;
		if (CtorParamInitFunc == null || (jsonParameters = CtorParamInitFunc()) == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeCtorParams(context, Type);
		}
		else
		{
			InitializeConstructorParameters(jsonParameters, sourceGenMode: true);
		}
	}

	internal JsonTypeInfo()
	{
	}

	internal JsonTypeInfo(Type type, JsonSerializerOptions options, bool dummy)
	{
		Type = type;
		Options = options ?? throw new ArgumentNullException("options");
		PropertyInfoForTypeInfo = null;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	internal JsonTypeInfo(Type type, JsonSerializerOptions options)
		: this(type, GetConverter(type, null, null, out var runtimeType, options), runtimeType, options)
	{
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	internal JsonTypeInfo(Type type, JsonConverter converter, Type runtimeType, JsonSerializerOptions options)
	{
		Type = type;
		Options = options;
		JsonNumberHandling? numberHandlingForType = GetNumberHandlingForType(Type);
		PropertyInfoForTypeInfo = CreatePropertyInfoForTypeInfo(Type, runtimeType, converter, numberHandlingForType, Options);
		ElementType = converter.ElementType;
		switch (PropertyInfoForTypeInfo.ConverterStrategy)
		{
		case ConverterStrategy.Object:
		{
			CreateObject = Options.MemberAccessorStrategy.CreateConstructor(type);
			Dictionary<string, JsonPropertyInfo> ignoredMembers = null;
			PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			bool propertyOrderSpecified = false;
			PropertyCache = new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, properties.Length);
			Type type2 = type;
			while (true)
			{
				PropertyInfo[] array = properties;
				foreach (PropertyInfo propertyInfo in array)
				{
					bool flag = propertyInfo.IsVirtual();
					string name = propertyInfo.Name;
					if (propertyInfo.GetIndexParameters().Length != 0 || PropertyIsOverridenAndIgnored(name, propertyInfo.PropertyType, flag, ignoredMembers))
					{
						continue;
					}
					MethodInfo? getMethod = propertyInfo.GetMethod;
					if ((object)getMethod == null || !getMethod.IsPublic)
					{
						MethodInfo? setMethod = propertyInfo.SetMethod;
						if ((object)setMethod == null || !setMethod.IsPublic)
						{
							if (JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(propertyInfo) != null)
							{
								ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(name, type2);
							}
							continue;
						}
					}
					CacheMember(type2, propertyInfo.PropertyType, propertyInfo, flag, numberHandlingForType, ref propertyOrderSpecified, ref ignoredMembers);
				}
				FieldInfo[] fields = type2.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					string name2 = fieldInfo.Name;
					if (PropertyIsOverridenAndIgnored(name2, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
					{
						continue;
					}
					bool flag2 = JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(fieldInfo) != null;
					if (fieldInfo.IsPublic)
					{
						if (flag2 || Options.IncludeFields)
						{
							CacheMember(type2, fieldInfo.FieldType, fieldInfo, isVirtual: false, numberHandlingForType, ref propertyOrderSpecified, ref ignoredMembers);
						}
					}
					else if (flag2)
					{
						ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(name2, type2);
					}
				}
				type2 = type2.BaseType;
				if (type2 == null)
				{
					break;
				}
				properties = type2.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (propertyOrderSpecified)
			{
				PropertyCache.List.Sort((KeyValuePair<string, JsonPropertyInfo> p1, KeyValuePair<string, JsonPropertyInfo> p2) => p1.Value.Order.CompareTo(p2.Value.Order));
			}
			if (converter.ConstructorIsParameterized)
			{
				converter.Initialize(Options, this);
				ParameterInfo[] parameters = converter.ConstructorInfo.GetParameters();
				int num = parameters.Length;
				JsonParameterInfoValues[] parameterInfoArray = GetParameterInfoArray(parameters);
				InitializeConstructorParameters(parameterInfoArray);
			}
			break;
		}
		case ConverterStrategy.Enumerable:
			CreateObject = Options.MemberAccessorStrategy.CreateConstructor(runtimeType);
			if (converter.RequiresDynamicMemberAccessors)
			{
				converter.Initialize(Options, this);
			}
			break;
		case ConverterStrategy.Dictionary:
			KeyType = converter.KeyType;
			CreateObject = Options.MemberAccessorStrategy.CreateConstructor(runtimeType);
			if (converter.RequiresDynamicMemberAccessors)
			{
				converter.Initialize(Options, this);
			}
			break;
		case ConverterStrategy.Value:
			CreateObject = Options.MemberAccessorStrategy.CreateConstructor(type);
			break;
		case ConverterStrategy.None:
			ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
			break;
		default:
			throw new InvalidOperationException();
		}
	}

	private void CacheMember(Type declaringType, Type memberType, MemberInfo memberInfo, bool isVirtual, JsonNumberHandling? typeNumberHandling, ref bool propertyOrderSpecified, ref Dictionary<string, JsonPropertyInfo> ignoredMembers)
	{
		bool flag = memberInfo.GetCustomAttribute(typeof(JsonExtensionDataAttribute)) != null;
		if (flag && DataExtensionProperty != null)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
		}
		JsonPropertyInfo jsonPropertyInfo = AddProperty(memberInfo, memberType, declaringType, isVirtual, typeNumberHandling, Options);
		if (flag)
		{
			ValidateAndAssignDataExtensionProperty(jsonPropertyInfo);
			return;
		}
		CacheMember(jsonPropertyInfo, PropertyCache, ref ignoredMembers);
		propertyOrderSpecified |= jsonPropertyInfo.Order != 0;
	}

	private void CacheMember(JsonPropertyInfo jsonPropertyInfo, JsonPropertyDictionary<JsonPropertyInfo> propertyCache, ref Dictionary<string, JsonPropertyInfo> ignoredMembers)
	{
		string clrName = jsonPropertyInfo.ClrName;
		if (!propertyCache.TryAdd(jsonPropertyInfo.NameAsString, jsonPropertyInfo))
		{
			JsonPropertyInfo jsonPropertyInfo2 = propertyCache[jsonPropertyInfo.NameAsString];
			if (jsonPropertyInfo2.IsIgnored)
			{
				propertyCache[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
			}
			else if (!jsonPropertyInfo.IsIgnored && jsonPropertyInfo2.ClrName != clrName)
			{
				Dictionary<string, JsonPropertyInfo> obj = ignoredMembers;
				if (obj == null || !obj.ContainsKey(clrName))
				{
					ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, jsonPropertyInfo);
				}
			}
		}
		if (jsonPropertyInfo.IsIgnored)
		{
			(ignoredMembers ?? (ignoredMembers = new Dictionary<string, JsonPropertyInfo>())).Add(clrName, jsonPropertyInfo);
		}
	}

	private void InitializeConstructorParameters(JsonParameterInfoValues[] jsonParameters, bool sourceGenMode = false)
	{
		JsonPropertyDictionary<JsonParameterInfo> jsonPropertyDictionary = new JsonPropertyDictionary<JsonParameterInfo>(Options.PropertyNameCaseInsensitive, jsonParameters.Length);
		Dictionary<ParameterLookupKey, ParameterLookupValue> dictionary = new Dictionary<ParameterLookupKey, ParameterLookupValue>(PropertyCache.Count);
		foreach (KeyValuePair<string, JsonPropertyInfo> item in PropertyCache.List)
		{
			JsonPropertyInfo value = item.Value;
			string clrName = value.ClrName;
			ParameterLookupKey key = new ParameterLookupKey(clrName, value.DeclaredPropertyType);
			ParameterLookupValue value2 = new ParameterLookupValue(value);
			if (!dictionary.TryAdd(in key, in value2))
			{
				ParameterLookupValue parameterLookupValue = dictionary[key];
				parameterLookupValue.DuplicateName = clrName;
			}
		}
		foreach (JsonParameterInfoValues jsonParameterInfoValues in jsonParameters)
		{
			ParameterLookupKey parameterLookupKey = new ParameterLookupKey(jsonParameterInfoValues.Name, jsonParameterInfoValues.ParameterType);
			if (dictionary.TryGetValue(parameterLookupKey, out var value3))
			{
				if (value3.DuplicateName != null)
				{
					ThrowHelper.ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(Type, jsonParameterInfoValues.Name, value3.JsonPropertyInfo.NameAsString, value3.DuplicateName);
				}
				JsonPropertyInfo jsonPropertyInfo = value3.JsonPropertyInfo;
				JsonParameterInfo value4 = CreateConstructorParameter(jsonParameterInfoValues, jsonPropertyInfo, sourceGenMode, Options);
				jsonPropertyDictionary.Add(jsonPropertyInfo.NameAsString, value4);
			}
			else if (DataExtensionProperty != null && StringComparer.OrdinalIgnoreCase.Equals(parameterLookupKey.Name, DataExtensionProperty.NameAsString))
			{
				ThrowHelper.ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(DataExtensionProperty);
			}
		}
		ParameterCache = jsonPropertyDictionary;
		ParameterCount = jsonParameters.Length;
	}

	private static JsonParameterInfoValues[] GetParameterInfoArray(ParameterInfo[] parameters)
	{
		int num = parameters.Length;
		JsonParameterInfoValues[] array = new JsonParameterInfoValues[num];
		for (int i = 0; i < num; i++)
		{
			ParameterInfo parameterInfo = parameters[i];
			JsonParameterInfoValues jsonParameterInfoValues = new JsonParameterInfoValues
			{
				Name = parameterInfo.Name,
				ParameterType = parameterInfo.ParameterType,
				Position = parameterInfo.Position,
				HasDefaultValue = parameterInfo.HasDefaultValue,
				DefaultValue = parameterInfo.GetDefaultValue()
			};
			array[i] = jsonParameterInfoValues;
		}
		return array;
	}

	private static bool PropertyIsOverridenAndIgnored(string currentMemberName, Type currentMemberType, bool currentMemberIsVirtual, Dictionary<string, JsonPropertyInfo> ignoredMembers)
	{
		if (ignoredMembers == null || !ignoredMembers.TryGetValue(currentMemberName, out var value))
		{
			return false;
		}
		if (currentMemberType == value.DeclaredPropertyType && currentMemberIsVirtual)
		{
			return value.IsVirtual;
		}
		return false;
	}

	private void ValidateAndAssignDataExtensionProperty(JsonPropertyInfo jsonPropertyInfo)
	{
		if (!IsValidDataExtensionProperty(jsonPropertyInfo))
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(Type, jsonPropertyInfo);
		}
		DataExtensionProperty = jsonPropertyInfo;
	}

	private bool IsValidDataExtensionProperty(JsonPropertyInfo jsonPropertyInfo)
	{
		Type declaredPropertyType = jsonPropertyInfo.DeclaredPropertyType;
		if (typeof(IDictionary<string, object>).IsAssignableFrom(declaredPropertyType) || typeof(IDictionary<string, JsonElement>).IsAssignableFrom(declaredPropertyType) || (declaredPropertyType.FullName == "System.Text.Json.Nodes.JsonObject" && (object)declaredPropertyType.Assembly == GetType().Assembly))
		{
			return Options.GetConverterInternal(declaredPropertyType) != null;
		}
		return false;
	}

	private static JsonParameterInfo CreateConstructorParameter(JsonParameterInfoValues parameterInfo, JsonPropertyInfo jsonPropertyInfo, bool sourceGenMode, JsonSerializerOptions options)
	{
		if (jsonPropertyInfo.IsIgnored)
		{
			return JsonParameterInfo.CreateIgnoredParameterPlaceholder(parameterInfo, jsonPropertyInfo, sourceGenMode);
		}
		JsonConverter converterBase = jsonPropertyInfo.ConverterBase;
		JsonParameterInfo jsonParameterInfo = converterBase.CreateJsonParameterInfo();
		jsonParameterInfo.Initialize(parameterInfo, jsonPropertyInfo, options);
		return jsonParameterInfo;
	}

	private static JsonConverter GetConverter(Type type, Type parentClassType, MemberInfo memberInfo, out Type runtimeType, JsonSerializerOptions options)
	{
		ValidateType(type, parentClassType, memberInfo, options);
		JsonConverter jsonConverter = options.DetermineConverter(parentClassType, type, memberInfo);
		Type runtimeType2 = jsonConverter.RuntimeType;
		if (type == runtimeType2)
		{
			runtimeType = type;
		}
		else if (type.IsInterface)
		{
			runtimeType = runtimeType2;
		}
		else if (runtimeType2.IsInterface)
		{
			runtimeType = type;
		}
		else if (type.IsAssignableFrom(runtimeType2))
		{
			runtimeType = runtimeType2;
		}
		else if (runtimeType2.IsAssignableFrom(type) || jsonConverter.TypeToConvert.IsAssignableFrom(type))
		{
			runtimeType = type;
		}
		else
		{
			runtimeType = null;
			ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
		}
		return jsonConverter;
	}

	private static void ValidateType(Type type, Type parentClassType, MemberInfo memberInfo, JsonSerializerOptions options)
	{
		if (!options.TypeIsCached(type) && IsInvalidForSerialization(type))
		{
			ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(type, parentClassType, memberInfo);
		}
	}

	private static bool IsInvalidForSerialization(Type type)
	{
		if (!type.IsPointer && !IsByRefLike(type))
		{
			return type.ContainsGenericParameters;
		}
		return true;
	}

	private static bool IsByRefLike(Type type)
	{
		return type.IsByRefLike;
	}

	private static JsonNumberHandling? GetNumberHandlingForType(Type type)
	{
		return ((JsonNumberHandlingAttribute)JsonSerializerOptions.GetAttributeThatCanHaveMultiple(type, typeof(JsonNumberHandlingAttribute)))?.Handling;
	}
}
