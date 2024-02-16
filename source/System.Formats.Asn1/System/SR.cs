using System.Resources;
using FxResources.System.Formats.Asn1;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

	internal static string Argument_EnumeratedValueRequiresNonFlagsEnum => GetResourceString("Argument_EnumeratedValueRequiresNonFlagsEnum");

	internal static string Argument_EnumeratedValueBackingTypeNotSupported => GetResourceString("Argument_EnumeratedValueBackingTypeNotSupported");

	internal static string Argument_InvalidOidValue => GetResourceString("Argument_InvalidOidValue");

	internal static string Argument_NamedBitListRequiresFlagsEnum => GetResourceString("Argument_NamedBitListRequiresFlagsEnum");

	internal static string Argument_SourceOverlapsDestination => GetResourceString("Argument_SourceOverlapsDestination");

	internal static string Argument_Tag_NotCharacterString => GetResourceString("Argument_Tag_NotCharacterString");

	internal static string Argument_IntegerCannotBeEmpty => GetResourceString("Argument_IntegerCannotBeEmpty");

	internal static string Argument_IntegerRedundantByte => GetResourceString("Argument_IntegerRedundantByte");

	internal static string Argument_UniversalValueIsFixed => GetResourceString("Argument_UniversalValueIsFixed");

	internal static string Argument_UnusedBitCountMustBeZero => GetResourceString("Argument_UnusedBitCountMustBeZero");

	internal static string Argument_UnusedBitCountRange => GetResourceString("Argument_UnusedBitCountRange");

	internal static string Argument_UnusedBitWasSet => GetResourceString("Argument_UnusedBitWasSet");

	internal static string Argument_WriteEncodedValue_OneValueAtATime => GetResourceString("Argument_WriteEncodedValue_OneValueAtATime");

	internal static string AsnWriter_EncodeUnbalancedStack => GetResourceString("AsnWriter_EncodeUnbalancedStack");

	internal static string AsnWriter_PopWrongTag => GetResourceString("AsnWriter_PopWrongTag");

	internal static string ContentException_CerRequiresIndefiniteLength => GetResourceString("ContentException_CerRequiresIndefiniteLength");

	internal static string ContentException_ConstructedEncodingRequired => GetResourceString("ContentException_ConstructedEncodingRequired");

	internal static string ContentException_DefaultMessage => GetResourceString("ContentException_DefaultMessage");

	internal static string ContentException_EnumeratedValueTooBig => GetResourceString("ContentException_EnumeratedValueTooBig");

	internal static string ContentException_InvalidUnderCer_TryBerOrDer => GetResourceString("ContentException_InvalidUnderCer_TryBerOrDer");

	internal static string ContentException_InvalidUnderCerOrDer_TryBer => GetResourceString("ContentException_InvalidUnderCerOrDer_TryBer");

	internal static string ContentException_InvalidUnderDer_TryBerOrCer => GetResourceString("ContentException_InvalidUnderDer_TryBerOrCer");

	internal static string ContentException_InvalidTag => GetResourceString("ContentException_InvalidTag");

	internal static string ContentException_LengthExceedsPayload => GetResourceString("ContentException_LengthExceedsPayload");

	internal static string ContentException_LengthRuleSetConstraint => GetResourceString("ContentException_LengthRuleSetConstraint");

	internal static string ContentException_LengthTooBig => GetResourceString("ContentException_LengthTooBig");

	internal static string ContentException_NamedBitListValueTooBig => GetResourceString("ContentException_NamedBitListValueTooBig");

	internal static string ContentException_PrimitiveEncodingRequired => GetResourceString("ContentException_PrimitiveEncodingRequired");

	internal static string ContentException_SetOfNotSorted => GetResourceString("ContentException_SetOfNotSorted");

	internal static string ContentException_TooMuchData => GetResourceString("ContentException_TooMuchData");

	internal static string ContentException_WrongTag => GetResourceString("ContentException_WrongTag");

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

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}
}
