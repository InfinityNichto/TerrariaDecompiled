using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class FSharpTypeConverterFactory : JsonConverterFactory
{
	private ObjectConverterFactory _recordConverterFactory;

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public FSharpTypeConverterFactory()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
	public override bool CanConvert(Type typeToConvert)
	{
		if (FSharpCoreReflectionProxy.IsFSharpType(typeToConvert))
		{
			return FSharpCoreReflectionProxy.Instance.DetectFSharpKind(typeToConvert) != FSharpCoreReflectionProxy.FSharpKind.Unrecognized;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		object[] args = null;
		Type type2;
		switch (FSharpCoreReflectionProxy.Instance.DetectFSharpKind(typeToConvert))
		{
		case FSharpCoreReflectionProxy.FSharpKind.Option:
		{
			Type type = typeToConvert.GetGenericArguments()[0];
			type2 = typeof(FSharpOptionConverter<, >).MakeGenericType(typeToConvert, type);
			args = new object[1] { options.GetConverterInternal(type) };
			break;
		}
		case FSharpCoreReflectionProxy.FSharpKind.ValueOption:
		{
			Type type = typeToConvert.GetGenericArguments()[0];
			type2 = typeof(FSharpValueOptionConverter<, >).MakeGenericType(typeToConvert, type);
			args = new object[1] { options.GetConverterInternal(type) };
			break;
		}
		case FSharpCoreReflectionProxy.FSharpKind.List:
		{
			Type type = typeToConvert.GetGenericArguments()[0];
			type2 = typeof(FSharpListConverter<, >).MakeGenericType(typeToConvert, type);
			break;
		}
		case FSharpCoreReflectionProxy.FSharpKind.Set:
		{
			Type type = typeToConvert.GetGenericArguments()[0];
			type2 = typeof(FSharpSetConverter<, >).MakeGenericType(typeToConvert, type);
			break;
		}
		case FSharpCoreReflectionProxy.FSharpKind.Map:
		{
			Type[] genericArguments = typeToConvert.GetGenericArguments();
			Type type3 = genericArguments[0];
			Type type4 = genericArguments[1];
			type2 = typeof(FSharpMapConverter<, , >).MakeGenericType(typeToConvert, type3, type4);
			break;
		}
		case FSharpCoreReflectionProxy.FSharpKind.Record:
		{
			ObjectConverterFactory objectConverterFactory = _recordConverterFactory ?? (_recordConverterFactory = new ObjectConverterFactory(useDefaultConstructorInUnannotatedStructs: false));
			return objectConverterFactory.CreateConverter(typeToConvert, options);
		}
		case FSharpCoreReflectionProxy.FSharpKind.Union:
			throw new NotSupportedException(System.SR.FSharpDiscriminatedUnionsNotSupported);
		default:
			throw new Exception();
		}
		return (JsonConverter)Activator.CreateInstance(type2, args);
	}
}
