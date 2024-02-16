using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class LargeObjectWithParameterizedConstructorConverterWithReflection<T> : LargeObjectWithParameterizedConstructorConverter<T>
{
	internal override bool RequiresDynamicMemberAccessors => true;

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public LargeObjectWithParameterizedConstructorConverterWithReflection()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
		jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateParameterizedConstructor<T>(base.ConstructorInfo);
	}
}
