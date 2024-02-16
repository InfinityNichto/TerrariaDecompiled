using System.Collections.Generic;

namespace System.Threading;

internal static class AsyncLocalValueMap
{
	private sealed class EmptyAsyncLocalValueMap : IAsyncLocalValueMap
	{
		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value == null && treatNullValueAsNonexistent)
			{
				return this;
			}
			return new OneElementAsyncLocalValueMap(key, value);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			value = null;
			return false;
		}
	}

	private sealed class OneElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly IAsyncLocal _key1;

		private readonly object _value1;

		public OneElementAsyncLocalValueMap(IAsyncLocal key, object value)
		{
			_key1 = key;
			_value1 = value;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				if (key != _key1)
				{
					return new TwoElementAsyncLocalValueMap(_key1, _value1, key, value);
				}
				return new OneElementAsyncLocalValueMap(key, value);
			}
			if (key != _key1)
			{
				return this;
			}
			return Empty;
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _key1)
			{
				value = _value1;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class TwoElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly IAsyncLocal _key1;

		private readonly IAsyncLocal _key2;

		private readonly object _value1;

		private readonly object _value2;

		public TwoElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2)
		{
			_key1 = key1;
			_value1 = value1;
			_key2 = key2;
			_value2 = value2;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				if (key != _key1)
				{
					if (key != _key2)
					{
						return new ThreeElementAsyncLocalValueMap(_key1, _value1, _key2, _value2, key, value);
					}
					return new TwoElementAsyncLocalValueMap(_key1, _value1, key, value);
				}
				return new TwoElementAsyncLocalValueMap(key, value, _key2, _value2);
			}
			if (key != _key1)
			{
				if (key != _key2)
				{
					return this;
				}
				return new OneElementAsyncLocalValueMap(_key1, _value1);
			}
			return new OneElementAsyncLocalValueMap(_key2, _value2);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _key1)
			{
				value = _value1;
				return true;
			}
			if (key == _key2)
			{
				value = _value2;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class ThreeElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly IAsyncLocal _key1;

		private readonly IAsyncLocal _key2;

		private readonly IAsyncLocal _key3;

		private readonly object _value1;

		private readonly object _value2;

		private readonly object _value3;

		public ThreeElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2, IAsyncLocal key3, object value3)
		{
			_key1 = key1;
			_value1 = value1;
			_key2 = key2;
			_value2 = value2;
			_key3 = key3;
			_value3 = value3;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				if (key == _key1)
				{
					return new ThreeElementAsyncLocalValueMap(key, value, _key2, _value2, _key3, _value3);
				}
				if (key == _key2)
				{
					return new ThreeElementAsyncLocalValueMap(_key1, _value1, key, value, _key3, _value3);
				}
				if (key == _key3)
				{
					return new ThreeElementAsyncLocalValueMap(_key1, _value1, _key2, _value2, key, value);
				}
				MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new MultiElementAsyncLocalValueMap(4);
				multiElementAsyncLocalValueMap.UnsafeStore(0, _key1, _value1);
				multiElementAsyncLocalValueMap.UnsafeStore(1, _key2, _value2);
				multiElementAsyncLocalValueMap.UnsafeStore(2, _key3, _value3);
				multiElementAsyncLocalValueMap.UnsafeStore(3, key, value);
				return multiElementAsyncLocalValueMap;
			}
			if (key != _key1)
			{
				if (key != _key2)
				{
					if (key != _key3)
					{
						return this;
					}
					return new TwoElementAsyncLocalValueMap(_key1, _value1, _key2, _value2);
				}
				return new TwoElementAsyncLocalValueMap(_key1, _value1, _key3, _value3);
			}
			return new TwoElementAsyncLocalValueMap(_key2, _value2, _key3, _value3);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _key1)
			{
				value = _value1;
				return true;
			}
			if (key == _key2)
			{
				value = _value2;
				return true;
			}
			if (key == _key3)
			{
				value = _value3;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class MultiElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object>[] _keyValues;

		internal MultiElementAsyncLocalValueMap(int count)
		{
			_keyValues = new KeyValuePair<IAsyncLocal, object>[count];
		}

		internal void UnsafeStore(int index, IAsyncLocal key, object value)
		{
			_keyValues[index] = new KeyValuePair<IAsyncLocal, object>(key, value);
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			for (int i = 0; i < _keyValues.Length; i++)
			{
				if (key == _keyValues[i].Key)
				{
					if (value != null || !treatNullValueAsNonexistent)
					{
						MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new MultiElementAsyncLocalValueMap(_keyValues.Length);
						Array.Copy(_keyValues, multiElementAsyncLocalValueMap._keyValues, _keyValues.Length);
						multiElementAsyncLocalValueMap._keyValues[i] = new KeyValuePair<IAsyncLocal, object>(key, value);
						return multiElementAsyncLocalValueMap;
					}
					if (_keyValues.Length == 4)
					{
						return i switch
						{
							2 => new ThreeElementAsyncLocalValueMap(_keyValues[0].Key, _keyValues[0].Value, _keyValues[1].Key, _keyValues[1].Value, _keyValues[3].Key, _keyValues[3].Value), 
							1 => new ThreeElementAsyncLocalValueMap(_keyValues[0].Key, _keyValues[0].Value, _keyValues[2].Key, _keyValues[2].Value, _keyValues[3].Key, _keyValues[3].Value), 
							0 => new ThreeElementAsyncLocalValueMap(_keyValues[1].Key, _keyValues[1].Value, _keyValues[2].Key, _keyValues[2].Value, _keyValues[3].Key, _keyValues[3].Value), 
							_ => new ThreeElementAsyncLocalValueMap(_keyValues[0].Key, _keyValues[0].Value, _keyValues[1].Key, _keyValues[1].Value, _keyValues[2].Key, _keyValues[2].Value), 
						};
					}
					MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap2 = new MultiElementAsyncLocalValueMap(_keyValues.Length - 1);
					if (i != 0)
					{
						Array.Copy(_keyValues, multiElementAsyncLocalValueMap2._keyValues, i);
					}
					if (i != _keyValues.Length - 1)
					{
						Array.Copy(_keyValues, i + 1, multiElementAsyncLocalValueMap2._keyValues, i, _keyValues.Length - i - 1);
					}
					return multiElementAsyncLocalValueMap2;
				}
			}
			if (value == null && treatNullValueAsNonexistent)
			{
				return this;
			}
			if (_keyValues.Length < 16)
			{
				MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap3 = new MultiElementAsyncLocalValueMap(_keyValues.Length + 1);
				Array.Copy(_keyValues, multiElementAsyncLocalValueMap3._keyValues, _keyValues.Length);
				multiElementAsyncLocalValueMap3._keyValues[_keyValues.Length] = new KeyValuePair<IAsyncLocal, object>(key, value);
				return multiElementAsyncLocalValueMap3;
			}
			ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new ManyElementAsyncLocalValueMap(17);
			KeyValuePair<IAsyncLocal, object>[] keyValues = _keyValues;
			for (int j = 0; j < keyValues.Length; j++)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = keyValues[j];
				manyElementAsyncLocalValueMap[keyValuePair.Key] = keyValuePair.Value;
			}
			manyElementAsyncLocalValueMap[key] = value;
			return manyElementAsyncLocalValueMap;
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			KeyValuePair<IAsyncLocal, object>[] keyValues = _keyValues;
			for (int i = 0; i < keyValues.Length; i++)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = keyValues[i];
				if (key == keyValuePair.Key)
				{
					value = keyValuePair.Value;
					return true;
				}
			}
			value = null;
			return false;
		}
	}

	private sealed class ManyElementAsyncLocalValueMap : Dictionary<IAsyncLocal, object>, IAsyncLocalValueMap
	{
		public ManyElementAsyncLocalValueMap(int capacity)
			: base(capacity)
		{
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			int count = base.Count;
			bool flag = ContainsKey(key);
			if (value != null || !treatNullValueAsNonexistent)
			{
				ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new ManyElementAsyncLocalValueMap(count + ((!flag) ? 1 : 0));
				using (Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<IAsyncLocal, object> current = enumerator.Current;
						manyElementAsyncLocalValueMap[current.Key] = current.Value;
					}
				}
				manyElementAsyncLocalValueMap[key] = value;
				return manyElementAsyncLocalValueMap;
			}
			if (flag)
			{
				if (count == 17)
				{
					MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new MultiElementAsyncLocalValueMap(16);
					int num = 0;
					using Enumerator enumerator2 = GetEnumerator();
					while (enumerator2.MoveNext())
					{
						KeyValuePair<IAsyncLocal, object> current2 = enumerator2.Current;
						if (key != current2.Key)
						{
							multiElementAsyncLocalValueMap.UnsafeStore(num++, current2.Key, current2.Value);
						}
					}
					return multiElementAsyncLocalValueMap;
				}
				ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap2 = new ManyElementAsyncLocalValueMap(count - 1);
				using Enumerator enumerator3 = GetEnumerator();
				while (enumerator3.MoveNext())
				{
					KeyValuePair<IAsyncLocal, object> current3 = enumerator3.Current;
					if (key != current3.Key)
					{
						manyElementAsyncLocalValueMap2[current3.Key] = current3.Value;
					}
				}
				return manyElementAsyncLocalValueMap2;
			}
			return this;
		}
	}

	public static IAsyncLocalValueMap Empty { get; } = new EmptyAsyncLocalValueMap();


	public static bool IsEmpty(IAsyncLocalValueMap asyncLocalValueMap)
	{
		return asyncLocalValueMap == Empty;
	}

	public static IAsyncLocalValueMap Create(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
	{
		if (value == null && treatNullValueAsNonexistent)
		{
			return Empty;
		}
		return new OneElementAsyncLocalValueMap(key, value);
	}
}
