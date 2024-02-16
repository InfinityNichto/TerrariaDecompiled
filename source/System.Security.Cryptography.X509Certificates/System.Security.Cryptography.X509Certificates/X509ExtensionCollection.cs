using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509ExtensionCollection : ICollection, IEnumerable, IEnumerable<X509Extension>
{
	private readonly List<X509Extension> _list = new List<X509Extension>();

	public int Count => _list.Count;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public X509Extension this[int index]
	{
		get
		{
			if (index < 0)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumNotStarted);
			}
			if (index >= _list.Count)
			{
				throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
			}
			return _list[index];
		}
	}

	public X509Extension? this[string oid]
	{
		get
		{
			string value = new Oid(oid).Value;
			foreach (X509Extension item in _list)
			{
				if (string.Equals(item.Oid.Value, value, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
			return null;
		}
	}

	public int Add(X509Extension extension)
	{
		if (extension == null)
		{
			throw new ArgumentNullException("extension");
		}
		_list.Add(extension);
		return _list.Count - 1;
	}

	public void CopyTo(X509Extension[] array, int index)
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

	public X509ExtensionEnumerator GetEnumerator()
	{
		return new X509ExtensionEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new X509ExtensionEnumerator(this);
	}

	IEnumerator<X509Extension> IEnumerable<X509Extension>.GetEnumerator()
	{
		return GetEnumerator();
	}
}
