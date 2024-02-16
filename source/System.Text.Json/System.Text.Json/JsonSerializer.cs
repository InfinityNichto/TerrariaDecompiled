using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json;

public static class JsonSerializer
{
	internal static readonly byte[] s_idPropertyName = new byte[3] { 36, 105, 100 };

	internal static readonly byte[] s_refPropertyName = new byte[4] { 36, 114, 101, 102 };

	internal static readonly byte[] s_valuesPropertyName = new byte[7] { 36, 118, 97, 108, 117, 101, 115 };

	internal static readonly JsonEncodedText s_metadataId = JsonEncodedText.Encode("$id");

	internal static readonly JsonEncodedText s_metadataRef = JsonEncodedText.Encode("$ref");

	internal static readonly JsonEncodedText s_metadataValues = JsonEncodedText.Encode("$values");

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(this JsonDocument document, JsonSerializerOptions? options = null)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadDocument<TValue>(document, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerOptions? options = null)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadDocument<object>(document, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonDocument document, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadDocument<TValue>(document, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerContext context)
	{
		if (document == null)
		{
			throw new ArgumentNullException("document");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadDocument<object>(document, typeInfo);
	}

	private static TValue ReadDocument<TValue>(JsonDocument document, JsonTypeInfo jsonTypeInfo)
	{
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpan<TValue>(span, jsonTypeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(this JsonElement element, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadUsingMetadata<TValue>(element, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(this JsonElement element, Type returnType, JsonSerializerOptions? options = null)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadUsingMetadata<object>(element, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonElement element, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadUsingMetadata<TValue>(element, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonElement element, Type returnType, JsonSerializerContext context)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadUsingMetadata<object>(element, typeInfo);
	}

	private static TValue ReadUsingMetadata<TValue>(JsonElement element, JsonTypeInfo jsonTypeInfo)
	{
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpan<TValue>(span, jsonTypeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(this JsonNode? node, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadNode<TValue>(node, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(this JsonNode? node, Type returnType, JsonSerializerOptions? options = null)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadNode<object>(node, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonNode? node, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadNode<TValue>(node, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonNode? node, Type returnType, JsonSerializerContext context)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadNode<object>(node, typeInfo);
	}

	private static TValue ReadNode<TValue>(JsonNode node, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			if (node == null)
			{
				utf8JsonWriter.WriteNullValue();
			}
			else
			{
				node.WriteTo(utf8JsonWriter, options);
			}
		}
		return ReadFromSpan<TValue>(pooledByteBufferWriter.WrittenMemory.Span, jsonTypeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteDocumentUsingSerializer(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteDocumentUsingSerializer(in value, typeInfo);
	}

	public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return WriteDocumentUsingGeneratedSerializer(in value, jsonTypeInfo);
	}

	public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		return WriteDocumentUsingGeneratedSerializer(in value, GetTypeInfo(context, runtimeTypeAndValidateInputType));
	}

	private static JsonDocument WriteDocumentUsingGeneratedSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonDocument.ParseRented(pooledByteBufferWriter, options.GetDocumentOptions());
	}

	private static JsonDocument WriteDocumentUsingSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonDocument.ParseRented(pooledByteBufferWriter, options.GetDocumentOptions());
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonElement SerializeToElement<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteElementUsingSerializer(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteElementUsingSerializer(in value, typeInfo);
	}

	public static JsonElement SerializeToElement<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return WriteElementUsingGeneratedSerializer(in value, jsonTypeInfo);
	}

	public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, runtimeTypeAndValidateInputType);
		return WriteElementUsingGeneratedSerializer(in value, typeInfo);
	}

	private static JsonElement WriteElementUsingGeneratedSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonElement.ParseValue(pooledByteBufferWriter.WrittenMemory.Span, options.GetDocumentOptions());
	}

	private static JsonElement WriteElementUsingSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonElement.ParseValue(pooledByteBufferWriter.WrittenMemory.Span, options.GetDocumentOptions());
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonNode? SerializeToNode<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteNodeUsingSerializer(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static JsonNode? SerializeToNode(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteNodeUsingSerializer(in value, typeInfo);
	}

	public static JsonNode? SerializeToNode<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return WriteNodeUsingGeneratedSerializer(in value, jsonTypeInfo);
	}

	public static JsonNode? SerializeToNode(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, runtimeTypeAndValidateInputType);
		return WriteNodeUsingGeneratedSerializer(in value, typeInfo);
	}

	private static JsonNode WriteNodeUsingGeneratedSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonNode.Parse(pooledByteBufferWriter.WrittenMemory.Span, options.GetNodeOptions());
	}

	private static JsonNode WriteNodeUsingSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonNode.Parse(pooledByteBufferWriter.WrittenMemory.Span, options.GetNodeOptions());
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static JsonTypeInfo GetTypeInfo(JsonSerializerOptions options, Type runtimeType)
	{
		if (options == null)
		{
			options = JsonSerializerOptions.s_defaultOptions;
		}
		if (!options.IsInitializedForReflectionSerializer)
		{
			options.InitializeForReflectionSerializer();
		}
		return options.GetOrAddClassForRootType(runtimeType);
	}

	private static JsonTypeInfo GetTypeInfo(JsonSerializerContext context, Type type)
	{
		JsonTypeInfo typeInfo = context.GetTypeInfo(type);
		if (typeInfo == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NoMetadataForType(type);
		}
		return typeInfo;
	}

	internal static bool IsValidNumberHandlingValue(JsonNumberHandling handling)
	{
		return JsonHelpers.IsInRangeInclusive((int)handling, 0, 7);
	}

	internal static bool ResolveMetadataForJsonObject<T>(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		JsonConverter converterBase = state.Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
		if ((int)state.Current.ObjectState < 2 && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadNameOrEndObject))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadNameOrEndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				state.Current.ObjectState = StackFrameObjectState.PropertyValue;
				state.Current.PropertyState = StackFramePropertyState.ReadName;
				return true;
			}
			ReadOnlySpan<byte> span = reader.GetSpan();
			switch (GetMetadataPropertyName(span))
			{
			case MetadataPropertyName.Id:
				state.Current.JsonPropertyName = s_idPropertyName;
				if (!converterBase.CanHaveIdMetadata)
				{
					ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converterBase.TypeToConvert);
				}
				state.Current.ObjectState = StackFrameObjectState.ReadAheadIdValue;
				break;
			case MetadataPropertyName.Ref:
				state.Current.JsonPropertyName = s_refPropertyName;
				if (converterBase.IsValueType)
				{
					ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converterBase.TypeToConvert);
				}
				state.Current.ObjectState = StackFrameObjectState.ReadAheadRefValue;
				break;
			case MetadataPropertyName.Values:
				ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(span, ref state, in reader);
				break;
			default:
				state.Current.ObjectState = StackFrameObjectState.PropertyValue;
				state.Current.PropertyState = StackFramePropertyState.ReadName;
				return true;
			}
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefValue)
		{
			if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefValue))
			{
				return false;
			}
		}
		else if (state.Current.ObjectState == StackFrameObjectState.ReadAheadIdValue && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadIdValue))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadRefValue)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
			}
			string @string = reader.GetString();
			object obj = state.ReferenceResolver.ResolveReference(@string);
			ValidateValueIsCorrectType<T>(obj, @string);
			state.Current.ReturnValue = obj;
			state.Current.ObjectState = StackFrameObjectState.ReadAheadRefEndObject;
		}
		else if (state.Current.ObjectState == StackFrameObjectState.ReadIdValue)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
			}
			converterBase.CreateInstanceForReferenceResolver(ref reader, ref state, options);
			string string2 = reader.GetString();
			state.ReferenceResolver.AddReference(string2, state.Current.ReturnValue);
			state.Current.ObjectState = StackFrameObjectState.CreatedObject;
		}
		state.Current.JsonPropertyName = null;
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefEndObject && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefEndObject))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject && reader.TokenType != JsonTokenType.EndObject)
		{
			ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
		}
		return true;
	}

	internal static bool ResolveMetadataForJsonArray<T>(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		JsonConverter converterBase = state.Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
		if ((int)state.Current.ObjectState < 2 && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadNameOrEndObject))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadNameOrEndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converterBase.TypeToConvert);
			}
			ReadOnlySpan<byte> span = reader.GetSpan();
			switch (GetMetadataPropertyName(span))
			{
			case MetadataPropertyName.Id:
				state.Current.JsonPropertyName = s_idPropertyName;
				if (!converterBase.CanHaveIdMetadata)
				{
					ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converterBase.TypeToConvert);
				}
				state.Current.ObjectState = StackFrameObjectState.ReadAheadIdValue;
				break;
			case MetadataPropertyName.Ref:
				state.Current.JsonPropertyName = s_refPropertyName;
				if (converterBase.IsValueType)
				{
					ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converterBase.TypeToConvert);
				}
				state.Current.ObjectState = StackFrameObjectState.ReadAheadRefValue;
				break;
			case MetadataPropertyName.Values:
				ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues(ref state, span);
				break;
			default:
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, converterBase.TypeToConvert, in reader);
				break;
			}
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefValue)
		{
			if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefValue))
			{
				return false;
			}
		}
		else if (state.Current.ObjectState == StackFrameObjectState.ReadAheadIdValue && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadIdValue))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadRefValue)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
			}
			string @string = reader.GetString();
			object obj = state.ReferenceResolver.ResolveReference(@string);
			ValidateValueIsCorrectType<T>(obj, @string);
			state.Current.ReturnValue = obj;
			state.Current.ObjectState = StackFrameObjectState.ReadAheadRefEndObject;
		}
		else if (state.Current.ObjectState == StackFrameObjectState.ReadIdValue)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
			}
			converterBase.CreateInstanceForReferenceResolver(ref reader, ref state, options);
			string string2 = reader.GetString();
			state.ReferenceResolver.AddReference(string2, state.Current.ReturnValue);
			state.Current.ObjectState = StackFrameObjectState.ReadAheadValuesName;
		}
		state.Current.JsonPropertyName = null;
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefEndObject && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefEndObject))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
		{
			if (reader.TokenType != JsonTokenType.EndObject)
			{
				ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
			}
			return true;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadValuesName && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadValuesName))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadValuesName)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converterBase.TypeToConvert);
			}
			ReadOnlySpan<byte> span2 = reader.GetSpan();
			if (GetMetadataPropertyName(span2) != MetadataPropertyName.Values)
			{
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, converterBase.TypeToConvert, in reader);
			}
			state.Current.JsonPropertyName = s_valuesPropertyName;
			state.Current.ObjectState = StackFrameObjectState.ReadAheadValuesStartArray;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadAheadValuesStartArray && !TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadValuesStartArray))
		{
			return false;
		}
		if (state.Current.ObjectState == StackFrameObjectState.ReadValuesStartArray)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				ThrowHelper.ThrowJsonException_MetadataValuesInvalidToken(reader.TokenType);
			}
			state.Current.ValidateEndTokenOnArray = true;
			state.Current.ObjectState = StackFrameObjectState.CreatedObject;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryReadAheadMetadataAndSetState(ref Utf8JsonReader reader, ref ReadStack state, StackFrameObjectState nextState)
	{
		state.Current.ObjectState = nextState;
		return reader.Read();
	}

	internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName)
	{
		if (propertyName.Length > 0 && propertyName[0] == 36)
		{
			switch (propertyName.Length)
			{
			case 3:
				if (propertyName[1] == 105 && propertyName[2] == 100)
				{
					return MetadataPropertyName.Id;
				}
				break;
			case 4:
				if (propertyName[1] == 114 && propertyName[2] == 101 && propertyName[3] == 102)
				{
					return MetadataPropertyName.Ref;
				}
				break;
			case 7:
				if (propertyName[1] == 118 && propertyName[2] == 97 && propertyName[3] == 108 && propertyName[4] == 117 && propertyName[5] == 101 && propertyName[6] == 115)
				{
					return MetadataPropertyName.Values;
				}
				break;
			}
		}
		return MetadataPropertyName.NoMetadata;
	}

	internal static bool TryGetReferenceFromJsonElement(ref ReadStack state, JsonElement element, out object referenceValue)
	{
		bool flag = false;
		referenceValue = null;
		if (element.ValueKind == JsonValueKind.Object)
		{
			int num = 0;
			foreach (JsonProperty item in element.EnumerateObject())
			{
				num++;
				if (flag)
				{
					ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
				}
				else if (item.EscapedNameEquals(s_refPropertyName))
				{
					if (num > 1)
					{
						ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
					}
					if (item.Value.ValueKind != JsonValueKind.String)
					{
						ThrowHelper.ThrowJsonException_MetadataValueWasNotString(item.Value.ValueKind);
					}
					referenceValue = state.ReferenceResolver.ResolveReference(item.Value.GetString());
					flag = true;
				}
			}
		}
		return flag;
	}

	private static void ValidateValueIsCorrectType<T>(object value, string referenceId)
	{
		try
		{
			T val = (T)value;
		}
		catch (InvalidCastException)
		{
			ThrowHelper.ThrowInvalidOperationException_MetadataReferenceOfTypeCannotBeAssignedToType(referenceId, value.GetType(), typeof(T));
			throw;
		}
	}

	internal static JsonPropertyInfo LookupProperty(object obj, ReadOnlySpan<byte> unescapedPropertyName, ref ReadStack state, JsonSerializerOptions options, out bool useExtensionProperty, bool createExtensionProperty = true)
	{
		useExtensionProperty = false;
		byte[] utf8PropertyName;
		JsonPropertyInfo jsonPropertyInfo = state.Current.JsonTypeInfo.GetProperty(unescapedPropertyName, ref state.Current, out utf8PropertyName);
		state.Current.PropertyIndex++;
		state.Current.JsonPropertyName = utf8PropertyName;
		if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
		{
			JsonPropertyInfo dataExtensionProperty = state.Current.JsonTypeInfo.DataExtensionProperty;
			if (dataExtensionProperty != null && dataExtensionProperty.HasGetter && dataExtensionProperty.HasSetter)
			{
				state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
				if (createExtensionProperty)
				{
					CreateDataExtensionProperty(obj, dataExtensionProperty, options);
				}
				jsonPropertyInfo = dataExtensionProperty;
				useExtensionProperty = true;
			}
		}
		state.Current.JsonPropertyInfo = jsonPropertyInfo;
		state.Current.NumberHandling = jsonPropertyInfo.NumberHandling;
		return jsonPropertyInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ReadOnlySpan<byte> GetPropertyName(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		ReadOnlySpan<byte> span = reader.GetSpan();
		ReadOnlySpan<byte> result;
		if (reader._stringHasEscaping)
		{
			int idx = span.IndexOf<byte>(92);
			result = JsonReaderHelper.GetUnescapedSpan(span, idx);
		}
		else
		{
			result = span;
		}
		if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && span.Length > 0 && span[0] == 36)
		{
			ThrowHelper.ThrowUnexpectedMetadataException(span, ref reader, ref state);
		}
		return result;
	}

	internal static void CreateDataExtensionProperty(object obj, JsonPropertyInfo jsonPropertyInfo, JsonSerializerOptions options)
	{
		object obj2 = jsonPropertyInfo.GetValueAsObject(obj);
		if (obj2 != null)
		{
			return;
		}
		if (jsonPropertyInfo.RuntimeTypeInfo.CreateObject == null)
		{
			if (jsonPropertyInfo.DeclaredPropertyType.FullName == "System.Text.Json.Nodes.JsonObject")
			{
				obj2 = jsonPropertyInfo.ConverterBase.CreateObject(options);
			}
			else
			{
				ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(jsonPropertyInfo.DeclaredPropertyType);
			}
		}
		else
		{
			obj2 = jsonPropertyInfo.RuntimeTypeInfo.CreateObject();
		}
		jsonPropertyInfo.SetExtensionDictionaryAsObject(obj, obj2);
	}

	private static TValue ReadCore<TValue>(JsonConverter jsonConverter, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		if (jsonConverter is JsonConverter<TValue> jsonConverter2)
		{
			return jsonConverter2.ReadCore(ref reader, options, ref state);
		}
		object obj = jsonConverter.ReadCoreAsObject(ref reader, options, ref state);
		return (TValue)obj;
	}

	private static TValue ReadFromSpan<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo, int? actualByteCount = null)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		JsonReaderState state = new JsonReaderState(options.GetReaderOptions());
		Utf8JsonReader reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state);
		ReadStack state2 = default(ReadStack);
		state2.Initialize(jsonTypeInfo);
		JsonConverter converterBase = jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
		if (converterBase is JsonConverter<TValue> jsonConverter)
		{
			return jsonConverter.ReadCore(ref reader, options, ref state2);
		}
		object obj = converterBase.ReadCoreAsObject(ref reader, options, ref state2);
		return (TValue)obj;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadFromSpan<TValue>(utf8Json, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpan<object>(utf8Json, typeInfo);
	}

	public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadFromSpan<TValue>(utf8Json, jsonTypeInfo);
	}

	public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerContext context)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		return ReadFromSpan<object>(utf8Json, GetTypeInfo(context, returnType));
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadAllAsync<TValue>(utf8Json, typeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(Stream utf8Json, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		return ReadAllUsingOptions<TValue>(utf8Json, typeof(TValue), options);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static ValueTask<object?> DeserializeAsync(Stream utf8Json, Type returnType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadAllAsync<object>(utf8Json, typeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(Stream utf8Json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		return ReadAllUsingOptions<object>(utf8Json, returnType, options);
	}

	public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Json, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadAllAsync<TValue>(utf8Json, jsonTypeInfo, cancellationToken);
	}

	public static TValue? Deserialize<TValue>(Stream utf8Json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadAll<TValue>(utf8Json, jsonTypeInfo);
	}

	public static ValueTask<object?> DeserializeAsync(Stream utf8Json, Type returnType, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		return ReadAllAsync<object>(utf8Json, GetTypeInfo(context, returnType), cancellationToken);
	}

	public static object? Deserialize(Stream utf8Json, Type returnType, JsonSerializerContext context)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		return ReadAll<object>(utf8Json, GetTypeInfo(context, returnType));
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (options == null)
		{
			options = JsonSerializerOptions.s_defaultOptions;
		}
		if (!options.IsInitializedForReflectionSerializer)
		{
			options.InitializeForReflectionSerializer();
		}
		return CreateAsyncEnumerableDeserializer(utf8Json, options, cancellationToken);
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
		static async IAsyncEnumerable<TValue> CreateAsyncEnumerableDeserializer(Stream utf8Json, JsonSerializerOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
			JsonConverter converter = QueueOfTConverter<Queue<TValue>, TValue>.Instance;
			JsonTypeInfo jsonTypeInfo = CreateQueueJsonTypeInfo<TValue>(converter, options);
			ReadStack readStack = default(ReadStack);
			readStack.Initialize(jsonTypeInfo, supportContinuation: true);
			JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
			try
			{
				do
				{
					bufferState = await ReadFromStreamAsync(utf8Json, bufferState, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					ContinueDeserialize<Queue<TValue>>(ref bufferState, ref jsonReaderState, ref readStack, converter, options);
					object returnValue = readStack.Current.ReturnValue;
					if (returnValue is Queue<TValue> queue)
					{
						while (queue.Count > 0)
						{
							yield return queue.Dequeue();
						}
					}
				}
				while (!bufferState.IsFinalBlock);
			}
			finally
			{
				bufferState.Dispose();
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Workaround for https://github.com/mono/linker/issues/1416. All usages are marked as unsafe.")]
	private static JsonTypeInfo CreateQueueJsonTypeInfo<TValue>(JsonConverter queueConverter, JsonSerializerOptions queueOptions)
	{
		return new JsonTypeInfo(typeof(Queue<TValue>), queueConverter, typeof(Queue<TValue>), queueOptions);
	}

	internal static async ValueTask<TValue> ReadAllAsync<TValue>(Stream utf8Json, JsonTypeInfo jsonTypeInfo, CancellationToken cancellationToken)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
		ReadStack readStack = default(ReadStack);
		readStack.Initialize(jsonTypeInfo, supportContinuation: true);
		JsonConverter converter = readStack.Current.JsonPropertyInfo.ConverterBase;
		JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
		try
		{
			TValue result;
			do
			{
				bufferState = await ReadFromStreamAsync(utf8Json, bufferState, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				result = ContinueDeserialize<TValue>(ref bufferState, ref jsonReaderState, ref readStack, converter, options);
			}
			while (!bufferState.IsFinalBlock);
			return result;
		}
		finally
		{
			bufferState.Dispose();
		}
	}

	internal static TValue ReadAll<TValue>(Stream utf8Json, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
		ReadStack readStack = default(ReadStack);
		readStack.Initialize(jsonTypeInfo, supportContinuation: true);
		JsonConverter converterBase = readStack.Current.JsonPropertyInfo.ConverterBase;
		JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
		try
		{
			TValue result;
			do
			{
				bufferState = ReadFromStream(utf8Json, bufferState);
				result = ContinueDeserialize<TValue>(ref bufferState, ref jsonReaderState, ref readStack, converterBase, options);
			}
			while (!bufferState.IsFinalBlock);
			return result;
		}
		finally
		{
			bufferState.Dispose();
		}
	}

	internal static async ValueTask<ReadBufferState> ReadFromStreamAsync(Stream utf8Json, ReadBufferState bufferState, CancellationToken cancellationToken)
	{
		do
		{
			int num = await utf8Json.ReadAsync(bufferState.Buffer.AsMemory(bufferState.BytesInBuffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				bufferState.IsFinalBlock = true;
				break;
			}
			bufferState.BytesInBuffer += num;
		}
		while (bufferState.BytesInBuffer != bufferState.Buffer.Length);
		return bufferState;
	}

	internal static ReadBufferState ReadFromStream(Stream utf8Json, ReadBufferState bufferState)
	{
		do
		{
			int num = utf8Json.Read(bufferState.Buffer.AsSpan(bufferState.BytesInBuffer));
			if (num == 0)
			{
				bufferState.IsFinalBlock = true;
				break;
			}
			bufferState.BytesInBuffer += num;
		}
		while (bufferState.BytesInBuffer != bufferState.Buffer.Length);
		return bufferState;
	}

	internal static TValue ContinueDeserialize<TValue>(ref ReadBufferState bufferState, ref JsonReaderState jsonReaderState, ref ReadStack readStack, JsonConverter converter, JsonSerializerOptions options)
	{
		if (bufferState.BytesInBuffer > bufferState.ClearMax)
		{
			bufferState.ClearMax = bufferState.BytesInBuffer;
		}
		int num = 0;
		if (bufferState.IsFirstIteration)
		{
			bufferState.IsFirstIteration = false;
			if (bufferState.Buffer.AsSpan().StartsWith(JsonConstants.Utf8Bom))
			{
				num += JsonConstants.Utf8Bom.Length;
				bufferState.BytesInBuffer -= JsonConstants.Utf8Bom.Length;
			}
		}
		TValue result = ReadCore<TValue>(ref jsonReaderState, bufferState.IsFinalBlock, new ReadOnlySpan<byte>(bufferState.Buffer, num, bufferState.BytesInBuffer), options, ref readStack, converter);
		int num2 = checked((int)readStack.BytesConsumed);
		bufferState.BytesInBuffer -= num2;
		if (!bufferState.IsFinalBlock)
		{
			if ((uint)bufferState.BytesInBuffer > (uint)bufferState.Buffer.Length / 2u)
			{
				byte[] buffer = bufferState.Buffer;
				int clearMax = bufferState.ClearMax;
				byte[] array = ArrayPool<byte>.Shared.Rent((bufferState.Buffer.Length < 1073741823) ? (bufferState.Buffer.Length * 2) : int.MaxValue);
				Buffer.BlockCopy(buffer, num2 + num, array, 0, bufferState.BytesInBuffer);
				bufferState.Buffer = array;
				bufferState.ClearMax = bufferState.BytesInBuffer;
				new Span<byte>(buffer, 0, clearMax).Clear();
				ArrayPool<byte>.Shared.Return(buffer);
			}
			else if (bufferState.BytesInBuffer != 0)
			{
				Buffer.BlockCopy(bufferState.Buffer, num2 + num, bufferState.Buffer, 0, bufferState.BytesInBuffer);
			}
		}
		return result;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	private static TValue ReadAllUsingOptions<TValue>(Stream utf8Json, Type returnType, JsonSerializerOptions options)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadAll<TValue>(utf8Json, typeInfo);
	}

	private static TValue ReadCore<TValue>(ref JsonReaderState readerState, bool isFinalBlock, ReadOnlySpan<byte> buffer, JsonSerializerOptions options, ref ReadStack state, JsonConverter converterBase)
	{
		Utf8JsonReader reader = new Utf8JsonReader(buffer, isFinalBlock, readerState);
		state.ReadAhead = !isFinalBlock;
		state.BytesConsumed = 0L;
		TValue result = ReadCore<TValue>(converterBase, ref reader, options, ref state);
		readerState = reader.CurrentState;
		return result;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(string json, JsonSerializerOptions? options = null)
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadFromSpan<TValue>(json.AsSpan(), typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return ReadFromSpan<TValue>(json, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(string json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpan<object>(json.AsSpan(), typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(ReadOnlySpan<char> json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpan<object>(json, typeInfo);
	}

	public static TValue? Deserialize<TValue>(string json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadFromSpan<TValue>(json.AsSpan(), jsonTypeInfo);
	}

	public static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return ReadFromSpan<TValue>(json, jsonTypeInfo);
	}

	public static object? Deserialize(string json, Type returnType, JsonSerializerContext context)
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadFromSpan<object>(json.AsSpan(), typeInfo);
	}

	public static object? Deserialize(ReadOnlySpan<char> json, Type returnType, JsonSerializerContext context)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadFromSpan<object>(json, typeInfo);
	}

	private static TValue ReadFromSpan<TValue>(ReadOnlySpan<char> json, JsonTypeInfo jsonTypeInfo)
	{
		byte[] array = null;
		Span<byte> span = (((long)json.Length > 349525L) ? new byte[JsonReaderHelper.GetUtf8ByteCount(json)] : (array = ArrayPool<byte>.Shared.Rent(json.Length * 3)));
		try
		{
			int utf8FromText = JsonReaderHelper.GetUtf8FromText(json, span);
			span = span.Slice(0, utf8FromText);
			return ReadFromSpan<TValue>(span, jsonTypeInfo, utf8FromText);
		}
		finally
		{
			if (array != null)
			{
				span.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(options, typeof(TValue));
		return Read<TValue>(ref reader, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return Read<object>(ref reader, typeInfo);
	}

	public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return Read<TValue>(ref reader, jsonTypeInfo);
	}

	public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerContext context)
	{
		if (returnType == null)
		{
			throw new ArgumentNullException("returnType");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		return Read<object>(ref reader, GetTypeInfo(context, returnType));
	}

	private static TValue Read<TValue>(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo)
	{
		ReadStack state = default(ReadStack);
		state.Initialize(jsonTypeInfo);
		JsonReaderState currentState = reader.CurrentState;
		if (currentState.Options.CommentHandling == JsonCommentHandling.Allow)
		{
			throw new ArgumentException(System.SR.JsonSerializerDoesNotSupportComments, "reader");
		}
		Utf8JsonReader utf8JsonReader = reader;
		ReadOnlySpan<byte> readOnlySpan = default(ReadOnlySpan<byte>);
		ReadOnlySequence<byte> source = default(ReadOnlySequence<byte>);
		try
		{
			JsonTokenType tokenType = reader.TokenType;
			ReadOnlySpan<byte> bytes;
			if ((tokenType == JsonTokenType.None || tokenType == JsonTokenType.PropertyName) && !reader.Read())
			{
				bytes = default(ReadOnlySpan<byte>);
				ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.ExpectedOneCompleteToken, 0, bytes);
			}
			switch (reader.TokenType)
			{
			case JsonTokenType.StartObject:
			case JsonTokenType.StartArray:
			{
				long tokenStartIndex = reader.TokenStartIndex;
				if (!reader.TrySkip())
				{
					bytes = default(ReadOnlySpan<byte>);
					ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.NotEnoughData, 0, bytes);
				}
				long num = reader.BytesConsumed - tokenStartIndex;
				ReadOnlySequence<byte> originalSequence = reader.OriginalSequence;
				if (originalSequence.IsEmpty)
				{
					bytes = reader.OriginalSpan;
					readOnlySpan = checked(bytes.Slice((int)tokenStartIndex, (int)num));
				}
				else
				{
					source = originalSequence.Slice(tokenStartIndex, num);
				}
				break;
			}
			case JsonTokenType.Number:
			case JsonTokenType.True:
			case JsonTokenType.False:
			case JsonTokenType.Null:
				if (reader.HasValueSequence)
				{
					source = reader.ValueSequence;
				}
				else
				{
					readOnlySpan = reader.ValueSpan;
				}
				break;
			case JsonTokenType.String:
			{
				ReadOnlySequence<byte> originalSequence2 = reader.OriginalSequence;
				if (originalSequence2.IsEmpty)
				{
					bytes = reader.ValueSpan;
					int length = bytes.Length + 2;
					readOnlySpan = reader.OriginalSpan.Slice((int)reader.TokenStartIndex, length);
					break;
				}
				long num2 = 2L;
				if (reader.HasValueSequence)
				{
					num2 += reader.ValueSequence.Length;
				}
				else
				{
					long num3 = num2;
					bytes = reader.ValueSpan;
					num2 = num3 + bytes.Length;
				}
				source = originalSequence2.Slice(reader.TokenStartIndex, num2);
				break;
			}
			default:
			{
				byte b;
				if (reader.HasValueSequence)
				{
					bytes = reader.ValueSequence.First.Span;
					b = bytes[0];
				}
				else
				{
					bytes = reader.ValueSpan;
					b = bytes[0];
				}
				byte nextByte = b;
				bytes = default(ReadOnlySpan<byte>);
				ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.ExpectedStartOfValueNotFound, nextByte, bytes);
				break;
			}
			}
		}
		catch (JsonReaderException ex)
		{
			reader = utf8JsonReader;
			ThrowHelper.ReThrowWithPath(ref state, ex);
		}
		int num4 = (readOnlySpan.IsEmpty ? checked((int)source.Length) : readOnlySpan.Length);
		byte[] array = ArrayPool<byte>.Shared.Rent(num4);
		Span<byte> span = array.AsSpan(0, num4);
		try
		{
			if (readOnlySpan.IsEmpty)
			{
				source.CopyTo(span);
			}
			else
			{
				readOnlySpan.CopyTo(span);
			}
			JsonReaderOptions options = currentState.Options;
			Utf8JsonReader reader2 = new Utf8JsonReader(span, options);
			JsonConverter converterBase = state.Current.JsonPropertyInfo.ConverterBase;
			return ReadCore<TValue>(converterBase, ref reader2, jsonTypeInfo.Options, ref state);
		}
		catch (JsonException)
		{
			reader = utf8JsonReader;
			throw;
		}
		finally
		{
			span.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteBytesUsingSerializer(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteBytesUsingSerializer(in value, typeInfo);
	}

	public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return WriteBytesUsingGeneratedSerializer(in value, jsonTypeInfo);
	}

	public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, runtimeTypeAndValidateInputType);
		return WriteBytesUsingGeneratedSerializer(in value, typeInfo);
	}

	private static byte[] WriteBytesUsingGeneratedSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
		}
		return pooledByteBufferWriter.WrittenMemory.ToArray();
	}

	private static byte[] WriteBytesUsingSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingSerializer(writer, in value, jsonTypeInfo);
		}
		return pooledByteBufferWriter.WrittenMemory.ToArray();
	}

	internal static MetadataPropertyName WriteReferenceForObject(JsonConverter jsonConverter, object currentValue, ref WriteStack state, Utf8JsonWriter writer)
	{
		MetadataPropertyName result;
		if (state.BoxedStructReferenceId != null)
		{
			writer.WriteString(s_metadataId, state.BoxedStructReferenceId);
			result = MetadataPropertyName.Id;
			state.BoxedStructReferenceId = null;
		}
		else if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
		{
			result = MetadataPropertyName.NoMetadata;
		}
		else
		{
			bool alreadyExists;
			string reference = state.ReferenceResolver.GetReference(currentValue, out alreadyExists);
			if (alreadyExists)
			{
				writer.WriteString(s_metadataRef, reference);
				writer.WriteEndObject();
				result = MetadataPropertyName.Ref;
			}
			else
			{
				writer.WriteString(s_metadataId, reference);
				result = MetadataPropertyName.Id;
			}
		}
		return result;
	}

	internal static MetadataPropertyName WriteReferenceForCollection(JsonConverter jsonConverter, object currentValue, ref WriteStack state, Utf8JsonWriter writer)
	{
		MetadataPropertyName result;
		if (state.BoxedStructReferenceId != null)
		{
			writer.WriteStartObject();
			writer.WriteString(s_metadataId, state.BoxedStructReferenceId);
			writer.WriteStartArray(s_metadataValues);
			result = MetadataPropertyName.Id;
			state.BoxedStructReferenceId = null;
		}
		else if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
		{
			writer.WriteStartArray();
			result = MetadataPropertyName.NoMetadata;
		}
		else
		{
			bool alreadyExists;
			string reference = state.ReferenceResolver.GetReference(currentValue, out alreadyExists);
			if (alreadyExists)
			{
				writer.WriteStartObject();
				writer.WriteString(s_metadataRef, reference);
				writer.WriteEndObject();
				result = MetadataPropertyName.Ref;
			}
			else
			{
				writer.WriteStartObject();
				writer.WriteString(s_metadataId, reference);
				writer.WriteStartArray(s_metadataValues);
				result = MetadataPropertyName.Id;
			}
		}
		return result;
	}

	internal static bool TryWriteReferenceForBoxedStruct(object currentValue, ref WriteStack state, Utf8JsonWriter writer)
	{
		bool alreadyExists;
		string reference = state.ReferenceResolver.GetReference(currentValue, out alreadyExists);
		if (alreadyExists)
		{
			writer.WriteStartObject();
			writer.WriteString(s_metadataRef, reference);
			writer.WriteEndObject();
		}
		else
		{
			state.BoxedStructReferenceId = reference;
		}
		return alreadyExists;
	}

	private static bool WriteCore<TValue>(JsonConverter jsonConverter, Utf8JsonWriter writer, in TValue value, JsonSerializerOptions options, ref WriteStack state)
	{
		bool result = ((!(jsonConverter is JsonConverter<TValue> jsonConverter2)) ? jsonConverter.WriteCoreAsObject(writer, value, options, ref state) : jsonConverter2.WriteCore(writer, in value, options, ref state));
		writer.Flush();
		return result;
	}

	private static void WriteUsingGeneratedSerializer<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo.HasSerialize && jsonTypeInfo is JsonTypeInfo<TValue> jsonTypeInfo2)
		{
			JsonSerializerContext context = jsonTypeInfo2.Options._context;
			if (context != null && context.CanUseSerializationLogic)
			{
				jsonTypeInfo2.SerializeHandler(writer, value);
				writer.Flush();
				return;
			}
		}
		WriteUsingSerializer(writer, in value, jsonTypeInfo);
	}

	private static void WriteUsingSerializer<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		WriteStack state = default(WriteStack);
		state.Initialize(jsonTypeInfo, supportContinuation: false);
		JsonConverter converterBase = jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
		if (converterBase is JsonConverter<TValue> jsonConverter)
		{
			jsonConverter.WriteCore(writer, in value, jsonTypeInfo.Options, ref state);
		}
		else
		{
			converterBase.WriteCoreAsObject(writer, value, jsonTypeInfo.Options, ref state);
		}
		writer.Flush();
	}

	private static Type GetRuntimeType<TValue>(in TValue value)
	{
		Type type = typeof(TValue);
		if (type == JsonTypeInfo.ObjectType && value != null)
		{
			type = value.GetType();
		}
		return type;
	}

	private static Type GetRuntimeTypeAndValidateInputType(object value, Type inputType)
	{
		if ((object)inputType == null)
		{
			throw new ArgumentNullException("inputType");
		}
		if (value != null)
		{
			Type type = value.GetType();
			if (!inputType.IsAssignableFrom(type))
			{
				ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
			}
			if (inputType == JsonTypeInfo.ObjectType)
			{
				return type;
			}
		}
		return inputType;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task SerializeAsync<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteStreamAsync(utf8Json, value, typeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static void Serialize<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		WriteStream(utf8Json, in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static Task SerializeAsync(Stream utf8Json, object? value, Type inputType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteStreamAsync(utf8Json, value, typeInfo, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static void Serialize(Stream utf8Json, object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		WriteStream(utf8Json, in value, typeInfo);
	}

	public static Task SerializeAsync<TValue>(Stream utf8Json, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		return WriteStreamAsync(utf8Json, value, jsonTypeInfo, cancellationToken);
	}

	public static void Serialize<TValue>(Stream utf8Json, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		WriteStream(utf8Json, in value, jsonTypeInfo);
	}

	public static Task SerializeAsync(Stream utf8Json, object? value, Type inputType, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		return WriteStreamAsync(utf8Json, value, GetTypeInfo(context, runtimeTypeAndValidateInputType), cancellationToken);
	}

	public static void Serialize(Stream utf8Json, object? value, Type inputType, JsonSerializerContext context)
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		WriteStream(utf8Json, in value, GetTypeInfo(context, runtimeTypeAndValidateInputType));
	}

	private static async Task WriteStreamAsync<TValue>(Stream utf8Json, TValue value, JsonTypeInfo jsonTypeInfo, CancellationToken cancellationToken)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		JsonWriterOptions writerOptions = options.GetWriterOptions();
		using PooledByteBufferWriter bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using Utf8JsonWriter writer = new Utf8JsonWriter(bufferWriter, writerOptions);
		WriteStack state = new WriteStack
		{
			CancellationToken = cancellationToken
		};
		JsonConverter converter = state.Initialize(jsonTypeInfo, supportContinuation: true);
		try
		{
			bool isFinalBlock;
			do
			{
				state.FlushThreshold = (int)((float)bufferWriter.Capacity * 0.9f);
				try
				{
					isFinalBlock = WriteCore(converter, writer, in value, options, ref state);
					if (state.SuppressFlush)
					{
						state.SuppressFlush = false;
						continue;
					}
					await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					bufferWriter.Clear();
				}
				finally
				{
					if (state.PendingTask != null)
					{
						try
						{
							await state.PendingTask.ConfigureAwait(continueOnCapturedContext: false);
						}
						catch
						{
						}
					}
					List<IAsyncDisposable> completedAsyncDisposables = state.CompletedAsyncDisposables;
					if (completedAsyncDisposables != null && completedAsyncDisposables.Count > 0)
					{
						await state.DisposeCompletedAsyncDisposables().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			while (!isFinalBlock);
		}
		catch
		{
			await state.DisposePendingDisposablesOnExceptionAsync().ConfigureAwait(continueOnCapturedContext: false);
			throw;
		}
	}

	private static void WriteStream<TValue>(Stream utf8Json, in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		JsonWriterOptions writerOptions = options.GetWriterOptions();
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, writerOptions);
		WriteStack state = default(WriteStack);
		JsonConverter jsonConverter = state.Initialize(jsonTypeInfo, supportContinuation: true);
		bool flag;
		do
		{
			state.FlushThreshold = (int)((float)pooledByteBufferWriter.Capacity * 0.9f);
			flag = WriteCore(jsonConverter, writer, in value, options, ref state);
			pooledByteBufferWriter.WriteToStream(utf8Json);
			pooledByteBufferWriter.Clear();
		}
		while (!flag);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		return WriteStringUsingSerializer(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static string Serialize(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		return WriteStringUsingSerializer(in value, typeInfo);
	}

	public static string Serialize<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		return WriteStringUsingGeneratedSerializer(in value, jsonTypeInfo);
	}

	public static string Serialize(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, runtimeTypeAndValidateInputType);
		return WriteStringUsingGeneratedSerializer(in value, typeInfo);
	}

	private static string WriteStringUsingGeneratedSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonReaderHelper.TranscodeHelper(pooledByteBufferWriter.WrittenMemory.Span);
	}

	private static string WriteStringUsingSerializer<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			WriteUsingSerializer(writer, in value, jsonTypeInfo);
		}
		return JsonReaderHelper.TranscodeHelper(pooledByteBufferWriter.WrittenMemory.Span);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		Type runtimeType = GetRuntimeType(in value);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeType);
		WriteUsingSerializer(writer, in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, runtimeTypeAndValidateInputType);
		WriteUsingSerializer(writer, in value, typeInfo);
	}

	public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (jsonTypeInfo == null)
		{
			throw new ArgumentNullException("jsonTypeInfo");
		}
		WriteUsingGeneratedSerializer(writer, in value, jsonTypeInfo);
	}

	public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerContext context)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		Type runtimeTypeAndValidateInputType = GetRuntimeTypeAndValidateInputType(value, inputType);
		WriteUsingGeneratedSerializer(writer, in value, GetTypeInfo(context, runtimeTypeAndValidateInputType));
	}
}
