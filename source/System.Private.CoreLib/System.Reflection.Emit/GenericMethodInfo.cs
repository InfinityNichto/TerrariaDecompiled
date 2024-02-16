namespace System.Reflection.Emit;

internal sealed class GenericMethodInfo
{
	internal RuntimeMethodHandle m_methodHandle;

	internal RuntimeTypeHandle m_context;

	internal GenericMethodInfo(RuntimeMethodHandle methodHandle, RuntimeTypeHandle context)
	{
		m_methodHandle = methodHandle;
		m_context = context;
	}
}
