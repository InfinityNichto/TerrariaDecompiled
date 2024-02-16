using System.Resources;
using FxResources.System.IO.MemoryMappedFiles;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string IO_FileNotFound => GetResourceString("IO_FileNotFound");

	internal static string IO_FileNotFound_FileName => GetResourceString("IO_FileNotFound_FileName");

	internal static string IO_AlreadyExists_Name => GetResourceString("IO_AlreadyExists_Name");

	internal static string IO_FileExists_Name => GetResourceString("IO_FileExists_Name");

	internal static string IO_SharingViolation_File => GetResourceString("IO_SharingViolation_File");

	internal static string IO_SharingViolation_NoFileName => GetResourceString("IO_SharingViolation_NoFileName");

	internal static string IO_PathNotFound_Path => GetResourceString("IO_PathNotFound_Path");

	internal static string IO_PathNotFound_NoPathName => GetResourceString("IO_PathNotFound_NoPathName");

	internal static string IO_PathTooLong => GetResourceString("IO_PathTooLong");

	internal static string UnauthorizedAccess_IODenied_Path => GetResourceString("UnauthorizedAccess_IODenied_Path");

	internal static string UnauthorizedAccess_IODenied_NoPathName => GetResourceString("UnauthorizedAccess_IODenied_NoPathName");

	internal static string Argument_MapNameEmptyString => GetResourceString("Argument_MapNameEmptyString");

	internal static string Argument_EmptyFile => GetResourceString("Argument_EmptyFile");

	internal static string Argument_NewMMFWriteAccessNotAllowed => GetResourceString("Argument_NewMMFWriteAccessNotAllowed");

	internal static string Argument_ReadAccessWithLargeCapacity => GetResourceString("Argument_ReadAccessWithLargeCapacity");

	internal static string Argument_NewMMFAppendModeNotAllowed => GetResourceString("Argument_NewMMFAppendModeNotAllowed");

	internal static string Argument_NewMMFTruncateModeNotAllowed => GetResourceString("Argument_NewMMFTruncateModeNotAllowed");

	internal static string ArgumentNull_MapName => GetResourceString("ArgumentNull_MapName");

	internal static string ArgumentNull_FileStream => GetResourceString("ArgumentNull_FileStream");

	internal static string ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed => GetResourceString("ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed");

	internal static string ArgumentOutOfRange_NeedPositiveNumber => GetResourceString("ArgumentOutOfRange_NeedPositiveNumber");

	internal static string ArgumentOutOfRange_PositiveOrDefaultCapacityRequired => GetResourceString("ArgumentOutOfRange_PositiveOrDefaultCapacityRequired");

	internal static string ArgumentOutOfRange_PositiveOrDefaultSizeRequired => GetResourceString("ArgumentOutOfRange_PositiveOrDefaultSizeRequired");

	internal static string ArgumentOutOfRange_CapacityGEFileSizeRequired => GetResourceString("ArgumentOutOfRange_CapacityGEFileSizeRequired");

	internal static string IO_NotEnoughMemory => GetResourceString("IO_NotEnoughMemory");

	internal static string InvalidOperation_CantCreateFileMapping => GetResourceString("InvalidOperation_CantCreateFileMapping");

	internal static string NotSupported_MMViewStreamsFixedLength => GetResourceString("NotSupported_MMViewStreamsFixedLength");

	internal static string ObjectDisposed_ViewAccessorClosed => GetResourceString("ObjectDisposed_ViewAccessorClosed");

	internal static string ObjectDisposed_StreamIsClosed => GetResourceString("ObjectDisposed_StreamIsClosed");

	internal static string IO_PathTooLong_Path => GetResourceString("IO_PathTooLong_Path");

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
}
