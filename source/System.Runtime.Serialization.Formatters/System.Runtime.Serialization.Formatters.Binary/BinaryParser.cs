using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryParser
{
	private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	internal ObjectReader _objectReader;

	internal Stream _input;

	internal long _topId;

	internal long _headerId;

	internal SizedArray _objectMapIdTable;

	internal SizedArray _assemIdToAssemblyTable;

	internal SerStack _stack = new SerStack("ObjectProgressStack");

	internal BinaryTypeEnum _expectedType = BinaryTypeEnum.ObjectUrt;

	internal object _expectedTypeInformation;

	internal ParseRecord _prs;

	private BinaryAssemblyInfo _systemAssemblyInfo;

	private readonly BinaryReader _dataReader;

	private SerStack _opPool;

	private BinaryObject _binaryObject;

	private BinaryObjectWithMap _bowm;

	private BinaryObjectWithMapTyped _bowmt;

	internal BinaryObjectString _objectString;

	internal BinaryCrossAppDomainString _crossAppDomainString;

	internal MemberPrimitiveTyped _memberPrimitiveTyped;

	private byte[] _byteBuffer;

	internal MemberPrimitiveUnTyped memberPrimitiveUnTyped;

	internal MemberReference _memberReference;

	internal ObjectNull _objectNull;

	internal static volatile MessageEnd _messageEnd;

	internal BinaryAssemblyInfo SystemAssemblyInfo => _systemAssemblyInfo ?? (_systemAssemblyInfo = new BinaryAssemblyInfo(Converter.s_urtAssemblyString, Converter.s_urtAssembly));

	internal SizedArray ObjectMapIdTable => _objectMapIdTable ?? (_objectMapIdTable = new SizedArray());

	internal SizedArray AssemIdToAssemblyTable => _assemIdToAssemblyTable ?? (_assemIdToAssemblyTable = new SizedArray(2));

	internal ParseRecord PRs => _prs ?? (_prs = new ParseRecord());

	internal BinaryParser(Stream stream, ObjectReader objectReader)
	{
		_input = stream;
		_objectReader = objectReader;
		_dataReader = new BinaryReader(_input, s_encoding);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	internal void Run()
	{
		try
		{
			bool flag = true;
			ReadBegin();
			ReadSerializationHeaderRecord();
			while (flag)
			{
				BinaryHeaderEnum binaryHeaderEnum = BinaryHeaderEnum.Object;
				switch (_expectedType)
				{
				case BinaryTypeEnum.String:
				case BinaryTypeEnum.Object:
				case BinaryTypeEnum.ObjectUrt:
				case BinaryTypeEnum.ObjectUser:
				case BinaryTypeEnum.ObjectArray:
				case BinaryTypeEnum.StringArray:
				case BinaryTypeEnum.PrimitiveArray:
				{
					byte b = _dataReader.ReadByte();
					binaryHeaderEnum = (BinaryHeaderEnum)b;
					switch (binaryHeaderEnum)
					{
					case BinaryHeaderEnum.Assembly:
					case BinaryHeaderEnum.CrossAppDomainAssembly:
						ReadAssembly(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.Object:
						ReadObject();
						break;
					case BinaryHeaderEnum.CrossAppDomainMap:
						ReadCrossAppDomainMap();
						break;
					case BinaryHeaderEnum.ObjectWithMap:
					case BinaryHeaderEnum.ObjectWithMapAssemId:
						ReadObjectWithMap(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.ObjectWithMapTyped:
					case BinaryHeaderEnum.ObjectWithMapTypedAssemId:
						ReadObjectWithMapTyped(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.ObjectString:
					case BinaryHeaderEnum.CrossAppDomainString:
						ReadObjectString(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.Array:
					case BinaryHeaderEnum.ArraySinglePrimitive:
					case BinaryHeaderEnum.ArraySingleObject:
					case BinaryHeaderEnum.ArraySingleString:
						ReadArray(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.MemberPrimitiveTyped:
						ReadMemberPrimitiveTyped();
						break;
					case BinaryHeaderEnum.MemberReference:
						ReadMemberReference();
						break;
					case BinaryHeaderEnum.ObjectNull:
					case BinaryHeaderEnum.ObjectNullMultiple256:
					case BinaryHeaderEnum.ObjectNullMultiple:
						ReadObjectNull(binaryHeaderEnum);
						break;
					case BinaryHeaderEnum.MessageEnd:
						flag = false;
						ReadMessageEnd();
						ReadEnd();
						break;
					default:
						throw new SerializationException(System.SR.Format(System.SR.Serialization_BinaryHeader, b));
					}
					break;
				}
				case BinaryTypeEnum.Primitive:
					ReadMemberPrimitiveUnTyped();
					break;
				default:
					throw new SerializationException(System.SR.Serialization_TypeExpected);
				}
				if (binaryHeaderEnum == BinaryHeaderEnum.Assembly)
				{
					continue;
				}
				bool flag2 = false;
				while (!flag2)
				{
					ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
					if (objectProgress == null)
					{
						_expectedType = BinaryTypeEnum.ObjectUrt;
						_expectedTypeInformation = null;
						flag2 = true;
						continue;
					}
					flag2 = objectProgress.GetNext(out objectProgress._expectedType, out objectProgress._expectedTypeInformation);
					_expectedType = objectProgress._expectedType;
					_expectedTypeInformation = objectProgress._expectedTypeInformation;
					if (!flag2)
					{
						PRs.Init();
						if (objectProgress._memberValueEnum == InternalMemberValueE.Nested)
						{
							PRs._parseTypeEnum = InternalParseTypeE.MemberEnd;
							PRs._memberTypeEnum = objectProgress._memberTypeEnum;
							PRs._memberValueEnum = objectProgress._memberValueEnum;
							_objectReader.Parse(PRs);
						}
						else
						{
							PRs._parseTypeEnum = InternalParseTypeE.ObjectEnd;
							PRs._memberTypeEnum = objectProgress._memberTypeEnum;
							PRs._memberValueEnum = objectProgress._memberValueEnum;
							_objectReader.Parse(PRs);
						}
						_stack.Pop();
						PutOp(objectProgress);
					}
				}
			}
		}
		catch (EndOfStreamException)
		{
			throw new SerializationException(System.SR.Serialization_StreamEnd);
		}
	}

	internal void ReadBegin()
	{
	}

	internal void ReadEnd()
	{
	}

	internal bool ReadBoolean()
	{
		return _dataReader.ReadBoolean();
	}

	internal byte ReadByte()
	{
		return _dataReader.ReadByte();
	}

	internal byte[] ReadBytes(int length)
	{
		return _dataReader.ReadBytes(length);
	}

	internal void ReadBytes(byte[] byteA, int offset, int size)
	{
		while (size > 0)
		{
			int num = _dataReader.Read(byteA, offset, size);
			if (num == 0)
			{
				throw new EndOfStreamException(System.SR.IO_EOF_ReadBeyondEOF);
			}
			offset += num;
			size -= num;
		}
	}

	internal char ReadChar()
	{
		return _dataReader.ReadChar();
	}

	internal char[] ReadChars(int length)
	{
		return _dataReader.ReadChars(length);
	}

	internal decimal ReadDecimal()
	{
		return decimal.Parse(_dataReader.ReadString(), CultureInfo.InvariantCulture);
	}

	internal float ReadSingle()
	{
		return _dataReader.ReadSingle();
	}

	internal double ReadDouble()
	{
		return _dataReader.ReadDouble();
	}

	internal short ReadInt16()
	{
		return _dataReader.ReadInt16();
	}

	internal int ReadInt32()
	{
		return _dataReader.ReadInt32();
	}

	internal long ReadInt64()
	{
		return _dataReader.ReadInt64();
	}

	internal sbyte ReadSByte()
	{
		return (sbyte)ReadByte();
	}

	internal string ReadString()
	{
		return _dataReader.ReadString();
	}

	internal TimeSpan ReadTimeSpan()
	{
		return new TimeSpan(ReadInt64());
	}

	internal DateTime ReadDateTime()
	{
		return FromBinaryRaw(ReadInt64());
	}

	private static DateTime FromBinaryRaw(long dateData)
	{
		new DateTime(dateData & 0x3FFFFFFFFFFFFFFFL);
		return MemoryMarshal.Cast<long, DateTime>(MemoryMarshal.CreateReadOnlySpan(ref dateData, 1))[0];
	}

	internal ushort ReadUInt16()
	{
		return _dataReader.ReadUInt16();
	}

	internal uint ReadUInt32()
	{
		return _dataReader.ReadUInt32();
	}

	internal ulong ReadUInt64()
	{
		return _dataReader.ReadUInt64();
	}

	internal void ReadSerializationHeaderRecord()
	{
		SerializationHeaderRecord serializationHeaderRecord = new SerializationHeaderRecord();
		serializationHeaderRecord.Read(this);
		_topId = ((serializationHeaderRecord._topId > 0) ? _objectReader.GetId(serializationHeaderRecord._topId) : serializationHeaderRecord._topId);
		_headerId = ((serializationHeaderRecord._headerId > 0) ? _objectReader.GetId(serializationHeaderRecord._headerId) : serializationHeaderRecord._headerId);
	}

	internal void ReadAssembly(BinaryHeaderEnum binaryHeaderEnum)
	{
		BinaryAssembly binaryAssembly = new BinaryAssembly();
		if (binaryHeaderEnum == BinaryHeaderEnum.CrossAppDomainAssembly)
		{
			BinaryCrossAppDomainAssembly binaryCrossAppDomainAssembly = new BinaryCrossAppDomainAssembly();
			binaryCrossAppDomainAssembly.Read(this);
			binaryAssembly._assemId = binaryCrossAppDomainAssembly._assemId;
			binaryAssembly._assemblyString = _objectReader.CrossAppDomainArray(binaryCrossAppDomainAssembly._assemblyIndex) as string;
			if (binaryAssembly._assemblyString == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_CrossAppDomainError, "String", binaryCrossAppDomainAssembly._assemblyIndex));
			}
		}
		else
		{
			binaryAssembly.Read(this);
		}
		AssemIdToAssemblyTable[binaryAssembly._assemId] = new BinaryAssemblyInfo(binaryAssembly._assemblyString);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadObject()
	{
		if (_binaryObject == null)
		{
			_binaryObject = new BinaryObject();
		}
		_binaryObject.Read(this);
		ObjectMap objectMap = (ObjectMap)ObjectMapIdTable[_binaryObject._mapId];
		if (objectMap == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_Map, _binaryObject._mapId));
		}
		ObjectProgress op = GetOp();
		ParseRecord pr = op._pr;
		_stack.Push(op);
		op._objectTypeEnum = InternalObjectTypeE.Object;
		op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
		op._memberNames = objectMap._memberNames;
		op._memberTypes = objectMap._memberTypes;
		op._typeInformationA = objectMap._typeInformationA;
		op._memberLength = op._binaryTypeEnumA.Length;
		ObjectProgress objectProgress = (ObjectProgress)_stack.PeekPeek();
		if (objectProgress == null || objectProgress._isInitial)
		{
			op._name = objectMap._objectName;
			pr._parseTypeEnum = InternalParseTypeE.Object;
			op._memberValueEnum = InternalMemberValueE.Empty;
		}
		else
		{
			pr._parseTypeEnum = InternalParseTypeE.Member;
			pr._memberValueEnum = InternalMemberValueE.Nested;
			op._memberValueEnum = InternalMemberValueE.Nested;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				pr._name = objectProgress._name;
				pr._memberTypeEnum = InternalMemberTypeE.Field;
				op._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			case InternalObjectTypeE.Array:
				pr._memberTypeEnum = InternalMemberTypeE.Item;
				op._memberTypeEnum = InternalMemberTypeE.Item;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_Map, objectProgress._objectTypeEnum.ToString()));
			}
		}
		pr._objectId = _objectReader.GetId(_binaryObject._objectId);
		pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);
		if (pr._objectId == _topId)
		{
			pr._objectPositionEnum = InternalObjectPositionE.Top;
		}
		pr._objectTypeEnum = InternalObjectTypeE.Object;
		pr._keyDt = objectMap._objectName;
		pr._dtType = objectMap._objectType;
		pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
		_objectReader.Parse(pr);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	internal void ReadCrossAppDomainMap()
	{
		BinaryCrossAppDomainMap binaryCrossAppDomainMap = new BinaryCrossAppDomainMap();
		binaryCrossAppDomainMap.Read(this);
		object obj = _objectReader.CrossAppDomainArray(binaryCrossAppDomainMap._crossAppDomainArrayIndex);
		if (obj is BinaryObjectWithMap record)
		{
			ReadObjectWithMap(record);
			return;
		}
		if (obj is BinaryObjectWithMapTyped record2)
		{
			ReadObjectWithMapTyped(record2);
			return;
		}
		throw new SerializationException(System.SR.Format(System.SR.Serialization_CrossAppDomainError, "BinaryObjectMap", obj));
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	internal void ReadObjectWithMap(BinaryHeaderEnum binaryHeaderEnum)
	{
		if (_bowm == null)
		{
			_bowm = new BinaryObjectWithMap(binaryHeaderEnum);
		}
		else
		{
			_bowm._binaryHeaderEnum = binaryHeaderEnum;
		}
		_bowm.Read(this);
		ReadObjectWithMap(_bowm);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	private void ReadObjectWithMap(BinaryObjectWithMap record)
	{
		BinaryAssemblyInfo binaryAssemblyInfo = null;
		ObjectProgress op = GetOp();
		ParseRecord pr = op._pr;
		_stack.Push(op);
		if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapAssemId)
		{
			if (record._assemId < 1)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_Assembly, record._name));
			}
			binaryAssemblyInfo = (BinaryAssemblyInfo)AssemIdToAssemblyTable[record._assemId];
			if (binaryAssemblyInfo == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_Assembly, record._assemId + " " + record._name));
			}
		}
		else if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMap)
		{
			binaryAssemblyInfo = SystemAssemblyInfo;
		}
		Type type = _objectReader.GetType(binaryAssemblyInfo, record._name);
		ObjectMap objectMap = ObjectMap.Create(record._name, type, record._memberNames, _objectReader, record._objectId, binaryAssemblyInfo);
		ObjectMapIdTable[record._objectId] = objectMap;
		op._objectTypeEnum = InternalObjectTypeE.Object;
		op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
		op._typeInformationA = objectMap._typeInformationA;
		op._memberLength = op._binaryTypeEnumA.Length;
		op._memberNames = objectMap._memberNames;
		op._memberTypes = objectMap._memberTypes;
		ObjectProgress objectProgress = (ObjectProgress)_stack.PeekPeek();
		if (objectProgress == null || objectProgress._isInitial)
		{
			op._name = record._name;
			pr._parseTypeEnum = InternalParseTypeE.Object;
			op._memberValueEnum = InternalMemberValueE.Empty;
		}
		else
		{
			pr._parseTypeEnum = InternalParseTypeE.Member;
			pr._memberValueEnum = InternalMemberValueE.Nested;
			op._memberValueEnum = InternalMemberValueE.Nested;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				pr._name = objectProgress._name;
				pr._memberTypeEnum = InternalMemberTypeE.Field;
				op._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			case InternalObjectTypeE.Array:
				pr._memberTypeEnum = InternalMemberTypeE.Item;
				op._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectTypeEnum, objectProgress._objectTypeEnum.ToString()));
			}
		}
		pr._objectTypeEnum = InternalObjectTypeE.Object;
		pr._objectId = _objectReader.GetId(record._objectId);
		pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);
		if (pr._objectId == _topId)
		{
			pr._objectPositionEnum = InternalObjectPositionE.Top;
		}
		pr._keyDt = record._name;
		pr._dtType = objectMap._objectType;
		pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
		_objectReader.Parse(pr);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal void ReadObjectWithMapTyped(BinaryHeaderEnum binaryHeaderEnum)
	{
		if (_bowmt == null)
		{
			_bowmt = new BinaryObjectWithMapTyped(binaryHeaderEnum);
		}
		else
		{
			_bowmt._binaryHeaderEnum = binaryHeaderEnum;
		}
		_bowmt.Read(this);
		ReadObjectWithMapTyped(_bowmt);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	private void ReadObjectWithMapTyped(BinaryObjectWithMapTyped record)
	{
		BinaryAssemblyInfo binaryAssemblyInfo = null;
		ObjectProgress op = GetOp();
		ParseRecord pr = op._pr;
		_stack.Push(op);
		if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTypedAssemId)
		{
			if (record._assemId < 1)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_AssemblyId, record._name));
			}
			binaryAssemblyInfo = (BinaryAssemblyInfo)AssemIdToAssemblyTable[record._assemId];
			if (binaryAssemblyInfo == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_AssemblyId, record._assemId + " " + record._name));
			}
		}
		else if (record._binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTyped)
		{
			binaryAssemblyInfo = SystemAssemblyInfo;
		}
		ObjectMap objectMap = ObjectMap.Create(record._name, record._memberNames, record._binaryTypeEnumA, record._typeInformationA, record._memberAssemIds, _objectReader, record._objectId, binaryAssemblyInfo, AssemIdToAssemblyTable);
		ObjectMapIdTable[record._objectId] = objectMap;
		op._objectTypeEnum = InternalObjectTypeE.Object;
		op._binaryTypeEnumA = objectMap._binaryTypeEnumA;
		op._typeInformationA = objectMap._typeInformationA;
		op._memberLength = op._binaryTypeEnumA.Length;
		op._memberNames = objectMap._memberNames;
		op._memberTypes = objectMap._memberTypes;
		ObjectProgress objectProgress = (ObjectProgress)_stack.PeekPeek();
		if (objectProgress == null || objectProgress._isInitial)
		{
			op._name = record._name;
			pr._parseTypeEnum = InternalParseTypeE.Object;
			op._memberValueEnum = InternalMemberValueE.Empty;
		}
		else
		{
			pr._parseTypeEnum = InternalParseTypeE.Member;
			pr._memberValueEnum = InternalMemberValueE.Nested;
			op._memberValueEnum = InternalMemberValueE.Nested;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				pr._name = objectProgress._name;
				pr._memberTypeEnum = InternalMemberTypeE.Field;
				op._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			case InternalObjectTypeE.Array:
				pr._memberTypeEnum = InternalMemberTypeE.Item;
				op._memberTypeEnum = InternalMemberTypeE.Item;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectTypeEnum, objectProgress._objectTypeEnum.ToString()));
			}
		}
		pr._objectTypeEnum = InternalObjectTypeE.Object;
		pr._objectInfo = objectMap.CreateObjectInfo(ref pr._si, ref pr._memberData);
		pr._objectId = _objectReader.GetId(record._objectId);
		if (pr._objectId == _topId)
		{
			pr._objectPositionEnum = InternalObjectPositionE.Top;
		}
		pr._keyDt = record._name;
		pr._dtType = objectMap._objectType;
		pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
		_objectReader.Parse(pr);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadObjectString(BinaryHeaderEnum binaryHeaderEnum)
	{
		if (_objectString == null)
		{
			_objectString = new BinaryObjectString();
		}
		if (binaryHeaderEnum == BinaryHeaderEnum.ObjectString)
		{
			_objectString.Read(this);
		}
		else
		{
			if (_crossAppDomainString == null)
			{
				_crossAppDomainString = new BinaryCrossAppDomainString();
			}
			_crossAppDomainString.Read(this);
			_objectString._value = _objectReader.CrossAppDomainArray(_crossAppDomainString._value) as string;
			if (_objectString._value == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_CrossAppDomainError, "String", _crossAppDomainString._value));
			}
			_objectString._objectId = _crossAppDomainString._objectId;
		}
		PRs.Init();
		PRs._parseTypeEnum = InternalParseTypeE.Object;
		PRs._objectId = _objectReader.GetId(_objectString._objectId);
		if (PRs._objectId == _topId)
		{
			PRs._objectPositionEnum = InternalObjectPositionE.Top;
		}
		PRs._objectTypeEnum = InternalObjectTypeE.Object;
		ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
		PRs._value = _objectString._value;
		PRs._keyDt = "System.String";
		PRs._dtType = Converter.s_typeofString;
		PRs._dtTypeCode = InternalPrimitiveTypeE.Invalid;
		PRs._varValue = _objectString._value;
		if (objectProgress == null)
		{
			PRs._parseTypeEnum = InternalParseTypeE.Object;
			PRs._name = "System.String";
		}
		else
		{
			PRs._parseTypeEnum = InternalParseTypeE.Member;
			PRs._memberValueEnum = InternalMemberValueE.InlineValue;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				PRs._name = objectProgress._name;
				PRs._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			case InternalObjectTypeE.Array:
				PRs._memberTypeEnum = InternalMemberTypeE.Item;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectTypeEnum, objectProgress._objectTypeEnum.ToString()));
			}
		}
		_objectReader.Parse(PRs);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadMemberPrimitiveTyped()
	{
		if (_memberPrimitiveTyped == null)
		{
			_memberPrimitiveTyped = new MemberPrimitiveTyped();
		}
		_memberPrimitiveTyped.Read(this);
		PRs._objectTypeEnum = InternalObjectTypeE.Object;
		ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
		PRs.Init();
		PRs._varValue = _memberPrimitiveTyped._value;
		PRs._keyDt = Converter.ToComType(_memberPrimitiveTyped._primitiveTypeEnum);
		PRs._dtType = Converter.ToType(_memberPrimitiveTyped._primitiveTypeEnum);
		PRs._dtTypeCode = _memberPrimitiveTyped._primitiveTypeEnum;
		if (objectProgress == null)
		{
			PRs._parseTypeEnum = InternalParseTypeE.Object;
			PRs._name = "System.Variant";
		}
		else
		{
			PRs._parseTypeEnum = InternalParseTypeE.Member;
			PRs._memberValueEnum = InternalMemberValueE.InlineValue;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				PRs._name = objectProgress._name;
				PRs._memberTypeEnum = InternalMemberTypeE.Field;
				break;
			case InternalObjectTypeE.Array:
				PRs._memberTypeEnum = InternalMemberTypeE.Item;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectTypeEnum, objectProgress._objectTypeEnum.ToString()));
			}
		}
		_objectReader.Parse(PRs);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadArray(BinaryHeaderEnum binaryHeaderEnum)
	{
		BinaryAssemblyInfo binaryAssemblyInfo = null;
		BinaryArray binaryArray = new BinaryArray(binaryHeaderEnum);
		binaryArray.Read(this);
		if (binaryArray._binaryTypeEnum == BinaryTypeEnum.ObjectUser)
		{
			if (binaryArray._assemId < 1)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_AssemblyId, binaryArray._typeInformation));
			}
			binaryAssemblyInfo = (BinaryAssemblyInfo)AssemIdToAssemblyTable[binaryArray._assemId];
		}
		else
		{
			binaryAssemblyInfo = SystemAssemblyInfo;
		}
		ObjectProgress op = GetOp();
		ParseRecord pr = op._pr;
		op._objectTypeEnum = InternalObjectTypeE.Array;
		op._binaryTypeEnum = binaryArray._binaryTypeEnum;
		op._typeInformation = binaryArray._typeInformation;
		ObjectProgress objectProgress = (ObjectProgress)_stack.PeekPeek();
		if (objectProgress == null || binaryArray._objectId > 0)
		{
			op._name = "System.Array";
			pr._parseTypeEnum = InternalParseTypeE.Object;
			op._memberValueEnum = InternalMemberValueE.Empty;
		}
		else
		{
			pr._parseTypeEnum = InternalParseTypeE.Member;
			pr._memberValueEnum = InternalMemberValueE.Nested;
			op._memberValueEnum = InternalMemberValueE.Nested;
			switch (objectProgress._objectTypeEnum)
			{
			case InternalObjectTypeE.Object:
				pr._name = objectProgress._name;
				pr._memberTypeEnum = InternalMemberTypeE.Field;
				op._memberTypeEnum = InternalMemberTypeE.Field;
				pr._keyDt = objectProgress._name;
				pr._dtType = objectProgress._dtType;
				break;
			case InternalObjectTypeE.Array:
				pr._memberTypeEnum = InternalMemberTypeE.Item;
				op._memberTypeEnum = InternalMemberTypeE.Item;
				break;
			default:
				throw new SerializationException(System.SR.Format(System.SR.Serialization_ObjectTypeEnum, objectProgress._objectTypeEnum.ToString()));
			}
		}
		pr._objectId = _objectReader.GetId(binaryArray._objectId);
		if (pr._objectId == _topId)
		{
			pr._objectPositionEnum = InternalObjectPositionE.Top;
		}
		else if (_headerId > 0 && pr._objectId == _headerId)
		{
			pr._objectPositionEnum = InternalObjectPositionE.Headers;
		}
		else
		{
			pr._objectPositionEnum = InternalObjectPositionE.Child;
		}
		pr._objectTypeEnum = InternalObjectTypeE.Array;
		BinaryTypeConverter.TypeFromInfo(binaryArray._binaryTypeEnum, binaryArray._typeInformation, _objectReader, binaryAssemblyInfo, out pr._arrayElementTypeCode, out pr._arrayElementTypeString, out pr._arrayElementType, out pr._isArrayVariant);
		pr._dtTypeCode = InternalPrimitiveTypeE.Invalid;
		pr._rank = binaryArray._rank;
		pr._lengthA = binaryArray._lengthA;
		pr._lowerBoundA = binaryArray._lowerBoundA;
		bool flag = false;
		switch (binaryArray._binaryArrayTypeEnum)
		{
		case BinaryArrayTypeEnum.Single:
		case BinaryArrayTypeEnum.SingleOffset:
			op._numItems = binaryArray._lengthA[0];
			pr._arrayTypeEnum = InternalArrayTypeE.Single;
			if (Converter.IsWriteAsByteArray(pr._arrayElementTypeCode) && binaryArray._lowerBoundA[0] == 0)
			{
				flag = true;
				ReadArrayAsBytes(pr);
			}
			break;
		case BinaryArrayTypeEnum.Jagged:
		case BinaryArrayTypeEnum.JaggedOffset:
			op._numItems = binaryArray._lengthA[0];
			pr._arrayTypeEnum = InternalArrayTypeE.Jagged;
			break;
		case BinaryArrayTypeEnum.Rectangular:
		case BinaryArrayTypeEnum.RectangularOffset:
		{
			int num = 1;
			for (int i = 0; i < binaryArray._rank; i++)
			{
				num *= binaryArray._lengthA[i];
			}
			op._numItems = num;
			pr._arrayTypeEnum = InternalArrayTypeE.Rectangular;
			break;
		}
		default:
			throw new SerializationException(System.SR.Format(System.SR.Serialization_ArrayType, binaryArray._binaryArrayTypeEnum.ToString()));
		}
		if (!flag)
		{
			_stack.Push(op);
		}
		else
		{
			PutOp(op);
		}
		_objectReader.Parse(pr);
		if (flag)
		{
			pr._parseTypeEnum = InternalParseTypeE.ObjectEnd;
			_objectReader.Parse(pr);
		}
	}

	private void ReadArrayAsBytes(ParseRecord pr)
	{
		if (pr._arrayElementTypeCode == InternalPrimitiveTypeE.Byte)
		{
			pr._newObj = ReadBytes(pr._lengthA[0]);
			return;
		}
		if (pr._arrayElementTypeCode == InternalPrimitiveTypeE.Char)
		{
			pr._newObj = ReadChars(pr._lengthA[0]);
			return;
		}
		int num = Converter.TypeLength(pr._arrayElementTypeCode);
		pr._newObj = Converter.CreatePrimitiveArray(pr._arrayElementTypeCode, pr._lengthA[0]);
		Array array = (Array)pr._newObj;
		int i = 0;
		if (_byteBuffer == null)
		{
			_byteBuffer = new byte[4096];
		}
		int num2;
		for (; i < array.Length; i += num2)
		{
			num2 = Math.Min(4096 / num, array.Length - i);
			int num3 = num2 * num;
			ReadBytes(_byteBuffer, 0, num3);
			if (!BitConverter.IsLittleEndian)
			{
				for (int j = 0; j < num3; j += num)
				{
					for (int k = 0; k < num / 2; k++)
					{
						byte b = _byteBuffer[j + k];
						_byteBuffer[j + k] = _byteBuffer[j + num - 1 - k];
						_byteBuffer[j + num - 1 - k] = b;
					}
				}
			}
			Buffer.BlockCopy(_byteBuffer, 0, array, i * num, num3);
		}
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadMemberPrimitiveUnTyped()
	{
		ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
		if (memberPrimitiveUnTyped == null)
		{
			memberPrimitiveUnTyped = new MemberPrimitiveUnTyped();
		}
		memberPrimitiveUnTyped.Set((InternalPrimitiveTypeE)_expectedTypeInformation);
		memberPrimitiveUnTyped.Read(this);
		PRs.Init();
		PRs._varValue = memberPrimitiveUnTyped._value;
		PRs._dtTypeCode = (InternalPrimitiveTypeE)_expectedTypeInformation;
		PRs._dtType = Converter.ToType(PRs._dtTypeCode);
		PRs._parseTypeEnum = InternalParseTypeE.Member;
		PRs._memberValueEnum = InternalMemberValueE.InlineValue;
		if (objectProgress._objectTypeEnum == InternalObjectTypeE.Object)
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Field;
			PRs._name = objectProgress._name;
		}
		else
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Item;
		}
		_objectReader.Parse(PRs);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadMemberReference()
	{
		if (_memberReference == null)
		{
			_memberReference = new MemberReference();
		}
		_memberReference.Read(this);
		ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
		PRs.Init();
		PRs._idRef = _objectReader.GetId(_memberReference._idRef);
		PRs._parseTypeEnum = InternalParseTypeE.Member;
		PRs._memberValueEnum = InternalMemberValueE.Reference;
		if (objectProgress._objectTypeEnum == InternalObjectTypeE.Object)
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Field;
			PRs._name = objectProgress._name;
			PRs._dtType = objectProgress._dtType;
		}
		else
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Item;
		}
		_objectReader.Parse(PRs);
	}

	[RequiresUnreferencedCode("ObjectReader requires unreferenced code")]
	private void ReadObjectNull(BinaryHeaderEnum binaryHeaderEnum)
	{
		if (_objectNull == null)
		{
			_objectNull = new ObjectNull();
		}
		_objectNull.Read(this, binaryHeaderEnum);
		ObjectProgress objectProgress = (ObjectProgress)_stack.Peek();
		PRs.Init();
		PRs._parseTypeEnum = InternalParseTypeE.Member;
		PRs._memberValueEnum = InternalMemberValueE.Null;
		if (objectProgress._objectTypeEnum == InternalObjectTypeE.Object)
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Field;
			PRs._name = objectProgress._name;
			PRs._dtType = objectProgress._dtType;
		}
		else
		{
			PRs._memberTypeEnum = InternalMemberTypeE.Item;
			PRs._consecutiveNullArrayEntryCount = _objectNull._nullCount;
			objectProgress.ArrayCountIncrement(_objectNull._nullCount - 1);
		}
		_objectReader.Parse(PRs);
	}

	private void ReadMessageEnd()
	{
		if (_messageEnd == null)
		{
			_messageEnd = new MessageEnd();
		}
		_messageEnd.Read(this);
		if (!_stack.IsEmpty())
		{
			throw new SerializationException(System.SR.Serialization_StreamEnd);
		}
	}

	internal object ReadValue(InternalPrimitiveTypeE code)
	{
		return code switch
		{
			InternalPrimitiveTypeE.Boolean => ReadBoolean(), 
			InternalPrimitiveTypeE.Byte => ReadByte(), 
			InternalPrimitiveTypeE.Char => ReadChar(), 
			InternalPrimitiveTypeE.Double => ReadDouble(), 
			InternalPrimitiveTypeE.Int16 => ReadInt16(), 
			InternalPrimitiveTypeE.Int32 => ReadInt32(), 
			InternalPrimitiveTypeE.Int64 => ReadInt64(), 
			InternalPrimitiveTypeE.SByte => ReadSByte(), 
			InternalPrimitiveTypeE.Single => ReadSingle(), 
			InternalPrimitiveTypeE.UInt16 => ReadUInt16(), 
			InternalPrimitiveTypeE.UInt32 => ReadUInt32(), 
			InternalPrimitiveTypeE.UInt64 => ReadUInt64(), 
			InternalPrimitiveTypeE.Decimal => ReadDecimal(), 
			InternalPrimitiveTypeE.TimeSpan => ReadTimeSpan(), 
			InternalPrimitiveTypeE.DateTime => ReadDateTime(), 
			_ => throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeCode, code.ToString())), 
		};
	}

	private ObjectProgress GetOp()
	{
		ObjectProgress objectProgress;
		if (_opPool != null && !_opPool.IsEmpty())
		{
			objectProgress = (ObjectProgress)_opPool.Pop();
			objectProgress.Init();
		}
		else
		{
			objectProgress = new ObjectProgress();
		}
		return objectProgress;
	}

	private void PutOp(ObjectProgress op)
	{
		if (_opPool == null)
		{
			_opPool = new SerStack("opPool");
		}
		_opPool.Push(op);
	}
}
