using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler;

internal sealed class KeyedStack<TKey, TValue> where TValue : class
{
	private readonly Dictionary<TKey, Stack<TValue>> _data = new Dictionary<TKey, Stack<TValue>>();

	internal void Push(TKey key, TValue value)
	{
		if (!_data.TryGetValue(key, out var value2))
		{
			_data.Add(key, value2 = new Stack<TValue>());
		}
		value2.Push(value);
	}

	internal TValue TryPop(TKey key)
	{
		if (!_data.TryGetValue(key, out var value) || !value.TryPop(out var result))
		{
			return null;
		}
		return result;
	}
}
