using System.Threading.Tasks;

namespace System.Xml;

internal class XmlCharCheckingReader : XmlWrappingReader
{
	private enum State
	{
		Initial,
		InReadBinary,
		Error,
		Interactive
	}

	private State _state;

	private readonly bool _checkCharacters;

	private readonly bool _ignoreWhitespace;

	private readonly bool _ignoreComments;

	private readonly bool _ignorePis;

	private readonly DtdProcessing _dtdProcessing;

	private XmlNodeType _lastNodeType;

	private ReadContentAsBinaryHelper _readBinaryHelper;

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings settings = reader.Settings;
			settings = ((settings != null) ? settings.Clone() : new XmlReaderSettings());
			if (_checkCharacters)
			{
				settings.CheckCharacters = true;
			}
			if (_ignoreWhitespace)
			{
				settings.IgnoreWhitespace = true;
			}
			if (_ignoreComments)
			{
				settings.IgnoreComments = true;
			}
			if (_ignorePis)
			{
				settings.IgnoreProcessingInstructions = true;
			}
			if (_dtdProcessing != (DtdProcessing)(-1))
			{
				settings.DtdProcessing = _dtdProcessing;
			}
			settings.ReadOnly = true;
			return settings;
		}
	}

	public override ReadState ReadState
	{
		get
		{
			switch (_state)
			{
			case State.Initial:
				if (reader.ReadState != ReadState.Closed)
				{
					return ReadState.Initial;
				}
				return ReadState.Closed;
			case State.Error:
				return ReadState.Error;
			default:
				return reader.ReadState;
			}
		}
	}

	public override bool CanReadBinaryContent => true;

	internal XmlCharCheckingReader(XmlReader reader, bool checkCharacters, bool ignoreWhitespace, bool ignoreComments, bool ignorePis, DtdProcessing dtdProcessing)
		: base(reader)
	{
		_state = State.Initial;
		_checkCharacters = checkCharacters;
		_ignoreWhitespace = ignoreWhitespace;
		_ignoreComments = ignoreComments;
		_ignorePis = ignorePis;
		_dtdProcessing = dtdProcessing;
		_lastNodeType = XmlNodeType.None;
	}

	public override bool MoveToAttribute(string name)
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.MoveToAttribute(name);
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.MoveToAttribute(name, ns);
	}

	public override void MoveToAttribute(int i)
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		reader.MoveToAttribute(i);
	}

	public override bool MoveToFirstAttribute()
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.MoveToFirstAttribute();
	}

	public override bool MoveToNextAttribute()
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.MoveToNextAttribute();
	}

	public override bool MoveToElement()
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.MoveToElement();
	}

	public override bool Read()
	{
		switch (_state)
		{
		case State.Initial:
			_state = State.Interactive;
			if (reader.ReadState != 0)
			{
				break;
			}
			goto case State.Interactive;
		case State.Error:
			return false;
		case State.InReadBinary:
			FinishReadBinary();
			_state = State.Interactive;
			goto case State.Interactive;
		case State.Interactive:
			if (!reader.Read())
			{
				return false;
			}
			break;
		default:
			return false;
		}
		XmlNodeType nodeType = reader.NodeType;
		if (!_checkCharacters)
		{
			switch (nodeType)
			{
			case XmlNodeType.Comment:
				if (_ignoreComments)
				{
					return Read();
				}
				break;
			case XmlNodeType.Whitespace:
				if (_ignoreWhitespace)
				{
					return Read();
				}
				break;
			case XmlNodeType.ProcessingInstruction:
				if (_ignorePis)
				{
					return Read();
				}
				break;
			case XmlNodeType.DocumentType:
				if (_dtdProcessing == DtdProcessing.Prohibit)
				{
					Throw(System.SR.Xml_DtdIsProhibitedEx, string.Empty);
				}
				else if (_dtdProcessing == DtdProcessing.Ignore)
				{
					return Read();
				}
				break;
			}
			return true;
		}
		switch (nodeType)
		{
		case XmlNodeType.Element:
			if (!_checkCharacters)
			{
				break;
			}
			ValidateQName(reader.Prefix, reader.LocalName);
			if (reader.MoveToFirstAttribute())
			{
				do
				{
					ValidateQName(reader.Prefix, reader.LocalName);
					CheckCharacters(reader.Value);
				}
				while (reader.MoveToNextAttribute());
				reader.MoveToElement();
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
			if (_checkCharacters)
			{
				CheckCharacters(reader.Value);
			}
			break;
		case XmlNodeType.EntityReference:
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
			}
			break;
		case XmlNodeType.ProcessingInstruction:
			if (_ignorePis)
			{
				return Read();
			}
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
				CheckCharacters(reader.Value);
			}
			break;
		case XmlNodeType.Comment:
			if (_ignoreComments)
			{
				return Read();
			}
			if (_checkCharacters)
			{
				CheckCharacters(reader.Value);
			}
			break;
		case XmlNodeType.DocumentType:
			if (_dtdProcessing == DtdProcessing.Prohibit)
			{
				Throw(System.SR.Xml_DtdIsProhibitedEx, string.Empty);
			}
			else if (_dtdProcessing == DtdProcessing.Ignore)
			{
				return Read();
			}
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
				CheckCharacters(reader.Value);
				string attribute = reader.GetAttribute("SYSTEM");
				if (attribute != null)
				{
					CheckCharacters(attribute);
				}
				attribute = reader.GetAttribute("PUBLIC");
				int invCharIndex;
				if (attribute != null && (invCharIndex = XmlCharType.IsPublicId(attribute)) >= 0)
				{
					Throw(System.SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(attribute, invCharIndex));
				}
			}
			break;
		case XmlNodeType.Whitespace:
			if (_ignoreWhitespace)
			{
				return Read();
			}
			if (_checkCharacters)
			{
				CheckWhitespace(reader.Value);
			}
			break;
		case XmlNodeType.SignificantWhitespace:
			if (_checkCharacters)
			{
				CheckWhitespace(reader.Value);
			}
			break;
		case XmlNodeType.EndElement:
			if (_checkCharacters)
			{
				ValidateQName(reader.Prefix, reader.LocalName);
			}
			break;
		}
		_lastNodeType = nodeType;
		return true;
	}

	public override bool ReadAttributeValue()
	{
		if (_state == State.InReadBinary)
		{
			FinishReadBinary();
		}
		return reader.ReadAttributeValue();
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return base.ReadContentAsBase64(buffer, index, count);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return base.ReadContentAsBase64(buffer, index, count);
		}
		_state = State.Interactive;
		int result = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return base.ReadContentAsBinHex(buffer, index, count);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return base.ReadContentAsBinHex(buffer, index, count);
		}
		_state = State.Interactive;
		int result = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return base.ReadElementContentAsBase64(buffer, index, count);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return base.ReadElementContentAsBase64(buffer, index, count);
		}
		_state = State.Interactive;
		int result = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);
		_state = State.InReadBinary;
		return result;
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return base.ReadElementContentAsBinHex(buffer, index, count);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return base.ReadElementContentAsBinHex(buffer, index, count);
		}
		_state = State.Interactive;
		int result = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);
		_state = State.InReadBinary;
		return result;
	}

	private void Throw(string res, string arg)
	{
		_state = State.Error;
		throw new XmlException(res, arg, (IXmlLineInfo)null);
	}

	private void Throw(string res, string[] args)
	{
		_state = State.Error;
		throw new XmlException(res, args, null);
	}

	private void CheckWhitespace(string value)
	{
		int invCharIndex;
		if ((invCharIndex = XmlCharType.IsOnlyWhitespaceWithPos(value)) != -1)
		{
			Throw(System.SR.Xml_InvalidWhitespaceCharacter, XmlException.BuildCharExceptionArgs(value, invCharIndex));
		}
	}

	private void ValidateQName(string name)
	{
		ValidateNames.ParseQNameThrow(name);
	}

	private void ValidateQName(string prefix, string localName)
	{
		try
		{
			if (prefix.Length > 0)
			{
				ValidateNames.ParseNCNameThrow(prefix);
			}
			ValidateNames.ParseNCNameThrow(localName);
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	private void CheckCharacters(string value)
	{
		XmlConvert.VerifyCharData(value, ExceptionType.ArgumentException, ExceptionType.XmlException);
	}

	private void FinishReadBinary()
	{
		_state = State.Interactive;
		if (_readBinaryHelper != null)
		{
			_readBinaryHelper.Finish();
		}
	}

	public override async Task<bool> ReadAsync()
	{
		switch (_state)
		{
		case State.Initial:
			_state = State.Interactive;
			if (reader.ReadState != 0)
			{
				break;
			}
			goto case State.Interactive;
		case State.Error:
			return false;
		case State.InReadBinary:
			await FinishReadBinaryAsync().ConfigureAwait(continueOnCapturedContext: false);
			_state = State.Interactive;
			goto case State.Interactive;
		case State.Interactive:
			if (!(await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return false;
			}
			break;
		default:
			return false;
		}
		XmlNodeType nodeType = reader.NodeType;
		if (!_checkCharacters)
		{
			switch (nodeType)
			{
			case XmlNodeType.Comment:
				if (_ignoreComments)
				{
					return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.Whitespace:
				if (_ignoreWhitespace)
				{
					return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.ProcessingInstruction:
				if (_ignorePis)
				{
					return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			case XmlNodeType.DocumentType:
				if (_dtdProcessing == DtdProcessing.Prohibit)
				{
					Throw(System.SR.Xml_DtdIsProhibitedEx, string.Empty);
				}
				else if (_dtdProcessing == DtdProcessing.Ignore)
				{
					return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				break;
			}
			return true;
		}
		switch (nodeType)
		{
		case XmlNodeType.Element:
			if (!_checkCharacters)
			{
				break;
			}
			ValidateQName(reader.Prefix, reader.LocalName);
			if (reader.MoveToFirstAttribute())
			{
				do
				{
					ValidateQName(reader.Prefix, reader.LocalName);
					CheckCharacters(reader.Value);
				}
				while (reader.MoveToNextAttribute());
				reader.MoveToElement();
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
			if (_checkCharacters)
			{
				CheckCharacters(await reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
			}
			break;
		case XmlNodeType.EntityReference:
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
			}
			break;
		case XmlNodeType.ProcessingInstruction:
			if (_ignorePis)
			{
				return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
				CheckCharacters(reader.Value);
			}
			break;
		case XmlNodeType.Comment:
			if (_ignoreComments)
			{
				return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_checkCharacters)
			{
				CheckCharacters(reader.Value);
			}
			break;
		case XmlNodeType.DocumentType:
			if (_dtdProcessing == DtdProcessing.Prohibit)
			{
				Throw(System.SR.Xml_DtdIsProhibitedEx, string.Empty);
			}
			else if (_dtdProcessing == DtdProcessing.Ignore)
			{
				return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_checkCharacters)
			{
				ValidateQName(reader.Name);
				CheckCharacters(reader.Value);
				string attribute = reader.GetAttribute("SYSTEM");
				if (attribute != null)
				{
					CheckCharacters(attribute);
				}
				attribute = reader.GetAttribute("PUBLIC");
				int invCharIndex;
				if (attribute != null && (invCharIndex = XmlCharType.IsPublicId(attribute)) >= 0)
				{
					Throw(System.SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(attribute, invCharIndex));
				}
			}
			break;
		case XmlNodeType.Whitespace:
			if (_ignoreWhitespace)
			{
				return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_checkCharacters)
			{
				CheckWhitespace(await reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
			}
			break;
		case XmlNodeType.SignificantWhitespace:
			if (_checkCharacters)
			{
				CheckWhitespace(await reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
			}
			break;
		case XmlNodeType.EndElement:
			if (_checkCharacters)
			{
				ValidateQName(reader.Prefix, reader.LocalName);
			}
			break;
		}
		_lastNodeType = nodeType;
		return true;
	}

	public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return await base.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return await base.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		_state = State.Interactive;
		int result = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_state = State.InReadBinary;
		return result;
	}

	public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return await base.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return await base.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		_state = State.Interactive;
		int result = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_state = State.InReadBinary;
		return result;
	}

	public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return await base.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return await base.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		_state = State.Interactive;
		int result = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_state = State.InReadBinary;
		return result;
	}

	public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_state != State.InReadBinary)
		{
			if (base.CanReadBinaryContent && !_checkCharacters)
			{
				_readBinaryHelper = null;
				_state = State.InReadBinary;
				return await base.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		else if (_readBinaryHelper == null)
		{
			return await base.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		}
		_state = State.Interactive;
		int result = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_state = State.InReadBinary;
		return result;
	}

	private async Task FinishReadBinaryAsync()
	{
		_state = State.Interactive;
		if (_readBinaryHelper != null)
		{
			await _readBinaryHelper.FinishAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
