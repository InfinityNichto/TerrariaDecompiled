using System.Threading;

namespace System.Net.Mime;

internal sealed class MultiAsyncResult : System.Net.LazyAsyncResult
{
	private readonly object _context;

	private int _outstanding;

	internal object Context => _context;

	internal MultiAsyncResult(object context, AsyncCallback callback, object state)
		: base(context, state, callback)
	{
		_context = context;
	}

	internal void Enter()
	{
		Increment();
	}

	internal void Leave()
	{
		Decrement();
	}

	internal void Leave(object result)
	{
		base.Result = result;
		Decrement();
	}

	private void Decrement()
	{
		if (Interlocked.Decrement(ref _outstanding) == -1)
		{
			InvokeCallback(base.Result);
		}
	}

	private void Increment()
	{
		Interlocked.Increment(ref _outstanding);
	}

	internal void CompleteSequence()
	{
		Decrement();
	}

	internal static object End(IAsyncResult result)
	{
		MultiAsyncResult multiAsyncResult = (MultiAsyncResult)result;
		multiAsyncResult.InternalWaitForCompletion();
		return multiAsyncResult.Result;
	}
}
