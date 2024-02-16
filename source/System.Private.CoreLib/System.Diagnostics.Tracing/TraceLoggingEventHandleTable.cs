using System.Threading;

namespace System.Diagnostics.Tracing;

internal sealed class TraceLoggingEventHandleTable
{
	private IntPtr[] m_innerTable;

	internal IntPtr this[int eventID]
	{
		get
		{
			IntPtr result = IntPtr.Zero;
			IntPtr[] array = Volatile.Read(ref m_innerTable);
			if (eventID >= 0 && eventID < array.Length)
			{
				result = array[eventID];
			}
			return result;
		}
	}

	internal TraceLoggingEventHandleTable()
	{
		m_innerTable = new IntPtr[10];
	}

	internal void SetEventHandle(int eventID, IntPtr eventHandle)
	{
		if (eventID >= m_innerTable.Length)
		{
			int num = m_innerTable.Length * 2;
			if (num <= eventID)
			{
				num = eventID + 1;
			}
			IntPtr[] array = new IntPtr[num];
			Array.Copy(m_innerTable, array, m_innerTable.Length);
			Volatile.Write(ref m_innerTable, array);
		}
		m_innerTable[eventID] = eventHandle;
	}
}
