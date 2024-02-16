using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal abstract class ObjectWithParameterizedConstructorConverter<T> : ObjectDefaultConverter<T>
{
	internal sealed override bool ConstructorIsParameterized => true;

	internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, [MaybeNullWhen(false)] out T value)
	{
		ArgumentState ctorArgumentState = state.Current.CtorArgumentState;
		object obj;
		if (state.UseFastPath)
		{
			ReadOnlySpan<byte> originalSpan = reader.OriginalSpan;
			ReadConstructorArguments(ref state, ref reader, options);
			obj = (T)CreateObject(ref state.Current);
			if (obj is IJsonOnDeserializing jsonOnDeserializing)
			{
				jsonOnDeserializing.OnDeserializing();
			}
			if (ctorArgumentState.FoundPropertyCount > 0)
			{
				(JsonPropertyInfo, JsonReaderState, long, byte[], string)[] foundProperties = ctorArgumentState.FoundProperties;
				for (int i = 0; i < ctorArgumentState.FoundPropertyCount; i++)
				{
					JsonPropertyInfo item = foundProperties[i].Item1;
					long item2 = foundProperties[i].Item3;
					byte[] item3 = foundProperties[i].Item4;
					string item4 = foundProperties[i].Item5;
					Utf8JsonReader reader2 = new Utf8JsonReader(originalSpan.Slice(checked((int)item2)), isFinalBlock: true, foundProperties[i].Item2);
					state.Current.JsonPropertyName = item3;
					state.Current.JsonPropertyInfo = item;
					state.Current.NumberHandling = item.NumberHandling;
					bool flag = item4 != null;
					if (flag)
					{
						state.Current.JsonPropertyNameAsString = item4;
						JsonSerializer.CreateDataExtensionProperty(obj, item, options);
					}
					ObjectDefaultConverter<T>.ReadPropertyValue(obj, ref state, ref reader2, item, flag);
				}
				(JsonPropertyInfo, JsonReaderState, long, byte[], string)[] foundProperties2 = ctorArgumentState.FoundProperties;
				ctorArgumentState.FoundProperties = null;
				ArrayPool<(JsonPropertyInfo, JsonReaderState, long, byte[], string)>.Shared.Return(foundProperties2, clearArray: true);
			}
		}
		else
		{
			if (state.Current.ObjectState == StackFrameObjectState.None)
			{
				state.Current.ObjectState = StackFrameObjectState.StartToken;
				BeginRead(ref state, ref reader, options);
			}
			if (!ReadConstructorArgumentsWithContinuation(ref state, ref reader, options))
			{
				value = default(T);
				return false;
			}
			obj = (T)CreateObject(ref state.Current);
			if (obj is IJsonOnDeserializing jsonOnDeserializing2)
			{
				jsonOnDeserializing2.OnDeserializing();
			}
			if (ctorArgumentState.FoundPropertyCount > 0)
			{
				for (int j = 0; j < ctorArgumentState.FoundPropertyCount; j++)
				{
					JsonPropertyInfo item5 = ctorArgumentState.FoundPropertiesAsync[j].Item1;
					object item6 = ctorArgumentState.FoundPropertiesAsync[j].Item2;
					string item7 = ctorArgumentState.FoundPropertiesAsync[j].Item3;
					if (item7 == null)
					{
						item5.SetExtensionDictionaryAsObject(obj, item6);
						continue;
					}
					JsonSerializer.CreateDataExtensionProperty(obj, item5, options);
					object valueAsObject = item5.GetValueAsObject(obj);
					if (valueAsObject is IDictionary<string, JsonElement> dictionary)
					{
						dictionary[item7] = (JsonElement)item6;
					}
					else
					{
						((IDictionary<string, object>)valueAsObject)[item7] = item6;
					}
				}
				(JsonPropertyInfo, object, string)[] foundPropertiesAsync = ctorArgumentState.FoundPropertiesAsync;
				ctorArgumentState.FoundPropertiesAsync = null;
				ArrayPool<(JsonPropertyInfo, object, string)>.Shared.Return(foundPropertiesAsync, clearArray: true);
			}
		}
		if (obj is IJsonOnDeserialized jsonOnDeserialized)
		{
			jsonOnDeserialized.OnDeserialized();
		}
		EndRead(ref state);
		value = (T)obj;
		if (state.Current.PropertyRefCache != null)
		{
			state.Current.JsonTypeInfo.UpdateSortedPropertyCache(ref state.Current);
		}
		if (ctorArgumentState.ParameterRefCache != null)
		{
			state.Current.JsonTypeInfo.UpdateSortedParameterCache(ref state.Current);
		}
		return true;
	}

	protected abstract void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options);

	protected abstract bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo);

	protected abstract object CreateObject(ref ReadStackFrame frame);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ReadConstructorArguments(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		BeginRead(ref state, ref reader, options);
		while (true)
		{
			reader.ReadWithVerify();
			JsonTokenType tokenType = reader.TokenType;
			if (tokenType == JsonTokenType.EndObject)
			{
				break;
			}
			if (TryLookupConstructorParameter(ref state, ref reader, options, out var jsonParameterInfo))
			{
				reader.ReadWithVerify();
				if (!jsonParameterInfo.ShouldDeserialize)
				{
					reader.Skip();
					state.Current.EndConstructorParameter();
				}
				else
				{
					ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo);
					state.Current.EndConstructorParameter();
				}
				continue;
			}
			ReadOnlySpan<byte> propertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
			bool useExtensionProperty;
			JsonPropertyInfo jsonPropertyInfo = JsonSerializer.LookupProperty(null, propertyName, ref state, options, out useExtensionProperty, createExtensionProperty: false);
			if (jsonPropertyInfo.ShouldDeserialize)
			{
				ArgumentState ctorArgumentState = state.Current.CtorArgumentState;
				if (ctorArgumentState.FoundProperties == null)
				{
					ctorArgumentState.FoundProperties = ArrayPool<(JsonPropertyInfo, JsonReaderState, long, byte[], string)>.Shared.Rent(Math.Max(1, state.Current.JsonTypeInfo.PropertyCache.Count));
				}
				else if (ctorArgumentState.FoundPropertyCount == ctorArgumentState.FoundProperties.Length)
				{
					(JsonPropertyInfo, JsonReaderState, long, byte[], string)[] array = ArrayPool<(JsonPropertyInfo, JsonReaderState, long, byte[], string)>.Shared.Rent(ctorArgumentState.FoundProperties.Length * 2);
					ctorArgumentState.FoundProperties.CopyTo(array, 0);
					(JsonPropertyInfo, JsonReaderState, long, byte[], string)[] foundProperties = ctorArgumentState.FoundProperties;
					ctorArgumentState.FoundProperties = array;
					ArrayPool<(JsonPropertyInfo, JsonReaderState, long, byte[], string)>.Shared.Return(foundProperties, clearArray: true);
				}
				ctorArgumentState.FoundProperties[ctorArgumentState.FoundPropertyCount++] = (jsonPropertyInfo, reader.CurrentState, reader.BytesConsumed, state.Current.JsonPropertyName, state.Current.JsonPropertyNameAsString);
			}
			reader.Skip();
			state.Current.EndProperty();
		}
	}

	private bool ReadConstructorArgumentsWithContinuation(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		while (true)
		{
			if (state.Current.PropertyState == StackFramePropertyState.None)
			{
				state.Current.PropertyState = StackFramePropertyState.ReadName;
				if (!reader.Read())
				{
					return false;
				}
			}
			JsonParameterInfo jsonParameterInfo;
			JsonPropertyInfo jsonPropertyInfo;
			if ((int)state.Current.PropertyState < 2)
			{
				state.Current.PropertyState = StackFramePropertyState.Name;
				JsonTokenType tokenType = reader.TokenType;
				if (tokenType == JsonTokenType.EndObject)
				{
					return true;
				}
				if (TryLookupConstructorParameter(ref state, ref reader, options, out jsonParameterInfo))
				{
					jsonPropertyInfo = null;
				}
				else
				{
					ReadOnlySpan<byte> propertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
					jsonPropertyInfo = JsonSerializer.LookupProperty(null, propertyName, ref state, options, out var useExtensionProperty, createExtensionProperty: false);
					state.Current.UseExtensionProperty = useExtensionProperty;
				}
			}
			else
			{
				jsonParameterInfo = state.Current.CtorArgumentState.JsonParameterInfo;
				jsonPropertyInfo = state.Current.JsonPropertyInfo;
			}
			if (jsonParameterInfo != null)
			{
				if (!HandleConstructorArgumentWithContinuation(ref state, ref reader, jsonParameterInfo))
				{
					return false;
				}
			}
			else if (!HandlePropertyWithContinuation(ref state, ref reader, jsonPropertyInfo))
			{
				break;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HandleConstructorArgumentWithContinuation(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo)
	{
		if ((int)state.Current.PropertyState < 3)
		{
			if (!jsonParameterInfo.ShouldDeserialize)
			{
				if (!reader.TrySkip())
				{
					return false;
				}
				state.Current.EndConstructorParameter();
				return true;
			}
			state.Current.PropertyState = StackFramePropertyState.ReadValue;
			if (!JsonConverter.SingleValueReadWithReadAhead(jsonParameterInfo.ConverterBase.ConverterStrategy, ref reader, ref state))
			{
				return false;
			}
		}
		if (!ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo))
		{
			return false;
		}
		state.Current.EndConstructorParameter();
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HandlePropertyWithContinuation(ref ReadStack state, ref Utf8JsonReader reader, JsonPropertyInfo jsonPropertyInfo)
	{
		if ((int)state.Current.PropertyState < 3)
		{
			if (!jsonPropertyInfo.ShouldDeserialize)
			{
				if (!reader.TrySkip())
				{
					return false;
				}
				state.Current.EndProperty();
				return true;
			}
			if (!ObjectDefaultConverter<T>.ReadAheadPropertyValue(ref state, ref reader, jsonPropertyInfo))
			{
				return false;
			}
		}
		object value;
		if (state.Current.UseExtensionProperty)
		{
			if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out value))
			{
				return false;
			}
		}
		else if (!jsonPropertyInfo.ReadJsonAsObject(ref state, ref reader, out value))
		{
			return false;
		}
		ArgumentState ctorArgumentState = state.Current.CtorArgumentState;
		if (ctorArgumentState.FoundPropertiesAsync == null)
		{
			ctorArgumentState.FoundPropertiesAsync = ArrayPool<(JsonPropertyInfo, object, string)>.Shared.Rent(Math.Max(1, state.Current.JsonTypeInfo.PropertyCache.Count));
		}
		else if (ctorArgumentState.FoundPropertyCount == ctorArgumentState.FoundPropertiesAsync.Length)
		{
			(JsonPropertyInfo, object, string)[] array = ArrayPool<(JsonPropertyInfo, object, string)>.Shared.Rent(ctorArgumentState.FoundPropertiesAsync.Length * 2);
			ctorArgumentState.FoundPropertiesAsync.CopyTo(array, 0);
			(JsonPropertyInfo, object, string)[] foundPropertiesAsync = ctorArgumentState.FoundPropertiesAsync;
			ctorArgumentState.FoundPropertiesAsync = array;
			ArrayPool<(JsonPropertyInfo, object, string)>.Shared.Return(foundPropertiesAsync, clearArray: true);
		}
		ctorArgumentState.FoundPropertiesAsync[ctorArgumentState.FoundPropertyCount++] = (jsonPropertyInfo, value, state.Current.JsonPropertyNameAsString);
		state.Current.EndProperty();
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BeginRead(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
		}
		if (state.Current.JsonTypeInfo.ParameterCount != state.Current.JsonTypeInfo.ParameterCache.Count)
		{
			ThrowHelper.ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(TypeToConvert);
		}
		state.Current.JsonPropertyInfo = null;
		InitializeConstructorArgumentCaches(ref state, options);
	}

	protected virtual void EndRead(ref ReadStack state)
	{
	}

	protected virtual bool TryLookupConstructorParameter(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options, out JsonParameterInfo jsonParameterInfo)
	{
		ReadOnlySpan<byte> propertyName = JsonSerializer.GetPropertyName(ref state, ref reader, options);
		jsonParameterInfo = state.Current.JsonTypeInfo.GetParameter(propertyName, ref state.Current, out var utf8PropertyName);
		state.Current.CtorArgumentState.ParameterIndex++;
		state.Current.JsonPropertyName = utf8PropertyName;
		state.Current.CtorArgumentState.JsonParameterInfo = jsonParameterInfo;
		state.Current.NumberHandling = jsonParameterInfo?.NumberHandling;
		return jsonParameterInfo != null;
	}
}
