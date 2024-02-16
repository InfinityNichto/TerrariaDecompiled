using System.Threading;

namespace System.ComponentModel;

public static class AsyncOperationManager
{
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static SynchronizationContext SynchronizationContext
	{
		get
		{
			if (System.Threading.SynchronizationContext.Current == null)
			{
				System.Threading.SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			}
			return System.Threading.SynchronizationContext.Current;
		}
		set
		{
			System.Threading.SynchronizationContext.SetSynchronizationContext(value);
		}
	}

	public static AsyncOperation CreateOperation(object? userSuppliedState)
	{
		return AsyncOperation.CreateOperation(userSuppliedState, SynchronizationContext);
	}
}
