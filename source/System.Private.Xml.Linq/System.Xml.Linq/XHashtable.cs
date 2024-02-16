using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Xml.Linq;

internal sealed class XHashtable<TValue>
{
	public delegate string ExtractKeyDelegate(TValue value);

	private sealed class XHashtableState
	{
		private struct Entry
		{
			public TValue Value;

			public int HashCode;

			public int Next;
		}

		private readonly int[] _buckets;

		private readonly Entry[] _entries;

		private int _numEntries;

		private readonly ExtractKeyDelegate _extractKey;

		public XHashtableState(ExtractKeyDelegate extractKey, int capacity)
		{
			_buckets = new int[capacity];
			_entries = new Entry[capacity];
			_extractKey = extractKey;
		}

		public XHashtableState Resize()
		{
			if (_numEntries < _buckets.Length)
			{
				return this;
			}
			int num = 0;
			for (int i = 0; i < _buckets.Length; i++)
			{
				int num2 = _buckets[i];
				if (num2 == 0)
				{
					num2 = Interlocked.CompareExchange(ref _buckets[i], -1, 0);
				}
				while (num2 > 0)
				{
					if (_extractKey(_entries[num2].Value) != null)
					{
						num++;
					}
					num2 = ((_entries[num2].Next != 0) ? _entries[num2].Next : Interlocked.CompareExchange(ref _entries[num2].Next, -1, 0));
				}
			}
			if (num < _buckets.Length / 2)
			{
				num = _buckets.Length;
			}
			else
			{
				num = _buckets.Length * 2;
				if (num < 0)
				{
					throw new OverflowException();
				}
			}
			XHashtableState xHashtableState = new XHashtableState(_extractKey, num);
			for (int j = 0; j < _buckets.Length; j++)
			{
				for (int num3 = _buckets[j]; num3 > 0; num3 = _entries[num3].Next)
				{
					xHashtableState.TryAdd(_entries[num3].Value, out var _);
				}
			}
			return xHashtableState;
		}

		public bool TryGetValue(string key, int index, int count, [MaybeNullWhen(false)] out TValue value)
		{
			int hashCode = ComputeHashCode(key, index, count);
			int entryIndex = 0;
			if (FindEntry(hashCode, key, index, count, ref entryIndex))
			{
				value = _entries[entryIndex].Value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		public bool TryAdd(TValue value, out TValue newValue)
		{
			newValue = value;
			string text = _extractKey(value);
			if (text == null)
			{
				return true;
			}
			int num = ComputeHashCode(text, 0, text.Length);
			int num2 = Interlocked.Increment(ref _numEntries);
			if (num2 < 0 || num2 >= _buckets.Length)
			{
				return false;
			}
			_entries[num2].Value = value;
			_entries[num2].HashCode = num;
			Thread.MemoryBarrier();
			int entryIndex = 0;
			while (!FindEntry(num, text, 0, text.Length, ref entryIndex))
			{
				entryIndex = ((entryIndex != 0) ? Interlocked.CompareExchange(ref _entries[entryIndex].Next, num2, 0) : Interlocked.CompareExchange(ref _buckets[num & (_buckets.Length - 1)], num2, 0));
				if (entryIndex <= 0)
				{
					return entryIndex == 0;
				}
			}
			newValue = _entries[entryIndex].Value;
			return true;
		}

		private bool FindEntry(int hashCode, string key, int index, int count, ref int entryIndex)
		{
			int num = entryIndex;
			int num2 = ((num != 0) ? num : _buckets[hashCode & (_buckets.Length - 1)]);
			while (num2 > 0)
			{
				if (_entries[num2].HashCode == hashCode)
				{
					string text = _extractKey(_entries[num2].Value);
					if (text == null)
					{
						if (_entries[num2].Next > 0)
						{
							_entries[num2].Value = default(TValue);
							num2 = _entries[num2].Next;
							if (num == 0)
							{
								_buckets[hashCode & (_buckets.Length - 1)] = num2;
							}
							else
							{
								_entries[num].Next = num2;
							}
							continue;
						}
					}
					else if (count == text.Length && string.CompareOrdinal(key, index, text, 0, count) == 0)
					{
						entryIndex = num2;
						return true;
					}
				}
				num = num2;
				num2 = _entries[num2].Next;
			}
			entryIndex = num;
			return false;
		}

		private static int ComputeHashCode(string key, int index, int count)
		{
			return string.GetHashCode(key.AsSpan(index, count));
		}
	}

	private XHashtableState _state;

	public XHashtable(ExtractKeyDelegate extractKey, int capacity)
	{
		_state = new XHashtableState(extractKey, capacity);
	}

	public bool TryGetValue(string key, int index, int count, [MaybeNullWhen(false)] out TValue value)
	{
		return _state.TryGetValue(key, index, count, out value);
	}

	public TValue Add(TValue value)
	{
		TValue newValue;
		while (!_state.TryAdd(value, out newValue))
		{
			lock (this)
			{
				XHashtableState state = _state.Resize();
				Thread.MemoryBarrier();
				_state = state;
			}
		}
		return newValue;
	}
}
