namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class IntSizedArray : ICloneable
{
	internal int[] _objects = new int[16];

	internal int[] _negObjects = new int[4];

	internal int this[int index]
	{
		get
		{
			if (index < 0)
			{
				if (-index <= _negObjects.Length - 1)
				{
					return _negObjects[-index];
				}
				return 0;
			}
			if (index <= _objects.Length - 1)
			{
				return _objects[index];
			}
			return 0;
		}
		set
		{
			if (index < 0)
			{
				if (-index > _negObjects.Length - 1)
				{
					IncreaseCapacity(index);
				}
				_negObjects[-index] = value;
			}
			else
			{
				if (index > _objects.Length - 1)
				{
					IncreaseCapacity(index);
				}
				_objects[index] = value;
			}
		}
	}

	public IntSizedArray()
	{
	}

	private IntSizedArray(IntSizedArray sizedArray)
	{
		_objects = new int[sizedArray._objects.Length];
		sizedArray._objects.CopyTo(_objects, 0);
		_negObjects = new int[sizedArray._negObjects.Length];
		sizedArray._negObjects.CopyTo(_negObjects, 0);
	}

	public object Clone()
	{
		return new IntSizedArray(this);
	}

	internal void IncreaseCapacity(int index)
	{
		try
		{
			if (index < 0)
			{
				int num = Math.Max(_negObjects.Length * 2, -index + 1);
				int[] array = new int[num];
				Array.Copy(_negObjects, array, _negObjects.Length);
				_negObjects = array;
			}
			else
			{
				int num2 = Math.Max(_objects.Length * 2, index + 1);
				int[] array2 = new int[num2];
				Array.Copy(_objects, array2, _objects.Length);
				_objects = array2;
			}
		}
		catch (Exception)
		{
			throw new SerializationException(System.SR.Serialization_CorruptedStream);
		}
	}
}
