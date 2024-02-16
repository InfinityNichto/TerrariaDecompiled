namespace System.Net.Http.HPack;

internal sealed class DynamicTable
{
	private HeaderField[] _buffer;

	private int _maxSize;

	private int _size;

	private int _count;

	private int _insertIndex;

	private int _removeIndex;

	public ref readonly HeaderField this[int index]
	{
		get
		{
			if (index >= _count)
			{
				throw new IndexOutOfRangeException();
			}
			index = _insertIndex - index - 1;
			if (index < 0)
			{
				index += _buffer.Length;
			}
			return ref _buffer[index];
		}
	}

	public DynamicTable(int maxSize)
	{
		_buffer = new HeaderField[maxSize / 32];
		_maxSize = maxSize;
	}

	public void Insert(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
	{
		int length = HeaderField.GetLength(name.Length, value.Length);
		EnsureAvailable(length);
		if (length <= _maxSize)
		{
			HeaderField headerField = new HeaderField(name, value);
			_buffer[_insertIndex] = headerField;
			_insertIndex = (_insertIndex + 1) % _buffer.Length;
			_size += headerField.Length;
			_count++;
		}
	}

	public void Resize(int maxSize)
	{
		if (maxSize > _maxSize)
		{
			HeaderField[] array = new HeaderField[maxSize / 32];
			int num = Math.Min(_buffer.Length - _removeIndex, _count);
			int length = _count - num;
			Array.Copy(_buffer, _removeIndex, array, 0, num);
			Array.Copy(_buffer, 0, array, num, length);
			_buffer = array;
			_removeIndex = 0;
			_insertIndex = _count;
			_maxSize = maxSize;
		}
		else
		{
			_maxSize = maxSize;
			EnsureAvailable(0);
		}
	}

	private void EnsureAvailable(int available)
	{
		while (_count > 0 && _maxSize - _size < available)
		{
			ref HeaderField reference = ref _buffer[_removeIndex];
			_size -= reference.Length;
			reference = default(HeaderField);
			_count--;
			_removeIndex = (_removeIndex + 1) % _buffer.Length;
		}
	}
}
