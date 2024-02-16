using System.Collections.Concurrent;

namespace System.Threading.Tasks;

internal sealed class TaskReplicator
{
	public delegate void ReplicatableUserAction<TState>(ref TState replicaState, int timeout, out bool yieldedBeforeCompletion);

	private abstract class Replica
	{
		protected readonly TaskReplicator _replicator;

		protected readonly int _timeout;

		protected int _remainingConcurrency;

		protected volatile Task _pendingTask;

		protected Replica(TaskReplicator replicator, int maxConcurrency, int timeout)
		{
			_replicator = replicator;
			_timeout = timeout;
			_remainingConcurrency = maxConcurrency - 1;
			_pendingTask = new Task(delegate(object s)
			{
				((Replica)s).Execute();
			}, this);
			_replicator._pendingReplicas.Enqueue(this);
		}

		public void Start()
		{
			_pendingTask.RunSynchronously(_replicator._scheduler);
		}

		public void Wait()
		{
			Task pendingTask;
			while ((pendingTask = _pendingTask) != null)
			{
				pendingTask.Wait();
			}
		}

		public void Execute()
		{
			try
			{
				if (!_replicator._stopReplicating && _remainingConcurrency > 0)
				{
					CreateNewReplica();
					_remainingConcurrency = 0;
				}
				ExecuteAction(out var yieldedBeforeCompletion);
				if (yieldedBeforeCompletion)
				{
					_pendingTask = new Task(delegate(object s)
					{
						((Replica)s).Execute();
					}, this, CancellationToken.None, TaskCreationOptions.None);
					_pendingTask.Start(_replicator._scheduler);
				}
				else
				{
					_replicator._stopReplicating = true;
					_pendingTask = null;
				}
			}
			catch (Exception item)
			{
				LazyInitializer.EnsureInitialized(ref _replicator._exceptions).Enqueue(item);
				if (_replicator._stopOnFirstFailure)
				{
					_replicator._stopReplicating = true;
				}
				_pendingTask = null;
			}
		}

		protected abstract void CreateNewReplica();

		protected abstract void ExecuteAction(out bool yieldedBeforeCompletion);
	}

	private sealed class Replica<TState> : Replica
	{
		private readonly ReplicatableUserAction<TState> _action;

		private TState _state;

		public Replica(TaskReplicator replicator, int maxConcurrency, int timeout, ReplicatableUserAction<TState> action)
			: base(replicator, maxConcurrency, timeout)
		{
			_action = action;
		}

		protected override void CreateNewReplica()
		{
			Replica<TState> replica = new Replica<TState>(_replicator, _remainingConcurrency, GenerateCooperativeMultitaskingTaskTimeout(), _action);
			replica._pendingTask.Start(_replicator._scheduler);
		}

		protected override void ExecuteAction(out bool yieldedBeforeCompletion)
		{
			_action(ref _state, _timeout, out yieldedBeforeCompletion);
		}
	}

	private readonly TaskScheduler _scheduler;

	private readonly bool _stopOnFirstFailure;

	private readonly ConcurrentQueue<Replica> _pendingReplicas = new ConcurrentQueue<Replica>();

	private ConcurrentQueue<Exception> _exceptions;

	private bool _stopReplicating;

	private TaskReplicator(ParallelOptions options, bool stopOnFirstFailure)
	{
		_scheduler = options.TaskScheduler ?? TaskScheduler.Current;
		_stopOnFirstFailure = stopOnFirstFailure;
	}

	public static void Run<TState>(ReplicatableUserAction<TState> action, ParallelOptions options, bool stopOnFirstFailure)
	{
		if (OperatingSystem.IsBrowser())
		{
			int timeout = 2147483646;
			TState replicaState = default(TState);
			action(ref replicaState, timeout, out var yieldedBeforeCompletion);
			if (yieldedBeforeCompletion)
			{
				throw new Exception("Replicated tasks cannot yield in this single-threaded browser environment");
			}
			return;
		}
		int maxConcurrency = ((options.EffectiveMaxConcurrencyLevel > 0) ? options.EffectiveMaxConcurrencyLevel : int.MaxValue);
		TaskReplicator taskReplicator = new TaskReplicator(options, stopOnFirstFailure);
		new Replica<TState>(taskReplicator, maxConcurrency, 1073741823, action).Start();
		Replica result;
		while (taskReplicator._pendingReplicas.TryDequeue(out result))
		{
			result.Wait();
		}
		if (taskReplicator._exceptions != null)
		{
			throw new AggregateException(taskReplicator._exceptions);
		}
	}

	private static int GenerateCooperativeMultitaskingTaskTimeout()
	{
		int processorCount = Environment.ProcessorCount;
		int tickCount = Environment.TickCount;
		return 100 + tickCount % processorCount * 50;
	}
}
