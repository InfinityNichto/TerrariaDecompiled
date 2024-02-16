namespace System.StubHelpers;

internal abstract class CleanupWorkListElement
{
	private CleanupWorkListElement m_Next;

	protected abstract void DestroyCore();

	public void Destroy()
	{
		DestroyCore();
		for (CleanupWorkListElement next = m_Next; next != null; next = next.m_Next)
		{
			next.DestroyCore();
		}
	}

	public static void AddToCleanupList(ref CleanupWorkListElement list, CleanupWorkListElement newElement)
	{
		if (list == null)
		{
			list = newElement;
			return;
		}
		newElement.m_Next = list;
		list = newElement;
	}
}
