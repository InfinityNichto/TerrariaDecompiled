namespace System.Linq.Parallel;

internal sealed class Shared<T>
{
	internal T Value;

	internal Shared(T value)
	{
		Value = value;
	}
}
