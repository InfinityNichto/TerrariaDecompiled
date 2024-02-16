using System.Text;

namespace System.Formats.Asn1;

internal static class AsnCharacterStringEncodings
{
	private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly Encoding s_bmpEncoding = new BMPEncoding();

	private static readonly Encoding s_ia5Encoding = new IA5Encoding();

	private static readonly Encoding s_visibleStringEncoding = new VisibleStringEncoding();

	private static readonly Encoding s_numericStringEncoding = new NumericStringEncoding();

	private static readonly Encoding s_printableStringEncoding = new PrintableStringEncoding();

	private static readonly Encoding s_t61Encoding = new T61Encoding();

	internal static Encoding GetEncoding(UniversalTagNumber encodingType)
	{
		return encodingType switch
		{
			UniversalTagNumber.UTF8String => s_utf8Encoding, 
			UniversalTagNumber.NumericString => s_numericStringEncoding, 
			UniversalTagNumber.PrintableString => s_printableStringEncoding, 
			UniversalTagNumber.IA5String => s_ia5Encoding, 
			UniversalTagNumber.VisibleString => s_visibleStringEncoding, 
			UniversalTagNumber.BMPString => s_bmpEncoding, 
			UniversalTagNumber.TeletexString => s_t61Encoding, 
			_ => throw new ArgumentOutOfRangeException("encodingType", encodingType, null), 
		};
	}
}
