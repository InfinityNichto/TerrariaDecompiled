using System.Collections;

namespace System.Xml.Serialization;

public class XmlArrayItemAttributes : CollectionBase
{
	public XmlArrayItemAttribute? this[int index]
	{
		get
		{
			return (XmlArrayItemAttribute)base.List[index];
		}
		set
		{
			base.List[index] = value;
		}
	}

	public int Add(XmlArrayItemAttribute? attribute)
	{
		return base.List.Add(attribute);
	}

	public void Insert(int index, XmlArrayItemAttribute? attribute)
	{
		base.List.Insert(index, attribute);
	}

	public int IndexOf(XmlArrayItemAttribute? attribute)
	{
		return base.List.IndexOf(attribute);
	}

	public bool Contains(XmlArrayItemAttribute? attribute)
	{
		return base.List.Contains(attribute);
	}

	public void Remove(XmlArrayItemAttribute? attribute)
	{
		base.List.Remove(attribute);
	}

	public void CopyTo(XmlArrayItemAttribute[] array, int index)
	{
		base.List.CopyTo(array, index);
	}
}
