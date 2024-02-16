using System.IO;
using System.Threading.Tasks;

namespace System.Xml;

internal class XmlEncodedRawTextWriterIndent : XmlEncodedRawTextWriter
{
	protected int _indentLevel;

	protected bool _newLineOnAttributes;

	protected string _indentChars;

	protected bool _mixedContent;

	private BitStack _mixedContentStack;

	protected ConformanceLevel _conformanceLevel;

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings settings = base.Settings;
			settings.ReadOnly = false;
			settings.Indent = true;
			settings.IndentChars = _indentChars;
			settings.NewLineOnAttributes = _newLineOnAttributes;
			settings.ReadOnly = true;
			return settings;
		}
	}

	public XmlEncodedRawTextWriterIndent(TextWriter writer, XmlWriterSettings settings)
		: base(writer, settings)
	{
		Init(settings);
	}

	public XmlEncodedRawTextWriterIndent(Stream stream, XmlWriterSettings settings)
		: base(stream, settings)
	{
		Init(settings);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		if (!_mixedContent && _textPos != _bufPos)
		{
			WriteIndent();
		}
		base.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (!_mixedContent && _textPos != _bufPos)
		{
			WriteIndent();
		}
		_indentLevel++;
		_mixedContentStack.PushBit(_mixedContent);
		base.WriteStartElement(prefix, localName, ns);
	}

	internal override void StartElementContent()
	{
		if (_indentLevel == 1 && _conformanceLevel == ConformanceLevel.Document)
		{
			_mixedContent = false;
		}
		else
		{
			_mixedContent = _mixedContentStack.PeekBit();
		}
		base.StartElementContent();
	}

	internal override void OnRootElement(ConformanceLevel currentConformanceLevel)
	{
		_conformanceLevel = currentConformanceLevel;
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		_indentLevel--;
		if (!_mixedContent && _contentPos != _bufPos && _textPos != _bufPos)
		{
			WriteIndent();
		}
		_mixedContent = _mixedContentStack.PopBit();
		base.WriteEndElement(prefix, localName, ns);
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		_indentLevel--;
		if (!_mixedContent && _contentPos != _bufPos && _textPos != _bufPos)
		{
			WriteIndent();
		}
		_mixedContent = _mixedContentStack.PopBit();
		base.WriteFullEndElement(prefix, localName, ns);
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (_newLineOnAttributes)
		{
			WriteIndent();
		}
		base.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteCData(string text)
	{
		_mixedContent = true;
		base.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		if (!_mixedContent && _textPos != _bufPos)
		{
			WriteIndent();
		}
		base.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string target, string text)
	{
		if (!_mixedContent && _textPos != _bufPos)
		{
			WriteIndent();
		}
		base.WriteProcessingInstruction(target, text);
	}

	public override void WriteEntityRef(string name)
	{
		_mixedContent = true;
		base.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		_mixedContent = true;
		base.WriteCharEntity(ch);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		_mixedContent = true;
		base.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteWhitespace(string ws)
	{
		_mixedContent = true;
		base.WriteWhitespace(ws);
	}

	public override void WriteString(string text)
	{
		_mixedContent = true;
		base.WriteString(text);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		_mixedContent = true;
		base.WriteChars(buffer, index, count);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		_mixedContent = true;
		base.WriteRaw(buffer, index, count);
	}

	public override void WriteRaw(string data)
	{
		_mixedContent = true;
		base.WriteRaw(data);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		_mixedContent = true;
		base.WriteBase64(buffer, index, count);
	}

	private void Init(XmlWriterSettings settings)
	{
		_indentLevel = 0;
		_indentChars = settings.IndentChars;
		_newLineOnAttributes = settings.NewLineOnAttributes;
		_mixedContentStack = new BitStack();
		if (!_checkCharacters)
		{
			return;
		}
		if (_newLineOnAttributes)
		{
			ValidateContentChars(_indentChars, "IndentChars", allowOnlyWhitespace: true);
			ValidateContentChars(_newLineChars, "NewLineChars", allowOnlyWhitespace: true);
			return;
		}
		ValidateContentChars(_indentChars, "IndentChars", allowOnlyWhitespace: false);
		if (_newLineHandling != 0)
		{
			ValidateContentChars(_newLineChars, "NewLineChars", allowOnlyWhitespace: false);
		}
	}

	private void WriteIndent()
	{
		RawText(_newLineChars);
		for (int num = _indentLevel; num > 0; num--)
		{
			RawText(_indentChars);
		}
	}

	public override async Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
	{
		CheckAsyncCall();
		if (!_mixedContent && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		await base.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteStartElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (!_mixedContent && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_indentLevel++;
		_mixedContentStack.PushBit(_mixedContent);
		await base.WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal override async Task WriteEndElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		_indentLevel--;
		if (!_mixedContent && _contentPos != _bufPos && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_mixedContent = _mixedContentStack.PopBit();
		await base.WriteEndElementAsync(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal override async Task WriteFullEndElementAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		_indentLevel--;
		if (!_mixedContent && _contentPos != _bufPos && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_mixedContent = _mixedContentStack.PopBit();
		await base.WriteFullEndElementAsync(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
	}

	protected internal override async Task WriteStartAttributeAsync(string prefix, string localName, string ns)
	{
		CheckAsyncCall();
		if (_newLineOnAttributes)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		await base.WriteStartAttributeAsync(prefix, localName, ns).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override Task WriteCDataAsync(string text)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteCDataAsync(text);
	}

	public override async Task WriteCommentAsync(string text)
	{
		CheckAsyncCall();
		if (!_mixedContent && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		await base.WriteCommentAsync(text).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task WriteProcessingInstructionAsync(string target, string text)
	{
		CheckAsyncCall();
		if (!_mixedContent && _textPos != _bufPos)
		{
			await WriteIndentAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		await base.WriteProcessingInstructionAsync(target, text).ConfigureAwait(continueOnCapturedContext: false);
	}

	public override Task WriteEntityRefAsync(string name)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteEntityRefAsync(name);
	}

	public override Task WriteCharEntityAsync(char ch)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteCharEntityAsync(ch);
	}

	public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteSurrogateCharEntityAsync(lowChar, highChar);
	}

	public override Task WriteWhitespaceAsync(string ws)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteWhitespaceAsync(ws);
	}

	public override Task WriteStringAsync(string text)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteStringAsync(text);
	}

	public override Task WriteCharsAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteCharsAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(char[] buffer, int index, int count)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteRawAsync(buffer, index, count);
	}

	public override Task WriteRawAsync(string data)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteRawAsync(data);
	}

	public override Task WriteBase64Async(byte[] buffer, int index, int count)
	{
		CheckAsyncCall();
		_mixedContent = true;
		return base.WriteBase64Async(buffer, index, count);
	}

	private async Task WriteIndentAsync()
	{
		CheckAsyncCall();
		await RawTextAsync(_newLineChars).ConfigureAwait(continueOnCapturedContext: false);
		for (int i = _indentLevel; i > 0; i--)
		{
			await RawTextAsync(_indentChars).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
