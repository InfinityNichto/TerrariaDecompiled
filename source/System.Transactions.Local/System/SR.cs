using System.Resources;
using FxResources.System.Transactions.Local;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string AsyncFlowAndESInteropNotSupported => GetResourceString("AsyncFlowAndESInteropNotSupported");

	internal static string BadAsyncResult => GetResourceString("BadAsyncResult");

	internal static string BadResourceManagerId => GetResourceString("BadResourceManagerId");

	internal static string CannotPromoteSnapshot => GetResourceString("CannotPromoteSnapshot");

	internal static string CannotSetCurrent => GetResourceString("CannotSetCurrent");

	internal static string CurrentDelegateSet => GetResourceString("CurrentDelegateSet");

	internal static string DisposeScope => GetResourceString("DisposeScope");

	internal static string EnlistmentStateException => GetResourceString("EnlistmentStateException");

	internal static string EsNotSupported => GetResourceString("EsNotSupported");

	internal static string InternalError => GetResourceString("InternalError");

	internal static string InvalidArgument => GetResourceString("InvalidArgument");

	internal static string InvalidIPromotableSinglePhaseNotificationSpecified => GetResourceString("InvalidIPromotableSinglePhaseNotificationSpecified");

	internal static string InvalidRecoveryInformation => GetResourceString("InvalidRecoveryInformation");

	internal static string InvalidScopeThread => GetResourceString("InvalidScopeThread");

	internal static string PromotionFailed => GetResourceString("PromotionFailed");

	internal static string PromotedReturnedInvalidValue => GetResourceString("PromotedReturnedInvalidValue");

	internal static string PromotedTransactionExists => GetResourceString("PromotedTransactionExists");

	internal static string TooLate => GetResourceString("TooLate");

	internal static string TraceTransactionTimeout => GetResourceString("TraceTransactionTimeout");

	internal static string TransactionAborted => GetResourceString("TransactionAborted");

	internal static string TransactionAlreadyCompleted => GetResourceString("TransactionAlreadyCompleted");

	internal static string TransactionIndoubt => GetResourceString("TransactionIndoubt");

	internal static string TransactionManagerCommunicationException => GetResourceString("TransactionManagerCommunicationException");

	internal static string TransactionScopeComplete => GetResourceString("TransactionScopeComplete");

	internal static string TransactionScopeIncorrectCurrent => GetResourceString("TransactionScopeIncorrectCurrent");

	internal static string TransactionScopeInvalidNesting => GetResourceString("TransactionScopeInvalidNesting");

	internal static string TransactionScopeIsolationLevelDifferentFromTransaction => GetResourceString("TransactionScopeIsolationLevelDifferentFromTransaction");

	internal static string TransactionScopeTimerObjectInvalid => GetResourceString("TransactionScopeTimerObjectInvalid");

	internal static string TransactionStateException => GetResourceString("TransactionStateException");

	internal static string UnexpectedFailureOfThreadPool => GetResourceString("UnexpectedFailureOfThreadPool");

	internal static string UnexpectedTimerFailure => GetResourceString("UnexpectedTimerFailure");

	internal static string UnrecognizedRecoveryInformation => GetResourceString("UnrecognizedRecoveryInformation");

	internal static string VolEnlistNoRecoveryInfo => GetResourceString("VolEnlistNoRecoveryInfo");

	internal static string DistributedTxIDInTransactionException => GetResourceString("DistributedTxIDInTransactionException");

	internal static string PromoterTypeInvalid => GetResourceString("PromoterTypeInvalid");

	internal static string PromoterTypeUnrecognized => GetResourceString("PromoterTypeUnrecognized");

	internal static string DistributedNotSupported => GetResourceString("DistributedNotSupported");

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
