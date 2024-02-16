using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization;

public class JsonStringEnumConverter : JsonConverterFactory
{
	private readonly JsonNamingPolicy _namingPolicy;

	private readonly EnumConverterOptions _converterOptions;

	public JsonStringEnumConverter()
		: this(null, allowIntegerValues: true)
	{
	}

	public JsonStringEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
	{
		_namingPolicy = namingPolicy;
		_converterOptions = ((!allowIntegerValues) ? EnumConverterOptions.AllowStrings : (EnumConverterOptions.AllowStrings | EnumConverterOptions.AllowNumbers));
	}

	public sealed override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.IsEnum;
	}

	public sealed override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		return EnumConverterFactory.Create(typeToConvert, _converterOptions, _namingPolicy, options);
	}
}
