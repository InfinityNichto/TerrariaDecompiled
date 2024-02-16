namespace System.IO.Compression;

internal enum ZipVersionNeededValues : ushort
{
	Default = 10,
	ExplicitDirectory = 20,
	Deflate = 20,
	Deflate64 = 21,
	Zip64 = 45
}
