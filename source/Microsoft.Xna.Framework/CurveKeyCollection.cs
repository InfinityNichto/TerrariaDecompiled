using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class CurveKeyCollection : ICollection<CurveKey>, IEnumerable<CurveKey>, IEnumerable
{
	private List<CurveKey> Keys = new List<CurveKey>();

	internal float TimeRange;

	internal float InvTimeRange;

	internal bool IsCacheAvailable = true;

	public CurveKey this[int index]
	{
		get
		{
			return Keys[index];
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			float position = Keys[index].Position;
			if (position == value.Position)
			{
				Keys[index] = value;
				return;
			}
			Keys.RemoveAt(index);
			Add(value);
		}
	}

	public int Count => Keys.Count;

	public bool IsReadOnly => false;

	public int IndexOf(CurveKey item)
	{
		return Keys.IndexOf(item);
	}

	public void RemoveAt(int index)
	{
		Keys.RemoveAt(index);
		IsCacheAvailable = false;
	}

	public void Add(CurveKey item)
	{
		if (item == null)
		{
			throw new ArgumentNullException();
		}
		int i = Keys.BinarySearch(item);
		if (i >= 0)
		{
			for (; i < Keys.Count && item.Position == Keys[i].Position; i++)
			{
			}
		}
		else
		{
			i = ~i;
		}
		Keys.Insert(i, item);
		IsCacheAvailable = false;
	}

	public void Clear()
	{
		Keys.Clear();
		TimeRange = (InvTimeRange = 0f);
		IsCacheAvailable = false;
	}

	public bool Contains(CurveKey item)
	{
		return Keys.Contains(item);
	}

	public void CopyTo(CurveKey[] array, int arrayIndex)
	{
		Keys.CopyTo(array, arrayIndex);
		IsCacheAvailable = false;
	}

	public bool Remove(CurveKey item)
	{
		IsCacheAvailable = false;
		return Keys.Remove(item);
	}

	public IEnumerator<CurveKey> GetEnumerator()
	{
		return Keys.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)Keys).GetEnumerator();
	}

	public CurveKeyCollection Clone()
	{
		CurveKeyCollection curveKeyCollection = new CurveKeyCollection();
		curveKeyCollection.Keys = new List<CurveKey>(Keys);
		curveKeyCollection.InvTimeRange = InvTimeRange;
		curveKeyCollection.TimeRange = TimeRange;
		curveKeyCollection.IsCacheAvailable = true;
		return curveKeyCollection;
	}

	internal void ComputeCacheValues()
	{
		TimeRange = (InvTimeRange = 0f);
		if (Keys.Count > 1)
		{
			TimeRange = Keys[Keys.Count - 1].Position - Keys[0].Position;
			if (TimeRange > float.Epsilon)
			{
				InvTimeRange = 1f / TimeRange;
			}
		}
		IsCacheAvailable = true;
	}
}
