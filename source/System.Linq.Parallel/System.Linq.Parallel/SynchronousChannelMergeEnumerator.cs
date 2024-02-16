namespace System.Linq.Parallel;

internal sealed class SynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
{
	private readonly SynchronousChannel<T>[] _channels;

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

	internal SynchronousChannelMergeEnumerator(QueryTaskGroupState taskGroupState, SynchronousChannel<T>[] channels)
		: base(taskGroupState)
	{
		_channels = channels;
		_channelIndex = -1;
	}

	public override bool MoveNext()
	{
		if (_channelIndex == -1)
		{
			_channelIndex = 0;
		}
		while (_channelIndex != _channels.Length)
		{
			SynchronousChannel<T> synchronousChannel = _channels[_channelIndex];
			if (synchronousChannel.Count == 0)
			{
				_channelIndex++;
				continue;
			}
			_currentElement = synchronousChannel.Dequeue();
			return true;
		}
		return false;
	}
}
