using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class Helpers
{
	[UnsupportedOSPlatformGuard("ios")]
	[UnsupportedOSPlatformGuard("tvos")]
	public static bool IsDSASupported
	{
		get
		{
			if (!OperatingSystem.IsIOS())
			{
				return !OperatingSystem.IsTvOS();
			}
			return false;
		}
	}

	[return: NotNullIfNotNull("src")]
	public static byte[] CloneByteArray(this byte[] src)
	{
		if (src == null)
		{
			return null;
		}
		return (byte[])src.Clone();
	}

	internal static ReadOnlySpan<byte> AsSpanParameter(this byte[] array, string paramName)
	{
		if (array == null)
		{
			throw new ArgumentNullException(paramName);
		}
		return new ReadOnlySpan<byte>(array);
	}

	public static char[] ToHexArrayUpper(this byte[] bytes)
	{
		char[] array = new char[bytes.Length * 2];
		System.HexConverter.EncodeToUtf16(bytes, array);
		return array;
	}

	public static string ToHexStringUpper(this byte[] bytes)
	{
		return Convert.ToHexString(bytes);
	}

	public static byte[] DecodeHexString(this string hexString)
	{
		int num = 0;
		ReadOnlySpan<char> readOnlySpan = hexString;
		if (readOnlySpan.Length != 0 && readOnlySpan[0] == '\u200e')
		{
			readOnlySpan = readOnlySpan.Slice(1);
		}
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			if (char.IsWhiteSpace(readOnlySpan[i]))
			{
				num++;
			}
		}
		uint num2 = (uint)(readOnlySpan.Length - num) / 2u;
		byte[] array = new byte[num2];
		byte b = 0;
		bool flag = false;
		int num3 = 0;
		for (int j = 0; j < readOnlySpan.Length; j++)
		{
			char c = readOnlySpan[j];
			if (!char.IsWhiteSpace(c))
			{
				b <<= 4;
				b |= (byte)System.HexConverter.FromChar(c);
				flag = !flag;
				if (!flag)
				{
					array[num3] = b;
					num3++;
				}
			}
		}
		return array;
	}

	public static bool ContentsEqual(this byte[] a1, byte[] a2)
	{
		if (a1 == null)
		{
			return a2 == null;
		}
		if (a2 == null || a1.Length != a2.Length)
		{
			return false;
		}
		for (int i = 0; i < a1.Length; i++)
		{
			if (a1[i] != a2[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static void AddRange<T>(this ICollection<T> coll, IEnumerable<T> newData)
	{
		foreach (T newDatum in newData)
		{
			coll.Add(newDatum);
		}
	}

	public static bool IsValidDay(this Calendar calendar, int year, int month, int day, int era)
	{
		if (calendar.IsValidMonth(year, month, era) && day >= 1)
		{
			return day <= calendar.GetDaysInMonth(year, month, era);
		}
		return false;
	}

	private static bool IsValidMonth(this Calendar calendar, int year, int month, int era)
	{
		if (calendar.IsValidYear(year, era) && month >= 1)
		{
			return month <= calendar.GetMonthsInYear(year, era);
		}
		return false;
	}

	private static bool IsValidYear(this Calendar calendar, int year, int era)
	{
		if (year >= calendar.GetYear(calendar.MinSupportedDateTime))
		{
			return year <= calendar.GetYear(calendar.MaxSupportedDateTime);
		}
		return false;
	}

	public static void ValidateDer(ReadOnlyMemory<byte> encodedValue)
	{
		try
		{
			AsnReader asnReader = new AsnReader(encodedValue, AsnEncodingRules.DER);
			while (asnReader.HasData)
			{
				Asn1Tag asn1Tag = asnReader.PeekTag();
				if (asn1Tag.TagClass == TagClass.Universal)
				{
					switch ((UniversalTagNumber)asn1Tag.TagValue)
					{
					case UniversalTagNumber.External:
					case UniversalTagNumber.Embedded:
					case UniversalTagNumber.Sequence:
					case UniversalTagNumber.Set:
					case UniversalTagNumber.UnrestrictedCharacterString:
						if (!asn1Tag.IsConstructed)
						{
							throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
						}
						break;
					default:
						if (asn1Tag.IsConstructed)
						{
							throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
						}
						break;
					}
				}
				if (asn1Tag.IsConstructed)
				{
					ValidateDer(asnReader.PeekContentBytes());
				}
				asnReader.ReadEncodedValue();
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	public static bool AreSamePublicECParameters(ECParameters aParameters, ECParameters bParameters)
	{
		if (aParameters.Curve.CurveType != bParameters.Curve.CurveType)
		{
			return false;
		}
		if (!aParameters.Q.X.ContentsEqual(bParameters.Q.X) || !aParameters.Q.Y.ContentsEqual(bParameters.Q.Y))
		{
			return false;
		}
		ECCurve curve = aParameters.Curve;
		ECCurve curve2 = bParameters.Curve;
		if (curve.IsNamed)
		{
			if (curve.Oid.Value == curve2.Oid.Value)
			{
				return curve.Oid.FriendlyName == curve2.Oid.FriendlyName;
			}
			return false;
		}
		if (!curve.IsExplicit)
		{
			return false;
		}
		if (!curve.G.X.ContentsEqual(curve2.G.X) || !curve.G.Y.ContentsEqual(curve2.G.Y) || !curve.Order.ContentsEqual(curve2.Order) || !curve.A.ContentsEqual(curve2.A) || !curve.B.ContentsEqual(curve2.B))
		{
			return false;
		}
		if (curve.IsPrime)
		{
			return curve.Prime.ContentsEqual(curve2.Prime);
		}
		if (curve.IsCharacteristic2)
		{
			return curve.Polynomial.ContentsEqual(curve2.Polynomial);
		}
		return false;
	}
}
