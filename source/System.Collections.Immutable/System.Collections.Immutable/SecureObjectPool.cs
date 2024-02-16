using System.Threading;

namespace System.Collections.Immutable;

internal static class SecureObjectPool
{
	private static int s_poolUserIdCounter;

	internal const int UnassignedId = -1;

	internal static int NewId()
	{
		int num;
		do
		{
			num = Interlocked.Increment(ref s_poolUserIdCounter);
		}
		while (num == -1);
		return num;
	}
}
internal sealed class SecureObjectPool<T, TCaller> where TCaller : ISecurePooledObjectUser
{
	public void TryAdd(TCaller caller, SecurePooledObject<T> item)
	{
		if (caller.PoolUserId == item.Owner)
		{
			item.Owner = -1;
			AllocFreeConcurrentStack<SecurePooledObject<T>>.TryAdd(item);
		}
	}

	public bool TryTake(TCaller caller, out SecurePooledObject<T>? item)
	{
		if (caller.PoolUserId != -1 && AllocFreeConcurrentStack<SecurePooledObject<T>>.TryTake(out item))
		{
			item.Owner = caller.PoolUserId;
			return true;
		}
		item = null;
		return false;
	}

	public SecurePooledObject<T> PrepNew(TCaller caller, T newValue)
	{
		Requires.NotNullAllowStructs(newValue, "newValue");
		SecurePooledObject<T> securePooledObject = new SecurePooledObject<T>(newValue);
		securePooledObject.Owner = caller.PoolUserId;
		return securePooledObject;
	}
}
