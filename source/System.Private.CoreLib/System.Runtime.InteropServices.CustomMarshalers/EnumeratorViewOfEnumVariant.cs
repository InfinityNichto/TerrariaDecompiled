using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class EnumeratorViewOfEnumVariant : ICustomAdapter, System.Collections.IEnumerator
{
	private readonly IEnumVARIANT _enumVariantObject;

	private bool _fetchedLastObject;

	private readonly object[] _nextArray = new object[1];

	private object _current;

	public object Current => _current;

	public EnumeratorViewOfEnumVariant(IEnumVARIANT enumVariantObject)
	{
		_enumVariantObject = enumVariantObject;
		_fetchedLastObject = false;
		_current = null;
	}

	public unsafe bool MoveNext()
	{
		if (_fetchedLastObject)
		{
			_current = null;
			return false;
		}
		int num = 0;
		if (_enumVariantObject.Next(1, _nextArray, (IntPtr)(&num)) == 1)
		{
			_fetchedLastObject = true;
			if (num == 0)
			{
				_current = null;
				return false;
			}
		}
		_current = _nextArray[0];
		return true;
	}

	public void Reset()
	{
		int num = _enumVariantObject.Reset();
		if (num < 0)
		{
			Marshal.ThrowExceptionForHR(num);
		}
		_fetchedLastObject = false;
		_current = null;
	}

	public object GetUnderlyingObject()
	{
		return _enumVariantObject;
	}
}
