using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

internal abstract class JsonDictionaryConverter<TDictionary> : JsonResumableConverter<TDictionary>
{
	internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Dictionary;

	protected internal abstract bool OnWriteResume(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state);
}
internal abstract class JsonDictionaryConverter<TDictionary, TKey, TValue> : JsonDictionaryConverter<TDictionary>
{
	protected JsonConverter<TKey> _keyConverter;

	protected JsonConverter<TValue> _valueConverter;

	internal override Type ElementType => typeof(TValue);

	internal override Type KeyType => typeof(TKey);

	protected abstract void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state);

	protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
	}

	protected virtual void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
	{
	}

	protected static JsonConverter<T> GetConverter<T>(JsonTypeInfo typeInfo)
	{
		return (JsonConverter<T>)typeInfo.PropertyInfoForTypeInfo.ConverterBase;
	}

	internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out TDictionary value)
	{
		JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo;
		if (state.UseFastPath)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
			}
			CreateCollection(ref reader, ref state);
			if (_valueConverter == null)
			{
				_valueConverter = GetConverter<TValue>(elementTypeInfo);
			}
			if (_valueConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					TKey key = ReadDictionaryKey(ref reader, ref state);
					reader.ReadWithVerify();
					TValue value2 = _valueConverter.Read(ref reader, ElementType, options);
					Add(key, in value2, options, ref state);
				}
			}
			else
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					TKey key2 = ReadDictionaryKey(ref reader, ref state);
					reader.ReadWithVerify();
					_valueConverter.TryRead(ref reader, ElementType, options, ref state, out var value3);
					Add(key2, in value3, options, ref state);
				}
			}
		}
		else
		{
			if (state.Current.ObjectState == StackFrameObjectState.None)
			{
				if (reader.TokenType != JsonTokenType.StartObject)
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
				}
				state.Current.ObjectState = StackFrameObjectState.StartToken;
			}
			bool flag = options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve;
			if (flag && (int)state.Current.ObjectState < 14)
			{
				if (!JsonSerializer.ResolveMetadataForJsonObject<TDictionary>(ref reader, ref state, options))
				{
					value = default(TDictionary);
					return false;
				}
				if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
				{
					value = (TDictionary)state.Current.ReturnValue;
					return true;
				}
			}
			if ((int)state.Current.ObjectState < 15)
			{
				CreateCollection(ref reader, ref state);
				state.Current.ObjectState = StackFrameObjectState.CreatedObject;
			}
			if (_valueConverter == null)
			{
				_valueConverter = GetConverter<TValue>(elementTypeInfo);
			}
			while (true)
			{
				if (state.Current.PropertyState == StackFramePropertyState.None)
				{
					state.Current.PropertyState = StackFramePropertyState.ReadName;
					if (!reader.Read())
					{
						value = default(TDictionary);
						return false;
					}
				}
				TKey val;
				if ((int)state.Current.PropertyState < 2)
				{
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					state.Current.PropertyState = StackFramePropertyState.Name;
					if (flag)
					{
						ReadOnlySpan<byte> span = reader.GetSpan();
						if (span.Length > 0 && span[0] == 36)
						{
							ThrowHelper.ThrowUnexpectedMetadataException(span, ref reader, ref state);
						}
					}
					val = ReadDictionaryKey(ref reader, ref state);
				}
				else
				{
					val = (TKey)state.Current.DictionaryKey;
				}
				if ((int)state.Current.PropertyState < 3)
				{
					state.Current.PropertyState = StackFramePropertyState.ReadValue;
					if (!JsonConverter.SingleValueReadWithReadAhead(_valueConverter.ConverterStrategy, ref reader, ref state))
					{
						state.Current.DictionaryKey = val;
						value = default(TDictionary);
						return false;
					}
				}
				if ((int)state.Current.PropertyState < 5)
				{
					if (!_valueConverter.TryRead(ref reader, typeof(TValue), options, ref state, out var value4))
					{
						state.Current.DictionaryKey = val;
						value = default(TDictionary);
						return false;
					}
					Add(val, in value4, options, ref state);
					state.Current.EndElement();
				}
			}
		}
		ConvertCollection(ref state, options);
		value = (TDictionary)state.Current.ReturnValue;
		return true;
		TKey ReadDictionaryKey(ref Utf8JsonReader reader, ref ReadStack state)
		{
			if (_keyConverter == null)
			{
				_keyConverter = GetConverter<TKey>(state.Current.JsonTypeInfo.KeyTypeInfo);
			}
			string @string;
			TKey result;
			if (_keyConverter.IsInternalConverter && KeyType == typeof(string))
			{
				@string = reader.GetString();
				result = (TKey)(object)@string;
			}
			else
			{
				result = _keyConverter.ReadAsPropertyNameCore(ref reader, KeyType, options);
				@string = reader.GetString();
			}
			state.Current.JsonPropertyNameAsString = @string;
			return result;
		}
	}

	internal sealed override bool OnTryWrite(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state)
	{
		if (dictionary == null)
		{
			writer.WriteNullValue();
			return true;
		}
		if (!state.Current.ProcessedStartToken)
		{
			state.Current.ProcessedStartToken = true;
			writer.WriteStartObject();
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && JsonSerializer.WriteReferenceForObject(this, dictionary, ref state, writer) == MetadataPropertyName.Ref)
			{
				return true;
			}
			state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		}
		bool flag = OnWriteResume(writer, dictionary, options, ref state);
		if (flag && !state.Current.ProcessedEndToken)
		{
			state.Current.ProcessedEndToken = true;
			writer.WriteEndObject();
		}
		return flag;
	}

	internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		CreateCollection(ref reader, ref state);
	}
}
