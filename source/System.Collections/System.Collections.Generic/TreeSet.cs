using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TreeSet<T> : SortedSet<T>
{
	public TreeSet()
	{
	}

	public TreeSet(IComparer<T>? comparer)
		: base(comparer)
	{
	}

	internal TreeSet(TreeSet<T> set, IComparer<T> comparer)
		: base((IEnumerable<T>)set, comparer)
	{
	}

	private TreeSet(SerializationInfo siInfo, StreamingContext context)
		: base(siInfo, context)
	{
	}

	internal override bool AddIfNotPresent(T item)
	{
		bool flag = base.AddIfNotPresent(item);
		if (!flag)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddingDuplicate, item));
		}
		return flag;
	}
}
