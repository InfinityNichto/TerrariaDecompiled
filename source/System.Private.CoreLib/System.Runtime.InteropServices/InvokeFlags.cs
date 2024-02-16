namespace System.Runtime.InteropServices;

[Flags]
internal enum InvokeFlags : short
{
	DISPATCH_METHOD = 1,
	DISPATCH_PROPERTYGET = 2,
	DISPATCH_PROPERTYPUT = 4,
	DISPATCH_PROPERTYPUTREF = 8
}
