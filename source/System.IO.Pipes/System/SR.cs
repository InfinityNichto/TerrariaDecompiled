using System.Resources;
using FxResources.System.IO.Pipes;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_NeedValidPipeAccessRights => GetResourceString("ArgumentOutOfRange_NeedValidPipeAccessRights");

	internal static string Argument_NeedNonemptyPipeName => GetResourceString("Argument_NeedNonemptyPipeName");

	internal static string Argument_NonContainerInvalidAnyFlag => GetResourceString("Argument_NonContainerInvalidAnyFlag");

	internal static string Argument_EmptyServerName => GetResourceString("Argument_EmptyServerName");

	internal static string Argument_InvalidHandle => GetResourceString("Argument_InvalidHandle");

	internal static string ArgumentNull_ServerName => GetResourceString("ArgumentNull_ServerName");

	internal static string ArgumentOutOfRange_AnonymousReserved => GetResourceString("ArgumentOutOfRange_AnonymousReserved");

	internal static string ArgumentOutOfRange_TransmissionModeByteOrMsg => GetResourceString("ArgumentOutOfRange_TransmissionModeByteOrMsg");

	internal static string ArgumentOutOfRange_DirectionModeInOutOrInOut => GetResourceString("ArgumentOutOfRange_DirectionModeInOutOrInOut");

	internal static string ArgumentOutOfRange_ImpersonationInvalid => GetResourceString("ArgumentOutOfRange_ImpersonationInvalid");

	internal static string ArgumentOutOfRange_OptionsInvalid => GetResourceString("ArgumentOutOfRange_OptionsInvalid");

	internal static string ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable => GetResourceString("ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable");

	internal static string ArgumentOutOfRange_InvalidTimeout => GetResourceString("ArgumentOutOfRange_InvalidTimeout");

	internal static string ArgumentOutOfRange_MaxNumServerInstances => GetResourceString("ArgumentOutOfRange_MaxNumServerInstances");

	internal static string InvalidOperation_PipeNotYetConnected => GetResourceString("InvalidOperation_PipeNotYetConnected");

	internal static string InvalidOperation_PipeDisconnected => GetResourceString("InvalidOperation_PipeDisconnected");

	internal static string InvalidOperation_PipeHandleNotSet => GetResourceString("InvalidOperation_PipeHandleNotSet");

	internal static string InvalidOperation_PipeReadModeNotMessage => GetResourceString("InvalidOperation_PipeReadModeNotMessage");

	internal static string InvalidOperation_PipeAlreadyConnected => GetResourceString("InvalidOperation_PipeAlreadyConnected");

	internal static string InvalidOperation_PipeAlreadyDisconnected => GetResourceString("InvalidOperation_PipeAlreadyDisconnected");

	internal static string IO_EOF_ReadBeyondEOF => GetResourceString("IO_EOF_ReadBeyondEOF");

	internal static string IO_FileNotFound => GetResourceString("IO_FileNotFound");

	internal static string IO_FileNotFound_FileName => GetResourceString("IO_FileNotFound_FileName");

	internal static string IO_AlreadyExists_Name => GetResourceString("IO_AlreadyExists_Name");

	internal static string IO_FileExists_Name => GetResourceString("IO_FileExists_Name");

	internal static string IO_IO_PipeBroken => GetResourceString("IO_IO_PipeBroken");

	internal static string IO_OperationAborted_Unexpected => GetResourceString("IO_OperationAborted_Unexpected");

	internal static string IO_SharingViolation_File => GetResourceString("IO_SharingViolation_File");

	internal static string IO_SharingViolation_NoFileName => GetResourceString("IO_SharingViolation_NoFileName");

	internal static string IO_PipeBroken => GetResourceString("IO_PipeBroken");

	internal static string IO_InvalidPipeHandle => GetResourceString("IO_InvalidPipeHandle");

	internal static string IO_PathNotFound_Path => GetResourceString("IO_PathNotFound_Path");

	internal static string IO_PathNotFound_NoPathName => GetResourceString("IO_PathNotFound_NoPathName");

	internal static string IO_PathTooLong => GetResourceString("IO_PathTooLong");

	internal static string NotSupported_UnreadableStream => GetResourceString("NotSupported_UnreadableStream");

	internal static string NotSupported_UnseekableStream => GetResourceString("NotSupported_UnseekableStream");

	internal static string NotSupported_UnwritableStream => GetResourceString("NotSupported_UnwritableStream");

	internal static string NotSupported_AnonymousPipeUnidirectional => GetResourceString("NotSupported_AnonymousPipeUnidirectional");

	internal static string NotSupported_AnonymousPipeMessagesNotSupported => GetResourceString("NotSupported_AnonymousPipeMessagesNotSupported");

	internal static string ObjectDisposed_PipeClosed => GetResourceString("ObjectDisposed_PipeClosed");

	internal static string UnauthorizedAccess_IODenied_Path => GetResourceString("UnauthorizedAccess_IODenied_Path");

	internal static string UnauthorizedAccess_IODenied_NoPathName => GetResourceString("UnauthorizedAccess_IODenied_NoPathName");

	internal static string IO_PathTooLong_Path => GetResourceString("IO_PathTooLong_Path");

	internal static string UnauthorizedAccess_NotOwnedByCurrentUser => GetResourceString("UnauthorizedAccess_NotOwnedByCurrentUser");

	internal static string NotSupported_PipeSecurityIsCurrentUserOnly => GetResourceString("NotSupported_PipeSecurityIsCurrentUserOnly");

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
