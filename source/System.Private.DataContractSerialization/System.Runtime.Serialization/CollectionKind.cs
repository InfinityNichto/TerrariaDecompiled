namespace System.Runtime.Serialization;

internal enum CollectionKind : byte
{
	None,
	GenericDictionary,
	Dictionary,
	GenericList,
	GenericCollection,
	List,
	GenericEnumerable,
	Collection,
	Enumerable,
	Array
}
