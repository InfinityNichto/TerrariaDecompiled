using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Text;

internal static class EncodingTable
{
	private static readonly int[] s_encodingNameIndices = new int[43]
	{
		0, 14, 28, 33, 38, 43, 50, 61, 76, 82,
		88, 103, 113, 123, 131, 140, 149, 165, 175, 190,
		192, 198, 203, 210, 227, 244, 261, 278, 289, 291,
		299, 305, 313, 321, 327, 335, 343, 348, 353, 372,
		391, 410, 429
	};

	private static readonly ushort[] s_codePagesByName = new ushort[42]
	{
		20127, 20127, 20127, 20127, 28591, 20127, 28591, 65000, 20127, 28591,
		1200, 28591, 28591, 20127, 20127, 28591, 20127, 28591, 28591, 28591,
		28591, 1200, 1200, 65000, 65001, 65000, 65001, 1201, 20127, 20127,
		1200, 1201, 1200, 12000, 12001, 12000, 65000, 65001, 65000, 65001,
		65000, 65001
	};

	private static readonly ushort[] s_mappedCodePages = new ushort[8] { 1200, 1201, 12000, 12001, 20127, 28591, 65000, 65001 };

	private static readonly int[] s_uiFamilyCodePages = new int[8] { 1200, 1200, 1200, 1200, 1252, 1252, 1200, 1200 };

	private static readonly int[] s_webNameIndices = new int[9] { 0, 6, 14, 20, 28, 36, 46, 51, 56 };

	private static readonly int[] s_englishNameIndices = new int[9] { 0, 7, 27, 43, 70, 78, 100, 115, 130 };

	private static readonly uint[] s_flags = new uint[8] { 512u, 0u, 0u, 0u, 257u, 771u, 257u, 771u };

	private static readonly Hashtable s_nameToCodePage = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

	private static CodePageDataItem[] s_codePageToCodePageData;

	internal static int GetCodePageFromName(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		object obj = s_nameToCodePage[name];
		if (obj != null)
		{
			return (int)obj;
		}
		int num = InternalGetCodePageFromName(name);
		s_nameToCodePage[name] = num;
		return num;
	}

	private static int InternalGetCodePageFromName(string name)
	{
		int i = 0;
		int num = s_encodingNameIndices.Length - 2;
		ReadOnlySpan<char> strA = name.ToLowerInvariant().AsSpan();
		while (num - i > 3)
		{
			int num2 = (num - i) / 2 + i;
			int num3 = string.CompareOrdinal(strA, "ansi_x3.4-1968ansi_x3.4-1986asciicp367cp819csasciicsisolatin1csunicode11utf7ibm367ibm819iso-10646-ucs-2iso-8859-1iso-ir-100iso-ir-6iso646-usiso8859-1iso_646.irv:1991iso_8859-1iso_8859-1:1987l1latin1ucs-2unicodeunicode-1-1-utf-7unicode-1-1-utf-8unicode-2-0-utf-7unicode-2-0-utf-8unicodefffeusus-asciiutf-16utf-16beutf-16leutf-32utf-32beutf-32leutf-7utf-8x-unicode-1-1-utf-7x-unicode-1-1-utf-8x-unicode-2-0-utf-7x-unicode-2-0-utf-8".AsSpan(s_encodingNameIndices[num2], s_encodingNameIndices[num2 + 1] - s_encodingNameIndices[num2]));
			if (num3 == 0)
			{
				return s_codePagesByName[num2];
			}
			if (num3 < 0)
			{
				num = num2;
			}
			else
			{
				i = num2;
			}
		}
		for (; i <= num; i++)
		{
			if (string.CompareOrdinal(strA, "ansi_x3.4-1968ansi_x3.4-1986asciicp367cp819csasciicsisolatin1csunicode11utf7ibm367ibm819iso-10646-ucs-2iso-8859-1iso-ir-100iso-ir-6iso646-usiso8859-1iso_646.irv:1991iso_8859-1iso_8859-1:1987l1latin1ucs-2unicodeunicode-1-1-utf-7unicode-1-1-utf-8unicode-2-0-utf-7unicode-2-0-utf-8unicodefffeusus-asciiutf-16utf-16beutf-16leutf-32utf-32beutf-32leutf-7utf-8x-unicode-1-1-utf-7x-unicode-1-1-utf-8x-unicode-2-0-utf-7x-unicode-2-0-utf-8".AsSpan(s_encodingNameIndices[i], s_encodingNameIndices[i + 1] - s_encodingNameIndices[i])) == 0)
			{
				return s_codePagesByName[i];
			}
		}
		throw new ArgumentException(SR.Format(SR.Argument_EncodingNotSupported, name), "name");
	}

	internal static EncodingInfo[] GetEncodings()
	{
		ushort[] array = s_mappedCodePages;
		EncodingInfo[] array2 = new EncodingInfo[LocalAppContextSwitches.EnableUnsafeUTF7Encoding ? array.Length : (array.Length - 1)];
		string text = "utf-16utf-16BEutf-32utf-32BEus-asciiiso-8859-1utf-7utf-8";
		int[] array3 = s_webNameIndices;
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			int num2 = array[i];
			if (num2 != 65000 || LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
			{
				array2[num++] = new EncodingInfo(num2, text[array3[i]..array3[i + 1]], GetDisplayName(num2, i));
			}
		}
		return array2;
	}

	internal static EncodingInfo[] GetEncodings(Dictionary<int, EncodingInfo> encodingInfoList)
	{
		ushort[] array = s_mappedCodePages;
		string text = "utf-16utf-16BEutf-32utf-32BEus-asciiiso-8859-1utf-7utf-8";
		int[] array2 = s_webNameIndices;
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i];
			if (!encodingInfoList.ContainsKey(num) && (num != 65000 || LocalAppContextSwitches.EnableUnsafeUTF7Encoding))
			{
				encodingInfoList[num] = new EncodingInfo(num, text[array2[i]..array2[i + 1]], GetDisplayName(num, i));
			}
		}
		if (!LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
		{
			encodingInfoList.Remove(65000);
		}
		EncodingInfo[] array3 = new EncodingInfo[encodingInfoList.Count];
		int num2 = 0;
		foreach (KeyValuePair<int, EncodingInfo> encodingInfo in encodingInfoList)
		{
			array3[num2++] = encodingInfo.Value;
		}
		return array3;
	}

	internal static CodePageDataItem GetCodePageDataItem(int codePage)
	{
		if (s_codePageToCodePageData == null)
		{
			Interlocked.CompareExchange(ref s_codePageToCodePageData, new CodePageDataItem[s_mappedCodePages.Length], null);
		}
		int num;
		switch (codePage)
		{
		case 1200:
			num = 0;
			break;
		case 1201:
			num = 1;
			break;
		case 12000:
			num = 2;
			break;
		case 12001:
			num = 3;
			break;
		case 20127:
			num = 4;
			break;
		case 28591:
			num = 5;
			break;
		case 65000:
			num = 6;
			break;
		case 65001:
			num = 7;
			break;
		default:
			return null;
		}
		CodePageDataItem codePageDataItem = s_codePageToCodePageData[num];
		if (codePageDataItem == null)
		{
			Interlocked.CompareExchange(ref s_codePageToCodePageData[num], InternalGetCodePageDataItem(codePage, num), null);
			codePageDataItem = s_codePageToCodePageData[num];
		}
		return codePageDataItem;
	}

	private static CodePageDataItem InternalGetCodePageDataItem(int codePage, int index)
	{
		int uiFamilyCodePage = s_uiFamilyCodePages[index];
		string text = "utf-16utf-16BEutf-32utf-32BEus-asciiiso-8859-1utf-7utf-8"[s_webNameIndices[index]..s_webNameIndices[index + 1]];
		string headerName = text;
		string bodyName = text;
		string displayName = GetDisplayName(codePage, index);
		uint flags = s_flags[index];
		return new CodePageDataItem(uiFamilyCodePage, text, headerName, bodyName, displayName, flags);
	}

	private static string GetDisplayName(int codePage, int englishNameIndex)
	{
		string text = SR.GetResourceString("Globalization_cp_" + codePage);
		if (string.IsNullOrEmpty(text))
		{
			text = "UnicodeUnicode (Big-Endian)Unicode (UTF-32)Unicode (UTF-32 Big-Endian)US-ASCIIWestern European (ISO)Unicode (UTF-7)Unicode (UTF-8)"[s_englishNameIndices[englishNameIndex]..s_englishNameIndices[englishNameIndex + 1]];
		}
		return text;
	}
}
