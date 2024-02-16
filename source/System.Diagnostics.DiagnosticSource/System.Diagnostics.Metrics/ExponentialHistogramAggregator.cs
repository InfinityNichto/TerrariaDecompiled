using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal sealed class ExponentialHistogramAggregator : Aggregator
{
	private struct Bucket
	{
		public double Value;

		public int Count;

		public Bucket(double value, int count)
		{
			Value = value;
			Count = count;
		}
	}

	private readonly QuantileAggregation _config;

	private int[][] _counters;

	private int _count;

	private readonly int _mantissaMax;

	private readonly int _mantissaMask;

	private readonly int _mantissaShift;

	public ExponentialHistogramAggregator(QuantileAggregation config)
	{
		_config = config;
		_counters = new int[4096][];
		if (_config.MaxRelativeError < 0.0001)
		{
			throw new ArgumentException();
		}
		int num = (int)Math.Ceiling(Math.Log(1.0 / _config.MaxRelativeError, 2.0)) - 1;
		_mantissaShift = 52 - num;
		_mantissaMax = 1 << num;
		_mantissaMask = _mantissaMax - 1;
	}

	public override IAggregationStatistics Collect()
	{
		int[][] counters;
		int count;
		lock (this)
		{
			counters = _counters;
			count = _count;
			_counters = new int[4096][];
			_count = 0;
		}
		QuantileValue[] array = new QuantileValue[_config.Quantiles.Length];
		int num = 0;
		if (num == _config.Quantiles.Length)
		{
			return new HistogramStatistics(array);
		}
		count -= GetInvalidCount(counters);
		int num2 = QuantileToRank(_config.Quantiles[num], count);
		int num3 = 0;
		foreach (Bucket item in IterateBuckets(counters))
		{
			num3 += item.Count;
			while (num3 > num2)
			{
				array[num] = new QuantileValue(_config.Quantiles[num], item.Value);
				num++;
				if (num == _config.Quantiles.Length)
				{
					return new HistogramStatistics(array);
				}
				num2 = QuantileToRank(_config.Quantiles[num], count);
			}
		}
		return new HistogramStatistics(Array.Empty<QuantileValue>());
	}

	private int GetInvalidCount(int[][] counters)
	{
		int[] array = counters[2047];
		int[] array2 = counters[4095];
		int num = 0;
		if (array != null)
		{
			int[] array3 = array;
			foreach (int num2 in array3)
			{
				num += num2;
			}
		}
		if (array2 != null)
		{
			int[] array4 = array2;
			foreach (int num3 in array4)
			{
				num += num3;
			}
		}
		return num;
	}

	private IEnumerable<Bucket> IterateBuckets(int[][] counters)
	{
		for (int exponent2 = 4094; exponent2 >= 2048; exponent2--)
		{
			int[] mantissaCounts2 = counters[exponent2];
			if (mantissaCounts2 != null)
			{
				for (int mantissa2 = _mantissaMax - 1; mantissa2 >= 0; mantissa2--)
				{
					int num = mantissaCounts2[mantissa2];
					if (num > 0)
					{
						yield return new Bucket(GetBucketCanonicalValue(exponent2, mantissa2), num);
					}
				}
			}
		}
		for (int exponent2 = 0; exponent2 < 2047; exponent2++)
		{
			int[] mantissaCounts2 = counters[exponent2];
			if (mantissaCounts2 == null)
			{
				continue;
			}
			for (int mantissa2 = 0; mantissa2 < _mantissaMax; mantissa2++)
			{
				int num2 = mantissaCounts2[mantissa2];
				if (num2 > 0)
				{
					yield return new Bucket(GetBucketCanonicalValue(exponent2, mantissa2), num2);
				}
			}
		}
	}

	public override void Update(double measurement)
	{
		lock (this)
		{
			ulong num = (ulong)BitConverter.DoubleToInt64Bits(measurement);
			int num2 = (int)(num >> 52);
			int num3 = (int)(num >> _mantissaShift) & _mantissaMask;
			ref int[] reference = ref _counters[num2];
			if (reference == null)
			{
				reference = new int[_mantissaMax];
			}
			reference[num3]++;
			_count++;
		}
	}

	private int QuantileToRank(double quantile, int count)
	{
		return Math.Min(Math.Max(0, (int)(quantile * (double)count)), count - 1);
	}

	private double GetBucketCanonicalValue(int exponent, int mantissa)
	{
		long value = ((long)exponent << 52) | ((long)mantissa << _mantissaShift);
		return BitConverter.Int64BitsToDouble(value);
	}
}
