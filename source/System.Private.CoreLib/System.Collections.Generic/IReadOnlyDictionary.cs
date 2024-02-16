using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	TValue this[TKey key] { get; }

	IEnumerable<TKey> Keys { get; }

	IEnumerable<TValue> Values { get; }

	bool ContainsKey(TKey key);

	bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
}
