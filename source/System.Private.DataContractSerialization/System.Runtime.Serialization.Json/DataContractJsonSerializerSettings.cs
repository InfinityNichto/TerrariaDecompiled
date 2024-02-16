using System.Collections.Generic;

namespace System.Runtime.Serialization.Json;

public class DataContractJsonSerializerSettings
{
	private int _maxItemsInObjectGraph = int.MaxValue;

	public string? RootName { get; set; }

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

	public EmitTypeInformation EmitTypeInformation { get; set; }

	public DateTimeFormat? DateTimeFormat { get; set; }

	public bool SerializeReadOnlyTypes { get; set; }

	public bool UseSimpleDictionaryFormat { get; set; }
}
