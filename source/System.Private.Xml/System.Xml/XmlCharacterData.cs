using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.XPath;

namespace System.Xml;

public abstract class XmlCharacterData : XmlLinkedNode
{
	private string _data;

	public override string? Value
	{
		get
		{
			return Data;
		}
		set
		{
			Data = value;
		}
	}

	public override string InnerText
	{
		get
		{
			return Value;
		}
		set
		{
			Value = value;
		}
	}

	public virtual string Data
	{
		get
		{
			if (_data != null)
			{
				return _data;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			XmlNode xmlNode = ParentNode;
			XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, value, XmlNodeChangedAction.Change);
			if (eventArgs != null)
			{
				BeforeEvent(eventArgs);
			}
			_data = value;
			if (eventArgs != null)
			{
				AfterEvent(eventArgs);
			}
		}
	}

	public virtual int Length
	{
		get
		{
			if (_data != null)
			{
				return _data.Length;
			}
			return 0;
		}
	}

	protected internal XmlCharacterData(string? data, XmlDocument doc)
		: base(doc)
	{
		_data = data;
	}

	public virtual string Substring(int offset, int count)
	{
		int num = ((_data != null) ? _data.Length : 0);
		if (num > 0)
		{
			if (num < offset + count)
			{
				count = num - offset;
			}
			return _data.Substring(offset, count);
		}
		return string.Empty;
	}

	public virtual void AppendData(string? strData)
	{
		XmlNode xmlNode = ParentNode;
		int num = ((_data != null) ? _data.Length : 0);
		if (strData != null)
		{
			num += strData.Length;
		}
		string text = new StringBuilder(num).Append(_data).Append(strData).ToString();
		XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, text, XmlNodeChangedAction.Change);
		if (eventArgs != null)
		{
			BeforeEvent(eventArgs);
		}
		_data = text;
		if (eventArgs != null)
		{
			AfterEvent(eventArgs);
		}
	}

	public virtual void InsertData(int offset, string? strData)
	{
		XmlNode xmlNode = ParentNode;
		int num = ((_data != null) ? _data.Length : 0);
		if (strData != null)
		{
			num += strData.Length;
		}
		string text = new StringBuilder(num).Append(_data).Insert(offset, strData).ToString();
		XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, text, XmlNodeChangedAction.Change);
		if (eventArgs != null)
		{
			BeforeEvent(eventArgs);
		}
		_data = text;
		if (eventArgs != null)
		{
			AfterEvent(eventArgs);
		}
	}

	public virtual void DeleteData(int offset, int count)
	{
		int num = ((_data != null) ? _data.Length : 0);
		if (num > 0 && num < offset + count)
		{
			count = Math.Max(num - offset, 0);
		}
		string text = new StringBuilder(_data).Remove(offset, count).ToString();
		XmlNode xmlNode = ParentNode;
		XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, text, XmlNodeChangedAction.Change);
		if (eventArgs != null)
		{
			BeforeEvent(eventArgs);
		}
		_data = text;
		if (eventArgs != null)
		{
			AfterEvent(eventArgs);
		}
	}

	public virtual void ReplaceData(int offset, int count, string? strData)
	{
		int num = ((_data != null) ? _data.Length : 0);
		if (num > 0 && num < offset + count)
		{
			count = Math.Max(num - offset, 0);
		}
		StringBuilder stringBuilder = new StringBuilder(_data).Remove(offset, count);
		string text = stringBuilder.Insert(offset, strData).ToString();
		XmlNode xmlNode = ParentNode;
		XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, text, XmlNodeChangedAction.Change);
		if (eventArgs != null)
		{
			BeforeEvent(eventArgs);
		}
		_data = text;
		if (eventArgs != null)
		{
			AfterEvent(eventArgs);
		}
	}

	internal bool CheckOnData(string data)
	{
		return XmlCharType.IsOnlyWhitespace(data);
	}

	internal bool DecideXPNodeTypeForTextNodes(XmlNode node, ref XPathNodeType xnt)
	{
		for (XmlNode xmlNode = node; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			switch (xmlNode.NodeType)
			{
			case XmlNodeType.SignificantWhitespace:
				xnt = XPathNodeType.SignificantWhitespace;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				xnt = XPathNodeType.Text;
				return false;
			case XmlNodeType.EntityReference:
				if (!DecideXPNodeTypeForTextNodes(xmlNode.FirstChild, ref xnt))
				{
					return false;
				}
				break;
			default:
				return false;
			case XmlNodeType.Whitespace:
				break;
			}
		}
		return true;
	}
}
