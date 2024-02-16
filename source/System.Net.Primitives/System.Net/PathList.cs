using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class PathList
{
	[Serializable]
	[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
	private sealed class PathListComparer : IComparer
	{
		internal static readonly PathListComparer StaticInstance = new PathListComparer();

		int IComparer.Compare(object ol, object or)
		{
			string text = CookieParser.CheckQuoted((string)ol);
			string text2 = CookieParser.CheckQuoted((string)or);
			int length = text.Length;
			int length2 = text2.Length;
			int num = Math.Min(length, length2);
			for (int i = 0; i < num; i++)
			{
				if (text[i] != text2[i])
				{
					return text[i] - text2[i];
				}
			}
			return length2 - length;
		}
	}

	private readonly SortedList m_list = SortedList.Synchronized(new SortedList(PathListComparer.StaticInstance));

	internal int Count => m_list.Count;

	internal ICollection Values => m_list.Values;

	internal object? this[string s]
	{
		get
		{
			lock (SyncRoot)
			{
				return m_list[s];
			}
		}
		set
		{
			lock (SyncRoot)
			{
				m_list[s] = value;
			}
		}
	}

	internal SortedList List => m_list;

	internal object SyncRoot => m_list.SyncRoot;

	internal int GetCookiesCount()
	{
		int num = 0;
		lock (SyncRoot)
		{
			IList valueList = m_list.GetValueList();
			int count = valueList.Count;
			for (int i = 0; i < count; i++)
			{
				num += ((CookieCollection)valueList[i]).Count;
			}
			return num;
		}
	}

	internal IDictionaryEnumerator GetEnumerator()
	{
		lock (SyncRoot)
		{
			return m_list.GetEnumerator();
		}
	}

	internal void Remove(object key)
	{
		lock (SyncRoot)
		{
			m_list.Remove(key);
		}
	}
}
