namespace System.Runtime.Serialization;

[DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
internal struct KeyValue<K, V> : IKeyValue
{
	private K _key;

	private V _value;

	[DataMember(IsRequired = true)]
	public K Key
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

	[DataMember(IsRequired = true)]
	public V Value
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

	object IKeyValue.Key
	{
		get
		{
			return _key;
		}
		set
		{
			_key = (K)value;
		}
	}

	object IKeyValue.Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = (V)value;
		}
	}

	internal KeyValue(K key, V value)
	{
		_key = key;
		_value = value;
	}
}
