using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class FSharpValueOptionConverter<TValueOption, TElement> : JsonConverter<TValueOption> where TValueOption : struct, IEquatable<TValueOption>
{
	private readonly JsonConverter<TElement> _elementConverter;

	private readonly FSharpCoreReflectionProxy.StructGetter<TValueOption, TElement> _optionValueGetter;

	private readonly Func<TElement, TValueOption> _optionConstructor;

	private readonly ConverterStrategy _converterStrategy;

	internal override ConverterStrategy ConverterStrategy => _converterStrategy;

	internal override Type ElementType => typeof(TElement);

	public override bool HandleNull => true;

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public FSharpValueOptionConverter(JsonConverter<TElement> elementConverter)
	{
		_elementConverter = elementConverter;
		_optionValueGetter = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionValueGetter<TValueOption, TElement>();
		_optionConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpValueOptionSomeConstructor<TValueOption, TElement>();
		_converterStrategy = _elementConverter.ConverterStrategy;
		base.CanUseDirectReadOrWrite = _converterStrategy == ConverterStrategy.Value;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TValueOption value)
	{
		if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
		{
			value = default(TValueOption);
			return true;
		}
		state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		if (_elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out var value2))
		{
			value = _optionConstructor(value2);
			return true;
		}
		value = default(TValueOption);
		return false;
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, TValueOption value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (value.Equals(default(TValueOption)))
		{
			writer.WriteNullValue();
			return true;
		}
		TElement value2 = _optionValueGetter(ref value);
		state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		return _elementConverter.TryWrite(writer, in value2, options, ref state);
	}

	public override void Write(Utf8JsonWriter writer, TValueOption value, JsonSerializerOptions options)
	{
		if (value.Equals(default(TValueOption)))
		{
			writer.WriteNullValue();
			return;
		}
		TElement value2 = _optionValueGetter(ref value);
		_elementConverter.Write(writer, value2, options);
	}

	public override TValueOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return default(TValueOption);
		}
		TElement arg = _elementConverter.Read(ref reader, typeToConvert, options);
		return _optionConstructor(arg);
	}
}
