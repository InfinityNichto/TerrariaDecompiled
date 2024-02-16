namespace System.Reflection.Emit;

internal readonly struct ExceptionHandler : IEquatable<ExceptionHandler>
{
	internal readonly int m_exceptionClass;

	internal readonly int m_tryStartOffset;

	internal readonly int m_tryEndOffset;

	internal readonly int m_filterOffset;

	internal readonly int m_handlerStartOffset;

	internal readonly int m_handlerEndOffset;

	internal readonly ExceptionHandlingClauseOptions m_kind;

	internal ExceptionHandler(int tryStartOffset, int tryEndOffset, int filterOffset, int handlerStartOffset, int handlerEndOffset, int kind, int exceptionTypeToken)
	{
		m_tryStartOffset = tryStartOffset;
		m_tryEndOffset = tryEndOffset;
		m_filterOffset = filterOffset;
		m_handlerStartOffset = handlerStartOffset;
		m_handlerEndOffset = handlerEndOffset;
		m_kind = (ExceptionHandlingClauseOptions)kind;
		m_exceptionClass = exceptionTypeToken;
	}

	public override int GetHashCode()
	{
		return m_exceptionClass ^ m_tryStartOffset ^ m_tryEndOffset ^ m_filterOffset ^ m_handlerStartOffset ^ m_handlerEndOffset ^ (int)m_kind;
	}

	public override bool Equals(object obj)
	{
		if (obj is ExceptionHandler)
		{
			return Equals((ExceptionHandler)obj);
		}
		return false;
	}

	public bool Equals(ExceptionHandler other)
	{
		if (other.m_exceptionClass == m_exceptionClass && other.m_tryStartOffset == m_tryStartOffset && other.m_tryEndOffset == m_tryEndOffset && other.m_filterOffset == m_filterOffset && other.m_handlerStartOffset == m_handlerStartOffset && other.m_handlerEndOffset == m_handlerEndOffset)
		{
			return other.m_kind == m_kind;
		}
		return false;
	}
}
