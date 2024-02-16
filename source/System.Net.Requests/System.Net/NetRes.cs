using System.Globalization;

namespace System.Net;

internal static class NetRes
{
	public static string GetWebStatusCodeString(FtpStatusCode statusCode, string statusDescription)
	{
		int num = (int)statusCode;
		string text = "(" + num.ToString(NumberFormatInfo.InvariantInfo) + ")";
		string text2 = null;
		switch (statusCode)
		{
		case FtpStatusCode.ServiceNotAvailable:
			text2 = System.SR.net_ftpstatuscode_ServiceNotAvailable;
			break;
		case FtpStatusCode.CantOpenData:
			text2 = System.SR.net_ftpstatuscode_CantOpenData;
			break;
		case FtpStatusCode.ConnectionClosed:
			text2 = System.SR.net_ftpstatuscode_ConnectionClosed;
			break;
		case FtpStatusCode.ActionNotTakenFileUnavailableOrBusy:
			text2 = System.SR.net_ftpstatuscode_ActionNotTakenFileUnavailableOrBusy;
			break;
		case FtpStatusCode.ActionAbortedLocalProcessingError:
			text2 = System.SR.net_ftpstatuscode_ActionAbortedLocalProcessingError;
			break;
		case FtpStatusCode.ActionNotTakenInsufficientSpace:
			text2 = System.SR.net_ftpstatuscode_ActionNotTakenInsufficientSpace;
			break;
		case FtpStatusCode.CommandSyntaxError:
			text2 = System.SR.net_ftpstatuscode_CommandSyntaxError;
			break;
		case FtpStatusCode.ArgumentSyntaxError:
			text2 = System.SR.net_ftpstatuscode_ArgumentSyntaxError;
			break;
		case FtpStatusCode.CommandNotImplemented:
			text2 = System.SR.net_ftpstatuscode_CommandNotImplemented;
			break;
		case FtpStatusCode.BadCommandSequence:
			text2 = System.SR.net_ftpstatuscode_BadCommandSequence;
			break;
		case FtpStatusCode.NotLoggedIn:
			text2 = System.SR.net_ftpstatuscode_NotLoggedIn;
			break;
		case FtpStatusCode.AccountNeeded:
			text2 = System.SR.net_ftpstatuscode_AccountNeeded;
			break;
		case FtpStatusCode.ActionNotTakenFileUnavailable:
			text2 = System.SR.net_ftpstatuscode_ActionNotTakenFileUnavailable;
			break;
		case FtpStatusCode.ActionAbortedUnknownPageType:
			text2 = System.SR.net_ftpstatuscode_ActionAbortedUnknownPageType;
			break;
		case FtpStatusCode.FileActionAborted:
			text2 = System.SR.net_ftpstatuscode_FileActionAborted;
			break;
		case FtpStatusCode.ActionNotTakenFilenameNotAllowed:
			text2 = System.SR.net_ftpstatuscode_ActionNotTakenFilenameNotAllowed;
			break;
		}
		if (text2 != null && text2.Length > 0)
		{
			text = text + " " + text2;
		}
		else if (statusDescription != null && statusDescription.Length > 0)
		{
			text = text + " " + statusDescription;
		}
		return text;
	}
}
