namespace System.Runtime.Serialization;

public readonly struct DeserializationToken : IDisposable
{
	private readonly DeserializationTracker _tracker;

	internal DeserializationToken(DeserializationTracker tracker)
	{
		_tracker = tracker;
	}

	public void Dispose()
	{
		if (_tracker == null || !_tracker.DeserializationInProgress)
		{
			return;
		}
		lock (_tracker)
		{
			if (_tracker.DeserializationInProgress)
			{
				_tracker.DeserializationInProgress = false;
				SerializationInfo.AsyncDeserializationInProgress.Value = false;
			}
		}
	}
}
