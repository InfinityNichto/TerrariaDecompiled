namespace System.Xml;

public class NameTable : XmlNameTable
{
	private sealed class Entry
	{
		internal string str;

		internal int hashCode;

		internal Entry next;

		internal Entry(string str, int hashCode, Entry next)
		{
			this.str = str;
			this.hashCode = hashCode;
			this.next = next;
		}
	}

	private Entry[] _entries;

	private int _count;

	private int _mask;

	public NameTable()
	{
		_mask = 31;
		_entries = new Entry[_mask + 1];
	}

	public override string Add(string key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (key.Length == 0)
		{
			return string.Empty;
		}
		int num = ComputeHash32(key);
		for (Entry entry = _entries[num & _mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == num && entry.str.Equals(key))
			{
				return entry.str;
			}
		}
		return AddEntry(key, num);
	}

	public override string Add(char[] key, int start, int len)
	{
		if (len == 0)
		{
			return string.Empty;
		}
		if (start >= key.Length || start < 0 || (long)start + (long)len > key.Length)
		{
			throw new IndexOutOfRangeException();
		}
		if (len < 0)
		{
			throw new ArgumentOutOfRangeException("len");
		}
		int hashCode = string.GetHashCode(key.AsSpan(start, len));
		for (Entry entry = _entries[hashCode & _mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == hashCode && entry.str.AsSpan().SequenceEqual(key.AsSpan(start, len)))
			{
				return entry.str;
			}
		}
		return AddEntry(new string(key, start, len), hashCode);
	}

	public override string? Get(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (value.Length == 0)
		{
			return string.Empty;
		}
		int num = ComputeHash32(value);
		for (Entry entry = _entries[num & _mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == num && entry.str.Equals(value))
			{
				return entry.str;
			}
		}
		return null;
	}

	public override string? Get(char[] key, int start, int len)
	{
		if (len == 0)
		{
			return string.Empty;
		}
		if (start >= key.Length || start < 0 || (long)start + (long)len > key.Length)
		{
			throw new IndexOutOfRangeException();
		}
		if (len < 0)
		{
			return null;
		}
		int hashCode = string.GetHashCode(key.AsSpan(start, len));
		for (Entry entry = _entries[hashCode & _mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == hashCode && entry.str.AsSpan().SequenceEqual(key.AsSpan(start, len)))
			{
				return entry.str;
			}
		}
		return null;
	}

	internal string GetOrAddEntry(string str, int hashCode)
	{
		for (Entry entry = _entries[hashCode & _mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == hashCode && entry.str.Equals(str))
			{
				return entry.str;
			}
		}
		return AddEntry(str, hashCode);
	}

	internal static int ComputeHash32(string key)
	{
		return string.GetHashCode(key.AsSpan());
	}

	private string AddEntry(string str, int hashCode)
	{
		int num = hashCode & _mask;
		Entry entry = new Entry(str, hashCode, _entries[num]);
		_entries[num] = entry;
		if (_count++ == _mask)
		{
			Grow();
		}
		return entry.str;
	}

	private void Grow()
	{
		int num = _mask * 2 + 1;
		Entry[] entries = _entries;
		Entry[] array = new Entry[num + 1];
		for (int i = 0; i < entries.Length; i++)
		{
			Entry entry = entries[i];
			while (entry != null)
			{
				int num2 = entry.hashCode & num;
				Entry next = entry.next;
				entry.next = array[num2];
				array[num2] = entry;
				entry = next;
			}
		}
		_entries = array;
		_mask = num;
	}
}
