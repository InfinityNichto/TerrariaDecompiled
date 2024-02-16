using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

public abstract class JsonConverter
{
	internal bool IsInternalConverterForNumberType;

	internal abstract ConverterStrategy ConverterStrategy { get; }

	internal bool CanUseDirectReadOrWrite { get; set; }

	internal virtual bool CanHaveIdMetadata => false;

	internal bool CanBePolymorphic { get; set; }

	internal abstract Type? ElementType { get; }

	internal abstract Type? KeyType { get; }

	internal bool IsValueType { get; set; }

	internal bool IsInternalConverter { get; set; }

	internal virtual Type RuntimeType => TypeToConvert;

	internal abstract Type TypeToConvert { get; }

	internal virtual bool ConstructorIsParameterized { get; }

	internal ConstructorInfo? ConstructorInfo { get; set; }

	internal virtual bool RequiresDynamicMemberAccessors { get; }

	internal JsonConverter()
	{
	}

	public abstract bool CanConvert(Type typeToConvert);

	internal virtual object CreateObject(JsonSerializerOptions options)
	{
		throw new InvalidOperationException(System.SR.NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty);
	}

	internal virtual void ReadElementAndSetProperty(object obj, string propertyName, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		throw new InvalidOperationException(System.SR.NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty);
	}

	internal abstract JsonPropertyInfo CreateJsonPropertyInfo();

	internal abstract JsonParameterInfo CreateJsonParameterInfo();

	internal abstract object ReadCoreAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state);

	internal bool ShouldFlush(Utf8JsonWriter writer, ref WriteStack state)
	{
		if (state.FlushThreshold > 0)
		{
			return writer.BytesPending > state.FlushThreshold;
		}
		return false;
	}

	internal abstract bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object value);

	internal abstract bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state);

	internal abstract bool WriteCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state);

	internal abstract void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty);

	internal virtual void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
	}

	internal virtual void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool SingleValueReadWithReadAhead(ConverterStrategy converterStrategy, ref Utf8JsonReader reader, ref ReadStack state)
	{
		if (!state.ReadAhead || converterStrategy != ConverterStrategy.Value)
		{
			return reader.Read();
		}
		return DoSingleValueReadWithReadAhead(ref reader, ref state);
	}

	internal static bool DoSingleValueReadWithReadAhead(ref Utf8JsonReader reader, ref ReadStack state)
	{
		JsonReaderState currentState = reader.CurrentState;
		long bytesConsumed = reader.BytesConsumed;
		if (!reader.Read())
		{
			return false;
		}
		JsonTokenType tokenType = reader.TokenType;
		if (tokenType == JsonTokenType.StartObject || tokenType == JsonTokenType.StartArray)
		{
			bool flag = reader.TrySkip();
			reader = new Utf8JsonReader(reader.OriginalSpan.Slice(checked((int)bytesConsumed)), reader.IsFinalBlock, currentState);
			state.BytesConsumed += bytesConsumed;
			if (!flag)
			{
				return false;
			}
			reader.ReadWithVerify();
		}
		return true;
	}
}
public abstract class JsonConverter<T> : JsonConverter
{
	internal override ConverterStrategy ConverterStrategy => ConverterStrategy.Value;

	internal override Type? KeyType => null;

	internal override Type? ElementType => null;

	public virtual bool HandleNull
	{
		get
		{
			HandleNullOnRead = !CanBeNull;
			HandleNullOnWrite = false;
			return false;
		}
	}

	internal bool HandleNullOnRead { get; private set; }

	internal bool HandleNullOnWrite { get; private set; }

	internal bool CanBeNull { get; }

	internal sealed override Type TypeToConvert => typeof(T);

	internal sealed override object ReadCoreAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		return ReadCore(ref reader, options, ref state);
	}

	internal T ReadCore(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		try
		{
			if (!state.IsContinuation)
			{
				if (!JsonConverter.SingleValueReadWithReadAhead(ConverterStrategy, ref reader, ref state))
				{
					if (state.SupportContinuation)
					{
						state.BytesConsumed += reader.BytesConsumed;
						if (state.Current.ReturnValue == null)
						{
							return default(T);
						}
						return (T)state.Current.ReturnValue;
					}
					state.BytesConsumed += reader.BytesConsumed;
					return default(T);
				}
			}
			else if (!JsonConverter.SingleValueReadWithReadAhead(ConverterStrategy.Value, ref reader, ref state))
			{
				state.BytesConsumed += reader.BytesConsumed;
				return default(T);
			}
			JsonPropertyInfo propertyInfoForTypeInfo = state.Current.JsonTypeInfo.PropertyInfoForTypeInfo;
			if (TryRead(ref reader, propertyInfoForTypeInfo.RuntimePropertyType, options, ref state, out var value) && !reader.Read() && !reader.IsFinalBlock)
			{
				state.Current.ReturnValue = value;
			}
			state.BytesConsumed += reader.BytesConsumed;
			return value;
		}
		catch (JsonReaderException ex)
		{
			ThrowHelper.ReThrowWithPath(ref state, ex);
			return default(T);
		}
		catch (FormatException ex2) when (ex2.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, in reader, ex2);
			return default(T);
		}
		catch (InvalidOperationException ex3) when (ex3.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, in reader, ex3);
			return default(T);
		}
		catch (JsonException ex4) when (ex4.Path == null)
		{
			ThrowHelper.AddJsonExceptionInformation(ref state, in reader, ex4);
			throw;
		}
		catch (NotSupportedException ex5)
		{
			if (ex5.Message.Contains(" Path: "))
			{
				throw;
			}
			ThrowHelper.ThrowNotSupportedException(ref state, in reader, ex5);
			return default(T);
		}
	}

	internal sealed override bool WriteCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (base.IsValueType)
		{
			if (value == null && Nullable.GetUnderlyingType(TypeToConvert) == null)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
			}
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles && value != null)
			{
				state.ReferenceResolver.PushReferenceForCycleDetection(value);
			}
		}
		T value2 = (T)value;
		return WriteCore(writer, in value2, options, ref state);
	}

	internal bool WriteCore(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
	{
		try
		{
			return TryWrite(writer, in value, options, ref state);
		}
		catch (InvalidOperationException ex) when (ex.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, ex);
			throw;
		}
		catch (JsonException ex2) when (ex2.Path == null)
		{
			ThrowHelper.AddJsonExceptionInformation(ref state, ex2);
			throw;
		}
		catch (NotSupportedException ex3)
		{
			if (ex3.Message.Contains(" Path: "))
			{
				throw;
			}
			ThrowHelper.ThrowNotSupportedException(ref state, ex3);
			return false;
		}
	}

	protected internal JsonConverter()
	{
		base.IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;
		base.CanBePolymorphic = base.IsInternalConverter && TypeToConvert == JsonTypeInfo.ObjectType;
		base.IsValueType = TypeToConvert.IsValueType;
		CanBeNull = default(T) == null;
		if (HandleNull)
		{
			HandleNullOnRead = true;
			HandleNullOnWrite = true;
		}
		base.CanUseDirectReadOrWrite = !base.CanBePolymorphic && base.IsInternalConverter && ConverterStrategy == ConverterStrategy.Value;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert == typeof(T);
	}

	internal sealed override JsonPropertyInfo CreateJsonPropertyInfo()
	{
		return new JsonPropertyInfo<T>();
	}

	internal sealed override JsonParameterInfo CreateJsonParameterInfo()
	{
		return new JsonParameterInfo<T>();
	}

	internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		T value2 = (T)value;
		return TryWrite(writer, in value2, options, ref state);
	}

	internal virtual bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		Write(writer, value, options);
		return true;
	}

	internal virtual bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
	{
		value = Read(ref reader, typeToConvert, options);
		return true;
	}

	public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

	internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
	{
		if (ConverterStrategy == ConverterStrategy.Value)
		{
			if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead)
			{
				if (!CanBeNull)
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
				}
				value = default(T);
				return true;
			}
			if (base.IsInternalConverter)
			{
				if (state.Current.NumberHandling.HasValue && IsInternalConverterForNumberType)
				{
					value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
				}
				else
				{
					value = Read(ref reader, typeToConvert, options);
				}
			}
			else
			{
				JsonTokenType tokenType = reader.TokenType;
				int currentDepth = reader.CurrentDepth;
				long bytesConsumed = reader.BytesConsumed;
				if (state.Current.NumberHandling.HasValue && IsInternalConverterForNumberType)
				{
					value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
				}
				else
				{
					value = Read(ref reader, typeToConvert, options);
				}
				VerifyRead(tokenType, currentDepth, bytesConsumed, isValueConverter: true, ref reader);
			}
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && base.CanBePolymorphic)
			{
				T val = value;
				if (val is JsonElement element && JsonSerializer.TryGetReferenceFromJsonElement(ref state, element, out var referenceValue))
				{
					value = (T)referenceValue;
				}
			}
			return true;
		}
		bool isContinuation = state.IsContinuation;
		state.Push();
		bool flag;
		if (base.IsInternalConverter)
		{
			if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead && !isContinuation)
			{
				if (!CanBeNull)
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
				}
				value = default(T);
				flag = true;
			}
			else
			{
				flag = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
			}
		}
		else
		{
			if (!isContinuation)
			{
				if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead)
				{
					if (!CanBeNull)
					{
						ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
					}
					value = default(T);
					state.Pop(success: true);
					return true;
				}
				state.Current.OriginalTokenType = reader.TokenType;
				state.Current.OriginalDepth = reader.CurrentDepth;
			}
			flag = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
			if (flag)
			{
				if (state.IsContinuation)
				{
					ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
				}
				VerifyRead(state.Current.OriginalTokenType, state.Current.OriginalDepth, 0L, isValueConverter: false, ref reader);
			}
		}
		state.Pop(flag);
		return flag;
	}

	internal sealed override bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object value)
	{
		T value2;
		bool result = TryRead(ref reader, TypeToConvert, options, ref state, out value2);
		value = value2;
		return result;
	}

	private static bool IsNull(T value)
	{
		return value == null;
	}

	internal bool TryWrite(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (writer.CurrentDepth >= options.EffectiveMaxDepth)
		{
			ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
		}
		if (default(T) == null && !HandleNullOnWrite && IsNull(value))
		{
			writer.WriteNullValue();
			return true;
		}
		bool flag = false;
		if (!typeof(T).IsValueType && value != null)
		{
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles && ConverterStrategy != ConverterStrategy.Value)
			{
				ReferenceResolver referenceResolver = state.ReferenceResolver;
				if (referenceResolver.ContainsReferenceForCycleDetection(value))
				{
					writer.WriteNullValue();
					return true;
				}
				referenceResolver.PushReferenceForCycleDetection(value);
				flag = true;
			}
			if (base.CanBePolymorphic)
			{
				Type type = value.GetType();
				if (type != TypeToConvert)
				{
					JsonConverter jsonConverter = state.Current.InitializeReEntry(type, options);
					if (jsonConverter.IsValueType)
					{
						switch (options.ReferenceHandlingStrategy)
						{
						case ReferenceHandlingStrategy.Preserve:
							if (jsonConverter.CanHaveIdMetadata && !state.IsContinuation && JsonSerializer.TryWriteReferenceForBoxedStruct(value, ref state, writer))
							{
								return true;
							}
							break;
						case ReferenceHandlingStrategy.IgnoreCycles:
							state.ReferenceResolver.PushReferenceForCycleDetection(value);
							flag = true;
							break;
						}
					}
					bool result = jsonConverter.TryWriteAsObject(writer, value, options, ref state);
					if (flag)
					{
						state.ReferenceResolver.PopReferenceForCycleDetection();
					}
					return result;
				}
			}
		}
		if (ConverterStrategy == ConverterStrategy.Value)
		{
			int currentDepth = writer.CurrentDepth;
			if (state.Current.NumberHandling.HasValue && IsInternalConverterForNumberType)
			{
				WriteNumberWithCustomHandling(writer, value, state.Current.NumberHandling.Value);
			}
			else
			{
				Write(writer, value, options);
			}
			VerifyWrite(currentDepth, writer);
			if (!typeof(T).IsValueType && flag)
			{
				state.ReferenceResolver.PopReferenceForCycleDetection();
			}
			return true;
		}
		bool isContinuation = state.IsContinuation;
		state.Push();
		if (!isContinuation)
		{
			state.Current.OriginalDepth = writer.CurrentDepth;
		}
		bool flag2 = OnTryWrite(writer, value, options, ref state);
		if (flag2)
		{
			VerifyWrite(state.Current.OriginalDepth, writer);
		}
		state.Pop(flag2);
		if (flag)
		{
			state.ReferenceResolver.PopReferenceForCycleDetection();
		}
		return flag2;
	}

	internal bool TryWriteDataExtensionProperty(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (!base.IsInternalConverter)
		{
			return TryWrite(writer, in value, options, ref state);
		}
		JsonDictionaryConverter<T> jsonDictionaryConverter = (this as JsonDictionaryConverter<T>) ?? ((this as JsonMetadataServicesConverter<T>)?.Converter as JsonDictionaryConverter<T>);
		if (jsonDictionaryConverter == null)
		{
			return TryWrite(writer, in value, options, ref state);
		}
		if (writer.CurrentDepth >= options.EffectiveMaxDepth)
		{
			ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
		}
		bool isContinuation = state.IsContinuation;
		state.Push();
		if (!isContinuation)
		{
			state.Current.OriginalDepth = writer.CurrentDepth;
		}
		state.Current.IsWritingExtensionDataProperty = true;
		state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		bool flag = jsonDictionaryConverter.OnWriteResume(writer, value, options, ref state);
		if (flag)
		{
			VerifyWrite(state.Current.OriginalDepth, writer);
		}
		state.Pop(flag);
		return flag;
	}

	internal void VerifyRead(JsonTokenType tokenType, int depth, long bytesConsumed, bool isValueConverter, ref Utf8JsonReader reader)
	{
		switch (tokenType)
		{
		case JsonTokenType.StartArray:
			if (reader.TokenType != JsonTokenType.EndArray)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			else if (depth != reader.CurrentDepth)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			return;
		case JsonTokenType.StartObject:
			if (reader.TokenType != JsonTokenType.EndObject)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			else if (depth != reader.CurrentDepth)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			return;
		}
		if (!isValueConverter)
		{
			if (!HandleNullOnRead || tokenType != JsonTokenType.Null)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
		}
		else if (reader.BytesConsumed != bytesConsumed)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
		}
	}

	internal void VerifyWrite(int originalDepth, Utf8JsonWriter writer)
	{
		if (originalDepth != writer.CurrentDepth)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
		}
	}

	public abstract void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

	public virtual T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (!base.IsInternalConverter && options.TryGetDefaultSimpleConverter(TypeToConvert, out var converter))
		{
			return ((JsonConverter<T>)converter).ReadAsPropertyNameCore(ref reader, TypeToConvert, options);
		}
		ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
		return default(T);
	}

	internal virtual T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		long bytesConsumed = reader.BytesConsumed;
		T result = ReadAsPropertyName(ref reader, typeToConvert, options);
		if (reader.BytesConsumed != bytesConsumed)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
		}
		return result;
	}

	public virtual void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (!base.IsInternalConverter && options.TryGetDefaultSimpleConverter(TypeToConvert, out var converter))
		{
			((JsonConverter<T>)converter).WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
		}
		else
		{
			ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
		}
	}

	internal virtual void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		if (isWritingExtensionDataProperty)
		{
			writer.WritePropertyName((string)(object)value);
			return;
		}
		int currentDepth = writer.CurrentDepth;
		WriteAsPropertyName(writer, value, options);
		if (currentDepth != writer.CurrentDepth || writer.TokenType != JsonTokenType.PropertyName)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
		}
	}

	internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		WriteAsPropertyNameCore(writer, (T)value, options, isWritingExtensionDataProperty);
	}

	internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
	{
		throw new InvalidOperationException();
	}
}
