using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal struct ObjectReferenceStack
{
	private const int MaximumArraySize = 16;

	private const int InitialArraySize = 4;

	private int _count;

	private object[] _objectArray;

	private bool[] _isReferenceArray;

	private Dictionary<object, object> _objectDictionary;

	internal void Push(object obj)
	{
		if (_objectArray == null)
		{
			_objectArray = new object[4];
			_objectArray[_count++] = obj;
			return;
		}
		if (_count < 16)
		{
			if (_count == _objectArray.Length)
			{
				Array.Resize(ref _objectArray, _objectArray.Length * 2);
			}
			_objectArray[_count++] = obj;
			return;
		}
		if (_objectDictionary == null)
		{
			_objectDictionary = new Dictionary<object, object>();
		}
		_objectDictionary.Add(obj, null);
		_count++;
	}

	internal void EnsureSetAsIsReference(object obj)
	{
		if (_count == 0)
		{
			return;
		}
		if (_count > 16)
		{
			_ = _objectDictionary;
			_objectDictionary.Remove(obj);
		}
		else if (_objectArray != null && _objectArray[_count - 1] == obj)
		{
			if (_isReferenceArray == null)
			{
				_isReferenceArray = new bool[_objectArray.Length];
			}
			else if (_count >= _isReferenceArray.Length)
			{
				Array.Resize(ref _isReferenceArray, _objectArray.Length);
			}
			_isReferenceArray[_count - 1] = true;
		}
	}

	internal void Pop(object obj)
	{
		if (_count > 16)
		{
			_ = _objectDictionary;
			_objectDictionary.Remove(obj);
		}
		_count--;
	}

	internal bool Contains(object obj)
	{
		int num = _count;
		if (num > 16)
		{
			if (_objectDictionary != null && _objectDictionary.ContainsKey(obj))
			{
				return true;
			}
			num = 16;
		}
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (obj == _objectArray[num2] && _isReferenceArray != null && !_isReferenceArray[num2])
			{
				return true;
			}
		}
		return false;
	}
}
