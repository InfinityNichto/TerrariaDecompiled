namespace System.Text.Json;

internal enum StackFrameObjectState : byte
{
	None,
	StartToken,
	ReadAheadNameOrEndObject,
	ReadNameOrEndObject,
	ReadAheadIdValue,
	ReadAheadRefValue,
	ReadIdValue,
	ReadRefValue,
	ReadAheadRefEndObject,
	ReadRefEndObject,
	ReadAheadValuesName,
	ReadValuesName,
	ReadAheadValuesStartArray,
	ReadValuesStartArray,
	PropertyValue,
	CreatedObject,
	ReadElements,
	EndToken,
	EndTokenValidation
}
