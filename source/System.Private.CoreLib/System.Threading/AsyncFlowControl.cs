using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public struct AsyncFlowControl : IEquatable<AsyncFlowControl>, IDisposable
{
	private Thread _thread;

	internal void Initialize(Thread currentThread)
	{
		_thread = currentThread;
	}

	public void Undo()
	{
		if (_thread == null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotUseAFCMultiple);
		}
		if (Thread.CurrentThread != _thread)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotUseAFCOtherThread);
		}
		if (!ExecutionContext.IsFlowSuppressed())
		{
			throw new InvalidOperationException(SR.InvalidOperation_AsyncFlowCtrlCtxMismatch);
		}
		_thread = null;
		ExecutionContext.RestoreFlow();
	}

	public void Dispose()
	{
		Undo();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is AsyncFlowControl obj2)
		{
			return Equals(obj2);
		}
		return false;
	}

	public bool Equals(AsyncFlowControl obj)
	{
		return _thread == obj._thread;
	}

	public override int GetHashCode()
	{
		return _thread?.GetHashCode() ?? 0;
	}

	public static bool operator ==(AsyncFlowControl a, AsyncFlowControl b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(AsyncFlowControl a, AsyncFlowControl b)
	{
		return !(a == b);
	}
}
