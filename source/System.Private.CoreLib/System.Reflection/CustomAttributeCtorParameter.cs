using System.Runtime.InteropServices;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CustomAttributeCtorParameter
{
	private readonly CustomAttributeType m_type;

	private readonly CustomAttributeEncodedArgument m_encodedArgument;

	public CustomAttributeEncodedArgument CustomAttributeEncodedArgument => m_encodedArgument;

	public CustomAttributeCtorParameter(CustomAttributeType type)
	{
		m_type = type;
		m_encodedArgument = default(CustomAttributeEncodedArgument);
	}
}
