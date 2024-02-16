using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ImmutableDictionaryOfTKeyTValueConverterWithReflection<TCollection, TKey, TValue> : ImmutableDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue> where TCollection : IReadOnlyDictionary<TKey, TValue>
{
	internal override bool RequiresDynamicMemberAccessors => true;

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public ImmutableDictionaryOfTKeyTValueConverterWithReflection()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo jsonTypeInfo = null)
	{
		jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>();
	}
}
