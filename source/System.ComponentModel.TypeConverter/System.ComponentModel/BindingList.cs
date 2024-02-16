using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class BindingList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : Collection<T>, IBindingList, IList, ICollection, IEnumerable, ICancelAddNew, IRaiseItemChangedEvents
{
	private int addNewPos = -1;

	private bool raiseListChangedEvents = true;

	private bool raiseItemChangedEvents;

	[NonSerialized]
	private PropertyDescriptorCollection _itemTypeProperties;

	[NonSerialized]
	private PropertyChangedEventHandler _propertyChangedEventHandler;

	[NonSerialized]
	private AddingNewEventHandler _onAddingNew;

	[NonSerialized]
	private ListChangedEventHandler _onListChanged;

	[NonSerialized]
	private int _lastChangeIndex = -1;

	private bool allowNew = true;

	private bool allowEdit = true;

	private bool allowRemove = true;

	private bool userSetAllowNew;

	private bool ItemTypeHasDefaultConstructor
	{
		get
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle.IsPrimitive)
			{
				return true;
			}
			return typeFromHandle.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, Type.EmptyTypes, null) != null;
		}
	}

	public bool RaiseListChangedEvents
	{
		get
		{
			return raiseListChangedEvents;
		}
		set
		{
			raiseListChangedEvents = value;
		}
	}

	private bool AddingNewHandled
	{
		get
		{
			if (_onAddingNew != null)
			{
				return _onAddingNew.GetInvocationList().Length != 0;
			}
			return false;
		}
	}

	public bool AllowNew
	{
		get
		{
			if (userSetAllowNew || allowNew)
			{
				return allowNew;
			}
			return AddingNewHandled;
		}
		set
		{
			bool flag = AllowNew;
			userSetAllowNew = true;
			allowNew = value;
			if (flag != value)
			{
				FireListChanged(ListChangedType.Reset, -1);
			}
		}
	}

	bool IBindingList.AllowNew => AllowNew;

	public bool AllowEdit
	{
		get
		{
			return allowEdit;
		}
		set
		{
			if (allowEdit != value)
			{
				allowEdit = value;
				FireListChanged(ListChangedType.Reset, -1);
			}
		}
	}

	bool IBindingList.AllowEdit => AllowEdit;

	public bool AllowRemove
	{
		get
		{
			return allowRemove;
		}
		set
		{
			if (allowRemove != value)
			{
				allowRemove = value;
				FireListChanged(ListChangedType.Reset, -1);
			}
		}
	}

	bool IBindingList.AllowRemove => AllowRemove;

	bool IBindingList.SupportsChangeNotification => SupportsChangeNotificationCore;

	protected virtual bool SupportsChangeNotificationCore => true;

	bool IBindingList.SupportsSearching => SupportsSearchingCore;

	protected virtual bool SupportsSearchingCore => false;

	bool IBindingList.SupportsSorting => SupportsSortingCore;

	protected virtual bool SupportsSortingCore => false;

	bool IBindingList.IsSorted => IsSortedCore;

	protected virtual bool IsSortedCore => false;

	PropertyDescriptor? IBindingList.SortProperty => SortPropertyCore;

	protected virtual PropertyDescriptor? SortPropertyCore => null;

	ListSortDirection IBindingList.SortDirection => SortDirectionCore;

	protected virtual ListSortDirection SortDirectionCore => ListSortDirection.Ascending;

	bool IRaiseItemChangedEvents.RaisesItemChangedEvents => raiseItemChangedEvents;

	public event AddingNewEventHandler AddingNew
	{
		add
		{
			bool flag = AllowNew;
			_onAddingNew = (AddingNewEventHandler)Delegate.Combine(_onAddingNew, value);
			if (flag != AllowNew)
			{
				FireListChanged(ListChangedType.Reset, -1);
			}
		}
		remove
		{
			bool flag = AllowNew;
			_onAddingNew = (AddingNewEventHandler)Delegate.Remove(_onAddingNew, value);
			if (flag != AllowNew)
			{
				FireListChanged(ListChangedType.Reset, -1);
			}
		}
	}

	public event ListChangedEventHandler ListChanged
	{
		add
		{
			_onListChanged = (ListChangedEventHandler)Delegate.Combine(_onListChanged, value);
		}
		remove
		{
			_onListChanged = (ListChangedEventHandler)Delegate.Remove(_onListChanged, value);
		}
	}

	[RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
	public BindingList()
	{
		Initialize();
	}

	[RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
	public BindingList(IList<T> list)
		: base(list)
	{
		Initialize();
	}

	[RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
	private void Initialize()
	{
		allowNew = ItemTypeHasDefaultConstructor;
		if (!typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T)))
		{
			return;
		}
		raiseItemChangedEvents = true;
		foreach (T item in base.Items)
		{
			HookPropertyChanged(item);
		}
	}

	protected virtual void OnAddingNew(AddingNewEventArgs e)
	{
		_onAddingNew?.Invoke(this, e);
	}

	private object FireAddingNew()
	{
		AddingNewEventArgs addingNewEventArgs = new AddingNewEventArgs(null);
		OnAddingNew(addingNewEventArgs);
		return addingNewEventArgs.NewObject;
	}

	protected virtual void OnListChanged(ListChangedEventArgs e)
	{
		_onListChanged?.Invoke(this, e);
	}

	public void ResetBindings()
	{
		FireListChanged(ListChangedType.Reset, -1);
	}

	public void ResetItem(int position)
	{
		FireListChanged(ListChangedType.ItemChanged, position);
	}

	private void FireListChanged(ListChangedType type, int index)
	{
		if (raiseListChangedEvents)
		{
			OnListChanged(new ListChangedEventArgs(type, index));
		}
	}

	protected override void ClearItems()
	{
		EndNew(addNewPos);
		if (raiseItemChangedEvents)
		{
			foreach (T item in base.Items)
			{
				UnhookPropertyChanged(item);
			}
		}
		base.ClearItems();
		FireListChanged(ListChangedType.Reset, -1);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "BindingList ctor is marked with RequiresUnreferencedCode.")]
	protected override void InsertItem(int index, T item)
	{
		EndNew(addNewPos);
		base.InsertItem(index, item);
		if (raiseItemChangedEvents)
		{
			HookPropertyChanged(item);
		}
		FireListChanged(ListChangedType.ItemAdded, index);
	}

	protected override void RemoveItem(int index)
	{
		if (!allowRemove && (addNewPos < 0 || addNewPos != index))
		{
			throw new NotSupportedException();
		}
		EndNew(addNewPos);
		if (raiseItemChangedEvents)
		{
			UnhookPropertyChanged(base[index]);
		}
		base.RemoveItem(index);
		FireListChanged(ListChangedType.ItemDeleted, index);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "BindingList ctor is marked with RequiresUnreferencedCode.")]
	protected override void SetItem(int index, T item)
	{
		if (raiseItemChangedEvents)
		{
			UnhookPropertyChanged(base[index]);
		}
		base.SetItem(index, item);
		if (raiseItemChangedEvents)
		{
			HookPropertyChanged(item);
		}
		FireListChanged(ListChangedType.ItemChanged, index);
	}

	public virtual void CancelNew(int itemIndex)
	{
		if (addNewPos >= 0 && addNewPos == itemIndex)
		{
			RemoveItem(addNewPos);
			addNewPos = -1;
		}
	}

	public virtual void EndNew(int itemIndex)
	{
		if (addNewPos >= 0 && addNewPos == itemIndex)
		{
			addNewPos = -1;
		}
	}

	public T AddNew()
	{
		return (T)((IBindingList)this).AddNew();
	}

	object IBindingList.AddNew()
	{
		object obj = AddNewCore();
		addNewPos = ((obj != null) ? IndexOf((T)obj) : (-1));
		return obj;
	}

	protected virtual object? AddNewCore()
	{
		object obj = FireAddingNew();
		if (obj == null)
		{
			obj = Activator.CreateInstance(typeof(T));
		}
		Add((T)obj);
		return obj;
	}

	void IBindingList.ApplySort(PropertyDescriptor prop, ListSortDirection direction)
	{
		ApplySortCore(prop, direction);
	}

	protected virtual void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
	{
		throw new NotSupportedException();
	}

	void IBindingList.RemoveSort()
	{
		RemoveSortCore();
	}

	protected virtual void RemoveSortCore()
	{
		throw new NotSupportedException();
	}

	int IBindingList.Find(PropertyDescriptor prop, object key)
	{
		return FindCore(prop, key);
	}

	protected virtual int FindCore(PropertyDescriptor prop, object key)
	{
		throw new NotSupportedException();
	}

	void IBindingList.AddIndex(PropertyDescriptor prop)
	{
	}

	void IBindingList.RemoveIndex(PropertyDescriptor prop)
	{
	}

	[RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
	private void HookPropertyChanged(T item)
	{
		if (item is INotifyPropertyChanged notifyPropertyChanged)
		{
			if (_propertyChangedEventHandler == null)
			{
				_propertyChangedEventHandler = Child_PropertyChanged;
			}
			notifyPropertyChanged.PropertyChanged += _propertyChangedEventHandler;
		}
	}

	private void UnhookPropertyChanged(T item)
	{
		if (item is INotifyPropertyChanged notifyPropertyChanged && _propertyChangedEventHandler != null)
		{
			notifyPropertyChanged.PropertyChanged -= _propertyChangedEventHandler;
		}
	}

	[RequiresUnreferencedCode("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
	private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!RaiseListChangedEvents)
		{
			return;
		}
		if (sender == null || e == null || string.IsNullOrEmpty(e.PropertyName))
		{
			ResetBindings();
			return;
		}
		T val;
		try
		{
			val = (T)sender;
		}
		catch (InvalidCastException)
		{
			ResetBindings();
			return;
		}
		int num = _lastChangeIndex;
		if (num < 0 || num >= base.Count || !base[num].Equals(val))
		{
			num = (_lastChangeIndex = IndexOf(val));
		}
		if (num == -1)
		{
			UnhookPropertyChanged(val);
			ResetBindings();
			return;
		}
		if (_itemTypeProperties == null)
		{
			_itemTypeProperties = TypeDescriptor.GetProperties(typeof(T));
		}
		PropertyDescriptor propDesc = _itemTypeProperties.Find(e.PropertyName, ignoreCase: true);
		ListChangedEventArgs e2 = new ListChangedEventArgs(ListChangedType.ItemChanged, num, propDesc);
		OnListChanged(e2);
	}
}
