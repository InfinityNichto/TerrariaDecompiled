using System.Collections.Generic;

namespace System.Data;

internal sealed class Listeners<TElem> where TElem : class
{
	internal delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

	internal delegate TResult Func<T1, TResult>(T1 arg1);

	private readonly List<TElem> _listeners;

	private readonly Func<TElem, bool> _filter;

	private readonly int _objectID;

	private int _listenerReaderCount;

	internal bool HasListeners => 0 < _listeners.Count;

	internal Listeners(int ObjectID, Func<TElem, bool> notifyFilter)
	{
		_listeners = new List<TElem>();
		_filter = notifyFilter;
		_objectID = ObjectID;
		_listenerReaderCount = 0;
	}

	internal void Add(TElem listener)
	{
		_listeners.Add(listener);
	}

	internal int IndexOfReference(TElem listener)
	{
		return Index.IndexOfReference(_listeners, listener);
	}

	internal void Remove(TElem listener)
	{
		int index = IndexOfReference(listener);
		_listeners[index] = null;
		if (_listenerReaderCount == 0)
		{
			_listeners.RemoveAt(index);
			_listeners.TrimExcess();
		}
	}

	internal void Notify<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<TElem, T1, T2, T3> action)
	{
		int count = _listeners.Count;
		if (0 >= count)
		{
			return;
		}
		int nullIndex = -1;
		_listenerReaderCount++;
		try
		{
			for (int i = 0; i < count; i++)
			{
				TElem arg4 = _listeners[i];
				if (_filter(arg4))
				{
					action(arg4, arg1, arg2, arg3);
					continue;
				}
				_listeners[i] = null;
				nullIndex = i;
			}
		}
		finally
		{
			_listenerReaderCount--;
		}
		if (_listenerReaderCount == 0)
		{
			RemoveNullListeners(nullIndex);
		}
	}

	private void RemoveNullListeners(int nullIndex)
	{
		int num = nullIndex;
		while (0 <= num)
		{
			if (_listeners[num] == null)
			{
				_listeners.RemoveAt(num);
			}
			num--;
		}
	}
}
