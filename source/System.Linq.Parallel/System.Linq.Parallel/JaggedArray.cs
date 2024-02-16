namespace System.Linq.Parallel;

internal static class JaggedArray<TElement>
{
	public static TElement[][] Allocate(int size1, int size2)
	{
		TElement[][] array = new TElement[size1][];
		for (int i = 0; i < size1; i++)
		{
			array[i] = new TElement[size2];
		}
		return array;
	}
}
