using System.Runtime.InteropServices;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CustomAttributeNamedParameter
{
	private readonly string m_argumentName;

	private readonly CustomAttributeEncoding m_fieldOrProperty;

	private readonly CustomAttributeEncoding m_padding;

	private readonly CustomAttributeType m_type;

	private readonly CustomAttributeEncodedArgument m_encodedArgument;

	public CustomAttributeEncodedArgument EncodedArgument => m_encodedArgument;

	public CustomAttributeNamedParameter(string argumentName, CustomAttributeEncoding fieldOrProperty, CustomAttributeType type)
	{
		if (argumentName == null)
		{
			throw new ArgumentNullException("argumentName");
		}
		m_argumentName = argumentName;
		m_fieldOrProperty = fieldOrProperty;
		m_padding = fieldOrProperty;
		m_type = type;
		m_encodedArgument = default(CustomAttributeEncodedArgument);
	}
}
