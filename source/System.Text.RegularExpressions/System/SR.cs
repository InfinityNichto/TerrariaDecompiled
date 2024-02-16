using System.Resources;
using FxResources.System.Text.RegularExpressions;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string AlternationHasNamedCapture => GetResourceString("AlternationHasNamedCapture");

	internal static string AlternationHasComment => GetResourceString("AlternationHasComment");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string ShorthandClassInCharacterRange => GetResourceString("ShorthandClassInCharacterRange");

	internal static string BeginIndexNotNegative => GetResourceString("BeginIndexNotNegative");

	internal static string QuantifierOrCaptureGroupOutOfRange => GetResourceString("QuantifierOrCaptureGroupOutOfRange");

	internal static string CaptureGroupOfZero => GetResourceString("CaptureGroupOfZero");

	internal static string CountTooSmall => GetResourceString("CountTooSmall");

	internal static string EnumNotStarted => GetResourceString("EnumNotStarted");

	internal static string AlternationHasMalformedCondition => GetResourceString("AlternationHasMalformedCondition");

	internal static string IllegalDefaultRegexMatchTimeoutInAppDomain => GetResourceString("IllegalDefaultRegexMatchTimeoutInAppDomain");

	internal static string UnescapedEndingBackslash => GetResourceString("UnescapedEndingBackslash");

	internal static string ReversedQuantifierRange => GetResourceString("ReversedQuantifierRange");

	internal static string InvalidUnicodePropertyEscape => GetResourceString("InvalidUnicodePropertyEscape");

	internal static string InternalError_ScanRegex => GetResourceString("InternalError_ScanRegex");

	internal static string CaptureGroupNameInvalid => GetResourceString("CaptureGroupNameInvalid");

	internal static string InvalidEmptyArgument => GetResourceString("InvalidEmptyArgument");

	internal static string LengthNotNegative => GetResourceString("LengthNotNegative");

	internal static string MalformedNamedReference => GetResourceString("MalformedNamedReference");

	internal static string AlternationHasMalformedReference => GetResourceString("AlternationHasMalformedReference");

	internal static string MalformedUnicodePropertyEscape => GetResourceString("MalformedUnicodePropertyEscape");

	internal static string MakeException => GetResourceString("MakeException");

	internal static string MissingControlCharacter => GetResourceString("MissingControlCharacter");

	internal static string NestedQuantifiersNotParenthesized => GetResourceString("NestedQuantifiersNotParenthesized");

	internal static string NoResultOnFailed => GetResourceString("NoResultOnFailed");

	internal static string InsufficientClosingParentheses => GetResourceString("InsufficientClosingParentheses");

	internal static string NotSupported_ReadOnlyCollection => GetResourceString("NotSupported_ReadOnlyCollection");

	internal static string OnlyAllowedOnce => GetResourceString("OnlyAllowedOnce");

	internal static string PlatformNotSupported_CompileToAssembly => GetResourceString("PlatformNotSupported_CompileToAssembly");

	internal static string QuantifierAfterNothing => GetResourceString("QuantifierAfterNothing");

	internal static string RegexMatchTimeoutException_Occurred => GetResourceString("RegexMatchTimeoutException_Occurred");

	internal static string ReplacementError => GetResourceString("ReplacementError");

	internal static string ReversedCharacterRange => GetResourceString("ReversedCharacterRange");

	internal static string ExclusionGroupNotLast => GetResourceString("ExclusionGroupNotLast");

	internal static string InsufficientOrInvalidHexDigits => GetResourceString("InsufficientOrInvalidHexDigits");

	internal static string AlternationHasTooManyConditions => GetResourceString("AlternationHasTooManyConditions");

	internal static string InsufficientOpeningParentheses => GetResourceString("InsufficientOpeningParentheses");

	internal static string UndefinedNumberedReference => GetResourceString("UndefinedNumberedReference");

	internal static string UndefinedNamedReference => GetResourceString("UndefinedNamedReference");

	internal static string AlternationHasUndefinedReference => GetResourceString("AlternationHasUndefinedReference");

	internal static string UnexpectedOpcode => GetResourceString("UnexpectedOpcode");

	internal static string UnrecognizedUnicodeProperty => GetResourceString("UnrecognizedUnicodeProperty");

	internal static string UnrecognizedControlCharacter => GetResourceString("UnrecognizedControlCharacter");

	internal static string UnrecognizedEscape => GetResourceString("UnrecognizedEscape");

	internal static string InvalidGroupingConstruct => GetResourceString("InvalidGroupingConstruct");

	internal static string UnterminatedBracket => GetResourceString("UnterminatedBracket");

	internal static string UnterminatedComment => GetResourceString("UnterminatedComment");

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

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}
}
