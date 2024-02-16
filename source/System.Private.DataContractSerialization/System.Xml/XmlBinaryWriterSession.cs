using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Xml;

public class XmlBinaryWriterSession
{
	private sealed class PriorityDictionary<K, V> where K : class
	{
		private struct Entry
		{
			public K Key;

			public V Value;

			public int Time;
		}

		private Dictionary<K, V> _dictionary;

		private readonly Entry[] _list;

		private int _listCount;

		private int _now;

		private int Now
		{
			get
			{
				if (++_now == int.MaxValue)
				{
					DecreaseAll();
				}
				return _now;
			}
		}

		public PriorityDictionary()
		{
			_list = new Entry[16];
		}

		public void Clear()
		{
			_now = 0;
			_listCount = 0;
			Array.Clear(_list);
			if (_dictionary != null)
			{
				_dictionary.Clear();
			}
		}

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
		{
			for (int i = 0; i < _listCount; i++)
			{
				if (_list[i].Key == key)
				{
					value = _list[i].Value;
					_list[i].Time = Now;
					return true;
				}
			}
			for (int j = 0; j < _listCount; j++)
			{
				if (_list[j].Key.Equals(key))
				{
					value = _list[j].Value;
					_list[j].Time = Now;
					return true;
				}
			}
			if (_dictionary == null)
			{
				value = default(V);
				return false;
			}
			if (!_dictionary.TryGetValue(key, out value))
			{
				return false;
			}
			int num = 0;
			int time = _list[0].Time;
			for (int k = 1; k < _listCount; k++)
			{
				if (_list[k].Time < time)
				{
					num = k;
					time = _list[k].Time;
				}
			}
			_list[num].Key = key;
			_list[num].Value = value;
			_list[num].Time = Now;
			return true;
		}

		public void Add(K key, V value)
		{
			if (_listCount < _list.Length)
			{
				_list[_listCount].Key = key;
				_list[_listCount].Value = value;
				_listCount++;
				return;
			}
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<K, V>();
				for (int i = 0; i < _listCount; i++)
				{
					_dictionary.Add(_list[i].Key, _list[i].Value);
				}
			}
			_dictionary.Add(key, value);
		}

		private void DecreaseAll()
		{
			for (int i = 0; i < _listCount; i++)
			{
				_list[i].Time /= 2;
			}
			_now /= 2;
		}
	}

	private sealed class IntArray
	{
		private int[] _array;

		public int this[int index]
		{
			get
			{
				if (index >= _array.Length)
				{
					return 0;
				}
				return _array[index];
			}
			set
			{
				if (index >= _array.Length)
				{
					int[] array = new int[Math.Max(index + 1, _array.Length * 2)];
					Array.Copy(_array, array, _array.Length);
					_array = array;
				}
				_array[index] = value;
			}
		}

		public IntArray(int size)
		{
			_array = new int[size];
		}
	}

	private readonly PriorityDictionary<string, int> _strings;

	private readonly PriorityDictionary<IXmlDictionary, IntArray> _maps;

	private int _nextKey;

	public XmlBinaryWriterSession()
	{
		_nextKey = 0;
		_maps = new PriorityDictionary<IXmlDictionary, IntArray>();
		_strings = new PriorityDictionary<string, int>();
	}

	public virtual bool TryAdd(XmlDictionaryString value, out int key)
	{
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
		}
		if (_maps.TryGetValue(value.Dictionary, out var value2))
		{
			key = value2[value.Key] - 1;
			if (key != -1)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.XmlKeyAlreadyExists));
			}
			key = Add(value.Value);
			value2[value.Key] = key + 1;
			return true;
		}
		key = Add(value.Value);
		value2 = AddKeys(value.Dictionary, value.Key + 1);
		value2[value.Key] = key + 1;
		return true;
	}

	private int Add(string s)
	{
		int num = _nextKey++;
		_strings.Add(s, num);
		return num;
	}

	private IntArray AddKeys(IXmlDictionary dictionary, int minCount)
	{
		IntArray intArray = new IntArray(Math.Max(minCount, 16));
		_maps.Add(dictionary, intArray);
		return intArray;
	}

	public void Reset()
	{
		_nextKey = 0;
		_maps.Clear();
		_strings.Clear();
	}

	internal bool TryLookup(XmlDictionaryString s, out int key)
	{
		if (_maps.TryGetValue(s.Dictionary, out var value))
		{
			key = value[s.Key] - 1;
			if (key != -1)
			{
				return true;
			}
		}
		if (_strings.TryGetValue(s.Value, out key))
		{
			if (value == null)
			{
				value = AddKeys(s.Dictionary, s.Key + 1);
			}
			value[s.Key] = key + 1;
			return true;
		}
		key = -1;
		return false;
	}
}
