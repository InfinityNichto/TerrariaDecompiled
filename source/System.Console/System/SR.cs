using System.Resources;
using FxResources.System.Console;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_ConsoleWindowBufferSize => GetResourceString("ArgumentOutOfRange_ConsoleWindowBufferSize");

	internal static string ArgumentOutOfRange_ConsoleWindowSize_Size => GetResourceString("ArgumentOutOfRange_ConsoleWindowSize_Size");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_NeedPosNum => GetResourceString("ArgumentOutOfRange_NeedPosNum");

	internal static string ArgumentNull_Buffer => GetResourceString("ArgumentNull_Buffer");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string NotSupported_UnseekableStream => GetResourceString("NotSupported_UnseekableStream");

	internal static string ObjectDisposed_FileClosed => GetResourceString("ObjectDisposed_FileClosed");

	internal static string NotSupported_UnwritableStream => GetResourceString("NotSupported_UnwritableStream");

	internal static string NotSupported_UnreadableStream => GetResourceString("NotSupported_UnreadableStream");

	internal static string IO_AlreadyExists_Name => GetResourceString("IO_AlreadyExists_Name");

	internal static string IO_FileExists_Name => GetResourceString("IO_FileExists_Name");

	internal static string IO_FileNotFound => GetResourceString("IO_FileNotFound");

	internal static string IO_FileNotFound_FileName => GetResourceString("IO_FileNotFound_FileName");

	internal static string IO_PathNotFound_NoPathName => GetResourceString("IO_PathNotFound_NoPathName");

	internal static string IO_PathNotFound_Path => GetResourceString("IO_PathNotFound_Path");

	internal static string IO_PathTooLong => GetResourceString("IO_PathTooLong");

	internal static string UnauthorizedAccess_IODenied_NoPathName => GetResourceString("UnauthorizedAccess_IODenied_NoPathName");

	internal static string UnauthorizedAccess_IODenied_Path => GetResourceString("UnauthorizedAccess_IODenied_Path");

	internal static string IO_SharingViolation_File => GetResourceString("IO_SharingViolation_File");

	internal static string IO_SharingViolation_NoFileName => GetResourceString("IO_SharingViolation_NoFileName");

	internal static string Arg_InvalidConsoleColor => GetResourceString("Arg_InvalidConsoleColor");

	internal static string IO_NoConsole => GetResourceString("IO_NoConsole");

	internal static string InvalidOperation_ConsoleReadKeyOnFile => GetResourceString("InvalidOperation_ConsoleReadKeyOnFile");

	internal static string ArgumentOutOfRange_ConsoleKey => GetResourceString("ArgumentOutOfRange_ConsoleKey");

	internal static string ArgumentOutOfRange_ConsoleBufferBoundaries => GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries");

	internal static string ArgumentOutOfRange_ConsoleWindowPos => GetResourceString("ArgumentOutOfRange_ConsoleWindowPos");

	internal static string InvalidOperation_ConsoleKeyAvailableOnFile => GetResourceString("InvalidOperation_ConsoleKeyAvailableOnFile");

	internal static string ArgumentOutOfRange_ConsoleBufferLessThanWindowSize => GetResourceString("ArgumentOutOfRange_ConsoleBufferLessThanWindowSize");

	internal static string ArgumentOutOfRange_CursorSize => GetResourceString("ArgumentOutOfRange_CursorSize");

	internal static string ArgumentOutOfRange_BeepFrequency => GetResourceString("ArgumentOutOfRange_BeepFrequency");

	internal static string ArgumentNull_Array => GetResourceString("ArgumentNull_Array");

	internal static string ArgumentOutOfRange_IndexCountBuffer => GetResourceString("ArgumentOutOfRange_IndexCountBuffer");

	internal static string ArgumentOutOfRange_IndexCount => GetResourceString("ArgumentOutOfRange_IndexCount");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string Argument_EncodingConversionOverflowBytes => GetResourceString("Argument_EncodingConversionOverflowBytes");

	internal static string Argument_EncodingConversionOverflowChars => GetResourceString("Argument_EncodingConversionOverflowChars");

	internal static string ArgumentOutOfRange_GetByteCountOverflow => GetResourceString("ArgumentOutOfRange_GetByteCountOverflow");

	internal static string ArgumentOutOfRange_GetCharCountOverflow => GetResourceString("ArgumentOutOfRange_GetCharCountOverflow");

	internal static string Argument_InvalidCharSequenceNoIndex => GetResourceString("Argument_InvalidCharSequenceNoIndex");

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

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}
}
