namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class SerStack
{
	internal object[] _objects = new object[5];

	internal string _stackId;

	internal int _top = -1;

	internal SerStack(string stackId)
	{
		_stackId = stackId;
	}

	internal void Push(object obj)
	{
		if (_top == _objects.Length - 1)
		{
			IncreaseCapacity();
		}
		_objects[++_top] = obj;
	}

	internal object Pop()
	{
		if (_top < 0)
		{
			return null;
		}
		object result = _objects[_top];
		_objects[_top--] = null;
		return result;
	}

	internal void IncreaseCapacity()
	{
		int num = _objects.Length * 2;
		object[] array = new object[num];
		Array.Copy(_objects, array, _objects.Length);
		_objects = array;
	}

	internal object Peek()
	{
		if (_top >= 0)
		{
			return _objects[_top];
		}
		return null;
	}

	internal object PeekPeek()
	{
		if (_top >= 1)
		{
			return _objects[_top - 1];
		}
		return null;
	}

	internal bool IsEmpty()
	{
		return _top <= 0;
	}
}
