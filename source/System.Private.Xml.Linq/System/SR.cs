using System.Resources;
using FxResources.System.Private.Xml.Linq;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Argument_AddAttribute => GetResourceString("Argument_AddAttribute");

	internal static string Argument_AddNode => GetResourceString("Argument_AddNode");

	internal static string Argument_AddNonWhitespace => GetResourceString("Argument_AddNonWhitespace");

	internal static string Argument_ConvertToString => GetResourceString("Argument_ConvertToString");

	internal static string Argument_InvalidExpandedName => GetResourceString("Argument_InvalidExpandedName");

	internal static string Argument_InvalidPIName => GetResourceString("Argument_InvalidPIName");

	internal static string Argument_InvalidPrefix => GetResourceString("Argument_InvalidPrefix");

	internal static string Argument_MustBeDerivedFrom => GetResourceString("Argument_MustBeDerivedFrom");

	internal static string Argument_NamespaceDeclarationPrefixed => GetResourceString("Argument_NamespaceDeclarationPrefixed");

	internal static string Argument_NamespaceDeclarationXml => GetResourceString("Argument_NamespaceDeclarationXml");

	internal static string Argument_NamespaceDeclarationXmlns => GetResourceString("Argument_NamespaceDeclarationXmlns");

	internal static string Argument_XObjectValue => GetResourceString("Argument_XObjectValue");

	internal static string InvalidOperation_DeserializeInstance => GetResourceString("InvalidOperation_DeserializeInstance");

	internal static string InvalidOperation_DocumentStructure => GetResourceString("InvalidOperation_DocumentStructure");

	internal static string InvalidOperation_DuplicateAttribute => GetResourceString("InvalidOperation_DuplicateAttribute");

	internal static string InvalidOperation_ExpectedEndOfFile => GetResourceString("InvalidOperation_ExpectedEndOfFile");

	internal static string InvalidOperation_ExpectedInteractive => GetResourceString("InvalidOperation_ExpectedInteractive");

	internal static string InvalidOperation_ExpectedNodeType => GetResourceString("InvalidOperation_ExpectedNodeType");

	internal static string InvalidOperation_ExternalCode => GetResourceString("InvalidOperation_ExternalCode");

	internal static string InvalidOperation_MissingAncestor => GetResourceString("InvalidOperation_MissingAncestor");

	internal static string InvalidOperation_MissingParent => GetResourceString("InvalidOperation_MissingParent");

	internal static string InvalidOperation_MissingRoot => GetResourceString("InvalidOperation_MissingRoot");

	internal static string InvalidOperation_UnexpectedNodeType => GetResourceString("InvalidOperation_UnexpectedNodeType");

	internal static string InvalidOperation_UnresolvedEntityReference => GetResourceString("InvalidOperation_UnresolvedEntityReference");

	internal static string InvalidOperation_WriteAttribute => GetResourceString("InvalidOperation_WriteAttribute");

	internal static string NotSupported_WriteBase64 => GetResourceString("NotSupported_WriteBase64");

	internal static string NotSupported_WriteEntityRef => GetResourceString("NotSupported_WriteEntityRef");

	internal static string Argument_CreateNavigator => GetResourceString("Argument_CreateNavigator");

	internal static string InvalidOperation_BadNodeType => GetResourceString("InvalidOperation_BadNodeType");

	internal static string InvalidOperation_UnexpectedEvaluation => GetResourceString("InvalidOperation_UnexpectedEvaluation");

	internal static string NotSupported_MoveToId => GetResourceString("NotSupported_MoveToId");

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
