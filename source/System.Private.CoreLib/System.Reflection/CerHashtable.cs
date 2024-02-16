using System.Collections;
using System.Threading;

namespace System.Reflection;

internal struct CerHashtable<K, V> where K : class
{
	private sealed class Table
	{
		internal K[] m_keys;

		internal V[] m_values;

		internal int m_count;

		internal Table(int size)
		{
			size = HashHelpers.GetPrime(size);
			m_keys = new K[size];
			m_values = new V[size];
		}

		internal void Insert(K key, V value)
		{
			int num = CerHashtable<K, V>.GetHashCodeHelper(key);
			if (num < 0)
			{
				num = ~num;
			}
			K[] keys = m_keys;
			int num2 = num % keys.Length;
			while (true)
			{
				K val = keys[num2];
				if (val == null)
				{
					break;
				}
				num2++;
				if (num2 >= keys.Length)
				{
					num2 -= keys.Length;
				}
			}
			m_count++;
			m_values[num2] = value;
			Volatile.Write(ref keys[num2], key);
		}
	}

	private Table m_Table;

	internal V this[K key]
	{
		get
		{
			Table table = Volatile.Read(ref m_Table);
			if (table == null)
			{
				return default(V);
			}
			int num = GetHashCodeHelper(key);
			if (num < 0)
			{
				num = ~num;
			}
			K[] keys = table.m_keys;
			int num2 = num % keys.Length;
			while (true)
			{
				K val = Volatile.Read(ref keys[num2]);
				if (val == null)
				{
					break;
				}
				if (val.Equals(key))
				{
					return table.m_values[num2];
				}
				num2++;
				if (num2 >= keys.Length)
				{
					num2 -= keys.Length;
				}
			}
			return default(V);
		}
		set
		{
			Table table = m_Table;
			if (table != null)
			{
				int num = 2 * (table.m_count + 1);
				if (num >= table.m_keys.Length)
				{
					Rehash(num);
				}
			}
			else
			{
				Rehash(7);
			}
			m_Table.Insert(key, value);
		}
	}

	private static int GetHashCodeHelper(K key)
	{
		if (!(key is string text))
		{
			return key.GetHashCode();
		}
		return text.GetNonRandomizedHashCode();
	}

	private void Rehash(int newSize)
	{
		Table table = new Table(newSize);
		Table table2 = m_Table;
		if (table2 != null)
		{
			K[] keys = table2.m_keys;
			V[] values = table2.m_values;
			for (int i = 0; i < keys.Length; i++)
			{
				K val = keys[i];
				if (val != null)
				{
					table.Insert(val, values[i]);
				}
			}
		}
		Volatile.Write(ref m_Table, table);
	}
}
