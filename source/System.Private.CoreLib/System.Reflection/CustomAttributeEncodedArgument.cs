using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CustomAttributeEncodedArgument
{
	private readonly long m_primitiveValue;

	private readonly CustomAttributeEncodedArgument[] m_arrayValue;

	private readonly string m_stringValue;

	private readonly CustomAttributeType m_type;

	public CustomAttributeType CustomAttributeType => m_type;

	public long PrimitiveValue => m_primitiveValue;

	public CustomAttributeEncodedArgument[] ArrayValue => m_arrayValue;

	public string StringValue => m_stringValue;

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void ParseAttributeArguments(IntPtr pCa, int cCa, ref CustomAttributeCtorParameter[] CustomAttributeCtorParameters, ref CustomAttributeNamedParameter[] CustomAttributeTypedArgument, RuntimeAssembly assembly);

	internal static void ParseAttributeArguments(ConstArray attributeBlob, ref CustomAttributeCtorParameter[] customAttributeCtorParameters, ref CustomAttributeNamedParameter[] customAttributeNamedParameters, RuntimeModule customAttributeModule)
	{
		if ((object)customAttributeModule == null)
		{
			throw new ArgumentNullException("customAttributeModule");
		}
		if (customAttributeCtorParameters.Length != 0 || customAttributeNamedParameters.Length != 0)
		{
			ParseAttributeArguments(attributeBlob.Signature, attributeBlob.Length, ref customAttributeCtorParameters, ref customAttributeNamedParameters, (RuntimeAssembly)customAttributeModule.Assembly);
		}
	}
}
