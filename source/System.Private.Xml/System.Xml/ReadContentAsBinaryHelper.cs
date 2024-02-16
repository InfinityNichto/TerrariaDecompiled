using System.Threading.Tasks;

namespace System.Xml;

internal sealed class ReadContentAsBinaryHelper
{
	private enum State
	{
		None,
		InReadContent,
		InReadElementContent
	}

	private readonly XmlReader _reader;

	private State _state;

	private int _valueOffset;

	private bool _isEnd;

	private readonly bool _canReadValueChunk;

	private readonly char[] _valueChunk;

	private int _valueChunkLength;

	private IncrementalReadDecoder _decoder;

	private Base64Decoder _base64Decoder;

	private BinHexDecoder _binHexDecoder;

	internal ReadContentAsBinaryHelper(XmlReader reader)
	{
		_reader = reader;
		_canReadValueChunk = reader.CanReadValueChunk;
		if (_canReadValueChunk)
		{
			_valueChunk = new char[256];
		}
	}

	internal static ReadContentAsBinaryHelper CreateOrReset(ReadContentAsBinaryHelper helper, XmlReader reader)
	{
		if (helper == null)
		{
			return new ReadContentAsBinaryHelper(reader);
		}
		helper.Reset();
		return helper;
	}

	internal int ReadContentAsBase64(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (!_reader.CanReadContentAs())
			{
				throw _reader.CreateReadContentAsException("ReadContentAsBase64");
			}
			if (!Init())
			{
				return 0;
			}
			break;
		case State.InReadContent:
			if (_decoder == _base64Decoder)
			{
				return ReadContentAsBinary(buffer, index, count);
			}
			break;
		case State.InReadElementContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
		InitBase64Decoder();
		return ReadContentAsBinary(buffer, index, count);
	}

	internal int ReadContentAsBinHex(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (!_reader.CanReadContentAs())
			{
				throw _reader.CreateReadContentAsException("ReadContentAsBinHex");
			}
			if (!Init())
			{
				return 0;
			}
			break;
		case State.InReadContent:
			if (_decoder == _binHexDecoder)
			{
				return ReadContentAsBinary(buffer, index, count);
			}
			break;
		case State.InReadElementContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
		InitBinHexDecoder();
		return ReadContentAsBinary(buffer, index, count);
	}

	internal int ReadElementContentAsBase64(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (_reader.NodeType != XmlNodeType.Element)
			{
				throw _reader.CreateReadElementContentAsException("ReadElementContentAsBase64");
			}
			if (!InitOnElement())
			{
				return 0;
			}
			break;
		case State.InReadContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		case State.InReadElementContent:
			if (_decoder == _base64Decoder)
			{
				return ReadElementContentAsBinary(buffer, index, count);
			}
			break;
		default:
			return 0;
		}
		InitBase64Decoder();
		return ReadElementContentAsBinary(buffer, index, count);
	}

	internal int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (_reader.NodeType != XmlNodeType.Element)
			{
				throw _reader.CreateReadElementContentAsException("ReadElementContentAsBinHex");
			}
			if (!InitOnElement())
			{
				return 0;
			}
			break;
		case State.InReadContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		case State.InReadElementContent:
			if (_decoder == _binHexDecoder)
			{
				return ReadElementContentAsBinary(buffer, index, count);
			}
			break;
		default:
			return 0;
		}
		InitBinHexDecoder();
		return ReadElementContentAsBinary(buffer, index, count);
	}

	internal void Finish()
	{
		if (_state != 0)
		{
			while (MoveToNextContentNode(moveIfOnContentNode: true))
			{
			}
			if (_state == State.InReadElementContent)
			{
				if (_reader.NodeType != XmlNodeType.EndElement)
				{
					throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
				}
				_reader.Read();
			}
		}
		Reset();
	}

	internal void Reset()
	{
		_state = State.None;
		_isEnd = false;
		_valueOffset = 0;
	}

	private bool Init()
	{
		if (!MoveToNextContentNode(moveIfOnContentNode: false))
		{
			return false;
		}
		_state = State.InReadContent;
		_isEnd = false;
		return true;
	}

	private bool InitOnElement()
	{
		bool isEmptyElement = _reader.IsEmptyElement;
		_reader.Read();
		if (isEmptyElement)
		{
			return false;
		}
		if (!MoveToNextContentNode(moveIfOnContentNode: false))
		{
			if (_reader.NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
			}
			_reader.Read();
			return false;
		}
		_state = State.InReadElementContent;
		_isEnd = false;
		return true;
	}

	private void InitBase64Decoder()
	{
		if (_base64Decoder == null)
		{
			_base64Decoder = new Base64Decoder();
		}
		else
		{
			_base64Decoder.Reset();
		}
		_decoder = _base64Decoder;
	}

	private void InitBinHexDecoder()
	{
		if (_binHexDecoder == null)
		{
			_binHexDecoder = new BinHexDecoder();
		}
		else
		{
			_binHexDecoder.Reset();
		}
		_decoder = _binHexDecoder;
	}

	private int ReadContentAsBinary(byte[] buffer, int index, int count)
	{
		if (_isEnd)
		{
			Reset();
			return 0;
		}
		_decoder.SetNextOutputBuffer(buffer, index, count);
		do
		{
			if (_canReadValueChunk)
			{
				while (true)
				{
					if (_valueOffset < _valueChunkLength)
					{
						int num = _decoder.Decode(_valueChunk, _valueOffset, _valueChunkLength - _valueOffset);
						_valueOffset += num;
					}
					if (_decoder.IsFull)
					{
						return _decoder.DecodedCount;
					}
					if ((_valueChunkLength = _reader.ReadValueChunk(_valueChunk, 0, 256)) == 0)
					{
						break;
					}
					_valueOffset = 0;
				}
			}
			else
			{
				string value = _reader.Value;
				int num2 = _decoder.Decode(value, _valueOffset, value.Length - _valueOffset);
				_valueOffset += num2;
				if (_decoder.IsFull)
				{
					return _decoder.DecodedCount;
				}
			}
			_valueOffset = 0;
		}
		while (MoveToNextContentNode(moveIfOnContentNode: true));
		_isEnd = true;
		return _decoder.DecodedCount;
	}

	private int ReadElementContentAsBinary(byte[] buffer, int index, int count)
	{
		if (count == 0)
		{
			return 0;
		}
		int num = ReadContentAsBinary(buffer, index, count);
		if (num > 0)
		{
			return num;
		}
		if (_reader.NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
		}
		_reader.Read();
		_state = State.None;
		return 0;
	}

	private bool MoveToNextContentNode(bool moveIfOnContentNode)
	{
		do
		{
			switch (_reader.NodeType)
			{
			case XmlNodeType.Attribute:
				return !moveIfOnContentNode;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (!moveIfOnContentNode)
				{
					return true;
				}
				goto IL_0078;
			case XmlNodeType.EntityReference:
				if (!_reader.CanResolveEntity)
				{
					break;
				}
				_reader.ResolveEntity();
				goto IL_0078;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.EndEntity:
				goto IL_0078;
			}
			return false;
			IL_0078:
			moveIfOnContentNode = false;
		}
		while (_reader.Read());
		return false;
	}

	internal async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (!_reader.CanReadContentAs())
			{
				throw _reader.CreateReadContentAsException("ReadContentAsBase64");
			}
			if (!(await InitAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			break;
		case State.InReadContent:
			if (_decoder == _base64Decoder)
			{
				return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case State.InReadElementContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
		InitBase64Decoder();
		return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (!_reader.CanReadContentAs())
			{
				throw _reader.CreateReadContentAsException("ReadContentAsBinHex");
			}
			if (!(await InitAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			break;
		case State.InReadContent:
			if (_decoder == _binHexDecoder)
			{
				return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case State.InReadElementContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
		InitBinHexDecoder();
		return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (_reader.NodeType != XmlNodeType.Element)
			{
				throw _reader.CreateReadElementContentAsException("ReadElementContentAsBase64");
			}
			if (!(await InitOnElementAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			break;
		case State.InReadContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		case State.InReadElementContent:
			if (_decoder == _base64Decoder)
			{
				return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		default:
			return 0;
		}
		InitBase64Decoder();
		return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
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
		switch (_state)
		{
		case State.None:
			if (_reader.NodeType != XmlNodeType.Element)
			{
				throw _reader.CreateReadElementContentAsException("ReadElementContentAsBinHex");
			}
			if (!(await InitOnElementAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			break;
		case State.InReadContent:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		case State.InReadElementContent:
			if (_decoder == _binHexDecoder)
			{
				return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		default:
			return 0;
		}
		InitBinHexDecoder();
		return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal async Task FinishAsync()
	{
		if (_state != 0)
		{
			while (await MoveToNextContentNodeAsync(moveIfOnContentNode: true).ConfigureAwait(continueOnCapturedContext: false))
			{
			}
			if (_state == State.InReadElementContent)
			{
				if (_reader.NodeType != XmlNodeType.EndElement)
				{
					throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
				}
				await _reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		Reset();
	}

	private async Task<bool> InitAsync()
	{
		if (!(await MoveToNextContentNodeAsync(moveIfOnContentNode: false).ConfigureAwait(continueOnCapturedContext: false)))
		{
			return false;
		}
		_state = State.InReadContent;
		_isEnd = false;
		return true;
	}

	private async Task<bool> InitOnElementAsync()
	{
		bool isEmpty = _reader.IsEmptyElement;
		await _reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (isEmpty)
		{
			return false;
		}
		if (!(await MoveToNextContentNodeAsync(moveIfOnContentNode: false).ConfigureAwait(continueOnCapturedContext: false)))
		{
			if (_reader.NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
			}
			await _reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return false;
		}
		_state = State.InReadElementContent;
		_isEnd = false;
		return true;
	}

	private async Task<int> ReadContentAsBinaryAsync(byte[] buffer, int index, int count)
	{
		if (_isEnd)
		{
			Reset();
			return 0;
		}
		_decoder.SetNextOutputBuffer(buffer, index, count);
		do
		{
			if (_canReadValueChunk)
			{
				while (true)
				{
					if (_valueOffset < _valueChunkLength)
					{
						int num = _decoder.Decode(_valueChunk, _valueOffset, _valueChunkLength - _valueOffset);
						_valueOffset += num;
					}
					if (_decoder.IsFull)
					{
						return _decoder.DecodedCount;
					}
					if ((_valueChunkLength = await _reader.ReadValueChunkAsync(_valueChunk, 0, 256).ConfigureAwait(continueOnCapturedContext: false)) == 0)
					{
						break;
					}
					_valueOffset = 0;
				}
			}
			else
			{
				string text = await _reader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false);
				int num2 = _decoder.Decode(text, _valueOffset, text.Length - _valueOffset);
				_valueOffset += num2;
				if (_decoder.IsFull)
				{
					return _decoder.DecodedCount;
				}
			}
			_valueOffset = 0;
		}
		while (await MoveToNextContentNodeAsync(moveIfOnContentNode: true).ConfigureAwait(continueOnCapturedContext: false));
		_isEnd = true;
		return _decoder.DecodedCount;
	}

	private async Task<int> ReadElementContentAsBinaryAsync(byte[] buffer, int index, int count)
	{
		if (count == 0)
		{
			return 0;
		}
		int num = await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		if (num > 0)
		{
			return num;
		}
		if (_reader.NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
		}
		await _reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		_state = State.None;
		return 0;
	}

	private async Task<bool> MoveToNextContentNodeAsync(bool moveIfOnContentNode)
	{
		do
		{
			switch (_reader.NodeType)
			{
			case XmlNodeType.Attribute:
				return !moveIfOnContentNode;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (!moveIfOnContentNode)
				{
					return true;
				}
				goto IL_00a5;
			case XmlNodeType.EntityReference:
				if (!_reader.CanResolveEntity)
				{
					break;
				}
				_reader.ResolveEntity();
				goto IL_00a5;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.EndEntity:
				goto IL_00a5;
			}
			return false;
			IL_00a5:
			moveIfOnContentNode = false;
		}
		while (await _reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false));
		return false;
	}
}
