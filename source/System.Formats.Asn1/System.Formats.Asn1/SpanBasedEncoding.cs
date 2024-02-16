using System.Text;

namespace System.Formats.Asn1;

internal abstract class SpanBasedEncoding : Encoding
{
	protected SpanBasedEncoding()
		: base(0, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback)
	{
	}

	protected abstract int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write);

	protected abstract int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write);

	public override int GetByteCount(char[] chars, int index, int count)
	{
		return GetByteCount(new ReadOnlySpan<char>(chars, index, count));
	}

	public unsafe override int GetByteCount(char* chars, int count)
	{
		return GetByteCount(new ReadOnlySpan<char>(chars, count));
	}

	public override int GetByteCount(string s)
	{
		return GetByteCount(s.AsSpan());
	}

	public override int GetByteCount(ReadOnlySpan<char> chars)
	{
		return GetBytes(chars, Span<byte>.Empty, write: false);
	}

	public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		return GetBytes(new ReadOnlySpan<char>(chars, charIndex, charCount), new Span<byte>(bytes, byteIndex, bytes.Length - byteIndex), write: true);
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		return GetBytes(new ReadOnlySpan<char>(chars, charCount), new Span<byte>(bytes, byteCount), write: true);
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetCharCount(new ReadOnlySpan<byte>(bytes, index, count));
	}

	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		return GetCharCount(new ReadOnlySpan<byte>(bytes, count));
	}

	public override int GetCharCount(ReadOnlySpan<byte> bytes)
	{
		return GetChars(bytes, Span<char>.Empty, write: false);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		return GetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), new Span<char>(chars, charIndex, chars.Length - charIndex), write: true);
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		return GetChars(new ReadOnlySpan<byte>(bytes, byteCount), new Span<char>(chars, charCount), write: true);
	}
}
