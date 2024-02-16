using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class FSharpMapConverter<TMap, TKey, TValue> : DictionaryDefaultConverter<TMap, TKey, TValue> where TMap : IEnumerable<KeyValuePair<TKey, TValue>>
{
	private readonly Func<IEnumerable<Tuple<TKey, TValue>>, TMap> _mapConstructor;

	internal override bool CanHaveIdMetadata => false;

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public FSharpMapConverter()
	{
		_mapConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpMapConstructor<TMap, TKey, TValue>();
	}

	protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		((List<Tuple<TKey, TValue>>)state.Current.ReturnValue).Add(new Tuple<TKey, TValue>(key, value));
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
	{
		state.Current.ReturnValue = new List<Tuple<TKey, TValue>>();
	}

	protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = _mapConstructor((List<Tuple<TKey, TValue>>)state.Current.ReturnValue);
	}
}
