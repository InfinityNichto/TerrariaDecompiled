using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Collections;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct DictionaryEntry
{
	private object _key;

	private object _value;

	public object Key
	{
		get
		{
			return _key;
		}
		set
		{
			_key = value;
		}
	}

	public object? Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	public DictionaryEntry(object key, object? value)
	{
		_key = key;
		_value = value;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Deconstruct(out object key, out object? value)
	{
		key = Key;
		value = Value;
	}
}
