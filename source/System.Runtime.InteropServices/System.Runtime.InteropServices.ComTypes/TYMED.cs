namespace System.Runtime.InteropServices.ComTypes;

[Flags]
public enum TYMED
{
	TYMED_HGLOBAL = 1,
	TYMED_FILE = 2,
	TYMED_ISTREAM = 4,
	TYMED_ISTORAGE = 8,
	TYMED_GDI = 0x10,
	TYMED_MFPICT = 0x20,
	TYMED_ENHMF = 0x40,
	TYMED_NULL = 0
}
