namespace System.Threading;

internal abstract class QueueUserWorkItemCallbackBase : IThreadPoolWorkItem
{
	public virtual void Execute()
	{
	}
}
