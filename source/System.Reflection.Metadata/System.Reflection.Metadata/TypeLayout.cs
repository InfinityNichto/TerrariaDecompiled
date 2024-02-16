namespace System.Reflection.Metadata;

public readonly struct TypeLayout
{
	private readonly int _size;

	private readonly int _packingSize;

	public int Size => _size;

	public int PackingSize => _packingSize;

	public bool IsDefault
	{
		get
		{
			if (_size == 0)
			{
				return _packingSize == 0;
			}
			return false;
		}
	}

	public TypeLayout(int size, int packingSize)
	{
		_size = size;
		_packingSize = packingSize;
	}
}
