namespace System.Reflection.Internal;

internal static class EmptyArray<T>
{
	internal static readonly T[] Instance = new T[0];
}
