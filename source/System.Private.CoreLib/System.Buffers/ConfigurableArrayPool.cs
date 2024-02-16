using System.Diagnostics;
using System.Threading;

namespace System.Buffers;

internal sealed class ConfigurableArrayPool<T> : ArrayPool<T>
{
	private sealed class Bucket
	{
		internal readonly int _bufferLength;

		private readonly T[][] _buffers;

		private readonly int _poolId;

		private SpinLock _lock;

		private int _index;

		internal int Id => GetHashCode();

		internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
		{
			_lock = new SpinLock(Debugger.IsAttached);
			_buffers = new T[numberOfBuffers][];
			_bufferLength = bufferLength;
			_poolId = poolId;
		}

		internal T[] Rent()
		{
			T[][] buffers = _buffers;
			T[] array = null;
			bool lockTaken = false;
			bool flag = false;
			try
			{
				_lock.Enter(ref lockTaken);
				if (_index < buffers.Length)
				{
					array = buffers[_index];
					buffers[_index++] = null;
					flag = array == null;
				}
			}
			finally
			{
				if (lockTaken)
				{
					_lock.Exit(useMemoryBarrier: false);
				}
			}
			if (flag)
			{
				array = new T[_bufferLength];
				ArrayPoolEventSource log = ArrayPoolEventSource.Log;
				if (log.IsEnabled())
				{
					log.BufferAllocated(array.GetHashCode(), _bufferLength, _poolId, Id, ArrayPoolEventSource.BufferAllocatedReason.Pooled);
				}
			}
			return array;
		}

		internal void Return(T[] array)
		{
			if (array.Length != _bufferLength)
			{
				throw new ArgumentException(SR.ArgumentException_BufferNotFromPool, "array");
			}
			bool lockTaken = false;
			bool flag;
			try
			{
				_lock.Enter(ref lockTaken);
				flag = _index != 0;
				if (flag)
				{
					_buffers[--_index] = array;
				}
			}
			finally
			{
				if (lockTaken)
				{
					_lock.Exit(useMemoryBarrier: false);
				}
			}
			if (!flag)
			{
				ArrayPoolEventSource log = ArrayPoolEventSource.Log;
				if (log.IsEnabled())
				{
					log.BufferDropped(array.GetHashCode(), _bufferLength, _poolId, Id, ArrayPoolEventSource.BufferDroppedReason.Full);
				}
			}
		}
	}

	private readonly Bucket[] _buckets;

	private int Id => GetHashCode();

	internal ConfigurableArrayPool()
		: this(1048576, 50)
	{
	}

	internal ConfigurableArrayPool(int maxArrayLength, int maxArraysPerBucket)
	{
		if (maxArrayLength <= 0)
		{
			throw new ArgumentOutOfRangeException("maxArrayLength");
		}
		if (maxArraysPerBucket <= 0)
		{
			throw new ArgumentOutOfRangeException("maxArraysPerBucket");
		}
		if (maxArrayLength > 1073741824)
		{
			maxArrayLength = 1073741824;
		}
		else if (maxArrayLength < 16)
		{
			maxArrayLength = 16;
		}
		int id = Id;
		int num = Utilities.SelectBucketIndex(maxArrayLength);
		Bucket[] array = new Bucket[num + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, id);
		}
		_buckets = array;
	}

	public override T[] Rent(int minimumLength)
	{
		if (minimumLength < 0)
		{
			throw new ArgumentOutOfRangeException("minimumLength");
		}
		if (minimumLength == 0)
		{
			return Array.Empty<T>();
		}
		ArrayPoolEventSource log = ArrayPoolEventSource.Log;
		int num = Utilities.SelectBucketIndex(minimumLength);
		T[] array;
		if (num < _buckets.Length)
		{
			int num2 = num;
			do
			{
				array = _buckets[num2].Rent();
				if (array != null)
				{
					if (log.IsEnabled())
					{
						log.BufferRented(array.GetHashCode(), array.Length, Id, _buckets[num2].Id);
					}
					return array;
				}
			}
			while (++num2 < _buckets.Length && num2 != num + 2);
			array = new T[_buckets[num]._bufferLength];
		}
		else
		{
			array = new T[minimumLength];
		}
		if (log.IsEnabled())
		{
			int hashCode = array.GetHashCode();
			log.BufferRented(hashCode, array.Length, Id, -1);
			log.BufferAllocated(hashCode, array.Length, Id, -1, (num >= _buckets.Length) ? ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize : ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
		}
		return array;
	}

	public override void Return(T[] array, bool clearArray = false)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Length == 0)
		{
			return;
		}
		int num = Utilities.SelectBucketIndex(array.Length);
		bool flag = num < _buckets.Length;
		if (flag)
		{
			if (clearArray)
			{
				Array.Clear(array);
			}
			_buckets[num].Return(array);
		}
		ArrayPoolEventSource log = ArrayPoolEventSource.Log;
		if (log.IsEnabled())
		{
			int hashCode = array.GetHashCode();
			log.BufferReturned(hashCode, array.Length, Id);
			if (!flag)
			{
				log.BufferDropped(hashCode, array.Length, Id, -1, ArrayPoolEventSource.BufferDroppedReason.Full);
			}
		}
	}
}
