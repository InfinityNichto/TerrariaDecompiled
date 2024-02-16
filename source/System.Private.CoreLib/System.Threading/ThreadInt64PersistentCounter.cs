using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Threading;

internal sealed class ThreadInt64PersistentCounter
{
	private sealed class ThreadLocalNode
	{
		private uint _count;

		private readonly ThreadInt64PersistentCounter _counter;

		public uint Count => _count;

		public ThreadLocalNode(ThreadInt64PersistentCounter counter)
		{
			_counter = counter;
		}

		public void Dispose()
		{
			ThreadInt64PersistentCounter counter = _counter;
			s_lock.Acquire();
			try
			{
				counter._overflowCount += _count;
				counter._nodes.Remove(this);
			}
			finally
			{
				s_lock.Release();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Increment()
		{
			uint num = _count + 1;
			if (num != 0)
			{
				_count = num;
			}
			else
			{
				OnIncrementOverflow();
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void OnIncrementOverflow()
		{
			ThreadInt64PersistentCounter counter = _counter;
			s_lock.Acquire();
			try
			{
				_count = 0u;
				counter._overflowCount += 4294967296L;
			}
			finally
			{
				s_lock.Release();
			}
		}
	}

	private sealed class ThreadLocalNodeFinalizationHelper
	{
		private readonly ThreadLocalNode _node;

		public ThreadLocalNodeFinalizationHelper(ThreadLocalNode node)
		{
			_node = node;
		}

		~ThreadLocalNodeFinalizationHelper()
		{
			_node.Dispose();
		}
	}

	private static readonly LowLevelLock s_lock = new LowLevelLock();

	[ThreadStatic]
	private static List<ThreadLocalNodeFinalizationHelper> t_nodeFinalizationHelpers;

	private long _overflowCount;

	private HashSet<ThreadLocalNode> _nodes = new HashSet<ThreadLocalNode>();

	public long Count
	{
		get
		{
			s_lock.Acquire();
			long num = _overflowCount;
			try
			{
				foreach (ThreadLocalNode node in _nodes)
				{
					num += node.Count;
				}
			}
			catch (OutOfMemoryException)
			{
			}
			finally
			{
				s_lock.Release();
			}
			return num;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Increment(object threadLocalCountObject)
	{
		Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Increment();
	}

	public object CreateThreadLocalCountObject()
	{
		ThreadLocalNode threadLocalNode = new ThreadLocalNode(this);
		List<ThreadLocalNodeFinalizationHelper> list = t_nodeFinalizationHelpers ?? (t_nodeFinalizationHelpers = new List<ThreadLocalNodeFinalizationHelper>(1));
		list.Add(new ThreadLocalNodeFinalizationHelper(threadLocalNode));
		s_lock.Acquire();
		try
		{
			_nodes.Add(threadLocalNode);
			return threadLocalNode;
		}
		finally
		{
			s_lock.Release();
		}
	}
}
