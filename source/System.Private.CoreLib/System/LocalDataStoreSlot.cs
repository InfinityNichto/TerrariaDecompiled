using System.Threading;

namespace System;

public sealed class LocalDataStoreSlot
{
	internal ThreadLocal<object?> Data { get; private set; }

	internal LocalDataStoreSlot(ThreadLocal<object> data)
	{
		Data = data;
		GC.SuppressFinalize(this);
	}

	~LocalDataStoreSlot()
	{
	}
}
