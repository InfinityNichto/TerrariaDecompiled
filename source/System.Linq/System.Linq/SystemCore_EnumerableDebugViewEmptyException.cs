namespace System.Linq;

internal sealed class SystemCore_EnumerableDebugViewEmptyException : Exception
{
	public string Empty => System.SR.EmptyEnumerable;
}
