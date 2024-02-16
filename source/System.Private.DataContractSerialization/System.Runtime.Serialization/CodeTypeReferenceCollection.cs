using System.Collections;

namespace System.Runtime.Serialization;

internal sealed class CodeTypeReferenceCollection : CollectionBase
{
	public CodeTypeReference this[int index]
	{
		get
		{
			return (CodeTypeReference)base.List[index];
		}
		set
		{
			base.List[index] = value;
		}
	}

	public CodeTypeReferenceCollection()
	{
	}

	public CodeTypeReferenceCollection(CodeTypeReferenceCollection value)
	{
		AddRange(value);
	}

	public CodeTypeReferenceCollection(CodeTypeReference[] value)
	{
		AddRange(value);
	}

	public int Add(CodeTypeReference value)
	{
		return base.List.Add(value);
	}

	public void Add(string value)
	{
		Add(new CodeTypeReference(value));
	}

	public void Add(Type value)
	{
		Add(new CodeTypeReference(value));
	}

	public void AddRange(CodeTypeReference[] value)
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

	public void AddRange(CodeTypeReferenceCollection value)
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

	public bool Contains(CodeTypeReference value)
	{
		return base.List.Contains(value);
	}

	public void CopyTo(CodeTypeReference[] array, int index)
	{
		base.List.CopyTo(array, index);
	}

	public int IndexOf(CodeTypeReference value)
	{
		return base.List.IndexOf(value);
	}

	public void Insert(int index, CodeTypeReference value)
	{
		base.List.Insert(index, value);
	}

	public void Remove(CodeTypeReference value)
	{
		base.List.Remove(value);
	}
}
