namespace System.Threading;

public readonly struct AsyncLocalValueChangedArgs<T>
{
	public T? PreviousValue { get; }

	public T? CurrentValue { get; }

	public bool ThreadContextChanged { get; }

	internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged)
	{
		PreviousValue = previousValue;
		CurrentValue = currentValue;
		ThreadContextChanged = contextChanged;
	}
}
