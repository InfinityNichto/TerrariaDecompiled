using System.Reflection;

namespace System.Runtime.Serialization;

internal sealed class ObjectHolder
{
	private object _object;

	internal readonly long _id;

	private int _missingElementsRemaining;

	private int _missingDecendents;

	internal SerializationInfo _serInfo;

	internal ISerializationSurrogate _surrogate;

	internal FixupHolderList _missingElements;

	internal LongList _dependentObjects;

	internal ObjectHolder _next;

	internal int _flags;

	private bool _markForFixupWhenAvailable;

	private ValueTypeFixupInfo _valueFixup;

	private TypeLoadExceptionHolder _typeLoad;

	private bool _reachable;

	internal bool IsIncompleteObjectReference
	{
		get
		{
			return (_flags & 1) != 0;
		}
		set
		{
			if (value)
			{
				_flags |= 1;
			}
			else
			{
				_flags &= -2;
			}
		}
	}

	internal bool RequiresDelayedFixup => (_flags & 7) != 0;

	internal bool RequiresValueTypeFixup => (_flags & 8) != 0;

	internal bool ValueTypeFixupPerformed
	{
		get
		{
			if ((_flags & 0x8000) == 0)
			{
				if (_object != null)
				{
					if (_dependentObjects != null)
					{
						return _dependentObjects.Count == 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		set
		{
			if (value)
			{
				_flags |= 32768;
			}
		}
	}

	internal bool HasISerializable => (_flags & 2) != 0;

	internal bool HasSurrogate => (_flags & 4) != 0;

	internal bool CanSurrogatedObjectValueChange
	{
		get
		{
			if (_surrogate != null)
			{
				return _surrogate.GetType() != typeof(SurrogateForCyclicalReference);
			}
			return true;
		}
	}

	internal bool CanObjectValueChange
	{
		get
		{
			if (!IsIncompleteObjectReference)
			{
				if (!HasSurrogate)
				{
					return false;
				}
				return CanSurrogatedObjectValueChange;
			}
			return true;
		}
	}

	internal int DirectlyDependentObjects => _missingElementsRemaining;

	internal int TotalDependentObjects => _missingElementsRemaining + _missingDecendents;

	internal bool Reachable
	{
		get
		{
			return _reachable;
		}
		set
		{
			_reachable = value;
		}
	}

	internal bool TypeLoadExceptionReachable => _typeLoad != null;

	internal TypeLoadExceptionHolder TypeLoadException
	{
		get
		{
			return _typeLoad;
		}
		set
		{
			_typeLoad = value;
		}
	}

	internal object ObjectValue => _object;

	internal SerializationInfo SerializationInfo
	{
		get
		{
			return _serInfo;
		}
		set
		{
			_serInfo = value;
		}
	}

	internal ISerializationSurrogate Surrogate => _surrogate;

	internal LongList DependentObjects
	{
		get
		{
			return _dependentObjects;
		}
		set
		{
			_dependentObjects = value;
		}
	}

	internal bool RequiresSerInfoFixup
	{
		get
		{
			if ((_flags & 4) == 0 && (_flags & 2) == 0)
			{
				return false;
			}
			return (_flags & 0x4000) == 0;
		}
		set
		{
			if (!value)
			{
				_flags |= 16384;
			}
			else
			{
				_flags &= -16385;
			}
		}
	}

	internal ValueTypeFixupInfo ValueFixup => _valueFixup;

	internal bool CompletelyFixed
	{
		get
		{
			if (!RequiresSerInfoFixup)
			{
				return !IsIncompleteObjectReference;
			}
			return false;
		}
	}

	internal long ContainerID
	{
		get
		{
			if (_valueFixup == null)
			{
				return 0L;
			}
			return _valueFixup.ContainerID;
		}
	}

	internal ObjectHolder(long objID)
		: this(null, objID, null, null, 0L, null, null)
	{
	}

	internal ObjectHolder(object obj, long objID, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainingObj, FieldInfo field, int[] arrayIndex)
	{
		_object = obj;
		_id = objID;
		_flags = 0;
		_missingElementsRemaining = 0;
		_missingDecendents = 0;
		_dependentObjects = null;
		_next = null;
		_serInfo = info;
		_surrogate = surrogate;
		_markForFixupWhenAvailable = false;
		if (obj is TypeLoadExceptionHolder)
		{
			_typeLoad = (TypeLoadExceptionHolder)obj;
		}
		if (idOfContainingObj != 0L && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
		{
			if (idOfContainingObj == objID)
			{
				throw new SerializationException(System.SR.Serialization_ParentChildIdentical);
			}
			_valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
		}
		SetFlags();
	}

	internal ObjectHolder(string obj, long objID, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainingObj, FieldInfo field, int[] arrayIndex)
	{
		_object = obj;
		_id = objID;
		_flags = 0;
		_missingElementsRemaining = 0;
		_missingDecendents = 0;
		_dependentObjects = null;
		_next = null;
		_serInfo = info;
		_surrogate = surrogate;
		_markForFixupWhenAvailable = false;
		if (idOfContainingObj != 0L && arrayIndex != null)
		{
			_valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
		}
		if (_valueFixup != null)
		{
			_flags |= 8;
		}
	}

	private void IncrementDescendentFixups(int amount)
	{
		_missingDecendents += amount;
	}

	internal void DecrementFixupsRemaining(ObjectManager manager)
	{
		_missingElementsRemaining--;
		if (RequiresValueTypeFixup)
		{
			UpdateDescendentDependencyChain(-1, manager);
		}
	}

	internal void RemoveDependency(long id)
	{
		_dependentObjects.RemoveElement(id);
	}

	internal void AddFixup(FixupHolder fixup, ObjectManager manager)
	{
		if (_missingElements == null)
		{
			_missingElements = new FixupHolderList();
		}
		_missingElements.Add(fixup);
		_missingElementsRemaining++;
		if (RequiresValueTypeFixup)
		{
			UpdateDescendentDependencyChain(1, manager);
		}
	}

	private void UpdateDescendentDependencyChain(int amount, ObjectManager manager)
	{
		ObjectHolder objectHolder = this;
		do
		{
			objectHolder = manager.FindOrCreateObjectHolder(objectHolder.ContainerID);
			objectHolder.IncrementDescendentFixups(amount);
		}
		while (objectHolder.RequiresValueTypeFixup);
	}

	internal void AddDependency(long dependentObject)
	{
		if (_dependentObjects == null)
		{
			_dependentObjects = new LongList();
		}
		_dependentObjects.Add(dependentObject);
	}

	internal void UpdateData(object obj, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainer, FieldInfo field, int[] arrayIndex, ObjectManager manager)
	{
		SetObjectValue(obj, manager);
		_serInfo = info;
		_surrogate = surrogate;
		if (idOfContainer != 0L && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
		{
			if (idOfContainer == _id)
			{
				throw new SerializationException(System.SR.Serialization_ParentChildIdentical);
			}
			_valueFixup = new ValueTypeFixupInfo(idOfContainer, field, arrayIndex);
		}
		SetFlags();
		if (RequiresValueTypeFixup)
		{
			UpdateDescendentDependencyChain(_missingElementsRemaining, manager);
		}
	}

	internal void MarkForCompletionWhenAvailable()
	{
		_markForFixupWhenAvailable = true;
	}

	internal void SetFlags()
	{
		if (_object is IObjectReference)
		{
			_flags |= 1;
		}
		_flags &= -7;
		if (_surrogate != null)
		{
			_flags |= 4;
		}
		else if (_object is ISerializable)
		{
			_flags |= 2;
		}
		if (_valueFixup != null)
		{
			_flags |= 8;
		}
	}

	internal void SetObjectValue(object obj, ObjectManager manager)
	{
		_object = obj;
		if (obj == manager.TopObject)
		{
			_reachable = true;
		}
		if (obj is TypeLoadExceptionHolder)
		{
			_typeLoad = (TypeLoadExceptionHolder)obj;
		}
		if (_markForFixupWhenAvailable)
		{
			manager.CompleteObject(this, bObjectFullyComplete: true);
		}
	}
}
