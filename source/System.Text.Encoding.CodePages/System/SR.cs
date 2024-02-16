using System.Resources;
using FxResources.System.Text.Encoding.CodePages;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentNull_Array => GetResourceString("ArgumentNull_Array");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_IndexCount => GetResourceString("ArgumentOutOfRange_IndexCount");

	internal static string ArgumentOutOfRange_IndexCountBuffer => GetResourceString("ArgumentOutOfRange_IndexCountBuffer");

	internal static string NotSupported_NoCodepageData => GetResourceString("NotSupported_NoCodepageData");

	internal static string Argument_EncodingConversionOverflowBytes => GetResourceString("Argument_EncodingConversionOverflowBytes");

	internal static string Argument_InvalidCharSequenceNoIndex => GetResourceString("Argument_InvalidCharSequenceNoIndex");

	internal static string ArgumentOutOfRange_GetByteCountOverflow => GetResourceString("ArgumentOutOfRange_GetByteCountOverflow");

	internal static string Argument_EncodingConversionOverflowChars => GetResourceString("Argument_EncodingConversionOverflowChars");

	internal static string ArgumentOutOfRange_GetCharCountOverflow => GetResourceString("ArgumentOutOfRange_GetCharCountOverflow");

	internal static string Argument_EncoderFallbackNotEmpty => GetResourceString("Argument_EncoderFallbackNotEmpty");

	internal static string Argument_RecursiveFallback => GetResourceString("Argument_RecursiveFallback");

	internal static string ArgumentOutOfRange_Range => GetResourceString("ArgumentOutOfRange_Range");

	internal static string Argument_CodepageNotSupported => GetResourceString("Argument_CodepageNotSupported");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string MissingEncodingNameResource => GetResourceString("MissingEncodingNameResource");

	internal static string Globalization_cp_37 => GetResourceString("Globalization_cp_37");

	internal static string Globalization_cp_437 => GetResourceString("Globalization_cp_437");

	internal static string Globalization_cp_500 => GetResourceString("Globalization_cp_500");

	internal static string Globalization_cp_708 => GetResourceString("Globalization_cp_708");

	internal static string Globalization_cp_720 => GetResourceString("Globalization_cp_720");

	internal static string Globalization_cp_737 => GetResourceString("Globalization_cp_737");

	internal static string Globalization_cp_775 => GetResourceString("Globalization_cp_775");

	internal static string Globalization_cp_850 => GetResourceString("Globalization_cp_850");

	internal static string Globalization_cp_852 => GetResourceString("Globalization_cp_852");

	internal static string Globalization_cp_855 => GetResourceString("Globalization_cp_855");

	internal static string Globalization_cp_857 => GetResourceString("Globalization_cp_857");

	internal static string Globalization_cp_858 => GetResourceString("Globalization_cp_858");

	internal static string Globalization_cp_860 => GetResourceString("Globalization_cp_860");

	internal static string Globalization_cp_861 => GetResourceString("Globalization_cp_861");

	internal static string Globalization_cp_862 => GetResourceString("Globalization_cp_862");

	internal static string Globalization_cp_863 => GetResourceString("Globalization_cp_863");

	internal static string Globalization_cp_864 => GetResourceString("Globalization_cp_864");

	internal static string Globalization_cp_865 => GetResourceString("Globalization_cp_865");

	internal static string Globalization_cp_866 => GetResourceString("Globalization_cp_866");

	internal static string Globalization_cp_869 => GetResourceString("Globalization_cp_869");

	internal static string Globalization_cp_870 => GetResourceString("Globalization_cp_870");

	internal static string Globalization_cp_874 => GetResourceString("Globalization_cp_874");

	internal static string Globalization_cp_875 => GetResourceString("Globalization_cp_875");

	internal static string Globalization_cp_932 => GetResourceString("Globalization_cp_932");

	internal static string Globalization_cp_936 => GetResourceString("Globalization_cp_936");

	internal static string Globalization_cp_949 => GetResourceString("Globalization_cp_949");

	internal static string Globalization_cp_950 => GetResourceString("Globalization_cp_950");

	internal static string Globalization_cp_1026 => GetResourceString("Globalization_cp_1026");

	internal static string Globalization_cp_1047 => GetResourceString("Globalization_cp_1047");

	internal static string Globalization_cp_1140 => GetResourceString("Globalization_cp_1140");

	internal static string Globalization_cp_1141 => GetResourceString("Globalization_cp_1141");

	internal static string Globalization_cp_1142 => GetResourceString("Globalization_cp_1142");

	internal static string Globalization_cp_1143 => GetResourceString("Globalization_cp_1143");

	internal static string Globalization_cp_1144 => GetResourceString("Globalization_cp_1144");

	internal static string Globalization_cp_1145 => GetResourceString("Globalization_cp_1145");

	internal static string Globalization_cp_1146 => GetResourceString("Globalization_cp_1146");

	internal static string Globalization_cp_1147 => GetResourceString("Globalization_cp_1147");

	internal static string Globalization_cp_1148 => GetResourceString("Globalization_cp_1148");

	internal static string Globalization_cp_1149 => GetResourceString("Globalization_cp_1149");

	internal static string Globalization_cp_1250 => GetResourceString("Globalization_cp_1250");

	internal static string Globalization_cp_1251 => GetResourceString("Globalization_cp_1251");

	internal static string Globalization_cp_1252 => GetResourceString("Globalization_cp_1252");

	internal static string Globalization_cp_1253 => GetResourceString("Globalization_cp_1253");

	internal static string Globalization_cp_1254 => GetResourceString("Globalization_cp_1254");

	internal static string Globalization_cp_1255 => GetResourceString("Globalization_cp_1255");

	internal static string Globalization_cp_1256 => GetResourceString("Globalization_cp_1256");

	internal static string Globalization_cp_1257 => GetResourceString("Globalization_cp_1257");

	internal static string Globalization_cp_1258 => GetResourceString("Globalization_cp_1258");

	internal static string Globalization_cp_1361 => GetResourceString("Globalization_cp_1361");

	internal static string Globalization_cp_10000 => GetResourceString("Globalization_cp_10000");

	internal static string Globalization_cp_10001 => GetResourceString("Globalization_cp_10001");

	internal static string Globalization_cp_10002 => GetResourceString("Globalization_cp_10002");

	internal static string Globalization_cp_10003 => GetResourceString("Globalization_cp_10003");

	internal static string Globalization_cp_10004 => GetResourceString("Globalization_cp_10004");

	internal static string Globalization_cp_10005 => GetResourceString("Globalization_cp_10005");

	internal static string Globalization_cp_10006 => GetResourceString("Globalization_cp_10006");

	internal static string Globalization_cp_10007 => GetResourceString("Globalization_cp_10007");

	internal static string Globalization_cp_10008 => GetResourceString("Globalization_cp_10008");

	internal static string Globalization_cp_10010 => GetResourceString("Globalization_cp_10010");

	internal static string Globalization_cp_10017 => GetResourceString("Globalization_cp_10017");

	internal static string Globalization_cp_10021 => GetResourceString("Globalization_cp_10021");

	internal static string Globalization_cp_10029 => GetResourceString("Globalization_cp_10029");

	internal static string Globalization_cp_10079 => GetResourceString("Globalization_cp_10079");

	internal static string Globalization_cp_10081 => GetResourceString("Globalization_cp_10081");

	internal static string Globalization_cp_10082 => GetResourceString("Globalization_cp_10082");

	internal static string Globalization_cp_20000 => GetResourceString("Globalization_cp_20000");

	internal static string Globalization_cp_20001 => GetResourceString("Globalization_cp_20001");

	internal static string Globalization_cp_20002 => GetResourceString("Globalization_cp_20002");

	internal static string Globalization_cp_20003 => GetResourceString("Globalization_cp_20003");

	internal static string Globalization_cp_20004 => GetResourceString("Globalization_cp_20004");

	internal static string Globalization_cp_20005 => GetResourceString("Globalization_cp_20005");

	internal static string Globalization_cp_20105 => GetResourceString("Globalization_cp_20105");

	internal static string Globalization_cp_20106 => GetResourceString("Globalization_cp_20106");

	internal static string Globalization_cp_20107 => GetResourceString("Globalization_cp_20107");

	internal static string Globalization_cp_20108 => GetResourceString("Globalization_cp_20108");

	internal static string Globalization_cp_20261 => GetResourceString("Globalization_cp_20261");

	internal static string Globalization_cp_20269 => GetResourceString("Globalization_cp_20269");

	internal static string Globalization_cp_20273 => GetResourceString("Globalization_cp_20273");

	internal static string Globalization_cp_20277 => GetResourceString("Globalization_cp_20277");

	internal static string Globalization_cp_20278 => GetResourceString("Globalization_cp_20278");

	internal static string Globalization_cp_20280 => GetResourceString("Globalization_cp_20280");

	internal static string Globalization_cp_20284 => GetResourceString("Globalization_cp_20284");

	internal static string Globalization_cp_20285 => GetResourceString("Globalization_cp_20285");

	internal static string Globalization_cp_20290 => GetResourceString("Globalization_cp_20290");

	internal static string Globalization_cp_20297 => GetResourceString("Globalization_cp_20297");

	internal static string Globalization_cp_20420 => GetResourceString("Globalization_cp_20420");

	internal static string Globalization_cp_20423 => GetResourceString("Globalization_cp_20423");

	internal static string Globalization_cp_20424 => GetResourceString("Globalization_cp_20424");

	internal static string Globalization_cp_20833 => GetResourceString("Globalization_cp_20833");

	internal static string Globalization_cp_20838 => GetResourceString("Globalization_cp_20838");

	internal static string Globalization_cp_20866 => GetResourceString("Globalization_cp_20866");

	internal static string Globalization_cp_20871 => GetResourceString("Globalization_cp_20871");

	internal static string Globalization_cp_20880 => GetResourceString("Globalization_cp_20880");

	internal static string Globalization_cp_20905 => GetResourceString("Globalization_cp_20905");

	internal static string Globalization_cp_20924 => GetResourceString("Globalization_cp_20924");

	internal static string Globalization_cp_20932 => GetResourceString("Globalization_cp_20932");

	internal static string Globalization_cp_20936 => GetResourceString("Globalization_cp_20936");

	internal static string Globalization_cp_20949 => GetResourceString("Globalization_cp_20949");

	internal static string Globalization_cp_21025 => GetResourceString("Globalization_cp_21025");

	internal static string Globalization_cp_21027 => GetResourceString("Globalization_cp_21027");

	internal static string Globalization_cp_21866 => GetResourceString("Globalization_cp_21866");

	internal static string Globalization_cp_28592 => GetResourceString("Globalization_cp_28592");

	internal static string Globalization_cp_28593 => GetResourceString("Globalization_cp_28593");

	internal static string Globalization_cp_28594 => GetResourceString("Globalization_cp_28594");

	internal static string Globalization_cp_28595 => GetResourceString("Globalization_cp_28595");

	internal static string Globalization_cp_28596 => GetResourceString("Globalization_cp_28596");

	internal static string Globalization_cp_28597 => GetResourceString("Globalization_cp_28597");

	internal static string Globalization_cp_28598 => GetResourceString("Globalization_cp_28598");

	internal static string Globalization_cp_28599 => GetResourceString("Globalization_cp_28599");

	internal static string Globalization_cp_28603 => GetResourceString("Globalization_cp_28603");

	internal static string Globalization_cp_28605 => GetResourceString("Globalization_cp_28605");

	internal static string Globalization_cp_29001 => GetResourceString("Globalization_cp_29001");

	internal static string Globalization_cp_38598 => GetResourceString("Globalization_cp_38598");

	internal static string Globalization_cp_50000 => GetResourceString("Globalization_cp_50000");

	internal static string Globalization_cp_50220 => GetResourceString("Globalization_cp_50220");

	internal static string Globalization_cp_50221 => GetResourceString("Globalization_cp_50221");

	internal static string Globalization_cp_50222 => GetResourceString("Globalization_cp_50222");

	internal static string Globalization_cp_50225 => GetResourceString("Globalization_cp_50225");

	internal static string Globalization_cp_50227 => GetResourceString("Globalization_cp_50227");

	internal static string Globalization_cp_50229 => GetResourceString("Globalization_cp_50229");

	internal static string Globalization_cp_50930 => GetResourceString("Globalization_cp_50930");

	internal static string Globalization_cp_50931 => GetResourceString("Globalization_cp_50931");

	internal static string Globalization_cp_50933 => GetResourceString("Globalization_cp_50933");

	internal static string Globalization_cp_50935 => GetResourceString("Globalization_cp_50935");

	internal static string Globalization_cp_50937 => GetResourceString("Globalization_cp_50937");

	internal static string Globalization_cp_50939 => GetResourceString("Globalization_cp_50939");

	internal static string Globalization_cp_51932 => GetResourceString("Globalization_cp_51932");

	internal static string Globalization_cp_51936 => GetResourceString("Globalization_cp_51936");

	internal static string Globalization_cp_51949 => GetResourceString("Globalization_cp_51949");

	internal static string Globalization_cp_52936 => GetResourceString("Globalization_cp_52936");

	internal static string Globalization_cp_54936 => GetResourceString("Globalization_cp_54936");

	internal static string Globalization_cp_57002 => GetResourceString("Globalization_cp_57002");

	internal static string Globalization_cp_57003 => GetResourceString("Globalization_cp_57003");

	internal static string Globalization_cp_57004 => GetResourceString("Globalization_cp_57004");

	internal static string Globalization_cp_57005 => GetResourceString("Globalization_cp_57005");

	internal static string Globalization_cp_57006 => GetResourceString("Globalization_cp_57006");

	internal static string Globalization_cp_57007 => GetResourceString("Globalization_cp_57007");

	internal static string Globalization_cp_57008 => GetResourceString("Globalization_cp_57008");

	internal static string Globalization_cp_57009 => GetResourceString("Globalization_cp_57009");

	internal static string Globalization_cp_57010 => GetResourceString("Globalization_cp_57010");

	internal static string Globalization_cp_57011 => GetResourceString("Globalization_cp_57011");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}
}
