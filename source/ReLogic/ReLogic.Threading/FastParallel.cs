using System;
using System.Threading;

namespace ReLogic.Threading;

public static class FastParallel
{
	private class RangeTask
	{
		private readonly ParallelForAction _action;

		private readonly int _fromInclusive;

		private readonly int _toExclusive;

		private readonly object _context;

		private readonly CountdownEvent _countdown;

		public RangeTask(ParallelForAction action, int fromInclusive, int toExclusive, object context, CountdownEvent countdown)
		{
			_action = action;
			_fromInclusive = fromInclusive;
			_toExclusive = toExclusive;
			_context = context;
			_countdown = countdown;
		}

		public void Invoke()
		{
			try
			{
				if (_fromInclusive != _toExclusive)
				{
					_action(_fromInclusive, _toExclusive, _context);
				}
			}
			finally
			{
				_countdown.Signal();
			}
		}
	}

	public static bool ForceTasksOnCallingThread { get; set; }

	static FastParallel()
	{
		ForceTasksOnCallingThread = false;
	}

	public static void For(int fromInclusive, int toExclusive, ParallelForAction callback, object context = null)
	{
		int num = toExclusive - fromInclusive;
		if (num == 0)
		{
			return;
		}
		int num2 = Math.Min(Math.Max(1, Environment.ProcessorCount + 1 - 1 - 1), num);
		if (ForceTasksOnCallingThread)
		{
			num2 = 1;
		}
		int num3 = num / num2;
		int num4 = num % num2;
		CountdownEvent countdownEvent = new CountdownEvent(num2);
		int num5 = toExclusive;
		for (int num6 = num2 - 1; num6 >= 0; num6--)
		{
			int num7 = num3;
			if (num6 < num4)
			{
				num7++;
			}
			num5 -= num7;
			int num8 = num5;
			int toExclusive2 = num8 + num7;
			RangeTask rangeTask = new RangeTask(callback, num8, toExclusive2, context, countdownEvent);
			if (num6 < 1)
			{
				InvokeTask(rangeTask);
			}
			else
			{
				ThreadPool.QueueUserWorkItem(InvokeTask, rangeTask);
			}
		}
		if (countdownEvent.Wait(10000))
		{
			return;
		}
		ThreadPool.GetAvailableThreads(out var workerThreads, out var _);
		throw new Exception($"Fatal Deadlock in FastParallelFor. pending: {ThreadPool.PendingWorkItemCount}. avail: {workerThreads}");
	}

	private static void InvokeTask(object context)
	{
		((RangeTask)context).Invoke();
	}
}
