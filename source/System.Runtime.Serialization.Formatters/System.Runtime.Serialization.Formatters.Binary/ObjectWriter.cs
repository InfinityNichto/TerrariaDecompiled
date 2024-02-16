using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ObjectWriter
{
	private Queue<object> _objectQueue;

	private ObjectIDGenerator _idGenerator;

	private int _currentId;

	private readonly ISurrogateSelector _surrogates;

	private readonly StreamingContext _context;

	private BinaryFormatterWriter _serWriter;

	private readonly SerializationObjectManager _objectManager;

	private long _topId;

	private readonly InternalFE _formatterEnums;

	private readonly SerializationBinder _binder;

	private SerObjectInfoInit _serObjectInfoInit;

	private IFormatterConverter _formatterConverter;

	internal object[] _crossAppDomainArray;

	private object _previousObj;

	private long _previousId;

	private Type _previousType;

	private InternalPrimitiveTypeE _previousCode;

	private Dictionary<string, long> _assemblyToIdTable;

	private readonly SerStack _niPool = new SerStack("NameInfo Pool");

	internal SerializationObjectManager ObjectManager => _objectManager;

	internal ObjectWriter(ISurrogateSelector selector, StreamingContext context, InternalFE formatterEnums, SerializationBinder binder)
	{
		_currentId = 1;
		_surrogates = selector;
		_context = context;
		_binder = binder;
		_formatterEnums = formatterEnums;
		_objectManager = new SerializationObjectManager(context);
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	internal void Serialize(object graph, BinaryFormatterWriter serWriter)
	{
		if (graph == null)
		{
			throw new ArgumentNullException("graph");
		}
		if (serWriter == null)
		{
			throw new ArgumentNullException("serWriter");
		}
		_serWriter = serWriter;
		serWriter.WriteBegin();
		long num = 0L;
		_idGenerator = new ObjectIDGenerator();
		_objectQueue = new Queue<object>();
		_formatterConverter = new FormatterConverter();
		_serObjectInfoInit = new SerObjectInfoInit();
		_topId = InternalGetId(graph, assignUniqueIdToValueType: false, null, out var _);
		num = -1L;
		WriteSerializedStreamHeader(_topId, num);
		_objectQueue.Enqueue(graph);
		object next;
		long objID;
		while ((next = GetNext(out objID)) != null)
		{
			WriteObjectInfo writeObjectInfo = null;
			if (next is WriteObjectInfo)
			{
				writeObjectInfo = (WriteObjectInfo)next;
			}
			else
			{
				writeObjectInfo = WriteObjectInfo.Serialize(next, _surrogates, _context, _serObjectInfoInit, _formatterConverter, this, _binder);
				writeObjectInfo._assemId = GetAssemblyId(writeObjectInfo);
			}
			writeObjectInfo._objectId = objID;
			NameInfo nameInfo = TypeToNameInfo(writeObjectInfo);
			Write(writeObjectInfo, nameInfo, nameInfo);
			PutNameInfo(nameInfo);
			writeObjectInfo.ObjectEnd();
		}
		serWriter.WriteSerializationHeaderEnd();
		serWriter.WriteEnd();
		_objectManager.RaiseOnSerializedEvent();
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void Write(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo)
	{
		object obj = objectInfo._obj;
		if (obj == null)
		{
			throw new ArgumentNullException("objectInfo._obj");
		}
		Type objectType = objectInfo._objectType;
		long objectId = objectInfo._objectId;
		if ((object)objectType == Converter.s_typeofString)
		{
			memberNameInfo._objectId = objectId;
			_serWriter.WriteObjectString((int)objectId, obj.ToString());
			return;
		}
		if (objectInfo._isArray)
		{
			WriteArray(objectInfo, memberNameInfo, null);
			return;
		}
		objectInfo.GetMemberInfo(out var outMemberNames, out var outMemberTypes, out var outMemberData);
		if (objectInfo._isSi || CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways))
		{
			memberNameInfo._transmitTypeOnObject = true;
			memberNameInfo._isParentTypeOnObject = true;
			typeNameInfo._transmitTypeOnObject = true;
			typeNameInfo._isParentTypeOnObject = true;
		}
		WriteObjectInfo[] array = new WriteObjectInfo[outMemberNames.Length];
		for (int i = 0; i < outMemberTypes.Length; i++)
		{
			Type type = ((outMemberTypes[i] != null) ? outMemberTypes[i] : ((outMemberData[i] != null) ? GetType(outMemberData[i]) : Converter.s_typeofObject));
			if (ToCode(type) == InternalPrimitiveTypeE.Invalid && (object)type != Converter.s_typeofString)
			{
				if (outMemberData[i] != null)
				{
					array[i] = WriteObjectInfo.Serialize(outMemberData[i], _surrogates, _context, _serObjectInfoInit, _formatterConverter, this, _binder);
					array[i]._assemId = GetAssemblyId(array[i]);
				}
				else
				{
					array[i] = WriteObjectInfo.Serialize(outMemberTypes[i], _surrogates, _context, _serObjectInfoInit, _formatterConverter, _binder);
					array[i]._assemId = GetAssemblyId(array[i]);
				}
			}
		}
		Write(objectInfo, memberNameInfo, typeNameInfo, outMemberNames, outMemberTypes, outMemberData, array);
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void Write(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo, string[] memberNames, Type[] memberTypes, object[] memberData, WriteObjectInfo[] memberObjectInfos)
	{
		int num = memberNames.Length;
		if (memberNameInfo != null)
		{
			memberNameInfo._objectId = objectInfo._objectId;
			_serWriter.WriteObject(memberNameInfo, typeNameInfo, num, memberNames, memberTypes, memberObjectInfos);
		}
		else if ((object)objectInfo._objectType != Converter.s_typeofString)
		{
			typeNameInfo._objectId = objectInfo._objectId;
			_serWriter.WriteObject(typeNameInfo, null, num, memberNames, memberTypes, memberObjectInfos);
		}
		if (memberNameInfo._isParentTypeOnObject)
		{
			memberNameInfo._transmitTypeOnObject = true;
			memberNameInfo._isParentTypeOnObject = false;
		}
		else
		{
			memberNameInfo._transmitTypeOnObject = false;
		}
		for (int i = 0; i < num; i++)
		{
			WriteMemberSetup(objectInfo, memberNameInfo, typeNameInfo, memberNames[i], memberTypes[i], memberData[i], memberObjectInfos[i]);
		}
		if (memberNameInfo != null)
		{
			memberNameInfo._objectId = objectInfo._objectId;
			_serWriter.WriteObjectEnd(memberNameInfo, typeNameInfo);
		}
		else if ((object)objectInfo._objectType != Converter.s_typeofString)
		{
			_serWriter.WriteObjectEnd(typeNameInfo, typeNameInfo);
		}
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void WriteMemberSetup(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo, string memberName, Type memberType, object memberData, WriteObjectInfo memberObjectInfo)
	{
		NameInfo nameInfo = MemberToNameInfo(memberName);
		if (memberObjectInfo != null)
		{
			nameInfo._assemId = memberObjectInfo._assemId;
		}
		nameInfo._type = memberType;
		NameInfo nameInfo2 = ((memberObjectInfo != null) ? TypeToNameInfo(memberObjectInfo) : TypeToNameInfo(memberType));
		nameInfo._transmitTypeOnObject = memberNameInfo._transmitTypeOnObject;
		nameInfo._isParentTypeOnObject = memberNameInfo._isParentTypeOnObject;
		WriteMembers(nameInfo, nameInfo2, memberData, objectInfo, typeNameInfo, memberObjectInfo);
		PutNameInfo(nameInfo);
		PutNameInfo(nameInfo2);
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void WriteMembers(NameInfo memberNameInfo, NameInfo memberTypeNameInfo, object memberData, WriteObjectInfo objectInfo, NameInfo typeNameInfo, WriteObjectInfo memberObjectInfo)
	{
		Type type = memberNameInfo._type;
		bool assignUniqueIdToValueType = false;
		if ((object)type == Converter.s_typeofObject || Nullable.GetUnderlyingType(type) != null)
		{
			memberTypeNameInfo._transmitTypeOnMember = true;
			memberNameInfo._transmitTypeOnMember = true;
		}
		if (CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways) || objectInfo._isSi)
		{
			memberTypeNameInfo._transmitTypeOnObject = true;
			memberNameInfo._transmitTypeOnObject = true;
			memberNameInfo._isParentTypeOnObject = true;
		}
		if (CheckForNull(objectInfo, memberNameInfo, memberTypeNameInfo, memberData))
		{
			return;
		}
		Type type2 = null;
		if (memberTypeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Invalid)
		{
			type2 = GetType(memberData);
			if ((object)type != type2)
			{
				memberTypeNameInfo._transmitTypeOnMember = true;
				memberNameInfo._transmitTypeOnMember = true;
			}
		}
		if ((object)type == Converter.s_typeofObject)
		{
			assignUniqueIdToValueType = true;
			type = GetType(memberData);
			if (memberObjectInfo == null)
			{
				TypeToNameInfo(type, memberTypeNameInfo);
			}
			else
			{
				TypeToNameInfo(memberObjectInfo, memberTypeNameInfo);
			}
		}
		if (memberObjectInfo != null && memberObjectInfo._isArray)
		{
			long num = Schedule(memberData, assignUniqueIdToValueType: false, null, memberObjectInfo);
			if (num > 0)
			{
				memberNameInfo._objectId = num;
				WriteObjectRef(memberNameInfo, num);
				return;
			}
			_serWriter.WriteMemberNested(memberNameInfo);
			memberObjectInfo._objectId = num;
			memberNameInfo._objectId = num;
			WriteArray(memberObjectInfo, memberNameInfo, memberObjectInfo);
			objectInfo.ObjectEnd();
		}
		else if (!WriteKnownValueClass(memberNameInfo, memberTypeNameInfo, memberData))
		{
			if (type2 == null)
			{
				type2 = GetType(memberData);
			}
			long num2 = Schedule(memberData, assignUniqueIdToValueType, type2, memberObjectInfo);
			if (num2 < 0)
			{
				memberObjectInfo._objectId = num2;
				NameInfo nameInfo = TypeToNameInfo(memberObjectInfo);
				nameInfo._objectId = num2;
				Write(memberObjectInfo, memberNameInfo, nameInfo);
				PutNameInfo(nameInfo);
				memberObjectInfo.ObjectEnd();
			}
			else
			{
				memberNameInfo._objectId = num2;
				WriteObjectRef(memberNameInfo, num2);
			}
		}
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void WriteArray(WriteObjectInfo objectInfo, NameInfo memberNameInfo, WriteObjectInfo memberObjectInfo)
	{
		bool flag = false;
		if (memberNameInfo == null)
		{
			memberNameInfo = TypeToNameInfo(objectInfo);
			flag = true;
		}
		memberNameInfo._isArray = true;
		long objectId = objectInfo._objectId;
		memberNameInfo._objectId = objectInfo._objectId;
		Array array = (Array)objectInfo._obj;
		Type objectType = objectInfo._objectType;
		Type elementType = objectType.GetElementType();
		WriteObjectInfo writeObjectInfo = null;
		if (!elementType.IsPrimitive)
		{
			writeObjectInfo = WriteObjectInfo.Serialize(elementType, _surrogates, _context, _serObjectInfoInit, _formatterConverter, _binder);
			writeObjectInfo._assemId = GetAssemblyId(writeObjectInfo);
		}
		NameInfo nameInfo = ((writeObjectInfo == null) ? TypeToNameInfo(elementType) : TypeToNameInfo(writeObjectInfo));
		nameInfo._isArray = nameInfo._type.IsArray;
		NameInfo nameInfo2 = memberNameInfo;
		nameInfo2._objectId = objectId;
		nameInfo2._isArray = true;
		nameInfo._objectId = objectId;
		nameInfo._transmitTypeOnMember = memberNameInfo._transmitTypeOnMember;
		nameInfo._transmitTypeOnObject = memberNameInfo._transmitTypeOnObject;
		nameInfo._isParentTypeOnObject = memberNameInfo._isParentTypeOnObject;
		int rank = array.Rank;
		int[] array2 = new int[rank];
		int[] array3 = new int[rank];
		int[] array4 = new int[rank];
		for (int i = 0; i < rank; i++)
		{
			array2[i] = array.GetLength(i);
			array3[i] = array.GetLowerBound(i);
			array4[i] = array.GetUpperBound(i);
		}
		InternalArrayTypeE internalArrayTypeE = (nameInfo._arrayEnum = (nameInfo._isArray ? ((rank == 1) ? InternalArrayTypeE.Jagged : InternalArrayTypeE.Rectangular) : ((rank == 1) ? InternalArrayTypeE.Single : InternalArrayTypeE.Rectangular)));
		if ((object)elementType == Converter.s_typeofByte && rank == 1 && array3[0] == 0)
		{
			_serWriter.WriteObjectByteArray(memberNameInfo, nameInfo2, writeObjectInfo, nameInfo, array2[0], array3[0], (byte[])array);
			return;
		}
		if ((object)elementType == Converter.s_typeofObject || Nullable.GetUnderlyingType(elementType) != null)
		{
			memberNameInfo._transmitTypeOnMember = true;
			nameInfo._transmitTypeOnMember = true;
		}
		if (CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways))
		{
			memberNameInfo._transmitTypeOnObject = true;
			nameInfo._transmitTypeOnObject = true;
		}
		switch (internalArrayTypeE)
		{
		case InternalArrayTypeE.Single:
		{
			_serWriter.WriteSingleArray(memberNameInfo, nameInfo2, writeObjectInfo, nameInfo, array2[0], array3[0], array);
			if (Converter.IsWriteAsByteArray(nameInfo._primitiveTypeEnum) && array3[0] == 0)
			{
				break;
			}
			object[] array5 = null;
			if (!elementType.IsValueType)
			{
				array5 = (object[])array;
			}
			int num = array4[0] + 1;
			for (int k = array3[0]; k < num; k++)
			{
				if (array5 == null)
				{
					WriteArrayMember(objectInfo, nameInfo, array.GetValue(k));
				}
				else
				{
					WriteArrayMember(objectInfo, nameInfo, array5[k]);
				}
			}
			_serWriter.WriteItemEnd();
			break;
		}
		case InternalArrayTypeE.Jagged:
		{
			nameInfo2._objectId = objectId;
			_serWriter.WriteJaggedArray(memberNameInfo, nameInfo2, writeObjectInfo, nameInfo, array2[0], array3[0]);
			Array array6 = array;
			for (int l = array3[0]; l < array4[0] + 1; l++)
			{
				WriteArrayMember(objectInfo, nameInfo, array6.GetValue(l));
			}
			_serWriter.WriteItemEnd();
			break;
		}
		default:
		{
			nameInfo2._objectId = objectId;
			_serWriter.WriteRectangleArray(memberNameInfo, nameInfo2, writeObjectInfo, nameInfo, rank, array2, array3);
			bool flag2 = false;
			for (int j = 0; j < rank; j++)
			{
				if (array2[j] == 0)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				WriteRectangle(objectInfo, rank, array2, array, nameInfo, array3);
			}
			_serWriter.WriteItemEnd();
			break;
		}
		}
		_serWriter.WriteObjectEnd(memberNameInfo, nameInfo2);
		PutNameInfo(nameInfo);
		if (flag)
		{
			PutNameInfo(memberNameInfo);
		}
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void WriteArrayMember(WriteObjectInfo objectInfo, NameInfo arrayElemTypeNameInfo, object data)
	{
		arrayElemTypeNameInfo._isArrayItem = true;
		if (CheckForNull(objectInfo, arrayElemTypeNameInfo, arrayElemTypeNameInfo, data))
		{
			return;
		}
		NameInfo nameInfo = null;
		Type type = null;
		bool flag = false;
		if (arrayElemTypeNameInfo._transmitTypeOnMember)
		{
			flag = true;
		}
		if (!flag && !arrayElemTypeNameInfo.IsSealed)
		{
			type = GetType(data);
			if ((object)arrayElemTypeNameInfo._type != type)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (type == null)
			{
				type = GetType(data);
			}
			nameInfo = TypeToNameInfo(type);
			nameInfo._transmitTypeOnMember = true;
			nameInfo._objectId = arrayElemTypeNameInfo._objectId;
			nameInfo._assemId = arrayElemTypeNameInfo._assemId;
			nameInfo._isArrayItem = true;
		}
		else
		{
			nameInfo = arrayElemTypeNameInfo;
			nameInfo._isArrayItem = true;
		}
		if (!WriteKnownValueClass(arrayElemTypeNameInfo, nameInfo, data))
		{
			bool assignUniqueIdToValueType = false;
			if ((object)arrayElemTypeNameInfo._type == Converter.s_typeofObject)
			{
				assignUniqueIdToValueType = true;
			}
			long num = (nameInfo._objectId = (arrayElemTypeNameInfo._objectId = Schedule(data, assignUniqueIdToValueType, nameInfo._type)));
			if (num < 1)
			{
				WriteObjectInfo writeObjectInfo = WriteObjectInfo.Serialize(data, _surrogates, _context, _serObjectInfoInit, _formatterConverter, this, _binder);
				writeObjectInfo._objectId = num;
				writeObjectInfo._assemId = (((object)arrayElemTypeNameInfo._type != Converter.s_typeofObject && Nullable.GetUnderlyingType(arrayElemTypeNameInfo._type) == null) ? nameInfo._assemId : GetAssemblyId(writeObjectInfo));
				NameInfo nameInfo2 = TypeToNameInfo(writeObjectInfo);
				nameInfo2._objectId = num;
				writeObjectInfo._objectId = num;
				Write(writeObjectInfo, nameInfo, nameInfo2);
				writeObjectInfo.ObjectEnd();
			}
			else
			{
				_serWriter.WriteItemObjectRef(arrayElemTypeNameInfo, (int)num);
			}
		}
		if (arrayElemTypeNameInfo._transmitTypeOnMember)
		{
			PutNameInfo(nameInfo);
		}
	}

	[RequiresUnreferencedCode("ObjectWriter requires unreferenced code")]
	private void WriteRectangle(WriteObjectInfo objectInfo, int rank, int[] maxA, Array array, NameInfo arrayElemNameTypeInfo, int[] lowerBoundA)
	{
		int[] array2 = new int[rank];
		int[] array3 = null;
		bool flag = false;
		if (lowerBoundA != null)
		{
			for (int i = 0; i < rank; i++)
			{
				if (lowerBoundA[i] != 0)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			array3 = new int[rank];
		}
		bool flag2 = true;
		while (flag2)
		{
			flag2 = false;
			if (flag)
			{
				for (int j = 0; j < rank; j++)
				{
					array3[j] = array2[j] + lowerBoundA[j];
				}
				WriteArrayMember(objectInfo, arrayElemNameTypeInfo, array.GetValue(array3));
			}
			else
			{
				WriteArrayMember(objectInfo, arrayElemNameTypeInfo, array.GetValue(array2));
			}
			for (int num = rank - 1; num > -1; num--)
			{
				if (array2[num] < maxA[num] - 1)
				{
					array2[num]++;
					if (num < rank - 1)
					{
						for (int k = num + 1; k < rank; k++)
						{
							array2[k] = 0;
						}
					}
					flag2 = true;
					break;
				}
			}
		}
	}

	private object GetNext(out long objID)
	{
		if (_objectQueue.Count == 0)
		{
			objID = 0L;
			return null;
		}
		object obj = _objectQueue.Dequeue();
		object obj2 = ((obj is WriteObjectInfo) ? ((WriteObjectInfo)obj)._obj : obj);
		objID = _idGenerator.HasId(obj2, out var firstTime);
		if (firstTime)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjNoID, obj2));
		}
		return obj;
	}

	private long InternalGetId(object obj, bool assignUniqueIdToValueType, Type type, out bool isNew)
	{
		if (obj == _previousObj)
		{
			isNew = false;
			return _previousId;
		}
		_idGenerator._currentCount = _currentId;
		if (type != null && type.IsValueType && !assignUniqueIdToValueType)
		{
			isNew = false;
			return -1 * _currentId++;
		}
		_currentId++;
		long id = _idGenerator.GetId(obj, out isNew);
		_previousObj = obj;
		_previousId = id;
		return id;
	}

	private long Schedule(object obj, bool assignUniqueIdToValueType, Type type)
	{
		return Schedule(obj, assignUniqueIdToValueType, type, null);
	}

	private long Schedule(object obj, bool assignUniqueIdToValueType, Type type, WriteObjectInfo objectInfo)
	{
		long num = 0L;
		if (obj != null)
		{
			num = InternalGetId(obj, assignUniqueIdToValueType, type, out var isNew);
			if (isNew && num > 0)
			{
				_objectQueue.Enqueue(objectInfo ?? obj);
			}
		}
		return num;
	}

	private bool WriteKnownValueClass(NameInfo memberNameInfo, NameInfo typeNameInfo, object data)
	{
		if ((object)typeNameInfo._type == Converter.s_typeofString)
		{
			WriteString(memberNameInfo, typeNameInfo, data);
		}
		else
		{
			if (typeNameInfo._primitiveTypeEnum == InternalPrimitiveTypeE.Invalid)
			{
				return false;
			}
			if (typeNameInfo._isArray)
			{
				_serWriter.WriteItem(memberNameInfo, typeNameInfo, data);
			}
			else
			{
				_serWriter.WriteMember(memberNameInfo, typeNameInfo, data);
			}
		}
		return true;
	}

	private void WriteObjectRef(NameInfo nameInfo, long objectId)
	{
		_serWriter.WriteMemberObjectRef(nameInfo, (int)objectId);
	}

	private void WriteString(NameInfo memberNameInfo, NameInfo typeNameInfo, object stringObject)
	{
		bool isNew = true;
		long num = -1L;
		if (!CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.XsdString))
		{
			num = InternalGetId(stringObject, assignUniqueIdToValueType: false, null, out isNew);
		}
		typeNameInfo._objectId = num;
		if (isNew || num < 0)
		{
			_serWriter.WriteMemberString(memberNameInfo, typeNameInfo, (string)stringObject);
		}
		else
		{
			WriteObjectRef(memberNameInfo, num);
		}
	}

	private bool CheckForNull(WriteObjectInfo objectInfo, NameInfo memberNameInfo, NameInfo typeNameInfo, object data)
	{
		bool flag = data == null;
		if (flag && (_formatterEnums._serializerTypeEnum == InternalSerializerTypeE.Binary || memberNameInfo._isArrayItem || memberNameInfo._transmitTypeOnObject || memberNameInfo._transmitTypeOnMember || objectInfo._isSi || CheckTypeFormat(_formatterEnums._typeFormat, FormatterTypeStyle.TypesAlways)))
		{
			if (typeNameInfo._isArrayItem)
			{
				if (typeNameInfo._arrayEnum == InternalArrayTypeE.Single)
				{
					_serWriter.WriteDelayedNullItem();
				}
				else
				{
					_serWriter.WriteNullItem(memberNameInfo, typeNameInfo);
				}
			}
			else
			{
				_serWriter.WriteNullMember(memberNameInfo, typeNameInfo);
			}
		}
		return flag;
	}

	private void WriteSerializedStreamHeader(long topId, long headerId)
	{
		_serWriter.WriteSerializationHeader((int)topId, (int)headerId, 1, 0);
	}

	private NameInfo TypeToNameInfo(Type type, WriteObjectInfo objectInfo, InternalPrimitiveTypeE code, NameInfo nameInfo)
	{
		if (nameInfo == null)
		{
			nameInfo = GetNameInfo();
		}
		else
		{
			nameInfo.Init();
		}
		if (code == InternalPrimitiveTypeE.Invalid && objectInfo != null)
		{
			nameInfo.NIname = objectInfo.GetTypeFullName();
			nameInfo._assemId = objectInfo._assemId;
		}
		nameInfo._primitiveTypeEnum = code;
		nameInfo._type = type;
		return nameInfo;
	}

	private NameInfo TypeToNameInfo(Type type)
	{
		return TypeToNameInfo(type, null, ToCode(type), null);
	}

	private NameInfo TypeToNameInfo(WriteObjectInfo objectInfo)
	{
		return TypeToNameInfo(objectInfo._objectType, objectInfo, ToCode(objectInfo._objectType), null);
	}

	private NameInfo TypeToNameInfo(WriteObjectInfo objectInfo, NameInfo nameInfo)
	{
		return TypeToNameInfo(objectInfo._objectType, objectInfo, ToCode(objectInfo._objectType), nameInfo);
	}

	private void TypeToNameInfo(Type type, NameInfo nameInfo)
	{
		TypeToNameInfo(type, null, ToCode(type), nameInfo);
	}

	private NameInfo MemberToNameInfo(string name)
	{
		NameInfo nameInfo = GetNameInfo();
		nameInfo.NIname = name;
		return nameInfo;
	}

	internal InternalPrimitiveTypeE ToCode(Type type)
	{
		if ((object)_previousType == type)
		{
			return _previousCode;
		}
		InternalPrimitiveTypeE internalPrimitiveTypeE = Converter.ToCode(type);
		if (internalPrimitiveTypeE != 0)
		{
			_previousType = type;
			_previousCode = internalPrimitiveTypeE;
		}
		return internalPrimitiveTypeE;
	}

	private long GetAssemblyId(WriteObjectInfo objectInfo)
	{
		if (_assemblyToIdTable == null)
		{
			_assemblyToIdTable = new Dictionary<string, long>();
		}
		long value = 0L;
		string assemblyString = objectInfo.GetAssemblyString();
		string assemblyString2 = assemblyString;
		if (assemblyString.Length == 0)
		{
			value = 0L;
		}
		else if (assemblyString.Equals(Converter.s_urtAssemblyString) || assemblyString.Equals(Converter.s_urtAlternativeAssemblyString))
		{
			value = 0L;
		}
		else
		{
			bool isNew = false;
			if (_assemblyToIdTable.TryGetValue(assemblyString, out value))
			{
				isNew = false;
			}
			else
			{
				value = InternalGetId("___AssemblyString___" + assemblyString, assignUniqueIdToValueType: false, null, out isNew);
				_assemblyToIdTable[assemblyString] = value;
			}
			_serWriter.WriteAssembly(objectInfo._objectType, assemblyString2, (int)value, isNew);
		}
		return value;
	}

	private Type GetType(object obj)
	{
		return obj.GetType();
	}

	private NameInfo GetNameInfo()
	{
		NameInfo nameInfo;
		if (!_niPool.IsEmpty())
		{
			nameInfo = (NameInfo)_niPool.Pop();
			nameInfo.Init();
		}
		else
		{
			nameInfo = new NameInfo();
		}
		return nameInfo;
	}

	private bool CheckTypeFormat(FormatterTypeStyle test, FormatterTypeStyle want)
	{
		return (test & want) == want;
	}

	private void PutNameInfo(NameInfo nameInfo)
	{
		_niPool.Push(nameInfo);
	}
}
