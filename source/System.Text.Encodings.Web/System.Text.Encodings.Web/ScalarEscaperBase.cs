namespace System.Text.Encodings.Web;

internal abstract class ScalarEscaperBase
{
	internal abstract int EncodeUtf16(Rune value, Span<char> destination);

	internal abstract int EncodeUtf8(Rune value, Span<byte> destination);
}
