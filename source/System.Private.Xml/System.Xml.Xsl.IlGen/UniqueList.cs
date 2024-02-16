using System.Collections.Generic;

namespace System.Xml.Xsl.IlGen;

internal sealed class UniqueList<T>
{
	private readonly Dictionary<T, int> _lookup = new Dictionary<T, int>();

	private readonly List<T> _list = new List<T>();

	public int Add(T value)
	{
		int num;
		if (!_lookup.ContainsKey(value))
		{
			num = _list.Count;
			_lookup.Add(value, num);
			_list.Add(value);
		}
		else
		{
			num = _lookup[value];
		}
		return num;
	}

	public T[] ToArray()
	{
		return _list.ToArray();
	}
}
