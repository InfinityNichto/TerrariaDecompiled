using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Collections;

internal static class HashHelpers
{
	private static readonly int[] s_primes = new int[72]
	{
		3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
		89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
		631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
		4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
		25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363,
		156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
		968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
		5999471, 7199369
	};

	private static ConditionalWeakTable<object, SerializationInfo> s_serializationInfoTable;

	public static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
	{
		get
		{
			if (s_serializationInfoTable == null)
			{
				Interlocked.CompareExchange(ref s_serializationInfoTable, new ConditionalWeakTable<object, SerializationInfo>(), null);
			}
			return s_serializationInfoTable;
		}
	}

	public static bool IsPrime(int candidate)
	{
		if (((uint)candidate & (true ? 1u : 0u)) != 0)
		{
			int num = (int)Math.Sqrt(candidate);
			for (int i = 3; i <= num; i += 2)
			{
				if (candidate % i == 0)
				{
					return false;
				}
			}
			return true;
		}
		return candidate == 2;
	}

	public static int GetPrime(int min)
	{
		if (min < 0)
		{
			throw new ArgumentException(SR.Arg_HTCapacityOverflow);
		}
		int[] array = s_primes;
		foreach (int num in array)
		{
			if (num >= min)
			{
				return num;
			}
		}
		for (int j = min | 1; j < int.MaxValue; j += 2)
		{
			if (IsPrime(j) && (j - 1) % 101 != 0)
			{
				return j;
			}
		}
		return min;
	}

	public static int ExpandPrime(int oldSize)
	{
		int num = 2 * oldSize;
		if ((uint)num > 2147483587u && 2147483587 > oldSize)
		{
			return 2147483587;
		}
		return GetPrime(num);
	}

	public static ulong GetFastModMultiplier(uint divisor)
	{
		return ulong.MaxValue / (ulong)divisor + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FastMod(uint value, uint divisor, ulong multiplier)
	{
		return (uint)(((multiplier * value >> 32) + 1) * divisor >> 32);
	}
}
