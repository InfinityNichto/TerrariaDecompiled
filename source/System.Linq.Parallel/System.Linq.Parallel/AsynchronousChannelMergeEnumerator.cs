namespace System.Linq.Parallel;

internal sealed class AsynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
{
	private readonly AsynchronousChannel<T>[] _channels;

	private IntValueEvent _consumerEvent;

	private readonly bool[] _done;

	private int _channelIndex;

	private T _currentElement;

	public override T Current
	{
		get
		{
			if (_channelIndex == -1 || _channelIndex == _channels.Length)
			{
				throw new InvalidOperationException(System.SR.PLINQ_CommonEnumerator_Current_NotStarted);
			}
			return _currentElement;
		}
	}

	internal AsynchronousChannelMergeEnumerator(QueryTaskGroupState taskGroupState, AsynchronousChannel<T>[] channels, IntValueEvent consumerEvent)
		: base(taskGroupState)
	{
		_channels = channels;
		_channelIndex = -1;
		_done = new bool[_channels.Length];
		_consumerEvent = consumerEvent;
	}

	public override bool MoveNext()
	{
		int num = _channelIndex;
		if (num == -1)
		{
			num = (_channelIndex = 0);
		}
		if (num == _channels.Length)
		{
			return false;
		}
		if (!_done[num] && _channels[num].TryDequeue(ref _currentElement))
		{
			_channelIndex = (num + 1) % _channels.Length;
			return true;
		}
		return MoveNextSlowPath();
	}

	private bool MoveNextSlowPath()
	{
		int num = 0;
		int num2 = _channelIndex;
		int channelIndex;
		while ((channelIndex = _channelIndex) != _channels.Length)
		{
			AsynchronousChannel<T> asynchronousChannel = _channels[channelIndex];
			bool flag = _done[channelIndex];
			if (!flag && asynchronousChannel.TryDequeue(ref _currentElement))
			{
				_channelIndex = (channelIndex + 1) % _channels.Length;
				return true;
			}
			if (!flag && asynchronousChannel.IsDone)
			{
				if (!asynchronousChannel.IsChunkBufferEmpty)
				{
					bool flag2 = asynchronousChannel.TryDequeue(ref _currentElement);
					return true;
				}
				_done[channelIndex] = true;
				flag = true;
				asynchronousChannel.Dispose();
			}
			if (flag && ++num == _channels.Length)
			{
				channelIndex = (_channelIndex = _channels.Length);
				break;
			}
			channelIndex = (_channelIndex = (channelIndex + 1) % _channels.Length);
			if (channelIndex != num2)
			{
				continue;
			}
			try
			{
				num = 0;
				for (int i = 0; i < _channels.Length; i++)
				{
					bool isDone = false;
					if (!_done[i] && _channels[i].TryDequeue(ref _currentElement, ref isDone))
					{
						return true;
					}
					if (isDone)
					{
						if (!_done[i])
						{
							_done[i] = true;
						}
						if (++num == _channels.Length)
						{
							channelIndex = (_channelIndex = _channels.Length);
							break;
						}
					}
				}
				if (channelIndex == _channels.Length)
				{
					break;
				}
				_consumerEvent.Wait();
				channelIndex = (_channelIndex = _consumerEvent.Value);
				_consumerEvent.Reset();
				num2 = channelIndex;
				num = 0;
				continue;
			}
			finally
			{
				for (int j = 0; j < _channels.Length; j++)
				{
					if (!_done[j])
					{
						_channels[j].DoneWithDequeueWait();
					}
				}
			}
		}
		_taskGroupState.QueryEnd(userInitiatedDispose: false);
		return false;
	}

	public override void Dispose()
	{
		if (_consumerEvent != null)
		{
			base.Dispose();
			_consumerEvent.Dispose();
			_consumerEvent = null;
		}
	}
}
