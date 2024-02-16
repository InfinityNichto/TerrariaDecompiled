using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CookieCollection : ICollection<Cookie>, IEnumerable<Cookie>, IEnumerable, IReadOnlyCollection<Cookie>, ICollection
{
	internal enum Stamp
	{
		Check,
		Set,
		SetToUnused,
		SetToMaxUsed
	}

	private readonly ArrayList m_list = new ArrayList();

	private int m_version;

	private DateTime m_TimeStamp = DateTime.MinValue;

	private bool m_has_other_versions;

	public Cookie this[int index]
	{
		get
		{
			if (index < 0 || index >= m_list.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return m_list[index] as Cookie;
		}
	}

	public Cookie? this[string name]
	{
		get
		{
			foreach (Cookie item in m_list)
			{
				if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
			return null;
		}
	}

	public bool IsReadOnly => false;

	public int Count => m_list.Count;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	internal bool IsOtherVersionSeen => m_has_other_versions;

	[OnSerializing]
	private void OnSerializing(StreamingContext context)
	{
		m_version = m_list.Count;
	}

	public void Add(Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		int num = IndexOf(cookie);
		if (num == -1)
		{
			m_list.Add(cookie);
		}
		else
		{
			m_list[num] = cookie;
		}
	}

	public void Add(CookieCollection cookies)
	{
		if (cookies == null)
		{
			throw new ArgumentNullException("cookies");
		}
		foreach (Cookie item in cookies.m_list)
		{
			Add(item);
		}
	}

	public void Clear()
	{
		m_list.Clear();
	}

	public bool Contains(Cookie cookie)
	{
		return IndexOf(cookie) >= 0;
	}

	public bool Remove(Cookie cookie)
	{
		int num = IndexOf(cookie);
		if (num == -1)
		{
			return false;
		}
		m_list.RemoveAt(num);
		return true;
	}

	public void CopyTo(Array array, int index)
	{
		((ICollection)m_list).CopyTo(array, index);
	}

	public void CopyTo(Cookie[] array, int index)
	{
		m_list.CopyTo(array, index);
	}

	internal DateTime TimeStamp(Stamp how)
	{
		switch (how)
		{
		case Stamp.Set:
			m_TimeStamp = DateTime.Now;
			break;
		case Stamp.SetToMaxUsed:
			m_TimeStamp = DateTime.MaxValue;
			break;
		case Stamp.SetToUnused:
			m_TimeStamp = DateTime.MinValue;
			break;
		}
		return m_TimeStamp;
	}

	internal int InternalAdd(Cookie cookie, bool isStrict)
	{
		int result = 1;
		if (isStrict)
		{
			int num = 0;
			int count = m_list.Count;
			for (int i = 0; i < count; i++)
			{
				Cookie cookie2 = (Cookie)m_list[i];
				if (CookieComparer.Compare(cookie, cookie2) == 0)
				{
					result = 0;
					if (cookie2.Variant <= cookie.Variant)
					{
						m_list[num] = cookie;
					}
					break;
				}
				num++;
			}
			if (num == m_list.Count)
			{
				m_list.Add(cookie);
			}
		}
		else
		{
			m_list.Add(cookie);
		}
		if (cookie.Version != 1)
		{
			m_has_other_versions = true;
		}
		return result;
	}

	internal int IndexOf(Cookie cookie)
	{
		int num = 0;
		foreach (Cookie item in m_list)
		{
			if (CookieComparer.Compare(cookie, item) == 0)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	internal void RemoveAt(int idx)
	{
		m_list.RemoveAt(idx);
	}

	IEnumerator<Cookie> IEnumerable<Cookie>.GetEnumerator()
	{
		foreach (Cookie item in m_list)
		{
			yield return item;
		}
	}

	public IEnumerator GetEnumerator()
	{
		return m_list.GetEnumerator();
	}
}
