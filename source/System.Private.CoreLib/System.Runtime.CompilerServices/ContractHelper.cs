#define DEBUG
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Runtime.CompilerServices;

public static class ContractHelper
{
	internal static event EventHandler<ContractFailedEventArgs>? InternalContractFailed;

	[DebuggerNonUserCode]
	public static string? RaiseContractFailedEvent(ContractFailureKind failureKind, string? userMessage, string? conditionText, Exception? innerException)
	{
		if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), "failureKind");
		}
		string text = "contract failed.";
		ContractFailedEventArgs contractFailedEventArgs = null;
		string result;
		try
		{
			text = GetDisplayMessage(failureKind, userMessage, conditionText);
			EventHandler<ContractFailedEventArgs> internalContractFailed = ContractHelper.InternalContractFailed;
			if (internalContractFailed != null)
			{
				contractFailedEventArgs = new ContractFailedEventArgs(failureKind, text, conditionText, innerException);
				Delegate[] invocationList = internalContractFailed.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					EventHandler<ContractFailedEventArgs> eventHandler = (EventHandler<ContractFailedEventArgs>)invocationList[i];
					try
					{
						eventHandler(null, contractFailedEventArgs);
					}
					catch (Exception thrownDuringHandler)
					{
						contractFailedEventArgs.thrownDuringHandler = thrownDuringHandler;
						contractFailedEventArgs.SetUnwind();
					}
				}
				if (contractFailedEventArgs.Unwind)
				{
					if (innerException == null)
					{
						innerException = contractFailedEventArgs.thrownDuringHandler;
					}
					throw new ContractException(failureKind, text, userMessage, conditionText, innerException);
				}
			}
		}
		finally
		{
			result = ((contractFailedEventArgs == null || !contractFailedEventArgs.Handled) ? text : null);
		}
		return result;
	}

	[DebuggerNonUserCode]
	public static void TriggerFailure(ContractFailureKind kind, string? displayMessage, string? userMessage, string? conditionText, Exception? innerException)
	{
		if (string.IsNullOrEmpty(displayMessage))
		{
			displayMessage = GetDisplayMessage(kind, userMessage, conditionText);
		}
		Debug.ContractFailure(displayMessage, string.Empty, GetFailureMessage(kind, null));
	}

	private static string GetFailureMessage(ContractFailureKind failureKind, string conditionText)
	{
		bool flag = !string.IsNullOrEmpty(conditionText);
		switch (failureKind)
		{
		case ContractFailureKind.Assert:
			if (!flag)
			{
				return SR.AssertionFailed;
			}
			return SR.Format(SR.AssertionFailed_Cnd, conditionText);
		case ContractFailureKind.Assume:
			if (!flag)
			{
				return SR.AssumptionFailed;
			}
			return SR.Format(SR.AssumptionFailed_Cnd, conditionText);
		case ContractFailureKind.Precondition:
			if (!flag)
			{
				return SR.PreconditionFailed;
			}
			return SR.Format(SR.PreconditionFailed_Cnd, conditionText);
		case ContractFailureKind.Postcondition:
			if (!flag)
			{
				return SR.PostconditionFailed;
			}
			return SR.Format(SR.PostconditionFailed_Cnd, conditionText);
		case ContractFailureKind.Invariant:
			if (!flag)
			{
				return SR.InvariantFailed;
			}
			return SR.Format(SR.InvariantFailed_Cnd, conditionText);
		case ContractFailureKind.PostconditionOnException:
			if (!flag)
			{
				return SR.PostconditionOnExceptionFailed;
			}
			return SR.Format(SR.PostconditionOnExceptionFailed_Cnd, conditionText);
		default:
			Contract.Assume(condition: false, "Unreachable code");
			return SR.AssumptionFailed;
		}
	}

	private static string GetDisplayMessage(ContractFailureKind failureKind, string userMessage, string conditionText)
	{
		string text = (string.IsNullOrEmpty(conditionText) ? "" : GetFailureMessage(failureKind, conditionText));
		if (!string.IsNullOrEmpty(userMessage))
		{
			return text + "  " + userMessage;
		}
		return text;
	}
}
