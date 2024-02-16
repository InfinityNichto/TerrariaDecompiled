using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TaskCanceledException : OperationCanceledException
{
	[NonSerialized]
	private readonly Task _canceledTask;

	public Task? Task => _canceledTask;

	public TaskCanceledException()
		: base(SR.TaskCanceledException_ctor_DefaultMessage)
	{
	}

	public TaskCanceledException(string? message)
		: base(message)
	{
	}

	public TaskCanceledException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public TaskCanceledException(string? message, Exception? innerException, CancellationToken token)
		: base(message, innerException, token)
	{
	}

	public TaskCanceledException(Task? task)
		: base(SR.TaskCanceledException_ctor_DefaultMessage, task?.CancellationToken ?? CancellationToken.None)
	{
		_canceledTask = task;
	}

	protected TaskCanceledException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
