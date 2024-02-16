namespace System.Reflection.Emit;

internal sealed class VarArgMethod
{
	internal RuntimeMethodInfo m_method;

	internal DynamicMethod m_dynamicMethod;

	internal SignatureHelper m_signature;

	internal VarArgMethod(DynamicMethod dm, SignatureHelper signature)
	{
		m_dynamicMethod = dm;
		m_signature = signature;
	}

	internal VarArgMethod(RuntimeMethodInfo method, SignatureHelper signature)
	{
		m_method = method;
		m_signature = signature;
	}
}
