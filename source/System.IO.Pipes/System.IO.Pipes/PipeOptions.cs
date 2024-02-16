namespace System.IO.Pipes;

[Flags]
public enum PipeOptions
{
	None = 0,
	WriteThrough = int.MinValue,
	Asynchronous = 0x40000000,
	CurrentUserOnly = 0x20000000
}
