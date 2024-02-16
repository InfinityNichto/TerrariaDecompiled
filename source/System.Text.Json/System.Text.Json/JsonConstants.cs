namespace System.Text.Json;

internal static class JsonConstants
{
	public static ReadOnlySpan<byte> Utf8Bom => "\ufeff"u8;

	public static ReadOnlySpan<byte> TrueValue => "true"u8;

	public static ReadOnlySpan<byte> FalseValue => "false"u8;

	public static ReadOnlySpan<byte> NullValue => "null"u8;

	public static ReadOnlySpan<byte> NaNValue => "NaN"u8;

	public static ReadOnlySpan<byte> PositiveInfinityValue => "Infinity"u8;

	public static ReadOnlySpan<byte> NegativeInfinityValue => "-Infinity"u8;

	public static ReadOnlySpan<byte> Delimiters => ",}] \n\r\t/"u8;

	public static ReadOnlySpan<byte> EscapableChars => "\"nrt/ubf"u8;
}
