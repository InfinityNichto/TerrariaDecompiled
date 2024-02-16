using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization;

internal abstract class ReflectionReader
{
	private delegate object CollectionReadItemDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs);

	private delegate object CollectionSetItemDelegate(object resultCollection, object collectionItem, int itemIndex);

	private static MethodInfo s_getCollectionSetItemDelegateMethod;

	private static readonly MethodInfo s_objectToKeyValuePairGetKey = typeof(ReflectionReader).GetMethod("ObjectToKeyValuePairGetKey", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly MethodInfo s_objectToKeyValuePairGetValue = typeof(ReflectionReader).GetMethod("ObjectToKeyValuePairGetValue", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly Type[] s_arrayConstructorParameters = new Type[1] { Globals.TypeOfInt };

	private static readonly object[] s_arrayConstructorArguments = new object[1] { 32 };

	private static MethodInfo CollectionSetItemDelegateMethod
	{
		get
		{
			if (s_getCollectionSetItemDelegateMethod == null)
			{
				s_getCollectionSetItemDelegateMethod = typeof(ReflectionReader).GetMethod("GetCollectionSetItemDelegate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getCollectionSetItemDelegateMethod;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, ClassDataContract classContract)
	{
		object obj = CreateObject(classContract);
		context.AddNewObject(obj);
		InvokeOnDeserializing(context, classContract, obj);
		if (classContract.IsISerializable)
		{
			obj = ReadISerializable(xmlReader, context, classContract);
		}
		else
		{
			ReflectionReadMembers(xmlReader, context, memberNames, memberNamespaces, classContract, ref obj);
		}
		if (obj is IObjectReference obj2)
		{
			obj = context.GetRealObject(obj2, context.GetObjectId());
		}
		obj = ResolveAdapterObject(obj, classContract);
		InvokeDeserializationCallback(obj);
		InvokeOnDeserialized(context, classContract, obj);
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString collectionItemName, XmlDictionaryString collectionItemNamespace, CollectionDataContract collectionContract)
	{
		object collectionMember = context.GetCollectionMember();
		if (!ReflectionReadSpecialCollection(xmlReader, context, collectionContract, collectionMember) && xmlReader.IsStartElement(collectionItemName, collectionItemNamespace))
		{
			if (collectionMember == null)
			{
				XmlObjectSerializerReadContext.ThrowNullValueReturnedForGetOnlyCollectionException(collectionContract.UnderlyingType);
			}
			bool isReadOnlyCollection = true;
			collectionMember = ReadCollectionItems(xmlReader, context, collectionItemName, collectionItemNamespace, collectionContract, collectionMember, isReadOnlyCollection);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString collectionItemName, XmlDictionaryString collectionItemNamespace, CollectionDataContract collectionContract)
	{
		return ReflectionReadCollectionCore(xmlReader, context, collectionItemName, collectionItemNamespace, collectionContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReflectionReadCollectionCore(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString collectionItemName, XmlDictionaryString collectionItemNamespace, CollectionDataContract collectionContract)
	{
		bool flag = collectionContract.Kind == CollectionKind.Array;
		int arraySize = context.GetArraySize();
		object resultArray = null;
		if (flag && ReflectionTryReadPrimitiveArray(xmlReader, context, collectionItemName, collectionItemNamespace, collectionContract.UnderlyingType, collectionContract.ItemType, arraySize, out resultArray))
		{
			return resultArray;
		}
		object obj = ReflectionCreateCollection(collectionContract);
		context.AddNewObject(obj);
		context.IncrementItemCount(1);
		if (!ReflectionReadSpecialCollection(xmlReader, context, collectionContract, obj))
		{
			bool isReadOnlyCollection = false;
			obj = ReadCollectionItems(xmlReader, context, collectionItemName, collectionItemNamespace, collectionContract, obj, isReadOnlyCollection);
		}
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionReadItemDelegate GetCollectionReadItemDelegate(CollectionDataContract collectionContract)
	{
		if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
		{
			return GetReadDictionaryItemDelegate;
		}
		return GetReflectionReadValueDelegate(collectionContract.ItemType);
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		object GetReadDictionaryItemDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs)
		{
			return ReflectionReadDictionaryItem(xmlReader, context, collectionContract);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReadCollectionItems(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString collectionItemName, XmlDictionaryString collectionItemNamespace, CollectionDataContract collectionContract, object resultCollection, bool isReadOnlyCollection)
	{
		string collectionContractItemName = GetCollectionContractItemName(collectionContract);
		string collectionContractNamespace = GetCollectionContractNamespace(collectionContract);
		Type itemType = collectionContract.ItemType;
		CollectionReadItemDelegate collectionReadItemDelegate = GetCollectionReadItemDelegate(collectionContract);
		MethodInfo methodInfo = CollectionSetItemDelegateMethod.MakeGenericMethod(itemType);
		CollectionSetItemDelegate collectionSetItemDelegate = (CollectionSetItemDelegate)methodInfo.Invoke(this, new object[3] { collectionContract, resultCollection, isReadOnlyCollection });
		int num = 0;
		while (true)
		{
			if (xmlReader.IsStartElement(collectionItemName, collectionItemNamespace))
			{
				object collectionItem = collectionReadItemDelegate(xmlReader, context, collectionContract, itemType, collectionContractItemName, collectionContractNamespace);
				resultCollection = collectionSetItemDelegate(resultCollection, collectionItem, num);
				num++;
				continue;
			}
			if (xmlReader.NodeType == XmlNodeType.EndElement)
			{
				break;
			}
			if (!xmlReader.IsStartElement())
			{
				throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, xmlReader);
			}
			context.SkipUnknownElement(xmlReader);
		}
		context.IncrementItemCount(num);
		if (!isReadOnlyCollection && IsArrayLikeCollection(collectionContract))
		{
			MethodInfo methodInfo2 = XmlFormatGeneratorStatics.TrimArraySizeMethod.MakeGenericMethod(itemType);
			resultCollection = methodInfo2.Invoke(null, new object[2] { resultCollection, num });
		}
		return resultCollection;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected abstract void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, ClassDataContract classContract, ref object obj);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected abstract object ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract);

	protected abstract string GetCollectionContractItemName(CollectionDataContract collectionContract);

	protected abstract string GetCollectionContractNamespace(CollectionDataContract collectionContract);

	protected abstract string GetClassContractNamespace(ClassDataContract classContract);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual bool ReflectionReadSpecialCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, object resultCollection)
	{
		return false;
	}

	protected int ReflectionGetMembers(ClassDataContract classContract, DataMember[] members)
	{
		int num = ((classContract.BaseContract != null) ? ReflectionGetMembers(classContract.BaseContract, members) : 0);
		int num2 = num;
		int num3 = 0;
		while (num3 < classContract.Members.Count)
		{
			members[num2 + num3] = classContract.Members[num3];
			num3++;
			num++;
		}
		return num;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected void ReflectionReadMember(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, ClassDataContract classContract, ref object obj, int memberIndex, DataMember[] members)
	{
		DataMember dataMember = members[memberIndex];
		if (dataMember.IsGetOnlyCollection)
		{
			object collectionMember = ReflectionGetMemberValue(obj, dataMember);
			context.StoreCollectionMemberInfo(collectionMember);
			ReflectionReadValue(xmlReader, context, dataMember, GetClassContractNamespace(classContract));
		}
		else
		{
			context.ResetCollectionMemberInfo();
			object memberValue = ReflectionReadValue(xmlReader, context, dataMember, classContract.StableName.Namespace);
			MemberInfo memberInfo = dataMember.MemberInfo;
			ReflectionSetMemberValue(ref obj, memberValue, dataMember);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected object ReflectionReadValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, Type type, string name, string ns, PrimitiveDataContract primitiveContractForOriginalType = null)
	{
		object obj = null;
		int num = 0;
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			num++;
			type = type.GetGenericArguments()[0];
		}
		PrimitiveDataContract primitiveDataContract = ((num != 0) ? PrimitiveDataContract.GetPrimitiveDataContract(type) : (primitiveContractForOriginalType ?? PrimitiveDataContract.GetPrimitiveDataContract(type)));
		if ((primitiveDataContract != null && primitiveDataContract.UnderlyingType != Globals.TypeOfObject) || num != 0 || type.IsValueType)
		{
			return ReadItemOfPrimitiveType(xmlReader, context, type, name, ns, primitiveDataContract, num);
		}
		return ReflectionInternalDeserialize(xmlReader, context, null, type, name, ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReadItemOfPrimitiveType(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, Type type, string name, string ns, PrimitiveDataContract primitiveContract, int nullables)
	{
		context.ReadAttributes(xmlReader);
		string text = context.ReadIfNullOrRef(xmlReader, type, DataContract.IsTypeSerializable(type));
		bool isValueType = type.IsValueType;
		if (text != null)
		{
			if (text.Length == 0)
			{
				text = context.GetObjectId();
				if (!string.IsNullOrEmpty(text) && isValueType)
				{
					throw new SerializationException(System.SR.Format(System.SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
				}
				if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
				{
					return primitiveContract.ReadXmlValue(xmlReader, context);
				}
				return ReflectionInternalDeserialize(xmlReader, context, null, type, name, ns);
			}
			if (isValueType)
			{
				throw new SerializationException(System.SR.Format(System.SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
			}
			return context.GetExistingObject(text, type, name, ns);
		}
		if (isValueType && nullables == 0)
		{
			throw new SerializationException(System.SR.Format(System.SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
		}
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static object ReadISerializable(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, ClassDataContract classContract)
	{
		SerializationInfo serializationInfo = context.ReadSerializationInfo(xmlReader, classContract.UnderlyingType);
		StreamingContext streamingContext = context.GetStreamingContext();
		ConstructorInfo iSerializableConstructor = classContract.GetISerializableConstructor();
		return iSerializableConstructor.Invoke(new object[2] { serializationInfo, streamingContext });
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionReadItemDelegate GetReflectionReadValueDelegate(Type type)
	{
		int nullables = 0;
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			nullables++;
			type = type.GetGenericArguments()[0];
		}
		PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
		if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.IsValueType)
		{
			return GetReadItemOfPrimitiveTypeDelegate;
		}
		return ReflectionInternalDeserialize;
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		object GetReadItemOfPrimitiveTypeDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs)
		{
			return ReadItemOfPrimitiveType(xmlReader, context, itemType, itemName, itemNs, primitiveContract, nullables);
		}
	}

	private object ReflectionGetMemberValue(object obj, DataMember dataMember)
	{
		return dataMember.Getter(obj);
	}

	private void ReflectionSetMemberValue(ref object obj, object memberValue, DataMember dataMember)
	{
		dataMember.Setter(ref obj, memberValue);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReflectionReadValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, DataMember dataMember, string ns)
	{
		Type memberType = dataMember.MemberType;
		string name = dataMember.Name;
		return ReflectionReadValue(xmlReader, context, memberType, name, ns, dataMember.MemberPrimitiveContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReflectionInternalDeserialize(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type type, string name, string ns)
	{
		return context.InternalDeserialize(xmlReader, DataContract.GetId(type.TypeHandle), type.TypeHandle, name, ns);
	}

	private void InvokeOnDeserializing(XmlObjectSerializerReadContext context, ClassDataContract classContract, object obj)
	{
		if (classContract.BaseContract != null)
		{
			InvokeOnDeserializing(context, classContract.BaseContract, obj);
		}
		if (classContract.OnDeserializing != null)
		{
			StreamingContext streamingContext = context.GetStreamingContext();
			classContract.OnDeserializing.Invoke(obj, new object[1] { streamingContext });
		}
	}

	private void InvokeOnDeserialized(XmlObjectSerializerReadContext context, ClassDataContract classContract, object obj)
	{
		if (classContract.BaseContract != null)
		{
			InvokeOnDeserialized(context, classContract.BaseContract, obj);
		}
		if (classContract.OnDeserialized != null)
		{
			StreamingContext streamingContext = context.GetStreamingContext();
			classContract.OnDeserialized.Invoke(obj, new object[1] { streamingContext });
		}
	}

	private void InvokeDeserializationCallback(object obj)
	{
		if (obj is IDeserializationCallback deserializationCallback)
		{
			deserializationCallback.OnDeserialization(null);
		}
	}

	private static object CreateObject(ClassDataContract classContract)
	{
		if (!classContract.CreateNewInstanceViaDefaultConstructor(out var obj))
		{
			Type underlyingType = classContract.UnderlyingType;
			return XmlFormatReaderGenerator.UnsafeGetUninitializedObject(underlyingType);
		}
		return obj;
	}

	private static object ResolveAdapterObject(object obj, ClassDataContract classContract)
	{
		Type type = obj.GetType();
		if (type == Globals.TypeOfDateTimeOffsetAdapter)
		{
			obj = DateTimeOffsetAdapter.GetDateTimeOffset((DateTimeOffsetAdapter)obj);
		}
		else if (type == Globals.TypeOfMemoryStreamAdapter)
		{
			obj = MemoryStreamAdapter.GetMemoryStream((MemoryStreamAdapter)obj);
		}
		else if (obj is IKeyValuePairAdapter)
		{
			obj = classContract.GetKeyValuePairMethodInfo.Invoke(obj, Array.Empty<object>());
		}
		return obj;
	}

	private bool IsArrayLikeInterface(CollectionDataContract collectionContract)
	{
		if (collectionContract.UnderlyingType.IsInterface)
		{
			CollectionKind kind = collectionContract.Kind;
			if (kind - 3 <= CollectionKind.List)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsArrayLikeCollection(CollectionDataContract collectionContract)
	{
		if (collectionContract.Kind != CollectionKind.Array)
		{
			return IsArrayLikeInterface(collectionContract);
		}
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ReflectionCreateCollection(CollectionDataContract collectionContract)
	{
		if (IsArrayLikeCollection(collectionContract))
		{
			Type type = collectionContract.ItemType.MakeArrayType();
			ConstructorInfo constructor = type.GetConstructor(s_arrayConstructorParameters);
			return constructor.Invoke(s_arrayConstructorArguments);
		}
		if (collectionContract.Kind == CollectionKind.GenericDictionary && collectionContract.UnderlyingType.IsInterface)
		{
			Type type2 = Globals.TypeOfDictionaryGeneric.MakeGenericType(collectionContract.ItemType.GetGenericArguments());
			ConstructorInfo constructor2 = type2.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
			return constructor2.Invoke(Array.Empty<object>());
		}
		if (collectionContract.UnderlyingType.IsValueType)
		{
			return Activator.CreateInstance(collectionContract.UnderlyingType);
		}
		if (collectionContract.UnderlyingType == Globals.TypeOfIDictionary)
		{
			return new Dictionary<object, object>();
		}
		ConstructorInfo constructor3 = collectionContract.Constructor;
		return constructor3.Invoke(Array.Empty<object>());
	}

	private static object ObjectToKeyValuePairGetKey<K, V>(object o)
	{
		return ((KeyValue<K, V>)o).Key;
	}

	private static object ObjectToKeyValuePairGetValue<K, V>(object o)
	{
		return ((KeyValue<K, V>)o).Value;
	}

	private CollectionSetItemDelegate GetCollectionSetItemDelegate<T>(CollectionDataContract collectionContract, object resultCollectionObject, bool isReadOnlyCollection)
	{
		if (isReadOnlyCollection && collectionContract.Kind == CollectionKind.Array)
		{
			int arraySize = ((Array)resultCollectionObject).Length;
			return delegate(object resultCollection, object collectionItem, int index)
			{
				if (index == arraySize)
				{
					XmlObjectSerializerReadContext.ThrowArrayExceededSizeException(arraySize, collectionContract.UnderlyingType);
				}
				((T[])resultCollection)[index] = (T)collectionItem;
				return resultCollection;
			};
		}
		if (!isReadOnlyCollection && IsArrayLikeCollection(collectionContract))
		{
			return delegate(object resultCollection, object collectionItem, int index)
			{
				resultCollection = XmlObjectSerializerReadContext.EnsureArraySize((T[])resultCollection, index);
				((T[])resultCollection)[index] = (T)collectionItem;
				return resultCollection;
			};
		}
		if (collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary)
		{
			Type keyType2 = collectionContract.ItemType.GenericTypeArguments[0];
			Type valueType2 = collectionContract.ItemType.GenericTypeArguments[1];
			Func<object, object> objectToKeyValuePairGetKey = MakeGenericMethod(s_objectToKeyValuePairGetKey, keyType2, valueType2).CreateDelegate<Func<object, object>>();
			Func<object, object> objectToKeyValuePairGetValue = MakeGenericMethod(s_objectToKeyValuePairGetValue, keyType2, valueType2).CreateDelegate<Func<object, object>>();
			if (collectionContract.Kind == CollectionKind.GenericDictionary)
			{
				return delegate(object resultCollection, object collectionItem, int index)
				{
					object obj = objectToKeyValuePairGetKey(collectionItem);
					object obj2 = objectToKeyValuePairGetValue(collectionItem);
					collectionContract.AddMethod.Invoke(resultCollection, new object[2] { obj, obj2 });
					return resultCollection;
				};
			}
			return delegate(object resultCollection, object collectionItem, int index)
			{
				object key = objectToKeyValuePairGetKey(collectionItem);
				object value = objectToKeyValuePairGetValue(collectionItem);
				IDictionary dictionary = (IDictionary)resultCollection;
				dictionary.Add(key, value);
				return resultCollection;
			};
		}
		Type type = resultCollectionObject.GetType();
		Type typeFromHandle = typeof(ICollection<T>);
		Type typeOfIList = Globals.TypeOfIList;
		if (typeFromHandle.IsAssignableFrom(type))
		{
			return delegate(object resultCollection, object collectionItem, int index)
			{
				((ICollection<T>)resultCollection).Add((T)collectionItem);
				return resultCollection;
			};
		}
		if (typeOfIList.IsAssignableFrom(type))
		{
			return delegate(object resultCollection, object collectionItem, int index)
			{
				((IList)resultCollection).Add(collectionItem);
				return resultCollection;
			};
		}
		MethodInfo addMethod = collectionContract.AddMethod;
		if (addMethod == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType))));
		}
		return delegate(object resultCollection, object collectionItem, int index)
		{
			addMethod.Invoke(resultCollection, new object[1] { collectionItem });
			return resultCollection;
		};
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that ObjectToKeyValuePairGetKey and ObjectToKeyValuePairGetValue are not annotated.")]
		static MethodInfo MakeGenericMethod(MethodInfo method, Type keyType, Type valueType)
		{
			return method.MakeGenericMethod(keyType, valueType);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool ReflectionTryReadPrimitiveArray(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString collectionItemName, XmlDictionaryString collectionItemNamespace, Type type, Type itemType, int arraySize, [NotNullWhen(true)] out object resultArray)
	{
		resultArray = null;
		PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
		if (primitiveDataContract == null)
		{
			return false;
		}
		switch (itemType.GetTypeCode())
		{
		case TypeCode.Boolean:
		{
			if (xmlReader.TryReadBooleanArray(context, collectionItemName, collectionItemNamespace, arraySize, out var array6))
			{
				resultArray = array6;
			}
			break;
		}
		case TypeCode.DateTime:
		{
			if (xmlReader.TryReadDateTimeArray(context, collectionItemName, collectionItemNamespace, arraySize, out var array2))
			{
				resultArray = array2;
			}
			break;
		}
		case TypeCode.Decimal:
		{
			if (xmlReader.TryReadDecimalArray(context, collectionItemName, collectionItemNamespace, arraySize, out var array4))
			{
				resultArray = array4;
			}
			break;
		}
		case TypeCode.Int32:
		{
			if (xmlReader.TryReadInt32Array(context, collectionItemName, collectionItemNamespace, arraySize, out var array7))
			{
				resultArray = array7;
			}
			break;
		}
		case TypeCode.Int64:
		{
			if (xmlReader.TryReadInt64Array(context, collectionItemName, collectionItemNamespace, arraySize, out var array5))
			{
				resultArray = array5;
			}
			break;
		}
		case TypeCode.Single:
		{
			if (xmlReader.TryReadSingleArray(context, collectionItemName, collectionItemNamespace, arraySize, out var array3))
			{
				resultArray = array3;
			}
			break;
		}
		case TypeCode.Double:
		{
			if (xmlReader.TryReadDoubleArray(context, collectionItemName, collectionItemNamespace, arraySize, out var array))
			{
				resultArray = array;
			}
			break;
		}
		default:
			return false;
		}
		return resultArray != null;
	}
}
