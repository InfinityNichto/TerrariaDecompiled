namespace System.Security.Cryptography;

[Flags]
internal enum CngExportPolicies
{
	None = 0,
	AllowPlaintextExport = 2
}
