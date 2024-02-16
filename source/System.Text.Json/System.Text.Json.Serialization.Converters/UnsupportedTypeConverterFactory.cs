using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization.Converters;

internal sealed class UnsupportedTypeConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type)
	{
		if (!(type == typeof(Type)) && !(type == typeof(SerializationInfo)) && !(type == typeof(IntPtr)) && !(type == typeof(UIntPtr)) && !(type == typeof(DateOnly)))
		{
			return type == typeof(TimeOnly);
		}
		return true;
	}

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		return (JsonConverter)Activator.CreateInstance(typeof(UnsupportedTypeConverter<>).MakeGenericType(type), BindingFlags.Instance | BindingFlags.Public, null, null, null);
	}
}
