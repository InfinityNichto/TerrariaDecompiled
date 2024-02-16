using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public abstract class XContainer : XNode
{
	private sealed class ContentReader
	{
		private readonly NamespaceCache _eCache;

		private readonly NamespaceCache _aCache;

		private readonly IXmlLineInfo _lineInfo;

		private XContainer _currentContainer;

		private string _baseUri;

		public ContentReader(XContainer rootContainer)
		{
			_currentContainer = rootContainer;
		}

		public ContentReader(XContainer rootContainer, XmlReader r, LoadOptions o)
		{
			_currentContainer = rootContainer;
			_baseUri = (((o & LoadOptions.SetBaseUri) != 0) ? r.BaseURI : null);
			_lineInfo = (((o & LoadOptions.SetLineInfo) != 0) ? (r as IXmlLineInfo) : null);
		}

		public bool ReadContentFrom(XContainer rootContainer, XmlReader r)
		{
			switch (r.NodeType)
			{
			case XmlNodeType.Element:
			{
				XElement xElement = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
				if (r.MoveToFirstAttribute())
				{
					do
					{
						xElement.AppendAttributeSkipNotify(new XAttribute(_aCache.Get((r.Prefix.Length == 0) ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value));
					}
					while (r.MoveToNextAttribute());
					r.MoveToElement();
				}
				_currentContainer.AddNodeSkipNotify(xElement);
				if (!r.IsEmptyElement)
				{
					_currentContainer = xElement;
				}
				break;
			}
			case XmlNodeType.EndElement:
				if (_currentContainer.content == null)
				{
					_currentContainer.content = string.Empty;
				}
				if (_currentContainer == rootContainer)
				{
					return false;
				}
				_currentContainer = _currentContainer.parent;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				_currentContainer.AddStringSkipNotify(r.Value);
				break;
			case XmlNodeType.CDATA:
				_currentContainer.AddNodeSkipNotify(new XCData(r.Value));
				break;
			case XmlNodeType.Comment:
				_currentContainer.AddNodeSkipNotify(new XComment(r.Value));
				break;
			case XmlNodeType.ProcessingInstruction:
				_currentContainer.AddNodeSkipNotify(new XProcessingInstruction(r.Name, r.Value));
				break;
			case XmlNodeType.DocumentType:
				_currentContainer.AddNodeSkipNotify(new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), r.Value));
				break;
			case XmlNodeType.EntityReference:
				if (!r.CanResolveEntity)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_UnresolvedEntityReference);
				}
				r.ResolveEntity();
				break;
			default:
				throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
			case XmlNodeType.EndEntity:
				break;
			}
			return true;
		}

		public async ValueTask<bool> ReadContentFromAsync(XContainer rootContainer, XmlReader r)
		{
			switch (r.NodeType)
			{
			case XmlNodeType.Element:
			{
				XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
				if (r.MoveToFirstAttribute())
				{
					do
					{
						XElement xElement = e;
						XName name = _aCache.Get((r.Prefix.Length == 0) ? string.Empty : r.NamespaceURI).GetName(r.LocalName);
						xElement.AppendAttributeSkipNotify(new XAttribute(name, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)));
					}
					while (r.MoveToNextAttribute());
					r.MoveToElement();
				}
				_currentContainer.AddNodeSkipNotify(e);
				if (!r.IsEmptyElement)
				{
					_currentContainer = e;
				}
				break;
			}
			case XmlNodeType.EndElement:
				if (_currentContainer.content == null)
				{
					_currentContainer.content = string.Empty;
				}
				if (_currentContainer == rootContainer)
				{
					return false;
				}
				_currentContainer = _currentContainer.parent;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				XContainer currentContainer = _currentContainer;
				currentContainer.AddStringSkipNotify(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.CDATA:
			{
				XContainer currentContainer = _currentContainer;
				currentContainer.AddNodeSkipNotify(new XCData(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)));
				break;
			}
			case XmlNodeType.Comment:
			{
				XContainer currentContainer = _currentContainer;
				currentContainer.AddNodeSkipNotify(new XComment(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)));
				break;
			}
			case XmlNodeType.ProcessingInstruction:
			{
				XContainer currentContainer = _currentContainer;
				string localName = r.Name;
				currentContainer.AddNodeSkipNotify(new XProcessingInstruction(localName, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)));
				break;
			}
			case XmlNodeType.DocumentType:
			{
				XContainer currentContainer = _currentContainer;
				string localName = r.LocalName;
				string attribute = r.GetAttribute("PUBLIC");
				string attribute2 = r.GetAttribute("SYSTEM");
				currentContainer.AddNodeSkipNotify(new XDocumentType(localName, attribute, attribute2, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false)));
				break;
			}
			case XmlNodeType.EntityReference:
				if (!r.CanResolveEntity)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_UnresolvedEntityReference);
				}
				r.ResolveEntity();
				break;
			default:
				throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
			case XmlNodeType.EndEntity:
				break;
			}
			return true;
		}

		public bool ReadContentFrom(XContainer rootContainer, XmlReader r, LoadOptions o)
		{
			XNode xNode = null;
			string baseURI = r.BaseURI;
			switch (r.NodeType)
			{
			case XmlNodeType.Element:
			{
				XElement xElement2 = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
				if (_baseUri != null && _baseUri != baseURI)
				{
					xElement2.SetBaseUri(baseURI);
				}
				if (_lineInfo != null && _lineInfo.HasLineInfo())
				{
					xElement2.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				if (r.MoveToFirstAttribute())
				{
					do
					{
						XAttribute xAttribute = new XAttribute(_aCache.Get((r.Prefix.Length == 0) ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value);
						if (_lineInfo != null && _lineInfo.HasLineInfo())
						{
							xAttribute.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
						}
						xElement2.AppendAttributeSkipNotify(xAttribute);
					}
					while (r.MoveToNextAttribute());
					r.MoveToElement();
				}
				_currentContainer.AddNodeSkipNotify(xElement2);
				if (!r.IsEmptyElement)
				{
					_currentContainer = xElement2;
					if (_baseUri != null)
					{
						_baseUri = baseURI;
					}
				}
				break;
			}
			case XmlNodeType.EndElement:
				if (_currentContainer.content == null)
				{
					_currentContainer.content = string.Empty;
				}
				if (_currentContainer is XElement xElement && _lineInfo != null && _lineInfo.HasLineInfo())
				{
					xElement.SetEndElementLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				if (_currentContainer == rootContainer)
				{
					return false;
				}
				if (_baseUri != null && _currentContainer.HasBaseUri)
				{
					_baseUri = _currentContainer.parent.BaseUri;
				}
				_currentContainer = _currentContainer.parent;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if ((_baseUri != null && _baseUri != baseURI) || (_lineInfo != null && _lineInfo.HasLineInfo()))
				{
					xNode = new XText(r.Value);
				}
				else
				{
					_currentContainer.AddStringSkipNotify(r.Value);
				}
				break;
			case XmlNodeType.CDATA:
				xNode = new XCData(r.Value);
				break;
			case XmlNodeType.Comment:
				xNode = new XComment(r.Value);
				break;
			case XmlNodeType.ProcessingInstruction:
				xNode = new XProcessingInstruction(r.Name, r.Value);
				break;
			case XmlNodeType.DocumentType:
				xNode = new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), r.Value);
				break;
			case XmlNodeType.EntityReference:
				if (!r.CanResolveEntity)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_UnresolvedEntityReference);
				}
				r.ResolveEntity();
				break;
			default:
				throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
			case XmlNodeType.EndEntity:
				break;
			}
			if (xNode != null)
			{
				if (_baseUri != null && _baseUri != baseURI)
				{
					xNode.SetBaseUri(baseURI);
				}
				if (_lineInfo != null && _lineInfo.HasLineInfo())
				{
					xNode.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				_currentContainer.AddNodeSkipNotify(xNode);
				xNode = null;
			}
			return true;
		}

		public async ValueTask<bool> ReadContentFromAsync(XContainer rootContainer, XmlReader r, LoadOptions o)
		{
			XNode newNode = null;
			string baseUri = r.BaseURI;
			switch (r.NodeType)
			{
			case XmlNodeType.Element:
			{
				XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
				if (_baseUri != null && _baseUri != baseUri)
				{
					e.SetBaseUri(baseUri);
				}
				if (_lineInfo != null && _lineInfo.HasLineInfo())
				{
					e.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				if (r.MoveToFirstAttribute())
				{
					do
					{
						XName name = _aCache.Get((r.Prefix.Length == 0) ? string.Empty : r.NamespaceURI).GetName(r.LocalName);
						XAttribute xAttribute = new XAttribute(name, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
						if (_lineInfo != null && _lineInfo.HasLineInfo())
						{
							xAttribute.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
						}
						e.AppendAttributeSkipNotify(xAttribute);
					}
					while (r.MoveToNextAttribute());
					r.MoveToElement();
				}
				_currentContainer.AddNodeSkipNotify(e);
				if (!r.IsEmptyElement)
				{
					_currentContainer = e;
					if (_baseUri != null)
					{
						_baseUri = baseUri;
					}
				}
				break;
			}
			case XmlNodeType.EndElement:
				if (_currentContainer.content == null)
				{
					_currentContainer.content = string.Empty;
				}
				if (_currentContainer is XElement xElement && _lineInfo != null && _lineInfo.HasLineInfo())
				{
					xElement.SetEndElementLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				if (_currentContainer == rootContainer)
				{
					return false;
				}
				if (_baseUri != null && _currentContainer.HasBaseUri)
				{
					_baseUri = _currentContainer.parent.BaseUri;
				}
				_currentContainer = _currentContainer.parent;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				if ((_baseUri != null && _baseUri != baseUri) || (_lineInfo != null && _lineInfo.HasLineInfo()))
				{
					newNode = new XText(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
					break;
				}
				XContainer currentContainer = _currentContainer;
				currentContainer.AddStringSkipNotify(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.CDATA:
				newNode = new XCData(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			case XmlNodeType.Comment:
				newNode = new XComment(await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			case XmlNodeType.ProcessingInstruction:
			{
				string localName = r.Name;
				newNode = new XProcessingInstruction(localName, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.DocumentType:
			{
				string localName = r.LocalName;
				string attribute = r.GetAttribute("PUBLIC");
				string attribute2 = r.GetAttribute("SYSTEM");
				newNode = new XDocumentType(localName, attribute, attribute2, await r.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
				break;
			}
			case XmlNodeType.EntityReference:
				if (!r.CanResolveEntity)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_UnresolvedEntityReference);
				}
				r.ResolveEntity();
				break;
			default:
				throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
			case XmlNodeType.EndEntity:
				break;
			}
			if (newNode != null)
			{
				if (_baseUri != null && _baseUri != baseUri)
				{
					newNode.SetBaseUri(baseUri);
				}
				if (_lineInfo != null && _lineInfo.HasLineInfo())
				{
					newNode.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
				}
				_currentContainer.AddNodeSkipNotify(newNode);
			}
			return true;
		}
	}

	internal object content;

	public XNode? FirstNode => LastNode?.next;

	public XNode? LastNode
	{
		get
		{
			if (content == null)
			{
				return null;
			}
			if (content is XNode result)
			{
				return result;
			}
			if (content is string text)
			{
				if (text.Length == 0)
				{
					return null;
				}
				XText xText = new XText(text);
				xText.parent = this;
				xText.next = xText;
				Interlocked.CompareExchange<object>(ref content, (object)xText, (object)text);
			}
			return (XNode)content;
		}
	}

	internal XContainer()
	{
	}

	internal XContainer(XContainer other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		if (other.content is string)
		{
			content = other.content;
			return;
		}
		XNode xNode = (XNode)other.content;
		if (xNode != null)
		{
			do
			{
				xNode = xNode.next;
				AppendNodeSkipNotify(xNode.CloneNode());
			}
			while (xNode != other.content);
		}
	}

	public void Add(object? content)
	{
		if (SkipNotify())
		{
			AddContentSkipNotify(content);
		}
		else
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
			if (content is XAttribute a)
			{
				AddAttribute(a);
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
				foreach (object obj in array2)
				{
					Add(obj);
				}
				return;
			}
			if (content is IEnumerable enumerable)
			{
				{
					foreach (object item in enumerable)
					{
						Add(item);
					}
					return;
				}
			}
			AddString(GetStringValue(content));
		}
	}

	public void Add(params object?[] content)
	{
		Add((object?)content);
	}

	public void AddFirst(object? content)
	{
		new Inserter(this, null).Add(content);
	}

	public void AddFirst(params object?[] content)
	{
		AddFirst((object?)content);
	}

	public XmlWriter CreateWriter()
	{
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.ConformanceLevel = ((!(this is XDocument)) ? ConformanceLevel.Fragment : ConformanceLevel.Document);
		return XmlWriter.Create(new XNodeBuilder(this), xmlWriterSettings);
	}

	public IEnumerable<XNode> DescendantNodes()
	{
		return GetDescendantNodes(self: false);
	}

	public IEnumerable<XElement> Descendants()
	{
		return GetDescendants(null, self: false);
	}

	public IEnumerable<XElement> Descendants(XName? name)
	{
		if (!(name != null))
		{
			return XElement.EmptySequence;
		}
		return GetDescendants(name, self: false);
	}

	public XElement? Element(XName name)
	{
		XNode xNode = content as XNode;
		if (xNode != null)
		{
			do
			{
				xNode = xNode.next;
				if (xNode is XElement xElement && xElement.name == name)
				{
					return xElement;
				}
			}
			while (xNode != content);
		}
		return null;
	}

	public IEnumerable<XElement> Elements()
	{
		return GetElements(null);
	}

	public IEnumerable<XElement> Elements(XName? name)
	{
		if (!(name != null))
		{
			return XElement.EmptySequence;
		}
		return GetElements(name);
	}

	public IEnumerable<XNode> Nodes()
	{
		XNode i = LastNode;
		if (i != null)
		{
			do
			{
				i = i.next;
				yield return i;
			}
			while (i.parent == this && i != content);
		}
	}

	public void RemoveNodes()
	{
		if (SkipNotify())
		{
			RemoveNodesSkipNotify();
			return;
		}
		while (content != null)
		{
			if (content is string text)
			{
				if (text.Length > 0)
				{
					ConvertTextToNode();
				}
				else if (this is XElement)
				{
					NotifyChanging(this, XObjectChangeEventArgs.Value);
					if (text != content)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
					}
					content = null;
					NotifyChanged(this, XObjectChangeEventArgs.Value);
				}
				else
				{
					content = null;
				}
			}
			if (content is XNode { next: var xNode2 } xNode)
			{
				NotifyChanging(xNode2, XObjectChangeEventArgs.Remove);
				if (xNode != content || xNode2 != xNode.next)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
				}
				if (xNode2 != xNode)
				{
					xNode.next = xNode2.next;
				}
				else
				{
					content = null;
				}
				xNode2.parent = null;
				xNode2.next = null;
				NotifyChanged(xNode2, XObjectChangeEventArgs.Remove);
			}
		}
	}

	public void ReplaceNodes(object? content)
	{
		content = GetContentSnapshot(content);
		RemoveNodes();
		Add(content);
	}

	public void ReplaceNodes(params object?[] content)
	{
		ReplaceNodes((object?)content);
	}

	internal virtual void AddAttribute(XAttribute a)
	{
	}

	internal virtual void AddAttributeSkipNotify(XAttribute a)
	{
	}

	internal void AddContentSkipNotify(object content)
	{
		if (content == null)
		{
			return;
		}
		if (content is XNode n)
		{
			AddNodeSkipNotify(n);
			return;
		}
		if (content is string s)
		{
			AddStringSkipNotify(s);
			return;
		}
		if (content is XAttribute a)
		{
			AddAttributeSkipNotify(a);
			return;
		}
		if (content is XStreamingElement other)
		{
			AddNodeSkipNotify(new XElement(other));
			return;
		}
		if (content is object[] array)
		{
			object[] array2 = array;
			foreach (object obj in array2)
			{
				AddContentSkipNotify(obj);
			}
			return;
		}
		if (content is IEnumerable enumerable)
		{
			{
				foreach (object item in enumerable)
				{
					AddContentSkipNotify(item);
				}
				return;
			}
		}
		AddStringSkipNotify(GetStringValue(content));
	}

	internal void AddNode(XNode n)
	{
		ValidateNode(n, this);
		if (n.parent != null)
		{
			n = n.CloneNode();
		}
		else
		{
			XNode xNode = this;
			while (xNode.parent != null)
			{
				xNode = xNode.parent;
			}
			if (n == xNode)
			{
				n = n.CloneNode();
			}
		}
		ConvertTextToNode();
		AppendNode(n);
	}

	internal void AddNodeSkipNotify(XNode n)
	{
		ValidateNode(n, this);
		if (n.parent != null)
		{
			n = n.CloneNode();
		}
		else
		{
			XNode xNode = this;
			while (xNode.parent != null)
			{
				xNode = xNode.parent;
			}
			if (n == xNode)
			{
				n = n.CloneNode();
			}
		}
		ConvertTextToNode();
		AppendNodeSkipNotify(n);
	}

	internal void AddString(string s)
	{
		ValidateString(s);
		if (content == null)
		{
			if (s.Length > 0)
			{
				AppendNode(new XText(s));
			}
			else if (this is XElement)
			{
				NotifyChanging(this, XObjectChangeEventArgs.Value);
				if (content != null)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
				}
				content = s;
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
			else
			{
				content = s;
			}
		}
		else if (s.Length > 0)
		{
			ConvertTextToNode();
			if (content is XText xText && !(xText is XCData))
			{
				xText.Value += s;
			}
			else
			{
				AppendNode(new XText(s));
			}
		}
	}

	internal void AddStringSkipNotify(string s)
	{
		ValidateString(s);
		if (content == null)
		{
			content = s;
		}
		else if (s.Length > 0)
		{
			if (content is string text)
			{
				content = text + s;
			}
			else if (content is XText xText && !(xText is XCData))
			{
				xText.text += s;
			}
			else
			{
				AppendNodeSkipNotify(new XText(s));
			}
		}
	}

	internal void AppendNode(XNode n)
	{
		bool flag = NotifyChanging(n, XObjectChangeEventArgs.Add);
		if (n.parent != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		AppendNodeSkipNotify(n);
		if (flag)
		{
			NotifyChanged(n, XObjectChangeEventArgs.Add);
		}
	}

	internal void AppendNodeSkipNotify(XNode n)
	{
		n.parent = this;
		if (content == null || content is string)
		{
			n.next = n;
		}
		else
		{
			XNode xNode = (XNode)content;
			n.next = xNode.next;
			xNode.next = n;
		}
		content = n;
	}

	internal override void AppendText(StringBuilder sb)
	{
		if (content is string value)
		{
			sb.Append(value);
			return;
		}
		XNode xNode = (XNode)content;
		if (xNode != null)
		{
			do
			{
				xNode = xNode.next;
				xNode.AppendText(sb);
			}
			while (xNode != content);
		}
	}

	private string GetTextOnly()
	{
		if (content == null)
		{
			return null;
		}
		string text = content as string;
		if (text == null)
		{
			XNode xNode = (XNode)content;
			do
			{
				xNode = xNode.next;
				if (xNode.NodeType != XmlNodeType.Text)
				{
					return null;
				}
				text += ((XText)xNode).Value;
			}
			while (xNode != content);
		}
		return text;
	}

	private string CollectText(ref XNode n)
	{
		string text = "";
		while (n != null && n.NodeType == XmlNodeType.Text)
		{
			text += ((XText)n).Value;
			n = ((n != content) ? n.next : null);
		}
		return text;
	}

	internal bool ContentsEqual(XContainer e)
	{
		if (content == e.content)
		{
			return true;
		}
		string textOnly = GetTextOnly();
		if (textOnly != null)
		{
			return textOnly == e.GetTextOnly();
		}
		XNode xNode = content as XNode;
		XNode xNode2 = e.content as XNode;
		if (xNode != null && xNode2 != null)
		{
			xNode = xNode.next;
			xNode2 = xNode2.next;
			while (!(CollectText(ref xNode) != e.CollectText(ref xNode2)))
			{
				if (xNode == null && xNode2 == null)
				{
					return true;
				}
				if (xNode == null || xNode2 == null || !xNode.DeepEquals(xNode2))
				{
					break;
				}
				xNode = ((xNode != content) ? xNode.next : null);
				xNode2 = ((xNode2 != e.content) ? xNode2.next : null);
			}
		}
		return false;
	}

	internal int ContentsHashCode()
	{
		string textOnly = GetTextOnly();
		if (textOnly != null)
		{
			return textOnly.GetHashCode();
		}
		int num = 0;
		XNode n = content as XNode;
		if (n != null)
		{
			do
			{
				n = n.next;
				string text = CollectText(ref n);
				if (text.Length > 0)
				{
					num ^= text.GetHashCode();
				}
				if (n == null)
				{
					break;
				}
				num ^= n.GetDeepHashCode();
			}
			while (n != content);
		}
		return num;
	}

	internal void ConvertTextToNode()
	{
		string value = content as string;
		if (!string.IsNullOrEmpty(value))
		{
			XText xText = new XText(value);
			xText.parent = this;
			xText.next = xText;
			content = xText;
		}
	}

	internal IEnumerable<XNode> GetDescendantNodes(bool self)
	{
		if (self)
		{
			yield return this;
		}
		XNode i = this;
		while (true)
		{
			XNode firstNode;
			if (i is XContainer xContainer && (firstNode = xContainer.FirstNode) != null)
			{
				i = firstNode;
			}
			else
			{
				while (i != null && i != this && i == i.parent.content)
				{
					i = i.parent;
				}
				if (i == null || i == this)
				{
					break;
				}
				i = i.next;
			}
			yield return i;
		}
	}

	internal IEnumerable<XElement> GetDescendants(XName name, bool self)
	{
		if (self)
		{
			XElement xElement = (XElement)this;
			if (name == null || xElement.name == name)
			{
				yield return xElement;
			}
		}
		XNode i = this;
		XContainer xContainer = this;
		while (true)
		{
			if (xContainer != null && xContainer.content is XNode)
			{
				i = ((XNode)xContainer.content).next;
			}
			else
			{
				while (i != this && i == i.parent.content)
				{
					i = i.parent;
				}
				if (i == this)
				{
					break;
				}
				i = i.next;
			}
			XElement e = i as XElement;
			if (e != null && (name == null || e.name == name))
			{
				yield return e;
			}
			xContainer = e;
		}
	}

	private IEnumerable<XElement> GetElements(XName name)
	{
		XNode i = content as XNode;
		if (i == null)
		{
			yield break;
		}
		do
		{
			i = i.next;
			if (i is XElement xElement && (name == null || xElement.name == name))
			{
				yield return xElement;
			}
		}
		while (i.parent == this && i != content);
	}

	internal static string GetStringValue(object value)
	{
		string text2;
		if (!(value is string text))
		{
			if (!(value is int num))
			{
				if (!(value is double num2))
				{
					if (!(value is long num3))
					{
						if (!(value is float num4))
						{
							if (!(value is decimal num5))
							{
								if (!(value is short num6))
								{
									if (!(value is sbyte b))
									{
										if (!(value is bool flag))
										{
											if (!(value is DateTime dateTime))
											{
												if (!(value is DateTimeOffset dateTimeOffset))
												{
													if (!(value is TimeSpan timeSpan))
													{
														if (value is XObject)
														{
															throw new ArgumentException(System.SR.Argument_XObjectValue);
														}
														text2 = value.ToString();
													}
													else
													{
														text2 = XmlConvert.ToString(timeSpan);
													}
												}
												else
												{
													text2 = XmlConvert.ToString(dateTimeOffset);
												}
											}
											else
											{
												text2 = XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.RoundtripKind);
											}
										}
										else
										{
											text2 = XmlConvert.ToString(flag);
										}
									}
									else
									{
										text2 = XmlConvert.ToString(b);
									}
								}
								else
								{
									text2 = XmlConvert.ToString(num6);
								}
							}
							else
							{
								text2 = XmlConvert.ToString(num5);
							}
						}
						else
						{
							text2 = XmlConvert.ToString(num4);
						}
					}
					else
					{
						text2 = XmlConvert.ToString(num3);
					}
				}
				else
				{
					text2 = XmlConvert.ToString(num2);
				}
			}
			else
			{
				text2 = XmlConvert.ToString(num);
			}
		}
		else
		{
			text2 = text;
		}
		string text3 = text2;
		if (text3 == null)
		{
			throw new ArgumentException(System.SR.Argument_ConvertToString);
		}
		return text3;
	}

	internal void ReadContentFrom(XmlReader r)
	{
		if (r.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		ContentReader contentReader = new ContentReader(this);
		while (contentReader.ReadContentFrom(this, r) && r.Read())
		{
		}
	}

	internal void ReadContentFrom(XmlReader r, LoadOptions o)
	{
		if ((o & (LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)) == 0)
		{
			ReadContentFrom(r);
			return;
		}
		if (r.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		ContentReader contentReader = new ContentReader(this, r, o);
		while (contentReader.ReadContentFrom(this, r, o) && r.Read())
		{
		}
	}

	internal async Task ReadContentFromAsync(XmlReader r, CancellationToken cancellationToken)
	{
		if (r.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		ContentReader cr = new ContentReader(this);
		bool flag;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();
			flag = await cr.ReadContentFromAsync(this, r).ConfigureAwait(continueOnCapturedContext: false);
			if (flag)
			{
				flag = await r.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (flag);
	}

	internal async Task ReadContentFromAsync(XmlReader r, LoadOptions o, CancellationToken cancellationToken)
	{
		if ((o & (LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)) == 0)
		{
			await ReadContentFromAsync(r, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (r.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		ContentReader cr = new ContentReader(this, r, o);
		bool flag;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();
			flag = await cr.ReadContentFromAsync(this, r, o).ConfigureAwait(continueOnCapturedContext: false);
			if (flag)
			{
				flag = await r.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (flag);
	}

	internal void RemoveNode(XNode n)
	{
		bool flag = NotifyChanging(n, XObjectChangeEventArgs.Remove);
		if (n.parent != this)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		XNode xNode = (XNode)content;
		while (xNode.next != n)
		{
			xNode = xNode.next;
		}
		if (xNode == n)
		{
			content = null;
		}
		else
		{
			if (content == n)
			{
				content = xNode;
			}
			xNode.next = n.next;
		}
		n.parent = null;
		n.next = null;
		if (flag)
		{
			NotifyChanged(n, XObjectChangeEventArgs.Remove);
		}
	}

	private void RemoveNodesSkipNotify()
	{
		XNode xNode = content as XNode;
		if (xNode != null)
		{
			do
			{
				XNode xNode2 = xNode.next;
				xNode.parent = null;
				xNode.next = null;
				xNode = xNode2;
			}
			while (xNode != content);
		}
		content = null;
	}

	internal virtual void ValidateNode(XNode node, XNode previous)
	{
	}

	internal virtual void ValidateString(string s)
	{
	}

	internal void WriteContentTo(XmlWriter writer)
	{
		if (content == null)
		{
			return;
		}
		if (content is string text)
		{
			if (this is XDocument)
			{
				writer.WriteWhitespace(text);
			}
			else
			{
				writer.WriteString(text);
			}
			return;
		}
		XNode xNode = (XNode)content;
		do
		{
			xNode = xNode.next;
			xNode.WriteTo(writer);
		}
		while (xNode != content);
	}

	internal async Task WriteContentToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		if (content == null)
		{
			return;
		}
		if (content is string text)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Task task = ((!(this is XDocument)) ? writer.WriteStringAsync(text) : writer.WriteWhitespaceAsync(text));
			await task.ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		XNode i = (XNode)content;
		do
		{
			i = i.next;
			await i.WriteToAsync(writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (i != content);
	}

	private static void AddContentToList(List<object> list, object content)
	{
		IEnumerable enumerable = ((content is string) ? null : (content as IEnumerable));
		if (enumerable == null)
		{
			list.Add(content);
			return;
		}
		foreach (object item in enumerable)
		{
			if (item != null)
			{
				AddContentToList(list, item);
			}
		}
	}

	[return: NotNullIfNotNull("content")]
	internal static object GetContentSnapshot(object content)
	{
		if (content is string || !(content is IEnumerable))
		{
			return content;
		}
		List<object> list = new List<object>();
		AddContentToList(list, content);
		return list;
	}
}
