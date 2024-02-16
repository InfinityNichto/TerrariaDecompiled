using System.Collections;

namespace System.Diagnostics;

public class ProcessModuleCollection : ReadOnlyCollectionBase
{
	public ProcessModule this[int index] => (ProcessModule)base.InnerList[index];

	protected ProcessModuleCollection()
	{
	}

	public ProcessModuleCollection(ProcessModule[] processModules)
	{
		base.InnerList.AddRange(processModules);
	}

	internal ProcessModuleCollection(int capacity)
	{
		if (capacity > 0)
		{
			base.InnerList.Capacity = capacity;
		}
	}

	internal void Add(ProcessModule module)
	{
		base.InnerList.Add(module);
	}

	public int IndexOf(ProcessModule module)
	{
		return base.InnerList.IndexOf(module);
	}

	public bool Contains(ProcessModule module)
	{
		return base.InnerList.Contains(module);
	}

	public void CopyTo(ProcessModule[] array, int index)
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
				ProcessModule processModule = (ProcessModule)enumerator.Current;
				processModule.Dispose();
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
