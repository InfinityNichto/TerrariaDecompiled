using System.Text;

namespace System.Reflection.Internal;

internal sealed class PooledStringBuilder
{
	public readonly StringBuilder Builder = new StringBuilder();

	private readonly ObjectPool<PooledStringBuilder> _pool;

	private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

	public int Length => Builder.Length;

	private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
	{
		_pool = pool;
	}

	public void Free()
	{
		StringBuilder builder = Builder;
		if (builder.Capacity <= 1024)
		{
			builder.Clear();
			_pool.Free(this);
		}
	}

	public string ToStringAndFree()
	{
		string result = Builder.ToString();
		Free();
		return result;
	}

	public static ObjectPool<PooledStringBuilder> CreatePool()
	{
		ObjectPool<PooledStringBuilder> pool = null;
		pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool), 32);
		return pool;
	}

	public static PooledStringBuilder GetInstance()
	{
		return s_poolInstance.Allocate();
	}
}
