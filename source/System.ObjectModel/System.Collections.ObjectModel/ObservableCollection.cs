using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.ObjectModel;

[Serializable]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
public class ObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
	[Serializable]
	[TypeForwardedFrom("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
	private sealed class SimpleMonitor : IDisposable
	{
		internal int _busyCount;

		[NonSerialized]
		internal ObservableCollection<T> _collection;

		public SimpleMonitor(ObservableCollection<T> collection)
		{
			_collection = collection;
		}

		public void Dispose()
		{
			_collection._blockReentrancyCount--;
		}
	}

	private SimpleMonitor _monitor;

	[NonSerialized]
	private int _blockReentrancyCount;

	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add
		{
			PropertyChanged += value;
		}
		remove
		{
			PropertyChanged -= value;
		}
	}

	public virtual event NotifyCollectionChangedEventHandler? CollectionChanged;

	protected virtual event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection()
	{
	}

	public ObservableCollection(IEnumerable<T> collection)
		: base((IList<T>)CreateCopy(collection, "collection"))
	{
	}

	public ObservableCollection(List<T> list)
		: base((IList<T>)CreateCopy(list, "list"))
	{
	}

	private static List<T> CreateCopy(IEnumerable<T> collection, string paramName)
	{
		if (collection == null)
		{
			throw new ArgumentNullException(paramName);
		}
		return new List<T>(collection);
	}

	public void Move(int oldIndex, int newIndex)
	{
		MoveItem(oldIndex, newIndex);
	}

	protected override void ClearItems()
	{
		CheckReentrancy();
		base.ClearItems();
		OnCountPropertyChanged();
		OnIndexerPropertyChanged();
		OnCollectionReset();
	}

	protected override void RemoveItem(int index)
	{
		CheckReentrancy();
		T val = base[index];
		base.RemoveItem(index);
		OnCountPropertyChanged();
		OnIndexerPropertyChanged();
		OnCollectionChanged(NotifyCollectionChangedAction.Remove, val, index);
	}

	protected override void InsertItem(int index, T item)
	{
		CheckReentrancy();
		base.InsertItem(index, item);
		OnCountPropertyChanged();
		OnIndexerPropertyChanged();
		OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
	}

	protected override void SetItem(int index, T item)
	{
		CheckReentrancy();
		T val = base[index];
		base.SetItem(index, item);
		OnIndexerPropertyChanged();
		OnCollectionChanged(NotifyCollectionChangedAction.Replace, val, item, index);
	}

	protected virtual void MoveItem(int oldIndex, int newIndex)
	{
		CheckReentrancy();
		T val = base[oldIndex];
		base.RemoveItem(oldIndex);
		base.InsertItem(newIndex, val);
		OnIndexerPropertyChanged();
		OnCollectionChanged(NotifyCollectionChangedAction.Move, val, newIndex, oldIndex);
	}

	protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		this.PropertyChanged?.Invoke(this, e);
	}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		NotifyCollectionChangedEventHandler collectionChanged = this.CollectionChanged;
		if (collectionChanged != null)
		{
			_blockReentrancyCount++;
			try
			{
				collectionChanged(this, e);
			}
			finally
			{
				_blockReentrancyCount--;
			}
		}
	}

	protected IDisposable BlockReentrancy()
	{
		_blockReentrancyCount++;
		return EnsureMonitorInitialized();
	}

	protected void CheckReentrancy()
	{
		if (_blockReentrancyCount > 0)
		{
			NotifyCollectionChangedEventHandler collectionChanged = this.CollectionChanged;
			if (collectionChanged != null && collectionChanged.GetInvocationList().Length > 1)
			{
				throw new InvalidOperationException(System.SR.ObservableCollectionReentrancyNotAllowed);
			}
		}
	}

	private void OnCountPropertyChanged()
	{
		OnPropertyChanged(EventArgsCache.CountPropertyChanged);
	}

	private void OnIndexerPropertyChanged()
	{
		OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
	}

	private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
	}

	private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
	}

	private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
	{
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
	}

	private void OnCollectionReset()
	{
		OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
	}

	private SimpleMonitor EnsureMonitorInitialized()
	{
		return _monitor ?? (_monitor = new SimpleMonitor(this));
	}

	[OnSerializing]
	private void OnSerializing(StreamingContext context)
	{
		EnsureMonitorInitialized();
		_monitor._busyCount = _blockReentrancyCount;
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext context)
	{
		if (_monitor != null)
		{
			_blockReentrancyCount = _monitor._busyCount;
			_monitor._collection = this;
		}
	}
}
