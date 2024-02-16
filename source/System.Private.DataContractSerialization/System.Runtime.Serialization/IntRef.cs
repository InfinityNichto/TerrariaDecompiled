namespace System.Runtime.Serialization;

internal sealed class IntRef
{
	private readonly int _value;

	public int Value => _value;

	public IntRef(int value)
	{
		_value = value;
	}
}
