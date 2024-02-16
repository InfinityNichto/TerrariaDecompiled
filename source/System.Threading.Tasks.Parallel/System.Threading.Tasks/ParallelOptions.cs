namespace System.Threading.Tasks;

public class ParallelOptions
{
	private TaskScheduler _scheduler;

	private int _maxDegreeOfParallelism;

	private CancellationToken _cancellationToken;

	public TaskScheduler? TaskScheduler
	{
		get
		{
			return _scheduler;
		}
		set
		{
			_scheduler = value;
		}
	}

	internal TaskScheduler EffectiveTaskScheduler => _scheduler ?? System.Threading.Tasks.TaskScheduler.Current;

	public int MaxDegreeOfParallelism
	{
		get
		{
			return _maxDegreeOfParallelism;
		}
		set
		{
			if (value == 0 || value < -1)
			{
				throw new ArgumentOutOfRangeException("MaxDegreeOfParallelism");
			}
			_maxDegreeOfParallelism = value;
		}
	}

	public CancellationToken CancellationToken
	{
		get
		{
			return _cancellationToken;
		}
		set
		{
			_cancellationToken = value;
		}
	}

	internal int EffectiveMaxConcurrencyLevel
	{
		get
		{
			int num = MaxDegreeOfParallelism;
			int maximumConcurrencyLevel = EffectiveTaskScheduler.MaximumConcurrencyLevel;
			if (maximumConcurrencyLevel > 0 && maximumConcurrencyLevel != int.MaxValue)
			{
				num = ((num == -1) ? maximumConcurrencyLevel : Math.Min(maximumConcurrencyLevel, num));
			}
			return num;
		}
	}

	public ParallelOptions()
	{
		_scheduler = System.Threading.Tasks.TaskScheduler.Default;
		_maxDegreeOfParallelism = -1;
		_cancellationToken = CancellationToken.None;
	}
}
