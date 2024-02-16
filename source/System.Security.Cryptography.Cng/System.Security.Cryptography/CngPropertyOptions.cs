namespace System.Security.Cryptography;

[Flags]
public enum CngPropertyOptions
{
	None = 0,
	CustomProperty = 0x40000000,
	Persist = int.MinValue
}
