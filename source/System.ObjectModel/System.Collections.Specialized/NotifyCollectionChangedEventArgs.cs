namespace System.Collections.Specialized;

public class NotifyCollectionChangedEventArgs : EventArgs
{
	private readonly NotifyCollectionChangedAction _action;

	private readonly IList _newItems;

	private readonly IList _oldItems;

	private readonly int _newStartingIndex = -1;

	private readonly int _oldStartingIndex = -1;

	public NotifyCollectionChangedAction Action => _action;

	public IList? NewItems => _newItems;

	public IList? OldItems => _oldItems;

	public int NewStartingIndex => _newStartingIndex;

	public int OldStartingIndex => _oldStartingIndex;

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
	{
		if (action != NotifyCollectionChangedAction.Reset)
		{
			throw new ArgumentException(System.SR.Format(System.SR.WrongActionForCtor, NotifyCollectionChangedAction.Reset), "action");
		}
		_action = action;
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem)
		: this(action, changedItem, -1)
	{
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index)
	{
		switch (action)
		{
		case NotifyCollectionChangedAction.Reset:
			if (changedItem != null)
			{
				throw new ArgumentException(System.SR.ResetActionRequiresNullItem, "action");
			}
			if (index != -1)
			{
				throw new ArgumentException(System.SR.ResetActionRequiresIndexMinus1, "action");
			}
			break;
		case NotifyCollectionChangedAction.Add:
			_newItems = new SingleItemReadOnlyList(changedItem);
			_newStartingIndex = index;
			break;
		case NotifyCollectionChangedAction.Remove:
			_oldItems = new SingleItemReadOnlyList(changedItem);
			_oldStartingIndex = index;
			break;
		default:
			throw new ArgumentException(System.SR.MustBeResetAddOrRemoveActionForCtor, "action");
		}
		_action = action;
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems)
		: this(action, changedItems, -1)
	{
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int startingIndex)
	{
		switch (action)
		{
		case NotifyCollectionChangedAction.Reset:
			if (changedItems != null)
			{
				throw new ArgumentException(System.SR.ResetActionRequiresNullItem, "action");
			}
			if (startingIndex != -1)
			{
				throw new ArgumentException(System.SR.ResetActionRequiresIndexMinus1, "action");
			}
			break;
		case NotifyCollectionChangedAction.Add:
		case NotifyCollectionChangedAction.Remove:
			if (changedItems == null)
			{
				throw new ArgumentNullException("changedItems");
			}
			if (startingIndex < -1)
			{
				throw new ArgumentException(System.SR.IndexCannotBeNegative, "startingIndex");
			}
			if (action == NotifyCollectionChangedAction.Add)
			{
				_newItems = new ReadOnlyList(changedItems);
				_newStartingIndex = startingIndex;
			}
			else
			{
				_oldItems = new ReadOnlyList(changedItems);
				_oldStartingIndex = startingIndex;
			}
			break;
		default:
			throw new ArgumentException(System.SR.MustBeResetAddOrRemoveActionForCtor, "action");
		}
		_action = action;
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem)
		: this(action, newItem, oldItem, -1)
	{
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? newItem, object? oldItem, int index)
	{
		if (action != NotifyCollectionChangedAction.Replace)
		{
			throw new ArgumentException(System.SR.Format(System.SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), "action");
		}
		_action = action;
		_newItems = new SingleItemReadOnlyList(newItem);
		_oldItems = new SingleItemReadOnlyList(oldItem);
		_newStartingIndex = (_oldStartingIndex = index);
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
		: this(action, newItems, oldItems, -1)
	{
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex)
	{
		if (action != NotifyCollectionChangedAction.Replace)
		{
			throw new ArgumentException(System.SR.Format(System.SR.WrongActionForCtor, NotifyCollectionChangedAction.Replace), "action");
		}
		if (newItems == null)
		{
			throw new ArgumentNullException("newItems");
		}
		if (oldItems == null)
		{
			throw new ArgumentNullException("oldItems");
		}
		_action = action;
		_newItems = new ReadOnlyList(newItems);
		_oldItems = new ReadOnlyList(oldItems);
		_newStartingIndex = (_oldStartingIndex = startingIndex);
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? changedItem, int index, int oldIndex)
	{
		if (action != NotifyCollectionChangedAction.Move)
		{
			throw new ArgumentException(System.SR.Format(System.SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), "action");
		}
		if (index < 0)
		{
			throw new ArgumentException(System.SR.IndexCannotBeNegative, "index");
		}
		_action = action;
		_newItems = (_oldItems = new SingleItemReadOnlyList(changedItem));
		_newStartingIndex = index;
		_oldStartingIndex = oldIndex;
	}

	public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList? changedItems, int index, int oldIndex)
	{
		if (action != NotifyCollectionChangedAction.Move)
		{
			throw new ArgumentException(System.SR.Format(System.SR.WrongActionForCtor, NotifyCollectionChangedAction.Move), "action");
		}
		if (index < 0)
		{
			throw new ArgumentException(System.SR.IndexCannotBeNegative, "index");
		}
		_action = action;
		_newItems = (_oldItems = ((changedItems != null) ? new ReadOnlyList(changedItems) : null));
		_newStartingIndex = index;
		_oldStartingIndex = oldIndex;
	}
}
