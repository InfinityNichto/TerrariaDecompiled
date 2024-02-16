using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ObjectReader
{
	internal sealed class TypeNAssembly
	{
		public Type Type;

		public string AssemblyName;
	}

	internal sealed class TopLevelAssemblyTypeResolver
	{
		private readonly Assembly _topLevelAssembly;

		public TopLevelAssemblyTypeResolver(Assembly topLevelAssembly)
		{
			_topLevelAssembly = topLevelAssembly;
		}

		[RequiresUnreferencedCode("Types might be removed")]
		public Type ResolveType(Assembly assembly, string simpleTypeName, bool ignoreCase)
		{
			if (assembly == null)
			{
				assembly = _topLevelAssembly;
			}
			return assembly.GetType(simpleTypeName, throwOnError: false, ignoreCase);
		}
	}

	internal Stream _stream;

	internal ISurrogateSelector _surrogates;

	internal StreamingContext _context;

	internal ObjectManager _objectManager;

	internal InternalFE _formatterEnums;

	internal SerializationBinder _binder;

	internal long _topId;

	internal bool _isSimpleAssembly;

	internal object _topObject;

	internal SerObjectInfoInit _serObjectInfoInit;

	internal IFormatterConverter _formatterConverter;

	internal SerStack _stack;

	private SerStack _valueFixupStack;

	internal object[] _crossAppDomainArray;

	private bool _fullDeserialization;

	private bool _oldFormatDetected;

	private IntSizedArray _valTypeObjectIdTable;

	private readonly NameCache _typeCache = new NameCache();

	private string _previousAssemblyString;

	private string _previousName;

	private Type _previousType;

	private SerStack ValueFixupStack => _valueFixupStack ?? (_valueFixupStack = new SerStack("ValueType Fixup Stack"));

	internal object TopObject
	{
		get
		{
			return _topObject;
		}
		set
		{
			_topObject = value;
			if (_objectManager != null)
			{
				_objectManager.TopObject = value;
			}
		}
	}

	internal ObjectReader(Stream stream, ISurrogateSelector selector, StreamingContext context, InternalFE formatterEnums, SerializationBinder binder)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		_stream = stream;
		_surrogates = selector;
		_context = context;
		_binder = binder;
		_formatterEnums = formatterEnums;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal object Deserialize(BinaryParser serParser)
	{
		if (serParser == null)
		{
			throw new ArgumentNullException("serParser");
		}
		_fullDeserialization = false;
		TopObject = null;
		_topId = 0L;
		_isSimpleAssembly = _formatterEnums._assemblyFormat == FormatterAssemblyStyle.Simple;
		using (SerializationInfo.StartDeserialization())
		{
			if (_fullDeserialization)
			{
				_objectManager = new ObjectManager(_surrogates, _context);
				_serObjectInfoInit = new SerObjectInfoInit();
			}
			serParser.Run();
			if (_fullDeserialization)
			{
				_objectManager.DoFixups();
			}
			if (TopObject == null)
			{
				throw new SerializationException(System.SR.Serialization_TopObject);
			}
			if (HasSurrogate(TopObject.GetType()) && _topId != 0L)
			{
				TopObject = _objectManager.GetObject(_topId);
			}
			if (TopObject is IObjectReference)
			{
				TopObject = ((IObjectReference)TopObject).GetRealObject(_context);
			}
			if (_fullDeserialization)
			{
				_objectManager.RaiseDeserializationEvent();
			}
			return TopObject;
		}
	}

	private bool HasSurrogate(Type t)
	{
		ISurrogateSelector selector;
		if (_surrogates != null)
		{
			return _surrogates.GetSurrogate(t, _context, out selector) != null;
		}
		return false;
	}

	private void CheckSerializable(Type t)
	{
		if (!t.IsSerializable && !HasSurrogate(t))
		{
			throw new SerializationException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.Serialization_NonSerType, t.FullName, t.Assembly.FullName));
		}
	}

	private void InitFullDeserialization()
	{
		_fullDeserialization = true;
		_stack = new SerStack("ObjectReader Object Stack");
		_objectManager = new ObjectManager(_surrogates, _context);
		if (_formatterConverter == null)
		{
			_formatterConverter = new FormatterConverter();
		}
	}

	internal object CrossAppDomainArray(int index)
	{
		return _crossAppDomainArray[index];
	}

	internal ReadObjectInfo CreateReadObjectInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType)
	{
		return ReadObjectInfo.Create(objectType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
	}

	internal ReadObjectInfo CreateReadObjectInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, string[] memberNames, Type[] memberTypes)
	{
		return ReadObjectInfo.Create(objectType, memberNames, memberTypes, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	internal void Parse(ParseRecord pr)
	{
		switch (pr._parseTypeEnum)
		{
		case InternalParseTypeE.SerializedStreamHeader:
			ParseSerializedStreamHeader(pr);
			break;
		case InternalParseTypeE.SerializedStreamHeaderEnd:
			ParseSerializedStreamHeaderEnd(pr);
			break;
		case InternalParseTypeE.Object:
			ParseObject(pr);
			break;
		case InternalParseTypeE.ObjectEnd:
			ParseObjectEnd(pr);
			break;
		case InternalParseTypeE.Member:
			ParseMember(pr);
			break;
		case InternalParseTypeE.MemberEnd:
			ParseMemberEnd(pr);
			break;
		default:
			throw new SerializationException(System.SR.Format(System.SR.Serialization_XMLElement, pr._name));
		case InternalParseTypeE.Envelope:
		case InternalParseTypeE.EnvelopeEnd:
		case InternalParseTypeE.Body:
		case InternalParseTypeE.BodyEnd:
			break;
		}
	}

	private void ParseError(ParseRecord processing, ParseRecord onStack)
	{
		throw new SerializationException(System.SR.Format(System.SR.Serialization_ParseError, onStack._name + " " + onStack._parseTypeEnum.ToString() + " " + processing._name + " " + processing._parseTypeEnum));
	}

	private void ParseSerializedStreamHeader(ParseRecord pr)
	{
		_stack.Push(pr);
	}

	private void ParseSerializedStreamHeaderEnd(ParseRecord pr)
	{
		_stack.Pop();
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseObject(ParseRecord pr)
	{
		if (!_fullDeserialization)
		{
			InitFullDeserialization();
		}
		if (pr._objectPositionEnum == InternalObjectPositionE.Top)
		{
			_topId = pr._objectId;
		}
		if (pr._parseTypeEnum == InternalParseTypeE.Object)
		{
			_stack.Push(pr);
		}
		if (pr._objectTypeEnum == InternalObjectTypeE.Array)
		{
			ParseArray(pr);
			return;
		}
		if (pr._dtType == null)
		{
			pr._newObj = new TypeLoadExceptionHolder(pr._keyDt);
			return;
		}
		if ((object)pr._dtType == Converter.s_typeofString)
		{
			if (pr._value != null)
			{
				pr._newObj = pr._value;
				if (pr._objectPositionEnum == InternalObjectPositionE.Top)
				{
					TopObject = pr._newObj;
					return;
				}
				_stack.Pop();
				RegisterObject(pr._newObj, pr, (ParseRecord)_stack.Peek());
			}
			return;
		}
		CheckSerializable(pr._dtType);
		pr._newObj = FormatterServices.GetUninitializedObject(pr._dtType);
		_objectManager.RaiseOnDeserializingEvent(pr._newObj);
		if (pr._newObj == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_TopObjectInstantiate, pr._dtType));
		}
		if (pr._objectPositionEnum == InternalObjectPositionE.Top)
		{
			TopObject = pr._newObj;
		}
		if (pr._objectInfo == null)
		{
			pr._objectInfo = ReadObjectInfo.Create(pr._dtType, _surrogates, _context, _objectManager, _serObjectInfoInit, _formatterConverter, _isSimpleAssembly);
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseObjectEnd(ParseRecord pr)
	{
		ParseRecord parseRecord = ((ParseRecord)_stack.Peek()) ?? pr;
		if (parseRecord._objectPositionEnum == InternalObjectPositionE.Top && (object)parseRecord._dtType == Converter.s_typeofString)
		{
			parseRecord._newObj = parseRecord._value;
			TopObject = parseRecord._newObj;
			return;
		}
		_stack.Pop();
		ParseRecord parseRecord2 = (ParseRecord)_stack.Peek();
		if (parseRecord._newObj == null)
		{
			return;
		}
		if (parseRecord._objectTypeEnum == InternalObjectTypeE.Array)
		{
			if (parseRecord._objectPositionEnum == InternalObjectPositionE.Top)
			{
				TopObject = parseRecord._newObj;
			}
			RegisterObject(parseRecord._newObj, parseRecord, parseRecord2);
			return;
		}
		parseRecord._objectInfo.PopulateObjectMembers(parseRecord._newObj, parseRecord._memberData);
		if (!parseRecord._isRegistered && parseRecord._objectId > 0)
		{
			RegisterObject(parseRecord._newObj, parseRecord, parseRecord2);
		}
		if (parseRecord._isValueTypeFixup)
		{
			ValueFixup valueFixup = (ValueFixup)ValueFixupStack.Pop();
			valueFixup.Fixup(parseRecord, parseRecord2);
		}
		if (parseRecord._objectPositionEnum == InternalObjectPositionE.Top)
		{
			TopObject = parseRecord._newObj;
		}
		parseRecord._objectInfo.ObjectEnd();
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseArray(ParseRecord pr)
	{
		if (pr._arrayTypeEnum == InternalArrayTypeE.Base64)
		{
			pr._newObj = ((pr._value.Length > 0) ? Convert.FromBase64String(pr._value) : Array.Empty<byte>());
			if (_stack.Peek() == pr)
			{
				_stack.Pop();
			}
			if (pr._objectPositionEnum == InternalObjectPositionE.Top)
			{
				TopObject = pr._newObj;
			}
			ParseRecord objectPr = (ParseRecord)_stack.Peek();
			RegisterObject(pr._newObj, pr, objectPr);
			return;
		}
		if (pr._newObj != null && Converter.IsWriteAsByteArray(pr._arrayElementTypeCode))
		{
			if (pr._objectPositionEnum == InternalObjectPositionE.Top)
			{
				TopObject = pr._newObj;
			}
			ParseRecord objectPr2 = (ParseRecord)_stack.Peek();
			RegisterObject(pr._newObj, pr, objectPr2);
			return;
		}
		if (pr._arrayTypeEnum == InternalArrayTypeE.Jagged || pr._arrayTypeEnum == InternalArrayTypeE.Single)
		{
			bool flag = true;
			if (pr._lowerBoundA == null || pr._lowerBoundA[0] == 0)
			{
				if ((object)pr._arrayElementType == Converter.s_typeofString)
				{
					object[] objectA = new string[pr._lengthA[0]];
					pr._objectA = objectA;
					pr._newObj = pr._objectA;
					flag = false;
				}
				else if ((object)pr._arrayElementType == Converter.s_typeofObject)
				{
					pr._objectA = new object[pr._lengthA[0]];
					pr._newObj = pr._objectA;
					flag = false;
				}
				else if (pr._arrayElementType != null)
				{
					pr._newObj = Array.CreateInstance(pr._arrayElementType, pr._lengthA[0]);
				}
				pr._isLowerBound = false;
			}
			else
			{
				if (pr._arrayElementType != null)
				{
					pr._newObj = Array.CreateInstance(pr._arrayElementType, pr._lengthA, pr._lowerBoundA);
				}
				pr._isLowerBound = true;
			}
			if (pr._arrayTypeEnum == InternalArrayTypeE.Single)
			{
				if (!pr._isLowerBound && Converter.IsWriteAsByteArray(pr._arrayElementTypeCode))
				{
					pr._primitiveArray = new PrimitiveArray(pr._arrayElementTypeCode, (Array)pr._newObj);
				}
				else if (flag && pr._arrayElementType != null && !pr._arrayElementType.IsValueType && !pr._isLowerBound)
				{
					pr._objectA = (object[])pr._newObj;
				}
			}
			pr._indexMap = new int[1];
			return;
		}
		if (pr._arrayTypeEnum == InternalArrayTypeE.Rectangular)
		{
			pr._isLowerBound = false;
			if (pr._lowerBoundA != null)
			{
				for (int i = 0; i < pr._rank; i++)
				{
					if (pr._lowerBoundA[i] != 0)
					{
						pr._isLowerBound = true;
					}
				}
			}
			if (pr._arrayElementType != null)
			{
				pr._newObj = ((!pr._isLowerBound) ? Array.CreateInstance(pr._arrayElementType, pr._lengthA) : Array.CreateInstance(pr._arrayElementType, pr._lengthA, pr._lowerBoundA));
			}
			int num = 1;
			for (int j = 0; j < pr._rank; j++)
			{
				num *= pr._lengthA[j];
			}
			pr._indexMap = new int[pr._rank];
			pr._rectangularMap = new int[pr._rank];
			pr._linearlength = num;
			return;
		}
		throw new SerializationException(System.SR.Format(System.SR.Serialization_ArrayType, pr._arrayTypeEnum));
	}

	private void NextRectangleMap(ParseRecord pr)
	{
		for (int num = pr._rank - 1; num > -1; num--)
		{
			if (pr._rectangularMap[num] < pr._lengthA[num] - 1)
			{
				pr._rectangularMap[num]++;
				if (num < pr._rank - 1)
				{
					for (int i = num + 1; i < pr._rank; i++)
					{
						pr._rectangularMap[i] = 0;
					}
				}
				Array.Copy(pr._rectangularMap, pr._indexMap, pr._rank);
				break;
			}
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseArrayMember(ParseRecord pr)
	{
		ParseRecord parseRecord = (ParseRecord)_stack.Peek();
		if (parseRecord._arrayTypeEnum == InternalArrayTypeE.Rectangular)
		{
			if (parseRecord._memberIndex > 0)
			{
				NextRectangleMap(parseRecord);
			}
			if (parseRecord._isLowerBound)
			{
				for (int i = 0; i < parseRecord._rank; i++)
				{
					parseRecord._indexMap[i] = parseRecord._rectangularMap[i] + parseRecord._lowerBoundA[i];
				}
			}
		}
		else
		{
			parseRecord._indexMap[0] = ((!parseRecord._isLowerBound) ? parseRecord._memberIndex : (parseRecord._lowerBoundA[0] + parseRecord._memberIndex));
		}
		if (pr._memberValueEnum == InternalMemberValueE.Reference)
		{
			object @object = _objectManager.GetObject(pr._idRef);
			if (@object == null)
			{
				int[] array = new int[parseRecord._rank];
				Array.Copy(parseRecord._indexMap, array, parseRecord._rank);
				_objectManager.RecordArrayElementFixup(parseRecord._objectId, array, pr._idRef);
			}
			else if (parseRecord._objectA != null)
			{
				parseRecord._objectA[parseRecord._indexMap[0]] = @object;
			}
			else
			{
				((Array)parseRecord._newObj).SetValue(@object, parseRecord._indexMap);
			}
		}
		else if (pr._memberValueEnum == InternalMemberValueE.Nested)
		{
			if (pr._dtType == null)
			{
				pr._dtType = parseRecord._arrayElementType;
			}
			ParseObject(pr);
			_stack.Push(pr);
			if (parseRecord._arrayElementType != null)
			{
				if (parseRecord._arrayElementType.IsValueType && pr._arrayElementTypeCode == InternalPrimitiveTypeE.Invalid)
				{
					pr._isValueTypeFixup = true;
					ValueFixupStack.Push(new ValueFixup((Array)parseRecord._newObj, parseRecord._indexMap));
				}
				else if (parseRecord._objectA != null)
				{
					parseRecord._objectA[parseRecord._indexMap[0]] = pr._newObj;
				}
				else
				{
					((Array)parseRecord._newObj).SetValue(pr._newObj, parseRecord._indexMap);
				}
			}
		}
		else if (pr._memberValueEnum == InternalMemberValueE.InlineValue)
		{
			if ((object)parseRecord._arrayElementType == Converter.s_typeofString || (object)pr._dtType == Converter.s_typeofString)
			{
				ParseString(pr, parseRecord);
				if (parseRecord._objectA != null)
				{
					parseRecord._objectA[parseRecord._indexMap[0]] = pr._value;
				}
				else
				{
					((Array)parseRecord._newObj).SetValue(pr._value, parseRecord._indexMap);
				}
			}
			else if (parseRecord._isArrayVariant)
			{
				if (pr._keyDt == null)
				{
					throw new SerializationException(System.SR.Serialization_ArrayTypeObject);
				}
				object obj = null;
				if ((object)pr._dtType == Converter.s_typeofString)
				{
					ParseString(pr, parseRecord);
					obj = pr._value;
				}
				else
				{
					obj = ((pr._varValue != null) ? pr._varValue : Converter.FromString(pr._value, pr._dtTypeCode));
				}
				if (parseRecord._objectA != null)
				{
					parseRecord._objectA[parseRecord._indexMap[0]] = obj;
				}
				else
				{
					((Array)parseRecord._newObj).SetValue(obj, parseRecord._indexMap);
				}
			}
			else if (parseRecord._primitiveArray != null)
			{
				parseRecord._primitiveArray.SetValue(pr._value, parseRecord._indexMap[0]);
			}
			else
			{
				object obj2 = ((pr._varValue != null) ? pr._varValue : Converter.FromString(pr._value, parseRecord._arrayElementTypeCode));
				if (parseRecord._objectA != null)
				{
					parseRecord._objectA[parseRecord._indexMap[0]] = obj2;
				}
				else
				{
					((Array)parseRecord._newObj).SetValue(obj2, parseRecord._indexMap);
				}
			}
		}
		else if (pr._memberValueEnum == InternalMemberValueE.Null)
		{
			parseRecord._memberIndex += pr._consecutiveNullArrayEntryCount - 1;
		}
		else
		{
			ParseError(pr, parseRecord);
		}
		parseRecord._memberIndex++;
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseArrayMemberEnd(ParseRecord pr)
	{
		if (pr._memberValueEnum == InternalMemberValueE.Nested)
		{
			ParseObjectEnd(pr);
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseMember(ParseRecord pr)
	{
		ParseRecord parseRecord = (ParseRecord)_stack.Peek();
		InternalMemberTypeE memberTypeEnum = pr._memberTypeEnum;
		if (memberTypeEnum != InternalMemberTypeE.Field && memberTypeEnum == InternalMemberTypeE.Item)
		{
			ParseArrayMember(pr);
			return;
		}
		if (pr._dtType == null && parseRecord._objectInfo._isTyped)
		{
			pr._dtType = parseRecord._objectInfo.GetType(pr._name);
			if (pr._dtType != null)
			{
				pr._dtTypeCode = Converter.ToCode(pr._dtType);
			}
		}
		if (pr._memberValueEnum == InternalMemberValueE.Null)
		{
			parseRecord._objectInfo.AddValue(pr._name, null, ref parseRecord._si, ref parseRecord._memberData);
		}
		else if (pr._memberValueEnum == InternalMemberValueE.Nested)
		{
			ParseObject(pr);
			_stack.Push(pr);
			if (pr._objectInfo != null && pr._objectInfo._objectType != null && pr._objectInfo._objectType.IsValueType)
			{
				pr._isValueTypeFixup = true;
				ValueFixupStack.Push(new ValueFixup(parseRecord._newObj, pr._name, parseRecord._objectInfo));
			}
			else
			{
				parseRecord._objectInfo.AddValue(pr._name, pr._newObj, ref parseRecord._si, ref parseRecord._memberData);
			}
		}
		else if (pr._memberValueEnum == InternalMemberValueE.Reference)
		{
			object @object = _objectManager.GetObject(pr._idRef);
			if (@object == null)
			{
				parseRecord._objectInfo.AddValue(pr._name, null, ref parseRecord._si, ref parseRecord._memberData);
				parseRecord._objectInfo.RecordFixup(parseRecord._objectId, pr._name, pr._idRef);
			}
			else
			{
				parseRecord._objectInfo.AddValue(pr._name, @object, ref parseRecord._si, ref parseRecord._memberData);
			}
		}
		else if (pr._memberValueEnum == InternalMemberValueE.InlineValue)
		{
			if ((object)pr._dtType == Converter.s_typeofString)
			{
				ParseString(pr, parseRecord);
				parseRecord._objectInfo.AddValue(pr._name, pr._value, ref parseRecord._si, ref parseRecord._memberData);
			}
			else if (pr._dtTypeCode == InternalPrimitiveTypeE.Invalid)
			{
				if (pr._arrayTypeEnum == InternalArrayTypeE.Base64)
				{
					parseRecord._objectInfo.AddValue(pr._name, Convert.FromBase64String(pr._value), ref parseRecord._si, ref parseRecord._memberData);
					return;
				}
				if ((object)pr._dtType == Converter.s_typeofObject)
				{
					throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeMissing, pr._name));
				}
				ParseString(pr, parseRecord);
				if ((object)pr._dtType == Converter.s_typeofSystemVoid)
				{
					parseRecord._objectInfo.AddValue(pr._name, pr._dtType, ref parseRecord._si, ref parseRecord._memberData);
				}
				else if (parseRecord._objectInfo._isSi)
				{
					parseRecord._objectInfo.AddValue(pr._name, pr._value, ref parseRecord._si, ref parseRecord._memberData);
				}
			}
			else
			{
				object value = ((pr._varValue != null) ? pr._varValue : Converter.FromString(pr._value, pr._dtTypeCode));
				parseRecord._objectInfo.AddValue(pr._name, value, ref parseRecord._si, ref parseRecord._memberData);
			}
		}
		else
		{
			ParseError(pr, parseRecord);
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseMemberEnd(ParseRecord pr)
	{
		switch (pr._memberTypeEnum)
		{
		case InternalMemberTypeE.Item:
			ParseArrayMemberEnd(pr);
			break;
		case InternalMemberTypeE.Field:
			if (pr._memberValueEnum == InternalMemberValueE.Nested)
			{
				ParseObjectEnd(pr);
			}
			break;
		default:
			ParseError(pr, (ParseRecord)_stack.Peek());
			break;
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ParseString(ParseRecord pr, ParseRecord parentPr)
	{
		if (!pr._isRegistered && pr._objectId > 0)
		{
			RegisterObject(pr._value, pr, parentPr, bIsString: true);
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void RegisterObject(object obj, ParseRecord pr, ParseRecord objectPr)
	{
		RegisterObject(obj, pr, objectPr, bIsString: false);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void RegisterObject(object obj, ParseRecord pr, ParseRecord objectPr, bool bIsString)
	{
		if (pr._isRegistered)
		{
			return;
		}
		pr._isRegistered = true;
		SerializationInfo serializationInfo = null;
		long idOfContainingObj = 0L;
		MemberInfo member = null;
		int[] arrayIndex = null;
		if (objectPr != null)
		{
			arrayIndex = objectPr._indexMap;
			idOfContainingObj = objectPr._objectId;
			if (objectPr._objectInfo != null && !objectPr._objectInfo._isSi)
			{
				member = objectPr._objectInfo.GetMemberInfo(pr._name);
			}
		}
		serializationInfo = pr._si;
		if (bIsString)
		{
			_objectManager.RegisterString((string)obj, pr._objectId, serializationInfo, idOfContainingObj, member);
		}
		else
		{
			_objectManager.RegisterObject(obj, pr._objectId, serializationInfo, idOfContainingObj, member, arrayIndex);
		}
	}

	internal long GetId(long objectId)
	{
		if (!_fullDeserialization)
		{
			InitFullDeserialization();
		}
		if (objectId > 0)
		{
			return objectId;
		}
		if (_oldFormatDetected || objectId == -1)
		{
			_oldFormatDetected = true;
			if (_valTypeObjectIdTable == null)
			{
				_valTypeObjectIdTable = new IntSizedArray();
			}
			long num = 0L;
			if ((num = _valTypeObjectIdTable[(int)objectId]) == 0L)
			{
				num = int.MaxValue + objectId;
				_valTypeObjectIdTable[(int)objectId] = (int)num;
			}
			return num;
		}
		return -1 * objectId;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal Type Bind(string assemblyString, string typeString)
	{
		Type type = null;
		if (_binder != null)
		{
			type = _binder.BindToType(assemblyString, typeString);
		}
		if (type == null)
		{
			type = FastBindToType(assemblyString, typeString);
		}
		return type;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal Type FastBindToType(string assemblyName, string typeName)
	{
		Type type = null;
		TypeNAssembly typeNAssembly = (TypeNAssembly)_typeCache.GetCachedValue(typeName);
		if (typeNAssembly == null || typeNAssembly.AssemblyName != assemblyName)
		{
			if (assemblyName == null)
			{
				return null;
			}
			Assembly assembly = null;
			AssemblyName assemblyName2 = null;
			try
			{
				assemblyName2 = new AssemblyName(assemblyName);
			}
			catch
			{
				return null;
			}
			if (_isSimpleAssembly)
			{
				assembly = ResolveSimpleAssemblyName(assemblyName2);
			}
			else
			{
				try
				{
					assembly = Assembly.Load(assemblyName2);
				}
				catch
				{
				}
			}
			if (assembly == null)
			{
				return null;
			}
			if (_isSimpleAssembly)
			{
				GetSimplyNamedTypeFromAssembly(assembly, typeName, ref type);
			}
			else
			{
				type = FormatterServices.GetTypeFromAssembly(assembly, typeName);
			}
			if (type == null)
			{
				return null;
			}
			CheckTypeForwardedTo(assembly, type.Assembly, type);
			typeNAssembly = new TypeNAssembly();
			typeNAssembly.Type = type;
			typeNAssembly.AssemblyName = assemblyName;
			_typeCache.SetCachedValue(typeNAssembly);
		}
		return typeNAssembly.Type;
	}

	private static Assembly ResolveSimpleAssemblyName(AssemblyName assemblyName)
	{
		try
		{
			return Assembly.Load(assemblyName);
		}
		catch
		{
		}
		if (assemblyName != null)
		{
			try
			{
				return Assembly.Load(assemblyName.Name);
			}
			catch
			{
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	private static void GetSimplyNamedTypeFromAssembly(Assembly assm, string typeName, ref Type type)
	{
		try
		{
			type = FormatterServices.GetTypeFromAssembly(assm, typeName);
		}
		catch (TypeLoadException)
		{
		}
		catch (FileNotFoundException)
		{
		}
		catch (FileLoadException)
		{
		}
		catch (BadImageFormatException)
		{
		}
		if (type == null)
		{
			type = Type.GetType(typeName, ResolveSimpleAssemblyName, new TopLevelAssemblyTypeResolver(assm).ResolveType, throwOnError: false);
		}
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal Type GetType(BinaryAssemblyInfo assemblyInfo, string name)
	{
		Type type;
		if (_previousName != null && _previousName.Length == name.Length && _previousName.Equals(name) && _previousAssemblyString != null && _previousAssemblyString.Length == assemblyInfo._assemblyString.Length && _previousAssemblyString.Equals(assemblyInfo._assemblyString))
		{
			type = _previousType;
		}
		else
		{
			type = Bind(assemblyInfo._assemblyString, name);
			if (type == null)
			{
				Assembly assembly = assemblyInfo.GetAssembly();
				if (_isSimpleAssembly)
				{
					GetSimplyNamedTypeFromAssembly(assembly, name, ref type);
				}
				else
				{
					type = FormatterServices.GetTypeFromAssembly(assembly, name);
				}
				if (type != null)
				{
					CheckTypeForwardedTo(assembly, type.Assembly, type);
				}
			}
			_previousAssemblyString = assemblyInfo._assemblyString;
			_previousName = name;
			_previousType = type;
		}
		return type;
	}

	private static void CheckTypeForwardedTo(Assembly sourceAssembly, Assembly destAssembly, Type resolvedType)
	{
	}
}
