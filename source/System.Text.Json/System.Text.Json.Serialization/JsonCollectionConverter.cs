using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

internal abstract class JsonCollectionConverter<TCollection, TElement> : JsonResumableConverter<TCollection>
{
	internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Enumerable;

	internal override Type ElementType => typeof(TElement);

	protected abstract void Add(in TElement value, ref ReadStack state);

	protected abstract void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options);

	protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
	}

	protected static JsonConverter<TElement> GetElementConverter(JsonTypeInfo elementTypeInfo)
	{
		return (JsonConverter<TElement>)elementTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
	}

	protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
	{
		return (JsonConverter<TElement>)state.Current.DeclaredJsonPropertyInfo.ConverterBase;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out TCollection value)
	{
		JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo;
		if (state.UseFastPath)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
			}
			CreateCollection(ref reader, ref state, options);
			JsonConverter<TElement> elementConverter = GetElementConverter(elementTypeInfo);
			if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndArray)
					{
						break;
					}
					TElement value2 = elementConverter.Read(ref reader, elementConverter.TypeToConvert, options);
					Add(in value2, ref state);
				}
			}
			else
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndArray)
					{
						break;
					}
					elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out var value3);
					Add(in value3, ref state);
				}
			}
		}
		else
		{
			bool flag = options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve;
			if (state.Current.ObjectState == StackFrameObjectState.None)
			{
				if (reader.TokenType == JsonTokenType.StartArray)
				{
					state.Current.ObjectState = StackFrameObjectState.PropertyValue;
				}
				else if (flag)
				{
					if (reader.TokenType != JsonTokenType.StartObject)
					{
						ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
					}
					state.Current.ObjectState = StackFrameObjectState.StartToken;
				}
				else
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
				}
			}
			if (flag && (int)state.Current.ObjectState < 14)
			{
				if (!JsonSerializer.ResolveMetadataForJsonArray<TCollection>(ref reader, ref state, options))
				{
					value = default(TCollection);
					return false;
				}
				if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
				{
					value = (TCollection)state.Current.ReturnValue;
					return true;
				}
			}
			if ((int)state.Current.ObjectState < 15)
			{
				CreateCollection(ref reader, ref state, options);
				state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
				state.Current.ObjectState = StackFrameObjectState.CreatedObject;
			}
			if ((int)state.Current.ObjectState < 16)
			{
				JsonConverter<TElement> elementConverter2 = GetElementConverter(elementTypeInfo);
				while (true)
				{
					if ((int)state.Current.PropertyState < 3)
					{
						state.Current.PropertyState = StackFramePropertyState.ReadValue;
						if (!JsonConverter.SingleValueReadWithReadAhead(elementConverter2.ConverterStrategy, ref reader, ref state))
						{
							value = default(TCollection);
							return false;
						}
					}
					if ((int)state.Current.PropertyState < 4)
					{
						if (reader.TokenType == JsonTokenType.EndArray)
						{
							break;
						}
						state.Current.PropertyState = StackFramePropertyState.ReadValueIsEnd;
					}
					if ((int)state.Current.PropertyState < 5)
					{
						if (!elementConverter2.TryRead(ref reader, typeof(TElement), options, ref state, out var value4))
						{
							value = default(TCollection);
							return false;
						}
						Add(in value4, ref state);
						state.Current.EndElement();
					}
				}
				state.Current.ObjectState = StackFrameObjectState.ReadElements;
			}
			if ((int)state.Current.ObjectState < 17)
			{
				state.Current.ObjectState = StackFrameObjectState.EndToken;
				if (state.Current.ValidateEndTokenOnArray && !reader.Read())
				{
					value = default(TCollection);
					return false;
				}
			}
			if ((int)state.Current.ObjectState < 18 && state.Current.ValidateEndTokenOnArray && reader.TokenType != JsonTokenType.EndObject)
			{
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, typeToConvert, in reader);
			}
		}
		ConvertCollection(ref state, options);
		value = (TCollection)state.Current.ReturnValue;
		return true;
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		bool flag;
		if (value == null)
		{
			writer.WriteNullValue();
			flag = true;
		}
		else
		{
			if (!state.Current.ProcessedStartToken)
			{
				state.Current.ProcessedStartToken = true;
				if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
				{
					MetadataPropertyName metadataPropertyName = JsonSerializer.WriteReferenceForCollection(this, value, ref state, writer);
					if (metadataPropertyName == MetadataPropertyName.Ref)
					{
						return true;
					}
					state.Current.MetadataPropertyName = metadataPropertyName;
				}
				else
				{
					writer.WriteStartArray();
				}
				state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
			}
			flag = OnWriteResume(writer, value, options, ref state);
			if (flag && !state.Current.ProcessedEndToken)
			{
				state.Current.ProcessedEndToken = true;
				writer.WriteEndArray();
				if (state.Current.MetadataPropertyName == MetadataPropertyName.Id)
				{
					writer.WriteEndObject();
				}
			}
		}
		return flag;
	}

	protected abstract bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state);

	internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		CreateCollection(ref reader, ref state, options);
	}
}
