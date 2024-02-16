namespace System.Threading;

internal struct DeferredDisposableLifetime<T> where T : class, IDeferredDisposable
{
	private int _count;

	public bool AddRef()
	{
		int num;
		int value;
		do
		{
			num = Volatile.Read(ref _count);
			if (num < 0)
			{
				throw new ObjectDisposedException(typeof(T).ToString());
			}
			value = checked(num + 1);
		}
		while (Interlocked.CompareExchange(ref _count, value, num) != num);
		return true;
	}

	public void Release(T obj)
	{
		int num3;
		while (true)
		{
			int num = Volatile.Read(ref _count);
			if (num > 0)
			{
				int num2 = num - 1;
				if (Interlocked.CompareExchange(ref _count, num2, num) == num)
				{
					if (num2 == 0)
					{
						obj.OnFinalRelease(disposed: false);
					}
					return;
				}
			}
			else
			{
				num3 = num + 1;
				if (Interlocked.CompareExchange(ref _count, num3, num) == num)
				{
					break;
				}
			}
		}
		if (num3 == -1)
		{
			obj.OnFinalRelease(disposed: true);
		}
	}

	public void Dispose(T obj)
	{
		int num;
		int num2;
		do
		{
			num = Volatile.Read(ref _count);
			if (num < 0)
			{
				return;
			}
			num2 = -1 - num;
		}
		while (Interlocked.CompareExchange(ref _count, num2, num) != num);
		if (num2 == -1)
		{
			obj.OnFinalRelease(disposed: true);
		}
	}
}
