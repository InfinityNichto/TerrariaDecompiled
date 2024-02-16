namespace System.Text.Json;

internal enum ConverterStrategy : byte
{
	None = 0,
	Object = 1,
	Value = 2,
	Enumerable = 8,
	Dictionary = 0x10
}
