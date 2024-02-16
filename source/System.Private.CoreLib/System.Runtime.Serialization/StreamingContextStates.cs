namespace System.Runtime.Serialization;

[Flags]
public enum StreamingContextStates
{
	CrossProcess = 1,
	CrossMachine = 2,
	File = 4,
	Persistence = 8,
	Remoting = 0x10,
	Other = 0x20,
	Clone = 0x40,
	CrossAppDomain = 0x80,
	All = 0xFF
}
