using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

public class TraceListenerCollection : IList, ICollection, IEnumerable
{
	private readonly List<TraceListener> _list;

	public TraceListener this[int i]
	{
		get
		{
			return _list[i];
		}
		set
		{
			InitializeListener(value);
			_list[i] = value;
		}
	}

	public TraceListener? this[string name]
	{
		get
		{
			IEnumerator enumerator = GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					TraceListener traceListener = (TraceListener)enumerator.Current;
					if (traceListener.Name == name)
					{
						return traceListener;
					}
				}
			}
			finally
			{
				IDisposable disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			return null;
		}
	}

	public int Count => _list.Count;

	object? IList.this[int index]
	{
		get
		{
			return _list[index];
		}
		set
		{
			if (!(value is TraceListener traceListener))
			{
				throw new ArgumentException(System.SR.MustAddListener, "value");
			}
			InitializeListener(traceListener);
			_list[index] = traceListener;
		}
	}

	bool IList.IsReadOnly => false;

	bool IList.IsFixedSize => false;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => true;

	internal TraceListenerCollection()
	{
		_list = new List<TraceListener>(1);
	}

	public int Add(TraceListener listener)
	{
		InitializeListener(listener);
		lock (TraceInternal.critSec)
		{
			return ((IList)_list).Add((object?)listener);
		}
	}

	public void AddRange(TraceListener[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		for (int i = 0; i < value.Length; i++)
		{
			Add(value[i]);
		}
	}

	public void AddRange(TraceListenerCollection value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int count = value.Count;
		for (int i = 0; i < count; i++)
		{
			Add(value[i]);
		}
	}

	public void Clear()
	{
		_list.Clear();
	}

	public bool Contains(TraceListener? listener)
	{
		return ((IList)this).Contains((object?)listener);
	}

	public void CopyTo(TraceListener[] listeners, int index)
	{
		((ICollection)this).CopyTo((Array)listeners, index);
	}

	public IEnumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	internal void InitializeListener(TraceListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		listener.IndentSize = TraceInternal.IndentSize;
		listener.IndentLevel = TraceInternal.IndentLevel;
	}

	public int IndexOf(TraceListener? listener)
	{
		return ((IList)this).IndexOf((object?)listener);
	}

	public void Insert(int index, TraceListener listener)
	{
		InitializeListener(listener);
		lock (TraceInternal.critSec)
		{
			_list.Insert(index, listener);
		}
	}

	public void Remove(TraceListener? listener)
	{
		((IList)this).Remove((object?)listener);
	}

	public void Remove(string name)
	{
		TraceListener traceListener = this[name];
		if (traceListener != null)
		{
			((IList)this).Remove((object?)traceListener);
		}
	}

	public void RemoveAt(int index)
	{
		lock (TraceInternal.critSec)
		{
			_list.RemoveAt(index);
		}
	}

	int IList.Add(object value)
	{
		if (!(value is TraceListener listener))
		{
			throw new ArgumentException(System.SR.MustAddListener, "value");
		}
		InitializeListener(listener);
		lock (TraceInternal.critSec)
		{
			return ((IList)_list).Add(value);
		}
	}

	bool IList.Contains(object value)
	{
		return _list.Contains((TraceListener)value);
	}

	int IList.IndexOf(object value)
	{
		return _list.IndexOf((TraceListener)value);
	}

	void IList.Insert(int index, object value)
	{
		if (!(value is TraceListener listener))
		{
			throw new ArgumentException(System.SR.MustAddListener, "value");
		}
		InitializeListener(listener);
		lock (TraceInternal.critSec)
		{
			_list.Insert(index, (TraceListener)value);
		}
	}

	void IList.Remove(object value)
	{
		lock (TraceInternal.critSec)
		{
			_list.Remove((TraceListener)value);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_list).CopyTo(array, index);
	}
}
