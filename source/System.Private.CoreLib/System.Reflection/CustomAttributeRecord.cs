using System.Runtime.InteropServices;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal struct CustomAttributeRecord
{
	internal ConstArray blob;

	internal MetadataToken tkCtor;
}
