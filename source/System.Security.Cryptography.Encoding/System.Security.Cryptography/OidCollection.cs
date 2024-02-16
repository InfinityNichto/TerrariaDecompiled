using System.Collections;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public sealed class OidCollection : ICollection, IEnumerable
{
	private Oid[] _oids = Array.Empty<Oid>();

	private int _count;

	public Oid this[int index]
	{
		get
		{
			if ((uint)index >= (uint)_count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return _oids[index];
		}
	}

	public Oid? this[string oid]
	{
		get
		{
			string text = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
			if (text == null)
			{
				text = oid;
			}
			for (int i = 0; i < _count; i++)
			{
				Oid oid2 = _oids[i];
				if (oid2.Value == text)
				{
					return oid2;
				}
			}
			return null;
		}
	}

	public int Count => _count;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public int Add(Oid oid)
	{
		int count = _count;
		if (count == _oids.Length)
		{
			Array.Resize(ref _oids, (count == 0) ? 4 : (count * 2));
		}
		_oids[count] = oid;
		_count = count + 1;
		return count;
	}

	public OidEnumerator GetEnumerator()
	{
		return new OidEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
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

	public void CopyTo(Oid[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0 || index >= array.Length)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		Array.Copy(_oids, 0, array, index, _count);
	}
}
