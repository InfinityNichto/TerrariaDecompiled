using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509ChainElementCollection : ICollection, IEnumerable, IEnumerable<X509ChainElement>
{
	private readonly X509ChainElement[] _elements;

	public int Count => _elements.Length;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public X509ChainElement this[int index]
	{
		get
		{
			if (index < 0)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumNotStarted);
			}
			if (index >= _elements.Length)
			{
				throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
			}
			return _elements[index];
		}
	}

	internal X509ChainElementCollection()
	{
		_elements = Array.Empty<X509ChainElement>();
	}

	internal X509ChainElementCollection(X509ChainElement[] chainElements)
	{
		_elements = chainElements;
	}

	public void CopyTo(X509ChainElement[] array, int index)
	{
		((ICollection)this).CopyTo((Array)array, index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported);
		}
		if (index < 0 || index >= array.Length)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		if (index + Count > array.Length)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		for (int i = 0; i < Count; i++)
		{
			array.SetValue(this[i], index);
			index++;
		}
	}

	public X509ChainElementEnumerator GetEnumerator()
	{
		return new X509ChainElementEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new X509ChainElementEnumerator(this);
	}

	IEnumerator<X509ChainElement> IEnumerable<X509ChainElement>.GetEnumerator()
	{
		return GetEnumerator();
	}
}
