using System.Collections;

namespace System.ComponentModel.Design;

public class DesignerCollection : ICollection, IEnumerable
{
	private readonly IList _designers;

	public int Count => _designers.Count;

	public virtual IDesignerHost? this[int index] => (IDesignerHost)_designers[index];

	int ICollection.Count => Count;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => null;

	public DesignerCollection(IDesignerHost[]? designers)
	{
		if (designers != null)
		{
			_designers = new ArrayList(designers);
		}
		else
		{
			_designers = new ArrayList();
		}
	}

	public DesignerCollection(IList? designers)
	{
		_designers = designers ?? new ArrayList();
	}

	public IEnumerator GetEnumerator()
	{
		return _designers.GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		_designers.CopyTo(array, index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
