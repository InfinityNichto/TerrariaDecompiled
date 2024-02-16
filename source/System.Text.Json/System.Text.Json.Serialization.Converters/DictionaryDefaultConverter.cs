using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal abstract class DictionaryDefaultConverter<TDictionary, TKey, TValue> : JsonDictionaryConverter<TDictionary, TKey, TValue> where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
{
	internal override bool CanHaveIdMetadata => true;

	protected internal override bool OnWriteResume(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options, ref WriteStack state)
	{
		IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
		if (state.Current.CollectionEnumerator == null)
		{
			enumerator = value.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				enumerator.Dispose();
				return true;
			}
		}
		else
		{
			enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.CollectionEnumerator;
		}
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (_keyConverter == null)
		{
			_keyConverter = JsonDictionaryConverter<TDictionary, TKey, TValue>.GetConverter<TKey>(jsonTypeInfo.KeyTypeInfo);
		}
		if (_valueConverter == null)
		{
			_valueConverter = JsonDictionaryConverter<TDictionary, TKey, TValue>.GetConverter<TValue>(jsonTypeInfo.ElementTypeInfo);
		}
		do
		{
			if (ShouldFlush(writer, ref state))
			{
				state.Current.CollectionEnumerator = enumerator;
				return false;
			}
			if ((int)state.Current.PropertyState < 2)
			{
				state.Current.PropertyState = StackFramePropertyState.Name;
				TKey key = enumerator.Current.Key;
				_keyConverter.WriteAsPropertyNameCore(writer, key, options, state.Current.IsWritingExtensionDataProperty);
			}
			TValue value2 = enumerator.Current.Value;
			if (!_valueConverter.TryWrite(writer, in value2, options, ref state))
			{
				state.Current.CollectionEnumerator = enumerator;
				return false;
			}
			state.Current.EndDictionaryElement();
		}
		while (enumerator.MoveNext());
		enumerator.Dispose();
		return true;
	}
}
