using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public abstract class XNode : XObject
{
	private static XNodeDocumentOrderComparer s_documentOrderComparer;

	private static XNodeEqualityComparer s_equalityComparer;

	internal XNode next;

	public XNode? NextNode
	{
		get
		{
			if (parent != null && this != parent.content)
			{
				return next;
			}
			return null;
		}
	}

	public XNode? PreviousNode
	{
		get
		{
			if (parent == null)
			{
				return null;
			}
			XNode xNode = ((XNode)parent.content).next;
			XNode result = null;
			while (xNode != this)
			{
				result = xNode;
				xNode = xNode.next;
			}
			return result;
		}
	}

	public static XNodeDocumentOrderComparer DocumentOrderComparer
	{
		get
		{
			if (s_documentOrderComparer == null)
			{
				s_documentOrderComparer = new XNodeDocumentOrderComparer();
			}
			return s_documentOrderComparer;
		}
	}

	public static XNodeEqualityComparer EqualityComparer
	{
		get
		{
			if (s_equalityComparer == null)
			{
				s_equalityComparer = new XNodeEqualityComparer();
			}
			return s_equalityComparer;
		}
	}

	internal XNode()
	{
	}

	public void AddAfterSelf(object? content)
	{
		if (parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
		}
		new Inserter(parent, this).Add(content);
	}

	public void AddAfterSelf(params object?[] content)
	{
		AddAfterSelf((object?)content);
	}

	public void AddBeforeSelf(object? content)
	{
		if (parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
		}
		XNode xNode = (XNode)parent.content;
		while (xNode.next != this)
		{
			xNode = xNode.next;
		}
		if (xNode == parent.content)
		{
			xNode = null;
		}
		new Inserter(parent, xNode).Add(content);
	}

	public void AddBeforeSelf(params object?[] content)
	{
		AddBeforeSelf((object?)content);
	}

	public IEnumerable<XElement> Ancestors()
	{
		return GetAncestors(null, self: false);
	}

	public IEnumerable<XElement> Ancestors(XName? name)
	{
		if (!(name != null))
		{
			return XElement.EmptySequence;
		}
		return GetAncestors(name, self: false);
	}

	public static int CompareDocumentOrder(XNode? n1, XNode? n2)
	{
		if (n1 == n2)
		{
			return 0;
		}
		if (n1 == null)
		{
			return -1;
		}
		if (n2 == null)
		{
			return 1;
		}
		if (n1.parent != n2.parent)
		{
			int num = 0;
			XNode xNode = n1;
			while (xNode.parent != null)
			{
				xNode = xNode.parent;
				num++;
			}
			XNode xNode2 = n2;
			while (xNode2.parent != null)
			{
				xNode2 = xNode2.parent;
				num--;
			}
			if (xNode != xNode2)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MissingAncestor);
			}
			if (num < 0)
			{
				do
				{
					n2 = n2.parent;
					num++;
				}
				while (num != 0);
				if (n1 == n2)
				{
					return -1;
				}
			}
			else if (num > 0)
			{
				do
				{
					n1 = n1.parent;
					num--;
				}
				while (num != 0);
				if (n1 == n2)
				{
					return 1;
				}
			}
			while (n1.parent != n2.parent)
			{
				n1 = n1.parent;
				n2 = n2.parent;
			}
		}
		else if (n1.parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingAncestor);
		}
		XNode xNode3 = (XNode)n1.parent.content;
		do
		{
			xNode3 = xNode3.next;
			if (xNode3 == n1)
			{
				return -1;
			}
		}
		while (xNode3 != n2);
		return 1;
	}

	public XmlReader CreateReader()
	{
		return new XNodeReader(this, null);
	}

	public XmlReader CreateReader(ReaderOptions readerOptions)
	{
		return new XNodeReader(this, null, readerOptions);
	}

	public IEnumerable<XNode> NodesAfterSelf()
	{
		XNode i = this;
		while (i.parent != null && i != i.parent.content)
		{
			i = i.next;
			yield return i;
		}
	}

	public IEnumerable<XNode> NodesBeforeSelf()
	{
		if (parent == null)
		{
			yield break;
		}
		XNode i = (XNode)parent.content;
		do
		{
			i = i.next;
			if (i != this)
			{
				yield return i;
				continue;
			}
			break;
		}
		while (parent != null && parent == i.parent);
	}

	public IEnumerable<XElement> ElementsAfterSelf()
	{
		return GetElementsAfterSelf(null);
	}

	public IEnumerable<XElement> ElementsAfterSelf(XName? name)
	{
		if (!(name != null))
		{
			return XElement.EmptySequence;
		}
		return GetElementsAfterSelf(name);
	}

	public IEnumerable<XElement> ElementsBeforeSelf()
	{
		return GetElementsBeforeSelf(null);
	}

	public IEnumerable<XElement> ElementsBeforeSelf(XName? name)
	{
		if (!(name != null))
		{
			return XElement.EmptySequence;
		}
		return GetElementsBeforeSelf(name);
	}

	public bool IsAfter(XNode? node)
	{
		return CompareDocumentOrder(this, node) > 0;
	}

	public bool IsBefore(XNode? node)
	{
		return CompareDocumentOrder(this, node) < 0;
	}

	public static XNode ReadFrom(XmlReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		switch (reader.NodeType)
		{
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return new XText(reader);
		case XmlNodeType.CDATA:
			return new XCData(reader);
		case XmlNodeType.Comment:
			return new XComment(reader);
		case XmlNodeType.DocumentType:
			return new XDocumentType(reader);
		case XmlNodeType.Element:
			return new XElement(reader);
		case XmlNodeType.ProcessingInstruction:
			return new XProcessingInstruction(reader);
		default:
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, reader.NodeType));
		}
	}

	public static Task<XNode> ReadFromAsync(XmlReader reader, CancellationToken cancellationToken)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<XNode>(cancellationToken);
		}
		return ReadFromAsyncInternal(reader, cancellationToken);
	}

	private static async Task<XNode> ReadFromAsyncInternal(XmlReader reader, CancellationToken cancellationToken)
	{
		if (reader.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		XNode ret;
		switch (reader.NodeType)
		{
		case XmlNodeType.Text:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			ret = new XText(reader.Value);
			break;
		case XmlNodeType.CDATA:
			ret = new XCData(reader.Value);
			break;
		case XmlNodeType.Comment:
			ret = new XComment(reader.Value);
			break;
		case XmlNodeType.DocumentType:
		{
			string name2 = reader.Name;
			string attribute = reader.GetAttribute("PUBLIC");
			string attribute2 = reader.GetAttribute("SYSTEM");
			string value2 = reader.Value;
			ret = new XDocumentType(name2, attribute, attribute2, value2);
			break;
		}
		case XmlNodeType.Element:
			return await XElement.CreateAsync(reader, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		case XmlNodeType.ProcessingInstruction:
		{
			string name = reader.Name;
			string value = reader.Value;
			ret = new XProcessingInstruction(name, value);
			break;
		}
		default:
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_UnexpectedNodeType, reader.NodeType));
		}
		cancellationToken.ThrowIfCancellationRequested();
		await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		return ret;
	}

	public void Remove()
	{
		if (parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
		}
		parent.RemoveNode(this);
	}

	public void ReplaceWith(object? content)
	{
		if (parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
		}
		XContainer xContainer = parent;
		XNode xNode = (XNode)parent.content;
		while (xNode.next != this)
		{
			xNode = xNode.next;
		}
		if (xNode == parent.content)
		{
			xNode = null;
		}
		parent.RemoveNode(this);
		if (xNode != null && xNode.parent != xContainer)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		new Inserter(xContainer, xNode).Add(content);
	}

	public void ReplaceWith(params object?[] content)
	{
		ReplaceWith((object?)content);
	}

	public override string ToString()
	{
		return GetXmlString(GetSaveOptionsFromAnnotations());
	}

	public string ToString(SaveOptions options)
	{
		return GetXmlString(options);
	}

	public static bool DeepEquals(XNode? n1, XNode? n2)
	{
		if (n1 == n2)
		{
			return true;
		}
		if (n1 == null || n2 == null)
		{
			return false;
		}
		return n1.DeepEquals(n2);
	}

	public abstract void WriteTo(XmlWriter writer);

	public abstract Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken);

	internal virtual void AppendText(StringBuilder sb)
	{
	}

	internal abstract XNode CloneNode();

	internal abstract bool DeepEquals(XNode node);

	internal IEnumerable<XElement> GetAncestors(XName name, bool self)
	{
		for (XElement e = (self ? this : parent) as XElement; e != null; e = e.parent as XElement)
		{
			if (name == null || e.name == name)
			{
				yield return e;
			}
		}
	}

	private IEnumerable<XElement> GetElementsAfterSelf(XName name)
	{
		XNode i = this;
		while (i.parent != null && i != i.parent.content)
		{
			i = i.next;
			if (i is XElement xElement && (name == null || xElement.name == name))
			{
				yield return xElement;
			}
		}
	}

	private IEnumerable<XElement> GetElementsBeforeSelf(XName name)
	{
		if (parent == null)
		{
			yield break;
		}
		XNode i = (XNode)parent.content;
		do
		{
			i = i.next;
			if (i != this)
			{
				if (i is XElement xElement && (name == null || xElement.name == name))
				{
					yield return xElement;
				}
				continue;
			}
			break;
		}
		while (parent != null && parent == i.parent);
	}

	internal abstract int GetDeepHashCode();

	internal static XmlReaderSettings GetXmlReaderSettings(LoadOptions o)
	{
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
		if ((o & LoadOptions.PreserveWhitespace) == 0)
		{
			xmlReaderSettings.IgnoreWhitespace = true;
		}
		xmlReaderSettings.DtdProcessing = DtdProcessing.Parse;
		xmlReaderSettings.MaxCharactersFromEntities = 10000000L;
		return xmlReaderSettings;
	}

	internal static XmlWriterSettings GetXmlWriterSettings(SaveOptions o)
	{
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		if ((o & SaveOptions.DisableFormatting) == 0)
		{
			xmlWriterSettings.Indent = true;
		}
		if ((o & SaveOptions.OmitDuplicateNamespaces) != 0)
		{
			xmlWriterSettings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
		}
		return xmlWriterSettings;
	}

	private string GetXmlString(SaveOptions o)
	{
		using StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		if ((o & SaveOptions.DisableFormatting) == 0)
		{
			xmlWriterSettings.Indent = true;
		}
		if ((o & SaveOptions.OmitDuplicateNamespaces) != 0)
		{
			xmlWriterSettings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
		}
		if (this is XText)
		{
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
		}
		using (XmlWriter writer = XmlWriter.Create(stringWriter, xmlWriterSettings))
		{
			if (this is XDocument xDocument)
			{
				xDocument.WriteContentTo(writer);
			}
			else
			{
				WriteTo(writer);
			}
		}
		return stringWriter.ToString();
	}
}
