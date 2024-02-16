using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Tracing;

internal sealed class EventPayload : IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
{
	private readonly string[] m_names;

	private readonly object[] m_values;

	public ICollection<string> Keys => m_names;

	public ICollection<object> Values => m_values;

	public object this[string key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = 0;
			string[] names = m_names;
			foreach (string text in names)
			{
				if (text == key)
				{
					return m_values[num];
				}
				num++;
			}
			throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key));
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public int Count => m_names.Length;

	public bool IsReadOnly => true;

	internal EventPayload(string[] payloadNames, object[] payloadValues)
	{
		m_names = payloadNames;
		m_values = payloadValues;
	}

	public void Add(string key, object value)
	{
		throw new NotSupportedException();
	}

	public void Add(KeyValuePair<string, object> payloadEntry)
	{
		throw new NotSupportedException();
	}

	public void Clear()
	{
		throw new NotSupportedException();
	}

	public bool Contains(KeyValuePair<string, object> entry)
	{
		return ContainsKey(entry.Key);
	}

	public bool ContainsKey(string key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		string[] names = m_names;
		foreach (string text in names)
		{
			if (text == key)
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		for (int i = 0; i < Keys.Count; i++)
		{
			yield return new KeyValuePair<string, object>(m_names[i], m_values[i]);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
	}

	public void CopyTo(KeyValuePair<string, object>[] payloadEntries, int count)
	{
		throw new NotSupportedException();
	}

	public bool Remove(string key)
	{
		throw new NotSupportedException();
	}

	public bool Remove(KeyValuePair<string, object> entry)
	{
		throw new NotSupportedException();
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = 0;
		string[] names = m_names;
		foreach (string text in names)
		{
			if (text == key)
			{
				value = m_values[num];
				return true;
			}
			num++;
		}
		value = null;
		return false;
	}
}
