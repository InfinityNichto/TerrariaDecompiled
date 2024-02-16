using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

public readonly struct StreamingContext
{
	private readonly object _additionalContext;

	private readonly StreamingContextStates _state;

	public StreamingContextStates State => _state;

	public object? Context => _additionalContext;

	public StreamingContext(StreamingContextStates state)
		: this(state, null)
	{
	}

	public StreamingContext(StreamingContextStates state, object? additional)
	{
		_state = state;
		_additionalContext = additional;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is StreamingContext streamingContext))
		{
			return false;
		}
		if (streamingContext._additionalContext == _additionalContext)
		{
			return streamingContext._state == _state;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)_state;
	}
}
