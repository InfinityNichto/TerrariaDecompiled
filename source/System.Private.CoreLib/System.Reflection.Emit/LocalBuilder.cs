namespace System.Reflection.Emit;

public sealed class LocalBuilder : LocalVariableInfo
{
	private int m_localIndex;

	private Type m_localType;

	private MethodInfo m_methodBuilder;

	private bool m_isPinned;

	public override bool IsPinned => m_isPinned;

	public override Type LocalType => m_localType;

	public override int LocalIndex => m_localIndex;

	internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder)
		: this(localIndex, localType, methodBuilder, isPinned: false)
	{
	}

	internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
	{
		m_isPinned = isPinned;
		m_localIndex = localIndex;
		m_localType = localType;
		m_methodBuilder = methodBuilder;
	}

	internal int GetLocalIndex()
	{
		return m_localIndex;
	}

	internal MethodInfo GetMethodBuilder()
	{
		return m_methodBuilder;
	}
}
