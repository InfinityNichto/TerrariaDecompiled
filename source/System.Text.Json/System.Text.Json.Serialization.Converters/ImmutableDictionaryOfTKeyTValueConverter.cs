using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal class ImmutableDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue> : DictionaryDefaultConverter<TDictionary, TKey, TValue> where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
	internal sealed override bool CanHaveIdMetadata => false;

	protected sealed override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		((Dictionary<TKey, TValue>)state.Current.ReturnValue)[key] = value;
	}

	protected sealed override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
	{
		state.Current.ReturnValue = new Dictionary<TKey, TValue>();
	}

	protected sealed override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary> func = (Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary>)state.Current.JsonTypeInfo.CreateObjectWithArgs;
		state.Current.ReturnValue = func((Dictionary<TKey, TValue>)state.Current.ReturnValue);
	}
}
