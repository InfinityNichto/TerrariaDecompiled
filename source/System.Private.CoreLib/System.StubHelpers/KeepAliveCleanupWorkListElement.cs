namespace System.StubHelpers;

internal sealed class KeepAliveCleanupWorkListElement : CleanupWorkListElement
{
	private object m_obj;

	public KeepAliveCleanupWorkListElement(object obj)
	{
		m_obj = obj;
	}

	protected override void DestroyCore()
	{
		GC.KeepAlive(m_obj);
	}
}
