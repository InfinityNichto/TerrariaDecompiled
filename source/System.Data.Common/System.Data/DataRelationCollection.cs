using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Threading;

namespace System.Data;

[DefaultEvent("CollectionChanged")]
[DefaultProperty("Table")]
[Editor("Microsoft.VSDesigner.Data.Design.DataRelationCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public abstract class DataRelationCollection : InternalDataCollectionBase
{
	internal sealed class DataTableRelationCollection : DataRelationCollection
	{
		private readonly DataTable _table;

		private readonly ArrayList _relations;

		private readonly bool _fParentCollection;

		protected override ArrayList List => _relations;

		public override DataRelation this[int index]
		{
			get
			{
				if (index >= 0 && index < _relations.Count)
				{
					return (DataRelation)_relations[index];
				}
				throw ExceptionBuilder.RelationOutOfRange(index);
			}
		}

		public override DataRelation this[string name]
		{
			get
			{
				int num = InternalIndexOf(name);
				if (num == -2)
				{
					throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
				}
				if (num >= 0)
				{
					return (DataRelation)List[num];
				}
				return null;
			}
		}

		internal event CollectionChangeEventHandler RelationPropertyChanged;

		internal DataTableRelationCollection(DataTable table, bool fParentCollection)
		{
			if (table == null)
			{
				throw ExceptionBuilder.RelationTableNull();
			}
			_table = table;
			_fParentCollection = fParentCollection;
			_relations = new ArrayList();
		}

		private void EnsureDataSet()
		{
			if (_table.DataSet == null)
			{
				throw ExceptionBuilder.RelationTableWasRemoved();
			}
		}

		protected override DataSet GetDataSet()
		{
			EnsureDataSet();
			return _table.DataSet;
		}

		internal void OnRelationPropertyChanged(CollectionChangeEventArgs ccevent)
		{
			if (!_fParentCollection)
			{
				_table.UpdatePropertyDescriptorCollectionCache();
			}
			this.RelationPropertyChanged?.Invoke(this, ccevent);
		}

		private void AddCache(DataRelation relation)
		{
			_relations.Add(relation);
			if (!_fParentCollection)
			{
				_table.UpdatePropertyDescriptorCollectionCache();
			}
		}

		protected override void AddCore(DataRelation relation)
		{
			if (_fParentCollection)
			{
				if (relation.ChildTable != _table)
				{
					throw ExceptionBuilder.ChildTableMismatch();
				}
			}
			else if (relation.ParentTable != _table)
			{
				throw ExceptionBuilder.ParentTableMismatch();
			}
			GetDataSet().Relations.Add(relation);
			AddCache(relation);
		}

		public override bool CanRemove(DataRelation relation)
		{
			if (!base.CanRemove(relation))
			{
				return false;
			}
			if (_fParentCollection)
			{
				if (relation.ChildTable != _table)
				{
					return false;
				}
			}
			else if (relation.ParentTable != _table)
			{
				return false;
			}
			return true;
		}

		private void RemoveCache(DataRelation relation)
		{
			for (int i = 0; i < _relations.Count; i++)
			{
				if (relation == _relations[i])
				{
					_relations.RemoveAt(i);
					if (!_fParentCollection)
					{
						_table.UpdatePropertyDescriptorCollectionCache();
					}
					return;
				}
			}
			throw ExceptionBuilder.RelationDoesNotExist();
		}

		protected override void RemoveCore(DataRelation relation)
		{
			if (_fParentCollection)
			{
				if (relation.ChildTable != _table)
				{
					throw ExceptionBuilder.ChildTableMismatch();
				}
			}
			else if (relation.ParentTable != _table)
			{
				throw ExceptionBuilder.ParentTableMismatch();
			}
			GetDataSet().Relations.Remove(relation);
			RemoveCache(relation);
		}
	}

	internal sealed class DataSetRelationCollection : DataRelationCollection
	{
		private readonly DataSet _dataSet;

		private readonly ArrayList _relations;

		private DataRelation[] _delayLoadingRelations;

		protected override ArrayList List => _relations;

		public override DataRelation this[int index]
		{
			get
			{
				if (index >= 0 && index < _relations.Count)
				{
					return (DataRelation)_relations[index];
				}
				throw ExceptionBuilder.RelationOutOfRange(index);
			}
		}

		public override DataRelation this[string name]
		{
			get
			{
				int num = InternalIndexOf(name);
				if (num == -2)
				{
					throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
				}
				if (num >= 0)
				{
					return (DataRelation)List[num];
				}
				return null;
			}
		}

		internal DataSetRelationCollection(DataSet dataSet)
		{
			if (dataSet == null)
			{
				throw ExceptionBuilder.RelationDataSetNull();
			}
			_dataSet = dataSet;
			_relations = new ArrayList();
		}

		public override void AddRange(DataRelation[] relations)
		{
			if (_dataSet._fInitInProgress)
			{
				_delayLoadingRelations = relations;
			}
			else
			{
				if (relations == null)
				{
					return;
				}
				foreach (DataRelation dataRelation in relations)
				{
					if (dataRelation != null)
					{
						Add(dataRelation);
					}
				}
			}
		}

		public override void Clear()
		{
			base.Clear();
			if (_dataSet._fInitInProgress && _delayLoadingRelations != null)
			{
				_delayLoadingRelations = null;
			}
		}

		protected override DataSet GetDataSet()
		{
			return _dataSet;
		}

		protected override void AddCore(DataRelation relation)
		{
			base.AddCore(relation);
			if (relation.ChildTable.DataSet != _dataSet || relation.ParentTable.DataSet != _dataSet)
			{
				throw ExceptionBuilder.ForeignRelation();
			}
			relation.CheckState();
			if (relation.Nested)
			{
				relation.CheckNestedRelations();
			}
			if (relation._relationName.Length == 0)
			{
				relation._relationName = AssignName();
			}
			else
			{
				RegisterName(relation._relationName);
			}
			DataKey childKey = relation.ChildKey;
			for (int i = 0; i < _relations.Count; i++)
			{
				if (childKey.ColumnsEqual(((DataRelation)_relations[i]).ChildKey) && relation.ParentKey.ColumnsEqual(((DataRelation)_relations[i]).ParentKey))
				{
					throw ExceptionBuilder.RelationAlreadyExists();
				}
			}
			_relations.Add(relation);
			((DataTableRelationCollection)relation.ParentTable.ChildRelations).Add(relation);
			((DataTableRelationCollection)relation.ChildTable.ParentRelations).Add(relation);
			relation.SetDataSet(_dataSet);
			relation.ChildKey.GetSortIndex().AddRef();
			if (relation.Nested)
			{
				relation.ChildTable.CacheNestedParent();
			}
			ForeignKeyConstraint foreignKeyConstraint = relation.ChildTable.Constraints.FindForeignKeyConstraint(relation.ParentColumnsReference, relation.ChildColumnsReference);
			if (relation._createConstraints && foreignKeyConstraint == null)
			{
				relation.ChildTable.Constraints.Add(foreignKeyConstraint = new ForeignKeyConstraint(relation.ParentColumnsReference, relation.ChildColumnsReference));
				try
				{
					foreignKeyConstraint.ConstraintName = relation.RelationName;
				}
				catch (Exception e) when (ADP.IsCatchableExceptionType(e))
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
			UniqueConstraint parentKeyConstraint = relation.ParentTable.Constraints.FindKeyConstraint(relation.ParentColumnsReference);
			relation.SetParentKeyConstraint(parentKeyConstraint);
			relation.SetChildKeyConstraint(foreignKeyConstraint);
		}

		protected override void RemoveCore(DataRelation relation)
		{
			base.RemoveCore(relation);
			_dataSet.OnRemoveRelationHack(relation);
			relation.SetDataSet(null);
			relation.ChildKey.GetSortIndex().RemoveRef();
			if (relation.Nested)
			{
				relation.ChildTable.CacheNestedParent();
			}
			for (int i = 0; i < _relations.Count; i++)
			{
				if (relation == _relations[i])
				{
					_relations.RemoveAt(i);
					((DataTableRelationCollection)relation.ParentTable.ChildRelations).Remove(relation);
					((DataTableRelationCollection)relation.ChildTable.ParentRelations).Remove(relation);
					if (relation.Nested)
					{
						relation.ChildTable.CacheNestedParent();
					}
					UnregisterName(relation.RelationName);
					relation.SetParentKeyConstraint(null);
					relation.SetChildKeyConstraint(null);
					return;
				}
			}
			throw ExceptionBuilder.RelationDoesNotExist();
		}

		internal void FinishInitRelations()
		{
			if (_delayLoadingRelations == null)
			{
				return;
			}
			for (int i = 0; i < _delayLoadingRelations.Length; i++)
			{
				DataRelation dataRelation = _delayLoadingRelations[i];
				if (dataRelation._parentColumnNames == null || dataRelation._childColumnNames == null)
				{
					Add(dataRelation);
					continue;
				}
				int num = dataRelation._parentColumnNames.Length;
				DataColumn[] array = new DataColumn[num];
				DataColumn[] array2 = new DataColumn[num];
				for (int j = 0; j < num; j++)
				{
					if (dataRelation._parentTableNamespace == null)
					{
						array[j] = _dataSet.Tables[dataRelation._parentTableName].Columns[dataRelation._parentColumnNames[j]];
					}
					else
					{
						array[j] = _dataSet.Tables[dataRelation._parentTableName, dataRelation._parentTableNamespace].Columns[dataRelation._parentColumnNames[j]];
					}
					if (dataRelation._childTableNamespace == null)
					{
						array2[j] = _dataSet.Tables[dataRelation._childTableName].Columns[dataRelation._childColumnNames[j]];
					}
					else
					{
						array2[j] = _dataSet.Tables[dataRelation._childTableName, dataRelation._childTableNamespace].Columns[dataRelation._childColumnNames[j]];
					}
				}
				DataRelation dataRelation2 = new DataRelation(dataRelation._relationName, array, array2, createConstraints: false);
				dataRelation2.Nested = dataRelation._nested;
				Add(dataRelation2);
			}
			_delayLoadingRelations = null;
		}
	}

	private DataRelation _inTransition;

	private int _defaultNameIndex = 1;

	private CollectionChangeEventHandler _onCollectionChangedDelegate;

	private CollectionChangeEventHandler _onCollectionChangingDelegate;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	internal int ObjectID => _objectID;

	public abstract DataRelation this[int index] { get; }

	public abstract DataRelation? this[string? name] { get; }

	public event CollectionChangeEventHandler? CollectionChanged
	{
		add
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.add_CollectionChanged|API> {0}", ObjectID);
			_onCollectionChangedDelegate = (CollectionChangeEventHandler)Delegate.Combine(_onCollectionChangedDelegate, value);
		}
		remove
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.remove_CollectionChanged|API> {0}", ObjectID);
			_onCollectionChangedDelegate = (CollectionChangeEventHandler)Delegate.Remove(_onCollectionChangedDelegate, value);
		}
	}

	internal event CollectionChangeEventHandler? CollectionChanging
	{
		add
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.add_CollectionChanging|INFO> {0}", ObjectID);
			_onCollectionChangingDelegate = (CollectionChangeEventHandler)Delegate.Combine(_onCollectionChangingDelegate, value);
		}
		remove
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.remove_CollectionChanging|INFO> {0}", ObjectID);
			_onCollectionChangingDelegate = (CollectionChangeEventHandler)Delegate.Remove(_onCollectionChangingDelegate, value);
		}
	}

	public void Add(DataRelation relation)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelationCollection.Add|API> {0}, relation={1}", ObjectID, relation?.ObjectID ?? 0);
		try
		{
			if (_inTransition == relation)
			{
				return;
			}
			_inTransition = relation;
			try
			{
				OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, relation));
				AddCore(relation);
				OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, relation));
			}
			finally
			{
				_inTransition = null;
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public virtual void AddRange(DataRelation[]? relations)
	{
		if (relations == null)
		{
			return;
		}
		foreach (DataRelation dataRelation in relations)
		{
			if (dataRelation != null)
			{
				Add(dataRelation);
			}
		}
	}

	public virtual DataRelation Add(string? name, DataColumn[] parentColumns, DataColumn[] childColumns)
	{
		DataRelation dataRelation = new DataRelation(name, parentColumns, childColumns);
		Add(dataRelation);
		return dataRelation;
	}

	public virtual DataRelation Add(string? name, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
	{
		DataRelation dataRelation = new DataRelation(name, parentColumns, childColumns, createConstraints);
		Add(dataRelation);
		return dataRelation;
	}

	public virtual DataRelation Add(DataColumn[] parentColumns, DataColumn[] childColumns)
	{
		DataRelation dataRelation = new DataRelation(null, parentColumns, childColumns);
		Add(dataRelation);
		return dataRelation;
	}

	public virtual DataRelation Add(string? name, DataColumn parentColumn, DataColumn childColumn)
	{
		DataRelation dataRelation = new DataRelation(name, parentColumn, childColumn);
		Add(dataRelation);
		return dataRelation;
	}

	public virtual DataRelation Add(string? name, DataColumn parentColumn, DataColumn childColumn, bool createConstraints)
	{
		DataRelation dataRelation = new DataRelation(name, parentColumn, childColumn, createConstraints);
		Add(dataRelation);
		return dataRelation;
	}

	public virtual DataRelation Add(DataColumn parentColumn, DataColumn childColumn)
	{
		DataRelation dataRelation = new DataRelation(null, parentColumn, childColumn);
		Add(dataRelation);
		return dataRelation;
	}

	protected virtual void AddCore(DataRelation relation)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.AddCore|INFO> {0}, relation={1}", ObjectID, relation?.ObjectID ?? 0);
		if (relation == null)
		{
			throw ExceptionBuilder.ArgumentNull("relation");
		}
		relation.CheckState();
		DataSet dataSet = GetDataSet();
		if (relation.DataSet == dataSet)
		{
			throw ExceptionBuilder.RelationAlreadyInTheDataSet();
		}
		if (relation.DataSet != null)
		{
			throw ExceptionBuilder.RelationAlreadyInOtherDataSet();
		}
		if (relation.ChildTable.Locale.LCID != relation.ParentTable.Locale.LCID || relation.ChildTable.CaseSensitive != relation.ParentTable.CaseSensitive)
		{
			throw ExceptionBuilder.CaseLocaleMismatch();
		}
		if (relation.Nested)
		{
			relation.CheckNamespaceValidityForNestedRelations(relation.ParentTable.Namespace);
			relation.ValidateMultipleNestedRelations();
			relation.ParentTable.ElementColumnCount++;
		}
	}

	internal string AssignName()
	{
		string result = MakeName(_defaultNameIndex);
		_defaultNameIndex++;
		return result;
	}

	public virtual void Clear()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelationCollection.Clear|API> {0}", ObjectID);
		try
		{
			int count = Count;
			OnCollectionChanging(InternalDataCollectionBase.s_refreshEventArgs);
			for (int num = count - 1; num >= 0; num--)
			{
				_inTransition = this[num];
				RemoveCore(_inTransition);
			}
			OnCollectionChanged(InternalDataCollectionBase.s_refreshEventArgs);
			_inTransition = null;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public virtual bool Contains(string? name)
	{
		return InternalIndexOf(name) >= 0;
	}

	public void CopyTo(DataRelation[] array, int index)
	{
		if (array == null)
		{
			throw ExceptionBuilder.ArgumentNull("array");
		}
		if (index < 0)
		{
			throw ExceptionBuilder.ArgumentOutOfRange("index");
		}
		ArrayList list = List;
		if (array.Length - index < list.Count)
		{
			throw ExceptionBuilder.InvalidOffsetLength();
		}
		for (int i = 0; i < list.Count; i++)
		{
			array[index + i] = (DataRelation)list[i];
		}
	}

	public virtual int IndexOf(DataRelation? relation)
	{
		int count = List.Count;
		for (int i = 0; i < count; i++)
		{
			if (relation == (DataRelation)List[i])
			{
				return i;
			}
		}
		return -1;
	}

	public virtual int IndexOf(string? relationName)
	{
		int num = InternalIndexOf(relationName);
		if (num >= 0)
		{
			return num;
		}
		return -1;
	}

	internal int InternalIndexOf(string name)
	{
		int num = -1;
		if (name != null && 0 < name.Length)
		{
			int count = List.Count;
			int num2 = 0;
			for (int i = 0; i < count; i++)
			{
				DataRelation dataRelation = (DataRelation)List[i];
				switch (NamesEqual(dataRelation.RelationName, name, fCaseSensitive: false, GetDataSet().Locale))
				{
				case 1:
					return i;
				case -1:
					num = ((num == -1) ? i : (-2));
					break;
				}
			}
		}
		return num;
	}

	protected abstract DataSet GetDataSet();

	private string MakeName(int index)
	{
		if (index != 1)
		{
			return "Relation" + index.ToString(CultureInfo.InvariantCulture);
		}
		return "Relation1";
	}

	protected virtual void OnCollectionChanged(CollectionChangeEventArgs ccevent)
	{
		if (_onCollectionChangedDelegate != null)
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.OnCollectionChanged|INFO> {0}", ObjectID);
			_onCollectionChangedDelegate(this, ccevent);
		}
	}

	protected virtual void OnCollectionChanging(CollectionChangeEventArgs ccevent)
	{
		if (_onCollectionChangingDelegate != null)
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.OnCollectionChanging|INFO> {0}", ObjectID);
			_onCollectionChangingDelegate(this, ccevent);
		}
	}

	internal void RegisterName(string name)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.RegisterName|INFO> {0}, name='{1}'", ObjectID, name);
		CultureInfo locale = GetDataSet().Locale;
		int count = Count;
		for (int i = 0; i < count; i++)
		{
			if (NamesEqual(name, this[i].RelationName, fCaseSensitive: true, locale) != 0)
			{
				throw ExceptionBuilder.DuplicateRelation(this[i].RelationName);
			}
		}
		if (NamesEqual(name, MakeName(_defaultNameIndex), fCaseSensitive: true, locale) != 0)
		{
			_defaultNameIndex++;
		}
	}

	public virtual bool CanRemove(DataRelation? relation)
	{
		if (relation != null)
		{
			return relation.DataSet == GetDataSet();
		}
		return false;
	}

	public void Remove(DataRelation relation)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.Remove|API> {0}, relation={1}", ObjectID, relation?.ObjectID ?? 0);
		if (_inTransition == relation)
		{
			return;
		}
		_inTransition = relation;
		try
		{
			OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, relation));
			RemoveCore(relation);
			OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, relation));
		}
		finally
		{
			_inTransition = null;
		}
	}

	public void RemoveAt(int index)
	{
		DataRelation dataRelation = this[index];
		if (dataRelation == null)
		{
			throw ExceptionBuilder.RelationOutOfRange(index);
		}
		Remove(dataRelation);
	}

	public void Remove(string name)
	{
		DataRelation dataRelation = this[name];
		if (dataRelation == null)
		{
			throw ExceptionBuilder.RelationNotInTheDataSet(name);
		}
		Remove(dataRelation);
	}

	protected virtual void RemoveCore(DataRelation relation)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.RemoveCore|INFO> {0}, relation={1}", ObjectID, relation?.ObjectID ?? 0);
		if (relation == null)
		{
			throw ExceptionBuilder.ArgumentNull("relation");
		}
		DataSet dataSet = GetDataSet();
		if (relation.DataSet != dataSet)
		{
			throw ExceptionBuilder.RelationNotInTheDataSet(relation.RelationName);
		}
		if (relation.Nested)
		{
			relation.ParentTable.ElementColumnCount--;
			relation.ParentTable.Columns.UnregisterName(relation.ChildTable.TableName);
		}
	}

	internal void UnregisterName(string name)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelationCollection.UnregisterName|INFO> {0}, name='{1}'", ObjectID, name);
		if (NamesEqual(name, MakeName(_defaultNameIndex - 1), fCaseSensitive: true, GetDataSet().Locale) != 0)
		{
			do
			{
				_defaultNameIndex--;
			}
			while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
		}
	}
}
