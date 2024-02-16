using System.Collections;
using System.Collections.Generic;

namespace System.Xml;

public class XmlNamedNodeMap : IEnumerable
{
	internal struct SmallXmlNodeList
	{
		private sealed class SingleObjectEnumerator : IEnumerator
		{
			private readonly object _loneValue;

			private int _position = -1;

			public object Current
			{
				get
				{
					if (_position != 0)
					{
						throw new InvalidOperationException();
					}
					return _loneValue;
				}
			}

			public SingleObjectEnumerator(object value)
			{
				_loneValue = value;
			}

			public bool MoveNext()
			{
				if (_position < 0)
				{
					_position = 0;
					return true;
				}
				_position = 1;
				return false;
			}

			public void Reset()
			{
				_position = -1;
			}
		}

		private object _field;

		public int Count
		{
			get
			{
				if (_field == null)
				{
					return 0;
				}
				if (_field is List<object> list)
				{
					return list.Count;
				}
				return 1;
			}
		}

		public object this[int index]
		{
			get
			{
				if (_field == null)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				if (_field is List<object> list)
				{
					return list[index];
				}
				if (index != 0)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return _field;
			}
		}

		public void Add(object value)
		{
			if (_field == null)
			{
				if (value == null)
				{
					List<object> list = new List<object>();
					list.Add(null);
					_field = list;
				}
				else
				{
					_field = value;
				}
			}
			else if (_field is List<object> list2)
			{
				list2.Add(value);
			}
			else
			{
				List<object> list3 = new List<object>();
				list3.Add(_field);
				list3.Add(value);
				_field = list3;
			}
		}

		public void RemoveAt(int index)
		{
			if (_field == null)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (_field is List<object> list)
			{
				list.RemoveAt(index);
				return;
			}
			if (index != 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			_field = null;
		}

		public void Insert(int index, object value)
		{
			if (_field == null)
			{
				if (index != 0)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				Add(value);
				return;
			}
			if (_field is List<object> list)
			{
				list.Insert(index, value);
				return;
			}
			switch (index)
			{
			case 0:
			{
				List<object> list2 = new List<object>();
				list2.Add(value);
				list2.Add(_field);
				_field = list2;
				break;
			}
			case 1:
			{
				List<object> list2 = new List<object>();
				list2.Add(_field);
				list2.Add(value);
				_field = list2;
				break;
			}
			default:
				throw new ArgumentOutOfRangeException("index");
			}
		}

		public IEnumerator GetEnumerator()
		{
			if (_field == null)
			{
				return XmlDocument.EmptyEnumerator;
			}
			if (_field is List<object> list)
			{
				return list.GetEnumerator();
			}
			return new SingleObjectEnumerator(_field);
		}
	}

	internal XmlNode parent;

	internal SmallXmlNodeList nodes;

	public virtual int Count => nodes.Count;

	internal XmlNamedNodeMap(XmlNode parent)
	{
		this.parent = parent;
	}

	public virtual XmlNode? GetNamedItem(string name)
	{
		int num = FindNodeOffset(name);
		if (num >= 0)
		{
			return (XmlNode)nodes[num];
		}
		return null;
	}

	public virtual XmlNode? SetNamedItem(XmlNode? node)
	{
		if (node == null)
		{
			return null;
		}
		int num = FindNodeOffset(node.LocalName, node.NamespaceURI);
		if (num == -1)
		{
			AddNode(node);
			return null;
		}
		return ReplaceNodeAt(num, node);
	}

	public virtual XmlNode? RemoveNamedItem(string name)
	{
		int num = FindNodeOffset(name);
		if (num >= 0)
		{
			return RemoveNodeAt(num);
		}
		return null;
	}

	public virtual XmlNode? Item(int index)
	{
		if (index < 0 || index >= nodes.Count)
		{
			return null;
		}
		try
		{
			return (XmlNode)nodes[index];
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new IndexOutOfRangeException(System.SR.Xdom_IndexOutOfRange);
		}
	}

	public virtual XmlNode? GetNamedItem(string localName, string? namespaceURI)
	{
		int num = FindNodeOffset(localName, namespaceURI);
		if (num >= 0)
		{
			return (XmlNode)nodes[num];
		}
		return null;
	}

	public virtual XmlNode? RemoveNamedItem(string localName, string? namespaceURI)
	{
		int num = FindNodeOffset(localName, namespaceURI);
		if (num >= 0)
		{
			return RemoveNodeAt(num);
		}
		return null;
	}

	public virtual IEnumerator GetEnumerator()
	{
		return nodes.GetEnumerator();
	}

	internal int FindNodeOffset(string name)
	{
		int count = Count;
		for (int i = 0; i < count; i++)
		{
			XmlNode xmlNode = (XmlNode)nodes[i];
			if (name == xmlNode.Name)
			{
				return i;
			}
		}
		return -1;
	}

	internal int FindNodeOffset(string localName, string namespaceURI)
	{
		int count = Count;
		for (int i = 0; i < count; i++)
		{
			XmlNode xmlNode = (XmlNode)nodes[i];
			if (xmlNode.LocalName == localName && xmlNode.NamespaceURI == namespaceURI)
			{
				return i;
			}
		}
		return -1;
	}

	internal virtual XmlNode AddNode(XmlNode node)
	{
		XmlNode oldParent = ((node.NodeType != XmlNodeType.Attribute) ? node.ParentNode : ((XmlAttribute)node).OwnerElement);
		string value = node.Value;
		XmlNodeChangedEventArgs eventArgs = parent.GetEventArgs(node, oldParent, parent, value, value, XmlNodeChangedAction.Insert);
		if (eventArgs != null)
		{
			parent.BeforeEvent(eventArgs);
		}
		nodes.Add(node);
		node.SetParent(parent);
		if (eventArgs != null)
		{
			parent.AfterEvent(eventArgs);
		}
		return node;
	}

	internal virtual XmlNode AddNodeForLoad(XmlNode node, XmlDocument doc)
	{
		XmlNodeChangedEventArgs insertEventArgsForLoad = doc.GetInsertEventArgsForLoad(node, parent);
		if (insertEventArgsForLoad != null)
		{
			doc.BeforeEvent(insertEventArgsForLoad);
		}
		nodes.Add(node);
		node.SetParent(parent);
		if (insertEventArgsForLoad != null)
		{
			doc.AfterEvent(insertEventArgsForLoad);
		}
		return node;
	}

	internal virtual XmlNode RemoveNodeAt(int i)
	{
		XmlNode xmlNode = (XmlNode)nodes[i];
		string value = xmlNode.Value;
		XmlNodeChangedEventArgs eventArgs = parent.GetEventArgs(xmlNode, parent, null, value, value, XmlNodeChangedAction.Remove);
		if (eventArgs != null)
		{
			parent.BeforeEvent(eventArgs);
		}
		nodes.RemoveAt(i);
		xmlNode.SetParent(null);
		if (eventArgs != null)
		{
			parent.AfterEvent(eventArgs);
		}
		return xmlNode;
	}

	internal XmlNode ReplaceNodeAt(int i, XmlNode node)
	{
		XmlNode result = RemoveNodeAt(i);
		InsertNodeAt(i, node);
		return result;
	}

	internal virtual XmlNode InsertNodeAt(int i, XmlNode node)
	{
		XmlNode oldParent = ((node.NodeType != XmlNodeType.Attribute) ? node.ParentNode : ((XmlAttribute)node).OwnerElement);
		string value = node.Value;
		XmlNodeChangedEventArgs eventArgs = parent.GetEventArgs(node, oldParent, parent, value, value, XmlNodeChangedAction.Insert);
		if (eventArgs != null)
		{
			parent.BeforeEvent(eventArgs);
		}
		nodes.Insert(i, node);
		node.SetParent(parent);
		if (eventArgs != null)
		{
			parent.AfterEvent(eventArgs);
		}
		return node;
	}
}
