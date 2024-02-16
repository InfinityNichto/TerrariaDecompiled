using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Buffers;

internal sealed class TlsOverPerCoreLockedStacksArrayPool<T> : ArrayPool<T>
{
	private sealed class PerCoreLockedStacks
	{
		private static readonly int s_lockedStackCount = Math.Min(Environment.ProcessorCount, 64);

		private readonly LockedStack[] _perCoreStacks;

		public PerCoreLockedStacks()
		{
			LockedStack[] array = new LockedStack[s_lockedStackCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new LockedStack();
			}
			_perCoreStacks = array;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryPush(T[] array)
		{
			LockedStack[] perCoreStacks = _perCoreStacks;
			int num = (int)((uint)Thread.GetCurrentProcessorId() % (uint)s_lockedStackCount);
			for (int i = 0; i < perCoreStacks.Length; i++)
			{
				if (perCoreStacks[num].TryPush(array))
				{
					return true;
				}
				if (++num == perCoreStacks.Length)
				{
					num = 0;
				}
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] TryPop()
		{
			LockedStack[] perCoreStacks = _perCoreStacks;
			int num = (int)((uint)Thread.GetCurrentProcessorId() % (uint)s_lockedStackCount);
			for (int i = 0; i < perCoreStacks.Length; i++)
			{
				T[] result;
				if ((result = perCoreStacks[num].TryPop()) != null)
				{
					return result;
				}
				if (++num == perCoreStacks.Length)
				{
					num = 0;
				}
			}
			return null;
		}

		public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
		{
			LockedStack[] perCoreStacks = _perCoreStacks;
			for (int i = 0; i < perCoreStacks.Length; i++)
			{
				perCoreStacks[i].Trim(currentMilliseconds, id, pressure, bucketSize);
			}
		}
	}

	private sealed class LockedStack
	{
		private readonly T[][] _arrays = new T[8][];

		private int _count;

		private int _millisecondsTimestamp;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryPush(T[] array)
		{
			bool result = false;
			Monitor.Enter(this);
			T[][] arrays = _arrays;
			int count = _count;
			if ((uint)count < (uint)arrays.Length)
			{
				if (count == 0)
				{
					_millisecondsTimestamp = 0;
				}
				arrays[count] = array;
				_count = count + 1;
				result = true;
			}
			Monitor.Exit(this);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] TryPop()
		{
			T[] result = null;
			Monitor.Enter(this);
			T[][] arrays = _arrays;
			int num = _count - 1;
			if ((uint)num < (uint)arrays.Length)
			{
				result = arrays[num];
				arrays[num] = null;
				_count = num;
			}
			Monitor.Exit(this);
			return result;
		}

		public void Trim(int currentMilliseconds, int id, Utilities.MemoryPressure pressure, int bucketSize)
		{
			if (_count == 0)
			{
				return;
			}
			int num = ((pressure == Utilities.MemoryPressure.High) ? 10000 : 60000);
			lock (this)
			{
				if (_count == 0)
				{
					return;
				}
				if (_millisecondsTimestamp == 0)
				{
					_millisecondsTimestamp = currentMilliseconds;
				}
				else
				{
					if (currentMilliseconds - _millisecondsTimestamp <= num)
					{
						return;
					}
					ArrayPoolEventSource log = ArrayPoolEventSource.Log;
					int num2 = 1;
					switch (pressure)
					{
					case Utilities.MemoryPressure.High:
						num2 = 8;
						if (bucketSize > 16384)
						{
							num2++;
						}
						if (Unsafe.SizeOf<T>() > 16)
						{
							num2++;
						}
						if (Unsafe.SizeOf<T>() > 32)
						{
							num2++;
						}
						break;
					case Utilities.MemoryPressure.Medium:
						num2 = 2;
						break;
					}
					while (_count > 0 && num2-- > 0)
					{
						T[] array = _arrays[--_count];
						_arrays[_count] = null;
						if (log.IsEnabled())
						{
							log.BufferTrimmed(array.GetHashCode(), array.Length, id);
						}
					}
					_millisecondsTimestamp = ((_count > 0) ? (_millisecondsTimestamp + num / 4) : 0);
				}
			}
		}
	}

	private struct ThreadLocalArray
	{
		public T[] Array;

		public int MillisecondsTimeStamp;

		public ThreadLocalArray(T[] array)
		{
			Array = array;
			MillisecondsTimeStamp = 0;
		}
	}

	[ThreadStatic]
	private static ThreadLocalArray[] t_tlsBuckets;

	private readonly ConditionalWeakTable<ThreadLocalArray[], object> _allTlsBuckets = new ConditionalWeakTable<ThreadLocalArray[], object>();

	private readonly PerCoreLockedStacks[] _buckets = new PerCoreLockedStacks[27];

	private int _trimCallbackCreated;

	private int Id => GetHashCode();

	private PerCoreLockedStacks CreatePerCoreLockedStacks(int bucketIndex)
	{
		PerCoreLockedStacks perCoreLockedStacks = new PerCoreLockedStacks();
		return Interlocked.CompareExchange(ref _buckets[bucketIndex], perCoreLockedStacks, null) ?? perCoreLockedStacks;
	}

	public override T[] Rent(int minimumLength)
	{
		ArrayPoolEventSource log = ArrayPoolEventSource.Log;
		int num = Utilities.SelectBucketIndex(minimumLength);
		ThreadLocalArray[] array = t_tlsBuckets;
		T[] array2;
		if (array != null && (uint)num < (uint)array.Length)
		{
			array2 = array[num].Array;
			if (array2 != null)
			{
				array[num].Array = null;
				if (log.IsEnabled())
				{
					log.BufferRented(array2.GetHashCode(), array2.Length, Id, num);
				}
				return array2;
			}
		}
		PerCoreLockedStacks[] buckets = _buckets;
		if ((uint)num < (uint)buckets.Length)
		{
			PerCoreLockedStacks perCoreLockedStacks = buckets[num];
			if (perCoreLockedStacks != null)
			{
				array2 = perCoreLockedStacks.TryPop();
				if (array2 != null)
				{
					if (log.IsEnabled())
					{
						log.BufferRented(array2.GetHashCode(), array2.Length, Id, num);
					}
					return array2;
				}
			}
			minimumLength = Utilities.GetMaxSizeForBucket(num);
		}
		else
		{
			if (minimumLength == 0)
			{
				return Array.Empty<T>();
			}
			if (minimumLength < 0)
			{
				throw new ArgumentOutOfRangeException("minimumLength");
			}
		}
		array2 = GC.AllocateUninitializedArray<T>(minimumLength);
		if (log.IsEnabled())
		{
			int hashCode = array2.GetHashCode();
			log.BufferRented(hashCode, array2.Length, Id, -1);
			log.BufferAllocated(hashCode, array2.Length, Id, -1, (num >= _buckets.Length) ? ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize : ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
		}
		return array2;
	}

	public override void Return(T[] array, bool clearArray = false)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int num = Utilities.SelectBucketIndex(array.Length);
		ThreadLocalArray[] array2 = t_tlsBuckets ?? InitializeTlsBucketsAndTrimming();
		bool flag = false;
		bool flag2 = true;
		if ((uint)num < (uint)array2.Length)
		{
			flag = true;
			if (clearArray)
			{
				Array.Clear(array);
			}
			if (array.Length != Utilities.GetMaxSizeForBucket(num))
			{
				throw new ArgumentException(SR.ArgumentException_BufferNotFromPool, "array");
			}
			ref ThreadLocalArray reference = ref array2[num];
			T[] array3 = reference.Array;
			reference = new ThreadLocalArray(array);
			if (array3 != null)
			{
				PerCoreLockedStacks perCoreLockedStacks = _buckets[num] ?? CreatePerCoreLockedStacks(num);
				flag2 = perCoreLockedStacks.TryPush(array3);
			}
		}
		ArrayPoolEventSource log = ArrayPoolEventSource.Log;
		if (log.IsEnabled() && array.Length != 0)
		{
			log.BufferReturned(array.GetHashCode(), array.Length, Id);
			if (!(flag && flag2))
			{
				log.BufferDropped(array.GetHashCode(), array.Length, Id, flag ? num : (-1), (!flag) ? ArrayPoolEventSource.BufferDroppedReason.OverMaximumSize : ArrayPoolEventSource.BufferDroppedReason.Full);
			}
		}
	}

	public bool Trim()
	{
		int tickCount = Environment.TickCount;
		Utilities.MemoryPressure memoryPressure = Utilities.GetMemoryPressure();
		ArrayPoolEventSource log = ArrayPoolEventSource.Log;
		if (log.IsEnabled())
		{
			log.BufferTrimPoll(tickCount, (int)memoryPressure);
		}
		PerCoreLockedStacks[] buckets = _buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i]?.Trim(tickCount, Id, memoryPressure, Utilities.GetMaxSizeForBucket(i));
		}
		if (memoryPressure == Utilities.MemoryPressure.High)
		{
			if (log.IsEnabled())
			{
				foreach (KeyValuePair<ThreadLocalArray[], object> item in (IEnumerable<KeyValuePair<ThreadLocalArray[], object>>)_allTlsBuckets)
				{
					ThreadLocalArray[] key = item.Key;
					for (int j = 0; j < key.Length; j++)
					{
						T[] array = Interlocked.Exchange(ref key[j].Array, null);
						if (array != null)
						{
							log.BufferTrimmed(array.GetHashCode(), array.Length, Id);
						}
					}
				}
			}
			else
			{
				foreach (KeyValuePair<ThreadLocalArray[], object> item2 in (IEnumerable<KeyValuePair<ThreadLocalArray[], object>>)_allTlsBuckets)
				{
					Array.Clear(item2.Key);
				}
			}
		}
		else
		{
			uint num = ((memoryPressure != Utilities.MemoryPressure.Medium) ? 30000u : 15000u);
			uint num2 = num;
			foreach (KeyValuePair<ThreadLocalArray[], object> item3 in (IEnumerable<KeyValuePair<ThreadLocalArray[], object>>)_allTlsBuckets)
			{
				ThreadLocalArray[] key2 = item3.Key;
				for (int k = 0; k < key2.Length; k++)
				{
					if (key2[k].Array == null)
					{
						continue;
					}
					int millisecondsTimeStamp = key2[k].MillisecondsTimeStamp;
					if (millisecondsTimeStamp == 0)
					{
						key2[k].MillisecondsTimeStamp = tickCount;
					}
					else if (tickCount - millisecondsTimeStamp >= num2)
					{
						T[] array2 = Interlocked.Exchange(ref key2[k].Array, null);
						if (array2 != null && log.IsEnabled())
						{
							log.BufferTrimmed(array2.GetHashCode(), array2.Length, Id);
						}
					}
				}
			}
		}
		return true;
	}

	private ThreadLocalArray[] InitializeTlsBucketsAndTrimming()
	{
		ThreadLocalArray[] array = (t_tlsBuckets = new ThreadLocalArray[27]);
		_allTlsBuckets.Add(array, null);
		if (Interlocked.Exchange(ref _trimCallbackCreated, 1) == 0)
		{
			Gen2GcCallback.Register((object s) => ((TlsOverPerCoreLockedStacksArrayPool<T>)s).Trim(), this);
		}
		return array;
	}
}
