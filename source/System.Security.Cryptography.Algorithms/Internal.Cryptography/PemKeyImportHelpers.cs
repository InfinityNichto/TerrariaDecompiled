using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class PemKeyImportHelpers
{
	public delegate void ImportKeyAction(ReadOnlySpan<byte> source, out int bytesRead);

	public delegate ImportKeyAction FindImportActionFunc(ReadOnlySpan<char> label);

	public delegate void ImportEncryptedKeyAction<TPass>(ReadOnlySpan<TPass> password, ReadOnlySpan<byte> source, out int bytesRead);

	public static void ImportEncryptedPem<TPass>(ReadOnlySpan<char> input, ReadOnlySpan<TPass> password, ImportEncryptedKeyAction<TPass> importAction)
	{
		bool flag = false;
		PemFields pemFields = default(PemFields);
		ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
		ReadOnlySpan<char> readOnlySpan2 = input;
		PemFields fields;
		ReadOnlySpan<char> readOnlySpan3;
		while (PemEncoding.TryFind(readOnlySpan2, out fields))
		{
			readOnlySpan3 = readOnlySpan2;
			ReadOnlySpan<char> span = readOnlySpan3[fields.Label];
			if (span.SequenceEqual("ENCRYPTED PRIVATE KEY"))
			{
				if (flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				flag = true;
				pemFields = fields;
				readOnlySpan = readOnlySpan2;
			}
			Index end = fields.Location.End;
			readOnlySpan3 = readOnlySpan2;
			readOnlySpan2 = readOnlySpan3[end..];
		}
		if (!flag)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_NoPemFound, "input");
		}
		readOnlySpan3 = readOnlySpan;
		int length = readOnlySpan3.Length;
		Range base64Data = pemFields.Base64Data;
		int offset = base64Data.Start.GetOffset(length);
		int bytesRead = base64Data.End.GetOffset(length) - offset;
		ReadOnlySpan<char> chars = readOnlySpan3.Slice(offset, bytesRead);
		int decodedDataLength = pemFields.DecodedDataLength;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decodedDataLength);
		int bytesWritten = 0;
		try
		{
			if (!Convert.TryFromBase64Chars(chars, array, out bytesWritten))
			{
				throw new ArgumentException();
			}
			Span<byte> span2 = array.AsSpan(0, bytesWritten);
			importAction(password, span2, out bytesRead);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}

	public static void ImportPem(ReadOnlySpan<char> input, FindImportActionFunc callback)
	{
		ImportKeyAction importKeyAction = null;
		PemFields pemFields = default(PemFields);
		ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
		bool flag = false;
		ReadOnlySpan<char> readOnlySpan2 = input;
		PemFields fields;
		ReadOnlySpan<char> readOnlySpan3;
		while (PemEncoding.TryFind(readOnlySpan2, out fields))
		{
			readOnlySpan3 = readOnlySpan2;
			ReadOnlySpan<char> readOnlySpan4 = readOnlySpan3[fields.Label];
			ImportKeyAction importKeyAction2 = callback(readOnlySpan4);
			if (importKeyAction2 != null)
			{
				if (importKeyAction != null || flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				importKeyAction = importKeyAction2;
				pemFields = fields;
				readOnlySpan = readOnlySpan2;
			}
			else if (readOnlySpan4.SequenceEqual("ENCRYPTED PRIVATE KEY"))
			{
				if (importKeyAction != null || flag)
				{
					throw new ArgumentException(System.SR.Argument_PemImport_AmbiguousPem, "input");
				}
				flag = true;
			}
			Index end = fields.Location.End;
			readOnlySpan3 = readOnlySpan2;
			readOnlySpan2 = readOnlySpan3[end..];
		}
		if (flag)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_EncryptedPem, "input");
		}
		if (importKeyAction == null)
		{
			throw new ArgumentException(System.SR.Argument_PemImport_NoPemFound, "input");
		}
		readOnlySpan3 = readOnlySpan;
		int length = readOnlySpan3.Length;
		Range base64Data = pemFields.Base64Data;
		int offset = base64Data.Start.GetOffset(length);
		int bytesRead = base64Data.End.GetOffset(length) - offset;
		ReadOnlySpan<char> chars = readOnlySpan3.Slice(offset, bytesRead);
		int decodedDataLength = pemFields.DecodedDataLength;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decodedDataLength);
		int bytesWritten = 0;
		try
		{
			if (!Convert.TryFromBase64Chars(chars, array, out bytesWritten))
			{
				throw new ArgumentException();
			}
			Span<byte> span = array.AsSpan(0, bytesWritten);
			importKeyAction(span, out bytesRead);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}
}
