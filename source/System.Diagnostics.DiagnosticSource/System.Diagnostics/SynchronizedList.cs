using System.Collections.Generic;

namespace System.Diagnostics;

internal sealed class SynchronizedList<T>
{
	private readonly List<T> _list;

	private uint _version;

	public int Count => _list.Count;

	public SynchronizedList()
	{
		_list = new List<T>();
	}

	public void Add(T item)
	{
		lock (_list)
		{
			_list.Add(item);
			_version++;
		}
	}

	public bool AddIfNotExist(T item)
	{
		lock (_list)
		{
			if (!_list.Contains(item))
			{
				_list.Add(item);
				_version++;
				return true;
			}
			return false;
		}
	}

	public bool Remove(T item)
	{
		lock (_list)
		{
			if (_list.Remove(item))
			{
				_version++;
				return true;
			}
			return false;
		}
	}

	public void EnumWithFunc<TParent>(ActivitySource.Function<T, TParent> func, ref ActivityCreationOptions<TParent> data, ref ActivitySamplingResult samplingResult, ref ActivityCreationOptions<ActivityContext> dataWithContext)
	{
		uint version = _version;
		int num = 0;
		while (num < _list.Count)
		{
			T item;
			lock (_list)
			{
				if (version != _version)
				{
					version = _version;
					num = 0;
					continue;
				}
				item = _list[num];
				num++;
			}
			func(item, ref data, ref samplingResult, ref dataWithContext);
		}
	}

	public void EnumWithAction(Action<T, object> action, object arg)
	{
		uint version = _version;
		int num = 0;
		while (num < _list.Count)
		{
			T arg2;
			lock (_list)
			{
				if (version != _version)
				{
					version = _version;
					num = 0;
					continue;
				}
				arg2 = _list[num];
				num++;
			}
			action(arg2, arg);
		}
	}
}
