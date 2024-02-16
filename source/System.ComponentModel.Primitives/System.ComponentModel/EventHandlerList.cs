namespace System.ComponentModel;

public sealed class EventHandlerList : IDisposable
{
	private sealed class ListEntry
	{
		internal readonly ListEntry _next;

		internal readonly object _key;

		internal Delegate _handler;

		public ListEntry(object key, Delegate handler, ListEntry next)
		{
			_next = next;
			_key = key;
			_handler = handler;
		}
	}

	private ListEntry _head;

	private readonly Component _parent;

	public Delegate? this[object key]
	{
		get
		{
			ListEntry listEntry = null;
			if (_parent == null || _parent.CanRaiseEventsInternal)
			{
				listEntry = Find(key);
			}
			return listEntry?._handler;
		}
		set
		{
			ListEntry listEntry = Find(key);
			if (listEntry != null)
			{
				listEntry._handler = value;
			}
			else
			{
				_head = new ListEntry(key, value, _head);
			}
		}
	}

	internal EventHandlerList(Component parent)
	{
		_parent = parent;
	}

	public EventHandlerList()
	{
	}

	public void AddHandler(object key, Delegate? value)
	{
		ListEntry listEntry = Find(key);
		if (listEntry != null)
		{
			listEntry._handler = Delegate.Combine(listEntry._handler, value);
		}
		else
		{
			_head = new ListEntry(key, value, _head);
		}
	}

	public void AddHandlers(EventHandlerList listToAddFrom)
	{
		if (listToAddFrom == null)
		{
			throw new ArgumentNullException("listToAddFrom");
		}
		for (ListEntry listEntry = listToAddFrom._head; listEntry != null; listEntry = listEntry._next)
		{
			AddHandler(listEntry._key, listEntry._handler);
		}
	}

	public void Dispose()
	{
		_head = null;
	}

	private ListEntry Find(object key)
	{
		ListEntry listEntry = _head;
		while (listEntry != null && listEntry._key != key)
		{
			listEntry = listEntry._next;
		}
		return listEntry;
	}

	public void RemoveHandler(object key, Delegate? value)
	{
		ListEntry listEntry = Find(key);
		if (listEntry != null)
		{
			listEntry._handler = Delegate.Remove(listEntry._handler, value);
		}
	}
}
