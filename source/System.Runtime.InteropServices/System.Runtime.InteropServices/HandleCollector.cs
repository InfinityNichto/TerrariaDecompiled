using System.Threading;

namespace System.Runtime.InteropServices;

public sealed class HandleCollector
{
	private int _threshold;

	private int _handleCount;

	private readonly int[] _gcCounts = new int[3];

	private int _gcGeneration;

	public int Count => _handleCount;

	public int InitialThreshold { get; }

	public int MaximumThreshold { get; }

	public string Name { get; }

	public HandleCollector(string? name, int initialThreshold)
		: this(name, initialThreshold, int.MaxValue)
	{
	}

	public HandleCollector(string? name, int initialThreshold, int maximumThreshold)
	{
		if (initialThreshold < 0)
		{
			throw new ArgumentOutOfRangeException("initialThreshold", initialThreshold, System.SR.Arg_NeedNonNegNumRequired);
		}
		if (maximumThreshold < 0)
		{
			throw new ArgumentOutOfRangeException("maximumThreshold", maximumThreshold, System.SR.Arg_NeedNonNegNumRequired);
		}
		if (initialThreshold > maximumThreshold)
		{
			throw new ArgumentException(System.SR.Arg_InvalidThreshold, "initialThreshold");
		}
		Name = name ?? string.Empty;
		InitialThreshold = initialThreshold;
		MaximumThreshold = maximumThreshold;
		_threshold = initialThreshold;
		_handleCount = 0;
	}

	public void Add()
	{
		int num = -1;
		Interlocked.Increment(ref _handleCount);
		if (_handleCount < 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_HCCountOverflow);
		}
		if (_handleCount > _threshold)
		{
			lock (this)
			{
				_threshold = _handleCount + _handleCount / 10;
				num = _gcGeneration;
				if (_gcGeneration < 2)
				{
					_gcGeneration++;
				}
			}
		}
		if (num >= 0 && (num == 0 || _gcCounts[num] == GC.CollectionCount(num)))
		{
			GC.Collect(num);
			Thread.Sleep(10 * num);
		}
		for (int i = 1; i < 3; i++)
		{
			_gcCounts[i] = GC.CollectionCount(i);
		}
	}

	public void Remove()
	{
		Interlocked.Decrement(ref _handleCount);
		if (_handleCount < 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_HCCountOverflow);
		}
		int num = _handleCount + _handleCount / 10;
		if (num < _threshold - _threshold / 10)
		{
			lock (this)
			{
				if (num > InitialThreshold)
				{
					_threshold = num;
				}
				else
				{
					_threshold = InitialThreshold;
				}
				_gcGeneration = 0;
			}
		}
		for (int i = 1; i < 3; i++)
		{
			_gcCounts[i] = GC.CollectionCount(i);
		}
	}
}
