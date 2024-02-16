using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public ref struct Utf8JsonReader
{
	private readonly struct PartialStateForRollback
	{
		public readonly long _prevTotalConsumed;

		public readonly long _prevBytePositionInLine;

		public readonly int _prevConsumed;

		public readonly SequencePosition _prevCurrentPosition;

		public PartialStateForRollback(long totalConsumed, long bytePositionInLine, int consumed, SequencePosition currentPosition)
		{
			_prevTotalConsumed = totalConsumed;
			_prevBytePositionInLine = bytePositionInLine;
			_prevConsumed = consumed;
			_prevCurrentPosition = currentPosition;
		}

		public SequencePosition GetStartPosition(int offset = 0)
		{
			return new SequencePosition(_prevCurrentPosition.GetObject(), _prevCurrentPosition.GetInteger() + _prevConsumed + offset);
		}
	}

	private ReadOnlySpan<byte> _buffer;

	private readonly bool _isFinalBlock;

	private readonly bool _isInputSequence;

	private long _lineNumber;

	private long _bytePositionInLine;

	private int _consumed;

	private bool _inObject;

	private bool _isNotPrimitive;

	private JsonTokenType _tokenType;

	private JsonTokenType _previousTokenType;

	private JsonReaderOptions _readerOptions;

	private BitStack _bitStack;

	private long _totalConsumed;

	private bool _isLastSegment;

	internal bool _stringHasEscaping;

	private readonly bool _isMultiSegment;

	private bool _trailingCommaBeforeComment;

	private SequencePosition _nextPosition;

	private SequencePosition _currentPosition;

	private readonly ReadOnlySequence<byte> _sequence;

	private bool IsLastSpan
	{
		get
		{
			if (_isFinalBlock)
			{
				if (_isMultiSegment)
				{
					return _isLastSegment;
				}
				return true;
			}
			return false;
		}
	}

	internal ReadOnlySequence<byte> OriginalSequence => _sequence;

	internal ReadOnlySpan<byte> OriginalSpan
	{
		get
		{
			if (!_sequence.IsEmpty)
			{
				return default(ReadOnlySpan<byte>);
			}
			return _buffer;
		}
	}

	public ReadOnlySpan<byte> ValueSpan { get; private set; }

	public long BytesConsumed => _totalConsumed + _consumed;

	public long TokenStartIndex { get; private set; }

	public int CurrentDepth
	{
		get
		{
			int num = _bitStack.CurrentDepth;
			if (TokenType == JsonTokenType.StartArray || TokenType == JsonTokenType.StartObject)
			{
				num--;
			}
			return num;
		}
	}

	internal bool IsInArray => !_inObject;

	public JsonTokenType TokenType => _tokenType;

	public bool HasValueSequence { get; private set; }

	public bool IsFinalBlock => _isFinalBlock;

	public ReadOnlySequence<byte> ValueSequence { get; private set; }

	public SequencePosition Position
	{
		get
		{
			if (_isInputSequence)
			{
				return _sequence.GetPosition(_consumed, _currentPosition);
			}
			return default(SequencePosition);
		}
	}

	public JsonReaderState CurrentState
	{
		get
		{
			JsonReaderState result = default(JsonReaderState);
			result._lineNumber = _lineNumber;
			result._bytePositionInLine = _bytePositionInLine;
			result._inObject = _inObject;
			result._isNotPrimitive = _isNotPrimitive;
			result._stringHasEscaping = _stringHasEscaping;
			result._trailingCommaBeforeComment = _trailingCommaBeforeComment;
			result._tokenType = _tokenType;
			result._previousTokenType = _previousTokenType;
			result._readerOptions = _readerOptions;
			result._bitStack = _bitStack;
			return result;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"TokenType = {DebugTokenType} (TokenStartIndex = {TokenStartIndex}) Consumed = {BytesConsumed}";

	private string DebugTokenType => TokenType switch
	{
		JsonTokenType.Comment => "Comment", 
		JsonTokenType.EndArray => "EndArray", 
		JsonTokenType.EndObject => "EndObject", 
		JsonTokenType.False => "False", 
		JsonTokenType.None => "None", 
		JsonTokenType.Null => "Null", 
		JsonTokenType.Number => "Number", 
		JsonTokenType.PropertyName => "PropertyName", 
		JsonTokenType.StartArray => "StartArray", 
		JsonTokenType.StartObject => "StartObject", 
		JsonTokenType.String => "String", 
		JsonTokenType.True => "True", 
		_ => ((byte)TokenType).ToString(), 
	};

	public Utf8JsonReader(ReadOnlySpan<byte> jsonData, bool isFinalBlock, JsonReaderState state)
	{
		_buffer = jsonData;
		_isFinalBlock = isFinalBlock;
		_isInputSequence = false;
		_lineNumber = state._lineNumber;
		_bytePositionInLine = state._bytePositionInLine;
		_inObject = state._inObject;
		_isNotPrimitive = state._isNotPrimitive;
		_stringHasEscaping = state._stringHasEscaping;
		_trailingCommaBeforeComment = state._trailingCommaBeforeComment;
		_tokenType = state._tokenType;
		_previousTokenType = state._previousTokenType;
		_readerOptions = state._readerOptions;
		if (_readerOptions.MaxDepth == 0)
		{
			_readerOptions.MaxDepth = 64;
		}
		_bitStack = state._bitStack;
		_consumed = 0;
		TokenStartIndex = 0L;
		_totalConsumed = 0L;
		_isLastSegment = _isFinalBlock;
		_isMultiSegment = false;
		ValueSpan = ReadOnlySpan<byte>.Empty;
		_currentPosition = default(SequencePosition);
		_nextPosition = default(SequencePosition);
		_sequence = default(ReadOnlySequence<byte>);
		HasValueSequence = false;
		ValueSequence = ReadOnlySequence<byte>.Empty;
	}

	public Utf8JsonReader(ReadOnlySpan<byte> jsonData, JsonReaderOptions options = default(JsonReaderOptions))
		: this(jsonData, isFinalBlock: true, new JsonReaderState(options))
	{
	}

	public bool Read()
	{
		bool flag = (_isMultiSegment ? ReadMultiSegment() : ReadSingleSegment());
		if (!flag && _isFinalBlock && TokenType == JsonTokenType.None)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedJsonTokens, 0);
		}
		return flag;
	}

	public void Skip()
	{
		if (!_isFinalBlock)
		{
			throw ThrowHelper.GetInvalidOperationException_CannotSkipOnPartial();
		}
		SkipHelper();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SkipHelper()
	{
		if (TokenType == JsonTokenType.PropertyName)
		{
			bool flag = Read();
		}
		if (TokenType == JsonTokenType.StartObject || TokenType == JsonTokenType.StartArray)
		{
			int currentDepth = CurrentDepth;
			do
			{
				bool flag2 = Read();
			}
			while (currentDepth < CurrentDepth);
		}
	}

	public bool TrySkip()
	{
		if (_isFinalBlock)
		{
			SkipHelper();
			return true;
		}
		return TrySkipHelper();
	}

	private bool TrySkipHelper()
	{
		Utf8JsonReader utf8JsonReader = this;
		if (TokenType != JsonTokenType.PropertyName || Read())
		{
			if (TokenType != JsonTokenType.StartObject && TokenType != JsonTokenType.StartArray)
			{
				goto IL_0042;
			}
			int currentDepth = CurrentDepth;
			while (Read())
			{
				if (currentDepth < CurrentDepth)
				{
					continue;
				}
				goto IL_0042;
			}
		}
		this = utf8JsonReader;
		return false;
		IL_0042:
		return true;
	}

	public bool ValueTextEquals(ReadOnlySpan<byte> utf8Text)
	{
		if (!IsTokenTypeString(TokenType))
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedStringComparison(TokenType);
		}
		return TextEqualsHelper(utf8Text);
	}

	public bool ValueTextEquals(string? text)
	{
		return ValueTextEquals(text.AsSpan());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TextEqualsHelper(ReadOnlySpan<byte> otherUtf8Text)
	{
		if (HasValueSequence)
		{
			return CompareToSequence(otherUtf8Text);
		}
		if (_stringHasEscaping)
		{
			return UnescapeAndCompare(otherUtf8Text);
		}
		return otherUtf8Text.SequenceEqual(ValueSpan);
	}

	public unsafe bool ValueTextEquals(ReadOnlySpan<char> text)
	{
		if (!IsTokenTypeString(TokenType))
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedStringComparison(TokenType);
		}
		if (MatchNotPossible(text.Length))
		{
			return false;
		}
		byte[] array = null;
		int num = checked(text.Length * 3);
		Span<byte> utf8Destination;
		if (num > 256)
		{
			array = ArrayPool<byte>.Shared.Rent(num);
			utf8Destination = array;
		}
		else
		{
			byte* pointer = stackalloc byte[256];
			utf8Destination = new Span<byte>(pointer, 256);
		}
		ReadOnlySpan<byte> utf16Source = MemoryMarshal.AsBytes(text);
		int bytesConsumed;
		int bytesWritten;
		OperationStatus operationStatus = JsonWriterHelper.ToUtf8(utf16Source, utf8Destination, out bytesConsumed, out bytesWritten);
		bool result = operationStatus <= OperationStatus.DestinationTooSmall && TextEqualsHelper(utf8Destination.Slice(0, bytesWritten));
		if (array != null)
		{
			utf8Destination.Slice(0, bytesWritten).Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	private bool CompareToSequence(ReadOnlySpan<byte> other)
	{
		if (_stringHasEscaping)
		{
			return UnescapeSequenceAndCompare(other);
		}
		ReadOnlySequence<byte> valueSequence = ValueSequence;
		if (valueSequence.Length != other.Length)
		{
			return false;
		}
		int num = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = valueSequence.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlySpan<byte> span = enumerator.Current.Span;
			if (other.Slice(num).StartsWith(span))
			{
				num += span.Length;
				continue;
			}
			return false;
		}
		return true;
	}

	private bool UnescapeAndCompare(ReadOnlySpan<byte> other)
	{
		ReadOnlySpan<byte> valueSpan = ValueSpan;
		if (valueSpan.Length < other.Length || valueSpan.Length / 6 > other.Length)
		{
			return false;
		}
		int num = valueSpan.IndexOf<byte>(92);
		if (!other.StartsWith(valueSpan.Slice(0, num)))
		{
			return false;
		}
		return JsonReaderHelper.UnescapeAndCompare(valueSpan.Slice(num), other.Slice(num));
	}

	private bool UnescapeSequenceAndCompare(ReadOnlySpan<byte> other)
	{
		ReadOnlySequence<byte> valueSequence = ValueSequence;
		long length = valueSequence.Length;
		if (length < other.Length || length / 6 > other.Length)
		{
			return false;
		}
		int num = 0;
		bool result = false;
		ReadOnlySequence<byte>.Enumerator enumerator = valueSequence.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlySpan<byte> span = enumerator.Current.Span;
			int num2 = span.IndexOf<byte>(92);
			if (num2 != -1)
			{
				if (other.Slice(num).StartsWith(span.Slice(0, num2)))
				{
					num += num2;
					other = other.Slice(num);
					valueSequence = valueSequence.Slice(num);
					result = ((!valueSequence.IsSingleSegment) ? JsonReaderHelper.UnescapeAndCompare(valueSequence, other) : JsonReaderHelper.UnescapeAndCompare(valueSequence.First.Span, other));
				}
				break;
			}
			if (!other.Slice(num).StartsWith(span))
			{
				break;
			}
			num += span.Length;
		}
		return result;
	}

	private static bool IsTokenTypeString(JsonTokenType tokenType)
	{
		if (tokenType != JsonTokenType.PropertyName)
		{
			return tokenType == JsonTokenType.String;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool MatchNotPossible(int charTextLength)
	{
		if (HasValueSequence)
		{
			return MatchNotPossibleSequence(charTextLength);
		}
		int length = ValueSpan.Length;
		if (length < charTextLength || length / (_stringHasEscaping ? 6 : 3) > charTextLength)
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool MatchNotPossibleSequence(int charTextLength)
	{
		long length = ValueSequence.Length;
		if (length < charTextLength || length / (_stringHasEscaping ? 6 : 3) > charTextLength)
		{
			return true;
		}
		return false;
	}

	private void StartObject()
	{
		if (_bitStack.CurrentDepth >= _readerOptions.MaxDepth)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ObjectDepthTooLarge, 0);
		}
		_bitStack.PushTrue();
		ValueSpan = _buffer.Slice(_consumed, 1);
		_consumed++;
		_bytePositionInLine++;
		_tokenType = JsonTokenType.StartObject;
		_inObject = true;
	}

	private void EndObject()
	{
		if (!_inObject || _bitStack.CurrentDepth <= 0)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.MismatchedObjectArray, 125);
		}
		if (_trailingCommaBeforeComment)
		{
			if (!_readerOptions.AllowTrailingCommas)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
			}
			_trailingCommaBeforeComment = false;
		}
		_tokenType = JsonTokenType.EndObject;
		ValueSpan = _buffer.Slice(_consumed, 1);
		UpdateBitStackOnEndToken();
	}

	private void StartArray()
	{
		if (_bitStack.CurrentDepth >= _readerOptions.MaxDepth)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ArrayDepthTooLarge, 0);
		}
		_bitStack.PushFalse();
		ValueSpan = _buffer.Slice(_consumed, 1);
		_consumed++;
		_bytePositionInLine++;
		_tokenType = JsonTokenType.StartArray;
		_inObject = false;
	}

	private void EndArray()
	{
		if (_inObject || _bitStack.CurrentDepth <= 0)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.MismatchedObjectArray, 93);
		}
		if (_trailingCommaBeforeComment)
		{
			if (!_readerOptions.AllowTrailingCommas)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
			}
			_trailingCommaBeforeComment = false;
		}
		_tokenType = JsonTokenType.EndArray;
		ValueSpan = _buffer.Slice(_consumed, 1);
		UpdateBitStackOnEndToken();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateBitStackOnEndToken()
	{
		_consumed++;
		_bytePositionInLine++;
		_inObject = _bitStack.Pop();
	}

	private bool ReadSingleSegment()
	{
		bool flag = false;
		ValueSpan = default(ReadOnlySpan<byte>);
		if (HasMoreData())
		{
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpace();
				if (!HasMoreData())
				{
					goto IL_0132;
				}
				b = _buffer[_consumed];
			}
			TokenStartIndex = _consumed;
			if (_tokenType != 0)
			{
				if (b == 47)
				{
					flag = ConsumeNextTokenOrRollback(b);
				}
				else if (_tokenType == JsonTokenType.StartObject)
				{
					if (b == 125)
					{
						EndObject();
						goto IL_0130;
					}
					if (b != 34)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
					}
					int consumed = _consumed;
					long bytePositionInLine = _bytePositionInLine;
					long lineNumber = _lineNumber;
					flag = ConsumePropertyName();
					if (!flag)
					{
						_consumed = consumed;
						_tokenType = JsonTokenType.StartObject;
						_bytePositionInLine = bytePositionInLine;
						_lineNumber = lineNumber;
					}
				}
				else if (_tokenType != JsonTokenType.StartArray)
				{
					flag = ((_tokenType != JsonTokenType.PropertyName) ? ConsumeNextTokenOrRollback(b) : ConsumeValue(b));
				}
				else
				{
					if (b == 93)
					{
						EndArray();
						goto IL_0130;
					}
					flag = ConsumeValue(b);
				}
			}
			else
			{
				flag = ReadFirstToken(b);
			}
		}
		goto IL_0132;
		IL_0132:
		return flag;
		IL_0130:
		flag = true;
		goto IL_0132;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasMoreData()
	{
		if (_consumed >= (uint)_buffer.Length)
		{
			if (_isNotPrimitive && IsLastSpan)
			{
				if (_bitStack.CurrentDepth != 0)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ZeroDepthAtEnd, 0);
				}
				if (_readerOptions.CommentHandling == JsonCommentHandling.Allow && _tokenType == JsonTokenType.Comment)
				{
					return false;
				}
				if (_tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive, 0);
				}
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasMoreData(ExceptionResource resource)
	{
		if (_consumed >= (uint)_buffer.Length)
		{
			if (IsLastSpan)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, resource, 0);
			}
			return false;
		}
		return true;
	}

	private bool ReadFirstToken(byte first)
	{
		switch (first)
		{
		case 123:
			_bitStack.SetFirstBit();
			_tokenType = JsonTokenType.StartObject;
			ValueSpan = _buffer.Slice(_consumed, 1);
			_consumed++;
			_bytePositionInLine++;
			_inObject = true;
			_isNotPrimitive = true;
			break;
		case 91:
			_bitStack.ResetFirstBit();
			_tokenType = JsonTokenType.StartArray;
			ValueSpan = _buffer.Slice(_consumed, 1);
			_consumed++;
			_bytePositionInLine++;
			_isNotPrimitive = true;
			break;
		default:
		{
			ReadOnlySpan<byte> buffer = _buffer;
			if (JsonHelpers.IsDigit(first) || first == 45)
			{
				if (!TryGetNumber(buffer.Slice(_consumed), out var consumed))
				{
					return false;
				}
				_tokenType = JsonTokenType.Number;
				_consumed += consumed;
				_bytePositionInLine += consumed;
				return true;
			}
			if (!ConsumeValue(first))
			{
				return false;
			}
			if (_tokenType == JsonTokenType.StartObject || _tokenType == JsonTokenType.StartArray)
			{
				_isNotPrimitive = true;
			}
			break;
		}
		}
		return true;
	}

	private void SkipWhiteSpace()
	{
		ReadOnlySpan<byte> buffer = _buffer;
		while (_consumed < buffer.Length)
		{
			byte b = buffer[_consumed];
			if (b == 32 || b == 13 || b == 10 || b == 9)
			{
				if (b == 10)
				{
					_lineNumber++;
					_bytePositionInLine = 0L;
				}
				else
				{
					_bytePositionInLine++;
				}
				_consumed++;
				continue;
			}
			break;
		}
	}

	private bool ConsumeValue(byte marker)
	{
		while (true)
		{
			_trailingCommaBeforeComment = false;
			switch (marker)
			{
			case 34:
				return ConsumeString();
			case 123:
				StartObject();
				break;
			case 91:
				StartArray();
				break;
			default:
				if (JsonHelpers.IsDigit(marker) || marker == 45)
				{
					return ConsumeNumber();
				}
				switch (marker)
				{
				case 102:
					return ConsumeLiteral(JsonConstants.FalseValue, JsonTokenType.False);
				case 116:
					return ConsumeLiteral(JsonConstants.TrueValue, JsonTokenType.True);
				case 110:
					return ConsumeLiteral(JsonConstants.NullValue, JsonTokenType.Null);
				}
				switch (_readerOptions.CommentHandling)
				{
				case JsonCommentHandling.Allow:
					if (marker == 47)
					{
						return ConsumeComment();
					}
					break;
				default:
					if (marker != 47)
					{
						break;
					}
					if (SkipComment())
					{
						if (_consumed >= (uint)_buffer.Length)
						{
							if (_isNotPrimitive && IsLastSpan && _tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive, 0);
							}
							return false;
						}
						marker = _buffer[_consumed];
						if (marker <= 32)
						{
							SkipWhiteSpace();
							if (!HasMoreData())
							{
								return false;
							}
							marker = _buffer[_consumed];
						}
						goto IL_0140;
					}
					return false;
				case JsonCommentHandling.Disallow:
					break;
				}
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfValueNotFound, marker);
				break;
			}
			break;
			IL_0140:
			TokenStartIndex = _consumed;
		}
		return true;
	}

	private bool ConsumeLiteral(ReadOnlySpan<byte> literal, JsonTokenType tokenType)
	{
		ReadOnlySpan<byte> span = _buffer.Slice(_consumed);
		if (!span.StartsWith(literal))
		{
			return CheckLiteral(span, literal);
		}
		ValueSpan = span.Slice(0, literal.Length);
		_tokenType = tokenType;
		_consumed += literal.Length;
		_bytePositionInLine += literal.Length;
		return true;
	}

	private bool CheckLiteral(ReadOnlySpan<byte> span, ReadOnlySpan<byte> literal)
	{
		int num = 0;
		for (int i = 1; i < literal.Length; i++)
		{
			if (span.Length > i)
			{
				if (span[i] != literal[i])
				{
					_bytePositionInLine += i;
					ThrowInvalidLiteral(span);
				}
				continue;
			}
			num = i;
			break;
		}
		if (IsLastSpan)
		{
			_bytePositionInLine += num;
			ThrowInvalidLiteral(span);
		}
		return false;
	}

	private void ThrowInvalidLiteral(ReadOnlySpan<byte> span)
	{
		ThrowHelper.ThrowJsonReaderException(ref this, span[0] switch
		{
			116 => ExceptionResource.ExpectedTrue, 
			102 => ExceptionResource.ExpectedFalse, 
			_ => ExceptionResource.ExpectedNull, 
		}, 0, span);
	}

	private bool ConsumeNumber()
	{
		if (!TryGetNumber(_buffer.Slice(_consumed), out var consumed))
		{
			return false;
		}
		_tokenType = JsonTokenType.Number;
		_consumed += consumed;
		_bytePositionInLine += consumed;
		if (_consumed >= (uint)_buffer.Length && _isNotPrimitive)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, _buffer[_consumed - 1]);
		}
		return true;
	}

	private bool ConsumePropertyName()
	{
		_trailingCommaBeforeComment = false;
		if (!ConsumeString())
		{
			return false;
		}
		if (!HasMoreData(ExceptionResource.ExpectedValueAfterPropertyNameNotFound))
		{
			return false;
		}
		byte b = _buffer[_consumed];
		if (b <= 32)
		{
			SkipWhiteSpace();
			if (!HasMoreData(ExceptionResource.ExpectedValueAfterPropertyNameNotFound))
			{
				return false;
			}
			b = _buffer[_consumed];
		}
		if (b != 58)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedSeparatorAfterPropertyNameNotFound, b);
		}
		_consumed++;
		_bytePositionInLine++;
		_tokenType = JsonTokenType.PropertyName;
		return true;
	}

	private bool ConsumeString()
	{
		ReadOnlySpan<byte> readOnlySpan = _buffer.Slice(_consumed + 1);
		int num = readOnlySpan.IndexOfQuoteOrAnyControlOrBackSlash();
		if (num >= 0)
		{
			byte b = readOnlySpan[num];
			if (b == 34)
			{
				_bytePositionInLine += num + 2;
				ValueSpan = readOnlySpan.Slice(0, num);
				_stringHasEscaping = false;
				_tokenType = JsonTokenType.String;
				_consumed += num + 2;
				return true;
			}
			return ConsumeStringAndValidate(readOnlySpan, num);
		}
		if (IsLastSpan)
		{
			_bytePositionInLine += readOnlySpan.Length + 1;
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
		}
		return false;
	}

	private bool ConsumeStringAndValidate(ReadOnlySpan<byte> data, int idx)
	{
		long bytePositionInLine = _bytePositionInLine;
		long lineNumber = _lineNumber;
		_bytePositionInLine += idx + 1;
		bool flag = false;
		while (true)
		{
			if (idx < data.Length)
			{
				byte b = data[idx];
				if (b == 34)
				{
					if (!flag)
					{
						break;
					}
					flag = false;
				}
				else if (b == 92)
				{
					flag = !flag;
				}
				else if (flag)
				{
					int num = JsonConstants.EscapableChars.IndexOf(b);
					if (num == -1)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterAfterEscapeWithinString, b);
					}
					if (b == 117)
					{
						_bytePositionInLine++;
						if (!ValidateHexDigits(data, idx + 1))
						{
							idx = data.Length;
							goto IL_00e5;
						}
						idx += 4;
					}
					flag = false;
				}
				else if (b < 32)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterWithinString, b);
				}
				_bytePositionInLine++;
				idx++;
				continue;
			}
			goto IL_00e5;
			IL_00e5:
			if (idx < data.Length)
			{
				break;
			}
			if (IsLastSpan)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
			}
			_lineNumber = lineNumber;
			_bytePositionInLine = bytePositionInLine;
			return false;
		}
		_bytePositionInLine++;
		ValueSpan = data.Slice(0, idx);
		_stringHasEscaping = true;
		_tokenType = JsonTokenType.String;
		_consumed += idx + 2;
		return true;
	}

	private bool ValidateHexDigits(ReadOnlySpan<byte> data, int idx)
	{
		for (int i = idx; i < data.Length; i++)
		{
			byte nextByte = data[i];
			if (!JsonReaderHelper.IsHexDigit(nextByte))
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidHexCharacterWithinString, nextByte);
			}
			if (i - idx >= 3)
			{
				return true;
			}
			_bytePositionInLine++;
		}
		return false;
	}

	private bool TryGetNumber(ReadOnlySpan<byte> data, out int consumed)
	{
		consumed = 0;
		int i = 0;
		ConsumeNumberResult consumeNumberResult = ConsumeNegativeSign(ref data, ref i);
		if (consumeNumberResult == ConsumeNumberResult.NeedMoreData)
		{
			return false;
		}
		byte b = data[i];
		if (b == 48)
		{
			ConsumeNumberResult consumeNumberResult2 = ConsumeZero(ref data, ref i);
			if (consumeNumberResult2 == ConsumeNumberResult.NeedMoreData)
			{
				return false;
			}
			if (consumeNumberResult2 != 0)
			{
				b = data[i];
				goto IL_00a3;
			}
		}
		else
		{
			i++;
			ConsumeNumberResult consumeNumberResult3 = ConsumeIntegerDigits(ref data, ref i);
			if (consumeNumberResult3 == ConsumeNumberResult.NeedMoreData)
			{
				return false;
			}
			if (consumeNumberResult3 != 0)
			{
				b = data[i];
				if (b != 46 && b != 69 && b != 101)
				{
					_bytePositionInLine += i;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, b);
				}
				goto IL_00a3;
			}
		}
		goto IL_0152;
		IL_00a3:
		if (b == 46)
		{
			i++;
			ConsumeNumberResult consumeNumberResult4 = ConsumeDecimalDigits(ref data, ref i);
			if (consumeNumberResult4 == ConsumeNumberResult.NeedMoreData)
			{
				return false;
			}
			if (consumeNumberResult4 == ConsumeNumberResult.Success)
			{
				goto IL_0152;
			}
			b = data[i];
			if (b != 69 && b != 101)
			{
				_bytePositionInLine += i;
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedNextDigitEValueNotFound, b);
			}
		}
		i++;
		consumeNumberResult = ConsumeSign(ref data, ref i);
		if (consumeNumberResult == ConsumeNumberResult.NeedMoreData)
		{
			return false;
		}
		i++;
		switch (ConsumeIntegerDigits(ref data, ref i))
		{
		case ConsumeNumberResult.NeedMoreData:
			return false;
		default:
			_bytePositionInLine += i;
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, data[i]);
			break;
		case ConsumeNumberResult.Success:
			break;
		}
		goto IL_0152;
		IL_0152:
		ValueSpan = data.Slice(0, i);
		consumed = i;
		return true;
	}

	private ConsumeNumberResult ConsumeNegativeSign(ref ReadOnlySpan<byte> data, ref int i)
	{
		byte b = data[i];
		if (b == 45)
		{
			i++;
			if (i >= data.Length)
			{
				if (IsLastSpan)
				{
					_bytePositionInLine += i;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				return ConsumeNumberResult.NeedMoreData;
			}
			b = data[i];
			if (!JsonHelpers.IsDigit(b))
			{
				_bytePositionInLine += i;
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterSign, b);
			}
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private ConsumeNumberResult ConsumeZero(ref ReadOnlySpan<byte> data, ref int i)
	{
		i++;
		byte b = 0;
		if (i < data.Length)
		{
			b = data[i];
			if (JsonConstants.Delimiters.IndexOf(b) >= 0)
			{
				return ConsumeNumberResult.Success;
			}
			b = data[i];
			if (b != 46 && b != 69 && b != 101)
			{
				_bytePositionInLine += i;
				ThrowHelper.ThrowJsonReaderException(ref this, JsonHelpers.IsInRangeInclusive(b, 48, 57) ? ExceptionResource.InvalidLeadingZeroInNumber : ExceptionResource.ExpectedEndOfDigitNotFound, b);
			}
			return ConsumeNumberResult.OperationIncomplete;
		}
		if (IsLastSpan)
		{
			return ConsumeNumberResult.Success;
		}
		return ConsumeNumberResult.NeedMoreData;
	}

	private ConsumeNumberResult ConsumeIntegerDigits(ref ReadOnlySpan<byte> data, ref int i)
	{
		byte value = 0;
		while (i < data.Length)
		{
			value = data[i];
			if (!JsonHelpers.IsDigit(value))
			{
				break;
			}
			i++;
		}
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				return ConsumeNumberResult.Success;
			}
			return ConsumeNumberResult.NeedMoreData;
		}
		if (JsonConstants.Delimiters.IndexOf(value) >= 0)
		{
			return ConsumeNumberResult.Success;
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private ConsumeNumberResult ConsumeDecimalDigits(ref ReadOnlySpan<byte> data, ref int i)
	{
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				_bytePositionInLine += i;
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
			}
			return ConsumeNumberResult.NeedMoreData;
		}
		byte b = data[i];
		if (!JsonHelpers.IsDigit(b))
		{
			_bytePositionInLine += i;
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterDecimal, b);
		}
		i++;
		return ConsumeIntegerDigits(ref data, ref i);
	}

	private ConsumeNumberResult ConsumeSign(ref ReadOnlySpan<byte> data, ref int i)
	{
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				_bytePositionInLine += i;
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
			}
			return ConsumeNumberResult.NeedMoreData;
		}
		byte b = data[i];
		if (b == 43 || b == 45)
		{
			i++;
			if (i >= data.Length)
			{
				if (IsLastSpan)
				{
					_bytePositionInLine += i;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				return ConsumeNumberResult.NeedMoreData;
			}
			b = data[i];
		}
		if (!JsonHelpers.IsDigit(b))
		{
			_bytePositionInLine += i;
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterSign, b);
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private bool ConsumeNextTokenOrRollback(byte marker)
	{
		int consumed = _consumed;
		long bytePositionInLine = _bytePositionInLine;
		long lineNumber = _lineNumber;
		JsonTokenType tokenType = _tokenType;
		bool trailingCommaBeforeComment = _trailingCommaBeforeComment;
		switch (ConsumeNextToken(marker))
		{
		case ConsumeTokenResult.Success:
			return true;
		case ConsumeTokenResult.NotEnoughDataRollBackState:
			_consumed = consumed;
			_tokenType = tokenType;
			_bytePositionInLine = bytePositionInLine;
			_lineNumber = lineNumber;
			_trailingCommaBeforeComment = trailingCommaBeforeComment;
			break;
		}
		return false;
	}

	private ConsumeTokenResult ConsumeNextToken(byte marker)
	{
		if (_readerOptions.CommentHandling != 0)
		{
			if (_readerOptions.CommentHandling != JsonCommentHandling.Allow)
			{
				return ConsumeNextTokenUntilAfterAllCommentsAreSkipped(marker);
			}
			if (marker == 47)
			{
				if (!ConsumeComment())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (_tokenType == JsonTokenType.Comment)
			{
				return ConsumeNextTokenFromLastNonCommentToken();
			}
		}
		if (_bitStack.CurrentDepth == 0)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, marker);
		}
		switch (marker)
		{
		case 44:
		{
			_consumed++;
			_bytePositionInLine++;
			if (_consumed >= (uint)_buffer.Length)
			{
				if (IsLastSpan)
				{
					_consumed--;
					_bytePositionInLine--;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
				}
				return ConsumeTokenResult.NotEnoughDataRollBackState;
			}
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpace();
				if (!HasMoreData(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				b = _buffer[_consumed];
			}
			TokenStartIndex = _consumed;
			if (_readerOptions.CommentHandling == JsonCommentHandling.Allow && b == 47)
			{
				_trailingCommaBeforeComment = true;
				if (!ConsumeComment())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (_inObject)
			{
				if (b != 34)
				{
					if (b == 125)
					{
						if (_readerOptions.AllowTrailingCommas)
						{
							EndObject();
							return ConsumeTokenResult.Success;
						}
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
					}
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
				}
				if (!ConsumePropertyName())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (b == 93)
			{
				if (_readerOptions.AllowTrailingCommas)
				{
					EndArray();
					return ConsumeTokenResult.Success;
				}
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
			}
			if (!ConsumeValue(b))
			{
				return ConsumeTokenResult.NotEnoughDataRollBackState;
			}
			return ConsumeTokenResult.Success;
		}
		case 125:
			EndObject();
			break;
		case 93:
			EndArray();
			break;
		default:
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.FoundInvalidCharacter, marker);
			break;
		}
		return ConsumeTokenResult.Success;
	}

	private ConsumeTokenResult ConsumeNextTokenFromLastNonCommentToken()
	{
		if (JsonReaderHelper.IsTokenTypePrimitive(_previousTokenType))
		{
			_tokenType = (_inObject ? JsonTokenType.StartObject : JsonTokenType.StartArray);
		}
		else
		{
			_tokenType = _previousTokenType;
		}
		if (HasMoreData())
		{
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpace();
				if (!HasMoreData())
				{
					goto IL_0343;
				}
				b = _buffer[_consumed];
			}
			if (_bitStack.CurrentDepth == 0 && _tokenType != 0)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, b);
			}
			TokenStartIndex = _consumed;
			if (b != 44)
			{
				if (b == 125)
				{
					EndObject();
				}
				else
				{
					if (b != 93)
					{
						if (_tokenType == JsonTokenType.None)
						{
							if (ReadFirstToken(b))
							{
								goto IL_0341;
							}
						}
						else if (_tokenType == JsonTokenType.StartObject)
						{
							if (b != 34)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
							}
							int consumed = _consumed;
							long bytePositionInLine = _bytePositionInLine;
							long lineNumber = _lineNumber;
							if (ConsumePropertyName())
							{
								goto IL_0341;
							}
							_consumed = consumed;
							_tokenType = JsonTokenType.StartObject;
							_bytePositionInLine = bytePositionInLine;
							_lineNumber = lineNumber;
						}
						else if (_tokenType == JsonTokenType.StartArray)
						{
							if (ConsumeValue(b))
							{
								goto IL_0341;
							}
						}
						else if (_tokenType == JsonTokenType.PropertyName)
						{
							if (ConsumeValue(b))
							{
								goto IL_0341;
							}
						}
						else if (_inObject)
						{
							if (b != 34)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
							}
							if (ConsumePropertyName())
							{
								goto IL_0341;
							}
						}
						else if (ConsumeValue(b))
						{
							goto IL_0341;
						}
						goto IL_0343;
					}
					EndArray();
				}
				goto IL_0341;
			}
			if ((int)_previousTokenType <= 1 || _previousTokenType == JsonTokenType.StartArray || _trailingCommaBeforeComment)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueAfterComment, b);
			}
			_consumed++;
			_bytePositionInLine++;
			if (_consumed >= (uint)_buffer.Length)
			{
				if (IsLastSpan)
				{
					_consumed--;
					_bytePositionInLine--;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
				}
			}
			else
			{
				b = _buffer[_consumed];
				if (b <= 32)
				{
					SkipWhiteSpace();
					if (!HasMoreData(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
					{
						goto IL_0343;
					}
					b = _buffer[_consumed];
				}
				TokenStartIndex = _consumed;
				if (b == 47)
				{
					_trailingCommaBeforeComment = true;
					if (ConsumeComment())
					{
						goto IL_0341;
					}
				}
				else if (_inObject)
				{
					if (b != 34)
					{
						if (b == 125)
						{
							if (_readerOptions.AllowTrailingCommas)
							{
								EndObject();
								goto IL_0341;
							}
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
						}
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
					}
					if (ConsumePropertyName())
					{
						goto IL_0341;
					}
				}
				else
				{
					if (b == 93)
					{
						if (_readerOptions.AllowTrailingCommas)
						{
							EndArray();
							goto IL_0341;
						}
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
					}
					if (ConsumeValue(b))
					{
						goto IL_0341;
					}
				}
			}
		}
		goto IL_0343;
		IL_0343:
		return ConsumeTokenResult.NotEnoughDataRollBackState;
		IL_0341:
		return ConsumeTokenResult.Success;
	}

	private bool SkipAllComments(ref byte marker)
	{
		while (true)
		{
			if (marker == 47)
			{
				if (!SkipComment() || !HasMoreData())
				{
					break;
				}
				marker = _buffer[_consumed];
				if (marker <= 32)
				{
					SkipWhiteSpace();
					if (!HasMoreData())
					{
						break;
					}
					marker = _buffer[_consumed];
				}
				continue;
			}
			return true;
		}
		return false;
	}

	private bool SkipAllComments(ref byte marker, ExceptionResource resource)
	{
		while (true)
		{
			if (marker == 47)
			{
				if (!SkipComment() || !HasMoreData(resource))
				{
					break;
				}
				marker = _buffer[_consumed];
				if (marker <= 32)
				{
					SkipWhiteSpace();
					if (!HasMoreData(resource))
					{
						break;
					}
					marker = _buffer[_consumed];
				}
				continue;
			}
			return true;
		}
		return false;
	}

	private ConsumeTokenResult ConsumeNextTokenUntilAfterAllCommentsAreSkipped(byte marker)
	{
		if (SkipAllComments(ref marker))
		{
			TokenStartIndex = _consumed;
			if (_tokenType == JsonTokenType.StartObject)
			{
				if (marker == 125)
				{
					EndObject();
				}
				else
				{
					if (marker != 34)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, marker);
					}
					int consumed = _consumed;
					long bytePositionInLine = _bytePositionInLine;
					long lineNumber = _lineNumber;
					if (!ConsumePropertyName())
					{
						_consumed = consumed;
						_tokenType = JsonTokenType.StartObject;
						_bytePositionInLine = bytePositionInLine;
						_lineNumber = lineNumber;
						goto IL_0281;
					}
				}
			}
			else if (_tokenType == JsonTokenType.StartArray)
			{
				if (marker == 93)
				{
					EndArray();
				}
				else if (!ConsumeValue(marker))
				{
					goto IL_0281;
				}
			}
			else if (_tokenType == JsonTokenType.PropertyName)
			{
				if (!ConsumeValue(marker))
				{
					goto IL_0281;
				}
			}
			else if (_bitStack.CurrentDepth == 0)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, marker);
			}
			else
			{
				switch (marker)
				{
				case 44:
					_consumed++;
					_bytePositionInLine++;
					if (_consumed >= (uint)_buffer.Length)
					{
						if (IsLastSpan)
						{
							_consumed--;
							_bytePositionInLine--;
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
						}
						return ConsumeTokenResult.NotEnoughDataRollBackState;
					}
					marker = _buffer[_consumed];
					if (marker <= 32)
					{
						SkipWhiteSpace();
						if (!HasMoreData(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
						{
							return ConsumeTokenResult.NotEnoughDataRollBackState;
						}
						marker = _buffer[_consumed];
					}
					if (SkipAllComments(ref marker, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
					{
						TokenStartIndex = _consumed;
						if (_inObject)
						{
							if (marker != 34)
							{
								if (marker == 125)
								{
									if (_readerOptions.AllowTrailingCommas)
									{
										EndObject();
										break;
									}
									ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
								}
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, marker);
							}
							if (!ConsumePropertyName())
							{
								return ConsumeTokenResult.NotEnoughDataRollBackState;
							}
							return ConsumeTokenResult.Success;
						}
						if (marker == 93)
						{
							if (_readerOptions.AllowTrailingCommas)
							{
								EndArray();
								break;
							}
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
						}
						if (!ConsumeValue(marker))
						{
							return ConsumeTokenResult.NotEnoughDataRollBackState;
						}
						return ConsumeTokenResult.Success;
					}
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				case 125:
					EndObject();
					break;
				case 93:
					EndArray();
					break;
				default:
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.FoundInvalidCharacter, marker);
					break;
				}
			}
			return ConsumeTokenResult.Success;
		}
		goto IL_0281;
		IL_0281:
		return ConsumeTokenResult.IncompleteNoRollBackNecessary;
	}

	private bool SkipComment()
	{
		ReadOnlySpan<byte> readOnlySpan = _buffer.Slice(_consumed + 1);
		if (readOnlySpan.Length > 0)
		{
			int idx;
			switch (readOnlySpan[0])
			{
			case 47:
				return SkipSingleLineComment(readOnlySpan.Slice(1), out idx);
			case 42:
				return SkipMultiLineComment(readOnlySpan.Slice(1), out idx);
			}
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfValueNotFound, 47);
		}
		if (IsLastSpan)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfValueNotFound, 47);
		}
		return false;
	}

	private bool SkipSingleLineComment(ReadOnlySpan<byte> localBuffer, out int idx)
	{
		idx = FindLineSeparator(localBuffer);
		int num = 0;
		if (idx != -1)
		{
			num = idx;
			if (localBuffer[idx] != 10)
			{
				if (idx < localBuffer.Length - 1)
				{
					if (localBuffer[idx + 1] == 10)
					{
						num++;
					}
				}
				else if (!IsLastSpan)
				{
					return false;
				}
			}
			num++;
			_bytePositionInLine = 0L;
			_lineNumber++;
		}
		else
		{
			if (!IsLastSpan)
			{
				return false;
			}
			idx = localBuffer.Length;
			num = idx;
			_bytePositionInLine += 2 + localBuffer.Length;
		}
		_consumed += 2 + num;
		return true;
	}

	private int FindLineSeparator(ReadOnlySpan<byte> localBuffer)
	{
		int num = 0;
		while (true)
		{
			int num2 = localBuffer.IndexOfAny<byte>(10, 13, 226);
			if (num2 == -1)
			{
				return -1;
			}
			num += num2;
			if (localBuffer[num2] != 226)
			{
				break;
			}
			num++;
			localBuffer = localBuffer.Slice(num2 + 1);
			ThrowOnDangerousLineSeparator(localBuffer);
		}
		return num;
	}

	private void ThrowOnDangerousLineSeparator(ReadOnlySpan<byte> localBuffer)
	{
		if (localBuffer.Length >= 2)
		{
			byte b = localBuffer[1];
			if (localBuffer[0] == 128 && (b == 168 || b == 169))
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfLineSeparator, 0);
			}
		}
	}

	private bool SkipMultiLineComment(ReadOnlySpan<byte> localBuffer, out int idx)
	{
		idx = 0;
		while (true)
		{
			int num = localBuffer.Slice(idx).IndexOf<byte>(47);
			switch (num)
			{
			case -1:
				if (IsLastSpan)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfCommentNotFound, 0);
				}
				return false;
			default:
				if (localBuffer[num + idx - 1] == 42)
				{
					idx += num - 1;
					_consumed += 4 + idx;
					var (num2, num3) = JsonReaderHelper.CountNewLines(localBuffer.Slice(0, idx));
					_lineNumber += num2;
					if (num3 != -1)
					{
						_bytePositionInLine = idx - num3 + 1;
					}
					else
					{
						_bytePositionInLine += 4 + idx;
					}
					return true;
				}
				break;
			case 0:
				break;
			}
			idx += num + 1;
		}
	}

	private bool ConsumeComment()
	{
		ReadOnlySpan<byte> readOnlySpan = _buffer.Slice(_consumed + 1);
		if (readOnlySpan.Length > 0)
		{
			byte b = readOnlySpan[0];
			switch (b)
			{
			case 47:
				return ConsumeSingleLineComment(readOnlySpan.Slice(1), _consumed);
			case 42:
				return ConsumeMultiLineComment(readOnlySpan.Slice(1), _consumed);
			}
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterAtStartOfComment, b);
		}
		if (IsLastSpan)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
		}
		return false;
	}

	private bool ConsumeSingleLineComment(ReadOnlySpan<byte> localBuffer, int previousConsumed)
	{
		if (!SkipSingleLineComment(localBuffer, out var idx))
		{
			return false;
		}
		ValueSpan = _buffer.Slice(previousConsumed + 2, idx);
		if (_tokenType != JsonTokenType.Comment)
		{
			_previousTokenType = _tokenType;
		}
		_tokenType = JsonTokenType.Comment;
		return true;
	}

	private bool ConsumeMultiLineComment(ReadOnlySpan<byte> localBuffer, int previousConsumed)
	{
		if (!SkipMultiLineComment(localBuffer, out var idx))
		{
			return false;
		}
		ValueSpan = _buffer.Slice(previousConsumed + 2, idx);
		if (_tokenType != JsonTokenType.Comment)
		{
			_previousTokenType = _tokenType;
		}
		_tokenType = JsonTokenType.Comment;
		return true;
	}

	private ReadOnlySpan<byte> GetUnescapedSpan()
	{
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> readOnlySpan2 = readOnlySpan;
		if (_stringHasEscaping)
		{
			int idx = readOnlySpan2.IndexOf<byte>(92);
			readOnlySpan2 = JsonReaderHelper.GetUnescapedSpan(readOnlySpan2, idx);
		}
		return readOnlySpan2;
	}

	public Utf8JsonReader(ReadOnlySequence<byte> jsonData, bool isFinalBlock, JsonReaderState state)
	{
		ReadOnlyMemory<byte> memory = jsonData.First;
		_buffer = memory.Span;
		_isFinalBlock = isFinalBlock;
		_isInputSequence = true;
		_lineNumber = state._lineNumber;
		_bytePositionInLine = state._bytePositionInLine;
		_inObject = state._inObject;
		_isNotPrimitive = state._isNotPrimitive;
		_stringHasEscaping = state._stringHasEscaping;
		_trailingCommaBeforeComment = state._trailingCommaBeforeComment;
		_tokenType = state._tokenType;
		_previousTokenType = state._previousTokenType;
		_readerOptions = state._readerOptions;
		if (_readerOptions.MaxDepth == 0)
		{
			_readerOptions.MaxDepth = 64;
		}
		_bitStack = state._bitStack;
		_consumed = 0;
		TokenStartIndex = 0L;
		_totalConsumed = 0L;
		ValueSpan = ReadOnlySpan<byte>.Empty;
		_sequence = jsonData;
		HasValueSequence = false;
		ValueSequence = ReadOnlySequence<byte>.Empty;
		if (jsonData.IsSingleSegment)
		{
			_nextPosition = default(SequencePosition);
			_currentPosition = jsonData.Start;
			_isLastSegment = isFinalBlock;
			_isMultiSegment = false;
			return;
		}
		_currentPosition = jsonData.Start;
		_nextPosition = _currentPosition;
		bool flag = _buffer.Length == 0;
		if (flag)
		{
			SequencePosition nextPosition = _nextPosition;
			ReadOnlyMemory<byte> memory2;
			while (jsonData.TryGet(ref _nextPosition, out memory2))
			{
				_currentPosition = nextPosition;
				if (memory2.Length != 0)
				{
					_buffer = memory2.Span;
					break;
				}
				nextPosition = _nextPosition;
			}
		}
		_isLastSegment = !jsonData.TryGet(ref _nextPosition, out memory, !flag) && isFinalBlock;
		_isMultiSegment = true;
	}

	public Utf8JsonReader(ReadOnlySequence<byte> jsonData, JsonReaderOptions options = default(JsonReaderOptions))
		: this(jsonData, isFinalBlock: true, new JsonReaderState(options))
	{
	}

	private bool ReadMultiSegment()
	{
		bool flag = false;
		HasValueSequence = false;
		ValueSpan = default(ReadOnlySpan<byte>);
		ValueSequence = default(ReadOnlySequence<byte>);
		if (HasMoreDataMultiSegment())
		{
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpaceMultiSegment();
				if (!HasMoreDataMultiSegment())
				{
					goto IL_016c;
				}
				b = _buffer[_consumed];
			}
			TokenStartIndex = BytesConsumed;
			if (_tokenType != 0)
			{
				if (b == 47)
				{
					flag = ConsumeNextTokenOrRollbackMultiSegment(b);
				}
				else if (_tokenType == JsonTokenType.StartObject)
				{
					if (b == 125)
					{
						EndObject();
						goto IL_016a;
					}
					if (b != 34)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
					}
					long totalConsumed = _totalConsumed;
					int consumed = _consumed;
					long bytePositionInLine = _bytePositionInLine;
					long lineNumber = _lineNumber;
					SequencePosition currentPosition = _currentPosition;
					flag = ConsumePropertyNameMultiSegment();
					if (!flag)
					{
						_consumed = consumed;
						_tokenType = JsonTokenType.StartObject;
						_bytePositionInLine = bytePositionInLine;
						_lineNumber = lineNumber;
						_totalConsumed = totalConsumed;
						_currentPosition = currentPosition;
					}
				}
				else if (_tokenType != JsonTokenType.StartArray)
				{
					flag = ((_tokenType != JsonTokenType.PropertyName) ? ConsumeNextTokenOrRollbackMultiSegment(b) : ConsumeValueMultiSegment(b));
				}
				else
				{
					if (b == 93)
					{
						EndArray();
						goto IL_016a;
					}
					flag = ConsumeValueMultiSegment(b);
				}
			}
			else
			{
				flag = ReadFirstTokenMultiSegment(b);
			}
		}
		goto IL_016c;
		IL_016c:
		return flag;
		IL_016a:
		flag = true;
		goto IL_016c;
	}

	private bool ValidateStateAtEndOfData()
	{
		if (_bitStack.CurrentDepth != 0)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ZeroDepthAtEnd, 0);
		}
		if (_readerOptions.CommentHandling == JsonCommentHandling.Allow && _tokenType == JsonTokenType.Comment)
		{
			return false;
		}
		if (_tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive, 0);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasMoreDataMultiSegment()
	{
		if (_consumed >= (uint)_buffer.Length)
		{
			if (_isNotPrimitive && IsLastSpan && !ValidateStateAtEndOfData())
			{
				return false;
			}
			if (!GetNextSpan())
			{
				if (_isNotPrimitive && IsLastSpan)
				{
					ValidateStateAtEndOfData();
				}
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasMoreDataMultiSegment(ExceptionResource resource)
	{
		if (_consumed >= (uint)_buffer.Length)
		{
			if (IsLastSpan)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, resource, 0);
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, resource, 0);
				}
				return false;
			}
		}
		return true;
	}

	private bool GetNextSpan()
	{
		ReadOnlyMemory<byte> memory = default(ReadOnlyMemory<byte>);
		while (true)
		{
			SequencePosition currentPosition = _currentPosition;
			_currentPosition = _nextPosition;
			if (!_sequence.TryGet(ref _nextPosition, out memory))
			{
				_currentPosition = currentPosition;
				_isLastSegment = true;
				return false;
			}
			if (memory.Length != 0)
			{
				break;
			}
			_currentPosition = currentPosition;
		}
		if (_isFinalBlock)
		{
			_isLastSegment = !_sequence.TryGet(ref _nextPosition, out var _, advance: false);
		}
		_buffer = memory.Span;
		_totalConsumed += _consumed;
		_consumed = 0;
		return true;
	}

	private bool ReadFirstTokenMultiSegment(byte first)
	{
		switch (first)
		{
		case 123:
			_bitStack.SetFirstBit();
			_tokenType = JsonTokenType.StartObject;
			ValueSpan = _buffer.Slice(_consumed, 1);
			_consumed++;
			_bytePositionInLine++;
			_inObject = true;
			_isNotPrimitive = true;
			break;
		case 91:
			_bitStack.ResetFirstBit();
			_tokenType = JsonTokenType.StartArray;
			ValueSpan = _buffer.Slice(_consumed, 1);
			_consumed++;
			_bytePositionInLine++;
			_isNotPrimitive = true;
			break;
		default:
			if (JsonHelpers.IsDigit(first) || first == 45)
			{
				if (!TryGetNumberMultiSegment(_buffer.Slice(_consumed), out var consumed))
				{
					return false;
				}
				_tokenType = JsonTokenType.Number;
				_consumed += consumed;
				return true;
			}
			if (!ConsumeValueMultiSegment(first))
			{
				return false;
			}
			if (_tokenType == JsonTokenType.StartObject || _tokenType == JsonTokenType.StartArray)
			{
				_isNotPrimitive = true;
			}
			break;
		}
		return true;
	}

	private void SkipWhiteSpaceMultiSegment()
	{
		do
		{
			SkipWhiteSpace();
		}
		while (_consumed >= _buffer.Length && GetNextSpan());
	}

	private bool ConsumeValueMultiSegment(byte marker)
	{
		while (true)
		{
			_trailingCommaBeforeComment = false;
			switch (marker)
			{
			case 34:
				return ConsumeStringMultiSegment();
			case 123:
				StartObject();
				break;
			case 91:
				StartArray();
				break;
			default:
				if (JsonHelpers.IsDigit(marker) || marker == 45)
				{
					return ConsumeNumberMultiSegment();
				}
				switch (marker)
				{
				case 102:
					return ConsumeLiteralMultiSegment(JsonConstants.FalseValue, JsonTokenType.False);
				case 116:
					return ConsumeLiteralMultiSegment(JsonConstants.TrueValue, JsonTokenType.True);
				case 110:
					return ConsumeLiteralMultiSegment(JsonConstants.NullValue, JsonTokenType.Null);
				}
				switch (_readerOptions.CommentHandling)
				{
				case JsonCommentHandling.Allow:
					if (marker == 47)
					{
						SequencePosition currentPosition2 = _currentPosition;
						if (!SkipOrConsumeCommentMultiSegmentWithRollback())
						{
							_currentPosition = currentPosition2;
							return false;
						}
						return true;
					}
					break;
				default:
				{
					if (marker != 47)
					{
						break;
					}
					SequencePosition currentPosition = _currentPosition;
					if (SkipCommentMultiSegment(out var _))
					{
						if (_consumed >= (uint)_buffer.Length)
						{
							if (_isNotPrimitive && IsLastSpan && _tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive, 0);
							}
							if (!GetNextSpan())
							{
								if (_isNotPrimitive && IsLastSpan && _tokenType != JsonTokenType.EndArray && _tokenType != JsonTokenType.EndObject)
								{
									ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidEndOfJsonNonPrimitive, 0);
								}
								_currentPosition = currentPosition;
								return false;
							}
						}
						marker = _buffer[_consumed];
						if (marker <= 32)
						{
							SkipWhiteSpaceMultiSegment();
							if (!HasMoreDataMultiSegment())
							{
								_currentPosition = currentPosition;
								return false;
							}
							marker = _buffer[_consumed];
						}
						goto IL_01a8;
					}
					_currentPosition = currentPosition;
					return false;
				}
				case JsonCommentHandling.Disallow:
					break;
				}
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfValueNotFound, marker);
				break;
			}
			break;
			IL_01a8:
			TokenStartIndex = BytesConsumed;
		}
		return true;
	}

	private bool ConsumeLiteralMultiSegment(ReadOnlySpan<byte> literal, JsonTokenType tokenType)
	{
		ReadOnlySpan<byte> span = _buffer.Slice(_consumed);
		int consumed = literal.Length;
		if (!span.StartsWith(literal))
		{
			int consumed2 = _consumed;
			if (!CheckLiteralMultiSegment(span, literal, out consumed))
			{
				_consumed = consumed2;
				return false;
			}
		}
		else
		{
			ValueSpan = span.Slice(0, literal.Length);
			HasValueSequence = false;
		}
		_tokenType = tokenType;
		_consumed += consumed;
		_bytePositionInLine += consumed;
		return true;
	}

	private bool CheckLiteralMultiSegment(ReadOnlySpan<byte> span, ReadOnlySpan<byte> literal, out int consumed)
	{
		Span<byte> destination = stackalloc byte[5];
		int num = 0;
		long totalConsumed = _totalConsumed;
		SequencePosition currentPosition = _currentPosition;
		if (span.Length >= literal.Length || IsLastSpan)
		{
			_bytePositionInLine += FindMismatch(span, literal);
			int num2 = Math.Min(span.Length, (int)_bytePositionInLine + 1);
			span.Slice(0, num2).CopyTo(destination);
			num += num2;
		}
		else if (!literal.StartsWith(span))
		{
			_bytePositionInLine += FindMismatch(span, literal);
			int num3 = Math.Min(span.Length, (int)_bytePositionInLine + 1);
			span.Slice(0, num3).CopyTo(destination);
			num += num3;
		}
		else
		{
			ReadOnlySpan<byte> readOnlySpan = literal.Slice(span.Length);
			SequencePosition currentPosition2 = _currentPosition;
			int consumed2 = _consumed;
			int num4 = literal.Length - readOnlySpan.Length;
			while (true)
			{
				_totalConsumed += num4;
				_bytePositionInLine += num4;
				if (!GetNextSpan())
				{
					_totalConsumed = totalConsumed;
					consumed = 0;
					_currentPosition = currentPosition;
					if (IsLastSpan)
					{
						break;
					}
					return false;
				}
				int num5 = Math.Min(span.Length, destination.Length - num);
				span.Slice(0, num5).CopyTo(destination.Slice(num));
				num += num5;
				span = _buffer;
				if (span.StartsWith(readOnlySpan))
				{
					HasValueSequence = true;
					SequencePosition start = new SequencePosition(currentPosition2.GetObject(), currentPosition2.GetInteger() + consumed2);
					SequencePosition end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + readOnlySpan.Length);
					ValueSequence = _sequence.Slice(start, end);
					consumed = readOnlySpan.Length;
					return true;
				}
				if (!readOnlySpan.StartsWith(span))
				{
					_bytePositionInLine += FindMismatch(span, readOnlySpan);
					num5 = Math.Min(span.Length, (int)_bytePositionInLine + 1);
					span.Slice(0, num5).CopyTo(destination.Slice(num));
					num += num5;
					break;
				}
				readOnlySpan = readOnlySpan.Slice(span.Length);
				num4 = span.Length;
			}
		}
		_totalConsumed = totalConsumed;
		consumed = 0;
		_currentPosition = currentPosition;
		throw GetInvalidLiteralMultiSegment(destination.Slice(0, num).ToArray());
	}

	private int FindMismatch(ReadOnlySpan<byte> span, ReadOnlySpan<byte> literal)
	{
		int num = 0;
		int num2 = Math.Min(span.Length, literal.Length);
		int i;
		for (i = 0; i < num2 && span[i] == literal[i]; i++)
		{
		}
		return i;
	}

	private JsonException GetInvalidLiteralMultiSegment(ReadOnlySpan<byte> span)
	{
		return ThrowHelper.GetJsonReaderException(ref this, span[0] switch
		{
			116 => ExceptionResource.ExpectedTrue, 
			102 => ExceptionResource.ExpectedFalse, 
			_ => ExceptionResource.ExpectedNull, 
		}, 0, span);
	}

	private bool ConsumeNumberMultiSegment()
	{
		if (!TryGetNumberMultiSegment(_buffer.Slice(_consumed), out var consumed))
		{
			return false;
		}
		_tokenType = JsonTokenType.Number;
		_consumed += consumed;
		if (_consumed >= (uint)_buffer.Length && _isNotPrimitive)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, _buffer[_consumed - 1]);
		}
		return true;
	}

	private bool ConsumePropertyNameMultiSegment()
	{
		_trailingCommaBeforeComment = false;
		if (!ConsumeStringMultiSegment())
		{
			return false;
		}
		if (!HasMoreDataMultiSegment(ExceptionResource.ExpectedValueAfterPropertyNameNotFound))
		{
			return false;
		}
		byte b = _buffer[_consumed];
		if (b <= 32)
		{
			SkipWhiteSpaceMultiSegment();
			if (!HasMoreDataMultiSegment(ExceptionResource.ExpectedValueAfterPropertyNameNotFound))
			{
				return false;
			}
			b = _buffer[_consumed];
		}
		if (b != 58)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedSeparatorAfterPropertyNameNotFound, b);
		}
		_consumed++;
		_bytePositionInLine++;
		_tokenType = JsonTokenType.PropertyName;
		return true;
	}

	private bool ConsumeStringMultiSegment()
	{
		ReadOnlySpan<byte> readOnlySpan = _buffer.Slice(_consumed + 1);
		int num = readOnlySpan.IndexOfQuoteOrAnyControlOrBackSlash();
		if (num >= 0)
		{
			byte b = readOnlySpan[num];
			if (b == 34)
			{
				_bytePositionInLine += num + 2;
				ValueSpan = readOnlySpan.Slice(0, num);
				HasValueSequence = false;
				_stringHasEscaping = false;
				_tokenType = JsonTokenType.String;
				_consumed += num + 2;
				return true;
			}
			return ConsumeStringAndValidateMultiSegment(readOnlySpan, num);
		}
		if (IsLastSpan)
		{
			_bytePositionInLine += readOnlySpan.Length + 1;
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
		}
		return ConsumeStringNextSegment();
	}

	private bool ConsumeStringNextSegment()
	{
		PartialStateForRollback state = CaptureState();
		HasValueSequence = true;
		int num = _buffer.Length - _consumed;
		ReadOnlySpan<byte> buffer;
		int num2;
		while (true)
		{
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					_bytePositionInLine += num;
					RollBackState(in state, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
				}
				RollBackState(in state);
				return false;
			}
			buffer = _buffer;
			num2 = buffer.IndexOfQuoteOrAnyControlOrBackSlash();
			if (num2 >= 0)
			{
				break;
			}
			_totalConsumed += buffer.Length;
			_bytePositionInLine += buffer.Length;
		}
		byte b = buffer[num2];
		SequencePosition end;
		if (b == 34)
		{
			end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + num2);
			_bytePositionInLine += num + num2 + 1;
			_totalConsumed += num;
			_consumed = num2 + 1;
			_stringHasEscaping = false;
		}
		else
		{
			_bytePositionInLine += num + num2;
			_stringHasEscaping = true;
			bool flag = false;
			while (true)
			{
				if (num2 < buffer.Length)
				{
					byte b2 = buffer[num2];
					if (b2 == 34)
					{
						if (!flag)
						{
							break;
						}
						flag = false;
					}
					else if (b2 == 92)
					{
						flag = !flag;
					}
					else if (flag)
					{
						int num3 = JsonConstants.EscapableChars.IndexOf(b2);
						if (num3 == -1)
						{
							RollBackState(in state, isError: true);
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterAfterEscapeWithinString, b2);
						}
						if (b2 == 117)
						{
							_bytePositionInLine++;
							int num4 = 0;
							int num5 = num2 + 1;
							while (true)
							{
								if (num5 < buffer.Length)
								{
									byte nextByte = buffer[num5];
									if (!JsonReaderHelper.IsHexDigit(nextByte))
									{
										RollBackState(in state, isError: true);
										ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidHexCharacterWithinString, nextByte);
									}
									num4++;
									_bytePositionInLine++;
									if (num4 >= 4)
									{
										break;
									}
									num5++;
									continue;
								}
								if (!GetNextSpan())
								{
									if (IsLastSpan)
									{
										RollBackState(in state, isError: true);
										ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
									}
									RollBackState(in state);
									return false;
								}
								_totalConsumed += buffer.Length;
								buffer = _buffer;
								num5 = 0;
							}
							flag = false;
							num2 = num5 + 1;
							continue;
						}
						flag = false;
					}
					else if (b2 < 32)
					{
						RollBackState(in state, isError: true);
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterWithinString, b2);
					}
					_bytePositionInLine++;
					num2++;
					continue;
				}
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						RollBackState(in state, isError: true);
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
					}
					RollBackState(in state);
					return false;
				}
				_totalConsumed += buffer.Length;
				buffer = _buffer;
				num2 = 0;
			}
			_bytePositionInLine++;
			_consumed = num2 + 1;
			_totalConsumed += num;
			end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + num2);
		}
		SequencePosition startPosition = state.GetStartPosition(1);
		ValueSequence = _sequence.Slice(startPosition, end);
		_tokenType = JsonTokenType.String;
		return true;
	}

	private bool ConsumeStringAndValidateMultiSegment(ReadOnlySpan<byte> data, int idx)
	{
		PartialStateForRollback state = CaptureState();
		HasValueSequence = false;
		int num = _buffer.Length - _consumed;
		_bytePositionInLine += idx + 1;
		bool flag = false;
		while (true)
		{
			if (idx < data.Length)
			{
				byte b = data[idx];
				switch (b)
				{
				case 34:
					if (flag)
					{
						flag = false;
						goto IL_01b7;
					}
					if (HasValueSequence)
					{
						_bytePositionInLine++;
						_consumed = idx + 1;
						_totalConsumed += num;
						SequencePosition end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + idx);
						SequencePosition startPosition = state.GetStartPosition(1);
						ValueSequence = _sequence.Slice(startPosition, end);
					}
					else
					{
						_bytePositionInLine++;
						_consumed += idx + 2;
						ValueSpan = data.Slice(0, idx);
					}
					_stringHasEscaping = true;
					_tokenType = JsonTokenType.String;
					return true;
				case 92:
					flag = !flag;
					goto IL_01b7;
				default:
					{
						if (flag)
						{
							int num2 = JsonConstants.EscapableChars.IndexOf(b);
							if (num2 == -1)
							{
								RollBackState(in state, isError: true);
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterAfterEscapeWithinString, b);
							}
							if (b == 117)
							{
								_bytePositionInLine++;
								int num3 = 0;
								int num4 = idx + 1;
								while (true)
								{
									if (num4 < data.Length)
									{
										byte nextByte = data[num4];
										if (!JsonReaderHelper.IsHexDigit(nextByte))
										{
											RollBackState(in state, isError: true);
											ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidHexCharacterWithinString, nextByte);
										}
										num3++;
										_bytePositionInLine++;
										if (num3 >= 4)
										{
											break;
										}
										num4++;
										continue;
									}
									if (!GetNextSpan())
									{
										if (IsLastSpan)
										{
											RollBackState(in state, isError: true);
											ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
										}
										RollBackState(in state);
										return false;
									}
									if (HasValueSequence)
									{
										_totalConsumed += data.Length;
									}
									data = _buffer;
									num4 = 0;
									HasValueSequence = true;
								}
								flag = false;
								idx = num4 + 1;
								break;
							}
							flag = false;
						}
						else if (b < 32)
						{
							RollBackState(in state, isError: true);
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterWithinString, b);
						}
						goto IL_01b7;
					}
					IL_01b7:
					_bytePositionInLine++;
					idx++;
					break;
				}
			}
			else
			{
				if (!GetNextSpan())
				{
					break;
				}
				if (HasValueSequence)
				{
					_totalConsumed += data.Length;
				}
				data = _buffer;
				idx = 0;
				HasValueSequence = true;
			}
		}
		if (IsLastSpan)
		{
			RollBackState(in state, isError: true);
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.EndOfStringNotFound, 0);
		}
		RollBackState(in state);
		return false;
	}

	private void RollBackState(in PartialStateForRollback state, bool isError = false)
	{
		_totalConsumed = state._prevTotalConsumed;
		if (!isError)
		{
			_bytePositionInLine = state._prevBytePositionInLine;
		}
		_consumed = state._prevConsumed;
		_currentPosition = state._prevCurrentPosition;
	}

	private bool TryGetNumberMultiSegment(ReadOnlySpan<byte> data, out int consumed)
	{
		PartialStateForRollback rollBackState = CaptureState();
		consumed = 0;
		int i = 0;
		ConsumeNumberResult consumeNumberResult = ConsumeNegativeSignMultiSegment(ref data, ref i, in rollBackState);
		if (consumeNumberResult == ConsumeNumberResult.NeedMoreData)
		{
			RollBackState(in rollBackState);
			return false;
		}
		byte b = data[i];
		if (b == 48)
		{
			ConsumeNumberResult consumeNumberResult2 = ConsumeZeroMultiSegment(ref data, ref i, in rollBackState);
			if (consumeNumberResult2 == ConsumeNumberResult.NeedMoreData)
			{
				RollBackState(in rollBackState);
				return false;
			}
			if (consumeNumberResult2 != 0)
			{
				b = data[i];
				goto IL_00bf;
			}
		}
		else
		{
			ConsumeNumberResult consumeNumberResult3 = ConsumeIntegerDigitsMultiSegment(ref data, ref i);
			if (consumeNumberResult3 == ConsumeNumberResult.NeedMoreData)
			{
				RollBackState(in rollBackState);
				return false;
			}
			if (consumeNumberResult3 != 0)
			{
				b = data[i];
				if (b != 46 && b != 69 && b != 101)
				{
					RollBackState(in rollBackState, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, b);
				}
				goto IL_00bf;
			}
		}
		goto IL_01b1;
		IL_00bf:
		if (b == 46)
		{
			i++;
			_bytePositionInLine++;
			ConsumeNumberResult consumeNumberResult4 = ConsumeDecimalDigitsMultiSegment(ref data, ref i, in rollBackState);
			if (consumeNumberResult4 == ConsumeNumberResult.NeedMoreData)
			{
				RollBackState(in rollBackState);
				return false;
			}
			if (consumeNumberResult4 == ConsumeNumberResult.Success)
			{
				goto IL_01b1;
			}
			b = data[i];
			if (b != 69 && b != 101)
			{
				RollBackState(in rollBackState, isError: true);
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedNextDigitEValueNotFound, b);
			}
		}
		i++;
		_bytePositionInLine++;
		consumeNumberResult = ConsumeSignMultiSegment(ref data, ref i, in rollBackState);
		if (consumeNumberResult == ConsumeNumberResult.NeedMoreData)
		{
			RollBackState(in rollBackState);
			return false;
		}
		i++;
		_bytePositionInLine++;
		switch (ConsumeIntegerDigitsMultiSegment(ref data, ref i))
		{
		case ConsumeNumberResult.NeedMoreData:
			RollBackState(in rollBackState);
			return false;
		default:
			RollBackState(in rollBackState, isError: true);
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndOfDigitNotFound, data[i]);
			break;
		case ConsumeNumberResult.Success:
			break;
		}
		goto IL_01b1;
		IL_01b1:
		if (HasValueSequence)
		{
			SequencePosition startPosition = rollBackState.GetStartPosition();
			SequencePosition end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + i);
			ValueSequence = _sequence.Slice(startPosition, end);
			consumed = i;
		}
		else
		{
			ValueSpan = data.Slice(0, i);
			consumed = i;
		}
		return true;
	}

	private ConsumeNumberResult ConsumeNegativeSignMultiSegment(ref ReadOnlySpan<byte> data, ref int i, in PartialStateForRollback rollBackState)
	{
		byte b = data[i];
		if (b == 45)
		{
			i++;
			_bytePositionInLine++;
			if (i >= data.Length)
			{
				if (IsLastSpan)
				{
					RollBackState(in rollBackState, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						RollBackState(in rollBackState, isError: true);
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
					}
					return ConsumeNumberResult.NeedMoreData;
				}
				_totalConsumed += i;
				HasValueSequence = true;
				i = 0;
				data = _buffer;
			}
			b = data[i];
			if (!JsonHelpers.IsDigit(b))
			{
				RollBackState(in rollBackState, isError: true);
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterSign, b);
			}
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private ConsumeNumberResult ConsumeZeroMultiSegment(ref ReadOnlySpan<byte> data, ref int i, in PartialStateForRollback rollBackState)
	{
		i++;
		_bytePositionInLine++;
		byte value;
		if (i < data.Length)
		{
			value = data[i];
			if (JsonConstants.Delimiters.IndexOf(value) >= 0)
			{
				return ConsumeNumberResult.Success;
			}
		}
		else
		{
			if (IsLastSpan)
			{
				return ConsumeNumberResult.Success;
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					return ConsumeNumberResult.Success;
				}
				return ConsumeNumberResult.NeedMoreData;
			}
			_totalConsumed += i;
			HasValueSequence = true;
			i = 0;
			data = _buffer;
			value = data[i];
			if (JsonConstants.Delimiters.IndexOf(value) >= 0)
			{
				return ConsumeNumberResult.Success;
			}
		}
		value = data[i];
		if (value != 46 && value != 69 && value != 101)
		{
			RollBackState(in rollBackState, isError: true);
			ThrowHelper.ThrowJsonReaderException(ref this, JsonHelpers.IsInRangeInclusive(value, 48, 57) ? ExceptionResource.InvalidLeadingZeroInNumber : ExceptionResource.ExpectedEndOfDigitNotFound, value);
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private ConsumeNumberResult ConsumeIntegerDigitsMultiSegment(ref ReadOnlySpan<byte> data, ref int i)
	{
		byte value = 0;
		int num = 0;
		while (i < data.Length)
		{
			value = data[i];
			if (!JsonHelpers.IsDigit(value))
			{
				break;
			}
			num++;
			i++;
		}
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				_bytePositionInLine += num;
				return ConsumeNumberResult.Success;
			}
			while (true)
			{
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						_bytePositionInLine += num;
						return ConsumeNumberResult.Success;
					}
					return ConsumeNumberResult.NeedMoreData;
				}
				_totalConsumed += i;
				_bytePositionInLine += num;
				num = 0;
				HasValueSequence = true;
				i = 0;
				data = _buffer;
				while (i < data.Length)
				{
					value = data[i];
					if (!JsonHelpers.IsDigit(value))
					{
						break;
					}
					i++;
				}
				_bytePositionInLine += i;
				if (i < data.Length)
				{
					break;
				}
				if (IsLastSpan)
				{
					return ConsumeNumberResult.Success;
				}
			}
		}
		else
		{
			_bytePositionInLine += num;
		}
		if (JsonConstants.Delimiters.IndexOf(value) >= 0)
		{
			return ConsumeNumberResult.Success;
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private ConsumeNumberResult ConsumeDecimalDigitsMultiSegment(ref ReadOnlySpan<byte> data, ref int i, in PartialStateForRollback rollBackState)
	{
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				RollBackState(in rollBackState, isError: true);
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					RollBackState(in rollBackState, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				return ConsumeNumberResult.NeedMoreData;
			}
			_totalConsumed += i;
			HasValueSequence = true;
			i = 0;
			data = _buffer;
		}
		byte b = data[i];
		if (!JsonHelpers.IsDigit(b))
		{
			RollBackState(in rollBackState, isError: true);
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterDecimal, b);
		}
		i++;
		_bytePositionInLine++;
		return ConsumeIntegerDigitsMultiSegment(ref data, ref i);
	}

	private ConsumeNumberResult ConsumeSignMultiSegment(ref ReadOnlySpan<byte> data, ref int i, in PartialStateForRollback rollBackState)
	{
		if (i >= data.Length)
		{
			if (IsLastSpan)
			{
				RollBackState(in rollBackState, isError: true);
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					RollBackState(in rollBackState, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				return ConsumeNumberResult.NeedMoreData;
			}
			_totalConsumed += i;
			HasValueSequence = true;
			i = 0;
			data = _buffer;
		}
		byte b = data[i];
		if (b == 43 || b == 45)
		{
			i++;
			_bytePositionInLine++;
			if (i >= data.Length)
			{
				if (IsLastSpan)
				{
					RollBackState(in rollBackState, isError: true);
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
				}
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						RollBackState(in rollBackState, isError: true);
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundEndOfData, 0);
					}
					return ConsumeNumberResult.NeedMoreData;
				}
				_totalConsumed += i;
				HasValueSequence = true;
				i = 0;
				data = _buffer;
			}
			b = data[i];
		}
		if (!JsonHelpers.IsDigit(b))
		{
			RollBackState(in rollBackState, isError: true);
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.RequiredDigitNotFoundAfterSign, b);
		}
		return ConsumeNumberResult.OperationIncomplete;
	}

	private bool ConsumeNextTokenOrRollbackMultiSegment(byte marker)
	{
		long totalConsumed = _totalConsumed;
		int consumed = _consumed;
		long bytePositionInLine = _bytePositionInLine;
		long lineNumber = _lineNumber;
		JsonTokenType tokenType = _tokenType;
		SequencePosition currentPosition = _currentPosition;
		bool trailingCommaBeforeComment = _trailingCommaBeforeComment;
		switch (ConsumeNextTokenMultiSegment(marker))
		{
		case ConsumeTokenResult.Success:
			return true;
		case ConsumeTokenResult.NotEnoughDataRollBackState:
			_consumed = consumed;
			_tokenType = tokenType;
			_bytePositionInLine = bytePositionInLine;
			_lineNumber = lineNumber;
			_totalConsumed = totalConsumed;
			_currentPosition = currentPosition;
			_trailingCommaBeforeComment = trailingCommaBeforeComment;
			break;
		}
		return false;
	}

	private ConsumeTokenResult ConsumeNextTokenMultiSegment(byte marker)
	{
		if (_readerOptions.CommentHandling != 0)
		{
			if (_readerOptions.CommentHandling != JsonCommentHandling.Allow)
			{
				return ConsumeNextTokenUntilAfterAllCommentsAreSkippedMultiSegment(marker);
			}
			if (marker == 47)
			{
				if (!SkipOrConsumeCommentMultiSegmentWithRollback())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (_tokenType == JsonTokenType.Comment)
			{
				return ConsumeNextTokenFromLastNonCommentTokenMultiSegment();
			}
		}
		if (_bitStack.CurrentDepth == 0)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, marker);
		}
		switch (marker)
		{
		case 44:
		{
			_consumed++;
			_bytePositionInLine++;
			if (_consumed >= (uint)_buffer.Length)
			{
				if (IsLastSpan)
				{
					_consumed--;
					_bytePositionInLine--;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
				}
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						_consumed--;
						_bytePositionInLine--;
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
					}
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
			}
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpaceMultiSegment();
				if (!HasMoreDataMultiSegment(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				b = _buffer[_consumed];
			}
			TokenStartIndex = BytesConsumed;
			if (_readerOptions.CommentHandling == JsonCommentHandling.Allow && b == 47)
			{
				_trailingCommaBeforeComment = true;
				if (!SkipOrConsumeCommentMultiSegmentWithRollback())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (_inObject)
			{
				if (b != 34)
				{
					if (b == 125)
					{
						if (_readerOptions.AllowTrailingCommas)
						{
							EndObject();
							return ConsumeTokenResult.Success;
						}
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
					}
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
				}
				if (!ConsumePropertyNameMultiSegment())
				{
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				}
				return ConsumeTokenResult.Success;
			}
			if (b == 93)
			{
				if (_readerOptions.AllowTrailingCommas)
				{
					EndArray();
					return ConsumeTokenResult.Success;
				}
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
			}
			if (!ConsumeValueMultiSegment(b))
			{
				return ConsumeTokenResult.NotEnoughDataRollBackState;
			}
			return ConsumeTokenResult.Success;
		}
		case 125:
			EndObject();
			break;
		case 93:
			EndArray();
			break;
		default:
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.FoundInvalidCharacter, marker);
			break;
		}
		return ConsumeTokenResult.Success;
	}

	private ConsumeTokenResult ConsumeNextTokenFromLastNonCommentTokenMultiSegment()
	{
		if (JsonReaderHelper.IsTokenTypePrimitive(_previousTokenType))
		{
			_tokenType = (_inObject ? JsonTokenType.StartObject : JsonTokenType.StartArray);
		}
		else
		{
			_tokenType = _previousTokenType;
		}
		if (HasMoreDataMultiSegment())
		{
			byte b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpaceMultiSegment();
				if (!HasMoreDataMultiSegment())
				{
					goto IL_0393;
				}
				b = _buffer[_consumed];
			}
			if (_bitStack.CurrentDepth == 0 && _tokenType != 0)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, b);
			}
			TokenStartIndex = BytesConsumed;
			if (b != 44)
			{
				if (b == 125)
				{
					EndObject();
				}
				else
				{
					if (b != 93)
					{
						if (_tokenType == JsonTokenType.None)
						{
							if (ReadFirstTokenMultiSegment(b))
							{
								goto IL_0391;
							}
						}
						else if (_tokenType == JsonTokenType.StartObject)
						{
							if (b != 34)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
							}
							long totalConsumed = _totalConsumed;
							int consumed = _consumed;
							long bytePositionInLine = _bytePositionInLine;
							long lineNumber = _lineNumber;
							if (ConsumePropertyNameMultiSegment())
							{
								goto IL_0391;
							}
							_consumed = consumed;
							_tokenType = JsonTokenType.StartObject;
							_bytePositionInLine = bytePositionInLine;
							_lineNumber = lineNumber;
							_totalConsumed = totalConsumed;
						}
						else if (_tokenType == JsonTokenType.StartArray)
						{
							if (ConsumeValueMultiSegment(b))
							{
								goto IL_0391;
							}
						}
						else if (_tokenType == JsonTokenType.PropertyName)
						{
							if (ConsumeValueMultiSegment(b))
							{
								goto IL_0391;
							}
						}
						else if (_inObject)
						{
							if (b != 34)
							{
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
							}
							if (ConsumePropertyNameMultiSegment())
							{
								goto IL_0391;
							}
						}
						else if (ConsumeValueMultiSegment(b))
						{
							goto IL_0391;
						}
						goto IL_0393;
					}
					EndArray();
				}
				goto IL_0391;
			}
			if ((int)_previousTokenType <= 1 || _previousTokenType == JsonTokenType.StartArray || _trailingCommaBeforeComment)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueAfterComment, b);
			}
			_consumed++;
			_bytePositionInLine++;
			if (_consumed >= (uint)_buffer.Length)
			{
				if (IsLastSpan)
				{
					_consumed--;
					_bytePositionInLine--;
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
				}
				if (!GetNextSpan())
				{
					if (IsLastSpan)
					{
						_consumed--;
						_bytePositionInLine--;
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
					}
					goto IL_0393;
				}
			}
			b = _buffer[_consumed];
			if (b <= 32)
			{
				SkipWhiteSpaceMultiSegment();
				if (!HasMoreDataMultiSegment(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
				{
					goto IL_0393;
				}
				b = _buffer[_consumed];
			}
			TokenStartIndex = BytesConsumed;
			if (b == 47)
			{
				_trailingCommaBeforeComment = true;
				if (SkipOrConsumeCommentMultiSegmentWithRollback())
				{
					goto IL_0391;
				}
			}
			else if (_inObject)
			{
				if (b != 34)
				{
					if (b == 125)
					{
						if (_readerOptions.AllowTrailingCommas)
						{
							EndObject();
							goto IL_0391;
						}
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
					}
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, b);
				}
				if (ConsumePropertyNameMultiSegment())
				{
					goto IL_0391;
				}
			}
			else
			{
				if (b == 93)
				{
					if (_readerOptions.AllowTrailingCommas)
					{
						EndArray();
						goto IL_0391;
					}
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
				}
				if (ConsumeValueMultiSegment(b))
				{
					goto IL_0391;
				}
			}
		}
		goto IL_0393;
		IL_0393:
		return ConsumeTokenResult.NotEnoughDataRollBackState;
		IL_0391:
		return ConsumeTokenResult.Success;
	}

	private bool SkipAllCommentsMultiSegment(ref byte marker)
	{
		while (true)
		{
			if (marker == 47)
			{
				if (!SkipOrConsumeCommentMultiSegmentWithRollback() || !HasMoreDataMultiSegment())
				{
					break;
				}
				marker = _buffer[_consumed];
				if (marker <= 32)
				{
					SkipWhiteSpaceMultiSegment();
					if (!HasMoreDataMultiSegment())
					{
						break;
					}
					marker = _buffer[_consumed];
				}
				continue;
			}
			return true;
		}
		return false;
	}

	private bool SkipAllCommentsMultiSegment(ref byte marker, ExceptionResource resource)
	{
		while (true)
		{
			if (marker == 47)
			{
				if (!SkipOrConsumeCommentMultiSegmentWithRollback() || !HasMoreDataMultiSegment(resource))
				{
					break;
				}
				marker = _buffer[_consumed];
				if (marker <= 32)
				{
					SkipWhiteSpaceMultiSegment();
					if (!HasMoreDataMultiSegment(resource))
					{
						break;
					}
					marker = _buffer[_consumed];
				}
				continue;
			}
			return true;
		}
		return false;
	}

	private ConsumeTokenResult ConsumeNextTokenUntilAfterAllCommentsAreSkippedMultiSegment(byte marker)
	{
		if (SkipAllCommentsMultiSegment(ref marker))
		{
			TokenStartIndex = BytesConsumed;
			if (_tokenType == JsonTokenType.StartObject)
			{
				if (marker == 125)
				{
					EndObject();
				}
				else
				{
					if (marker != 34)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, marker);
					}
					long totalConsumed = _totalConsumed;
					int consumed = _consumed;
					long bytePositionInLine = _bytePositionInLine;
					long lineNumber = _lineNumber;
					SequencePosition currentPosition = _currentPosition;
					if (!ConsumePropertyNameMultiSegment())
					{
						_consumed = consumed;
						_tokenType = JsonTokenType.StartObject;
						_bytePositionInLine = bytePositionInLine;
						_lineNumber = lineNumber;
						_totalConsumed = totalConsumed;
						_currentPosition = currentPosition;
						goto IL_02e7;
					}
				}
			}
			else if (_tokenType == JsonTokenType.StartArray)
			{
				if (marker == 93)
				{
					EndArray();
				}
				else if (!ConsumeValueMultiSegment(marker))
				{
					goto IL_02e7;
				}
			}
			else if (_tokenType == JsonTokenType.PropertyName)
			{
				if (!ConsumeValueMultiSegment(marker))
				{
					goto IL_02e7;
				}
			}
			else if (_bitStack.CurrentDepth == 0)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedEndAfterSingleJson, marker);
			}
			else
			{
				switch (marker)
				{
				case 44:
					_consumed++;
					_bytePositionInLine++;
					if (_consumed >= (uint)_buffer.Length)
					{
						if (IsLastSpan)
						{
							_consumed--;
							_bytePositionInLine--;
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
						}
						if (!GetNextSpan())
						{
							if (IsLastSpan)
							{
								_consumed--;
								_bytePositionInLine--;
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound, 0);
							}
							return ConsumeTokenResult.NotEnoughDataRollBackState;
						}
					}
					marker = _buffer[_consumed];
					if (marker <= 32)
					{
						SkipWhiteSpaceMultiSegment();
						if (!HasMoreDataMultiSegment(ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
						{
							return ConsumeTokenResult.NotEnoughDataRollBackState;
						}
						marker = _buffer[_consumed];
					}
					if (SkipAllCommentsMultiSegment(ref marker, ExceptionResource.ExpectedStartOfPropertyOrValueNotFound))
					{
						TokenStartIndex = BytesConsumed;
						if (_inObject)
						{
							if (marker != 34)
							{
								if (marker == 125)
								{
									if (_readerOptions.AllowTrailingCommas)
									{
										EndObject();
										break;
									}
									ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd, 0);
								}
								ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.ExpectedStartOfPropertyNotFound, marker);
							}
							if (!ConsumePropertyNameMultiSegment())
							{
								return ConsumeTokenResult.NotEnoughDataRollBackState;
							}
							return ConsumeTokenResult.Success;
						}
						if (marker == 93)
						{
							if (_readerOptions.AllowTrailingCommas)
							{
								EndArray();
								break;
							}
							ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd, 0);
						}
						if (!ConsumeValueMultiSegment(marker))
						{
							return ConsumeTokenResult.NotEnoughDataRollBackState;
						}
						return ConsumeTokenResult.Success;
					}
					return ConsumeTokenResult.NotEnoughDataRollBackState;
				case 125:
					EndObject();
					break;
				case 93:
					EndArray();
					break;
				default:
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.FoundInvalidCharacter, marker);
					break;
				}
			}
			return ConsumeTokenResult.Success;
		}
		goto IL_02e7;
		IL_02e7:
		return ConsumeTokenResult.IncompleteNoRollBackNecessary;
	}

	private bool SkipOrConsumeCommentMultiSegmentWithRollback()
	{
		long bytesConsumed = BytesConsumed;
		SequencePosition start = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + _consumed);
		int tailBytesToIgnore;
		bool flag = SkipCommentMultiSegment(out tailBytesToIgnore);
		if (flag)
		{
			if (_readerOptions.CommentHandling == JsonCommentHandling.Allow)
			{
				SequencePosition end = new SequencePosition(_currentPosition.GetObject(), _currentPosition.GetInteger() + _consumed);
				ReadOnlySequence<byte> readOnlySequence = _sequence.Slice(start, end);
				readOnlySequence = readOnlySequence.Slice(2L, readOnlySequence.Length - 2 - tailBytesToIgnore);
				HasValueSequence = !readOnlySequence.IsSingleSegment;
				if (HasValueSequence)
				{
					ValueSequence = readOnlySequence;
				}
				else
				{
					ValueSpan = readOnlySequence.First.Span;
				}
				if (_tokenType != JsonTokenType.Comment)
				{
					_previousTokenType = _tokenType;
				}
				_tokenType = JsonTokenType.Comment;
			}
		}
		else
		{
			_totalConsumed = bytesConsumed;
			_consumed = 0;
		}
		return flag;
	}

	private bool SkipCommentMultiSegment(out int tailBytesToIgnore)
	{
		_consumed++;
		_bytePositionInLine++;
		ReadOnlySpan<byte> localBuffer = _buffer.Slice(_consumed);
		if (localBuffer.Length == 0)
		{
			if (IsLastSpan)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
				}
				tailBytesToIgnore = 0;
				return false;
			}
			localBuffer = _buffer;
		}
		byte b = localBuffer[0];
		if (b != 47 && b != 42)
		{
			ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.InvalidCharacterAtStartOfComment, b);
		}
		bool flag = b == 42;
		_consumed++;
		_bytePositionInLine++;
		localBuffer = localBuffer.Slice(1);
		if (localBuffer.Length == 0)
		{
			if (IsLastSpan)
			{
				tailBytesToIgnore = 0;
				if (flag)
				{
					ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
				}
				return true;
			}
			if (!GetNextSpan())
			{
				tailBytesToIgnore = 0;
				if (IsLastSpan)
				{
					if (flag)
					{
						ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
					}
					return true;
				}
				return false;
			}
			localBuffer = _buffer;
		}
		if (flag)
		{
			tailBytesToIgnore = 2;
			return SkipMultiLineCommentMultiSegment(localBuffer);
		}
		return SkipSingleLineCommentMultiSegment(localBuffer, out tailBytesToIgnore);
	}

	private bool SkipSingleLineCommentMultiSegment(ReadOnlySpan<byte> localBuffer, out int tailBytesToSkip)
	{
		bool flag = false;
		int dangerousLineSeparatorBytesConsumed = 0;
		tailBytesToSkip = 0;
		while (true)
		{
			if (flag)
			{
				if (localBuffer[0] == 10)
				{
					tailBytesToSkip++;
					_consumed++;
				}
				break;
			}
			int num = FindLineSeparatorMultiSegment(localBuffer, ref dangerousLineSeparatorBytesConsumed);
			if (num != -1)
			{
				tailBytesToSkip++;
				_consumed += num + 1;
				_bytePositionInLine += num + 1;
				if (localBuffer[num] == 10)
				{
					break;
				}
				if (num < localBuffer.Length - 1)
				{
					if (localBuffer[num + 1] == 10)
					{
						tailBytesToSkip++;
						_consumed++;
						_bytePositionInLine++;
					}
					break;
				}
				flag = true;
			}
			else
			{
				_consumed += localBuffer.Length;
				_bytePositionInLine += localBuffer.Length;
			}
			if (IsLastSpan)
			{
				if (flag)
				{
					break;
				}
				return true;
			}
			if (!GetNextSpan())
			{
				if (IsLastSpan)
				{
					if (flag)
					{
						break;
					}
					return true;
				}
				return false;
			}
			localBuffer = _buffer;
		}
		_bytePositionInLine = 0L;
		_lineNumber++;
		return true;
	}

	private int FindLineSeparatorMultiSegment(ReadOnlySpan<byte> localBuffer, ref int dangerousLineSeparatorBytesConsumed)
	{
		if (dangerousLineSeparatorBytesConsumed != 0)
		{
			ThrowOnDangerousLineSeparatorMultiSegment(localBuffer, ref dangerousLineSeparatorBytesConsumed);
			if (dangerousLineSeparatorBytesConsumed != 0)
			{
				return -1;
			}
		}
		int num = 0;
		do
		{
			int num2 = localBuffer.IndexOfAny<byte>(10, 13, 226);
			dangerousLineSeparatorBytesConsumed = 0;
			if (num2 == -1)
			{
				return -1;
			}
			if (localBuffer[num2] != 226)
			{
				return num + num2;
			}
			int num3 = num2 + 1;
			localBuffer = localBuffer.Slice(num3);
			num += num3;
			dangerousLineSeparatorBytesConsumed++;
			ThrowOnDangerousLineSeparatorMultiSegment(localBuffer, ref dangerousLineSeparatorBytesConsumed);
		}
		while (dangerousLineSeparatorBytesConsumed == 0);
		return -1;
	}

	private void ThrowOnDangerousLineSeparatorMultiSegment(ReadOnlySpan<byte> localBuffer, ref int dangerousLineSeparatorBytesConsumed)
	{
		if (localBuffer.IsEmpty)
		{
			return;
		}
		if (dangerousLineSeparatorBytesConsumed == 1)
		{
			if (localBuffer[0] != 128)
			{
				dangerousLineSeparatorBytesConsumed = 0;
				return;
			}
			localBuffer = localBuffer.Slice(1);
			dangerousLineSeparatorBytesConsumed++;
			if (localBuffer.IsEmpty)
			{
				return;
			}
		}
		if (dangerousLineSeparatorBytesConsumed == 2)
		{
			byte b = localBuffer[0];
			if (b == 168 || b == 169)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfLineSeparator, 0);
			}
			else
			{
				dangerousLineSeparatorBytesConsumed = 0;
			}
		}
	}

	private bool SkipMultiLineCommentMultiSegment(ReadOnlySpan<byte> localBuffer)
	{
		bool flag = false;
		bool flag2 = false;
		while (true)
		{
			if (flag)
			{
				if (localBuffer[0] == 47)
				{
					_consumed++;
					_bytePositionInLine++;
					return true;
				}
				flag = false;
			}
			if (flag2)
			{
				if (localBuffer[0] == 10)
				{
					_consumed++;
					localBuffer = localBuffer.Slice(1);
				}
				flag2 = false;
			}
			int num = localBuffer.IndexOfAny<byte>(42, 10, 13);
			if (num != -1)
			{
				int num2 = num + 1;
				byte b = localBuffer[num];
				localBuffer = localBuffer.Slice(num2);
				_consumed += num2;
				switch (b)
				{
				case 42:
					flag = true;
					_bytePositionInLine += num2;
					break;
				case 10:
					_bytePositionInLine = 0L;
					_lineNumber++;
					break;
				default:
					_bytePositionInLine = 0L;
					_lineNumber++;
					flag2 = true;
					break;
				}
			}
			else
			{
				_consumed += localBuffer.Length;
				_bytePositionInLine += localBuffer.Length;
				localBuffer = ReadOnlySpan<byte>.Empty;
			}
			if (!localBuffer.IsEmpty)
			{
				continue;
			}
			if (IsLastSpan)
			{
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
			}
			if (!GetNextSpan())
			{
				if (!IsLastSpan)
				{
					break;
				}
				ThrowHelper.ThrowJsonReaderException(ref this, ExceptionResource.UnexpectedEndOfDataWhileReadingComment, 0);
			}
			localBuffer = _buffer;
		}
		return false;
	}

	private PartialStateForRollback CaptureState()
	{
		return new PartialStateForRollback(_totalConsumed, _bytePositionInLine, _consumed, _currentPosition);
	}

	public string? GetString()
	{
		if (TokenType == JsonTokenType.Null)
		{
			return null;
		}
		if (TokenType != JsonTokenType.String && TokenType != JsonTokenType.PropertyName)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> readOnlySpan2 = readOnlySpan;
		if (_stringHasEscaping)
		{
			int idx = readOnlySpan2.IndexOf<byte>(92);
			return JsonReaderHelper.GetUnescapedString(readOnlySpan2, idx);
		}
		return JsonReaderHelper.TranscodeHelper(readOnlySpan2);
	}

	public string GetComment()
	{
		if (TokenType != JsonTokenType.Comment)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedComment(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> utf8Unescaped = readOnlySpan;
		return JsonReaderHelper.TranscodeHelper(utf8Unescaped);
	}

	public bool GetBoolean()
	{
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> readOnlySpan2 = readOnlySpan;
		if (TokenType == JsonTokenType.True)
		{
			return true;
		}
		if (TokenType == JsonTokenType.False)
		{
			return false;
		}
		throw ThrowHelper.GetInvalidOperationException_ExpectedBoolean(TokenType);
	}

	public byte[] GetBytesFromBase64()
	{
		if (!TryGetBytesFromBase64(out byte[] value))
		{
			throw ThrowHelper.GetFormatException(DataType.Base64String);
		}
		return value;
	}

	public byte GetByte()
	{
		if (!TryGetByte(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Byte);
		}
		return value;
	}

	internal byte GetByteWithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetByteCore(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.Byte);
		}
		return value;
	}

	[CLSCompliant(false)]
	public sbyte GetSByte()
	{
		if (!TryGetSByte(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.SByte);
		}
		return value;
	}

	internal sbyte GetSByteWithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetSByteCore(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.SByte);
		}
		return value;
	}

	public short GetInt16()
	{
		if (!TryGetInt16(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int16);
		}
		return value;
	}

	internal short GetInt16WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetInt16Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int16);
		}
		return value;
	}

	public int GetInt32()
	{
		if (!TryGetInt32(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int32);
		}
		return value;
	}

	internal int GetInt32WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetInt32Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int32);
		}
		return value;
	}

	public long GetInt64()
	{
		if (!TryGetInt64(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int64);
		}
		return value;
	}

	internal long GetInt64WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetInt64Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.Int64);
		}
		return value;
	}

	[CLSCompliant(false)]
	public ushort GetUInt16()
	{
		if (!TryGetUInt16(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt16);
		}
		return value;
	}

	internal ushort GetUInt16WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetUInt16Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt16);
		}
		return value;
	}

	[CLSCompliant(false)]
	public uint GetUInt32()
	{
		if (!TryGetUInt32(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt32);
		}
		return value;
	}

	internal uint GetUInt32WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetUInt32Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt32);
		}
		return value;
	}

	[CLSCompliant(false)]
	public ulong GetUInt64()
	{
		if (!TryGetUInt64(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt64);
		}
		return value;
	}

	internal ulong GetUInt64WithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetUInt64Core(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.UInt64);
		}
		return value;
	}

	public float GetSingle()
	{
		if (!TryGetSingle(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Single);
		}
		return value;
	}

	internal float GetSingleWithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (JsonReaderHelper.TryGetFloatingPointConstant(unescapedSpan, out float value))
		{
			return value;
		}
		if (Utf8Parser.TryParse(unescapedSpan, out value, out int bytesConsumed, '\0') && unescapedSpan.Length == bytesConsumed && JsonHelpers.IsFinite(value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(NumericType.Single);
	}

	internal float GetSingleFloatingPointConstant()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (JsonReaderHelper.TryGetFloatingPointConstant(unescapedSpan, out float value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(NumericType.Single);
	}

	public double GetDouble()
	{
		if (!TryGetDouble(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Double);
		}
		return value;
	}

	internal double GetDoubleWithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (JsonReaderHelper.TryGetFloatingPointConstant(unescapedSpan, out double value))
		{
			return value;
		}
		if (Utf8Parser.TryParse(unescapedSpan, out value, out int bytesConsumed, '\0') && unescapedSpan.Length == bytesConsumed && JsonHelpers.IsFinite(value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(NumericType.Double);
	}

	internal double GetDoubleFloatingPointConstant()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (JsonReaderHelper.TryGetFloatingPointConstant(unescapedSpan, out double value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(NumericType.Double);
	}

	public decimal GetDecimal()
	{
		if (!TryGetDecimal(out var value))
		{
			throw ThrowHelper.GetFormatException(NumericType.Decimal);
		}
		return value;
	}

	internal decimal GetDecimalWithQuotes()
	{
		ReadOnlySpan<byte> unescapedSpan = GetUnescapedSpan();
		if (!TryGetDecimalCore(out var value, unescapedSpan))
		{
			throw ThrowHelper.GetFormatException(NumericType.Decimal);
		}
		return value;
	}

	public DateTime GetDateTime()
	{
		if (!TryGetDateTime(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.DateTime);
		}
		return value;
	}

	internal DateTime GetDateTimeNoValidation()
	{
		if (!TryGetDateTimeCore(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.DateTime);
		}
		return value;
	}

	public DateTimeOffset GetDateTimeOffset()
	{
		if (!TryGetDateTimeOffset(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.DateTimeOffset);
		}
		return value;
	}

	internal DateTimeOffset GetDateTimeOffsetNoValidation()
	{
		if (!TryGetDateTimeOffsetCore(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.DateTimeOffset);
		}
		return value;
	}

	public Guid GetGuid()
	{
		if (!TryGetGuid(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.Guid);
		}
		return value;
	}

	internal Guid GetGuidNoValidation()
	{
		if (!TryGetGuidCore(out var value))
		{
			throw ThrowHelper.GetFormatException(DataType.Guid);
		}
		return value;
	}

	public bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value)
	{
		if (TokenType != JsonTokenType.String)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> readOnlySpan2 = readOnlySpan;
		if (_stringHasEscaping)
		{
			int idx = readOnlySpan2.IndexOf<byte>(92);
			return JsonReaderHelper.TryGetUnescapedBase64Bytes(readOnlySpan2, idx, out value);
		}
		return JsonReaderHelper.TryDecodeBase64(readOnlySpan2, out value);
	}

	public bool TryGetByte(out byte value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetByteCore(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetByteCore(out byte value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out byte value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0;
		return false;
	}

	[CLSCompliant(false)]
	public bool TryGetSByte(out sbyte value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetSByteCore(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetSByteCore(out sbyte value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out sbyte value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0;
		return false;
	}

	public bool TryGetInt16(out short value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetInt16Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetInt16Core(out short value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out short value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0;
		return false;
	}

	public bool TryGetInt32(out int value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetInt32Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetInt32Core(out int value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out int value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0;
		return false;
	}

	public bool TryGetInt64(out long value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetInt64Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetInt64Core(out long value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out long value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0L;
		return false;
	}

	[CLSCompliant(false)]
	public bool TryGetUInt16(out ushort value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetUInt16Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetUInt16Core(out ushort value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out ushort value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0;
		return false;
	}

	[CLSCompliant(false)]
	public bool TryGetUInt32(out uint value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetUInt32Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetUInt32Core(out uint value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out uint value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0u;
		return false;
	}

	[CLSCompliant(false)]
	public bool TryGetUInt64(out ulong value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetUInt64Core(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetUInt64Core(out ulong value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out ulong value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0uL;
		return false;
	}

	public bool TryGetSingle(out float value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> source = readOnlySpan;
		if (Utf8Parser.TryParse(source, out float value2, out int bytesConsumed, '\0') && source.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0f;
		return false;
	}

	public bool TryGetDouble(out double value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> source = readOnlySpan;
		if (Utf8Parser.TryParse(source, out double value2, out int bytesConsumed, '\0') && source.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = 0.0;
		return false;
	}

	public bool TryGetDecimal(out decimal value)
	{
		if (TokenType != JsonTokenType.Number)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (!HasValueSequence)
		{
			readOnlySpan = ValueSpan;
		}
		else
		{
			ReadOnlySequence<byte> sequence = ValueSequence;
			readOnlySpan = BuffersExtensions.ToArray(in sequence);
		}
		ReadOnlySpan<byte> span = readOnlySpan;
		return TryGetDecimalCore(out value, span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetDecimalCore(out decimal value, ReadOnlySpan<byte> span)
	{
		if (Utf8Parser.TryParse(span, out decimal value2, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			value = value2;
			return true;
		}
		value = default(decimal);
		return false;
	}

	public bool TryGetDateTime(out DateTime value)
	{
		if (TokenType != JsonTokenType.String)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
		}
		return TryGetDateTimeCore(out value);
	}

	internal bool TryGetDateTimeCore(out DateTime value)
	{
		ReadOnlySpan<byte> readOnlySpan = default(Span<byte>);
		int num = (_stringHasEscaping ? 252 : 42);
		if (HasValueSequence)
		{
			long length = ValueSequence.Length;
			if (!JsonHelpers.IsInRangeInclusive(length, 10L, num))
			{
				value = default(DateTime);
				return false;
			}
			Span<byte> destination = stackalloc byte[_stringHasEscaping ? 252 : 42];
			ReadOnlySequence<byte> source = ValueSequence;
			source.CopyTo(destination);
			readOnlySpan = destination.Slice(0, (int)length);
		}
		else
		{
			if (!JsonHelpers.IsInRangeInclusive(ValueSpan.Length, 10, num))
			{
				value = default(DateTime);
				return false;
			}
			readOnlySpan = ValueSpan;
		}
		if (_stringHasEscaping)
		{
			return JsonReaderHelper.TryGetEscapedDateTime(readOnlySpan, out value);
		}
		if (JsonHelpers.TryParseAsISO(readOnlySpan, out DateTime value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTime);
		return false;
	}

	public bool TryGetDateTimeOffset(out DateTimeOffset value)
	{
		if (TokenType != JsonTokenType.String)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
		}
		return TryGetDateTimeOffsetCore(out value);
	}

	internal bool TryGetDateTimeOffsetCore(out DateTimeOffset value)
	{
		ReadOnlySpan<byte> readOnlySpan = default(Span<byte>);
		int num = (_stringHasEscaping ? 252 : 42);
		if (HasValueSequence)
		{
			long length = ValueSequence.Length;
			if (!JsonHelpers.IsInRangeInclusive(length, 10L, num))
			{
				value = default(DateTimeOffset);
				return false;
			}
			Span<byte> destination = stackalloc byte[_stringHasEscaping ? 252 : 42];
			ReadOnlySequence<byte> source = ValueSequence;
			source.CopyTo(destination);
			readOnlySpan = destination.Slice(0, (int)length);
		}
		else
		{
			if (!JsonHelpers.IsInRangeInclusive(ValueSpan.Length, 10, num))
			{
				value = default(DateTimeOffset);
				return false;
			}
			readOnlySpan = ValueSpan;
		}
		if (_stringHasEscaping)
		{
			return JsonReaderHelper.TryGetEscapedDateTimeOffset(readOnlySpan, out value);
		}
		if (JsonHelpers.TryParseAsISO(readOnlySpan, out DateTimeOffset value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTimeOffset);
		return false;
	}

	public bool TryGetGuid(out Guid value)
	{
		if (TokenType != JsonTokenType.String)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
		}
		return TryGetGuidCore(out value);
	}

	internal bool TryGetGuidCore(out Guid value)
	{
		ReadOnlySpan<byte> readOnlySpan = default(Span<byte>);
		int num = (_stringHasEscaping ? 216 : 36);
		if (HasValueSequence)
		{
			long length = ValueSequence.Length;
			if (length > num)
			{
				value = default(Guid);
				return false;
			}
			Span<byte> destination = stackalloc byte[_stringHasEscaping ? 216 : 36];
			ReadOnlySequence<byte> source = ValueSequence;
			source.CopyTo(destination);
			readOnlySpan = destination.Slice(0, (int)length);
		}
		else
		{
			if (ValueSpan.Length > num)
			{
				value = default(Guid);
				return false;
			}
			readOnlySpan = ValueSpan;
		}
		if (_stringHasEscaping)
		{
			return JsonReaderHelper.TryGetEscapedGuid(readOnlySpan, out value);
		}
		if (readOnlySpan.Length == 36 && Utf8Parser.TryParse(readOnlySpan, out Guid value2, out int _, 'D'))
		{
			value = value2;
			return true;
		}
		value = default(Guid);
		return false;
	}
}
