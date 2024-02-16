using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class StackOrQueueConverterWithReflection<TCollection> : StackOrQueueConverter<TCollection> where TCollection : IEnumerable
{
	internal override bool RequiresDynamicMemberAccessors => true;

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public StackOrQueueConverterWithReflection()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
		jsonTypeInfo.AddMethodDelegate = options.MemberAccessorStrategy.CreateAddMethodDelegate<TCollection>();
	}
}
