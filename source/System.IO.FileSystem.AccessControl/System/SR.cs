using System.Resources;
using FxResources.System.IO.FileSystem.AccessControl;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string AccessControl_InvalidHandle => GetResourceString("AccessControl_InvalidHandle");

	internal static string Arg_MustBeIdentityReferenceType => GetResourceString("Arg_MustBeIdentityReferenceType");

	internal static string Argument_InvalidEnumValue => GetResourceString("Argument_InvalidEnumValue");

	internal static string Argument_InvalidName => GetResourceString("Argument_InvalidName");

	internal static string ArgumentOutOfRange_Enum => GetResourceString("ArgumentOutOfRange_Enum");

	internal static string ObjectDisposed_FileClosed => GetResourceString("ObjectDisposed_FileClosed");

	internal static string TypeUnrecognized_AccessControl => GetResourceString("TypeUnrecognized_AccessControl");

	internal static string InvalidOperation_RemoveFail => GetResourceString("InvalidOperation_RemoveFail");

	internal static string IO_AlreadyExists_Name => GetResourceString("IO_AlreadyExists_Name");

	internal static string IO_FileExists_Name => GetResourceString("IO_FileExists_Name");

	internal static string IO_FileNotFound => GetResourceString("IO_FileNotFound");

	internal static string IO_FileNotFound_FileName => GetResourceString("IO_FileNotFound_FileName");

	internal static string IO_PathNotFound_NoPathName => GetResourceString("IO_PathNotFound_NoPathName");

	internal static string IO_PathNotFound_Path => GetResourceString("IO_PathNotFound_Path");

	internal static string IO_PathTooLong => GetResourceString("IO_PathTooLong");

	internal static string IO_PathTooLong_Path => GetResourceString("IO_PathTooLong_Path");

	internal static string IO_SharingViolation_File => GetResourceString("IO_SharingViolation_File");

	internal static string IO_SharingViolation_NoFileName => GetResourceString("IO_SharingViolation_NoFileName");

	internal static string UnauthorizedAccess_IODenied_NoPathName => GetResourceString("UnauthorizedAccess_IODenied_NoPathName");

	internal static string UnauthorizedAccess_IODenied_Path => GetResourceString("UnauthorizedAccess_IODenied_Path");

	internal static string Arg_PathEmpty => GetResourceString("Arg_PathEmpty");

	internal static string ArgumentOutOfRange_NeedPosNum => GetResourceString("ArgumentOutOfRange_NeedPosNum");

	internal static string Argument_InvalidFileModeAndFileSystemRightsCombo => GetResourceString("Argument_InvalidFileModeAndFileSystemRightsCombo");

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
