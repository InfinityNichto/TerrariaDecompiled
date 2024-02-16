using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization;

internal struct ScopedKnownTypes
{
	internal Dictionary<XmlQualifiedName, DataContract>[] dataContractDictionaries;

	private int _count;

	internal void Push(Dictionary<XmlQualifiedName, DataContract> dataContractDictionary)
	{
		if (dataContractDictionaries == null)
		{
			dataContractDictionaries = new Dictionary<XmlQualifiedName, DataContract>[4];
		}
		else if (_count == dataContractDictionaries.Length)
		{
			Array.Resize(ref dataContractDictionaries, dataContractDictionaries.Length * 2);
		}
		dataContractDictionaries[_count++] = dataContractDictionary;
	}

	internal void Pop()
	{
		_count--;
	}

	internal DataContract GetDataContract(XmlQualifiedName qname)
	{
		for (int num = _count - 1; num >= 0; num--)
		{
			Dictionary<XmlQualifiedName, DataContract> dictionary = dataContractDictionaries[num];
			if (dictionary.TryGetValue(qname, out var value))
			{
				return value;
			}
		}
		return null;
	}
}
