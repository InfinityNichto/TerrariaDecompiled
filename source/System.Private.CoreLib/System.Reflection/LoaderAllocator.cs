namespace System.Reflection;

internal sealed class LoaderAllocator
{
	private LoaderAllocatorScout m_scout;

	private object[] m_slots;

	internal CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> m_methodInstantiations;

	private int m_slotsUsed;

	private LoaderAllocator()
	{
		m_slots = new object[5];
		m_scout = new LoaderAllocatorScout();
	}
}
