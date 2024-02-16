using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization;

public class ObjectManager
{
	private static readonly FieldInfo s_nullableValueField = typeof(Nullable<>).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);

	private DeserializationEventHandler _onDeserializationHandler;

	private SerializationEventHandler _onDeserializedHandler;

	internal ObjectHolder[] _objects;

	internal object _topObject;

	internal ObjectHolderList _specialFixupObjects;

	internal long _fixupCount;

	internal readonly ISurrogateSelector _selector;

	internal readonly StreamingContext _context;

	internal object? TopObject
	{
		get
		{
			return _topObject;
		}
		set
		{
			_topObject = value;
		}
	}

	internal ObjectHolderList SpecialFixupObjects => _specialFixupObjects ?? (_specialFixupObjects = new ObjectHolderList());

	public ObjectManager(ISurrogateSelector? selector, StreamingContext context)
	{
		_objects = new ObjectHolder[16];
		_selector = selector;
		_context = context;
	}

	private bool CanCallGetType(object obj)
	{
		return true;
	}

	internal ObjectHolder FindObjectHolder(long objectID)
	{
		int num = (int)(objectID & 0xFFFFF);
		if (num >= _objects.Length)
		{
			return null;
		}
		ObjectHolder objectHolder;
		for (objectHolder = _objects[num]; objectHolder != null; objectHolder = objectHolder._next)
		{
			if (objectHolder._id == objectID)
			{
				return objectHolder;
			}
		}
		return objectHolder;
	}

	internal ObjectHolder FindOrCreateObjectHolder(long objectID)
	{
		ObjectHolder objectHolder = FindObjectHolder(objectID);
		if (objectHolder == null)
		{
			objectHolder = new ObjectHolder(objectID);
			AddObjectHolder(objectHolder);
		}
		return objectHolder;
	}

	private void AddObjectHolder(ObjectHolder holder)
	{
		if (holder._id >= _objects.Length && _objects.Length != 1048576)
		{
			int num = 1048576;
			if (holder._id < 524288)
			{
				num = _objects.Length * 2;
				while (num <= holder._id && num < 1048576)
				{
					num *= 2;
				}
				if (num > 1048576)
				{
					num = 1048576;
				}
			}
			ObjectHolder[] array = new ObjectHolder[num];
			Array.Copy(_objects, array, _objects.Length);
			_objects = array;
		}
		int num2 = (int)(holder._id & 0xFFFFF);
		ObjectHolder next = _objects[num2];
		holder._next = next;
		_objects[num2] = holder;
	}

	private bool GetCompletionInfo(FixupHolder fixup, [NotNullWhen(true)] out ObjectHolder holder, out object member, bool bThrowIfMissing)
	{
		member = fixup._fixupInfo;
		holder = FindObjectHolder(fixup._id);
		if (holder == null || holder.CanObjectValueChange || holder.ObjectValue == null)
		{
			if (bThrowIfMissing)
			{
				if (holder == null)
				{
					throw new SerializationException(System.SR.Format(System.SR.Serialization_NeverSeen, fixup._id));
				}
				if (holder.IsIncompleteObjectReference)
				{
					throw new SerializationException(System.SR.Format(System.SR.Serialization_IORIncomplete, fixup._id));
				}
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectNotSupplied, fixup._id));
			}
			return false;
		}
		if (!holder.CompletelyFixed && holder.ObjectValue != null && holder.ObjectValue is ValueType)
		{
			SpecialFixupObjects.Add(holder);
			return false;
		}
		return true;
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	private void FixupSpecialObject(ObjectHolder holder)
	{
		ISurrogateSelector selector = null;
		if (holder.HasSurrogate)
		{
			ISerializationSurrogate surrogate = holder.Surrogate;
			object obj = surrogate.SetObjectData(holder.ObjectValue, holder.SerializationInfo, _context, selector);
			if (obj != null)
			{
				if (!holder.CanSurrogatedObjectValueChange && obj != holder.ObjectValue)
				{
					throw new SerializationException(System.SR.Format(System.SR.Serialization_NotCyclicallyReferenceableSurrogate, surrogate.GetType().FullName));
				}
				holder.SetObjectValue(obj, this);
			}
			holder._surrogate = null;
			holder.SetFlags();
		}
		else
		{
			CompleteISerializableObject(holder.ObjectValue, holder.SerializationInfo, _context);
		}
		holder.SerializationInfo = null;
		holder.RequiresSerInfoFixup = false;
		if (holder.RequiresValueTypeFixup && holder.ValueTypeFixupPerformed)
		{
			DoValueTypeFixup(null, holder, holder.ObjectValue);
		}
		DoNewlyRegisteredObjectFixups(holder);
	}

	private bool ResolveObjectReference(ObjectHolder holder)
	{
		int num = 0;
		try
		{
			object objectValue;
			do
			{
				objectValue = holder.ObjectValue;
				holder.SetObjectValue(((IObjectReference)holder.ObjectValue).GetRealObject(_context), this);
				if (holder.ObjectValue == null)
				{
					holder.SetObjectValue(objectValue, this);
					return false;
				}
				if (num++ == 100)
				{
					throw new SerializationException(System.SR.Serialization_TooManyReferences);
				}
			}
			while (holder.ObjectValue is IObjectReference && objectValue != holder.ObjectValue);
		}
		catch (NullReferenceException)
		{
			return false;
		}
		holder.IsIncompleteObjectReference = false;
		DoNewlyRegisteredObjectFixups(holder);
		return true;
	}

	private bool DoValueTypeFixup(FieldInfo memberToFix, ObjectHolder holder, object value)
	{
		FieldInfo[] array = new FieldInfo[4];
		int num = 0;
		int[] array2 = null;
		object objectValue = holder.ObjectValue;
		while (holder.RequiresValueTypeFixup)
		{
			if (num + 1 >= array.Length)
			{
				FieldInfo[] array3 = new FieldInfo[array.Length * 2];
				Array.Copy(array, array3, array.Length);
				array = array3;
			}
			ValueTypeFixupInfo valueFixup = holder.ValueFixup;
			objectValue = holder.ObjectValue;
			if (valueFixup.ParentField != null)
			{
				FieldInfo parentField = valueFixup.ParentField;
				ObjectHolder objectHolder = FindObjectHolder(valueFixup.ContainerID);
				if (objectHolder.ObjectValue == null)
				{
					break;
				}
				FieldInfo nullableValueField = GetNullableValueField(parentField.FieldType);
				if (nullableValueField != null)
				{
					array[num] = nullableValueField;
					num++;
				}
				array[num] = parentField;
				holder = objectHolder;
				num++;
				continue;
			}
			holder = FindObjectHolder(valueFixup.ContainerID);
			array2 = valueFixup.ParentIndex;
			break;
		}
		if (!(holder.ObjectValue is Array) && holder.ObjectValue != null)
		{
			objectValue = holder.ObjectValue;
		}
		if (num != 0)
		{
			FieldInfo[] array4 = new FieldInfo[num];
			for (int i = 0; i < num; i++)
			{
				FieldInfo fieldInfo = array[num - 1 - i];
				SerializationFieldInfo serializationFieldInfo = fieldInfo as SerializationFieldInfo;
				array4[i] = ((serializationFieldInfo == null) ? fieldInfo : serializationFieldInfo.FieldInfo);
			}
			TypedReference typedReference = TypedReference.MakeTypedReference(objectValue, array4);
			if (memberToFix != null)
			{
				memberToFix.SetValueDirect(typedReference, value);
			}
			else
			{
				TypedReference.SetTypedReference(typedReference, value);
			}
		}
		else if (memberToFix != null)
		{
			FormatterServices.SerializationSetValue(memberToFix, objectValue, value);
		}
		if (array2 != null && holder.ObjectValue != null)
		{
			((Array)holder.ObjectValue).SetValue(objectValue, array2);
		}
		return true;
	}

	private static FieldInfo GetNullableValueField(Type type)
	{
		if (Nullable.GetUnderlyingType(type) != null)
		{
			return (FieldInfo)type.GetMemberWithSameMetadataDefinitionAs(s_nullableValueField);
		}
		return null;
	}

	internal void CompleteObject(ObjectHolder holder, bool bObjectFullyComplete)
	{
		FixupHolderList missingElements = holder._missingElements;
		object member = null;
		ObjectHolder holder2 = null;
		int num = 0;
		if (holder.ObjectValue == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_MissingObject, holder._id));
		}
		if (missingElements == null)
		{
			return;
		}
		if (holder.HasSurrogate || holder.HasISerializable)
		{
			SerializationInfo serInfo = holder._serInfo;
			if (serInfo == null)
			{
				throw new SerializationException(System.SR.Serialization_InvalidFixupDiscovered);
			}
			if (missingElements != null)
			{
				for (int i = 0; i < missingElements._count; i++)
				{
					if (missingElements._values[i] != null && GetCompletionInfo(missingElements._values[i], out holder2, out member, bObjectFullyComplete))
					{
						object objectValue = holder2.ObjectValue;
						if (CanCallGetType(objectValue))
						{
							serInfo.UpdateValue((string)member, objectValue, objectValue.GetType());
						}
						else
						{
							serInfo.UpdateValue((string)member, objectValue, typeof(MarshalByRefObject));
						}
						num++;
						missingElements._values[i] = null;
						if (!bObjectFullyComplete)
						{
							holder.DecrementFixupsRemaining(this);
							holder2.RemoveDependency(holder._id);
						}
					}
				}
			}
		}
		else
		{
			for (int j = 0; j < missingElements._count; j++)
			{
				FixupHolder fixupHolder = missingElements._values[j];
				if (fixupHolder == null || !GetCompletionInfo(fixupHolder, out holder2, out member, bObjectFullyComplete))
				{
					continue;
				}
				if (holder2.TypeLoadExceptionReachable)
				{
					holder.TypeLoadException = holder2.TypeLoadException;
					if (holder.Reachable)
					{
						throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeLoadFailure, holder.TypeLoadException.TypeName));
					}
				}
				if (holder.Reachable)
				{
					holder2.Reachable = true;
				}
				switch (fixupHolder._fixupType)
				{
				case 1:
					if (holder.RequiresValueTypeFixup)
					{
						throw new SerializationException(System.SR.Serialization_ValueTypeFixup);
					}
					((Array)holder.ObjectValue).SetValue(holder2.ObjectValue, (int[])member);
					break;
				case 2:
				{
					MemberInfo memberInfo = (MemberInfo)member;
					if (memberInfo is FieldInfo)
					{
						if (holder.RequiresValueTypeFixup && holder.ValueTypeFixupPerformed)
						{
							if (!DoValueTypeFixup((FieldInfo)memberInfo, holder, holder2.ObjectValue))
							{
								throw new SerializationException(System.SR.Serialization_PartialValueTypeFixup);
							}
						}
						else
						{
							FormatterServices.SerializationSetValue(memberInfo, holder.ObjectValue, holder2.ObjectValue);
						}
						if (holder2.RequiresValueTypeFixup)
						{
							holder2.ValueTypeFixupPerformed = true;
						}
						break;
					}
					throw new SerializationException(System.SR.Serialization_UnableToFixup);
				}
				default:
					throw new SerializationException(System.SR.Serialization_UnableToFixup);
				}
				num++;
				missingElements._values[j] = null;
				if (!bObjectFullyComplete)
				{
					holder.DecrementFixupsRemaining(this);
					holder2.RemoveDependency(holder._id);
				}
			}
		}
		_fixupCount -= num;
		if (missingElements._count == num)
		{
			holder._missingElements = null;
		}
	}

	private void DoNewlyRegisteredObjectFixups(ObjectHolder holder)
	{
		if (holder.CanObjectValueChange)
		{
			return;
		}
		LongList dependentObjects = holder.DependentObjects;
		if (dependentObjects == null)
		{
			return;
		}
		dependentObjects.StartEnumeration();
		while (dependentObjects.MoveNext())
		{
			ObjectHolder objectHolder = FindObjectHolder(dependentObjects.Current);
			objectHolder.DecrementFixupsRemaining(this);
			if (objectHolder.DirectlyDependentObjects == 0)
			{
				if (objectHolder.ObjectValue != null)
				{
					CompleteObject(objectHolder, bObjectFullyComplete: true);
				}
				else
				{
					objectHolder.MarkForCompletionWhenAvailable();
				}
			}
		}
	}

	public virtual object? GetObject(long objectID)
	{
		if (objectID <= 0)
		{
			throw new ArgumentOutOfRangeException("objectID", System.SR.ArgumentOutOfRange_ObjectID);
		}
		ObjectHolder objectHolder = FindObjectHolder(objectID);
		if (objectHolder == null || objectHolder.CanObjectValueChange)
		{
			return null;
		}
		return objectHolder.ObjectValue;
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public virtual void RegisterObject(object obj, long objectID)
	{
		RegisterObject(obj, objectID, null, 0L, null);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public void RegisterObject(object obj, long objectID, SerializationInfo info)
	{
		RegisterObject(obj, objectID, info, 0L, null);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public void RegisterObject(object obj, long objectID, SerializationInfo? info, long idOfContainingObj, MemberInfo? member)
	{
		RegisterObject(obj, objectID, info, idOfContainingObj, member, null);
	}

	internal void RegisterString(string obj, long objectID, SerializationInfo info, long idOfContainingObj, MemberInfo member)
	{
		ObjectHolder holder = new ObjectHolder(obj, objectID, info, null, idOfContainingObj, (FieldInfo)member, null);
		AddObjectHolder(holder);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public void RegisterObject(object obj, long objectID, SerializationInfo? info, long idOfContainingObj, MemberInfo? member, int[]? arrayIndex)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (objectID <= 0)
		{
			throw new ArgumentOutOfRangeException("objectID", System.SR.ArgumentOutOfRange_ObjectID);
		}
		if (member != null && !(member is FieldInfo))
		{
			throw new SerializationException(System.SR.Serialization_UnknownMemberInfo);
		}
		ISerializationSurrogate surrogate = null;
		if (_selector != null)
		{
			Type type = (CanCallGetType(obj) ? obj.GetType() : typeof(MarshalByRefObject));
			surrogate = _selector.GetSurrogate(type, _context, out ISurrogateSelector _);
		}
		if (obj is IDeserializationCallback)
		{
			DeserializationEventHandler handler = ((IDeserializationCallback)obj).OnDeserialization;
			AddOnDeserialization(handler);
		}
		if (arrayIndex != null)
		{
			arrayIndex = (int[])arrayIndex.Clone();
		}
		ObjectHolder objectHolder = FindObjectHolder(objectID);
		if (objectHolder == null)
		{
			objectHolder = new ObjectHolder(obj, objectID, info, surrogate, idOfContainingObj, (FieldInfo)member, arrayIndex);
			AddObjectHolder(objectHolder);
			if (objectHolder.RequiresDelayedFixup)
			{
				SpecialFixupObjects.Add(objectHolder);
			}
			AddOnDeserialized(obj);
			return;
		}
		if (objectHolder.ObjectValue != null)
		{
			throw new SerializationException(System.SR.Serialization_RegisterTwice);
		}
		objectHolder.UpdateData(obj, info, surrogate, idOfContainingObj, (FieldInfo)member, arrayIndex, this);
		if (objectHolder.DirectlyDependentObjects > 0)
		{
			CompleteObject(objectHolder, bObjectFullyComplete: false);
		}
		if (objectHolder.RequiresDelayedFixup)
		{
			SpecialFixupObjects.Add(objectHolder);
		}
		if (objectHolder.CompletelyFixed)
		{
			DoNewlyRegisteredObjectFixups(objectHolder);
			objectHolder.DependentObjects = null;
		}
		if (objectHolder.TotalDependentObjects > 0)
		{
			AddOnDeserialized(obj);
		}
		else
		{
			RaiseOnDeserializedEvent(obj);
		}
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	internal void CompleteISerializableObject(object obj, SerializationInfo info, StreamingContext context)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (!(obj is ISerializable))
		{
			throw new ArgumentException(System.SR.Serialization_NotISer);
		}
		Type type = obj.GetType();
		ConstructorInfo deserializationConstructor;
		try
		{
			deserializationConstructor = GetDeserializationConstructor(type);
		}
		catch (Exception innerException)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_ConstructorNotFound, type), innerException);
		}
		deserializationConstructor.Invoke(obj, new object[2] { info, context });
	}

	internal static ConstructorInfo GetDeserializationConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type t)
	{
		ConstructorInfo[] constructors = t.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (ConstructorInfo constructorInfo in constructors)
		{
			ParameterInfo[] parameters = constructorInfo.GetParameters();
			if (parameters.Length == 2 && parameters[0].ParameterType == typeof(SerializationInfo) && parameters[1].ParameterType == typeof(StreamingContext))
			{
				return constructorInfo;
			}
		}
		throw new SerializationException(System.SR.Format(System.SR.Serialization_ConstructorNotFound, t.FullName));
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public virtual void DoFixups()
	{
		int num = -1;
		while (num != 0)
		{
			num = 0;
			ObjectHolderListEnumerator fixupEnumerator = SpecialFixupObjects.GetFixupEnumerator();
			while (fixupEnumerator.MoveNext())
			{
				ObjectHolder current = fixupEnumerator.Current;
				if (current.ObjectValue == null)
				{
					throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectNotSupplied, current._id));
				}
				if (current.TotalDependentObjects == 0)
				{
					if (current.RequiresSerInfoFixup)
					{
						FixupSpecialObject(current);
						num++;
					}
					else if (!current.IsIncompleteObjectReference)
					{
						CompleteObject(current, bObjectFullyComplete: true);
					}
					if (current.IsIncompleteObjectReference && ResolveObjectReference(current))
					{
						num++;
					}
				}
			}
		}
		if (_fixupCount == 0L)
		{
			if (TopObject is TypeLoadExceptionHolder)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeLoadFailure, ((TypeLoadExceptionHolder)TopObject).TypeName));
			}
			return;
		}
		for (int i = 0; i < _objects.Length; i++)
		{
			for (ObjectHolder current = _objects[i]; current != null; current = current._next)
			{
				if (current.TotalDependentObjects > 0)
				{
					CompleteObject(current, bObjectFullyComplete: true);
				}
			}
			if (_fixupCount == 0L)
			{
				return;
			}
		}
		throw new SerializationException(System.SR.Serialization_IncorrectNumberOfFixups);
	}

	private void RegisterFixup(FixupHolder fixup, long objectToBeFixed, long objectRequired)
	{
		ObjectHolder objectHolder = FindOrCreateObjectHolder(objectToBeFixed);
		if (objectHolder.RequiresSerInfoFixup && fixup._fixupType == 2)
		{
			throw new SerializationException(System.SR.Serialization_InvalidFixupType);
		}
		objectHolder.AddFixup(fixup, this);
		ObjectHolder objectHolder2 = FindOrCreateObjectHolder(objectRequired);
		objectHolder2.AddDependency(objectToBeFixed);
		_fixupCount++;
	}

	public virtual void RecordFixup(long objectToBeFixed, MemberInfo member, long objectRequired)
	{
		if (objectToBeFixed <= 0 || objectRequired <= 0)
		{
			throw new ArgumentOutOfRangeException((objectToBeFixed <= 0) ? "objectToBeFixed" : "objectRequired", System.SR.Serialization_IdTooSmall);
		}
		if (member == null)
		{
			throw new ArgumentNullException("member");
		}
		if (!(member is FieldInfo))
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_InvalidType, member.GetType()));
		}
		FixupHolder fixup = new FixupHolder(objectRequired, member, 2);
		RegisterFixup(fixup, objectToBeFixed, objectRequired);
	}

	public virtual void RecordDelayedFixup(long objectToBeFixed, string memberName, long objectRequired)
	{
		if (objectToBeFixed <= 0 || objectRequired <= 0)
		{
			throw new ArgumentOutOfRangeException((objectToBeFixed <= 0) ? "objectToBeFixed" : "objectRequired", System.SR.Serialization_IdTooSmall);
		}
		if (memberName == null)
		{
			throw new ArgumentNullException("memberName");
		}
		FixupHolder fixup = new FixupHolder(objectRequired, memberName, 4);
		RegisterFixup(fixup, objectToBeFixed, objectRequired);
	}

	public virtual void RecordArrayElementFixup(long arrayToBeFixed, int index, long objectRequired)
	{
		RecordArrayElementFixup(arrayToBeFixed, new int[1] { index }, objectRequired);
	}

	public virtual void RecordArrayElementFixup(long arrayToBeFixed, int[] indices, long objectRequired)
	{
		if (arrayToBeFixed <= 0 || objectRequired <= 0)
		{
			throw new ArgumentOutOfRangeException((arrayToBeFixed <= 0) ? "arrayToBeFixed" : "objectRequired", System.SR.Serialization_IdTooSmall);
		}
		if (indices == null)
		{
			throw new ArgumentNullException("indices");
		}
		FixupHolder fixup = new FixupHolder(objectRequired, indices, 1);
		RegisterFixup(fixup, arrayToBeFixed, objectRequired);
	}

	public virtual void RaiseDeserializationEvent()
	{
		_onDeserializedHandler?.Invoke(_context);
		_onDeserializationHandler?.Invoke(null);
	}

	internal virtual void AddOnDeserialization(DeserializationEventHandler handler)
	{
		_onDeserializationHandler = (DeserializationEventHandler)Delegate.Combine(_onDeserializationHandler, handler);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	internal virtual void AddOnDeserialized(object obj)
	{
		SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
		_onDeserializedHandler = serializationEventsForType.AddOnDeserialized(obj, _onDeserializedHandler);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	internal virtual void RaiseOnDeserializedEvent(object obj)
	{
		SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
		serializationEventsForType.InvokeOnDeserialized(obj, _context);
	}

	[RequiresUnreferencedCode("ObjectManager is not trim compatible because the Type of objects being managed cannot be statically discovered.")]
	public void RaiseOnDeserializingEvent(object obj)
	{
		SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
		serializationEventsForType.InvokeOnDeserializing(obj, _context);
	}
}
