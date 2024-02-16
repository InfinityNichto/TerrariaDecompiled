namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class SizedArray : ICloneable
{
	internal object[] _objects;

	internal object[] _negObjects;

	internal object this[int index]
	{
		get
		{
			if (index < 0)
			{
				if (-index <= _negObjects.Length - 1)
				{
					return _negObjects[-index];
				}
				return null;
			}
			if (index <= _objects.Length - 1)
			{
				return _objects[index];
			}
			return null;
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

	internal SizedArray()
	{
		_objects = new object[16];
		_negObjects = new object[4];
	}

	internal SizedArray(int length)
	{
		_objects = new object[length];
		_negObjects = new object[length];
	}

	private SizedArray(SizedArray sizedArray)
	{
		_objects = new object[sizedArray._objects.Length];
		sizedArray._objects.CopyTo(_objects, 0);
		_negObjects = new object[sizedArray._negObjects.Length];
		sizedArray._negObjects.CopyTo(_negObjects, 0);
	}

	public object Clone()
	{
		return new SizedArray(this);
	}

	internal void IncreaseCapacity(int index)
	{
		try
		{
			if (index < 0)
			{
				int num = Math.Max(_negObjects.Length * 2, -index + 1);
				object[] array = new object[num];
				Array.Copy(_negObjects, array, _negObjects.Length);
				_negObjects = array;
			}
			else
			{
				int num2 = Math.Max(_objects.Length * 2, index + 1);
				object[] array2 = new object[num2];
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
