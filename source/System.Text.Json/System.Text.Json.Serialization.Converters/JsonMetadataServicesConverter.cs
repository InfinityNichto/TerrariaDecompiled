using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonMetadataServicesConverter<T> : JsonResumableConverter<T>
{
	private readonly Func<JsonConverter<T>> _converterCreator;

	private readonly ConverterStrategy _converterStrategy;

	private JsonConverter<T> _converter;

	internal JsonConverter<T> Converter
	{
		get
		{
			if (_converter == null)
			{
				_converter = _converterCreator();
			}
			return _converter;
		}
	}

	internal override ConverterStrategy ConverterStrategy => _converterStrategy;

	internal override Type KeyType => Converter.KeyType;

	internal override Type ElementType => Converter.ElementType;

	internal override bool ConstructorIsParameterized => Converter.ConstructorIsParameterized;

	public JsonMetadataServicesConverter(Func<JsonConverter<T>> converterCreator, ConverterStrategy converterStrategy)
	{
		_converterCreator = converterCreator ?? throw new ArgumentNullException("converterCreator");
		_converterStrategy = converterStrategy;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (_converterStrategy == ConverterStrategy.Object)
		{
			if (jsonTypeInfo.PropertyCache == null)
			{
				jsonTypeInfo.InitializePropCache();
			}
			if (jsonTypeInfo.ParameterCache == null && jsonTypeInfo.IsObjectWithParameterizedCtor)
			{
				jsonTypeInfo.InitializeParameterCache();
			}
		}
		return Converter.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (!state.SupportContinuation && jsonTypeInfo is JsonTypeInfo<T> { SerializeHandler: not null } jsonTypeInfo2)
		{
			JsonSerializerContext context = jsonTypeInfo2.Options._context;
			if (context != null && context.CanUseSerializationLogic)
			{
				jsonTypeInfo2.SerializeHandler(writer, value);
				return true;
			}
		}
		if (_converterStrategy == ConverterStrategy.Object && jsonTypeInfo.PropertyCache == null)
		{
			jsonTypeInfo.InitializePropCache();
		}
		return Converter.OnTryWrite(writer, value, options, ref state);
	}
}
