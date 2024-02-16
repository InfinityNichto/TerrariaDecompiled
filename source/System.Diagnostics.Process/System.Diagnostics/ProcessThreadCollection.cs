using System.Collections;

namespace System.Diagnostics;

public class ProcessThreadCollection : ReadOnlyCollectionBase
{
	public ProcessThread this[int index] => (ProcessThread)base.InnerList[index];

	protected ProcessThreadCollection()
	{
	}

	public ProcessThreadCollection(ProcessThread[] processThreads)
	{
		base.InnerList.AddRange(processThreads);
	}

	public int Add(ProcessThread thread)
	{
		return ((IList)base.InnerList).Add((object?)thread);
	}

	public void Insert(int index, ProcessThread thread)
	{
		base.InnerList.Insert(index, thread);
	}

	public int IndexOf(ProcessThread thread)
	{
		return base.InnerList.IndexOf(thread);
	}

	public bool Contains(ProcessThread thread)
	{
		return base.InnerList.Contains(thread);
	}

	public void Remove(ProcessThread thread)
	{
		base.InnerList.Remove(thread);
	}

	public void CopyTo(ProcessThread[] array, int index)
	{
		base.InnerList.CopyTo(array, index);
	}

	internal void Dispose()
	{
		IEnumerator enumerator = GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ProcessThread processThread = (ProcessThread)enumerator.Current;
				processThread.Dispose();
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}
}
