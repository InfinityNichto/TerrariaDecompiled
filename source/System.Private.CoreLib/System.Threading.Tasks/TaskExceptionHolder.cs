using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks;

internal sealed class TaskExceptionHolder
{
	private readonly Task m_task;

	private volatile List<ExceptionDispatchInfo> m_faultExceptions;

	private ExceptionDispatchInfo m_cancellationException;

	private volatile bool m_isHandled;

	internal bool ContainsFaultList => m_faultExceptions != null;

	internal TaskExceptionHolder(Task task)
	{
		m_task = task;
	}

	~TaskExceptionHolder()
	{
		if (m_faultExceptions != null && !m_isHandled)
		{
			AggregateException exception = new AggregateException(SR.TaskExceptionHolder_UnhandledException, m_faultExceptions);
			UnobservedTaskExceptionEventArgs ueea = new UnobservedTaskExceptionEventArgs(exception);
			TaskScheduler.PublishUnobservedTaskException(m_task, ueea);
		}
	}

	internal void Add(object exceptionObject, bool representsCancellation)
	{
		if (representsCancellation)
		{
			SetCancellationException(exceptionObject);
		}
		else
		{
			AddFaultException(exceptionObject);
		}
	}

	private void SetCancellationException(object exceptionObject)
	{
		if (exceptionObject is OperationCanceledException source)
		{
			m_cancellationException = ExceptionDispatchInfo.Capture(source);
		}
		else
		{
			ExceptionDispatchInfo cancellationException = exceptionObject as ExceptionDispatchInfo;
			m_cancellationException = cancellationException;
		}
		MarkAsHandled(calledFromFinalizer: false);
	}

	private void AddFaultException(object exceptionObject)
	{
		List<ExceptionDispatchInfo> list = m_faultExceptions;
		if (list == null)
		{
			list = (m_faultExceptions = new List<ExceptionDispatchInfo>(1));
		}
		if (exceptionObject is Exception source)
		{
			list.Add(ExceptionDispatchInfo.Capture(source));
		}
		else if (exceptionObject is ExceptionDispatchInfo item)
		{
			list.Add(item);
		}
		else if (exceptionObject is IEnumerable<Exception> enumerable)
		{
			foreach (Exception item2 in enumerable)
			{
				list.Add(ExceptionDispatchInfo.Capture(item2));
			}
		}
		else
		{
			if (!(exceptionObject is IEnumerable<ExceptionDispatchInfo> collection))
			{
				throw new ArgumentException(SR.TaskExceptionHolder_UnknownExceptionType, "exceptionObject");
			}
			list.AddRange(collection);
		}
		if (list.Count > 0)
		{
			MarkAsUnhandled();
		}
	}

	private void MarkAsUnhandled()
	{
		if (m_isHandled)
		{
			GC.ReRegisterForFinalize(this);
			m_isHandled = false;
		}
	}

	internal void MarkAsHandled(bool calledFromFinalizer)
	{
		if (!m_isHandled)
		{
			if (!calledFromFinalizer)
			{
				GC.SuppressFinalize(this);
			}
			m_isHandled = true;
		}
	}

	internal AggregateException CreateExceptionObject(bool calledFromFinalizer, Exception includeThisException)
	{
		List<ExceptionDispatchInfo> faultExceptions = m_faultExceptions;
		MarkAsHandled(calledFromFinalizer);
		if (includeThisException == null)
		{
			return new AggregateException(faultExceptions);
		}
		Exception[] array = new Exception[faultExceptions.Count + 1];
		for (int i = 0; i < array.Length - 1; i++)
		{
			array[i] = faultExceptions[i].SourceException;
		}
		array[^1] = includeThisException;
		return new AggregateException(array);
	}

	internal List<ExceptionDispatchInfo> GetExceptionDispatchInfos()
	{
		List<ExceptionDispatchInfo> faultExceptions = m_faultExceptions;
		MarkAsHandled(calledFromFinalizer: false);
		return faultExceptions;
	}

	internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
	{
		return m_cancellationException;
	}
}
