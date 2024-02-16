namespace System.Threading.Tasks;

internal sealed class CompletionActionInvoker : IThreadPoolWorkItem
{
	private readonly ITaskCompletionAction m_action;

	private readonly Task m_completingTask;

	internal CompletionActionInvoker(ITaskCompletionAction action, Task completingTask)
	{
		m_action = action;
		m_completingTask = completingTask;
	}

	void IThreadPoolWorkItem.Execute()
	{
		m_action.Invoke(m_completingTask);
	}
}
