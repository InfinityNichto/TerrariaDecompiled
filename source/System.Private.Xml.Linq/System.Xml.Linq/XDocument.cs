using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XDocument : XContainer
{
	private XDeclaration _declaration;

	public XDeclaration? Declaration
	{
		get
		{
			return _declaration;
		}
		set
		{
			_declaration = value;
		}
	}

	public XDocumentType? DocumentType => GetFirstNode<XDocumentType>();

	public override XmlNodeType NodeType => XmlNodeType.Document;

	public XElement? Root => GetFirstNode<XElement>();

	public XDocument()
	{
	}

	public XDocument(params object?[] content)
		: this()
	{
		AddContentSkipNotify(content);
	}

	public XDocument(XDeclaration? declaration, params object?[] content)
		: this(content)
	{
		_declaration = declaration;
	}

	public XDocument(XDocument other)
		: base(other)
	{
		if (other._declaration != null)
		{
			_declaration = new XDeclaration(other._declaration);
		}
	}

	public static XDocument Load(string uri)
	{
		return Load(uri, LoadOptions.None);
	}

	public static XDocument Load(string uri, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(uri, xmlReaderSettings);
		return Load(reader, options);
	}

	public static XDocument Load(Stream stream)
	{
		return Load(stream, LoadOptions.None);
	}

	public static XDocument Load(Stream stream, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(stream, xmlReaderSettings);
		return Load(reader, options);
	}

	public static async Task<XDocument> LoadAsync(Stream stream, LoadOptions options, CancellationToken cancellationToken)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		xmlReaderSettings.Async = true;
		using XmlReader r = XmlReader.Create(stream, xmlReaderSettings);
		return await LoadAsync(r, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static XDocument Load(TextReader textReader)
	{
		return Load(textReader, LoadOptions.None);
	}

	public static XDocument Load(TextReader textReader, LoadOptions options)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(textReader, xmlReaderSettings);
		return Load(reader, options);
	}

	public static async Task<XDocument> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken)
	{
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		xmlReaderSettings.Async = true;
		using XmlReader r = XmlReader.Create(textReader, xmlReaderSettings);
		return await LoadAsync(r, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static XDocument Load(XmlReader reader)
	{
		return Load(reader, LoadOptions.None);
	}

	public static XDocument Load(XmlReader reader, LoadOptions options)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader.ReadState == ReadState.Initial)
		{
			reader.Read();
		}
		XDocument xDocument = InitLoad(reader, options);
		xDocument.ReadContentFrom(reader, options);
		if (!reader.EOF)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedEndOfFile);
		}
		if (xDocument.Root == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingRoot);
		}
		return xDocument;
	}

	public static Task<XDocument> LoadAsync(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<XDocument>(cancellationToken);
		}
		return LoadAsyncInternal(reader, options, cancellationToken);
	}

	private static async Task<XDocument> LoadAsyncInternal(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
	{
		if (reader.ReadState == ReadState.Initial)
		{
			await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		XDocument d = InitLoad(reader, options);
		await d.ReadContentFromAsync(reader, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!reader.EOF)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedEndOfFile);
		}
		if (d.Root == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingRoot);
		}
		return d;
	}

	private static XDocument InitLoad(XmlReader reader, LoadOptions options)
	{
		XDocument xDocument = new XDocument();
		if ((options & LoadOptions.SetBaseUri) != 0)
		{
			string baseURI = reader.BaseURI;
			if (!string.IsNullOrEmpty(baseURI))
			{
				xDocument.SetBaseUri(baseURI);
			}
		}
		if ((options & LoadOptions.SetLineInfo) != 0 && reader is IXmlLineInfo xmlLineInfo && xmlLineInfo.HasLineInfo())
		{
			xDocument.SetLineInfo(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
		}
		if (reader.NodeType == XmlNodeType.XmlDeclaration)
		{
			xDocument.Declaration = new XDeclaration(reader);
		}
		return xDocument;
	}

	public static XDocument Parse(string text)
	{
		return Parse(text, LoadOptions.None);
	}

	public static XDocument Parse(string text, LoadOptions options)
	{
		using StringReader input = new StringReader(text);
		XmlReaderSettings xmlReaderSettings = XNode.GetXmlReaderSettings(options);
		using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
		return Load(reader, options);
	}

	public void Save(Stream stream)
	{
		Save(stream, GetSaveOptionsFromAnnotations());
	}

	public void Save(Stream stream, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		if (_declaration != null && !string.IsNullOrEmpty(_declaration.Encoding))
		{
			try
			{
				xmlWriterSettings.Encoding = Encoding.GetEncoding(_declaration.Encoding);
			}
			catch (ArgumentException)
			{
			}
		}
		using XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings);
		Save(writer);
	}

	public async Task SaveAsync(Stream stream, SaveOptions options, CancellationToken cancellationToken)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		xmlWriterSettings.Async = true;
		if (_declaration != null && !string.IsNullOrEmpty(_declaration.Encoding))
		{
			try
			{
				xmlWriterSettings.Encoding = Encoding.GetEncoding(_declaration.Encoding);
			}
			catch (ArgumentException)
			{
			}
		}
		XmlWriter w = XmlWriter.Create(stream, xmlWriterSettings);
		ConfiguredAsyncDisposable configuredAsyncDisposable = w.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await WriteToAsync(w, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await w.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
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

	public void Save(XmlWriter writer)
	{
		WriteTo(writer);
	}

	public async Task SaveAsync(TextWriter textWriter, SaveOptions options, CancellationToken cancellationToken)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		xmlWriterSettings.Async = true;
		XmlWriter w = XmlWriter.Create(textWriter, xmlWriterSettings);
		ConfiguredAsyncDisposable configuredAsyncDisposable = w.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await WriteToAsync(w, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await w.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
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

	public void Save(string fileName)
	{
		Save(fileName, GetSaveOptionsFromAnnotations());
	}

	public Task SaveAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		return WriteToAsync(writer, cancellationToken);
	}

	public void Save(string fileName, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		if (_declaration != null && !string.IsNullOrEmpty(_declaration.Encoding))
		{
			try
			{
				xmlWriterSettings.Encoding = Encoding.GetEncoding(_declaration.Encoding);
			}
			catch (ArgumentException)
			{
			}
		}
		using XmlWriter writer = XmlWriter.Create(fileName, xmlWriterSettings);
		Save(writer);
	}

	public override void WriteTo(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (_declaration != null && _declaration.Standalone == "yes")
		{
			writer.WriteStartDocument(standalone: true);
		}
		else if (_declaration != null && _declaration.Standalone == "no")
		{
			writer.WriteStartDocument(standalone: false);
		}
		else
		{
			writer.WriteStartDocument();
		}
		WriteContentTo(writer);
		writer.WriteEndDocument();
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
		return WriteToAsyncInternal(writer, cancellationToken);
	}

	private async Task WriteToAsyncInternal(XmlWriter writer, CancellationToken cancellationToken)
	{
		Task task = ((_declaration != null && _declaration.Standalone == "yes") ? writer.WriteStartDocumentAsync(standalone: true) : ((_declaration == null || !(_declaration.Standalone == "no")) ? writer.WriteStartDocumentAsync() : writer.WriteStartDocumentAsync(standalone: false)));
		await task.ConfigureAwait(continueOnCapturedContext: false);
		await WriteContentToAsync(writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		await writer.WriteEndDocumentAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	internal override void AddAttribute(XAttribute a)
	{
		throw new ArgumentException(System.SR.Argument_AddAttribute);
	}

	internal override void AddAttributeSkipNotify(XAttribute a)
	{
		throw new ArgumentException(System.SR.Argument_AddAttribute);
	}

	internal override XNode CloneNode()
	{
		return new XDocument(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XDocument e)
		{
			return ContentsEqual(e);
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return ContentsHashCode();
	}

	private T GetFirstNode<T>() where T : XNode
	{
		XNode xNode = content as XNode;
		if (xNode != null)
		{
			do
			{
				xNode = xNode.next;
				if (xNode is T result)
				{
					return result;
				}
			}
			while (xNode != content);
		}
		return null;
	}

	internal static bool IsWhitespace(string s)
	{
		foreach (char c in s)
		{
			if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
			{
				return false;
			}
		}
		return true;
	}

	internal override void ValidateNode(XNode node, XNode previous)
	{
		switch (node.NodeType)
		{
		case XmlNodeType.Text:
			ValidateString(((XText)node).Value);
			break;
		case XmlNodeType.Element:
			ValidateDocument(previous, XmlNodeType.DocumentType, XmlNodeType.None);
			break;
		case XmlNodeType.DocumentType:
			ValidateDocument(previous, XmlNodeType.None, XmlNodeType.Element);
			break;
		case XmlNodeType.CDATA:
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddNode, XmlNodeType.CDATA));
		case XmlNodeType.Document:
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddNode, XmlNodeType.Document));
		}
	}

	private void ValidateDocument(XNode previous, XmlNodeType allowBefore, XmlNodeType allowAfter)
	{
		XNode xNode = content as XNode;
		if (xNode == null)
		{
			return;
		}
		if (previous == null)
		{
			allowBefore = allowAfter;
		}
		do
		{
			xNode = xNode.next;
			XmlNodeType nodeType = xNode.NodeType;
			if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.DocumentType)
			{
				if (nodeType != allowBefore)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_DocumentStructure);
				}
				allowBefore = XmlNodeType.None;
			}
			if (xNode == previous)
			{
				allowBefore = allowAfter;
			}
		}
		while (xNode != content);
	}

	internal override void ValidateString(string s)
	{
		if (!IsWhitespace(s))
		{
			throw new ArgumentException(System.SR.Argument_AddNonWhitespace);
		}
	}
}
