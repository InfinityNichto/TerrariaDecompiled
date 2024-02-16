using System.Runtime.InteropServices;

namespace System.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct CustomAttributeType
{
	private readonly string m_enumName;

	private readonly CustomAttributeEncoding m_encodedType;

	private readonly CustomAttributeEncoding m_encodedEnumType;

	private readonly CustomAttributeEncoding m_encodedArrayType;

	private readonly CustomAttributeEncoding m_padding;

	public CustomAttributeEncoding EncodedType => m_encodedType;

	public CustomAttributeEncoding EncodedEnumType => m_encodedEnumType;

	public CustomAttributeEncoding EncodedArrayType => m_encodedArrayType;

	public string EnumName => m_enumName;

	public CustomAttributeType(CustomAttributeEncoding encodedType, CustomAttributeEncoding encodedArrayType, CustomAttributeEncoding encodedEnumType, string enumName)
	{
		m_encodedType = encodedType;
		m_encodedArrayType = encodedArrayType;
		m_encodedEnumType = encodedEnumType;
		m_enumName = enumName;
		m_padding = m_encodedType;
	}
}
