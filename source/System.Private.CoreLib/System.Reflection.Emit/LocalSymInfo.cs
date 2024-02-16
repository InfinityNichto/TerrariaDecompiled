namespace System.Reflection.Emit;

internal sealed class LocalSymInfo
{
	internal int m_iLocalSymCount;

	internal string[] m_namespace;

	internal int m_iNameSpaceCount;

	internal LocalSymInfo()
	{
		m_iLocalSymCount = 0;
		m_iNameSpaceCount = 0;
	}

	private void EnsureCapacityNamespace()
	{
		if (m_iNameSpaceCount == 0)
		{
			m_namespace = new string[16];
		}
		else if (m_iNameSpaceCount == m_namespace.Length)
		{
			string[] array = new string[checked(m_iNameSpaceCount * 2)];
			Array.Copy(m_namespace, array, m_iNameSpaceCount);
			m_namespace = array;
		}
	}

	internal void AddUsingNamespace(string strNamespace)
	{
		EnsureCapacityNamespace();
		m_namespace[m_iNameSpaceCount] = strNamespace;
		checked
		{
			m_iNameSpaceCount++;
		}
	}
}
