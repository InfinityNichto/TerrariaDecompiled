using System.Collections;

namespace System.Xml.Serialization;

public class XmlAnyElementAttributes : CollectionBase
{
	public XmlAnyElementAttribute? this[int index]
	{
		get
		{
			return (XmlAnyElementAttribute)base.List[index];
		}
		set
		{
			base.List[index] = value;
		}
	}

	public int Add(XmlAnyElementAttribute? attribute)
	{
		return base.List.Add(attribute);
	}

	public void Insert(int index, XmlAnyElementAttribute? attribute)
	{
		base.List.Insert(index, attribute);
	}

	public int IndexOf(XmlAnyElementAttribute? attribute)
	{
		return base.List.IndexOf(attribute);
	}

	public bool Contains(XmlAnyElementAttribute? attribute)
	{
		return base.List.Contains(attribute);
	}

	public void Remove(XmlAnyElementAttribute? attribute)
	{
		base.List.Remove(attribute);
	}

	public void CopyTo(XmlAnyElementAttribute[] array, int index)
	{
		base.List.CopyTo(array, index);
	}
}
