namespace System.Diagnostics.Tracing;

internal struct EventPipeEventInstanceData
{
	internal IntPtr ProviderID;

	internal uint EventID;

	internal uint ThreadID;

	internal long TimeStamp;

	internal Guid ActivityId;

	internal Guid ChildActivityId;

	internal IntPtr Payload;

	internal uint PayloadLength;
}
