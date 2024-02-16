using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class ResettableCompletionSource<T> : IValueTaskSource<T>, IValueTaskSource
{
	private ManualResetValueTaskSourceCore<T> _valueTaskSource;

	public ResettableCompletionSource()
	{
		_valueTaskSource.RunContinuationsAsynchronously = true;
	}

	public ValueTask<T> GetValueTask()
	{
		return new ValueTask<T>(this, _valueTaskSource.Version);
	}

	public ValueTask GetTypelessValueTask()
	{
		return new ValueTask(this, _valueTaskSource.Version);
	}

	public ValueTaskSourceStatus GetStatus(short token)
	{
		return _valueTaskSource.GetStatus(token);
	}

	public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		_valueTaskSource.OnCompleted(continuation, state, token, flags);
	}

	public void Complete(T result)
	{
		_valueTaskSource.SetResult(result);
	}

	public void CompleteException(Exception ex)
	{
		_valueTaskSource.SetException(ex);
	}

	public T GetResult(short token)
	{
		bool flag = token == _valueTaskSource.Version;
		try
		{
			return _valueTaskSource.GetResult(token);
		}
		finally
		{
			if (flag)
			{
				_valueTaskSource.Reset();
			}
		}
	}

	void IValueTaskSource.GetResult(short token)
	{
		bool flag = token == _valueTaskSource.Version;
		try
		{
			_valueTaskSource.GetResult(token);
		}
		finally
		{
			if (flag)
			{
				_valueTaskSource.Reset();
			}
		}
	}
}
