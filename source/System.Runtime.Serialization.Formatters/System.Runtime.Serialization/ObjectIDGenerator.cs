using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

public class ObjectIDGenerator
{
	internal int _currentCount;

	private int _currentSize;

	private long[] _ids;

	private object[] _objs;

	public ObjectIDGenerator()
	{
		_currentCount = 1;
		_currentSize = 3;
		_ids = new long[_currentSize * 4];
		_objs = new object[_currentSize * 4];
	}

	private int FindElement(object obj, out bool found)
	{
		int num = RuntimeHelpers.GetHashCode(obj);
		int num2 = 1 + (num & 0x7FFFFFFF) % (_currentSize - 2);
		while (true)
		{
			int num3 = (num & 0x7FFFFFFF) % _currentSize * 4;
			for (int i = num3; i < num3 + 4; i++)
			{
				if (_objs[i] == null)
				{
					found = false;
					return i;
				}
				if (_objs[i] == obj)
				{
					found = true;
					return i;
				}
			}
			num += num2;
		}
	}

	public virtual long GetId(object obj, out bool firstTime)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		bool found;
		int num = FindElement(obj, out found);
		long result;
		if (!found)
		{
			_objs[num] = obj;
			_ids[num] = _currentCount++;
			result = _ids[num];
			if (_currentCount > _currentSize * 4 / 2)
			{
				Rehash();
			}
		}
		else
		{
			result = _ids[num];
		}
		firstTime = !found;
		return result;
	}

	public virtual long HasId(object obj, out bool firstTime)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		bool found;
		int num = FindElement(obj, out found);
		if (found)
		{
			firstTime = false;
			return _ids[num];
		}
		firstTime = true;
		return 0L;
	}

	private void Rehash()
	{
		int currentSize = _currentSize;
		int num = System.Collections.HashHelpers.ExpandPrime(currentSize);
		if (num == currentSize)
		{
			throw new SerializationException(System.SR.Serialization_TooManyElements);
		}
		_currentSize = num;
		long[] ids = new long[_currentSize * 4];
		object[] objs = new object[_currentSize * 4];
		long[] ids2 = _ids;
		object[] objs2 = _objs;
		_ids = ids;
		_objs = objs;
		for (int i = 0; i < objs2.Length; i++)
		{
			if (objs2[i] != null)
			{
				bool found;
				int num2 = FindElement(objs2[i], out found);
				_objs[num2] = objs2[i];
				_ids[num2] = ids2[i];
			}
		}
	}
}
