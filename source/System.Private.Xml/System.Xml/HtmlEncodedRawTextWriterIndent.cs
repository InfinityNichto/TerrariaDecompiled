using System.IO;

namespace System.Xml;

internal sealed class HtmlEncodedRawTextWriterIndent : HtmlEncodedRawTextWriter
{
	private int _indentLevel;

	private int _endBlockPos;

	private string _indentChars;

	private bool _newLineOnAttributes;

	public HtmlEncodedRawTextWriterIndent(TextWriter writer, XmlWriterSettings settings)
		: base(writer, settings)
	{
		Init(settings);
	}

	public HtmlEncodedRawTextWriterIndent(Stream stream, XmlWriterSettings settings)
		: base(stream, settings)
	{
		Init(settings);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		base.WriteDocType(name, pubid, sysid, subset);
		_endBlockPos = _bufPos;
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (_trackTextContent && _inTextContent)
		{
			ChangeTextContentMark(value: false);
		}
		_elementScope.Push((byte)_currentElementProperties);
		if (ns.Length == 0)
		{
			_currentElementProperties = (ElementProperties)HtmlEncodedRawTextWriter._elementPropertySearch.FindCaseInsensitiveString(localName);
			if (_endBlockPos == _bufPos && (_currentElementProperties & ElementProperties.BLOCK_WS) != 0)
			{
				WriteIndent();
			}
			_indentLevel++;
			_bufChars[_bufPos++] = '<';
		}
		else
		{
			_currentElementProperties = (ElementProperties)192u;
			if (_endBlockPos == _bufPos)
			{
				WriteIndent();
			}
			_indentLevel++;
			_bufChars[_bufPos++] = '<';
			if (prefix.Length != 0)
			{
				RawText(prefix);
				_bufChars[_bufPos++] = ':';
			}
		}
		RawText(localName);
		_attrEndPos = _bufPos;
	}

	internal override void StartElementContent()
	{
		_bufChars[_bufPos++] = '>';
		_contentPos = _bufPos;
		if ((_currentElementProperties & ElementProperties.HEAD) != 0)
		{
			WriteIndent();
			WriteMetaElement();
			_endBlockPos = _bufPos;
		}
		else if ((_currentElementProperties & ElementProperties.BLOCK_WS) != 0)
		{
			_endBlockPos = _bufPos;
		}
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		_indentLevel--;
		bool flag = (_currentElementProperties & ElementProperties.BLOCK_WS) != 0;
		if (flag && _endBlockPos == _bufPos && _contentPos != _bufPos)
		{
			WriteIndent();
		}
		base.WriteEndElement(prefix, localName, ns);
		_contentPos = 0;
		if (flag)
		{
			_endBlockPos = _bufPos;
		}
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (_newLineOnAttributes)
		{
			RawText(_newLineChars);
			_indentLevel++;
			WriteIndent();
			_indentLevel--;
		}
		base.WriteStartAttribute(prefix, localName, ns);
	}

	protected override void FlushBuffer()
	{
		_endBlockPos = ((_endBlockPos == _bufPos) ? 1 : 0);
		base.FlushBuffer();
	}

	private void Init(XmlWriterSettings settings)
	{
		_indentLevel = 0;
		_indentChars = settings.IndentChars;
		_newLineOnAttributes = settings.NewLineOnAttributes;
	}

	private void WriteIndent()
	{
		RawText(_newLineChars);
		for (int num = _indentLevel; num > 0; num--)
		{
			RawText(_indentChars);
		}
	}
}
