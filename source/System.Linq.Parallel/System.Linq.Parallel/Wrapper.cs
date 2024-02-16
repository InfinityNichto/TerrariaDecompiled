namespace System.Linq.Parallel;

internal struct Wrapper<T>
{
	internal T Value;

	internal Wrapper(T value)
	{
		Value = value;
	}
}
