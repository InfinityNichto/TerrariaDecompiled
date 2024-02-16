namespace System.Reflection;

public sealed class NullabilityInfo
{
	public Type Type { get; }

	public NullabilityState ReadState { get; internal set; }

	public NullabilityState WriteState { get; internal set; }

	public NullabilityInfo? ElementType { get; }

	public NullabilityInfo[] GenericTypeArguments { get; }

	internal NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState, NullabilityInfo elementType, NullabilityInfo[] typeArguments)
	{
		Type = type;
		ReadState = readState;
		WriteState = writeState;
		ElementType = elementType;
		GenericTypeArguments = typeArguments;
	}
}
