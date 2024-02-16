using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ImmutableEnumerableOfTConverterWithReflection<TCollection, TElement> : ImmutableEnumerableOfTConverter<TCollection, TElement> where TCollection : IEnumerable<TElement>
{
	internal override bool RequiresDynamicMemberAccessors => true;

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public ImmutableEnumerableOfTConverterWithReflection()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
		jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>();
	}
}
