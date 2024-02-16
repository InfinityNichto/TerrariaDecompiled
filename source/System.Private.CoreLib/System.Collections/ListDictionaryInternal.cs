using System.Runtime.CompilerServices;

namespace System.Collections;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ListDictionaryInternal : IDictionary, ICollection, IEnumerable
{
	private sealed class NodeEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private readonly ListDictionaryInternal list;

		private DictionaryNode current;

		private readonly int version;

		private bool start;

		public object Current => Entry;

		public DictionaryEntry Entry
		{
			get
			{
				if (current == null)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
				}
				return new DictionaryEntry(current.key, current.value);
			}
		}

		public object Key
		{
			get
			{
				if (current == null)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
				}
				return current.key;
			}
		}

		public object Value
		{
			get
			{
				if (current == null)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
				}
				return current.value;
			}
		}

		public NodeEnumerator(ListDictionaryInternal list)
		{
			this.list = list;
			version = list.version;
			start = true;
			current = null;
		}

		public bool MoveNext()
		{
			if (version != list.version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			if (start)
			{
				current = list.head;
				start = false;
			}
			else if (current != null)
			{
				current = current.next;
			}
			return current != null;
		}

		public void Reset()
		{
			if (version != list.version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			start = true;
			current = null;
		}
	}

	private sealed class NodeKeyValueCollection : ICollection, IEnumerable
	{
		private sealed class NodeKeyValueEnumerator : IEnumerator
		{
			private readonly ListDictionaryInternal list;

			private DictionaryNode current;

			private readonly int version;

			private readonly bool isKeys;

			private bool start;

			public object Current
			{
				get
				{
					if (current == null)
					{
						throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
					}
					if (!isKeys)
					{
						return current.value;
					}
					return current.key;
				}
			}

			public NodeKeyValueEnumerator(ListDictionaryInternal list, bool isKeys)
			{
				this.list = list;
				this.isKeys = isKeys;
				version = list.version;
				start = true;
				current = null;
			}

			public bool MoveNext()
			{
				if (version != list.version)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
				}
				if (start)
				{
					current = list.head;
					start = false;
				}
				else if (current != null)
				{
					current = current.next;
				}
				return current != null;
			}

			public void Reset()
			{
				if (version != list.version)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
				}
				start = true;
				current = null;
			}
		}

		private readonly ListDictionaryInternal list;

		private readonly bool isKeys;

		int ICollection.Count
		{
			get
			{
				int num = 0;
				for (DictionaryNode dictionaryNode = list.head; dictionaryNode != null; dictionaryNode = dictionaryNode.next)
				{
					num++;
				}
				return num;
			}
		}

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => list.SyncRoot;

		public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys)
		{
			this.list = list;
			this.isKeys = isKeys;
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < list.Count)
			{
				throw new ArgumentException(SR.ArgumentOutOfRange_Index, "index");
			}
			for (DictionaryNode dictionaryNode = list.head; dictionaryNode != null; dictionaryNode = dictionaryNode.next)
			{
				array.SetValue(isKeys ? dictionaryNode.key : dictionaryNode.value, index);
				index++;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new NodeKeyValueEnumerator(list, isKeys);
		}
	}

	[Serializable]
	private sealed class DictionaryNode
	{
		public object key;

		public object value;

		public DictionaryNode next;
	}

	private DictionaryNode head;

	private int version;

	private int count;

	public object? this[object key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", SR.ArgumentNull_Key);
			}
			for (DictionaryNode next = head; next != null; next = next.next)
			{
				if (next.key.Equals(key))
				{
					return next.value;
				}
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", SR.ArgumentNull_Key);
			}
			version++;
			DictionaryNode dictionaryNode = null;
			DictionaryNode next = head;
			while (next != null && !next.key.Equals(key))
			{
				dictionaryNode = next;
				next = next.next;
			}
			if (next != null)
			{
				next.value = value;
				return;
			}
			DictionaryNode dictionaryNode2 = new DictionaryNode();
			dictionaryNode2.key = key;
			dictionaryNode2.value = value;
			if (dictionaryNode != null)
			{
				dictionaryNode.next = dictionaryNode2;
			}
			else
			{
				head = dictionaryNode2;
			}
			count++;
		}
	}

	public int Count => count;

	public ICollection Keys => new NodeKeyValueCollection(this, isKeys: true);

	public bool IsReadOnly => false;

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public ICollection Values => new NodeKeyValueCollection(this, isKeys: false);

	public void Add(object key, object? value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", SR.ArgumentNull_Key);
		}
		version++;
		DictionaryNode dictionaryNode = null;
		for (DictionaryNode next = head; next != null; next = next.next)
		{
			if (next.key.Equals(key))
			{
				throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate__, next.key, key));
			}
			dictionaryNode = next;
		}
		DictionaryNode dictionaryNode2 = new DictionaryNode();
		dictionaryNode2.key = key;
		dictionaryNode2.value = value;
		if (dictionaryNode != null)
		{
			dictionaryNode.next = dictionaryNode2;
		}
		else
		{
			head = dictionaryNode2;
		}
		count++;
	}

	public void Clear()
	{
		count = 0;
		head = null;
		version++;
	}

	public bool Contains(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", SR.ArgumentNull_Key);
		}
		for (DictionaryNode next = head; next != null; next = next.next)
		{
			if (next.key.Equals(key))
			{
				return true;
			}
		}
		return false;
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_Index, "index");
		}
		for (DictionaryNode next = head; next != null; next = next.next)
		{
			array.SetValue(new DictionaryEntry(next.key, next.value), index);
			index++;
		}
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return new NodeEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new NodeEnumerator(this);
	}

	public void Remove(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", SR.ArgumentNull_Key);
		}
		version++;
		DictionaryNode dictionaryNode = null;
		DictionaryNode next = head;
		while (next != null && !next.key.Equals(key))
		{
			dictionaryNode = next;
			next = next.next;
		}
		if (next != null)
		{
			if (next == head)
			{
				head = next.next;
			}
			else
			{
				dictionaryNode.next = next.next;
			}
			count--;
		}
	}
}
