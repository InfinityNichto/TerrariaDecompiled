using System.Collections.Generic;

namespace System.Runtime.Serialization;

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/System.Collections.Generic")]
internal sealed class KeyValuePairAdapter<K, T> : IKeyValuePairAdapter
{
	private K _kvpKey;

	private T _kvpValue;

	[DataMember(Name = "key")]
	public K Key
	{
		get
		{
			return _kvpKey;
		}
		set
		{
			_kvpKey = value;
		}
	}

	[DataMember(Name = "value")]
	public T Value
	{
		get
		{
			return _kvpValue;
		}
		set
		{
			_kvpValue = value;
		}
	}

	public KeyValuePairAdapter(KeyValuePair<K, T> kvPair)
	{
		_kvpKey = kvPair.Key;
		_kvpValue = kvPair.Value;
	}

	internal KeyValuePair<K, T> GetKeyValuePair()
	{
		return new KeyValuePair<K, T>(_kvpKey, _kvpValue);
	}

	internal static KeyValuePairAdapter<K, T> GetKeyValuePairAdapter(KeyValuePair<K, T> kvPair)
	{
		return new KeyValuePairAdapter<K, T>(kvPair);
	}
}
