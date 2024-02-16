using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace System.Security;

internal static class IdentityHelper
{
	private static readonly char[] s_base32Char = new char[32]
	{
		'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
		'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
		'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3',
		'4', '5'
	};

	internal static string GetNormalizedUriHash(Uri uri)
	{
		return GetStrongHashSuitableForObjectName(uri.ToString());
	}

	internal static string GetNormalizedStrongNameHash(AssemblyName name)
	{
		byte[] publicKey = name.GetPublicKey();
		if (publicKey == null || publicKey.Length == 0)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(publicKey);
		binaryWriter.Write(name.Version.Major);
		binaryWriter.Write(name.Name);
		memoryStream.Position = 0L;
		return GetStrongHashSuitableForObjectName(memoryStream);
	}

	internal static string GetStrongHashSuitableForObjectName(string name)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(name.ToUpperInvariant());
		memoryStream.Position = 0L;
		return GetStrongHashSuitableForObjectName(memoryStream);
	}

	internal static string GetStrongHashSuitableForObjectName(Stream stream)
	{
		using SHA1 sHA = SHA1.Create();
		return ToBase32StringSuitableForDirName(sHA.ComputeHash(stream));
	}

	internal static string ToBase32StringSuitableForDirName(byte[] buff)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = buff.Length;
		int num2 = 0;
		do
		{
			byte b = (byte)((num2 < num) ? buff[num2++] : 0);
			byte b2 = (byte)((num2 < num) ? buff[num2++] : 0);
			byte b3 = (byte)((num2 < num) ? buff[num2++] : 0);
			byte b4 = (byte)((num2 < num) ? buff[num2++] : 0);
			byte b5 = (byte)((num2 < num) ? buff[num2++] : 0);
			stringBuilder.Append(s_base32Char[b & 0x1F]);
			stringBuilder.Append(s_base32Char[b2 & 0x1F]);
			stringBuilder.Append(s_base32Char[b3 & 0x1F]);
			stringBuilder.Append(s_base32Char[b4 & 0x1F]);
			stringBuilder.Append(s_base32Char[b5 & 0x1F]);
			stringBuilder.Append(s_base32Char[((b & 0xE0) >> 5) | ((b4 & 0x60) >> 2)]);
			stringBuilder.Append(s_base32Char[((b2 & 0xE0) >> 5) | ((b5 & 0x60) >> 2)]);
			b3 >>= 5;
			if ((b4 & 0x80u) != 0)
			{
				b3 = (byte)(b3 | 8u);
			}
			if ((b5 & 0x80u) != 0)
			{
				b3 = (byte)(b3 | 0x10u);
			}
			stringBuilder.Append(s_base32Char[b3]);
		}
		while (num2 < num);
		return stringBuilder.ToString();
	}
}
