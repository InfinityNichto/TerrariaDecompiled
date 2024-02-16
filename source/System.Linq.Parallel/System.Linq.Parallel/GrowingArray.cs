namespace System.Linq.Parallel;

internal sealed class GrowingArray<T>
{
	private T[] _array;

	private int _count;

	internal T[] InternalArray => _array;

	internal GrowingArray()
	{
		_array = new T[1024];
		_count = 0;
	}

	internal void Add(T element)
	{
		if (_count >= _array.Length)
		{
			GrowArray(2 * _array.Length);
		}
		_array[_count++] = element;
	}

	private void GrowArray(int newSize)
	{
		T[] array = new T[newSize];
		_array.CopyTo(array, 0);
		_array = array;
	}

	internal void CopyFrom(T[] otherArray, int otherCount)
	{
		if (_count + otherCount > _array.Length)
		{
			GrowArray(_count + otherCount);
		}
		Array.Copy(otherArray, 0, _array, _count, otherCount);
		_count += otherCount;
	}
}
