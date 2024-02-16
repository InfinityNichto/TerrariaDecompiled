using System.Threading.Tasks;

namespace System.Threading;

internal sealed class TimerHolder
{
	internal readonly TimerQueueTimer _timer;

	public TimerHolder(TimerQueueTimer timer)
	{
		_timer = timer;
	}

	~TimerHolder()
	{
		_timer.Close();
	}

	public void Close()
	{
		_timer.Close();
		GC.SuppressFinalize(this);
	}

	public bool Close(WaitHandle notifyObject)
	{
		bool result = _timer.Close(notifyObject);
		GC.SuppressFinalize(this);
		return result;
	}

	public ValueTask CloseAsync()
	{
		ValueTask result = _timer.CloseAsync();
		GC.SuppressFinalize(this);
		return result;
	}
}
