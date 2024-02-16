using System.Collections.Generic;
using System.Threading;

namespace System.Text;

internal static class EncodingTable
{
	private static readonly Dictionary<string, int> s_nameToCodePageCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<int, string> s_codePageToWebNameCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> s_codePageToEnglishNameCache = new Dictionary<int, string>();

	private static readonly ReaderWriterLockSlim s_cacheLock = new ReaderWriterLockSlim();

	private static readonly int[] s_encodingNameIndices = new int[365]
	{
		0, 3, 9, 17, 21, 31, 41, 51, 61, 71,
		81, 91, 101, 111, 121, 131, 141, 151, 158, 165,
		170, 177, 184, 191, 198, 205, 212, 219, 226, 233,
		240, 247, 254, 259, 265, 271, 277, 283, 288, 293,
		298, 303, 308, 313, 318, 323, 328, 333, 338, 343,
		350, 355, 360, 365, 370, 375, 380, 385, 390, 395,
		400, 405, 410, 415, 420, 425, 430, 435, 440, 446,
		453, 472, 480, 490, 498, 507, 515, 523, 531, 539,
		547, 555, 563, 571, 579, 587, 595, 603, 611, 619,
		627, 635, 644, 655, 666, 681, 692, 703, 714, 725,
		736, 752, 770, 785, 801, 808, 821, 837, 847, 859,
		867, 876, 883, 890, 897, 910, 922, 934, 946, 958,
		970, 982, 994, 1006, 1018, 1030, 1042, 1054, 1066, 1078,
		1093, 1105, 1117, 1129, 1141, 1153, 1168, 1186, 1204, 1222,
		1240, 1258, 1276, 1305, 1323, 1341, 1355, 1374, 1392, 1410,
		1427, 1435, 1443, 1451, 1457, 1463, 1469, 1514, 1521, 1527,
		1536, 1544, 1554, 1557, 1563, 1568, 1574, 1580, 1590, 1598,
		1606, 1614, 1622, 1630, 1638, 1646, 1654, 1662, 1670, 1678,
		1686, 1694, 1702, 1708, 1715, 1721, 1727, 1733, 1739, 1745,
		1751, 1757, 1763, 1769, 1775, 1781, 1787, 1793, 1799, 1805,
		1811, 1817, 1823, 1829, 1835, 1841, 1847, 1853, 1859, 1865,
		1871, 1877, 1883, 1889, 1895, 1901, 1904, 1915, 1929, 1940,
		1953, 1969, 1982, 1998, 2009, 2020, 2031, 2041, 2051, 2061,
		2071, 2081, 2091, 2101, 2118, 2130, 2140, 2150, 2160, 2170,
		2180, 2190, 2200, 2210, 2220, 2230, 2239, 2248, 2259, 2269,
		2284, 2294, 2309, 2319, 2334, 2344, 2359, 2369, 2384, 2394,
		2409, 2419, 2434, 2444, 2459, 2464, 2467, 2471, 2477, 2484,
		2490, 2495, 2501, 2510, 2518, 2527, 2541, 2555, 2569, 2576,
		2584, 2586, 2588, 2590, 2592, 2594, 2600, 2606, 2612, 2618,
		2624, 2631, 2640, 2648, 2657, 2666, 2690, 2702, 2711, 2720,
		2724, 2731, 2738, 2744, 2756, 2768, 2780, 2792, 2804, 2816,
		2828, 2840, 2852, 2863, 2869, 2882, 2896, 2904, 2912, 2921,
		2930, 2939, 2948, 2957, 2966, 2975, 2984, 2993, 3016, 3021,
		3029, 3037, 3045, 3050, 3062, 3077, 3090, 3100, 3110, 3120,
		3130, 3140, 3150, 3160, 3170, 3180, 3190, 3202, 3210, 3227,
		3244, 3258, 3272, 3283, 3295, 3310, 3324, 3336, 3350, 3360,
		3373, 3388, 3398, 3404, 3412
	};

	private static readonly ushort[] s_codePagesByName = new ushort[364]
	{
		437, 28596, 708, 950, 950, 858, 20924, 1140, 1141, 1142,
		1143, 1144, 1145, 1146, 1147, 1148, 1149, 936, 950, 936,
		858, 20924, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147,
		1148, 1149, 37, 21025, 1026, 1252, 1256, 20273, 20278, 20280,
		20284, 20285, 20290, 20297, 20420, 20423, 20424, 437, 500, 50227,
		850, 852, 855, 857, 858, 860, 861, 862, 863, 864,
		865, 866, 869, 870, 20871, 875, 20880, 20905, 950, 51949,
		51932, 936, 936, 37, 1026, 20273, 20277, 20278, 20280, 20284,
		20285, 20290, 20297, 20420, 20423, 20424, 500, 870, 20871, 20880,
		20905, 20838, 50221, 50225, 936, 28592, 28593, 28594, 28599, 28605,
		28596, 28595, 28597, 28598, 20866, 949, 437, 932, 932, 28595,
		20106, 720, 862, 874, 20420, 500, 37, 500, 20277, 20284,
		20278, 20297, 20285, 20423, 20424, 20871, 20280, 37, 20277, 870,
		20278, 20905, 37, 37, 870, 20880, 1141, 1142, 1145, 1143,
		1147, 1146, 1148, 1149, 1144, 20290, 20924, 1142, 1143, 1140,
		28596, 28597, 28597, 51936, 51932, 51949, 51932, 54936, 936, 936,
		936, 936, 936, 20106, 28597, 28597, 28598, 52936, 20838, 858,
		20924, 1047, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147,
		1148, 1149, 37, 1026, 20273, 20277, 20278, 20280, 20284, 20285,
		20290, 20297, 20420, 20423, 20424, 437, 500, 737, 775, 850,
		852, 855, 857, 860, 861, 862, 863, 864, 865, 866,
		869, 870, 20871, 20880, 20905, 20105, 50220, 51932, 50225, 50225,
		50225, 51949, 51949, 874, 28603, 28605, 28592, 28593, 28594, 28595,
		28596, 28597, 28598, 28598, 38598, 28599, 28592, 28593, 28594, 28597,
		28596, 28598, 28595, 28599, 949, 936, 28592, 28605, 28592, 28592,
		28593, 28593, 28594, 28594, 28595, 28595, 28596, 28596, 28597, 28597,
		28598, 28598, 28599, 28599, 1361, 20866, 20866, 20866, 21866, 21866,
		20866, 949, 949, 949, 949, 949, 949, 949, 949, 949,
		28592, 28593, 28594, 28599, 28605, 28592, 28593, 28594, 28599, 28605,
		28598, 10000, 932, 20108, 20108, 858, 20107, 932, 932, 932,
		20107, 874, 28598, 1250, 1251, 1252, 1253, 1254, 1255, 1256,
		1257, 1258, 874, 1252, 20000, 20002, 1250, 1251, 20001, 20003,
		20004, 20005, 20261, 20269, 20936, 20949, 50227, 20833, 51932, 51936,
		51932, 29001, 20105, 20106, 20108, 20107, 57006, 57003, 57002, 57010,
		57008, 57009, 57007, 57011, 57004, 57005, 10004, 10029, 10008, 10002,
		10082, 10007, 10006, 10005, 10079, 10001, 10003, 10010, 10021, 10081,
		10017, 932, 932, 950
	};

	private static readonly ushort[] s_mappedCodePages = new ushort[132]
	{
		37, 437, 500, 708, 720, 737, 775, 850, 852, 855,
		857, 858, 860, 861, 862, 863, 864, 865, 866, 869,
		870, 874, 875, 932, 936, 949, 950, 1026, 1047, 1140,
		1141, 1142, 1143, 1144, 1145, 1146, 1147, 1148, 1149, 1250,
		1251, 1252, 1253, 1254, 1255, 1256, 1257, 1258, 1361, 10000,
		10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10010, 10017,
		10021, 10029, 10079, 10081, 10082, 20000, 20001, 20002, 20003, 20004,
		20005, 20105, 20106, 20107, 20108, 20261, 20269, 20273, 20277, 20278,
		20280, 20284, 20285, 20290, 20297, 20420, 20423, 20424, 20833, 20838,
		20866, 20871, 20880, 20905, 20924, 20932, 20936, 20949, 21025, 21866,
		28592, 28593, 28594, 28595, 28596, 28597, 28598, 28599, 28603, 28605,
		29001, 38598, 50220, 50221, 50222, 50225, 50227, 51932, 51936, 51949,
		52936, 54936, 57002, 57003, 57004, 57005, 57006, 57007, 57008, 57009,
		57010, 57011
	};

	private static readonly int[] s_webNameIndices = new int[133]
	{
		0, 6, 12, 18, 26, 33, 39, 45, 51, 57,
		63, 69, 77, 83, 89, 96, 102, 108, 114, 119,
		125, 131, 142, 147, 156, 162, 176, 180, 187, 195,
		203, 211, 219, 227, 235, 243, 251, 259, 267, 275,
		287, 299, 311, 323, 335, 347, 359, 371, 383, 388,
		397, 411, 428, 440, 452, 464, 475, 489, 506, 520,
		535, 545, 553, 568, 581, 595, 608, 617, 631, 640,
		649, 658, 663, 675, 688, 703, 712, 721, 727, 733,
		739, 745, 751, 757, 763, 769, 775, 781, 787, 810,
		818, 824, 830, 836, 842, 850, 856, 865, 874, 880,
		886, 896, 906, 916, 926, 936, 946, 956, 966, 977,
		988, 996, 1008, 1019, 1030, 1041, 1052, 1061, 1067, 1073,
		1079, 1089, 1096, 1106, 1116, 1126, 1136, 1146, 1156, 1166,
		1176, 1186, 1196
	};

	private static readonly int[] s_englishNameIndices = new int[133]
	{
		0, 22, 39, 65, 82, 94, 105, 117, 139, 161,
		173, 186, 210, 226, 241, 253, 274, 286, 298, 312,
		331, 364, 378, 403, 423, 450, 456, 482, 510, 521,
		548, 573, 605, 637, 660, 683, 703, 727, 758, 785,
		811, 829, 855, 870, 887, 903, 919, 935, 955, 969,
		991, 1005, 1030, 1042, 1054, 1066, 1077, 1091, 1115, 1129,
		1144, 1154, 1176, 1191, 1204, 1218, 1243, 1253, 1279, 1293,
		1308, 1319, 1341, 1353, 1366, 1381, 1385, 1393, 1413, 1440,
		1467, 1485, 1503, 1518, 1548, 1567, 1586, 1604, 1623, 1651,
		1668, 1685, 1707, 1736, 1756, 1767, 1805, 1835, 1849, 1888,
		1905, 1927, 1940, 1952, 1966, 1978, 1989, 2008, 2021, 2035,
		2048, 2054, 2074, 2088, 2120, 2160, 2172, 2201, 2215, 2239,
		2251, 2274, 2302, 2318, 2331, 2342, 2354, 2368, 2379, 2392,
		2407, 2421, 2434
	};

	internal static int GetCodePageFromName(string name)
	{
		if (name == null)
		{
			return 0;
		}
		s_cacheLock.EnterUpgradeableReadLock();
		try
		{
			if (s_nameToCodePageCache.TryGetValue(name, out var value))
			{
				return value;
			}
			value = InternalGetCodePageFromName(name);
			if (value == 0)
			{
				return 0;
			}
			s_cacheLock.EnterWriteLock();
			try
			{
				if (s_nameToCodePageCache.TryGetValue(name, out var value2))
				{
					return value2;
				}
				s_nameToCodePageCache.Add(name, value);
				return value;
			}
			finally
			{
				s_cacheLock.ExitWriteLock();
			}
		}
		finally
		{
			s_cacheLock.ExitUpgradeableReadLock();
		}
	}

	private static int InternalGetCodePageFromName(string name)
	{
		int i = 0;
		int num = s_encodingNameIndices.Length - 2;
		name = name.ToLowerInvariant();
		while (num - i > 3)
		{
			int num2 = (num - i) / 2 + i;
			int num3 = CompareOrdinal(name, "437arabicasmo-708big5big5-hkscsccsid00858ccsid00924ccsid01140ccsid01141ccsid01142ccsid01143ccsid01144ccsid01145ccsid01146ccsid01147ccsid01148ccsid01149chinesecn-big5cn-gbcp00858cp00924cp01140cp01141cp01142cp01143cp01144cp01145cp01146cp01147cp01148cp01149cp037cp1025cp1026cp1252cp1256cp273cp278cp280cp284cp285cp290cp297cp420cp423cp424cp437cp500cp50227cp850cp852cp855cp857cp858cp860cp861cp862cp863cp864cp865cp866cp869cp870cp871cp875cp880cp905csbig5cseuckrcseucpkdfmtjapanesecsgb2312csgb231280csibm037csibm1026csibm273csibm277csibm278csibm280csibm284csibm285csibm290csibm297csibm420csibm423csibm424csibm500csibm870csibm871csibm880csibm905csibmthaicsiso2022jpcsiso2022krcsiso58gb231280csisolatin2csisolatin3csisolatin4csisolatin5csisolatin9csisolatinarabiccsisolatincyrilliccsisolatingreekcsisolatinhebrewcskoi8rcsksc56011987cspc8codepage437csshiftjiscswindows31jcyrillicdin_66003dos-720dos-862dos-874ebcdic-cp-ar1ebcdic-cp-beebcdic-cp-caebcdic-cp-chebcdic-cp-dkebcdic-cp-esebcdic-cp-fiebcdic-cp-frebcdic-cp-gbebcdic-cp-grebcdic-cp-heebcdic-cp-isebcdic-cp-itebcdic-cp-nlebcdic-cp-noebcdic-cp-roeceebcdic-cp-seebcdic-cp-trebcdic-cp-usebcdic-cp-wtebcdic-cp-yuebcdic-cyrillicebcdic-de-273+euroebcdic-dk-277+euroebcdic-es-284+euroebcdic-fi-278+euroebcdic-fr-297+euroebcdic-gb-285+euroebcdic-international-500+euroebcdic-is-871+euroebcdic-it-280+euroebcdic-jp-kanaebcdic-latin9--euroebcdic-no-277+euroebcdic-se-278+euroebcdic-us-37+euroecma-114ecma-118elot_928euc-cneuc-jpeuc-krextended_unix_code_packed_format_for_japanesegb18030gb2312gb2312-80gb231280gb_2312-80gbkgermangreekgreek8hebrewhz-gb-2312ibm-thaiibm00858ibm00924ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149ibm037ibm1026ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424ibm437ibm500ibm737ibm775ibm850ibm852ibm855ibm857ibm860ibm861ibm862ibm863ibm864ibm865ibm866ibm869ibm870ibm871ibm880ibm905irviso-2022-jpiso-2022-jpeuciso-2022-kriso-2022-kr-7iso-2022-kr-7bitiso-2022-kr-8iso-2022-kr-8bitiso-8859-11iso-8859-13iso-8859-15iso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-8 visualiso-8859-8-iiso-8859-9iso-ir-101iso-ir-109iso-ir-110iso-ir-126iso-ir-127iso-ir-138iso-ir-144iso-ir-148iso-ir-149iso-ir-58iso8859-2iso_8859-15iso_8859-2iso_8859-2:1987iso_8859-3iso_8859-3:1988iso_8859-4iso_8859-4:1988iso_8859-5iso_8859-5:1988iso_8859-6iso_8859-6:1987iso_8859-7iso_8859-7:1987iso_8859-8iso_8859-8:1988iso_8859-9iso_8859-9:1989johabkoikoi8koi8-rkoi8-rukoi8-ukoi8rkoreanks-c-5601ks-c5601ks_c_5601ks_c_5601-1987ks_c_5601-1989ks_c_5601_1987ksc5601ksc_5601l2l3l4l5l9latin2latin3latin4latin5latin9logicalmacintoshms_kanjinorwegianns_4551-1pc-multilingual-850+eurosen_850200_bshift-jisshift_jissjisswedishtis-620visualwindows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258windows-874x-ansix-chinese-cnsx-chinese-etenx-cp1250x-cp1251x-cp20001x-cp20003x-cp20004x-cp20005x-cp20261x-cp20269x-cp20936x-cp20949x-cp50227x-ebcdic-koreanextendedx-eucx-euc-cnx-euc-jpx-europax-ia5x-ia5-germanx-ia5-norwegianx-ia5-swedishx-iscii-asx-iscii-bex-iscii-dex-iscii-gux-iscii-kax-iscii-max-iscii-orx-iscii-pax-iscii-tax-iscii-tex-mac-arabicx-mac-cex-mac-chinesesimpx-mac-chinesetradx-mac-croatianx-mac-cyrillicx-mac-greekx-mac-hebrewx-mac-icelandicx-mac-japanesex-mac-koreanx-mac-romanianx-mac-thaix-mac-turkishx-mac-ukrainianx-ms-cp932x-sjisx-x-big5", s_encodingNameIndices[num2], s_encodingNameIndices[num2 + 1] - s_encodingNameIndices[num2]);
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
			if (CompareOrdinal(name, "437arabicasmo-708big5big5-hkscsccsid00858ccsid00924ccsid01140ccsid01141ccsid01142ccsid01143ccsid01144ccsid01145ccsid01146ccsid01147ccsid01148ccsid01149chinesecn-big5cn-gbcp00858cp00924cp01140cp01141cp01142cp01143cp01144cp01145cp01146cp01147cp01148cp01149cp037cp1025cp1026cp1252cp1256cp273cp278cp280cp284cp285cp290cp297cp420cp423cp424cp437cp500cp50227cp850cp852cp855cp857cp858cp860cp861cp862cp863cp864cp865cp866cp869cp870cp871cp875cp880cp905csbig5cseuckrcseucpkdfmtjapanesecsgb2312csgb231280csibm037csibm1026csibm273csibm277csibm278csibm280csibm284csibm285csibm290csibm297csibm420csibm423csibm424csibm500csibm870csibm871csibm880csibm905csibmthaicsiso2022jpcsiso2022krcsiso58gb231280csisolatin2csisolatin3csisolatin4csisolatin5csisolatin9csisolatinarabiccsisolatincyrilliccsisolatingreekcsisolatinhebrewcskoi8rcsksc56011987cspc8codepage437csshiftjiscswindows31jcyrillicdin_66003dos-720dos-862dos-874ebcdic-cp-ar1ebcdic-cp-beebcdic-cp-caebcdic-cp-chebcdic-cp-dkebcdic-cp-esebcdic-cp-fiebcdic-cp-frebcdic-cp-gbebcdic-cp-grebcdic-cp-heebcdic-cp-isebcdic-cp-itebcdic-cp-nlebcdic-cp-noebcdic-cp-roeceebcdic-cp-seebcdic-cp-trebcdic-cp-usebcdic-cp-wtebcdic-cp-yuebcdic-cyrillicebcdic-de-273+euroebcdic-dk-277+euroebcdic-es-284+euroebcdic-fi-278+euroebcdic-fr-297+euroebcdic-gb-285+euroebcdic-international-500+euroebcdic-is-871+euroebcdic-it-280+euroebcdic-jp-kanaebcdic-latin9--euroebcdic-no-277+euroebcdic-se-278+euroebcdic-us-37+euroecma-114ecma-118elot_928euc-cneuc-jpeuc-krextended_unix_code_packed_format_for_japanesegb18030gb2312gb2312-80gb231280gb_2312-80gbkgermangreekgreek8hebrewhz-gb-2312ibm-thaiibm00858ibm00924ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149ibm037ibm1026ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424ibm437ibm500ibm737ibm775ibm850ibm852ibm855ibm857ibm860ibm861ibm862ibm863ibm864ibm865ibm866ibm869ibm870ibm871ibm880ibm905irviso-2022-jpiso-2022-jpeuciso-2022-kriso-2022-kr-7iso-2022-kr-7bitiso-2022-kr-8iso-2022-kr-8bitiso-8859-11iso-8859-13iso-8859-15iso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-8 visualiso-8859-8-iiso-8859-9iso-ir-101iso-ir-109iso-ir-110iso-ir-126iso-ir-127iso-ir-138iso-ir-144iso-ir-148iso-ir-149iso-ir-58iso8859-2iso_8859-15iso_8859-2iso_8859-2:1987iso_8859-3iso_8859-3:1988iso_8859-4iso_8859-4:1988iso_8859-5iso_8859-5:1988iso_8859-6iso_8859-6:1987iso_8859-7iso_8859-7:1987iso_8859-8iso_8859-8:1988iso_8859-9iso_8859-9:1989johabkoikoi8koi8-rkoi8-rukoi8-ukoi8rkoreanks-c-5601ks-c5601ks_c_5601ks_c_5601-1987ks_c_5601-1989ks_c_5601_1987ksc5601ksc_5601l2l3l4l5l9latin2latin3latin4latin5latin9logicalmacintoshms_kanjinorwegianns_4551-1pc-multilingual-850+eurosen_850200_bshift-jisshift_jissjisswedishtis-620visualwindows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258windows-874x-ansix-chinese-cnsx-chinese-etenx-cp1250x-cp1251x-cp20001x-cp20003x-cp20004x-cp20005x-cp20261x-cp20269x-cp20936x-cp20949x-cp50227x-ebcdic-koreanextendedx-eucx-euc-cnx-euc-jpx-europax-ia5x-ia5-germanx-ia5-norwegianx-ia5-swedishx-iscii-asx-iscii-bex-iscii-dex-iscii-gux-iscii-kax-iscii-max-iscii-orx-iscii-pax-iscii-tax-iscii-tex-mac-arabicx-mac-cex-mac-chinesesimpx-mac-chinesetradx-mac-croatianx-mac-cyrillicx-mac-greekx-mac-hebrewx-mac-icelandicx-mac-japanesex-mac-koreanx-mac-romanianx-mac-thaix-mac-turkishx-mac-ukrainianx-ms-cp932x-sjisx-x-big5", s_encodingNameIndices[i], s_encodingNameIndices[i + 1] - s_encodingNameIndices[i]) == 0)
			{
				return s_codePagesByName[i];
			}
		}
		return 0;
	}

	private static int CompareOrdinal(string s1, string s2, int index, int length)
	{
		int num = s1.Length;
		if (num > length)
		{
			num = length;
		}
		int i;
		for (i = 0; i < num && s1[i] == s2[index + i]; i++)
		{
		}
		if (i < num)
		{
			return s1[i] - s2[index + i];
		}
		return s1.Length - length;
	}

	internal static string GetWebNameFromCodePage(int codePage)
	{
		return GetNameFromCodePage(codePage, "ibm037ibm437ibm500asmo-708dos-720ibm737ibm775ibm850ibm852ibm855ibm857ibm00858ibm860ibm861dos-862ibm863ibm864ibm865cp866ibm869ibm870windows-874cp875shift_jisgb2312ks_c_5601-1987big5ibm1026ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149windows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258johabmacintoshx-mac-japanesex-mac-chinesetradx-mac-koreanx-mac-arabicx-mac-hebrewx-mac-greekx-mac-cyrillicx-mac-chinesesimpx-mac-romanianx-mac-ukrainianx-mac-thaix-mac-cex-mac-icelandicx-mac-turkishx-mac-croatianx-chinese-cnsx-cp20001x-chinese-etenx-cp20003x-cp20004x-cp20005x-ia5x-ia5-germanx-ia5-swedishx-ia5-norwegianx-cp20261x-cp20269ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424x-ebcdic-koreanextendedibm-thaikoi8-ribm871ibm880ibm905ibm00924euc-jpx-cp20936x-cp20949cp1025koi8-uiso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-9iso-8859-13iso-8859-15x-europaiso-8859-8-iiso-2022-jpcsiso2022jpiso-2022-jpiso-2022-krx-cp50227euc-jpeuc-cneuc-krhz-gb-2312gb18030x-iscii-dex-iscii-bex-iscii-tax-iscii-tex-iscii-asx-iscii-orx-iscii-kax-iscii-max-iscii-gux-iscii-pa", s_webNameIndices, s_codePageToWebNameCache);
	}

	internal static string GetEnglishNameFromCodePage(int codePage)
	{
		return GetNameFromCodePage(codePage, "IBM EBCDIC (US-Canada)OEM United StatesIBM EBCDIC (International)Arabic (ASMO 708)Arabic (DOS)Greek (DOS)Baltic (DOS)Western European (DOS)Central European (DOS)OEM CyrillicTurkish (DOS)OEM Multilingual Latin IPortuguese (DOS)Icelandic (DOS)Hebrew (DOS)French Canadian (DOS)Arabic (864)Nordic (DOS)Cyrillic (DOS)Greek, Modern (DOS)IBM EBCDIC (Multilingual Latin-2)Thai (Windows)IBM EBCDIC (Greek Modern)Japanese (Shift-JIS)Chinese Simplified (GB2312)KoreanChinese Traditional (Big5)IBM EBCDIC (Turkish Latin-5)IBM Latin-1IBM EBCDIC (US-Canada-Euro)IBM EBCDIC (Germany-Euro)IBM EBCDIC (Denmark-Norway-Euro)IBM EBCDIC (Finland-Sweden-Euro)IBM EBCDIC (Italy-Euro)IBM EBCDIC (Spain-Euro)IBM EBCDIC (UK-Euro)IBM EBCDIC (France-Euro)IBM EBCDIC (International-Euro)IBM EBCDIC (Icelandic-Euro)Central European (Windows)Cyrillic (Windows)Western European (Windows)Greek (Windows)Turkish (Windows)Hebrew (Windows)Arabic (Windows)Baltic (Windows)Vietnamese (Windows)Korean (Johab)Western European (Mac)Japanese (Mac)Chinese Traditional (Mac)Korean (Mac)Arabic (Mac)Hebrew (Mac)Greek (Mac)Cyrillic (Mac)Chinese Simplified (Mac)Romanian (Mac)Ukrainian (Mac)Thai (Mac)Central European (Mac)Icelandic (Mac)Turkish (Mac)Croatian (Mac)Chinese Traditional (CNS)TCA TaiwanChinese Traditional (Eten)IBM5550 TaiwanTeleText TaiwanWang TaiwanWestern European (IA5)German (IA5)Swedish (IA5)Norwegian (IA5)T.61ISO-6937IBM EBCDIC (Germany)IBM EBCDIC (Denmark-Norway)IBM EBCDIC (Finland-Sweden)IBM EBCDIC (Italy)IBM EBCDIC (Spain)IBM EBCDIC (UK)IBM EBCDIC (Japanese katakana)IBM EBCDIC (France)IBM EBCDIC (Arabic)IBM EBCDIC (Greek)IBM EBCDIC (Hebrew)IBM EBCDIC (Korean Extended)IBM EBCDIC (Thai)Cyrillic (KOI8-R)IBM EBCDIC (Icelandic)IBM EBCDIC (Cyrillic Russian)IBM EBCDIC (Turkish)IBM Latin-1Japanese (JIS 0208-1990 and 0212-1990)Chinese Simplified (GB2312-80)Korean WansungIBM EBCDIC (Cyrillic Serbian-Bulgarian)Cyrillic (KOI8-U)Central European (ISO)Latin 3 (ISO)Baltic (ISO)Cyrillic (ISO)Arabic (ISO)Greek (ISO)Hebrew (ISO-Visual)Turkish (ISO)Estonian (ISO)Latin 9 (ISO)EuropaHebrew (ISO-Logical)Japanese (JIS)Japanese (JIS-Allow 1 byte Kana)Japanese (JIS-Allow 1 byte Kana - SO/SI)Korean (ISO)Chinese Simplified (ISO-2022)Japanese (EUC)Chinese Simplified (EUC)Korean (EUC)Chinese Simplified (HZ)Chinese Simplified (GB18030)ISCII DevanagariISCII BengaliISCII TamilISCII TeluguISCII AssameseISCII OriyaISCII KannadaISCII MalayalamISCII GujaratiISCII Punjabi", s_englishNameIndices, s_codePageToEnglishNameCache);
	}

	private static string GetNameFromCodePage(int codePage, string names, int[] indices, Dictionary<int, string> cache)
	{
		for (int i = 0; i < s_mappedCodePages.Length; i++)
		{
			if (s_mappedCodePages[i] != codePage)
			{
				continue;
			}
			s_cacheLock.EnterUpgradeableReadLock();
			try
			{
				if (cache.TryGetValue(codePage, out var value))
				{
					return value;
				}
				value = names.Substring(indices[i], indices[i + 1] - indices[i]);
				s_cacheLock.EnterWriteLock();
				try
				{
					if (cache.TryGetValue(codePage, out var value2))
					{
						return value2;
					}
					cache.Add(codePage, value);
					return value;
				}
				finally
				{
					s_cacheLock.ExitWriteLock();
				}
			}
			finally
			{
				s_cacheLock.ExitUpgradeableReadLock();
			}
		}
		return null;
	}
}
