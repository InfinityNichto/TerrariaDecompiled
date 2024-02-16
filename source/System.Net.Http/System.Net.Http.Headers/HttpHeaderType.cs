namespace System.Net.Http.Headers;

[Flags]
internal enum HttpHeaderType : byte
{
	General = 1,
	Request = 2,
	Response = 4,
	Content = 8,
	Custom = 0x10,
	NonTrailing = 0x20,
	All = 0x3F,
	None = 0
}
