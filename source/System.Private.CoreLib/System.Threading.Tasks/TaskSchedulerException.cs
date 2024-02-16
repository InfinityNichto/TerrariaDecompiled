using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TaskSchedulerException : Exception
{
	public TaskSchedulerException()
		: base(SR.TaskSchedulerException_ctor_DefaultMessage)
	{
	}

	public TaskSchedulerException(string? message)
		: base(message)
	{
	}

	public TaskSchedulerException(Exception? innerException)
		: base(SR.TaskSchedulerException_ctor_DefaultMessage, innerException)
	{
	}

	public TaskSchedulerException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected TaskSchedulerException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
