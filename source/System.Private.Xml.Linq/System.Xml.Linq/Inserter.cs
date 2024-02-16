using System.Collections;

namespace System.Xml.Linq;

internal struct Inserter
{
	private readonly XContainer _parent;

	private XNode _previous;

	private string _text;

	public Inserter(XContainer parent, XNode anchor)
	{
		_parent = parent;
		_previous = anchor;
		_text = null;
	}

	public void Add(object content)
	{
		AddContent(content);
		if (_text == null)
		{
			return;
		}
		if (_parent.content == null)
		{
			if (_parent.SkipNotify())
			{
				_parent.content = _text;
			}
			else if (_text.Length > 0)
			{
				InsertNode(new XText(_text));
			}
			else if (_parent is XElement)
			{
				_parent.NotifyChanging(_parent, XObjectChangeEventArgs.Value);
				if (_parent.content != null)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
				}
				_parent.content = _text;
				_parent.NotifyChanged(_parent, XObjectChangeEventArgs.Value);
			}
			else
			{
				_parent.content = _text;
			}
		}
		else if (_text.Length > 0)
		{
			if (_previous is XText xText && !(_previous is XCData))
			{
				xText.Value += _text;
				return;
			}
			_parent.ConvertTextToNode();
			InsertNode(new XText(_text));
		}
	}

	private void AddContent(object content)
	{
		if (content == null)
		{
			return;
		}
		if (content is XNode n)
		{
			AddNode(n);
			return;
		}
		if (content is string s)
		{
			AddString(s);
			return;
		}
		if (content is XStreamingElement other)
		{
			AddNode(new XElement(other));
			return;
		}
		if (content is object[] array)
		{
			object[] array2 = array;
			foreach (object content2 in array2)
			{
				AddContent(content2);
			}
			return;
		}
		if (content is IEnumerable enumerable)
		{
			{
				foreach (object item in enumerable)
				{
					AddContent(item);
				}
				return;
			}
		}
		if (content is XAttribute)
		{
			throw new ArgumentException(System.SR.Argument_AddAttribute);
		}
		AddString(XContainer.GetStringValue(content));
	}

	private void AddNode(XNode n)
	{
		_parent.ValidateNode(n, _previous);
		if (n.parent != null)
		{
			n = n.CloneNode();
		}
		else
		{
			XNode parent = _parent;
			while (parent.parent != null)
			{
				parent = parent.parent;
			}
			if (n == parent)
			{
				n = n.CloneNode();
			}
		}
		_parent.ConvertTextToNode();
		if (_text != null)
		{
			if (_text.Length > 0)
			{
				if (_previous is XText xText && !(_previous is XCData))
				{
					xText.Value += _text;
				}
				else
				{
					InsertNode(new XText(_text));
				}
			}
			_text = null;
		}
		InsertNode(n);
	}

	private void AddString(string s)
	{
		_parent.ValidateString(s);
		_text += s;
	}

	private void InsertNode(XNode n)
	{
		bool flag = _parent.NotifyChanging(n, XObjectChangeEventArgs.Add);
		if (n.parent != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		n.parent = _parent;
		if (_parent.content == null || _parent.content is string)
		{
			n.next = n;
			_parent.content = n;
		}
		else if (_previous == null)
		{
			XNode xNode = (XNode)_parent.content;
			n.next = xNode.next;
			xNode.next = n;
		}
		else
		{
			n.next = _previous.next;
			_previous.next = n;
			if (_parent.content == _previous)
			{
				_parent.content = n;
			}
		}
		_previous = n;
		if (flag)
		{
			_parent.NotifyChanged(n, XObjectChangeEventArgs.Add);
		}
	}
}
