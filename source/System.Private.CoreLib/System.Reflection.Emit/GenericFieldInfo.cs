namespace System.Reflection.Emit;

internal sealed class GenericFieldInfo
{
	internal RuntimeFieldHandle m_fieldHandle;

	internal RuntimeTypeHandle m_context;

	internal GenericFieldInfo(RuntimeFieldHandle fieldHandle, RuntimeTypeHandle context)
	{
		m_fieldHandle = fieldHandle;
		m_context = context;
	}
}
