using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography;

internal static class SymmetricImportExportExtensions
{
	private static readonly CngKeyBlobFormat s_cipherKeyBlobFormat = new CngKeyBlobFormat("CipherKeyBlob");

	public static byte[] GetSymmetricKeyDataIfExportable(this CngKey cngKey, string algorithm)
	{
		byte[] buffer = cngKey.Export(s_cipherKeyBlobFormat);
		using MemoryStream input = new MemoryStream(buffer);
		using BinaryReader binaryReader = new BinaryReader(input, Encoding.Unicode);
		int num = binaryReader.ReadInt32();
		if (num != 16)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num2 = binaryReader.ReadInt32();
		if (num2 != 1380470851)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num3 = binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		string text = new string(binaryReader.ReadChars(num3 / 2 - 1));
		if (text != algorithm)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CngKeyWrongAlgorithm, text, algorithm));
		}
		if (binaryReader.ReadChar() != 0)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num4 = binaryReader.ReadInt32();
		if (num4 != 1296188491)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int num5 = binaryReader.ReadInt32();
		if (num5 != 1)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyBlobParsingError);
		}
		int count = binaryReader.ReadInt32();
		return binaryReader.ReadBytes(count);
	}
}
