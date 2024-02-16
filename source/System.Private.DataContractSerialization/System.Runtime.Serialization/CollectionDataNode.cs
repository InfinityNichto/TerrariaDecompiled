using System.Collections.Generic;
using System.Globalization;

namespace System.Runtime.Serialization;

internal sealed class CollectionDataNode : DataNode<Array>
{
	private IList<IDataNode> _items;

	private string _itemName;

	private string _itemNamespace;

	private int _size = -1;

	internal IList<IDataNode> Items
	{
		get
		{
			return _items;
		}
		set
		{
			_items = value;
		}
	}

	internal string ItemName
	{
		get
		{
			return _itemName;
		}
		set
		{
			_itemName = value;
		}
	}

	internal string ItemNamespace
	{
		get
		{
			return _itemNamespace;
		}
		set
		{
			_itemNamespace = value;
		}
	}

	internal int Size
	{
		get
		{
			return _size;
		}
		set
		{
			_size = value;
		}
	}

	internal CollectionDataNode()
	{
		dataType = Globals.TypeOfCollectionDataNode;
	}

	public override void GetData(ElementData element)
	{
		base.GetData(element);
		element.AddAttribute("z", "http://schemas.microsoft.com/2003/10/Serialization/", "Size", Size.ToString(NumberFormatInfo.InvariantInfo));
	}

	public override void Clear()
	{
		base.Clear();
		_items = null;
		_size = -1;
	}
}
