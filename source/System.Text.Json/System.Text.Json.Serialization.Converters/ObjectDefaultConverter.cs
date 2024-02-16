using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal class ObjectDefaultConverter<T> : JsonObjectConverter<T>
{
	internal override bool CanHaveIdMetadata => true;

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		object obj;
		if (state.UseFastPath)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
			}
			if (jsonTypeInfo.CreateObject == null)
			{
				ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(jsonTypeInfo.Type, ref reader, ref state);
			}
			obj = jsonTypeInfo.CreateObject();
			if (obj is IJsonOnDeserializing jsonOnDeserializing)
			{
				jsonOnDeserializing.OnDeserializing();
			}
			while (true)
			{
				reader.ReadWithVerify();
				JsonTokenType tokenType = reader.TokenType;
				if (tokenType == JsonTokenType.EndObject)
				{
					break;
				}
				ReadOnlySpan<byte> propertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
				bool useExtensionProperty;
				JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(obj, propertyName, ref state, options, out useExtensionProperty);
				ReadPropertyValue(obj, ref state, ref reader, jsonPropertyInfo, useExtensionProperty);
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
			if ((int)state.Current.ObjectState < 14 && options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
			{
				if (!JsonSerializer.ResolveMetadataForJsonObject<T>(ref reader, ref state, options))
				{
					value = default(T);
					return false;
				}
				if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
				{
					value = (T)state.Current.ReturnValue;
					return true;
				}
			}
			if ((int)state.Current.ObjectState < 15)
			{
				if (jsonTypeInfo.CreateObject == null)
				{
					ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(jsonTypeInfo.Type, ref reader, ref state);
				}
				obj = jsonTypeInfo.CreateObject();
				if (obj is IJsonOnDeserializing jsonOnDeserializing2)
				{
					jsonOnDeserializing2.OnDeserializing();
				}
				state.Current.ReturnValue = obj;
				state.Current.ObjectState = StackFrameObjectState.CreatedObject;
			}
			else
			{
				obj = state.Current.ReturnValue;
			}
			while (true)
			{
				if (state.Current.PropertyState == StackFramePropertyState.None)
				{
					state.Current.PropertyState = StackFramePropertyState.ReadName;
					if (!reader.Read())
					{
						state.Current.ReturnValue = obj;
						value = default(T);
						return false;
					}
				}
				JsonPropertyInfo jsonPropertyInfo2;
				if ((int)state.Current.PropertyState < 2)
				{
					state.Current.PropertyState = StackFramePropertyState.Name;
					JsonTokenType tokenType2 = reader.TokenType;
					if (tokenType2 == JsonTokenType.EndObject)
					{
						break;
					}
					ReadOnlySpan<byte> propertyName2 = JsonSerializer.GetPropertyName(ref state, ref reader, options);
					jsonPropertyInfo2 = JsonSerializer.LookupProperty(obj, propertyName2, ref state, options, out var useExtensionProperty2);
					state.Current.UseExtensionProperty = useExtensionProperty2;
				}
				else
				{
					jsonPropertyInfo2 = state.Current.JsonPropertyInfo;
				}
				if ((int)state.Current.PropertyState < 3)
				{
					if (!jsonPropertyInfo2.ShouldDeserialize)
					{
						if (!reader.TrySkip())
						{
							state.Current.ReturnValue = obj;
							value = default(T);
							return false;
						}
						state.Current.EndProperty();
						continue;
					}
					if (!ReadAheadPropertyValue(ref state, ref reader, jsonPropertyInfo2))
					{
						state.Current.ReturnValue = obj;
						value = default(T);
						return false;
					}
				}
				if ((int)state.Current.PropertyState >= 5)
				{
					continue;
				}
				if (!state.Current.UseExtensionProperty)
				{
					if (!jsonPropertyInfo2.ReadJsonAndSetMember(obj, ref state, ref reader))
					{
						state.Current.ReturnValue = obj;
						value = default(T);
						return false;
					}
				}
				else if (!jsonPropertyInfo2.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader))
				{
					state.Current.ReturnValue = obj;
					value = default(T);
					return false;
				}
				state.Current.EndProperty();
			}
		}
		if (obj is IJsonOnDeserialized jsonOnDeserialized)
		{
			jsonOnDeserialized.OnDeserialized();
		}
		value = (T)obj;
		if (state.Current.PropertyRefCache != null)
		{
			jsonTypeInfo.UpdateSortedPropertyCache(ref state.Current);
		}
		return true;
	}

	internal sealed override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		object obj = value;
		if (!state.SupportContinuation)
		{
			writer.WriteStartObject();
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && JsonSerializer.WriteReferenceForObject(this, obj, ref state, writer) == MetadataPropertyName.Ref)
			{
				return true;
			}
			if (obj is IJsonOnSerializing jsonOnSerializing)
			{
				jsonOnSerializing.OnSerializing();
			}
			List<KeyValuePair<string, JsonPropertyInfo>> list = jsonTypeInfo.PropertyCache.List;
			for (int i = 0; i < list.Count; i++)
			{
				JsonPropertyInfo value2 = list[i].Value;
				if (value2.ShouldSerialize)
				{
					state.Current.DeclaredJsonPropertyInfo = value2;
					state.Current.NumberHandling = value2.NumberHandling;
					bool memberAndWriteJson = value2.GetMemberAndWriteJson(obj, ref state, writer);
					state.Current.EndProperty();
				}
			}
			JsonPropertyInfo dataExtensionProperty = jsonTypeInfo.DataExtensionProperty;
			if (dataExtensionProperty != null && dataExtensionProperty.ShouldSerialize)
			{
				state.Current.DeclaredJsonPropertyInfo = dataExtensionProperty;
				state.Current.NumberHandling = dataExtensionProperty.NumberHandling;
				bool memberAndWriteJsonExtensionData = dataExtensionProperty.GetMemberAndWriteJsonExtensionData(obj, ref state, writer);
				state.Current.EndProperty();
			}
			writer.WriteEndObject();
		}
		else
		{
			if (!state.Current.ProcessedStartToken)
			{
				writer.WriteStartObject();
				if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && JsonSerializer.WriteReferenceForObject(this, obj, ref state, writer) == MetadataPropertyName.Ref)
				{
					return true;
				}
				if (obj is IJsonOnSerializing jsonOnSerializing2)
				{
					jsonOnSerializing2.OnSerializing();
				}
				state.Current.ProcessedStartToken = true;
			}
			List<KeyValuePair<string, JsonPropertyInfo>> list2 = jsonTypeInfo.PropertyCache.List;
			while (state.Current.EnumeratorIndex < list2.Count)
			{
				JsonPropertyInfo value3 = list2[state.Current.EnumeratorIndex].Value;
				if (value3.ShouldSerialize)
				{
					state.Current.DeclaredJsonPropertyInfo = value3;
					state.Current.NumberHandling = value3.NumberHandling;
					if (!value3.GetMemberAndWriteJson(obj, ref state, writer))
					{
						return false;
					}
					state.Current.EndProperty();
					state.Current.EnumeratorIndex++;
					if (ShouldFlush(writer, ref state))
					{
						return false;
					}
				}
				else
				{
					state.Current.EnumeratorIndex++;
				}
			}
			if (state.Current.EnumeratorIndex == list2.Count)
			{
				JsonPropertyInfo dataExtensionProperty2 = jsonTypeInfo.DataExtensionProperty;
				if (dataExtensionProperty2 != null && dataExtensionProperty2.ShouldSerialize)
				{
					state.Current.DeclaredJsonPropertyInfo = dataExtensionProperty2;
					state.Current.NumberHandling = dataExtensionProperty2.NumberHandling;
					if (!dataExtensionProperty2.GetMemberAndWriteJsonExtensionData(obj, ref state, writer))
					{
						return false;
					}
					state.Current.EndProperty();
					state.Current.EnumeratorIndex++;
					if (ShouldFlush(writer, ref state))
					{
						return false;
					}
				}
				else
				{
					state.Current.EnumeratorIndex++;
				}
			}
			if (!state.Current.ProcessedEndToken)
			{
				state.Current.ProcessedEndToken = true;
				writer.WriteEndObject();
			}
		}
		if (obj is IJsonOnSerialized jsonOnSerialized)
		{
			jsonOnSerialized.OnSerialized();
		}
		value = (T)obj;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void ReadPropertyValue(object obj, ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo, bool useExtensionProperty)
	{
		if (!jsonPropertyInfo.ShouldDeserialize)
		{
			reader.Skip();
		}
		else
		{
			reader.ReadWithVerify();
			if (!useExtensionProperty)
			{
				jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader);
			}
			else
			{
				jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader);
			}
		}
		state.Current.EndProperty();
	}

	protected static bool ReadAheadPropertyValue(ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo)
	{
		state.Current.PropertyState = StackFramePropertyState.ReadValue;
		if (!state.Current.UseExtensionProperty)
		{
			if (!JsonConverter.SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.ConverterStrategy, ref reader, ref state))
			{
				return false;
			}
		}
		else if (!JsonConverter.SingleValueReadWithReadAhead(ConverterStrategy.Value, ref reader, ref state))
		{
			return false;
		}
		return true;
	}

	internal sealed override void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		if (state.Current.JsonTypeInfo.CreateObject == null)
		{
			ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(state.Current.JsonTypeInfo.Type, ref reader, ref state);
		}
		object obj = state.Current.JsonTypeInfo.CreateObject();
		state.Current.ReturnValue = obj;
		if (obj is IJsonOnDeserializing jsonOnDeserializing)
		{
			jsonOnDeserializing.OnDeserializing();
		}
	}
}
