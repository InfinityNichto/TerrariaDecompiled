using System.Buffers;
using System.Numerics;
using System.Text.Unicode;

namespace System.Text.Encodings.Web;

internal sealed class DefaultHtmlEncoder : HtmlEncoder
{
	private sealed class EscaperImplementation : ScalarEscaperBase
	{
		internal static readonly EscaperImplementation Singleton = new EscaperImplementation();

		private EscaperImplementation()
		{
		}

		internal override int EncodeUtf8(Rune value, Span<byte> destination)
		{
			if (value.Value == 60)
			{
				if (SpanUtility.TryWriteBytes(destination, 38, 108, 116, 59))
				{
					return 4;
				}
			}
			else if (value.Value == 62)
			{
				if (SpanUtility.TryWriteBytes(destination, 38, 103, 116, 59))
				{
					return 4;
				}
			}
			else if (value.Value == 38)
			{
				if (SpanUtility.TryWriteBytes(destination, 38, 97, 109, 112, 59))
				{
					return 5;
				}
			}
			else
			{
				if (value.Value != 34)
				{
					return TryEncodeScalarAsHex(this, (uint)value.Value, destination);
				}
				if (SpanUtility.TryWriteBytes(destination, 38, 113, 117, 111, 116, 59))
				{
					return 6;
				}
			}
			return -1;
			static int TryEncodeScalarAsHex(object @this, uint scalarValue, Span<byte> destination)
			{
				int num = (int)((uint)BitOperations.Log2(scalarValue) / 4u + 4);
				if (SpanUtility.IsValidIndex(destination, num))
				{
					destination[num] = 59;
					SpanUtility.TryWriteBytes(destination, 38, 35, 120, 48);
					destination = destination.Slice(3, num - 3);
					int num2 = destination.Length - 1;
					while (SpanUtility.IsValidIndex(destination, num2))
					{
						char c = System.HexConverter.ToCharUpper((int)scalarValue);
						destination[num2] = (byte)c;
						scalarValue >>= 4;
						num2--;
					}
					return destination.Length + 4;
				}
				return -1;
			}
		}

		internal override int EncodeUtf16(Rune value, Span<char> destination)
		{
			if (value.Value == 60)
			{
				if (SpanUtility.TryWriteChars(destination, '&', 'l', 't', ';'))
				{
					return 4;
				}
			}
			else if (value.Value == 62)
			{
				if (SpanUtility.TryWriteChars(destination, '&', 'g', 't', ';'))
				{
					return 4;
				}
			}
			else if (value.Value == 38)
			{
				if (SpanUtility.TryWriteChars(destination, '&', 'a', 'm', 'p', ';'))
				{
					return 5;
				}
			}
			else
			{
				if (value.Value != 34)
				{
					return TryEncodeScalarAsHex(this, (uint)value.Value, destination);
				}
				if (SpanUtility.TryWriteChars(destination, '&', 'q', 'u', 'o', 't', ';'))
				{
					return 6;
				}
			}
			return -1;
			static int TryEncodeScalarAsHex(object @this, uint scalarValue, Span<char> destination)
			{
				int num = (int)((uint)BitOperations.Log2(scalarValue) / 4u + 4);
				if (SpanUtility.IsValidIndex(destination, num))
				{
					destination[num] = ';';
					SpanUtility.TryWriteChars(destination, '&', '#', 'x', '0');
					destination = destination.Slice(3, num - 3);
					int num2 = destination.Length - 1;
					while (SpanUtility.IsValidIndex(destination, num2))
					{
						char c = System.HexConverter.ToCharUpper((int)scalarValue);
						destination[num2] = c;
						scalarValue >>= 4;
						num2--;
					}
					return destination.Length + 4;
				}
				return -1;
			}
		}
	}

	internal static readonly DefaultHtmlEncoder BasicLatinSingleton = new DefaultHtmlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

	private readonly OptimizedInboxTextEncoder _innerEncoder;

	public override int MaxOutputCharactersPerInputCharacter => 8;

	internal DefaultHtmlEncoder(TextEncoderSettings settings)
	{
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		_innerEncoder = new OptimizedInboxTextEncoder(EscaperImplementation.Singleton, in settings.GetAllowedCodePointsBitmap());
	}

	private protected override OperationStatus EncodeCore(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock)
	{
		return _innerEncoder.Encode(source, destination, out charsConsumed, out charsWritten, isFinalBlock);
	}

	private protected override OperationStatus EncodeUtf8Core(ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
	{
		return _innerEncoder.EncodeUtf8(utf8Source, utf8Destination, out bytesConsumed, out bytesWritten, isFinalBlock);
	}

	private protected override int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
	{
		return _innerEncoder.GetIndexOfFirstCharToEncode(text);
	}

	public unsafe override int FindFirstCharacterToEncode(char* text, int textLength)
	{
		return _innerEncoder.FindFirstCharacterToEncode(text, textLength);
	}

	public override int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
	{
		return _innerEncoder.GetIndexOfFirstByteToEncode(utf8Text);
	}

	public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
	{
		return _innerEncoder.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
	}

	public override bool WillEncode(int unicodeScalar)
	{
		return !_innerEncoder.IsScalarValueAllowed(new Rune(unicodeScalar));
	}
}
