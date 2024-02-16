namespace System.Reflection.Emit;

internal sealed class ScopeTree
{
	internal int[] m_iOffsets;

	internal ScopeAction[] m_ScopeActions;

	internal int m_iCount;

	internal int m_iOpenScopeCount;

	internal LocalSymInfo[] m_localSymInfos;

	internal ScopeTree()
	{
		m_iOpenScopeCount = 0;
		m_iCount = 0;
	}

	internal int GetCurrentActiveScopeIndex()
	{
		if (m_iCount == 0)
		{
			return -1;
		}
		int num = m_iCount - 1;
		int num2 = 0;
		while (num2 > 0 || m_ScopeActions[num] == ScopeAction.Close)
		{
			num2 += (int)m_ScopeActions[num];
			num--;
		}
		return num;
	}

	internal void AddUsingNamespaceToCurrentScope(string strNamespace)
	{
		int currentActiveScopeIndex = GetCurrentActiveScopeIndex();
		ref LocalSymInfo reference = ref m_localSymInfos[currentActiveScopeIndex];
		if (reference == null)
		{
			reference = new LocalSymInfo();
		}
		m_localSymInfos[currentActiveScopeIndex].AddUsingNamespace(strNamespace);
	}

	internal void AddScopeInfo(ScopeAction sa, int iOffset)
	{
		if (sa == ScopeAction.Close && m_iOpenScopeCount <= 0)
		{
			throw new ArgumentException(SR.Argument_UnmatchingSymScope);
		}
		EnsureCapacity();
		m_ScopeActions[m_iCount] = sa;
		m_iOffsets[m_iCount] = iOffset;
		m_localSymInfos[m_iCount] = null;
		checked
		{
			m_iCount++;
		}
		m_iOpenScopeCount += 0 - sa;
	}

	internal void EnsureCapacity()
	{
		if (m_iCount == 0)
		{
			m_iOffsets = new int[16];
			m_ScopeActions = new ScopeAction[16];
			m_localSymInfos = new LocalSymInfo[16];
		}
		else if (m_iCount == m_iOffsets.Length)
		{
			int num = checked(m_iCount * 2);
			int[] array = new int[num];
			Array.Copy(m_iOffsets, array, m_iCount);
			m_iOffsets = array;
			ScopeAction[] array2 = new ScopeAction[num];
			Array.Copy(m_ScopeActions, array2, m_iCount);
			m_ScopeActions = array2;
			LocalSymInfo[] array3 = new LocalSymInfo[num];
			Array.Copy(m_localSymInfos, array3, m_iCount);
			m_localSymInfos = array3;
		}
	}
}
