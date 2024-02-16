using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class WriteObjectInfo
{
	internal int _objectInfoId;

	internal object _obj;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal Type _objectType;

	internal bool _isSi;

	internal bool _isNamed;

	internal bool _isArray;

	internal SerializationInfo _si;

	internal SerObjectInfoCache _cache;

	internal object[] _memberData;

	internal ISerializationSurrogate _serializationSurrogate;

	internal StreamingContext _context;

	internal SerObjectInfoInit _serObjectInfoInit;

	internal long _objectId;

	internal long _assemId;

	private string _binderTypeName;

	private string _binderAssemblyString;

	internal WriteObjectInfo()
	{
	}

	internal void ObjectEnd()
	{
		PutObjectInfo(_serObjectInfoInit, this);
	}

	private void InternalInit()
	{
		_obj = null;
		_objectType = null;
		_isSi = false;
		_isNamed = false;
		_isArray = false;
		_si = null;
		_cache = null;
		_memberData = null;
		_objectId = 0L;
		_assemId = 0L;
		_binderTypeName = null;
		_binderAssemblyString = null;
	}

	[RequiresUnreferencedCode("It isn't possible to statically get the Type of object")]
	internal static WriteObjectInfo Serialize(object obj, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, ObjectWriter objectWriter, SerializationBinder binder)
	{
		WriteObjectInfo objectInfo = GetObjectInfo(serObjectInfoInit);
		objectInfo.InitSerialize(obj, surrogateSelector, context, serObjectInfoInit, converter, objectWriter, binder);
		return objectInfo;
	}

	[RequiresUnreferencedCode("It isn't possible to statically get the Type of object")]
	internal void InitSerialize(object obj, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, ObjectWriter objectWriter, SerializationBinder binder)
	{
		_context = context;
		_obj = obj;
		_serObjectInfoInit = serObjectInfoInit;
		_objectType = obj.GetType();
		if (_objectType.IsArray)
		{
			_isArray = true;
			InitNoMembers();
			return;
		}
		InvokeSerializationBinder(binder);
		objectWriter.ObjectManager.RegisterObject(obj);
		if (surrogateSelector != null && (_serializationSurrogate = surrogateSelector.GetSurrogate(_objectType, context, out ISurrogateSelector _)) != null)
		{
			_si = new SerializationInfo(_objectType, converter);
			if (!_objectType.IsPrimitive)
			{
				_serializationSurrogate.GetObjectData(obj, _si, context);
			}
			InitSiWrite();
		}
		else if (obj is ISerializable)
		{
			if (!_objectType.IsSerializable)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_NonSerType, _objectType.FullName, _objectType.Assembly.FullName));
			}
			_si = new SerializationInfo(_objectType, converter);
			((ISerializable)obj).GetObjectData(_si, context);
			InitSiWrite();
			CheckTypeForwardedFrom(_cache, _objectType, _binderAssemblyString);
		}
		else
		{
			InitMemberInfo();
			CheckTypeForwardedFrom(_cache, _objectType, _binderAssemblyString);
		}
	}

	internal static WriteObjectInfo Serialize([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, SerializationBinder binder)
	{
		WriteObjectInfo objectInfo = GetObjectInfo(serObjectInfoInit);
		objectInfo.InitSerialize(objectType, surrogateSelector, context, serObjectInfoInit, converter, binder);
		return objectInfo;
	}

	internal void InitSerialize([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, SerializationBinder binder)
	{
		_objectType = objectType;
		_context = context;
		_serObjectInfoInit = serObjectInfoInit;
		if (objectType.IsArray)
		{
			InitNoMembers();
			return;
		}
		InvokeSerializationBinder(binder);
		if (surrogateSelector != null)
		{
			_serializationSurrogate = surrogateSelector.GetSurrogate(objectType, context, out ISurrogateSelector _);
		}
		if (_serializationSurrogate != null)
		{
			_si = new SerializationInfo(objectType, converter);
			_cache = new SerObjectInfoCache(objectType);
			_isSi = true;
		}
		else if ((object)objectType != Converter.s_typeofObject && Converter.s_typeofISerializable.IsAssignableFrom(objectType))
		{
			_si = new SerializationInfo(objectType, converter);
			_cache = new SerObjectInfoCache(objectType);
			CheckTypeForwardedFrom(_cache, objectType, _binderAssemblyString);
			_isSi = true;
		}
		if (!_isSi)
		{
			InitMemberInfo();
			CheckTypeForwardedFrom(_cache, objectType, _binderAssemblyString);
		}
	}

	private void InitSiWrite()
	{
		SerializationInfoEnumerator serializationInfoEnumerator = null;
		_isSi = true;
		serializationInfoEnumerator = _si.GetEnumerator();
		int num = 0;
		num = _si.MemberCount;
		int num2 = num;
		TypeInformation typeInformation = null;
		string fullTypeName = _si.FullTypeName;
		string assemblyName = _si.AssemblyName;
		bool hasTypeForwardedFrom = false;
		if (!_si.IsFullTypeNameSetExplicit)
		{
			typeInformation = BinaryFormatter.GetTypeInformation(_si.ObjectType);
			fullTypeName = typeInformation.FullTypeName;
			hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
		}
		if (!_si.IsAssemblyNameSetExplicit)
		{
			if (typeInformation == null)
			{
				typeInformation = BinaryFormatter.GetTypeInformation(_si.ObjectType);
			}
			assemblyName = typeInformation.AssemblyString;
			hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
		}
		_cache = new SerObjectInfoCache(fullTypeName, assemblyName, hasTypeForwardedFrom);
		_cache._memberNames = new string[num2];
		_cache._memberTypes = new Type[num2];
		_memberData = new object[num2];
		serializationInfoEnumerator = _si.GetEnumerator();
		int num3 = 0;
		while (serializationInfoEnumerator.MoveNext())
		{
			_cache._memberNames[num3] = serializationInfoEnumerator.Name;
			_cache._memberTypes[num3] = serializationInfoEnumerator.ObjectType;
			_memberData[num3] = serializationInfoEnumerator.Value;
			num3++;
		}
		_isNamed = true;
	}

	private static void CheckTypeForwardedFrom(SerObjectInfoCache cache, Type objectType, string binderAssemblyString)
	{
	}

	private void InitNoMembers()
	{
		if (!_serObjectInfoInit._seenBeforeTable.TryGetValue(_objectType, out _cache))
		{
			_cache = new SerObjectInfoCache(_objectType);
			_serObjectInfoInit._seenBeforeTable.Add(_objectType, _cache);
		}
	}

	private void InitMemberInfo()
	{
		if (!_serObjectInfoInit._seenBeforeTable.TryGetValue(_objectType, out _cache))
		{
			_cache = new SerObjectInfoCache(_objectType);
			_cache._memberInfos = FormatterServices.GetSerializableMembers(_objectType, _context);
			int num = _cache._memberInfos.Length;
			_cache._memberNames = new string[num];
			_cache._memberTypes = new Type[num];
			for (int i = 0; i < num; i++)
			{
				_cache._memberNames[i] = _cache._memberInfos[i].Name;
				_cache._memberTypes[i] = ((FieldInfo)_cache._memberInfos[i]).FieldType;
			}
			_serObjectInfoInit._seenBeforeTable.Add(_objectType, _cache);
		}
		if (_obj != null)
		{
			_memberData = FormatterServices.GetObjectData(_obj, _cache._memberInfos);
		}
		_isNamed = true;
	}

	internal string GetTypeFullName()
	{
		return _binderTypeName ?? _cache._fullTypeName;
	}

	internal string GetAssemblyString()
	{
		return _binderAssemblyString ?? _cache._assemblyString;
	}

	private void InvokeSerializationBinder(SerializationBinder binder)
	{
		BinaryFormatterEventSource.Log.SerializingObject(_objectType);
		binder?.BindToName(_objectType, out _binderAssemblyString, out _binderTypeName);
	}

	internal void GetMemberInfo(out string[] outMemberNames, out Type[] outMemberTypes, out object[] outMemberData)
	{
		outMemberNames = _cache._memberNames;
		outMemberTypes = _cache._memberTypes;
		outMemberData = _memberData;
		if (_isSi && !_isNamed)
		{
			throw new SerializationException(System.SR.Serialization_ISerializableMemberInfo);
		}
	}

	private static WriteObjectInfo GetObjectInfo(SerObjectInfoInit serObjectInfoInit)
	{
		WriteObjectInfo writeObjectInfo;
		if (!serObjectInfoInit._oiPool.IsEmpty())
		{
			writeObjectInfo = (WriteObjectInfo)serObjectInfoInit._oiPool.Pop();
			writeObjectInfo.InternalInit();
		}
		else
		{
			writeObjectInfo = new WriteObjectInfo();
			writeObjectInfo._objectInfoId = serObjectInfoInit._objectInfoIdCount++;
		}
		return writeObjectInfo;
	}

	private static void PutObjectInfo(SerObjectInfoInit serObjectInfoInit, WriteObjectInfo objectInfo)
	{
		serObjectInfoInit._oiPool.Push(objectInfo);
	}
}
