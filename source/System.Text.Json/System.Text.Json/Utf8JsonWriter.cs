using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class Utf8JsonWriter : IDisposable, IAsyncDisposable
{
	private static readonly int s_newLineLength = Environment.NewLine.Length;

	private IBufferWriter<byte> _output;

	private Stream _stream;

	private ArrayBufferWriter<byte> _arrayBufferWriter;

	private Memory<byte> _memory;

	private bool _inObject;

	private JsonTokenType _tokenType;

	private BitStack _bitStack;

	private int _currentDepth;

	private JsonWriterOptions _options;

	private static readonly char[] s_singleLineCommentDelimiter = new char[2] { '*', '/' };

	public int BytesPending { get; private set; }

	public long BytesCommitted { get; private set; }

	public JsonWriterOptions Options => _options;

	private int Indentation => CurrentDepth * 2;

	internal JsonTokenType TokenType => _tokenType;

	public int CurrentDepth => _currentDepth & 0x7FFFFFFF;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"BytesCommitted = {BytesCommitted} BytesPending = {BytesPending} CurrentDepth = {CurrentDepth}";

	private static ReadOnlySpan<byte> SingleLineCommentDelimiterUtf8 => "*/"u8;

	public Utf8JsonWriter(IBufferWriter<byte> bufferWriter, JsonWriterOptions options = default(JsonWriterOptions))
	{
		_output = bufferWriter ?? throw new ArgumentNullException("bufferWriter");
		_options = options;
	}

	public Utf8JsonWriter(Stream utf8Json, JsonWriterOptions options = default(JsonWriterOptions))
	{
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (!utf8Json.CanWrite)
		{
			throw new ArgumentException(System.SR.StreamNotWritable);
		}
		_stream = utf8Json;
		_options = options;
		_arrayBufferWriter = new ArrayBufferWriter<byte>();
	}

	public void Reset()
	{
		CheckNotDisposed();
		_arrayBufferWriter?.Clear();
		ResetHelper();
	}

	public void Reset(Stream utf8Json)
	{
		CheckNotDisposed();
		if (utf8Json == null)
		{
			throw new ArgumentNullException("utf8Json");
		}
		if (!utf8Json.CanWrite)
		{
			throw new ArgumentException(System.SR.StreamNotWritable);
		}
		_stream = utf8Json;
		if (_arrayBufferWriter == null)
		{
			_arrayBufferWriter = new ArrayBufferWriter<byte>();
		}
		else
		{
			_arrayBufferWriter.Clear();
		}
		_output = null;
		ResetHelper();
	}

	public void Reset(IBufferWriter<byte> bufferWriter)
	{
		CheckNotDisposed();
		_output = bufferWriter ?? throw new ArgumentNullException("bufferWriter");
		_stream = null;
		_arrayBufferWriter = null;
		ResetHelper();
	}

	private void ResetHelper()
	{
		BytesPending = 0;
		BytesCommitted = 0L;
		_memory = default(Memory<byte>);
		_inObject = false;
		_tokenType = JsonTokenType.None;
		_currentDepth = 0;
		_bitStack = default(BitStack);
	}

	private void CheckNotDisposed()
	{
		if (_stream == null && _output == null)
		{
			throw new ObjectDisposedException("Utf8JsonWriter");
		}
	}

	public void Flush()
	{
		CheckNotDisposed();
		_memory = default(Memory<byte>);
		if (_stream != null)
		{
			if (BytesPending != 0)
			{
				_arrayBufferWriter.Advance(BytesPending);
				BytesPending = 0;
				_stream.Write(_arrayBufferWriter.WrittenSpan);
				BytesCommitted += _arrayBufferWriter.WrittenCount;
				_arrayBufferWriter.Clear();
			}
			_stream.Flush();
		}
		else if (BytesPending != 0)
		{
			_output.Advance(BytesPending);
			BytesCommitted += BytesPending;
			BytesPending = 0;
		}
	}

	public void Dispose()
	{
		if (_stream != null || _output != null)
		{
			Flush();
			ResetHelper();
			_stream = null;
			_arrayBufferWriter = null;
			_output = null;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_stream != null || _output != null)
		{
			await FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
			ResetHelper();
			_stream = null;
			_arrayBufferWriter = null;
			_output = null;
		}
	}

	public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckNotDisposed();
		_memory = default(Memory<byte>);
		if (_stream != null)
		{
			if (BytesPending != 0)
			{
				_arrayBufferWriter.Advance(BytesPending);
				BytesPending = 0;
				await _stream.WriteAsync(_arrayBufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				BytesCommitted += _arrayBufferWriter.WrittenCount;
				_arrayBufferWriter.Clear();
			}
			await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (BytesPending != 0)
		{
			_output.Advance(BytesPending);
			BytesCommitted += BytesPending;
			BytesPending = 0;
		}
	}

	public void WriteStartArray()
	{
		WriteStart(91);
		_tokenType = JsonTokenType.StartArray;
	}

	public void WriteStartObject()
	{
		WriteStart(123);
		_tokenType = JsonTokenType.StartObject;
	}

	private void WriteStart(byte token)
	{
		if (CurrentDepth >= 1000)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.DepthTooLarge, _currentDepth, 0, JsonTokenType.None);
		}
		if (_options.IndentedOrNotSkipValidation)
		{
			WriteStartSlow(token);
		}
		else
		{
			WriteStartMinimized(token);
		}
		_currentDepth &= int.MaxValue;
		_currentDepth++;
	}

	private void WriteStartMinimized(byte token)
	{
		if (_memory.Length - BytesPending < 2)
		{
			Grow(2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = token;
	}

	private void WriteStartSlow(byte token)
	{
		if (_options.Indented)
		{
			if (!_options.SkipValidation)
			{
				ValidateStart();
				UpdateBitStackOnStart(token);
			}
			WriteStartIndented(token);
		}
		else
		{
			ValidateStart();
			UpdateBitStackOnStart(token);
			WriteStartMinimized(token);
		}
	}

	private void ValidateStart()
	{
		if (_inObject)
		{
			if (_tokenType != JsonTokenType.PropertyName)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayWithoutProperty, 0, 0, _tokenType);
			}
		}
		else if (CurrentDepth == 0 && _tokenType != 0)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose, 0, 0, _tokenType);
		}
	}

	private void WriteStartIndented(byte token)
	{
		int indentation = Indentation;
		int num = indentation + 1;
		int num2 = num + 3;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = token;
	}

	public void WriteStartArray(JsonEncodedText propertyName)
	{
		WriteStartHelper(propertyName.EncodedUtf8Bytes, 91);
		_tokenType = JsonTokenType.StartArray;
	}

	public void WriteStartObject(JsonEncodedText propertyName)
	{
		WriteStartHelper(propertyName.EncodedUtf8Bytes, 123);
		_tokenType = JsonTokenType.StartObject;
	}

	private void WriteStartHelper(ReadOnlySpan<byte> utf8PropertyName, byte token)
	{
		ValidateDepth();
		WriteStartByOptions(utf8PropertyName, token);
		_currentDepth &= int.MaxValue;
		_currentDepth++;
	}

	public void WriteStartArray(ReadOnlySpan<byte> utf8PropertyName)
	{
		ValidatePropertyNameAndDepth(utf8PropertyName);
		WriteStartEscape(utf8PropertyName, 91);
		_currentDepth &= int.MaxValue;
		_currentDepth++;
		_tokenType = JsonTokenType.StartArray;
	}

	public void WriteStartObject(ReadOnlySpan<byte> utf8PropertyName)
	{
		ValidatePropertyNameAndDepth(utf8PropertyName);
		WriteStartEscape(utf8PropertyName, 123);
		_currentDepth &= int.MaxValue;
		_currentDepth++;
		_tokenType = JsonTokenType.StartObject;
	}

	private void WriteStartEscape(ReadOnlySpan<byte> utf8PropertyName, byte token)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStartEscapeProperty(utf8PropertyName, token, num);
		}
		else
		{
			WriteStartByOptions(utf8PropertyName, token);
		}
	}

	private void WriteStartByOptions(ReadOnlySpan<byte> utf8PropertyName, byte token)
	{
		ValidateWritingProperty(token);
		if (_options.Indented)
		{
			WritePropertyNameIndented(utf8PropertyName, token);
		}
		else
		{
			WritePropertyNameMinimized(utf8PropertyName, token);
		}
	}

	private void WriteStartEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, byte token, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStartByOptions(destination.Slice(0, written), token);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public void WriteStartArray(string propertyName)
	{
		WriteStartArray((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan());
	}

	public void WriteStartObject(string propertyName)
	{
		WriteStartObject((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan());
	}

	public void WriteStartArray(ReadOnlySpan<char> propertyName)
	{
		ValidatePropertyNameAndDepth(propertyName);
		WriteStartEscape(propertyName, 91);
		_currentDepth &= int.MaxValue;
		_currentDepth++;
		_tokenType = JsonTokenType.StartArray;
	}

	public void WriteStartObject(ReadOnlySpan<char> propertyName)
	{
		ValidatePropertyNameAndDepth(propertyName);
		WriteStartEscape(propertyName, 123);
		_currentDepth &= int.MaxValue;
		_currentDepth++;
		_tokenType = JsonTokenType.StartObject;
	}

	private void WriteStartEscape(ReadOnlySpan<char> propertyName, byte token)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStartEscapeProperty(propertyName, token, num);
		}
		else
		{
			WriteStartByOptions(propertyName, token);
		}
	}

	private void WriteStartByOptions(ReadOnlySpan<char> propertyName, byte token)
	{
		ValidateWritingProperty(token);
		if (_options.Indented)
		{
			WritePropertyNameIndented(propertyName, token);
		}
		else
		{
			WritePropertyNameMinimized(propertyName, token);
		}
	}

	private void WriteStartEscapeProperty(ReadOnlySpan<char> propertyName, byte token, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStartByOptions(destination.Slice(0, written), token);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	public void WriteEndArray()
	{
		WriteEnd(93);
		_tokenType = JsonTokenType.EndArray;
	}

	public void WriteEndObject()
	{
		WriteEnd(125);
		_tokenType = JsonTokenType.EndObject;
	}

	private void WriteEnd(byte token)
	{
		if (_options.IndentedOrNotSkipValidation)
		{
			WriteEndSlow(token);
		}
		else
		{
			WriteEndMinimized(token);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		if (CurrentDepth != 0)
		{
			_currentDepth--;
		}
	}

	private void WriteEndMinimized(byte token)
	{
		if (_memory.Length - BytesPending < 1)
		{
			Grow(1);
		}
		_memory.Span[BytesPending++] = token;
	}

	private void WriteEndSlow(byte token)
	{
		if (_options.Indented)
		{
			if (!_options.SkipValidation)
			{
				ValidateEnd(token);
			}
			WriteEndIndented(token);
		}
		else
		{
			ValidateEnd(token);
			WriteEndMinimized(token);
		}
	}

	private void ValidateEnd(byte token)
	{
		if (_bitStack.CurrentDepth <= 0 || _tokenType == JsonTokenType.PropertyName)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, 0, token, _tokenType);
		}
		if (token == 93)
		{
			if (_inObject)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, 0, token, _tokenType);
			}
		}
		else if (!_inObject)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, 0, token, _tokenType);
		}
		_inObject = _bitStack.Pop();
	}

	private void WriteEndIndented(byte token)
	{
		if (_tokenType == JsonTokenType.StartObject || _tokenType == JsonTokenType.StartArray)
		{
			WriteEndMinimized(token);
			return;
		}
		int num = Indentation;
		if (num != 0)
		{
			num -= 2;
		}
		int num2 = num + 3;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		WriteNewLine(span);
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), num);
		BytesPending += num;
		span[BytesPending++] = token;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteNewLine(Span<byte> output)
	{
		if (s_newLineLength == 2)
		{
			output[BytesPending++] = 13;
		}
		output[BytesPending++] = 10;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateBitStackOnStart(byte token)
	{
		if (token == 91)
		{
			_bitStack.PushFalse();
			_inObject = false;
		}
		else
		{
			_bitStack.PushTrue();
			_inObject = true;
		}
	}

	private void Grow(int requiredSize)
	{
		if (_memory.Length == 0)
		{
			FirstCallToGetMemory(requiredSize);
			return;
		}
		int num = Math.Max(4096, requiredSize);
		if (_stream != null)
		{
			int num2 = BytesPending + num;
			JsonHelpers.ValidateInt32MaxArrayLength((uint)num2);
			_memory = _arrayBufferWriter.GetMemory(num2);
			return;
		}
		_output.Advance(BytesPending);
		BytesCommitted += BytesPending;
		BytesPending = 0;
		_memory = _output.GetMemory(num);
		if (_memory.Length < num)
		{
			ThrowHelper.ThrowInvalidOperationException_NeedLargerSpan();
		}
	}

	private void FirstCallToGetMemory(int requiredSize)
	{
		int num = Math.Max(256, requiredSize);
		if (_stream != null)
		{
			_memory = _arrayBufferWriter.GetMemory(num);
			return;
		}
		_memory = _output.GetMemory(num);
		if (_memory.Length < num)
		{
			ThrowHelper.ThrowInvalidOperationException_NeedLargerSpan();
		}
	}

	private void SetFlagToAddListSeparatorBeforeNextItem()
	{
		_currentDepth |= int.MinValue;
	}

	public void WriteBase64String(JsonEncodedText propertyName, ReadOnlySpan<byte> bytes)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		JsonWriterHelper.ValidateBytes(bytes);
		WriteBase64ByOptions(encodedUtf8Bytes, bytes);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteBase64String(string propertyName, ReadOnlySpan<byte> bytes)
	{
		WriteBase64String((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), bytes);
	}

	public void WriteBase64String(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
	{
		JsonWriterHelper.ValidatePropertyAndBytes(propertyName, bytes);
		WriteBase64Escape(propertyName, bytes);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteBase64String(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
	{
		JsonWriterHelper.ValidatePropertyAndBytes(utf8PropertyName, bytes);
		WriteBase64Escape(utf8PropertyName, bytes);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteBase64Escape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteBase64EscapeProperty(propertyName, bytes, num);
		}
		else
		{
			WriteBase64ByOptions(propertyName, bytes);
		}
	}

	private void WriteBase64Escape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteBase64EscapeProperty(utf8PropertyName, bytes, num);
		}
		else
		{
			WriteBase64ByOptions(utf8PropertyName, bytes);
		}
	}

	private void WriteBase64EscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteBase64ByOptions(destination.Slice(0, written), bytes);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteBase64EscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteBase64ByOptions(destination.Slice(0, written), bytes);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteBase64ByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteBase64Indented(propertyName, bytes);
		}
		else
		{
			WriteBase64Minimized(propertyName, bytes);
		}
	}

	private void WriteBase64ByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteBase64Indented(utf8PropertyName, bytes);
		}
		else
		{
			WriteBase64Minimized(utf8PropertyName, bytes);
		}
	}

	private void WriteBase64Minimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> bytes)
	{
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = escapedPropertyName.Length * 3 + maxEncodedToUtf8Length + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	private void WriteBase64Minimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> bytes)
	{
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = escapedPropertyName.Length + maxEncodedToUtf8Length + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	private void WriteBase64Indented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> bytes)
	{
		int indentation = Indentation;
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = indentation + escapedPropertyName.Length * 3 + maxEncodedToUtf8Length + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	private void WriteBase64Indented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> bytes)
	{
		int indentation = Indentation;
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = indentation + escapedPropertyName.Length + maxEncodedToUtf8Length + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	public void WriteString(JsonEncodedText propertyName, DateTime value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteStringByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, DateTime value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteString(ReadOnlySpan<char> propertyName, DateTime value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteStringEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, DateTime value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteStringEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringEscape(ReadOnlySpan<char> propertyName, DateTime value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteStringByOptions(propertyName, value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, DateTime value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, value);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, DateTime value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, DateTime value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> propertyName, DateTime value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(propertyName, value);
		}
		else
		{
			WriteStringMinimized(propertyName, value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, DateTime value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(utf8PropertyName, value);
		}
		else
		{
			WriteStringMinimized(utf8PropertyName, value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, DateTime value)
	{
		int num = escapedPropertyName.Length * 3 + 33 + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, DateTime value)
	{
		int num = escapedPropertyName.Length + 33 + 5;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, DateTime value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 33 + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, DateTime value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 33 + 6;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	internal void WritePropertyName(DateTime value)
	{
		Span<byte> buffer = stackalloc byte[33];
		JsonWriterHelper.WriteDateTimeTrimmed(buffer, value, out var bytesWritten);
		WritePropertyNameUnescaped(buffer.Slice(0, bytesWritten));
	}

	public void WriteString(JsonEncodedText propertyName, DateTimeOffset value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteStringByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, DateTimeOffset value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteString(ReadOnlySpan<char> propertyName, DateTimeOffset value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteStringEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteStringEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringEscape(ReadOnlySpan<char> propertyName, DateTimeOffset value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteStringByOptions(propertyName, value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, value);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, DateTimeOffset value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> propertyName, DateTimeOffset value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(propertyName, value);
		}
		else
		{
			WriteStringMinimized(propertyName, value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(utf8PropertyName, value);
		}
		else
		{
			WriteStringMinimized(utf8PropertyName, value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, DateTimeOffset value)
	{
		int num = escapedPropertyName.Length * 3 + 33 + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, DateTimeOffset value)
	{
		int num = escapedPropertyName.Length + 33 + 5;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, DateTimeOffset value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 33 + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, DateTimeOffset value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 33 + 6;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	internal void WritePropertyName(DateTimeOffset value)
	{
		Span<byte> buffer = stackalloc byte[33];
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(buffer, value, out var bytesWritten);
		WritePropertyNameUnescaped(buffer.Slice(0, bytesWritten));
	}

	public void WriteNumber(JsonEncodedText propertyName, decimal value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteNumberByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(string propertyName, decimal value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteNumber(ReadOnlySpan<char> propertyName, decimal value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteNumberEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, decimal value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteNumberEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberEscape(ReadOnlySpan<char> propertyName, decimal value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(propertyName, value);
		}
	}

	private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, decimal value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(utf8PropertyName, value);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, decimal value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, decimal value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, decimal value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(propertyName, value);
		}
		else
		{
			WriteNumberMinimized(propertyName, value);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, decimal value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(utf8PropertyName, value);
		}
		else
		{
			WriteNumberMinimized(utf8PropertyName, value);
		}
	}

	private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, decimal value)
	{
		int num = escapedPropertyName.Length * 3 + 31 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, decimal value)
	{
		int num = escapedPropertyName.Length + 31 + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, decimal value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 31 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, decimal value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 31 + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WritePropertyName(decimal value)
	{
		Span<byte> destination = stackalloc byte[31];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteNumber(JsonEncodedText propertyName, double value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		JsonWriterHelper.ValidateDouble(value);
		WriteNumberByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(string propertyName, double value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteNumber(ReadOnlySpan<char> propertyName, double value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		JsonWriterHelper.ValidateDouble(value);
		WriteNumberEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, double value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		JsonWriterHelper.ValidateDouble(value);
		WriteNumberEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberEscape(ReadOnlySpan<char> propertyName, double value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(propertyName, value);
		}
	}

	private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, double value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(utf8PropertyName, value);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, double value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, double value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, double value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(propertyName, value);
		}
		else
		{
			WriteNumberMinimized(propertyName, value);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, double value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(utf8PropertyName, value);
		}
		else
		{
			WriteNumberMinimized(utf8PropertyName, value);
		}
	}

	private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, double value)
	{
		int num = escapedPropertyName.Length * 3 + 128 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, double value)
	{
		int num = escapedPropertyName.Length + 128 + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, double value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 128 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, double value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 128 + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WritePropertyName(double value)
	{
		JsonWriterHelper.ValidateDouble(value);
		Span<byte> destination = stackalloc byte[128];
		int bytesWritten;
		bool flag = TryFormatDouble(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteNumber(JsonEncodedText propertyName, float value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		JsonWriterHelper.ValidateSingle(value);
		WriteNumberByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(string propertyName, float value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteNumber(ReadOnlySpan<char> propertyName, float value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		JsonWriterHelper.ValidateSingle(value);
		WriteNumberEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, float value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		JsonWriterHelper.ValidateSingle(value);
		WriteNumberEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberEscape(ReadOnlySpan<char> propertyName, float value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(propertyName, value);
		}
	}

	private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, float value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(utf8PropertyName, value);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, float value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, float value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, float value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(propertyName, value);
		}
		else
		{
			WriteNumberMinimized(propertyName, value);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, float value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(utf8PropertyName, value);
		}
		else
		{
			WriteNumberMinimized(utf8PropertyName, value);
		}
	}

	private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, float value)
	{
		int num = escapedPropertyName.Length * 3 + 128 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, float value)
	{
		int num = escapedPropertyName.Length + 128 + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, float value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 128 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, float value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 128 + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WritePropertyName(float value)
	{
		Span<byte> destination = stackalloc byte[128];
		int bytesWritten;
		bool flag = TryFormatSingle(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteString(JsonEncodedText propertyName, Guid value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteStringByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, Guid value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteString(ReadOnlySpan<char> propertyName, Guid value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteStringEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, Guid value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteStringEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringEscape(ReadOnlySpan<char> propertyName, Guid value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteStringByOptions(propertyName, value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, Guid value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, value);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, Guid value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, Guid value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> propertyName, Guid value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(propertyName, value);
		}
		else
		{
			WriteStringMinimized(propertyName, value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, Guid value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(utf8PropertyName, value);
		}
		else
		{
			WriteStringMinimized(utf8PropertyName, value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, Guid value)
	{
		int num = escapedPropertyName.Length * 3 + 36 + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, Guid value)
	{
		int num = escapedPropertyName.Length + 36 + 5;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, Guid value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 36 + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, Guid value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 36 + 6;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	internal void WritePropertyName(Guid value)
	{
		Span<byte> destination = stackalloc byte[36];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidatePropertyNameAndDepth(ReadOnlySpan<char> propertyName)
	{
		if (propertyName.Length > 166666666 || CurrentDepth >= 1000)
		{
			ThrowHelper.ThrowInvalidOperationOrArgumentException(propertyName, _currentDepth);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidatePropertyNameAndDepth(ReadOnlySpan<byte> utf8PropertyName)
	{
		if (utf8PropertyName.Length > 166666666 || CurrentDepth >= 1000)
		{
			ThrowHelper.ThrowInvalidOperationOrArgumentException(utf8PropertyName, _currentDepth);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidateDepth()
	{
		if (CurrentDepth >= 1000)
		{
			ThrowHelper.ThrowInvalidOperationException(_currentDepth);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidateWritingProperty()
	{
		if (!_options.SkipValidation && (!_inObject || _tokenType == JsonTokenType.PropertyName))
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWritePropertyWithinArray, 0, 0, _tokenType);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ValidateWritingProperty(byte token)
	{
		if (!_options.SkipValidation)
		{
			if (!_inObject || _tokenType == JsonTokenType.PropertyName)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWritePropertyWithinArray, 0, 0, _tokenType);
			}
			UpdateBitStackOnStart(token);
		}
	}

	private void WritePropertyNameMinimized(ReadOnlySpan<byte> escapedPropertyName, byte token)
	{
		int num = escapedPropertyName.Length + 4;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = token;
	}

	private void WritePropertyNameIndented(ReadOnlySpan<byte> escapedPropertyName, byte token)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 5;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = token;
	}

	private void WritePropertyNameMinimized(ReadOnlySpan<char> escapedPropertyName, byte token)
	{
		int num = escapedPropertyName.Length * 3 + 5;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = token;
	}

	private void WritePropertyNameIndented(ReadOnlySpan<char> escapedPropertyName, byte token)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 6 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = token;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void TranscodeAndWrite(ReadOnlySpan<char> escapedPropertyName, Span<byte> output)
	{
		ReadOnlySpan<byte> utf16Source = MemoryMarshal.AsBytes(escapedPropertyName);
		int bytesConsumed;
		int bytesWritten;
		OperationStatus operationStatus = JsonWriterHelper.ToUtf8(utf16Source, output.Slice(BytesPending), out bytesConsumed, out bytesWritten);
		BytesPending += bytesWritten;
	}

	public void WriteNull(JsonEncodedText propertyName)
	{
		WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.NullValue);
		_tokenType = JsonTokenType.Null;
	}

	internal void WriteNullSection(ReadOnlySpan<byte> escapedPropertyNameSection)
	{
		if (_options.Indented)
		{
			ReadOnlySpan<byte> utf8PropertyName = escapedPropertyNameSection.Slice(1, escapedPropertyNameSection.Length - 3);
			WriteLiteralHelper(utf8PropertyName, JsonConstants.NullValue);
			_tokenType = JsonTokenType.Null;
		}
		else
		{
			ReadOnlySpan<byte> nullValue = JsonConstants.NullValue;
			WriteLiteralSection(escapedPropertyNameSection, nullValue);
			SetFlagToAddListSeparatorBeforeNextItem();
			_tokenType = JsonTokenType.Null;
		}
	}

	private void WriteLiteralHelper(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
	{
		WriteLiteralByOptions(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
	}

	public void WriteNull(string propertyName)
	{
		WriteNull((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan());
	}

	public void WriteNull(ReadOnlySpan<char> propertyName)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		ReadOnlySpan<byte> nullValue = JsonConstants.NullValue;
		WriteLiteralEscape(propertyName, nullValue);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Null;
	}

	public void WriteNull(ReadOnlySpan<byte> utf8PropertyName)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		ReadOnlySpan<byte> nullValue = JsonConstants.NullValue;
		WriteLiteralEscape(utf8PropertyName, nullValue);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Null;
	}

	public void WriteBoolean(JsonEncodedText propertyName, bool value)
	{
		if (value)
		{
			WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.TrueValue);
			_tokenType = JsonTokenType.True;
		}
		else
		{
			WriteLiteralHelper(propertyName.EncodedUtf8Bytes, JsonConstants.FalseValue);
			_tokenType = JsonTokenType.False;
		}
	}

	public void WriteBoolean(string propertyName, bool value)
	{
		WriteBoolean((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteBoolean(ReadOnlySpan<char> propertyName, bool value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		ReadOnlySpan<byte> value2 = (value ? JsonConstants.TrueValue : JsonConstants.FalseValue);
		WriteLiteralEscape(propertyName, value2);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = (value ? JsonTokenType.True : JsonTokenType.False);
	}

	public void WriteBoolean(ReadOnlySpan<byte> utf8PropertyName, bool value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		ReadOnlySpan<byte> value2 = (value ? JsonConstants.TrueValue : JsonConstants.FalseValue);
		WriteLiteralEscape(utf8PropertyName, value2);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = (value ? JsonTokenType.True : JsonTokenType.False);
	}

	private void WriteLiteralEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteLiteralEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteLiteralByOptions(propertyName, value);
		}
	}

	private void WriteLiteralEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteLiteralEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteLiteralByOptions(utf8PropertyName, value);
		}
	}

	private void WriteLiteralEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteLiteralByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteLiteralEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteLiteralByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteLiteralByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteLiteralIndented(propertyName, value);
		}
		else
		{
			WriteLiteralMinimized(propertyName, value);
		}
	}

	private void WriteLiteralByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteLiteralIndented(utf8PropertyName, value);
		}
		else
		{
			WriteLiteralMinimized(utf8PropertyName, value);
		}
	}

	private void WriteLiteralMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
	{
		int num = escapedPropertyName.Length * 3 + value.Length + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		value.CopyTo(span.Slice(BytesPending));
		BytesPending += value.Length;
	}

	private void WriteLiteralMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
	{
		int num = escapedPropertyName.Length + value.Length + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		value.CopyTo(span.Slice(BytesPending));
		BytesPending += value.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteLiteralSection(ReadOnlySpan<byte> escapedPropertyNameSection, ReadOnlySpan<byte> value)
	{
		int num = escapedPropertyNameSection.Length + value.Length;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		escapedPropertyNameSection.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyNameSection.Length;
		value.CopyTo(span.Slice(BytesPending));
		BytesPending += value.Length;
	}

	private void WriteLiteralIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + value.Length + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		value.CopyTo(span.Slice(BytesPending));
		BytesPending += value.Length;
	}

	private void WriteLiteralIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + value.Length + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		value.CopyTo(span.Slice(BytesPending));
		BytesPending += value.Length;
	}

	internal void WritePropertyName(bool value)
	{
		Span<byte> destination = stackalloc byte[5];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteNumber(JsonEncodedText propertyName, long value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteNumberByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(string propertyName, long value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteNumber(ReadOnlySpan<char> propertyName, long value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteNumberEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, long value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteNumberEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	public void WriteNumber(JsonEncodedText propertyName, int value)
	{
		WriteNumber(propertyName, (long)value);
	}

	public void WriteNumber(string propertyName, int value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), (long)value);
	}

	public void WriteNumber(ReadOnlySpan<char> propertyName, int value)
	{
		WriteNumber(propertyName, (long)value);
	}

	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, int value)
	{
		WriteNumber(utf8PropertyName, (long)value);
	}

	private void WriteNumberEscape(ReadOnlySpan<char> propertyName, long value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(propertyName, value);
		}
	}

	private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, long value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(utf8PropertyName, value);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, long value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, long value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, long value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(propertyName, value);
		}
		else
		{
			WriteNumberMinimized(propertyName, value);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, long value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(utf8PropertyName, value);
		}
		else
		{
			WriteNumberMinimized(utf8PropertyName, value);
		}
	}

	private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, long value)
	{
		int num = escapedPropertyName.Length * 3 + 20 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, long value)
	{
		int num = escapedPropertyName.Length + 20 + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, long value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 20 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, long value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 20 + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WritePropertyName(int value)
	{
		WritePropertyName((long)value);
	}

	internal void WritePropertyName(long value)
	{
		Span<byte> destination = stackalloc byte[20];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WritePropertyName(JsonEncodedText propertyName)
	{
		WritePropertyNameHelper(propertyName.EncodedUtf8Bytes);
	}

	internal void WritePropertyNameSection(ReadOnlySpan<byte> escapedPropertyNameSection)
	{
		if (_options.Indented)
		{
			ReadOnlySpan<byte> utf8PropertyName = escapedPropertyNameSection.Slice(1, escapedPropertyNameSection.Length - 3);
			WritePropertyNameHelper(utf8PropertyName);
		}
		else
		{
			WriteStringPropertyNameSection(escapedPropertyNameSection);
			_currentDepth &= int.MaxValue;
			_tokenType = JsonTokenType.PropertyName;
		}
	}

	private void WritePropertyNameHelper(ReadOnlySpan<byte> utf8PropertyName)
	{
		WriteStringByOptionsPropertyName(utf8PropertyName);
		_currentDepth &= int.MaxValue;
		_tokenType = JsonTokenType.PropertyName;
	}

	public void WritePropertyName(string propertyName)
	{
		WritePropertyName((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan());
	}

	public void WritePropertyName(ReadOnlySpan<char> propertyName)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(propertyName, num);
		}
		else
		{
			WriteStringByOptionsPropertyName(propertyName);
		}
		_currentDepth &= int.MaxValue;
		_tokenType = JsonTokenType.PropertyName;
	}

	private unsafe void WriteStringEscapeProperty(ReadOnlySpan<char> propertyName, int firstEscapeIndexProp)
	{
		char[] array = null;
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
			Span<char> destination;
			if (maxEscapedLength > 128)
			{
				array = ArrayPool<char>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				char* pointer = stackalloc char[128];
				destination = new Span<char>(pointer, 128);
			}
			JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
			propertyName = destination.Slice(0, written);
		}
		WriteStringByOptionsPropertyName(propertyName);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringByOptionsPropertyName(ReadOnlySpan<char> propertyName)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndentedPropertyName(propertyName);
		}
		else
		{
			WriteStringMinimizedPropertyName(propertyName);
		}
	}

	private void WriteStringMinimizedPropertyName(ReadOnlySpan<char> escapedPropertyName)
	{
		int num = escapedPropertyName.Length * 3 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
	}

	private void WriteStringIndentedPropertyName(ReadOnlySpan<char> escapedPropertyName)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
	}

	public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeProperty(utf8PropertyName, num);
		}
		else
		{
			WriteStringByOptionsPropertyName(utf8PropertyName);
		}
		_currentDepth &= int.MaxValue;
		_tokenType = JsonTokenType.PropertyName;
	}

	private void WritePropertyNameUnescaped(ReadOnlySpan<byte> utf8PropertyName)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteStringByOptionsPropertyName(utf8PropertyName);
		_currentDepth &= int.MaxValue;
		_tokenType = JsonTokenType.PropertyName;
	}

	private unsafe void WriteStringEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, int firstEscapeIndexProp)
	{
		byte[] array = null;
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
			Span<byte> destination;
			if (maxEscapedLength > 256)
			{
				array = ArrayPool<byte>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				byte* pointer = stackalloc byte[256];
				destination = new Span<byte>(pointer, 256);
			}
			JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
			utf8PropertyName = destination.Slice(0, written);
		}
		WriteStringByOptionsPropertyName(utf8PropertyName);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringByOptionsPropertyName(ReadOnlySpan<byte> utf8PropertyName)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndentedPropertyName(utf8PropertyName);
		}
		else
		{
			WriteStringMinimizedPropertyName(utf8PropertyName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteStringMinimizedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
	{
		int num = escapedPropertyName.Length + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteStringPropertyNameSection(ReadOnlySpan<byte> escapedPropertyNameSection)
	{
		int num = escapedPropertyNameSection.Length + 1;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		escapedPropertyNameSection.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyNameSection.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteStringIndentedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
	}

	public void WriteString(JsonEncodedText propertyName, JsonEncodedText value)
	{
		WriteStringHelper(propertyName.EncodedUtf8Bytes, value.EncodedUtf8Bytes);
	}

	private void WriteStringHelper(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		WriteStringByOptions(utf8PropertyName, utf8Value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, JsonEncodedText value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteString(string propertyName, string? value)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		if (value == null)
		{
			WriteNull(propertyName.AsSpan());
		}
		else
		{
			WriteString(propertyName.AsSpan(), value.AsSpan());
		}
	}

	public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		JsonWriterHelper.ValidatePropertyAndValue(propertyName, value);
		WriteStringEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, utf8Value);
		WriteStringEscape(utf8PropertyName, utf8Value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(JsonEncodedText propertyName, string? value)
	{
		if (value == null)
		{
			WriteNull(propertyName);
		}
		else
		{
			WriteString(propertyName, value.AsSpan());
		}
	}

	public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<char> value)
	{
		WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, value);
	}

	private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
	{
		JsonWriterHelper.ValidateValue(value);
		int num = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeValueOnly(utf8PropertyName, value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, ReadOnlySpan<char> value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
	{
		JsonWriterHelper.ValidatePropertyAndValue(utf8PropertyName, value);
		WriteStringEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<byte> utf8Value)
	{
		WriteStringHelperEscapeValue(propertyName.EncodedUtf8Bytes, utf8Value);
	}

	private void WriteStringHelperEscapeValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidateValue(utf8Value);
		int num = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeValueOnly(utf8PropertyName, utf8Value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, utf8Value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(string propertyName, ReadOnlySpan<byte> utf8Value)
	{
		WriteString((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), utf8Value);
	}

	public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidatePropertyAndValue(propertyName, utf8Value);
		WriteStringEscape(propertyName, utf8Value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<char> propertyName, JsonEncodedText value)
	{
		WriteStringHelperEscapeProperty(propertyName, value.EncodedUtf8Bytes);
	}

	private void WriteStringHelperEscapeProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapePropertyOnly(propertyName, utf8Value, num);
		}
		else
		{
			WriteStringByOptions(propertyName, utf8Value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<char> propertyName, string? value)
	{
		if (value == null)
		{
			WriteNull(propertyName);
		}
		else
		{
			WriteString(propertyName, value.AsSpan());
		}
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, JsonEncodedText value)
	{
		WriteStringHelperEscapeProperty(utf8PropertyName, value.EncodedUtf8Bytes);
	}

	private void WriteStringHelperEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapePropertyOnly(utf8PropertyName, utf8Value, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, utf8Value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteString(ReadOnlySpan<byte> utf8PropertyName, string? value)
	{
		if (value == null)
		{
			WriteNull(utf8PropertyName);
		}
		else
		{
			WriteString(utf8PropertyName, value.AsSpan());
		}
	}

	private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndex)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndex);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndex, _options.Encoder, out var written);
		WriteStringByOptions(escapedPropertyName, destination.Slice(0, written));
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringEscapeValueOnly(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> value, int firstEscapeIndex)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndex);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(value, destination, firstEscapeIndex, _options.Encoder, out var written);
		WriteStringByOptions(escapedPropertyName, destination.Slice(0, written));
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringEscapePropertyOnly(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> escapedValue, int firstEscapeIndex)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndex);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndex, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), escapedValue);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteStringEscapePropertyOnly(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> escapedValue, int firstEscapeIndex)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndex);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndex, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written), escapedValue);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		int num = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
		int num2 = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num + num2 != -2)
		{
			WriteStringEscapePropertyOrValue(propertyName, value, num2, num);
		}
		else
		{
			WriteStringByOptions(propertyName, value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
		int num2 = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num + num2 != -2)
		{
			WriteStringEscapePropertyOrValue(utf8PropertyName, utf8Value, num2, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, utf8Value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
		int num2 = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num + num2 != -2)
		{
			WriteStringEscapePropertyOrValue(propertyName, utf8Value, num2, num);
		}
		else
		{
			WriteStringByOptions(propertyName, utf8Value);
		}
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
	{
		int num = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
		int num2 = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num + num2 != -2)
		{
			WriteStringEscapePropertyOrValue(utf8PropertyName, value, num2, num);
		}
		else
		{
			WriteStringByOptions(utf8PropertyName, value);
		}
	}

	private unsafe void WriteStringEscapePropertyOrValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
	{
		char[] array = null;
		char[] array2 = null;
		if (firstEscapeIndexVal != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);
			Span<char> destination;
			if (maxEscapedLength > 128)
			{
				array = ArrayPool<char>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				char* pointer = stackalloc char[128];
				destination = new Span<char>(pointer, 128);
			}
			JsonWriterHelper.EscapeString(value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
			value = destination.Slice(0, written);
		}
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength2 = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
			Span<char> destination2;
			if (maxEscapedLength2 > 128)
			{
				array2 = ArrayPool<char>.Shared.Rent(maxEscapedLength2);
				destination2 = array2;
			}
			else
			{
				char* pointer2 = stackalloc char[128];
				destination2 = new Span<char>(pointer2, 128);
			}
			JsonWriterHelper.EscapeString(propertyName, destination2, firstEscapeIndexProp, _options.Encoder, out var written2);
			propertyName = destination2.Slice(0, written2);
		}
		WriteStringByOptions(propertyName, value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<char>.Shared.Return(array2);
		}
	}

	private unsafe void WriteStringEscapePropertyOrValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
	{
		byte[] array = null;
		byte[] array2 = null;
		if (firstEscapeIndexVal != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
			Span<byte> destination;
			if (maxEscapedLength > 256)
			{
				array = ArrayPool<byte>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				byte* pointer = stackalloc byte[256];
				destination = new Span<byte>(pointer, 256);
			}
			JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
			utf8Value = destination.Slice(0, written);
		}
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength2 = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
			Span<byte> destination2;
			if (maxEscapedLength2 > 256)
			{
				array2 = ArrayPool<byte>.Shared.Rent(maxEscapedLength2);
				destination2 = array2;
			}
			else
			{
				byte* pointer2 = stackalloc byte[256];
				destination2 = new Span<byte>(pointer2, 256);
			}
			JsonWriterHelper.EscapeString(utf8PropertyName, destination2, firstEscapeIndexProp, _options.Encoder, out var written2);
			utf8PropertyName = destination2.Slice(0, written2);
		}
		WriteStringByOptions(utf8PropertyName, utf8Value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<byte>.Shared.Return(array2);
		}
	}

	private unsafe void WriteStringEscapePropertyOrValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value, int firstEscapeIndexProp, int firstEscapeIndexVal)
	{
		byte[] array = null;
		char[] array2 = null;
		if (firstEscapeIndexVal != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
			Span<byte> destination;
			if (maxEscapedLength > 256)
			{
				array = ArrayPool<byte>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				byte* pointer = stackalloc byte[256];
				destination = new Span<byte>(pointer, 256);
			}
			JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
			utf8Value = destination.Slice(0, written);
		}
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength2 = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
			Span<char> destination2;
			if (maxEscapedLength2 > 128)
			{
				array2 = ArrayPool<char>.Shared.Rent(maxEscapedLength2);
				destination2 = array2;
			}
			else
			{
				char* pointer2 = stackalloc char[128];
				destination2 = new Span<char>(pointer2, 128);
			}
			JsonWriterHelper.EscapeString(propertyName, destination2, firstEscapeIndexProp, _options.Encoder, out var written2);
			propertyName = destination2.Slice(0, written2);
		}
		WriteStringByOptions(propertyName, utf8Value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<char>.Shared.Return(array2);
		}
	}

	private unsafe void WriteStringEscapePropertyOrValue(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value, int firstEscapeIndexProp, int firstEscapeIndexVal)
	{
		char[] array = null;
		byte[] array2 = null;
		if (firstEscapeIndexVal != -1)
		{
			int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);
			Span<char> destination;
			if (maxEscapedLength > 128)
			{
				array = ArrayPool<char>.Shared.Rent(maxEscapedLength);
				destination = array;
			}
			else
			{
				char* pointer = stackalloc char[128];
				destination = new Span<char>(pointer, 128);
			}
			JsonWriterHelper.EscapeString(value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
			value = destination.Slice(0, written);
		}
		if (firstEscapeIndexProp != -1)
		{
			int maxEscapedLength2 = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
			Span<byte> destination2;
			if (maxEscapedLength2 > 256)
			{
				array2 = ArrayPool<byte>.Shared.Rent(maxEscapedLength2);
				destination2 = array2;
			}
			else
			{
				byte* pointer2 = stackalloc byte[256];
				destination2 = new Span<byte>(pointer2, 256);
			}
			JsonWriterHelper.EscapeString(utf8PropertyName, destination2, firstEscapeIndexProp, _options.Encoder, out var written2);
			utf8PropertyName = destination2.Slice(0, written2);
		}
		WriteStringByOptions(utf8PropertyName, value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<byte>.Shared.Return(array2);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(propertyName, value);
		}
		else
		{
			WriteStringMinimized(propertyName, value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(utf8PropertyName, utf8Value);
		}
		else
		{
			WriteStringMinimized(utf8PropertyName, utf8Value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(propertyName, utf8Value);
		}
		else
		{
			WriteStringMinimized(propertyName, utf8Value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteStringIndented(utf8PropertyName, value);
		}
		else
		{
			WriteStringMinimized(utf8PropertyName, value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
	{
		int num = (escapedPropertyName.Length + escapedValue.Length) * 3 + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
	{
		int num = escapedPropertyName.Length + escapedValue.Length + 5;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
	{
		int num = escapedPropertyName.Length * 3 + escapedValue.Length + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
	{
		int num = escapedValue.Length * 3 + escapedPropertyName.Length + 6;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<char> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + (escapedPropertyName.Length + escapedValue.Length) * 3 + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + escapedValue.Length + 6;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedPropertyName, ReadOnlySpan<byte> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + escapedValue.Length + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedPropertyName, ReadOnlySpan<char> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + escapedValue.Length * 3 + escapedPropertyName.Length + 7 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	[CLSCompliant(false)]
	public void WriteNumber(JsonEncodedText propertyName, ulong value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = propertyName.EncodedUtf8Bytes;
		WriteNumberByOptions(encodedUtf8Bytes, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	[CLSCompliant(false)]
	public void WriteNumber(string propertyName, ulong value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), value);
	}

	[CLSCompliant(false)]
	public void WriteNumber(ReadOnlySpan<char> propertyName, ulong value)
	{
		JsonWriterHelper.ValidateProperty(propertyName);
		WriteNumberEscape(propertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	[CLSCompliant(false)]
	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, ulong value)
	{
		JsonWriterHelper.ValidateProperty(utf8PropertyName);
		WriteNumberEscape(utf8PropertyName, value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	[CLSCompliant(false)]
	public void WriteNumber(JsonEncodedText propertyName, uint value)
	{
		WriteNumber(propertyName, (ulong)value);
	}

	[CLSCompliant(false)]
	public void WriteNumber(string propertyName, uint value)
	{
		WriteNumber((propertyName ?? throw new ArgumentNullException("propertyName")).AsSpan(), (ulong)value);
	}

	[CLSCompliant(false)]
	public void WriteNumber(ReadOnlySpan<char> propertyName, uint value)
	{
		WriteNumber(propertyName, (ulong)value);
	}

	[CLSCompliant(false)]
	public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, uint value)
	{
		WriteNumber(utf8PropertyName, (ulong)value);
	}

	private void WriteNumberEscape(ReadOnlySpan<char> propertyName, ulong value)
	{
		int num = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(propertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(propertyName, value);
		}
	}

	private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, ulong value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);
		if (num != -1)
		{
			WriteNumberEscapeProperty(utf8PropertyName, value, num);
		}
		else
		{
			WriteNumberByOptions(utf8PropertyName, value);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, ulong value, int firstEscapeIndexProp)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(propertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, ulong value, int firstEscapeIndexProp)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8PropertyName, destination, firstEscapeIndexProp, _options.Encoder, out var written);
		WriteNumberByOptions(destination.Slice(0, written), value);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, ulong value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(propertyName, value);
		}
		else
		{
			WriteNumberMinimized(propertyName, value);
		}
	}

	private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, ulong value)
	{
		ValidateWritingProperty();
		if (_options.Indented)
		{
			WriteNumberIndented(utf8PropertyName, value);
		}
		else
		{
			WriteNumberMinimized(utf8PropertyName, value);
		}
	}

	private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, ulong value)
	{
		int num = escapedPropertyName.Length * 3 + 20 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, ulong value)
	{
		int num = escapedPropertyName.Length + 20 + 3;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, ulong value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length * 3 + 20 + 5 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedPropertyName, span);
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, ulong value)
	{
		int indentation = Indentation;
		int num = indentation + escapedPropertyName.Length + 20 + 4;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 34;
		escapedPropertyName.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedPropertyName.Length;
		span[BytesPending++] = 34;
		span[BytesPending++] = 58;
		span[BytesPending++] = 32;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WritePropertyName(uint value)
	{
		WritePropertyName((ulong)value);
	}

	internal void WritePropertyName(ulong value)
	{
		Span<byte> destination = stackalloc byte[20];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WritePropertyNameUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteBase64StringValue(ReadOnlySpan<byte> bytes)
	{
		JsonWriterHelper.ValidateBytes(bytes);
		WriteBase64ByOptions(bytes);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteBase64ByOptions(ReadOnlySpan<byte> bytes)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteBase64Indented(bytes);
		}
		else
		{
			WriteBase64Minimized(bytes);
		}
	}

	private void WriteBase64Minimized(ReadOnlySpan<byte> bytes)
	{
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = maxEncodedToUtf8Length + 3;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	private void WriteBase64Indented(ReadOnlySpan<byte> bytes)
	{
		int indentation = Indentation;
		int maxEncodedToUtf8Length = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		int num = indentation + maxEncodedToUtf8Length + 3 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		Base64EncodeAndWrite(bytes, span, maxEncodedToUtf8Length);
		span[BytesPending++] = 34;
	}

	public void WriteCommentValue(string value)
	{
		WriteCommentValue((value ?? throw new ArgumentNullException("value")).AsSpan());
	}

	public void WriteCommentValue(ReadOnlySpan<char> value)
	{
		JsonWriterHelper.ValidateValue(value);
		if (value.IndexOf(s_singleLineCommentDelimiter) != -1)
		{
			ThrowHelper.ThrowArgumentException_InvalidCommentValue();
		}
		WriteCommentByOptions(value);
	}

	private void WriteCommentByOptions(ReadOnlySpan<char> value)
	{
		if (_options.Indented)
		{
			WriteCommentIndented(value);
		}
		else
		{
			WriteCommentMinimized(value);
		}
	}

	private void WriteCommentMinimized(ReadOnlySpan<char> value)
	{
		int num = value.Length * 3 + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		span[BytesPending++] = 47;
		int bytesConsumed = BytesPending++;
		span[bytesConsumed] = 42;
		ReadOnlySpan<byte> utf16Source = MemoryMarshal.AsBytes(value);
		int bytesWritten;
		OperationStatus operationStatus = JsonWriterHelper.ToUtf8(utf16Source, span.Slice(BytesPending), out bytesConsumed, out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 42;
		span[BytesPending++] = 47;
	}

	private void WriteCommentIndented(ReadOnlySpan<char> value)
	{
		int indentation = Indentation;
		int num = indentation + value.Length * 3 + 4 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_tokenType != 0)
		{
			WriteNewLine(span);
		}
		JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
		BytesPending += indentation;
		span[BytesPending++] = 47;
		int bytesConsumed = BytesPending++;
		span[bytesConsumed] = 42;
		ReadOnlySpan<byte> utf16Source = MemoryMarshal.AsBytes(value);
		int bytesWritten;
		OperationStatus operationStatus = JsonWriterHelper.ToUtf8(utf16Source, span.Slice(BytesPending), out bytesConsumed, out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 42;
		span[BytesPending++] = 47;
	}

	public void WriteCommentValue(ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidateValue(utf8Value);
		if (utf8Value.IndexOf(SingleLineCommentDelimiterUtf8) != -1)
		{
			ThrowHelper.ThrowArgumentException_InvalidCommentValue();
		}
		WriteCommentByOptions(utf8Value);
	}

	private void WriteCommentByOptions(ReadOnlySpan<byte> utf8Value)
	{
		if (_options.Indented)
		{
			WriteCommentIndented(utf8Value);
		}
		else
		{
			WriteCommentMinimized(utf8Value);
		}
	}

	private void WriteCommentMinimized(ReadOnlySpan<byte> utf8Value)
	{
		int num = utf8Value.Length + 4;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		span[BytesPending++] = 47;
		span[BytesPending++] = 42;
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
		span[BytesPending++] = 42;
		span[BytesPending++] = 47;
	}

	private void WriteCommentIndented(ReadOnlySpan<byte> utf8Value)
	{
		int indentation = Indentation;
		int num = indentation + utf8Value.Length + 4;
		int num2 = num + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 47;
		span[BytesPending++] = 42;
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
		span[BytesPending++] = 42;
		span[BytesPending++] = 47;
	}

	public void WriteStringValue(DateTime value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteStringValueIndented(value);
		}
		else
		{
			WriteStringValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringValueMinimized(DateTime value)
	{
		int num = 36;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringValueIndented(DateTime value)
	{
		int indentation = Indentation;
		int num = indentation + 33 + 3 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	public void WriteStringValue(DateTimeOffset value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteStringValueIndented(value);
		}
		else
		{
			WriteStringValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringValueMinimized(DateTimeOffset value)
	{
		int num = 36;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringValueIndented(DateTimeOffset value)
	{
		int indentation = Indentation;
		int num = indentation + 33 + 3 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		JsonWriterHelper.WriteDateTimeOffsetTrimmed(span.Slice(BytesPending), value, out var bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	public void WriteNumberValue(decimal value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(value);
		}
		else
		{
			WriteNumberValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(decimal value)
	{
		int num = 32;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberValueIndented(decimal value)
	{
		int indentation = Indentation;
		int num = indentation + 31 + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WriteNumberValueAsString(decimal value)
	{
		Span<byte> destination = stackalloc byte[31];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WriteNumberValueAsStringUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteNumberValue(double value)
	{
		JsonWriterHelper.ValidateDouble(value);
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(value);
		}
		else
		{
			WriteNumberValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(double value)
	{
		int num = 129;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberValueIndented(double value)
	{
		int indentation = Indentation;
		int num = indentation + 128 + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		int bytesWritten;
		bool flag = TryFormatDouble(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private static bool TryFormatDouble(double value, Span<byte> destination, out int bytesWritten)
	{
		return Utf8Formatter.TryFormat(value, destination, out bytesWritten);
	}

	internal void WriteNumberValueAsString(double value)
	{
		Span<byte> destination = stackalloc byte[128];
		int bytesWritten;
		bool flag = TryFormatDouble(value, destination, out bytesWritten);
		WriteNumberValueAsStringUnescaped(destination.Slice(0, bytesWritten));
	}

	internal void WriteFloatingPointConstant(double value)
	{
		if (double.IsNaN(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.NaNValue);
		}
		else if (double.IsPositiveInfinity(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.PositiveInfinityValue);
		}
		else if (double.IsNegativeInfinity(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.NegativeInfinityValue);
		}
		else
		{
			WriteNumberValue(value);
		}
	}

	public void WriteNumberValue(float value)
	{
		JsonWriterHelper.ValidateSingle(value);
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(value);
		}
		else
		{
			WriteNumberValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(float value)
	{
		int num = 129;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberValueIndented(float value)
	{
		int indentation = Indentation;
		int num = indentation + 128 + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		int bytesWritten;
		bool flag = TryFormatSingle(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private static bool TryFormatSingle(float value, Span<byte> destination, out int bytesWritten)
	{
		return Utf8Formatter.TryFormat(value, destination, out bytesWritten);
	}

	internal void WriteNumberValueAsString(float value)
	{
		Span<byte> destination = stackalloc byte[128];
		int bytesWritten;
		bool flag = TryFormatSingle(value, destination, out bytesWritten);
		WriteNumberValueAsStringUnescaped(destination.Slice(0, bytesWritten));
	}

	internal void WriteFloatingPointConstant(float value)
	{
		if (float.IsNaN(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.NaNValue);
		}
		else if (float.IsPositiveInfinity(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.PositiveInfinityValue);
		}
		else if (float.IsNegativeInfinity(value))
		{
			WriteNumberValueAsStringUnescaped(JsonConstants.NegativeInfinityValue);
		}
		else
		{
			WriteNumberValue(value);
		}
	}

	internal void WriteNumberValue(ReadOnlySpan<byte> utf8FormattedNumber)
	{
		JsonWriterHelper.ValidateValue(utf8FormattedNumber);
		JsonWriterHelper.ValidateNumber(utf8FormattedNumber);
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(utf8FormattedNumber);
		}
		else
		{
			WriteNumberValueMinimized(utf8FormattedNumber);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(ReadOnlySpan<byte> utf8Value)
	{
		int num = utf8Value.Length + 1;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
	}

	private void WriteNumberValueIndented(ReadOnlySpan<byte> utf8Value)
	{
		int indentation = Indentation;
		int num = indentation + utf8Value.Length + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
	}

	public void WriteStringValue(Guid value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteStringValueIndented(value);
		}
		else
		{
			WriteStringValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringValueMinimized(Guid value)
	{
		int num = 39;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void WriteStringValueIndented(Guid value)
	{
		int indentation = Indentation;
		int num = indentation + 36 + 3 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
		span[BytesPending++] = 34;
	}

	private void ValidateWritingValue()
	{
		if (_inObject)
		{
			if (_tokenType != JsonTokenType.PropertyName)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueWithinObject, 0, 0, _tokenType);
			}
		}
		else if (CurrentDepth == 0 && _tokenType != 0)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotWriteValueAfterPrimitiveOrClose, 0, 0, _tokenType);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Base64EncodeAndWrite(ReadOnlySpan<byte> bytes, Span<byte> output, int encodingLength)
	{
		byte[] array = null;
		Span<byte> span = ((encodingLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(encodingLength))) : stackalloc byte[256]);
		Span<byte> utf = span;
		int bytesConsumed;
		int bytesWritten;
		OperationStatus operationStatus = Base64.EncodeToUtf8(bytes, utf, out bytesConsumed, out bytesWritten);
		utf = utf.Slice(0, bytesWritten);
		Span<byte> destination = output.Slice(BytesPending);
		utf.Slice(0, bytesWritten).CopyTo(destination);
		BytesPending += bytesWritten;
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public void WriteNullValue()
	{
		WriteLiteralByOptions(JsonConstants.NullValue);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Null;
	}

	public void WriteBooleanValue(bool value)
	{
		if (value)
		{
			WriteLiteralByOptions(JsonConstants.TrueValue);
			_tokenType = JsonTokenType.True;
		}
		else
		{
			WriteLiteralByOptions(JsonConstants.FalseValue);
			_tokenType = JsonTokenType.False;
		}
		SetFlagToAddListSeparatorBeforeNextItem();
	}

	private void WriteLiteralByOptions(ReadOnlySpan<byte> utf8Value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteLiteralIndented(utf8Value);
		}
		else
		{
			WriteLiteralMinimized(utf8Value);
		}
	}

	private void WriteLiteralMinimized(ReadOnlySpan<byte> utf8Value)
	{
		int num = utf8Value.Length + 1;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
	}

	private void WriteLiteralIndented(ReadOnlySpan<byte> utf8Value)
	{
		int indentation = Indentation;
		int num = indentation + utf8Value.Length + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		utf8Value.CopyTo(span.Slice(BytesPending));
		BytesPending += utf8Value.Length;
	}

	public void WriteRawValue(string json, bool skipInputValidation = false)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		TranscodeAndWriteRawValue(json.AsSpan(), skipInputValidation);
	}

	public void WriteRawValue(ReadOnlySpan<char> json, bool skipInputValidation = false)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		TranscodeAndWriteRawValue(json, skipInputValidation);
	}

	public void WriteRawValue(ReadOnlySpan<byte> utf8Json, bool skipInputValidation = false)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (utf8Json.Length == int.MaxValue)
		{
			ThrowHelper.ThrowArgumentException_ValueTooLarge(int.MaxValue);
		}
		WriteRawValueCore(utf8Json, skipInputValidation);
	}

	private void TranscodeAndWriteRawValue(ReadOnlySpan<char> json, bool skipInputValidation)
	{
		if (json.Length > 715827882)
		{
			ThrowHelper.ThrowArgumentException_ValueTooLarge(json.Length);
		}
		byte[] array = null;
		Span<byte> span = (((long)json.Length > 349525L) ? new byte[JsonReaderHelper.GetUtf8ByteCount(json)] : (array = ArrayPool<byte>.Shared.Rent(json.Length * 3)));
		try
		{
			span = span[..JsonReaderHelper.GetUtf8FromText(json, span)];
			WriteRawValueCore(span, skipInputValidation);
		}
		finally
		{
			if (array != null)
			{
				span.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private void WriteRawValueCore(ReadOnlySpan<byte> utf8Json, bool skipInputValidation)
	{
		int length = utf8Json.Length;
		if (length == 0)
		{
			ThrowHelper.ThrowArgumentException(System.SR.ExpectedJsonTokens);
		}
		if (skipInputValidation)
		{
			_tokenType = JsonTokenType.String;
		}
		else
		{
			Utf8JsonReader utf8JsonReader = new Utf8JsonReader(utf8Json);
			while (utf8JsonReader.Read())
			{
			}
			_tokenType = utf8JsonReader.TokenType;
		}
		int num = length + 1;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		utf8Json.CopyTo(span.Slice(BytesPending));
		BytesPending += length;
		SetFlagToAddListSeparatorBeforeNextItem();
	}

	public void WriteNumberValue(int value)
	{
		WriteNumberValue((long)value);
	}

	public void WriteNumberValue(long value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(value);
		}
		else
		{
			WriteNumberValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(long value)
	{
		int num = 21;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberValueIndented(long value)
	{
		int indentation = Indentation;
		int num = indentation + 20 + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WriteNumberValueAsString(long value)
	{
		Span<byte> destination = stackalloc byte[20];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WriteNumberValueAsStringUnescaped(destination.Slice(0, bytesWritten));
	}

	public void WriteStringValue(JsonEncodedText value)
	{
		ReadOnlySpan<byte> encodedUtf8Bytes = value.EncodedUtf8Bytes;
		WriteStringByOptions(encodedUtf8Bytes);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	public void WriteStringValue(string? value)
	{
		if (value == null)
		{
			WriteNullValue();
		}
		else
		{
			WriteStringValue(value.AsSpan());
		}
	}

	public void WriteStringValue(ReadOnlySpan<char> value)
	{
		JsonWriterHelper.ValidateValue(value);
		WriteStringEscape(value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringEscape(ReadOnlySpan<char> value)
	{
		int num = JsonWriterHelper.NeedsEscaping(value, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeValue(value, num);
		}
		else
		{
			WriteStringByOptions(value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<char> value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteStringIndented(value);
		}
		else
		{
			WriteStringMinimized(value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<char> escapedValue)
	{
		int num = escapedValue.Length * 3 + 3;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<char> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + escapedValue.Length * 3 + 3 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		TranscodeAndWrite(escapedValue, span);
		span[BytesPending++] = 34;
	}

	private void WriteStringEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal)
	{
		char[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(value.Length, firstEscapeIndexVal);
		Span<char> span = ((maxEscapedLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(maxEscapedLength))) : stackalloc char[128]);
		Span<char> destination = span;
		JsonWriterHelper.EscapeString(value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written));
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}

	public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
	{
		JsonWriterHelper.ValidateValue(utf8Value);
		WriteStringEscape(utf8Value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	private void WriteStringEscape(ReadOnlySpan<byte> utf8Value)
	{
		int num = JsonWriterHelper.NeedsEscaping(utf8Value, _options.Encoder);
		if (num != -1)
		{
			WriteStringEscapeValue(utf8Value, num);
		}
		else
		{
			WriteStringByOptions(utf8Value);
		}
	}

	private void WriteStringByOptions(ReadOnlySpan<byte> utf8Value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteStringIndented(utf8Value);
		}
		else
		{
			WriteStringMinimized(utf8Value);
		}
	}

	private void WriteStringMinimized(ReadOnlySpan<byte> escapedValue)
	{
		int num = escapedValue.Length + 2;
		int num2 = num + 1;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringIndented(ReadOnlySpan<byte> escapedValue)
	{
		int indentation = Indentation;
		int num = indentation + escapedValue.Length + 2;
		int num2 = num + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num2)
		{
			Grow(num2);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		span[BytesPending++] = 34;
		escapedValue.CopyTo(span.Slice(BytesPending));
		BytesPending += escapedValue.Length;
		span[BytesPending++] = 34;
	}

	private void WriteStringEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
	{
		byte[] array = null;
		int maxEscapedLength = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);
		Span<byte> span = ((maxEscapedLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(maxEscapedLength))) : stackalloc byte[256]);
		Span<byte> destination = span;
		JsonWriterHelper.EscapeString(utf8Value, destination, firstEscapeIndexVal, _options.Encoder, out var written);
		WriteStringByOptions(destination.Slice(0, written));
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	internal void WriteNumberValueAsStringUnescaped(ReadOnlySpan<byte> utf8Value)
	{
		WriteStringByOptions(utf8Value);
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.String;
	}

	[CLSCompliant(false)]
	public void WriteNumberValue(uint value)
	{
		WriteNumberValue((ulong)value);
	}

	[CLSCompliant(false)]
	public void WriteNumberValue(ulong value)
	{
		if (!_options.SkipValidation)
		{
			ValidateWritingValue();
		}
		if (_options.Indented)
		{
			WriteNumberValueIndented(value);
		}
		else
		{
			WriteNumberValueMinimized(value);
		}
		SetFlagToAddListSeparatorBeforeNextItem();
		_tokenType = JsonTokenType.Number;
	}

	private void WriteNumberValueMinimized(ulong value)
	{
		int num = 21;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	private void WriteNumberValueIndented(ulong value)
	{
		int indentation = Indentation;
		int num = indentation + 20 + 1 + s_newLineLength;
		if (_memory.Length - BytesPending < num)
		{
			Grow(num);
		}
		Span<byte> span = _memory.Span;
		if (_currentDepth < 0)
		{
			span[BytesPending++] = 44;
		}
		if (_tokenType != JsonTokenType.PropertyName)
		{
			if (_tokenType != 0)
			{
				WriteNewLine(span);
			}
			JsonWriterHelper.WriteIndentation(span.Slice(BytesPending), indentation);
			BytesPending += indentation;
		}
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, span.Slice(BytesPending), out bytesWritten);
		BytesPending += bytesWritten;
	}

	internal void WriteNumberValueAsString(ulong value)
	{
		Span<byte> destination = stackalloc byte[20];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten);
		WriteNumberValueAsStringUnescaped(destination.Slice(0, bytesWritten));
	}
}
