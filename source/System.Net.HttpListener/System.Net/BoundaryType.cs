namespace System.Net;

internal enum BoundaryType
{
	ContentLength = 0,
	Chunked = 1,
	Multipart = 3,
	None = 4,
	Invalid = 5
}
