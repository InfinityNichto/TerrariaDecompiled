using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct DirectoryStringAsn
{
	internal string TeletexString;

	internal string PrintableString;

	internal ReadOnlyMemory<byte>? UniversalString;

	internal string Utf8String;

	internal string BmpString;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (TeletexString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.TeletexString, TeletexString);
			flag = true;
		}
		if (PrintableString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.PrintableString, PrintableString);
			flag = true;
		}
		if (UniversalString.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			if (!Asn1Tag.TryDecode(UniversalString.Value.Span, out var tag, out var _) || !tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.UniversalString)))
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteEncodedValue(UniversalString.Value.Span);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			flag = true;
		}
		if (Utf8String != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.UTF8String, Utf8String);
			flag = true;
		}
		if (BmpString != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteCharacterString(UniversalTagNumber.BMPString, BmpString);
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}
}
