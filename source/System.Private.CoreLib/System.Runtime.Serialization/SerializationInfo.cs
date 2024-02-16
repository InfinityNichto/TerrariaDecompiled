using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.Serialization;

public sealed class SerializationInfo
{
	private string[] _names;

	private object[] _values;

	private Type[] _types;

	private int _count;

	private readonly Dictionary<string, int> _nameToIndex;

	private readonly IFormatterConverter _converter;

	private string _rootTypeName;

	private string _rootTypeAssemblyName;

	private Type _rootType;

	[ThreadStatic]
	private static DeserializationTracker t_deserializationTracker;

	public string FullTypeName
	{
		get
		{
			return _rootTypeName;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_rootTypeName = value;
			IsFullTypeNameSetExplicit = true;
		}
	}

	public string AssemblyName
	{
		get
		{
			return _rootTypeAssemblyName;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_rootTypeAssemblyName = value;
			IsAssemblyNameSetExplicit = true;
		}
	}

	public bool IsFullTypeNameSetExplicit { get; private set; }

	public bool IsAssemblyNameSetExplicit { get; private set; }

	public int MemberCount => _count;

	public Type ObjectType => _rootType;

	internal static AsyncLocal<bool> AsyncDeserializationInProgress { get; } = new AsyncLocal<bool>();


	public static bool DeserializationInProgress
	{
		get
		{
			if (AsyncDeserializationInProgress.Value)
			{
				return true;
			}
			DeserializationTracker threadDeserializationTracker = GetThreadDeserializationTracker();
			return threadDeserializationTracker.DeserializationInProgress;
		}
	}

	[CLSCompliant(false)]
	public SerializationInfo(Type type, IFormatterConverter converter)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (converter == null)
		{
			throw new ArgumentNullException("converter");
		}
		_rootType = type;
		_rootTypeName = type.FullName;
		_rootTypeAssemblyName = type.Module.Assembly.FullName;
		_names = new string[4];
		_values = new object[4];
		_types = new Type[4];
		_nameToIndex = new Dictionary<string, int>();
		_converter = converter;
	}

	[CLSCompliant(false)]
	public SerializationInfo(Type type, IFormatterConverter converter, bool requireSameTokenInPartialTrust)
		: this(type, converter)
	{
	}

	public void SetType(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if ((object)_rootType != type)
		{
			_rootType = type;
			_rootTypeName = type.FullName;
			_rootTypeAssemblyName = type.Module.Assembly.FullName;
			IsFullTypeNameSetExplicit = false;
			IsAssemblyNameSetExplicit = false;
		}
	}

	public SerializationInfoEnumerator GetEnumerator()
	{
		return new SerializationInfoEnumerator(_names, _values, _types, _count);
	}

	private void ExpandArrays()
	{
		int num = _count * 2;
		if (num < _count && int.MaxValue > _count)
		{
			num = int.MaxValue;
		}
		string[] array = new string[num];
		object[] array2 = new object[num];
		Type[] array3 = new Type[num];
		Array.Copy(_names, array, _count);
		Array.Copy(_values, array2, _count);
		Array.Copy(_types, array3, _count);
		_names = array;
		_values = array2;
		_types = array3;
	}

	public void AddValue(string name, object? value, Type type)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		AddValueInternal(name, value, type);
	}

	public void AddValue(string name, object? value)
	{
		if (value == null)
		{
			AddValue(name, value, typeof(object));
		}
		else
		{
			AddValue(name, value, value.GetType());
		}
	}

	public void AddValue(string name, bool value)
	{
		AddValue(name, value, typeof(bool));
	}

	public void AddValue(string name, char value)
	{
		AddValue(name, value, typeof(char));
	}

	[CLSCompliant(false)]
	public void AddValue(string name, sbyte value)
	{
		AddValue(name, value, typeof(sbyte));
	}

	public void AddValue(string name, byte value)
	{
		AddValue(name, value, typeof(byte));
	}

	public void AddValue(string name, short value)
	{
		AddValue(name, value, typeof(short));
	}

	[CLSCompliant(false)]
	public void AddValue(string name, ushort value)
	{
		AddValue(name, value, typeof(ushort));
	}

	public void AddValue(string name, int value)
	{
		AddValue(name, value, typeof(int));
	}

	[CLSCompliant(false)]
	public void AddValue(string name, uint value)
	{
		AddValue(name, value, typeof(uint));
	}

	public void AddValue(string name, long value)
	{
		AddValue(name, value, typeof(long));
	}

	[CLSCompliant(false)]
	public void AddValue(string name, ulong value)
	{
		AddValue(name, value, typeof(ulong));
	}

	public void AddValue(string name, float value)
	{
		AddValue(name, value, typeof(float));
	}

	public void AddValue(string name, double value)
	{
		AddValue(name, value, typeof(double));
	}

	public void AddValue(string name, decimal value)
	{
		AddValue(name, value, typeof(decimal));
	}

	public void AddValue(string name, DateTime value)
	{
		AddValue(name, value, typeof(DateTime));
	}

	internal void AddValueInternal(string name, object value, Type type)
	{
		if (!_nameToIndex.TryAdd(name, _count))
		{
			throw new SerializationException(SR.Serialization_SameNameTwice);
		}
		if (_count >= _names.Length)
		{
			ExpandArrays();
		}
		_names[_count] = name;
		_values[_count] = value;
		_types[_count] = type;
		_count++;
	}

	public void UpdateValue(string name, object value, Type type)
	{
		int num = FindElement(name);
		if (num < 0)
		{
			AddValueInternal(name, value, type);
			return;
		}
		_values[num] = value;
		_types[num] = type;
	}

	private int FindElement(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_nameToIndex.TryGetValue(name, out var value))
		{
			return value;
		}
		return -1;
	}

	private object GetElement(string name, out Type foundType)
	{
		int num = FindElement(name);
		if (num == -1)
		{
			throw new SerializationException(SR.Format(SR.Serialization_NotFound, name));
		}
		foundType = _types[num];
		return _values[num];
	}

	private object GetElementNoThrow(string name, out Type foundType)
	{
		int num = FindElement(name);
		if (num == -1)
		{
			foundType = null;
			return null;
		}
		foundType = _types[num];
		return _values[num];
	}

	public object? GetValue(string name, Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (!type.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType);
		}
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType == type || type.IsAssignableFrom(foundType) || element == null)
		{
			return element;
		}
		return _converter.Convert(element, type);
	}

	internal object GetValueNoThrow(string name, Type type)
	{
		Type foundType;
		object elementNoThrow = GetElementNoThrow(name, out foundType);
		if (elementNoThrow == null)
		{
			return null;
		}
		if ((object)foundType == type || type.IsAssignableFrom(foundType))
		{
			return elementNoThrow;
		}
		return _converter.Convert(elementNoThrow, type);
	}

	public bool GetBoolean(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(bool))
		{
			return _converter.ToBoolean(element);
		}
		return (bool)element;
	}

	public char GetChar(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(char))
		{
			return _converter.ToChar(element);
		}
		return (char)element;
	}

	[CLSCompliant(false)]
	public sbyte GetSByte(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(sbyte))
		{
			return _converter.ToSByte(element);
		}
		return (sbyte)element;
	}

	public byte GetByte(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(byte))
		{
			return _converter.ToByte(element);
		}
		return (byte)element;
	}

	public short GetInt16(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(short))
		{
			return _converter.ToInt16(element);
		}
		return (short)element;
	}

	[CLSCompliant(false)]
	public ushort GetUInt16(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(ushort))
		{
			return _converter.ToUInt16(element);
		}
		return (ushort)element;
	}

	public int GetInt32(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(int))
		{
			return _converter.ToInt32(element);
		}
		return (int)element;
	}

	[CLSCompliant(false)]
	public uint GetUInt32(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(uint))
		{
			return _converter.ToUInt32(element);
		}
		return (uint)element;
	}

	public long GetInt64(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(long))
		{
			return _converter.ToInt64(element);
		}
		return (long)element;
	}

	[CLSCompliant(false)]
	public ulong GetUInt64(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(ulong))
		{
			return _converter.ToUInt64(element);
		}
		return (ulong)element;
	}

	public float GetSingle(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(float))
		{
			return _converter.ToSingle(element);
		}
		return (float)element;
	}

	public double GetDouble(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(double))
		{
			return _converter.ToDouble(element);
		}
		return (double)element;
	}

	public decimal GetDecimal(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(decimal))
		{
			return _converter.ToDecimal(element);
		}
		return (decimal)element;
	}

	public DateTime GetDateTime(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(DateTime))
		{
			return _converter.ToDateTime(element);
		}
		return (DateTime)element;
	}

	public string? GetString(string name)
	{
		Type foundType;
		object element = GetElement(name, out foundType);
		if ((object)foundType != typeof(string) && element != null)
		{
			return _converter.ToString(element);
		}
		return (string)element;
	}

	private static DeserializationTracker GetThreadDeserializationTracker()
	{
		return t_deserializationTracker ?? (t_deserializationTracker = new DeserializationTracker());
	}

	public static void ThrowIfDeserializationInProgress()
	{
		if (DeserializationInProgress)
		{
			throw new SerializationException(SR.Serialization_DangerousDeserialization);
		}
	}

	public static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
	{
		if (cachedValue == 0)
		{
			if (AppContext.TryGetSwitch("Switch.System.Runtime.Serialization.SerializationGuard." + switchSuffix, out var isEnabled) && isEnabled)
			{
				cachedValue = 1;
			}
			else
			{
				cachedValue = -1;
			}
		}
		if (cachedValue != 1)
		{
			if (cachedValue != -1)
			{
				throw new ArgumentOutOfRangeException("cachedValue");
			}
			if (DeserializationInProgress)
			{
				throw new SerializationException(SR.Format(SR.Serialization_DangerousDeserialization_Switch, "Switch.System.Runtime.Serialization.SerializationGuard." + switchSuffix));
			}
		}
	}

	public static DeserializationToken StartDeserialization()
	{
		if (LocalAppContextSwitches.SerializationGuard)
		{
			DeserializationTracker threadDeserializationTracker = GetThreadDeserializationTracker();
			if (!threadDeserializationTracker.DeserializationInProgress)
			{
				lock (threadDeserializationTracker)
				{
					if (!threadDeserializationTracker.DeserializationInProgress)
					{
						AsyncDeserializationInProgress.Value = true;
						threadDeserializationTracker.DeserializationInProgress = true;
						return new DeserializationToken(threadDeserializationTracker);
					}
				}
			}
		}
		return new DeserializationToken(null);
	}
}
