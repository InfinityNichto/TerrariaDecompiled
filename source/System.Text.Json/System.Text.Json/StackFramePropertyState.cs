namespace System.Text.Json;

internal enum StackFramePropertyState : byte
{
	None,
	ReadName,
	Name,
	ReadValue,
	ReadValueIsEnd,
	TryRead
}
