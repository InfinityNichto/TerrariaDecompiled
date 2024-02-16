using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace System.Data;

public class InternalDataCollectionBase : ICollection, IEnumerable
{
	internal static readonly CollectionChangeEventArgs s_refreshEventArgs = new CollectionChangeEventArgs(CollectionChangeAction.Refresh, null);

	[Browsable(false)]
	public virtual int Count => List.Count;

	[Browsable(false)]
	public bool IsReadOnly => false;

	[Browsable(false)]
	public bool IsSynchronized => false;

	[Browsable(false)]
	public object SyncRoot => this;

	protected virtual ArrayList List => null;

	public virtual void CopyTo(Array ar, int index)
	{
		List.CopyTo(ar, index);
	}

	public virtual IEnumerator GetEnumerator()
	{
		return List.GetEnumerator();
	}

	internal int NamesEqual(string s1, string s2, bool fCaseSensitive, CultureInfo locale)
	{
		if (fCaseSensitive)
		{
			if (string.Compare(s1, s2, ignoreCase: false, locale) != 0)
			{
				return 0;
			}
			return 1;
		}
		if (locale.CompareInfo.Compare(s1, s2, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
		{
			if (string.Compare(s1, s2, ignoreCase: false, locale) != 0)
			{
				return -1;
			}
			return 1;
		}
		return 0;
	}
}
