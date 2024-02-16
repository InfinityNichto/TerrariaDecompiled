namespace System.Runtime.Serialization;

public sealed class SafeSerializationEventArgs : EventArgs
{
	public StreamingContext StreamingContext { get; }

	public void AddSerializedState(ISafeSerializationData serializedState)
	{
	}
}
