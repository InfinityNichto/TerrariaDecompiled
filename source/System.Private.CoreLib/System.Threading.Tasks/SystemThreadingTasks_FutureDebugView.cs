namespace System.Threading.Tasks;

internal sealed class SystemThreadingTasks_FutureDebugView<TResult>
{
	private readonly Task<TResult> m_task;

	public TResult Result
	{
		get
		{
			if (m_task.Status != TaskStatus.RanToCompletion)
			{
				return default(TResult);
			}
			return m_task.Result;
		}
	}

	public object AsyncState => m_task.AsyncState;

	public TaskCreationOptions CreationOptions => m_task.CreationOptions;

	public Exception Exception => m_task.Exception;

	public int Id => m_task.Id;

	public bool CancellationPending
	{
		get
		{
			if (m_task.Status == TaskStatus.WaitingToRun)
			{
				return m_task.CancellationToken.IsCancellationRequested;
			}
			return false;
		}
	}

	public TaskStatus Status => m_task.Status;

	public SystemThreadingTasks_FutureDebugView(Task<TResult> task)
	{
		m_task = task;
	}
}
