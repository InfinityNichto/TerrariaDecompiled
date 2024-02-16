using System.Collections;

namespace System.Xml.Serialization;

internal sealed class WorkItems
{
	private readonly ArrayList _list = new ArrayList();

	internal ImportStructWorkItem this[int index]
	{
		get
		{
			return (ImportStructWorkItem)_list[index];
		}
		set
		{
			_list[index] = value;
		}
	}

	internal int Count => _list.Count;

	internal void Add(ImportStructWorkItem item)
	{
		_list.Add(item);
	}

	internal bool Contains(StructMapping mapping)
	{
		return IndexOf(mapping) >= 0;
	}

	internal int IndexOf(StructMapping mapping)
	{
		for (int i = 0; i < Count; i++)
		{
			if (this[i].Mapping == mapping)
			{
				return i;
			}
		}
		return -1;
	}

	internal void RemoveAt(int index)
	{
		_list.RemoveAt(index);
	}
}
