using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Group>))]
public class GroupCollection : IList<Group>, ICollection<Group>, IEnumerable<Group>, IEnumerable, IReadOnlyList<Group>, IReadOnlyCollection<Group>, IList, ICollection, IReadOnlyDictionary<string, Group>, IEnumerable<KeyValuePair<string, Group>>, IReadOnlyCollection<KeyValuePair<string, Group>>
{
	private sealed class Enumerator : IEnumerator<Group>, IEnumerator, IDisposable, IEnumerator<KeyValuePair<string, Group>>
	{
		private readonly GroupCollection _collection;

		private int _index;

		public Group Current
		{
			get
			{
				if (_index < 0 || _index >= _collection.Count)
				{
					throw new InvalidOperationException(System.SR.EnumNotStarted);
				}
				return _collection[_index];
			}
		}

		KeyValuePair<string, Group> IEnumerator<KeyValuePair<string, Group>>.Current
		{
			get
			{
				if ((uint)_index >= _collection.Count)
				{
					throw new InvalidOperationException(System.SR.EnumNotStarted);
				}
				Group group = _collection[_index];
				return new KeyValuePair<string, Group>(group.Name, group);
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(GroupCollection collection)
		{
			_collection = collection;
			_index = -1;
		}

		public bool MoveNext()
		{
			int count = _collection.Count;
			if (_index >= count)
			{
				return false;
			}
			_index++;
			return _index < count;
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly Match _match;

	private readonly Hashtable _captureMap;

	private Group[] _groups;

	public bool IsReadOnly => true;

	public int Count => _match._matchcount.Length;

	public Group this[int groupnum] => GetGroup(groupnum);

	public Group this[string groupname]
	{
		get
		{
			if (_match._regex != null)
			{
				return GetGroup(_match._regex.GroupNumberFromName(groupname));
			}
			return Group.s_emptyGroup;
		}
	}

	public bool IsSynchronized => false;

	public object SyncRoot => _match;

	Group IList<Group>.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	bool IList.IsFixedSize => true;

	object? IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	public IEnumerable<string> Keys
	{
		get
		{
			int i = 0;
			while (i < Count)
			{
				yield return GetGroup(i).Name;
				int num = i + 1;
				i = num;
			}
		}
	}

	public IEnumerable<Group> Values
	{
		get
		{
			int i = 0;
			while (i < Count)
			{
				yield return GetGroup(i);
				int num = i + 1;
				i = num;
			}
		}
	}

	internal GroupCollection(Match match, Hashtable caps)
	{
		_match = match;
		_captureMap = caps;
	}

	internal void Reset()
	{
		_groups = null;
	}

	public IEnumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<Group> IEnumerable<Group>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	private Group GetGroup(int groupnum)
	{
		if (_captureMap != null)
		{
			if (_captureMap.TryGetValue<int>(groupnum, out var value))
			{
				return GetGroupImpl(value);
			}
		}
		else if ((uint)groupnum < _match._matchcount.Length)
		{
			return GetGroupImpl(groupnum);
		}
		return Group.s_emptyGroup;
	}

	private Group GetGroupImpl(int groupnum)
	{
		if (groupnum == 0)
		{
			return _match;
		}
		if (_groups == null)
		{
			_groups = new Group[_match._matchcount.Length - 1];
			for (int i = 0; i < _groups.Length; i++)
			{
				string name = _match._regex.GroupNameFromNumber(i + 1);
				_groups[i] = new Group(_match.Text, _match._matches[i + 1], _match._matchcount[i + 1], name);
			}
		}
		return _groups[groupnum - 1];
	}

	public void CopyTo(Array array, int arrayIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int num = arrayIndex;
		for (int i = 0; i < Count; i++)
		{
			array.SetValue(this[i], num);
			num++;
		}
	}

	public void CopyTo(Group[] array, int arrayIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex");
		}
		if (array.Length - arrayIndex < Count)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
		int num = arrayIndex;
		for (int i = 0; i < Count; i++)
		{
			array[num] = this[i];
			num++;
		}
	}

	int IList<Group>.IndexOf(Group item)
	{
		for (int i = 0; i < Count; i++)
		{
			if (EqualityComparer<Group>.Default.Equals(this[i], item))
			{
				return i;
			}
		}
		return -1;
	}

	void IList<Group>.Insert(int index, Group item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList<Group>.RemoveAt(int index)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection<Group>.Add(Group item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection<Group>.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool ICollection<Group>.Contains(Group item)
	{
		return ((IList<Group>)this).IndexOf(item) >= 0;
	}

	bool ICollection<Group>.Remove(Group item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool IList.Contains(object value)
	{
		if (value is Group item)
		{
			return ((ICollection<Group>)this).Contains(item);
		}
		return false;
	}

	int IList.IndexOf(object value)
	{
		if (!(value is Group item))
		{
			return -1;
		}
		return ((IList<Group>)this).IndexOf(item);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	IEnumerator<KeyValuePair<string, Group>> IEnumerable<KeyValuePair<string, Group>>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public bool TryGetValue(string key, [NotNullWhen(true)] out Group? value)
	{
		Group group = this[key];
		if (group == Group.s_emptyGroup)
		{
			value = null;
			return false;
		}
		value = group;
		return true;
	}

	public bool ContainsKey(string key)
	{
		return _match._regex.GroupNumberFromName(key) >= 0;
	}
}
