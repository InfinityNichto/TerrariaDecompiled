using System.Resources;
using FxResources.System.Threading;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string CountdownEvent_Increment_AlreadyZero => GetResourceString("CountdownEvent_Increment_AlreadyZero");

	internal static string CountdownEvent_Increment_AlreadyMax => GetResourceString("CountdownEvent_Increment_AlreadyMax");

	internal static string CountdownEvent_Decrement_BelowZero => GetResourceString("CountdownEvent_Decrement_BelowZero");

	internal static string Common_OperationCanceled => GetResourceString("Common_OperationCanceled");

	internal static string Barrier_Dispose => GetResourceString("Barrier_Dispose");

	internal static string Barrier_SignalAndWait_InvalidOperation_ZeroTotal => GetResourceString("Barrier_SignalAndWait_InvalidOperation_ZeroTotal");

	internal static string Barrier_SignalAndWait_ArgumentOutOfRange => GetResourceString("Barrier_SignalAndWait_ArgumentOutOfRange");

	internal static string Barrier_RemoveParticipants_InvalidOperation => GetResourceString("Barrier_RemoveParticipants_InvalidOperation");

	internal static string Barrier_RemoveParticipants_ArgumentOutOfRange => GetResourceString("Barrier_RemoveParticipants_ArgumentOutOfRange");

	internal static string Barrier_RemoveParticipants_NonPositive_ArgumentOutOfRange => GetResourceString("Barrier_RemoveParticipants_NonPositive_ArgumentOutOfRange");

	internal static string Barrier_InvalidOperation_CalledFromPHA => GetResourceString("Barrier_InvalidOperation_CalledFromPHA");

	internal static string Barrier_AddParticipants_NonPositive_ArgumentOutOfRange => GetResourceString("Barrier_AddParticipants_NonPositive_ArgumentOutOfRange");

	internal static string Barrier_SignalAndWait_InvalidOperation_ThreadsExceeded => GetResourceString("Barrier_SignalAndWait_InvalidOperation_ThreadsExceeded");

	internal static string BarrierPostPhaseException => GetResourceString("BarrierPostPhaseException");

	internal static string Barrier_ctor_ArgumentOutOfRange => GetResourceString("Barrier_ctor_ArgumentOutOfRange");

	internal static string Barrier_AddParticipants_Overflow_ArgumentOutOfRange => GetResourceString("Barrier_AddParticipants_Overflow_ArgumentOutOfRange");

	internal static string Overflow_UInt16 => GetResourceString("Overflow_UInt16");

	internal static string ReaderWriterLock_Timeout => GetResourceString("ReaderWriterLock_Timeout");

	internal static string ArgumentOutOfRange_TimeoutMilliseconds => GetResourceString("ArgumentOutOfRange_TimeoutMilliseconds");

	internal static string ReaderWriterLock_NotOwner => GetResourceString("ReaderWriterLock_NotOwner");

	internal static string ExceptionFromHResult => GetResourceString("ExceptionFromHResult");

	internal static string ReaderWriterLock_InvalidLockCookie => GetResourceString("ReaderWriterLock_InvalidLockCookie");

	internal static string ReaderWriterLock_RestoreLockWithOwnedLocks => GetResourceString("ReaderWriterLock_RestoreLockWithOwnedLocks");

	internal static string HostExecutionContextManager_InvalidOperation_NotNewCaptureContext => GetResourceString("HostExecutionContextManager_InvalidOperation_NotNewCaptureContext");

	internal static string HostExecutionContextManager_InvalidOperation_CannotOverrideSetWithoutRevert => GetResourceString("HostExecutionContextManager_InvalidOperation_CannotOverrideSetWithoutRevert");

	internal static string HostExecutionContextManager_InvalidOperation_CannotUseSwitcherOtherThread => GetResourceString("HostExecutionContextManager_InvalidOperation_CannotUseSwitcherOtherThread");

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
