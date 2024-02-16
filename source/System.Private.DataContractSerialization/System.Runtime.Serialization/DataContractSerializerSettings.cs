using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization;

public class DataContractSerializerSettings
{
	private int _maxItemsInObjectGraph = int.MaxValue;

	public XmlDictionaryString? RootName { get; set; }

	public XmlDictionaryString? RootNamespace { get; set; }

	public IEnumerable<Type>? KnownTypes { get; set; }

	public int MaxItemsInObjectGraph
	{
		get
		{
			return _maxItemsInObjectGraph;
		}
		set
		{
			_maxItemsInObjectGraph = value;
		}
	}

	public bool IgnoreExtensionDataObject { get; set; }

	public bool PreserveObjectReferences { get; set; }

	public DataContractResolver? DataContractResolver { get; set; }

	public bool SerializeReadOnlyTypes { get; set; }
}
