using System.Resources;
using FxResources.System.Diagnostics.Process;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string NoAssociatedProcess => GetResourceString("NoAssociatedProcess");

	internal static string ProcessIdRequired => GetResourceString("ProcessIdRequired");

	internal static string NotSupportedRemote => GetResourceString("NotSupportedRemote");

	internal static string NoProcessInfo => GetResourceString("NoProcessInfo");

	internal static string WaitTillExit => GetResourceString("WaitTillExit");

	internal static string NoProcessHandle => GetResourceString("NoProcessHandle");

	internal static string MissingProccess => GetResourceString("MissingProccess");

	internal static string BadMinWorkset => GetResourceString("BadMinWorkset");

	internal static string BadMaxWorkset => GetResourceString("BadMaxWorkset");

	internal static string ProcessHasExited => GetResourceString("ProcessHasExited");

	internal static string ProcessHasExitedNoId => GetResourceString("ProcessHasExitedNoId");

	internal static string ThreadExited => GetResourceString("ThreadExited");

	internal static string ProcessDisabled => GetResourceString("ProcessDisabled");

	internal static string WaitReasonUnavailable => GetResourceString("WaitReasonUnavailable");

	internal static string NotSupportedRemoteThread => GetResourceString("NotSupportedRemoteThread");

	internal static string CouldntConnectToRemoteMachine => GetResourceString("CouldntConnectToRemoteMachine");

	internal static string CouldntGetProcessInfos => GetResourceString("CouldntGetProcessInfos");

	internal static string InputIdleUnkownError => GetResourceString("InputIdleUnkownError");

	internal static string FileNameMissing => GetResourceString("FileNameMissing");

	internal static string EnumProcessModuleFailed => GetResourceString("EnumProcessModuleFailed");

	internal static string EnumProcessModuleFailedDueToWow => GetResourceString("EnumProcessModuleFailedDueToWow");

	internal static string NoAsyncOperation => GetResourceString("NoAsyncOperation");

	internal static string InvalidApplication => GetResourceString("InvalidApplication");

	internal static string ErrorStartingProcess => GetResourceString("ErrorStartingProcess");

	internal static string StandardOutputEncodingNotAllowed => GetResourceString("StandardOutputEncodingNotAllowed");

	internal static string StandardErrorEncodingNotAllowed => GetResourceString("StandardErrorEncodingNotAllowed");

	internal static string CantGetStandardOut => GetResourceString("CantGetStandardOut");

	internal static string CantGetStandardIn => GetResourceString("CantGetStandardIn");

	internal static string CantGetStandardError => GetResourceString("CantGetStandardError");

	internal static string CantMixSyncAsyncOperation => GetResourceString("CantMixSyncAsyncOperation");

	internal static string CantRedirectStreams => GetResourceString("CantRedirectStreams");

	internal static string PendingAsyncOperation => GetResourceString("PendingAsyncOperation");

	internal static string InvalidParameter => GetResourceString("InvalidParameter");

	internal static string CategoryHelpCorrupt => GetResourceString("CategoryHelpCorrupt");

	internal static string CounterNameCorrupt => GetResourceString("CounterNameCorrupt");

	internal static string CounterDataCorrupt => GetResourceString("CounterDataCorrupt");

	internal static string CantGetProcessStartInfo => GetResourceString("CantGetProcessStartInfo");

	internal static string CantSetProcessStartInfo => GetResourceString("CantSetProcessStartInfo");

	internal static string ProcessInformationUnavailable => GetResourceString("ProcessInformationUnavailable");

	internal static string CantSetDuplicatePassword => GetResourceString("CantSetDuplicatePassword");

	internal static string ArgumentNull_Array => GetResourceString("ArgumentNull_Array");

	internal static string ArgumentOutOfRange_IndexCountBuffer => GetResourceString("ArgumentOutOfRange_IndexCountBuffer");

	internal static string ArgumentOutOfRange_IndexCount => GetResourceString("ArgumentOutOfRange_IndexCount");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string Argument_EncodingConversionOverflowBytes => GetResourceString("Argument_EncodingConversionOverflowBytes");

	internal static string Argument_EncodingConversionOverflowChars => GetResourceString("Argument_EncodingConversionOverflowChars");

	internal static string ArgumentOutOfRange_GetByteCountOverflow => GetResourceString("ArgumentOutOfRange_GetByteCountOverflow");

	internal static string ArgumentOutOfRange_GetCharCountOverflow => GetResourceString("ArgumentOutOfRange_GetCharCountOverflow");

	internal static string Argument_InvalidCharSequenceNoIndex => GetResourceString("Argument_InvalidCharSequenceNoIndex");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string CantStartAsUser => GetResourceString("CantStartAsUser");

	internal static string CantUseEnvVars => GetResourceString("CantUseEnvVars");

	internal static string UseShellExecuteNotSupported => GetResourceString("UseShellExecuteNotSupported");

	internal static string StandardInputEncodingNotAllowed => GetResourceString("StandardInputEncodingNotAllowed");

	internal static string ArgumentAndArgumentListInitialized => GetResourceString("ArgumentAndArgumentListInitialized");

	internal static string KillEntireProcessTree_DisallowedBecauseTreeContainsCallingProcess => GetResourceString("KillEntireProcessTree_DisallowedBecauseTreeContainsCallingProcess");

	internal static string KillEntireProcessTree_TerminationIncomplete => GetResourceString("KillEntireProcessTree_TerminationIncomplete");

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
