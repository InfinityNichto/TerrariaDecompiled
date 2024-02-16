using System.Collections;

namespace System.ComponentModel.Design;

public class DesignerVerbCollection : CollectionBase
{
	public DesignerVerb? this[int index]
	{
		get
		{
			return (DesignerVerb)base.List[index];
		}
		set
		{
			base.List[index] = value;
		}
	}

	public DesignerVerbCollection()
	{
	}

	public DesignerVerbCollection(DesignerVerb[] value)
	{
		AddRange(value);
	}

	public int Add(DesignerVerb? value)
	{
		return base.List.Add(value);
	}

	public void AddRange(DesignerVerb?[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		for (int i = 0; i < value.Length; i++)
		{
			Add(value[i]);
		}
	}

	public void AddRange(DesignerVerbCollection value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int count = value.Count;
		for (int i = 0; i < count; i++)
		{
			Add(value[i]);
		}
	}

	public void Insert(int index, DesignerVerb? value)
	{
		base.List.Insert(index, value);
	}

	public int IndexOf(DesignerVerb? value)
	{
		return base.List.IndexOf(value);
	}

	public bool Contains(DesignerVerb? value)
	{
		return base.List.Contains(value);
	}

	public void Remove(DesignerVerb? value)
	{
		base.List.Remove(value);
	}

	public void CopyTo(DesignerVerb?[] array, int index)
	{
		base.List.CopyTo(array, index);
	}

	protected override void OnValidate(object value)
	{
	}
}
