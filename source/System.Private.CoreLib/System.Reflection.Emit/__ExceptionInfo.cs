namespace System.Reflection.Emit;

internal sealed class __ExceptionInfo
{
	internal int m_startAddr;

	internal int[] m_filterAddr;

	internal int[] m_catchAddr;

	internal int[] m_catchEndAddr;

	internal int[] m_type;

	internal Type[] m_catchClass;

	internal Label m_endLabel;

	internal Label m_finallyEndLabel;

	internal int m_endAddr;

	internal int m_endFinally;

	internal int m_currentCatch;

	private int m_currentState;

	internal __ExceptionInfo(int startAddr, Label endLabel)
	{
		m_startAddr = startAddr;
		m_endAddr = -1;
		m_filterAddr = new int[4];
		m_catchAddr = new int[4];
		m_catchEndAddr = new int[4];
		m_catchClass = new Type[4];
		m_currentCatch = 0;
		m_endLabel = endLabel;
		m_type = new int[4];
		m_endFinally = -1;
		m_currentState = 0;
	}

	private void MarkHelper(int catchorfilterAddr, int catchEndAddr, Type catchClass, int type)
	{
		int currentCatch = m_currentCatch;
		if (currentCatch >= m_catchAddr.Length)
		{
			m_filterAddr = ILGenerator.EnlargeArray(m_filterAddr);
			m_catchAddr = ILGenerator.EnlargeArray(m_catchAddr);
			m_catchEndAddr = ILGenerator.EnlargeArray(m_catchEndAddr);
			m_catchClass = ILGenerator.EnlargeArray(m_catchClass);
			m_type = ILGenerator.EnlargeArray(m_type);
		}
		if (type == 1)
		{
			m_type[currentCatch] = type;
			m_filterAddr[currentCatch] = catchorfilterAddr;
			m_catchAddr[currentCatch] = -1;
			if (currentCatch > 0)
			{
				m_catchEndAddr[currentCatch - 1] = catchorfilterAddr;
			}
		}
		else
		{
			m_catchClass[currentCatch] = catchClass;
			if (m_type[currentCatch] != 1)
			{
				m_type[currentCatch] = type;
			}
			m_catchAddr[currentCatch] = catchorfilterAddr;
			if (currentCatch > 0 && m_type[currentCatch] != 1)
			{
				m_catchEndAddr[currentCatch - 1] = catchEndAddr;
			}
			m_catchEndAddr[currentCatch] = -1;
			m_currentCatch++;
		}
		if (m_endAddr == -1)
		{
			m_endAddr = catchorfilterAddr;
		}
	}

	internal void MarkFilterAddr(int filterAddr)
	{
		m_currentState = 1;
		MarkHelper(filterAddr, filterAddr, null, 1);
	}

	internal void MarkFaultAddr(int faultAddr)
	{
		m_currentState = 4;
		MarkHelper(faultAddr, faultAddr, null, 4);
	}

	internal void MarkCatchAddr(int catchAddr, Type catchException)
	{
		m_currentState = 2;
		MarkHelper(catchAddr, catchAddr, catchException, 0);
	}

	internal void MarkFinallyAddr(int finallyAddr, int endCatchAddr)
	{
		if (m_endFinally != -1)
		{
			throw new ArgumentException(SR.Argument_TooManyFinallyClause);
		}
		m_currentState = 3;
		m_endFinally = finallyAddr;
		MarkHelper(finallyAddr, endCatchAddr, null, 2);
	}

	internal void Done(int endAddr)
	{
		m_catchEndAddr[m_currentCatch - 1] = endAddr;
		m_currentState = 5;
	}

	internal int GetStartAddress()
	{
		return m_startAddr;
	}

	internal int GetEndAddress()
	{
		return m_endAddr;
	}

	internal int GetFinallyEndAddress()
	{
		return m_endFinally;
	}

	internal Label GetEndLabel()
	{
		return m_endLabel;
	}

	internal int[] GetFilterAddresses()
	{
		return m_filterAddr;
	}

	internal int[] GetCatchAddresses()
	{
		return m_catchAddr;
	}

	internal int[] GetCatchEndAddresses()
	{
		return m_catchEndAddr;
	}

	internal Type[] GetCatchClass()
	{
		return m_catchClass;
	}

	internal int GetNumberOfCatches()
	{
		return m_currentCatch;
	}

	internal int[] GetExceptionTypes()
	{
		return m_type;
	}

	internal void SetFinallyEndLabel(Label lbl)
	{
		m_finallyEndLabel = lbl;
	}

	internal bool IsInner(__ExceptionInfo exc)
	{
		int num = exc.m_currentCatch - 1;
		int num2 = m_currentCatch - 1;
		if (exc.m_catchEndAddr[num] < m_catchEndAddr[num2])
		{
			return true;
		}
		if (exc.m_catchEndAddr[num] != m_catchEndAddr[num2])
		{
			return false;
		}
		return exc.GetEndAddress() > GetEndAddress();
	}

	internal int GetCurrentState()
	{
		return m_currentState;
	}
}
