using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata;

[DebuggerDisplay("MemberInfo={MemberInfo}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JsonPropertyInfo
{
	internal static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

	private JsonTypeInfo _runtimeTypeInfo;

	internal ConverterStrategy ConverterStrategy;

	internal abstract JsonConverter ConverterBase { get; set; }

	internal Type DeclaredPropertyType { get; set; }

	internal bool HasGetter { get; set; }

	internal bool HasSetter { get; set; }

	internal bool IgnoreDefaultValuesOnRead { get; private set; }

	internal bool IgnoreDefaultValuesOnWrite { get; private set; }

	internal bool IsForTypeInfo { get; set; }

	internal string NameAsString { get; set; }

	internal byte[] NameAsUtf8Bytes { get; set; }

	internal byte[] EscapedNameSection { get; set; }

	internal JsonSerializerOptions Options { get; set; }

	internal int Order { get; set; }

	internal Type DeclaringType { get; set; }

	internal MemberInfo? MemberInfo { get; private set; }

	internal JsonTypeInfo RuntimeTypeInfo
	{
		get
		{
			if (_runtimeTypeInfo == null)
			{
				_runtimeTypeInfo = Options.GetOrAddClass(RuntimePropertyType);
			}
			return _runtimeTypeInfo;
		}
		set
		{
			_runtimeTypeInfo = value;
		}
	}

	internal Type? RuntimePropertyType { get; set; }

	internal bool ShouldSerialize { get; set; }

	internal bool ShouldDeserialize { get; set; }

	internal bool IsIgnored { get; set; }

	internal bool SrcGen_HasJsonInclude { get; set; }

	internal bool SrcGen_IsExtensionData { get; set; }

	internal bool SrcGen_IsPublic { get; set; }

	internal JsonNumberHandling? NumberHandling { get; set; }

	internal bool PropertyTypeCanBeNull { get; set; }

	internal JsonIgnoreCondition? IgnoreCondition { get; set; }

	internal MemberTypes MemberType { get; set; }

	internal string? ClrName { get; set; }

	internal bool IsVirtual { get; set; }

	internal abstract object? DefaultValue { get; }

	internal JsonPropertyInfo()
	{
	}

	internal static JsonPropertyInfo GetPropertyPlaceholder()
	{
		JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfo<object>();
		jsonPropertyInfo.NameAsString = string.Empty;
		return jsonPropertyInfo;
	}

	internal static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(MemberInfo memberInfo, Type memberType, bool isVirtual, JsonSerializerOptions options)
	{
		JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfo<sbyte>();
		jsonPropertyInfo.Options = options;
		jsonPropertyInfo.MemberInfo = memberInfo;
		jsonPropertyInfo.IsIgnored = true;
		jsonPropertyInfo.DeclaredPropertyType = memberType;
		jsonPropertyInfo.IsVirtual = isVirtual;
		jsonPropertyInfo.DeterminePropertyName();
		return jsonPropertyInfo;
	}

	internal virtual void GetPolicies(JsonIgnoreCondition? ignoreCondition, JsonNumberHandling? declaringTypeNumberHandling)
	{
		if (IsForTypeInfo)
		{
			DetermineNumberHandlingForTypeInfo(declaringTypeNumberHandling);
			return;
		}
		DetermineSerializationCapabilities(ignoreCondition);
		DeterminePropertyName();
		DetermineIgnoreCondition(ignoreCondition);
		JsonPropertyOrderAttribute attribute = GetAttribute<JsonPropertyOrderAttribute>(MemberInfo);
		if (attribute != null)
		{
			Order = attribute.Order;
		}
		DetermineNumberHandlingForProperty(GetAttribute<JsonNumberHandlingAttribute>(MemberInfo)?.Handling, declaringTypeNumberHandling);
	}

	private void DeterminePropertyName()
	{
		ClrName = MemberInfo.Name;
		JsonPropertyNameAttribute attribute = GetAttribute<JsonPropertyNameAttribute>(MemberInfo);
		if (attribute != null)
		{
			string name = attribute.Name;
			if (name == null)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(DeclaringType, this);
			}
			NameAsString = name;
		}
		else if (Options.PropertyNamingPolicy != null)
		{
			string text = Options.PropertyNamingPolicy.ConvertName(MemberInfo.Name);
			if (text == null)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(DeclaringType, this);
			}
			NameAsString = text;
		}
		else
		{
			NameAsString = MemberInfo.Name;
		}
		NameAsUtf8Bytes = Encoding.UTF8.GetBytes(NameAsString);
		EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(NameAsUtf8Bytes, Options.Encoder);
	}

	internal void DetermineSerializationCapabilities(JsonIgnoreCondition? ignoreCondition)
	{
		if ((ConverterStrategy & (ConverterStrategy)24) == 0)
		{
			bool flag = ignoreCondition.HasValue || ((MemberType == MemberTypes.Property) ? (!Options.IgnoreReadOnlyProperties) : (!Options.IgnoreReadOnlyFields));
			ShouldSerialize = HasGetter && (HasSetter || flag);
			ShouldDeserialize = HasSetter;
		}
		else if (HasGetter)
		{
			ShouldSerialize = true;
			if (HasSetter)
			{
				ShouldDeserialize = true;
			}
		}
	}

	internal void DetermineIgnoreCondition(JsonIgnoreCondition? ignoreCondition)
	{
		if (ignoreCondition.HasValue)
		{
			if (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault)
			{
				IgnoreDefaultValuesOnWrite = true;
			}
			else if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
			{
				if (PropertyTypeCanBeNull)
				{
					IgnoreDefaultValuesOnWrite = true;
				}
				else
				{
					ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(ClrName, DeclaringType);
				}
			}
		}
		else if (Options.IgnoreNullValues)
		{
			if (PropertyTypeCanBeNull)
			{
				IgnoreDefaultValuesOnRead = true;
				IgnoreDefaultValuesOnWrite = true;
			}
		}
		else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
		{
			if (PropertyTypeCanBeNull)
			{
				IgnoreDefaultValuesOnWrite = true;
			}
		}
		else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
		{
			IgnoreDefaultValuesOnWrite = true;
		}
	}

	internal void DetermineNumberHandlingForTypeInfo(JsonNumberHandling? numberHandling)
	{
		if (numberHandling.HasValue && numberHandling != JsonNumberHandling.Strict && !ConverterBase.IsInternalConverter)
		{
			ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
		}
		if (NumberHandingIsApplicable())
		{
			NumberHandling = numberHandling;
			if (!NumberHandling.HasValue && Options.NumberHandling != 0)
			{
				NumberHandling = Options.NumberHandling;
			}
		}
	}

	internal void DetermineNumberHandlingForProperty(JsonNumberHandling? propertyNumberHandling, JsonNumberHandling? declaringTypeNumberHandling)
	{
		if (NumberHandingIsApplicable())
		{
			JsonNumberHandling? numberHandling = propertyNumberHandling ?? declaringTypeNumberHandling;
			if (!numberHandling.HasValue && Options.NumberHandling != 0)
			{
				numberHandling = Options.NumberHandling;
			}
			NumberHandling = numberHandling;
		}
		else if (propertyNumberHandling.HasValue && propertyNumberHandling != JsonNumberHandling.Strict)
		{
			ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
		}
	}

	private bool NumberHandingIsApplicable()
	{
		if (ConverterBase.IsInternalConverterForNumberType)
		{
			return true;
		}
		Type type = ((ConverterBase.IsInternalConverter && ((ConverterStrategy)24 & ConverterStrategy) != 0) ? ConverterBase.ElementType : DeclaredPropertyType);
		type = Nullable.GetUnderlyingType(type) ?? type;
		if (!(type == typeof(byte)) && !(type == typeof(decimal)) && !(type == typeof(double)) && !(type == typeof(short)) && !(type == typeof(int)) && !(type == typeof(long)) && !(type == typeof(sbyte)) && !(type == typeof(float)) && !(type == typeof(ushort)) && !(type == typeof(uint)) && !(type == typeof(ulong)))
		{
			return type == JsonTypeInfo.ObjectType;
		}
		return true;
	}

	internal static TAttribute GetAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute
	{
		return (TAttribute)memberInfo.GetCustomAttribute(typeof(TAttribute), inherit: false);
	}

	internal abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);

	internal abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

	internal abstract object GetValueAsObject(object obj);

	internal virtual void Initialize(Type parentClassType, Type declaredPropertyType, Type runtimePropertyType, ConverterStrategy runtimeClassType, MemberInfo memberInfo, bool isVirtual, JsonConverter converter, JsonIgnoreCondition? ignoreCondition, JsonNumberHandling? parentTypeNumberHandling, JsonSerializerOptions options)
	{
		DeclaringType = parentClassType;
		DeclaredPropertyType = declaredPropertyType;
		RuntimePropertyType = runtimePropertyType;
		ConverterStrategy = runtimeClassType;
		MemberInfo = memberInfo;
		IsVirtual = isVirtual;
		ConverterBase = converter;
		Options = options;
	}

	internal abstract void InitializeForTypeInfo(Type declaredType, JsonTypeInfo runtimeTypeInfo, JsonConverter converter, JsonSerializerOptions options);

	internal bool ReadJsonAndAddExtensionProperty(object obj, ref ReadStack state, ref Utf8JsonReader reader)
	{
		object valueAsObject = GetValueAsObject(obj);
		if (valueAsObject is IDictionary<string, object> dictionary)
		{
			if (reader.TokenType == JsonTokenType.Null)
			{
				dictionary[state.Current.JsonPropertyNameAsString] = null;
			}
			else
			{
				JsonConverter<object> jsonConverter = (JsonConverter<object>)GetDictionaryValueConverter(JsonTypeInfo.ObjectType);
				object value = jsonConverter.Read(ref reader, JsonTypeInfo.ObjectType, Options);
				dictionary[state.Current.JsonPropertyNameAsString] = value;
			}
		}
		else if (valueAsObject is IDictionary<string, JsonElement> dictionary2)
		{
			Type typeFromHandle = typeof(JsonElement);
			JsonConverter<JsonElement> jsonConverter2 = (JsonConverter<JsonElement>)GetDictionaryValueConverter(typeFromHandle);
			JsonElement value2 = jsonConverter2.Read(ref reader, typeFromHandle, Options);
			dictionary2[state.Current.JsonPropertyNameAsString] = value2;
		}
		else
		{
			ConverterBase.ReadElementAndSetProperty(valueAsObject, state.Current.JsonPropertyNameAsString, ref reader, Options, ref state);
		}
		return true;
		JsonConverter GetDictionaryValueConverter(Type dictionaryValueType)
		{
			JsonTypeInfo elementTypeInfo = RuntimeTypeInfo.ElementTypeInfo;
			if (elementTypeInfo != null)
			{
				return elementTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
			}
			return Options.GetConverterInternal(dictionaryValueType);
		}
	}

	internal abstract bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader);

	internal abstract bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object value);

	internal bool ReadJsonExtensionDataValue(ref ReadStack state, ref Utf8JsonReader reader, out object value)
	{
		if (RuntimeTypeInfo.ElementType == JsonTypeInfo.ObjectType && reader.TokenType == JsonTokenType.Null)
		{
			value = null;
			return true;
		}
		JsonConverter<JsonElement> jsonConverter = (JsonConverter<JsonElement>)Options.GetConverterInternal(typeof(JsonElement));
		if (!jsonConverter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out var value2))
		{
			value = null;
			return false;
		}
		value = value2;
		return true;
	}

	internal abstract void SetExtensionDictionaryAsObject(object obj, object extensionDict);
}
internal sealed class JsonPropertyInfo<T> : JsonPropertyInfo
{
	private bool _converterIsExternalAndPolymorphic;

	private bool _propertyTypeEqualsTypeToConvert;

	internal Func<object, T> Get { get; set; }

	internal Action<object, T> Set { get; set; }

	internal override object DefaultValue => default(T);

	public JsonConverter<T> Converter { get; internal set; }

	internal override JsonConverter ConverterBase
	{
		get
		{
			return Converter;
		}
		set
		{
			Converter = (JsonConverter<T>)value;
		}
	}

	internal override void Initialize(Type parentClassType, Type declaredPropertyType, Type runtimePropertyType, ConverterStrategy runtimeClassType, MemberInfo memberInfo, bool isVirtual, JsonConverter converter, JsonIgnoreCondition? ignoreCondition, JsonNumberHandling? parentTypeNumberHandling, JsonSerializerOptions options)
	{
		base.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, runtimeClassType, memberInfo, isVirtual, converter, ignoreCondition, parentTypeNumberHandling, options);
		if (!(memberInfo is PropertyInfo propertyInfo))
		{
			if (memberInfo is FieldInfo fieldInfo)
			{
				base.HasGetter = true;
				Get = options.MemberAccessorStrategy.CreateFieldGetter<T>(fieldInfo);
				if (!fieldInfo.IsInitOnly)
				{
					base.HasSetter = true;
					Set = options.MemberAccessorStrategy.CreateFieldSetter<T>(fieldInfo);
				}
				base.MemberType = MemberTypes.Field;
			}
			else
			{
				base.IsForTypeInfo = true;
				base.HasGetter = true;
				base.HasSetter = true;
			}
		}
		else
		{
			bool flag = JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(propertyInfo) != null;
			MethodInfo getMethod = propertyInfo.GetMethod;
			if (getMethod != null && (getMethod.IsPublic || flag))
			{
				base.HasGetter = true;
				Get = options.MemberAccessorStrategy.CreatePropertyGetter<T>(propertyInfo);
			}
			MethodInfo setMethod = propertyInfo.SetMethod;
			if (setMethod != null && (setMethod.IsPublic || flag))
			{
				base.HasSetter = true;
				Set = options.MemberAccessorStrategy.CreatePropertySetter<T>(propertyInfo);
			}
			base.MemberType = MemberTypes.Property;
		}
		_converterIsExternalAndPolymorphic = !converter.IsInternalConverter && base.DeclaredPropertyType != converter.TypeToConvert;
		base.PropertyTypeCanBeNull = base.DeclaredPropertyType.CanBeNull();
		_propertyTypeEqualsTypeToConvert = typeof(T) == base.DeclaredPropertyType;
		GetPolicies(ignoreCondition, parentTypeNumberHandling);
	}

	internal void InitializeForSourceGen(JsonSerializerOptions options, JsonPropertyInfoValues<T> propertyInfo)
	{
		base.Options = options;
		base.ClrName = propertyInfo.PropertyName;
		if (propertyInfo.JsonPropertyName != null)
		{
			base.NameAsString = propertyInfo.JsonPropertyName;
		}
		else if (options.PropertyNamingPolicy == null)
		{
			base.NameAsString = base.ClrName;
		}
		else
		{
			base.NameAsString = options.PropertyNamingPolicy.ConvertName(base.ClrName);
			if (base.NameAsString == null)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(base.DeclaringType, this);
			}
		}
		if (base.NameAsUtf8Bytes == null)
		{
			byte[] array = (base.NameAsUtf8Bytes = Encoding.UTF8.GetBytes(base.NameAsString));
		}
		if (base.EscapedNameSection == null)
		{
			byte[] array = (base.EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(base.NameAsUtf8Bytes, base.Options.Encoder));
		}
		base.SrcGen_IsPublic = propertyInfo.IsPublic;
		base.SrcGen_HasJsonInclude = propertyInfo.HasJsonInclude;
		base.SrcGen_IsExtensionData = propertyInfo.IsExtensionData;
		base.DeclaredPropertyType = typeof(T);
		JsonTypeInfo propertyTypeInfo = propertyInfo.PropertyTypeInfo;
		Type declaringType = propertyInfo.DeclaringType;
		JsonConverter jsonConverter = propertyInfo.Converter;
		if (jsonConverter == null)
		{
			jsonConverter = propertyTypeInfo.PropertyInfoForTypeInfo.ConverterBase as JsonConverter<T>;
			if (jsonConverter == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.ConverterForPropertyMustBeValid, declaringType, base.ClrName, typeof(T)));
			}
		}
		ConverterBase = jsonConverter;
		if (propertyInfo.IgnoreCondition == JsonIgnoreCondition.Always)
		{
			base.IsIgnored = true;
			return;
		}
		Get = propertyInfo.Getter;
		Set = propertyInfo.Setter;
		base.HasGetter = Get != null;
		base.HasSetter = Set != null;
		base.RuntimeTypeInfo = propertyTypeInfo;
		base.DeclaringType = declaringType;
		base.IgnoreCondition = propertyInfo.IgnoreCondition;
		base.MemberType = (propertyInfo.IsProperty ? MemberTypes.Property : MemberTypes.Field);
		_converterIsExternalAndPolymorphic = !ConverterBase.IsInternalConverter && base.DeclaredPropertyType != ConverterBase.TypeToConvert;
		base.PropertyTypeCanBeNull = typeof(T).CanBeNull();
		_propertyTypeEqualsTypeToConvert = ConverterBase.TypeToConvert == typeof(T);
		ConverterStrategy = Converter.ConverterStrategy;
		base.RuntimePropertyType = base.DeclaredPropertyType;
		DetermineIgnoreCondition(base.IgnoreCondition);
		DetermineNumberHandlingForProperty(propertyInfo.NumberHandling, null);
		DetermineSerializationCapabilities(base.IgnoreCondition);
	}

	internal override void InitializeForTypeInfo(Type declaredType, JsonTypeInfo runtimeTypeInfo, JsonConverter converter, JsonSerializerOptions options)
	{
		base.DeclaredPropertyType = declaredType;
		base.RuntimePropertyType = declaredType;
		ConverterStrategy = converter.ConverterStrategy;
		base.RuntimeTypeInfo = runtimeTypeInfo;
		ConverterBase = converter;
		base.Options = options;
		base.IsForTypeInfo = true;
		base.HasGetter = true;
		base.HasSetter = true;
		_converterIsExternalAndPolymorphic = !converter.IsInternalConverter && declaredType != converter.TypeToConvert;
		base.PropertyTypeCanBeNull = declaredType.CanBeNull();
		_propertyTypeEqualsTypeToConvert = typeof(T) == declaredType;
	}

	internal override object GetValueAsObject(object obj)
	{
		if (base.IsForTypeInfo)
		{
			return obj;
		}
		return Get(obj);
	}

	internal override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
	{
		T value = Get(obj);
		if (!typeof(T).IsValueType && base.Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles && value != null && (Converter.CanBePolymorphic || ConverterStrategy != ConverterStrategy.Value) && state.ReferenceResolver.ContainsReferenceForCycleDetection(value))
		{
			value = default(T);
		}
		if (base.IgnoreDefaultValuesOnWrite)
		{
			if (value == null)
			{
				return true;
			}
			if (!base.PropertyTypeCanBeNull)
			{
				if (_propertyTypeEqualsTypeToConvert)
				{
					if (EqualityComparer<T>.Default.Equals(default(T), value))
					{
						return true;
					}
				}
				else if (base.RuntimeTypeInfo.GenericMethods.IsDefaultValue(value))
				{
					return true;
				}
			}
		}
		if (value == null)
		{
			if (Converter.HandleNullOnWrite)
			{
				if ((int)state.Current.PropertyState < 2)
				{
					state.Current.PropertyState = StackFramePropertyState.Name;
					writer.WritePropertyNameSection(base.EscapedNameSection);
				}
				int currentDepth = writer.CurrentDepth;
				Converter.Write(writer, value, base.Options);
				if (currentDepth != writer.CurrentDepth)
				{
					ThrowHelper.ThrowJsonException_SerializationConverterWrite(Converter);
				}
			}
			else
			{
				writer.WriteNullSection(base.EscapedNameSection);
			}
			return true;
		}
		if ((int)state.Current.PropertyState < 2)
		{
			state.Current.PropertyState = StackFramePropertyState.Name;
			writer.WritePropertyNameSection(base.EscapedNameSection);
		}
		return Converter.TryWrite(writer, in value, base.Options, ref state);
	}

	internal override bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer)
	{
		T val = Get(obj);
		if (val == null)
		{
			return true;
		}
		return Converter.TryWriteDataExtensionProperty(writer, val, base.Options, ref state);
	}

	internal override bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader)
	{
		bool flag = reader.TokenType == JsonTokenType.Null;
		bool flag2;
		if (flag && !Converter.HandleNullOnRead && !state.IsContinuation)
		{
			if (!base.PropertyTypeCanBeNull)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Converter.TypeToConvert);
			}
			if (!base.IgnoreDefaultValuesOnRead)
			{
				Set(obj, default(T));
			}
			flag2 = true;
		}
		else if (Converter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			if (!flag || !base.IgnoreDefaultValuesOnRead || !base.PropertyTypeCanBeNull)
			{
				T arg = Converter.Read(ref reader, base.RuntimePropertyType, base.Options);
				Set(obj, arg);
			}
			flag2 = true;
		}
		else
		{
			flag2 = true;
			if (!flag || !base.IgnoreDefaultValuesOnRead || !base.PropertyTypeCanBeNull || state.IsContinuation)
			{
				flag2 = Converter.TryRead(ref reader, base.RuntimePropertyType, base.Options, ref state, out var value);
				if (flag2)
				{
					if (_converterIsExternalAndPolymorphic)
					{
						if (value != null)
						{
							Type type = value.GetType();
							if (!base.DeclaredPropertyType.IsAssignableFrom(type))
							{
								ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(type, base.DeclaredPropertyType);
							}
						}
						else if (!base.PropertyTypeCanBeNull)
						{
							ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(base.DeclaredPropertyType);
						}
					}
					Set(obj, value);
				}
			}
		}
		return flag2;
	}

	internal override bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object value)
	{
		bool result;
		if (reader.TokenType == JsonTokenType.Null && !Converter.HandleNullOnRead && !state.IsContinuation)
		{
			if (!base.PropertyTypeCanBeNull)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Converter.TypeToConvert);
			}
			value = default(T);
			result = true;
		}
		else if (Converter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			value = Converter.Read(ref reader, base.RuntimePropertyType, base.Options);
			result = true;
		}
		else
		{
			result = Converter.TryRead(ref reader, base.RuntimePropertyType, base.Options, ref state, out var value2);
			value = value2;
		}
		return result;
	}

	internal override void SetExtensionDictionaryAsObject(object obj, object extensionDict)
	{
		T arg = (T)extensionDict;
		Set(obj, arg);
	}
}
