namespace System.Text.Json;

public struct JsonReaderState
{
	internal long _lineNumber;

	internal long _bytePositionInLine;

	internal bool _inObject;

	internal bool _isNotPrimitive;

	internal bool _stringHasEscaping;

	internal bool _trailingCommaBeforeComment;

	internal JsonTokenType _tokenType;

	internal JsonTokenType _previousTokenType;

	internal JsonReaderOptions _readerOptions;

	internal BitStack _bitStack;

	public JsonReaderOptions Options => _readerOptions;

	public JsonReaderState(JsonReaderOptions options = default(JsonReaderOptions))
	{
		_lineNumber = 0L;
		_bytePositionInLine = 0L;
		_inObject = false;
		_isNotPrimitive = false;
		_stringHasEscaping = false;
		_trailingCommaBeforeComment = false;
		_tokenType = JsonTokenType.None;
		_previousTokenType = JsonTokenType.None;
		_readerOptions = options;
		_bitStack = default(BitStack);
	}
}
