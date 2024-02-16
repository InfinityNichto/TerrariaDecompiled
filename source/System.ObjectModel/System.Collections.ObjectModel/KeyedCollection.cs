using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.ObjectModel;

[Serializable]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class KeyedCollection<TKey, TItem> : Collection<TItem> where TKey : notnull
{
	private readonly IEqualityComparer<TKey> comparer;

	private Dictionary<TKey, TItem> dict;

	private int keyCount;

	private readonly int threshold;

	private new List<TItem> Items => (List<TItem>)base.Items;

	public IEqualityComparer<TKey> Comparer => comparer;

	public TItem this[TKey key]
	{
		get
		{
			if (TryGetValue(key, out var item))
			{
				return item;
			}
			throw new KeyNotFoundException(System.SR.Format(System.SR.Arg_KeyNotFoundWithKey, key.ToString()));
		}
	}

	protected IDictionary<TKey, TItem>? Dictionary => dict;

	protected KeyedCollection()
		: this((IEqualityComparer<TKey>?)null, 0)
	{
	}

	protected KeyedCollection(IEqualityComparer<TKey>? comparer)
		: this(comparer, 0)
	{
	}

	protected KeyedCollection(IEqualityComparer<TKey>? comparer, int dictionaryCreationThreshold)
		: base((IList<TItem>)new List<TItem>())
	{
		if (dictionaryCreationThreshold < -1)
		{
			throw new ArgumentOutOfRangeException("dictionaryCreationThreshold", System.SR.ArgumentOutOfRange_InvalidThreshold);
		}
		this.comparer = comparer ?? EqualityComparer<TKey>.Default;
		threshold = ((dictionaryCreationThreshold == -1) ? int.MaxValue : dictionaryCreationThreshold);
	}

	public bool Contains(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (dict != null)
		{
			return dict.ContainsKey(key);
		}
		foreach (TItem item in Items)
		{
			if (comparer.Equals(GetKeyForItem(item), key))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TItem item)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (dict != null)
		{
			return dict.TryGetValue(key, out item);
		}
		foreach (TItem item2 in Items)
		{
			TKey keyForItem = GetKeyForItem(item2);
			if (keyForItem != null && comparer.Equals(key, keyForItem))
			{
				item = item2;
				return true;
			}
		}
		item = default(TItem);
		return false;
	}

	private bool ContainsItem(TItem item)
	{
		TKey keyForItem;
		if (dict == null || (keyForItem = GetKeyForItem(item)) == null)
		{
			return Items.Contains(item);
		}
		if (dict.TryGetValue(keyForItem, out var value))
		{
			return EqualityComparer<TItem>.Default.Equals(value, item);
		}
		return false;
	}

	public bool Remove(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (dict != null)
		{
			if (dict.TryGetValue(key, out var value))
			{
				return Remove(value);
			}
			return false;
		}
		for (int i = 0; i < Items.Count; i++)
		{
			if (comparer.Equals(GetKeyForItem(Items[i]), key))
			{
				RemoveItem(i);
				return true;
			}
		}
		return false;
	}

	protected void ChangeItemKey(TItem item, TKey newKey)
	{
		if (!ContainsItem(item))
		{
			throw new ArgumentException(System.SR.Argument_ItemNotExist, "item");
		}
		TKey keyForItem = GetKeyForItem(item);
		if (!comparer.Equals(keyForItem, newKey))
		{
			if (newKey != null)
			{
				AddKey(newKey, item);
			}
			if (keyForItem != null)
			{
				RemoveKey(keyForItem);
			}
		}
	}

	protected override void ClearItems()
	{
		base.ClearItems();
		dict?.Clear();
		keyCount = 0;
	}

	protected abstract TKey GetKeyForItem(TItem item);

	protected override void InsertItem(int index, TItem item)
	{
		TKey keyForItem = GetKeyForItem(item);
		if (keyForItem != null)
		{
			AddKey(keyForItem, item);
		}
		base.InsertItem(index, item);
	}

	protected override void RemoveItem(int index)
	{
		TKey keyForItem = GetKeyForItem(Items[index]);
		if (keyForItem != null)
		{
			RemoveKey(keyForItem);
		}
		base.RemoveItem(index);
	}

	protected override void SetItem(int index, TItem item)
	{
		TKey keyForItem = GetKeyForItem(item);
		TKey keyForItem2 = GetKeyForItem(Items[index]);
		if (comparer.Equals(keyForItem2, keyForItem))
		{
			if (keyForItem != null && dict != null)
			{
				dict[keyForItem] = item;
			}
		}
		else
		{
			if (keyForItem != null)
			{
				AddKey(keyForItem, item);
			}
			if (keyForItem2 != null)
			{
				RemoveKey(keyForItem2);
			}
		}
		base.SetItem(index, item);
	}

	private void AddKey(TKey key, TItem item)
	{
		if (dict != null)
		{
			dict.Add(key, item);
			return;
		}
		if (keyCount == threshold)
		{
			CreateDictionary();
			dict.Add(key, item);
			return;
		}
		if (Contains(key))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddingDuplicate, key), "key");
		}
		keyCount++;
	}

	private void CreateDictionary()
	{
		dict = new Dictionary<TKey, TItem>(comparer);
		foreach (TItem item in Items)
		{
			TKey keyForItem = GetKeyForItem(item);
			if (keyForItem != null)
			{
				dict.Add(keyForItem, item);
			}
		}
	}

	private void RemoveKey(TKey key)
	{
		if (dict != null)
		{
			dict.Remove(key);
		}
		else
		{
			keyCount--;
		}
	}
}
