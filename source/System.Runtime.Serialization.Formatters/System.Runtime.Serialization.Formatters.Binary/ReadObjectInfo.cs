using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ReadObjectInfo
{
	internal int _objectInfoId;

	internal static int _readObjectInfoCounter;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal Type _objectType;

	internal ObjectManager _objectManager;

	internal int _count;

	internal bool _isSi;

	internal bool _isTyped;

	internal bool _isSimpleAssembly;

	internal SerObjectInfoCache _cache;

	internal string[] _wireMemberNames;

	internal Type[] _wireMemberTypes;

	private int _lastPosition;

	internal ISerializationSurrogate _serializationSurrogate;

	internal StreamingContext _context;

	internal List<Type> _memberTypesList;

	internal SerObjectInfoInit _serObjectInfoInit;

	internal IFormatterConverter _formatterConverter;

	internal ReadObjectInfo()
	{
	}

	internal void ObjectEnd()
	{
	}

	internal void PrepareForReuse()
	{
		_lastPosition = 0;
	}

	internal static ReadObjectInfo Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
	{
		ReadObjectInfo objectInfo = GetObjectInfo(serObjectInfoInit);
		objectInfo.Init(objectType, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
		return objectInfo;
	}

	internal void Init([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
	{
		_objectType = objectType;
		_objectManager = objectManager;
		_context = context;
		_serObjectInfoInit = serObjectInfoInit;
		_formatterConverter = converter;
		_isSimpleAssembly = bSimpleAssembly;
		InitReadConstructor(objectType, surrogateSelector, context);
	}

	internal static ReadObjectInfo Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
	{
		ReadObjectInfo objectInfo = GetObjectInfo(serObjectInfoInit);
		objectInfo.Init(objectType, memberNames, memberTypes, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
		return objectInfo;
	}

	internal void Init([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
	{
		_objectType = objectType;
		_objectManager = objectManager;
		_wireMemberNames = memberNames;
		_wireMemberTypes = memberTypes;
		_context = context;
		_serObjectInfoInit = serObjectInfoInit;
		_formatterConverter = converter;
		_isSimpleAssembly = bSimpleAssembly;
		if (memberTypes != null)
		{
			_isTyped = true;
		}
		if (objectType != null)
		{
			InitReadConstructor(objectType, surrogateSelector, context);
		}
	}

	private void InitReadConstructor(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context)
	{
		BinaryFormatterEventSource.Log.DeserializingObject(objectType);
		if (objectType.IsArray)
		{
			InitNoMembers();
			return;
		}
		ISurrogateSelector selector = null;
		if (surrogateSelector != null)
		{
			_serializationSurrogate = surrogateSelector.GetSurrogate(objectType, context, out selector);
		}
		if (_serializationSurrogate != null)
		{
			_isSi = true;
		}
		else if ((object)objectType != Converter.s_typeofObject && Converter.s_typeofISerializable.IsAssignableFrom(objectType))
		{
			_isSi = true;
		}
		if (_isSi)
		{
			InitSiRead();
		}
		else
		{
			InitMemberInfo();
		}
	}

	private void InitSiRead()
	{
		if (_memberTypesList != null)
		{
			_memberTypesList = new List<Type>(20);
		}
	}

	private void InitNoMembers()
	{
		_cache = new SerObjectInfoCache(_objectType);
	}

	private void InitMemberInfo()
	{
		_cache = new SerObjectInfoCache(_objectType);
		_cache._memberInfos = FormatterServices.GetSerializableMembers(_objectType, _context);
		_count = _cache._memberInfos.Length;
		_cache._memberNames = new string[_count];
		_cache._memberTypes = new Type[_count];
		for (int i = 0; i < _count; i++)
		{
			_cache._memberNames[i] = _cache._memberInfos[i].Name;
			_cache._memberTypes[i] = GetMemberType(_cache._memberInfos[i]);
		}
		_isTyped = true;
	}

	internal MemberInfo GetMemberInfo(string name)
	{
		if (_cache == null)
		{
			return null;
		}
		if (_isSi)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_MemberInfo, _objectType?.ToString() + " " + name));
		}
		if (_cache._memberInfos == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_NoMemberInfo, _objectType?.ToString() + " " + name));
		}
		int num = Position(name);
		if (num == -1)
		{
			return null;
		}
		return _cache._memberInfos[num];
	}

	internal Type GetType(string name)
	{
		int num = Position(name);
		if (num == -1)
		{
			return null;
		}
		Type type = (_isTyped ? _cache._memberTypes[num] : _memberTypesList[num]);
		if (type == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_ISerializableTypes, _objectType?.ToString() + " " + name));
		}
		return type;
	}

	internal void AddValue(string name, object value, ref SerializationInfo si, ref object[] memberData)
	{
		if (_isSi)
		{
			si.AddValue(name, value);
			return;
		}
		int num = Position(name);
		if (num != -1)
		{
			memberData[num] = value;
		}
	}

	internal void InitDataStore(ref SerializationInfo si, ref object[] memberData)
	{
		if (_isSi)
		{
			if (si == null)
			{
				si = new SerializationInfo(_objectType, _formatterConverter);
			}
		}
		else if (memberData == null && _cache != null)
		{
			memberData = new object[_cache._memberNames.Length];
		}
	}

	internal void RecordFixup(long objectId, string name, long idRef)
	{
		if (_isSi)
		{
			if (_objectManager == null)
			{
				throw new SerializationException(System.SR.Serialization_CorruptedStream);
			}
			_objectManager.RecordDelayedFixup(objectId, name, idRef);
			return;
		}
		int num = Position(name);
		if (num != -1)
		{
			if (_objectManager == null)
			{
				throw new SerializationException(System.SR.Serialization_CorruptedStream);
			}
			_objectManager.RecordFixup(objectId, _cache._memberInfos[num], idRef);
		}
	}

	internal void PopulateObjectMembers(object obj, object[] memberData)
	{
		if (!_isSi && memberData != null)
		{
			FormatterServices.PopulateObjectMembers(obj, _cache._memberInfos, memberData);
		}
	}

	private int Position(string name)
	{
		if (_cache == null)
		{
			return -1;
		}
		if (_cache._memberNames.Length != 0 && _cache._memberNames[_lastPosition].Equals(name))
		{
			return _lastPosition;
		}
		if (++_lastPosition < _cache._memberNames.Length && _cache._memberNames[_lastPosition].Equals(name))
		{
			return _lastPosition;
		}
		for (int i = 0; i < _cache._memberNames.Length; i++)
		{
			if (_cache._memberNames[i].Equals(name))
			{
				_lastPosition = i;
				return _lastPosition;
			}
		}
		_lastPosition = 0;
		return -1;
	}

	internal Type[] GetMemberTypes(string[] inMemberNames, Type objectType)
	{
		if (_isSi)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_ISerializableTypes, objectType));
		}
		if (_cache == null)
		{
			return null;
		}
		if (_cache._memberTypes == null)
		{
			_cache._memberTypes = new Type[_count];
			for (int i = 0; i < _count; i++)
			{
				_cache._memberTypes[i] = GetMemberType(_cache._memberInfos[i]);
			}
		}
		bool flag = false;
		if (inMemberNames.Length < _cache._memberInfos.Length)
		{
			flag = true;
		}
		Type[] array = new Type[_cache._memberInfos.Length];
		bool flag2 = false;
		for (int j = 0; j < _cache._memberInfos.Length; j++)
		{
			if (!flag && inMemberNames[j].Equals(_cache._memberInfos[j].Name))
			{
				array[j] = _cache._memberTypes[j];
				continue;
			}
			flag2 = false;
			for (int k = 0; k < inMemberNames.Length; k++)
			{
				if (_cache._memberInfos[j].Name.Equals(inMemberNames[k]))
				{
					array[j] = _cache._memberTypes[j];
					flag2 = true;
					break;
				}
			}
			if (!flag2 && !_isSimpleAssembly && _cache._memberInfos[j].GetCustomAttribute(typeof(OptionalFieldAttribute), inherit: false) == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_MissingMember, _cache._memberNames[j], objectType, typeof(OptionalFieldAttribute).FullName));
			}
		}
		return array;
	}

	internal Type GetMemberType(MemberInfo objMember)
	{
		if (objMember is FieldInfo)
		{
			return ((FieldInfo)objMember).FieldType;
		}
		throw new SerializationException(System.SR.Format(System.SR.Serialization_SerMemberInfo, objMember.GetType()));
	}

	private static ReadObjectInfo GetObjectInfo(SerObjectInfoInit serObjectInfoInit)
	{
		ReadObjectInfo readObjectInfo = new ReadObjectInfo();
		readObjectInfo._objectInfoId = Interlocked.Increment(ref _readObjectInfoCounter);
		return readObjectInfo;
	}
}
