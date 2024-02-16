using System.Collections;

namespace System.Security.AccessControl;

public abstract class GenericAcl : ICollection, IEnumerable
{
	public static readonly byte AclRevision = 2;

	public static readonly byte AclRevisionDS = 4;

	public static readonly int MaxBinaryLength = 65535;

	public abstract byte Revision { get; }

	public abstract int BinaryLength { get; }

	public abstract GenericAce this[int index] { get; set; }

	public abstract int Count { get; }

	public bool IsSynchronized => false;

	public virtual object SyncRoot => this;

	public abstract void GetBinaryForm(byte[] binaryForm, int offset);

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new RankException(System.SR.Rank_MultiDimNotSupported);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentOutOfRangeException("array", System.SR.ArgumentOutOfRange_ArrayTooSmall);
		}
		for (int i = 0; i < Count; i++)
		{
			array.SetValue(this[i], index + i);
		}
	}

	public void CopyTo(GenericAce[] array, int index)
	{
		((ICollection)this).CopyTo((Array)array, index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new AceEnumerator(this);
	}

	public AceEnumerator GetEnumerator()
	{
		return (AceEnumerator)((IEnumerable)this).GetEnumerator();
	}
}
