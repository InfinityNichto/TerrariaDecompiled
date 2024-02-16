using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Xml.Linq;

[XmlSchemaProvider(null, IsAny = true)]
[TypeDescriptionProvider("MS.Internal.Xml.Linq.ComponentModel.XTypeDescriptionProvider`1[[System.Xml.Linq.XElement, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]],System.ComponentModel.TypeConverter")]
public class XElement : XContainer, IXmlSerializable
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct AsyncConstructionSentry
	{
	}

	internal XName name;

	internal XAttribute lastAttr;

	public static IEnumerable<XElement> EmptySequence => Array.Empty<XElement>();

	public XAttribute? FirstAttribute
	{
		get
		{
			if (lastAttr == null)
			{
				return null;
			}
			return lastAttr.next;
		}
	}

	public bool HasAttributes => lastAttr != null;

	public bool HasElements
	{
		get
		{
			XNode xNode = content as XNode;
			if (xNode != null)
			{
				do
				{
					if (xNode is XElement)
					{
						return true;
					}
					xNode = xNode.next;
				}
				while (xNode != content);
			}
			return false;
		}
	}

	public bool IsEmpty => content == null;

	public XAttribute? LastAttribute => lastAttr;

	public XName Name
	{
		get
		{
			return name;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Name);
			name = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Name);
			}
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.Element;

	public string Value
	{
		get
		{
			if (content == null)
			{
				return string.Empty;
			}
			if (content is string result)
			{
				return result;
			}
			StringBuilder sb = System.Text.StringBuilderCache.Acquire();
			AppendText(sb);
			return System.Text.StringBuilderCache.GetStringAndRelease(sb);
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			RemoveNodes();
			Add(value);
		}
	}

	public XElement(XName name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		this.name = name;
	}

	public XElement(XName name, object? content)
		: this(name)
	{
		AddContentSkipNotify(content);
	}

	public XElement(XName name, params object?[] content)
		: this(name, (object?)content)
	{
	}

	public XElement(XElement other)
		: base(other)
	{
		name = other.name;
		XAttribute xAttribute = other.lastAttr;
		if (xAttribute != null)
		{
			do
			{
				xAttribute = xAttribute.next;
				AppendAttributeSkipNotify(new XAttribute(xAttribute));
			}
			while (xAttribute != other.lastAttr);
		}
	}

	public XElement(XStreamingElement other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		name = other.name;
		AddContentSkipNotify(other.content);
	}

	internal XElement()
		: this("default")
	{
	}

	internal XElement(XmlReader r)
		: this(r, LoadOptions.None)
	{
	}

	private XElement(AsyncConstructionSentry s)
	{
	}

	internal XElement(XmlReader r, LoadOptions o)
	{
		ReadElementFrom(r, o);
	}

	internal static async Task<XElement> CreateAsync(XmlReader r, CancellationToken cancellationToken)
	{
		XElement xe = new XElement(default(AsyncConstructionSentry));
		await xe.ReadElementFromAsync(r, LoadOptions.None, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return xe;
	}

	public void Save(string fileName)
	{
		Save(fileName, GetSaveOptionsFromAnnotations());
	}

	public void Save(string fileName, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(fileName, xmlWriterSettings);
		Save(writer);
	}

	public IEnumerable<XElement> AncestorsAndSelf()
	{
		return GetAncestors(null, self: true);
	}

	public IEnumerable<XElement> AncestorsAndSelf(XName? name)
	{
		if (!(name != null))
		{
			return EmptySequence;
		}
		return GetAncestors(name, self: true);
	}

	public XAttribute? Attribute(XName name)
	{
		XAttribute xAttribute = lastAttr;
		if (xAttribute != null)
		{
			do
			{
				xAttribute = xAttribute.next;
				if (xAttribute.name == name)
				{
					return xAttribute;
				}
			}
			while (xAttribute != lastAttr);
		}
		return null;
	}

	public IEnumerable<XAttribute> Attributes()
	{
		return GetAttributes(null);
	}

	public IEnumerable<XAttribute> Attributes(XName? name)
	{
		if (!(name != null))
		{
			return XAttribute.EmptySequence;
		}
		return GetAttributes(name);
	}

	public IEnumerable<XNode> DescendantNodesAndSelf()
	{
		return GetDescendantNodes(self: true);
	}

	public IEnumerable<XElement> DescendantsAndSelf()
	{
		return GetDescendants(null, self: true);
	}

	public IEnumerable<XElement> DescendantsAndSelf(XName? name)
	{
		if (!(name != null))
		{
			return EmptySequence;
		}
		return GetDescendants(name, self: true);
	}

	public XNamespace GetDefaultNamespace()
	{
		string namespaceOfPrefixInScope = GetNamespaceOfPrefixInScope("xmlns", null);
		if (namespaceOfPrefixInScope == null)
		{
			return XNamespace.None;
		}
		return XNamespace.Get(namespaceOfPrefixInScope);
	}

	public XNamespace? GetNamespaceOfPrefix(string prefix)
	{
		if (prefix == null)
		{
			throw new ArgumentNullException("prefix");
		}
		if (prefix.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidPrefix, prefix));
		}
		if (prefix == "xmlns")
		{
			return XNamespace.Xmlns;
		}
		string namespaceOfPrefixInScope = GetNamespaceOfPrefixInScope(prefix, null);
		if (namespaceOfPrefixInScope != null)
		{
			return XNamespace.Get(namespaceOfPrefixInScope);
		}
		if (prefix == "xml")
		{
			return XNamespace.Xml;
		}
		return null;
	}

	public string? GetPrefixOfNamespace(XNamespace ns)
	{
		if (ns == null)
		{
			throw new ArgumentNullException("ns");
		}
		string namespaceName = ns.NamespaceName;
		bool flag = false;
		XElement xElement = this;
		do
		{
			XAttribute xAttribute = xElement.lastAttr;
			if (xAttribute != null)
			{
				bool flag2 = false;
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.IsNamespaceDeclaration)
					{
						if (xAttribute.Value == namespaceName && xAttribute.Name.NamespaceName.Length != 0 && (!flag || GetNamespaceOfPrefixInScope(xAttribute.Name.LocalName, xElement) == null))
						{
							return xAttribute.Name.LocalName;
						}
						flag2 = true;
					}
				}
				while (xAttribute != xElement.lastAttr);
				flag = flag || flag2;
			}
			xElement = xElement.parent as XElement;
		}
		while (xElement != null);
		if ((object)namespaceName == "http://www.w3.org/XML/1998/namespace")
		{
			if (!flag || GetNamespaceOfPrefixInScope("xml", null) == null)
			{
				return "xml";
			}
		}
		else if ((object)namespaceName == "http://www.w3.org/2000/xmlns/")
		{
			return "xmlns";
		}
		return null;
	}

	public static XElement Load(string uri)
	{
		return Load(uri, LoadOptions.None);
	}

	public static XElement Load(string uri, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(uri, xmlReaderSettings);
		return Load(reader, options);
	}

	public static XElement Load(Stream stream)
	{
		return Load(stream, LoadOptions.None);
	}

	public static XElement Load(Stream stream, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(stream, xmlReaderSettings);
		return Load(reader, options);
	}

	public static async Task<XElement> LoadAsync(Stream stream, LoadOptions options, CancellationToken cancellationToken)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		xmlReaderSettings.Async = true;
		using XmlReader r = XmlReader.Create(stream, xmlReaderSettings);
		return await LoadAsync(r, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static XElement Load(TextReader textReader)
	{
		return Load(textReader, LoadOptions.None);
	}

	public static XElement Load(TextReader textReader, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(textReader, xmlReaderSettings);
		return Load(reader, options);
	}

	public static async Task<XElement> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		xmlReaderSettings.Async = true;
		using XmlReader r = XmlReader.Create(textReader, xmlReaderSettings);
		return await LoadAsync(r, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static XElement Load(XmlReader reader)
	{
		return Load(reader, LoadOptions.None);
	}

	public static XElement Load(XmlReader reader, LoadOptions options)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.MoveToContent() != XmlNodeType.Element)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));
		}
		XElement result = new XElement(reader, options);
		reader.MoveToContent();
		if (!reader.EOF)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedEndOfFile);
		}
		return result;
	}

	public static Task<XElement> LoadAsync(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<XElement>(cancellationToken);
		}
		return LoadAsyncInternal(reader, options, cancellationToken);
	}

	private static async Task<XElement> LoadAsyncInternal(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
	{
		if (await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false) != XmlNodeType.Element)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));
		}
		XElement e = new XElement(default(AsyncConstructionSentry));
		await e.ReadElementFromAsync(reader, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		cancellationToken.ThrowIfCancellationRequested();
		await reader.MoveToContentAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (!reader.EOF)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedEndOfFile);
		}
		return e;
	}

	public static XElement Parse(string text)
	{
		return Parse(text, LoadOptions.None);
	}

	public static XElement Parse(string text, LoadOptions options)
	{
		using StringReader input = new StringReader(text);
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
		return Load(reader, options);
	}

	public void RemoveAll()
	{
		RemoveAttributes();
		RemoveNodes();
	}

	public void RemoveAttributes()
	{
		if (SkipNotify())
		{
			RemoveAttributesSkipNotify();
			return;
		}
		while (lastAttr != null)
		{
			XAttribute xAttribute = lastAttr.next;
			NotifyChanging(xAttribute, XObjectChangeEventArgs.Remove);
			if (lastAttr == null || xAttribute != lastAttr.next)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
			}
			if (xAttribute != lastAttr)
			{
				lastAttr.next = xAttribute.next;
			}
			else
			{
				lastAttr = null;
			}
			xAttribute.parent = null;
			xAttribute.next = null;
			NotifyChanged(xAttribute, XObjectChangeEventArgs.Remove);
		}
	}

	public void ReplaceAll(object? content)
	{
		content = XContainer.GetContentSnapshot(content);
		RemoveAll();
		Add(content);
	}

	public void ReplaceAll(params object?[] content)
	{
		ReplaceAll((object?)content);
	}

	public void ReplaceAttributes(object? content)
	{
		content = XContainer.GetContentSnapshot(content);
		RemoveAttributes();
		Add(content);
	}

	public void ReplaceAttributes(params object?[] content)
	{
		ReplaceAttributes((object?)content);
	}

	public void Save(Stream stream)
	{
		Save(stream, GetSaveOptionsFromAnnotations());
	}

	public void Save(Stream stream, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings);
		Save(writer);
	}

	public async Task SaveAsync(Stream stream, SaveOptions options, CancellationToken cancellationToken)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		xmlWriterSettings.Async = true;
		XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
		ConfiguredAsyncDisposable configuredAsyncDisposable = xmlWriter.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
			if (asyncDisposable != null)
			{
				await asyncDisposable.DisposeAsync();
			}
		}
	}

	public void Save(TextWriter textWriter)
	{
		Save(textWriter, GetSaveOptionsFromAnnotations());
	}

	public void Save(TextWriter textWriter, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(textWriter, xmlWriterSettings);
		Save(writer);
	}

	public async Task SaveAsync(TextWriter textWriter, SaveOptions options, CancellationToken cancellationToken)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		xmlWriterSettings.Async = true;
		XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings);
		ConfiguredAsyncDisposable configuredAsyncDisposable = xmlWriter.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			IAsyncDisposable asyncDisposable = configuredAsyncDisposable as IAsyncDisposable;
			if (asyncDisposable != null)
			{
				await asyncDisposable.DisposeAsync();
			}
		}
	}

	public void Save(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WriteStartDocument();
		WriteTo(writer);
		writer.WriteEndDocument();
	}

	public Task SaveAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return SaveAsyncInternal(writer, cancellationToken);
	}

	private async Task SaveAsyncInternal(XmlWriter writer, CancellationToken cancellationToken)
	{
		await writer.WriteStartDocumentAsync().ConfigureAwait(continueOnCapturedContext: false);
		await WriteToAsync(writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		cancellationToken.ThrowIfCancellationRequested();
		await writer.WriteEndDocumentAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public void SetAttributeValue(XName name, object? value)
	{
		XAttribute xAttribute = Attribute(name);
		if (value == null)
		{
			if (xAttribute != null)
			{
				RemoveAttribute(xAttribute);
			}
		}
		else if (xAttribute != null)
		{
			xAttribute.Value = XContainer.GetStringValue(value);
		}
		else
		{
			AppendAttribute(new XAttribute(name, value));
		}
	}

	public void SetElementValue(XName name, object? value)
	{
		XElement xElement = Element(name);
		if (value == null)
		{
			if (xElement != null)
			{
				RemoveNode(xElement);
			}
		}
		else if (xElement != null)
		{
			xElement.Value = XContainer.GetStringValue(value);
		}
		else
		{
			AddNode(new XElement(name, XContainer.GetStringValue(value)));
		}
	}

	public void SetValue(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Value = XContainer.GetStringValue(value);
	}

	public override void WriteTo(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		new ElementWriter(writer).WriteElement(this);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return new ElementWriter(writer).WriteElementAsync(this, cancellationToken);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator string?(XElement? element)
	{
		return element?.Value;
	}

	[CLSCompliant(false)]
	public static explicit operator bool(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToBoolean(element.Value.ToLowerInvariant());
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator bool?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToBoolean(element.Value.ToLowerInvariant());
	}

	[CLSCompliant(false)]
	public static explicit operator int(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToInt32(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator int?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToInt32(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToUInt32(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator uint?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToUInt32(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator long(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToInt64(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator long?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToInt64(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToUInt64(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator ulong?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToUInt64(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator float(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToSingle(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator float?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToSingle(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator double(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToDouble(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator double?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToDouble(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator decimal(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToDecimal(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator decimal?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToDecimal(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator DateTime(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return DateTime.Parse(element.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator DateTime?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return DateTime.Parse(element.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}

	[CLSCompliant(false)]
	public static explicit operator DateTimeOffset(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToDateTimeOffset(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator DateTimeOffset?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToDateTimeOffset(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator TimeSpan(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToTimeSpan(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator TimeSpan?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToTimeSpan(element.Value);
	}

	[CLSCompliant(false)]
	public static explicit operator Guid(XElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		return XmlConvert.ToGuid(element.Value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("element")]
	public static explicit operator Guid?(XElement? element)
	{
		if (element == null)
		{
			return null;
		}
		return XmlConvert.ToGuid(element.Value);
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (parent != null || annotations != null || content != null || lastAttr != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_DeserializeInstance);
		}
		if (reader.MoveToContent() != XmlNodeType.Element)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));
		}
		ReadElementFrom(reader, LoadOptions.None);
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		WriteTo(writer);
	}

	internal override void AddAttribute(XAttribute a)
	{
		if (Attribute(a.Name) != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_DuplicateAttribute);
		}
		if (a.parent != null)
		{
			a = new XAttribute(a);
		}
		AppendAttribute(a);
	}

	internal override void AddAttributeSkipNotify(XAttribute a)
	{
		if (Attribute(a.Name) != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_DuplicateAttribute);
		}
		if (a.parent != null)
		{
			a = new XAttribute(a);
		}
		AppendAttributeSkipNotify(a);
	}

	internal void AppendAttribute(XAttribute a)
	{
		bool flag = NotifyChanging(a, XObjectChangeEventArgs.Add);
		if (a.parent != null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		AppendAttributeSkipNotify(a);
		if (flag)
		{
			NotifyChanged(a, XObjectChangeEventArgs.Add);
		}
	}

	internal void AppendAttributeSkipNotify(XAttribute a)
	{
		a.parent = this;
		if (lastAttr == null)
		{
			a.next = a;
		}
		else
		{
			a.next = lastAttr.next;
			lastAttr.next = a;
		}
		lastAttr = a;
	}

	private bool AttributesEqual(XElement e)
	{
		XAttribute xAttribute = lastAttr;
		XAttribute xAttribute2 = e.lastAttr;
		if (xAttribute != null && xAttribute2 != null)
		{
			do
			{
				xAttribute = xAttribute.next;
				xAttribute2 = xAttribute2.next;
				if (xAttribute.name != xAttribute2.name || xAttribute.value != xAttribute2.value)
				{
					return false;
				}
			}
			while (xAttribute != lastAttr);
			return xAttribute2 == e.lastAttr;
		}
		if (xAttribute == null)
		{
			return xAttribute2 == null;
		}
		return false;
	}

	internal override XNode CloneNode()
	{
		return new XElement(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XElement xElement && name == xElement.name && ContentsEqual(xElement))
		{
			return AttributesEqual(xElement);
		}
		return false;
	}

	private IEnumerable<XAttribute> GetAttributes(XName name)
	{
		XAttribute a = lastAttr;
		if (a == null)
		{
			yield break;
		}
		do
		{
			a = a.next;
			if (name == null || a.name == name)
			{
				yield return a;
			}
		}
		while (a.parent == this && a != lastAttr);
	}

	private string GetNamespaceOfPrefixInScope(string prefix, XElement outOfScope)
	{
		for (XElement xElement = this; xElement != outOfScope; xElement = xElement.parent as XElement)
		{
			XAttribute xAttribute = xElement.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.IsNamespaceDeclaration && xAttribute.Name.LocalName == prefix)
					{
						return xAttribute.Value;
					}
				}
				while (xAttribute != xElement.lastAttr);
			}
		}
		return null;
	}

	internal override int GetDeepHashCode()
	{
		int hashCode = name.GetHashCode();
		hashCode ^= ContentsHashCode();
		XAttribute xAttribute = lastAttr;
		if (xAttribute != null)
		{
			do
			{
				xAttribute = xAttribute.next;
				hashCode ^= xAttribute.GetDeepHashCode();
			}
			while (xAttribute != lastAttr);
		}
		return hashCode;
	}

	private void ReadElementFrom(XmlReader r, LoadOptions o)
	{
		ReadElementFromImpl(r, o);
		if (!r.IsEmptyElement)
		{
			r.Read();
			ReadContentFrom(r, o);
		}
		r.Read();
	}

	private async Task ReadElementFromAsync(XmlReader r, LoadOptions o, CancellationToken cancellationTokentoken)
	{
		ReadElementFromImpl(r, o);
		if (!r.IsEmptyElement)
		{
			cancellationTokentoken.ThrowIfCancellationRequested();
			await r.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			await ReadContentFromAsync(r, o, cancellationTokentoken).ConfigureAwait(continueOnCapturedContext: false);
		}
		cancellationTokentoken.ThrowIfCancellationRequested();
		await r.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	private void ReadElementFromImpl(XmlReader r, LoadOptions o)
	{
		if (r.ReadState != ReadState.Interactive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		name = XNamespace.Get(r.NamespaceURI).GetName(r.LocalName);
		if ((o & LoadOptions.SetBaseUri) != 0)
		{
			string baseURI = r.BaseURI;
			if (!string.IsNullOrEmpty(baseURI))
			{
				SetBaseUri(baseURI);
			}
		}
		IXmlLineInfo xmlLineInfo = null;
		if ((o & LoadOptions.SetLineInfo) != 0)
		{
			xmlLineInfo = r as IXmlLineInfo;
			if (xmlLineInfo != null && xmlLineInfo.HasLineInfo())
			{
				SetLineInfo(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
			}
		}
		if (!r.MoveToFirstAttribute())
		{
			return;
		}
		do
		{
			XAttribute xAttribute = new XAttribute(XNamespace.Get((r.Prefix.Length == 0) ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value);
			if (xmlLineInfo != null && xmlLineInfo.HasLineInfo())
			{
				xAttribute.SetLineInfo(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
			}
			AppendAttributeSkipNotify(xAttribute);
		}
		while (r.MoveToNextAttribute());
		r.MoveToElement();
	}

	internal void RemoveAttribute(XAttribute a)
	{
		bool flag = NotifyChanging(a, XObjectChangeEventArgs.Remove);
		if (a.parent != this)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExternalCode);
		}
		XAttribute xAttribute = lastAttr;
		XAttribute xAttribute2;
		while ((xAttribute2 = xAttribute.next) != a)
		{
			xAttribute = xAttribute2;
		}
		if (xAttribute == a)
		{
			lastAttr = null;
		}
		else
		{
			if (lastAttr == a)
			{
				lastAttr = xAttribute;
			}
			xAttribute.next = a.next;
		}
		a.parent = null;
		a.next = null;
		if (flag)
		{
			NotifyChanged(a, XObjectChangeEventArgs.Remove);
		}
	}

	private void RemoveAttributesSkipNotify()
	{
		if (lastAttr != null)
		{
			XAttribute xAttribute = lastAttr;
			do
			{
				XAttribute xAttribute2 = xAttribute.next;
				xAttribute.parent = null;
				xAttribute.next = null;
				xAttribute = xAttribute2;
			}
			while (xAttribute != lastAttr);
			lastAttr = null;
		}
	}

	internal void SetEndElementLineInfo(int lineNumber, int linePosition)
	{
		AddAnnotation(new LineInfoEndElementAnnotation(lineNumber, linePosition));
	}

	internal override void ValidateNode(XNode node, XNode previous)
	{
		if (node is XDocument)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddNode, XmlNodeType.Document));
		}
		if (node is XDocumentType)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddNode, XmlNodeType.DocumentType));
		}
	}
}
