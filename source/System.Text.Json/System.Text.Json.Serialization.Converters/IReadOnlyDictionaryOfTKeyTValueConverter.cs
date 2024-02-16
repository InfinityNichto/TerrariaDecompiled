using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IReadOnlyDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue> : DictionaryDefaultConverter<TDictionary, TKey, TValue> where TDictionary : IReadOnlyDictionary<TKey, TValue>
{
	internal override Type RuntimeType => typeof(Dictionary<TKey, TValue>);

	protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		((Dictionary<TKey, TValue>)state.Current.ReturnValue)[key] = value;
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
	{
		if (!TypeToConvert.IsAssignableFrom(RuntimeType))
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
		}
		state.Current.ReturnValue = new Dictionary<TKey, TValue>();
	}
}
