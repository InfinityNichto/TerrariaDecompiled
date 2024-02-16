namespace System.Runtime.Serialization;

internal interface IKeyValue
{
	object Key { get; set; }

	object Value { get; set; }
}
