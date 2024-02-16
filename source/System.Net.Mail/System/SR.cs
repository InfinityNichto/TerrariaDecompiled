using System.Resources;
using FxResources.System.Net.Mail;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_emptystringcall => GetResourceString("net_emptystringcall");

	internal static string net_io_invalidasyncresult => GetResourceString("net_io_invalidasyncresult");

	internal static string net_io_invalidendcall => GetResourceString("net_io_invalidendcall");

	internal static string net_emptystringset => GetResourceString("net_emptystringset");

	internal static string net_MethodNotImplementedException => GetResourceString("net_MethodNotImplementedException");

	internal static string event_OperationReturnedSomething => GetResourceString("event_OperationReturnedSomething");

	internal static string SSPIInvalidHandleType => GetResourceString("SSPIInvalidHandleType");

	internal static string net_auth_message_not_encrypted => GetResourceString("net_auth_message_not_encrypted");

	internal static string MailBase64InvalidCharacter => GetResourceString("MailBase64InvalidCharacter");

	internal static string net_securitypackagesupport => GetResourceString("net_securitypackagesupport");

	internal static string MailCollectionIsReadOnly => GetResourceString("MailCollectionIsReadOnly");

	internal static string MailDateInvalidFormat => GetResourceString("MailDateInvalidFormat");

	internal static string MailHeaderFieldMalformedHeader => GetResourceString("MailHeaderFieldMalformedHeader");

	internal static string MailWriterIsInContent => GetResourceString("MailWriterIsInContent");

	internal static string net_log_operation_failed_with_error => GetResourceString("net_log_operation_failed_with_error");

	internal static string MimeTransferEncodingNotSupported => GetResourceString("MimeTransferEncodingNotSupported");

	internal static string InvalidHexDigit => GetResourceString("InvalidHexDigit");

	internal static string InvalidHeaderName => GetResourceString("InvalidHeaderName");

	internal static string ContentTypeInvalid => GetResourceString("ContentTypeInvalid");

	internal static string ContentDispositionInvalid => GetResourceString("ContentDispositionInvalid");

	internal static string MimePartCantResetStream => GetResourceString("MimePartCantResetStream");

	internal static string MediaTypeInvalid => GetResourceString("MediaTypeInvalid");

	internal static string InvalidPort => GetResourceString("InvalidPort");

	internal static string MailHeaderInvalidCID => GetResourceString("MailHeaderInvalidCID");

	internal static string MailServerResponse => GetResourceString("MailServerResponse");

	internal static string net_inasync => GetResourceString("net_inasync");

	internal static string net_timeout => GetResourceString("net_timeout");

	internal static string SmtpAllRecipientsFailed => GetResourceString("SmtpAllRecipientsFailed");

	internal static string SmtpBadCommandSequence => GetResourceString("SmtpBadCommandSequence");

	internal static string SmtpClientNotPermitted => GetResourceString("SmtpClientNotPermitted");

	internal static string SmtpCommandNotImplemented => GetResourceString("SmtpCommandNotImplemented");

	internal static string SmtpCommandParameterNotImplemented => GetResourceString("SmtpCommandParameterNotImplemented");

	internal static string SmtpCommandUnrecognized => GetResourceString("SmtpCommandUnrecognized");

	internal static string SmtpExceededStorageAllocation => GetResourceString("SmtpExceededStorageAllocation");

	internal static string SmtpFromRequired => GetResourceString("SmtpFromRequired");

	internal static string SmtpHelpMessage => GetResourceString("SmtpHelpMessage");

	internal static string SmtpInsufficientStorage => GetResourceString("SmtpInsufficientStorage");

	internal static string SmtpInvalidHostName => GetResourceString("SmtpInvalidHostName");

	internal static string SmtpInvalidOperationDuringSend => GetResourceString("SmtpInvalidOperationDuringSend");

	internal static string SmtpLocalErrorInProcessing => GetResourceString("SmtpLocalErrorInProcessing");

	internal static string SmtpMailboxBusy => GetResourceString("SmtpMailboxBusy");

	internal static string SmtpMailboxNameNotAllowed => GetResourceString("SmtpMailboxNameNotAllowed");

	internal static string SmtpMailboxUnavailable => GetResourceString("SmtpMailboxUnavailable");

	internal static string SmtpMustIssueStartTlsFirst => GetResourceString("SmtpMustIssueStartTlsFirst");

	internal static string SmtpNeedAbsolutePickupDirectory => GetResourceString("SmtpNeedAbsolutePickupDirectory");

	internal static string SmtpNonAsciiUserNotSupported => GetResourceString("SmtpNonAsciiUserNotSupported");

	internal static string SmtpOK => GetResourceString("SmtpOK");

	internal static string SmtpPickupDirectoryDoesnotSupportSsl => GetResourceString("SmtpPickupDirectoryDoesnotSupportSsl");

	internal static string SmtpRecipientFailed => GetResourceString("SmtpRecipientFailed");

	internal static string SmtpRecipientRequired => GetResourceString("SmtpRecipientRequired");

	internal static string SmtpSendMailFailure => GetResourceString("SmtpSendMailFailure");

	internal static string SmtpServiceClosingTransmissionChannel => GetResourceString("SmtpServiceClosingTransmissionChannel");

	internal static string SmtpServiceReady => GetResourceString("SmtpServiceReady");

	internal static string SmtpStartMailInput => GetResourceString("SmtpStartMailInput");

	internal static string SmtpSyntaxError => GetResourceString("SmtpSyntaxError");

	internal static string SmtpSystemStatus => GetResourceString("SmtpSystemStatus");

	internal static string SmtpTransactionFailed => GetResourceString("SmtpTransactionFailed");

	internal static string SmtpUserNotLocalTryAlternatePath => GetResourceString("SmtpUserNotLocalTryAlternatePath");

	internal static string SmtpUserNotLocalWillForward => GetResourceString("SmtpUserNotLocalWillForward");

	internal static string UnspecifiedHost => GetResourceString("UnspecifiedHost");

	internal static string SmtpAlreadyConnected => GetResourceString("SmtpAlreadyConnected");

	internal static string SmtpAuthenticationFailed => GetResourceString("SmtpAuthenticationFailed");

	internal static string net_completed_result => GetResourceString("net_completed_result");

	internal static string MailHeaderFieldInvalidCharacter => GetResourceString("MailHeaderFieldInvalidCharacter");

	internal static string SeekNotSupported => GetResourceString("SeekNotSupported");

	internal static string ReadNotSupported => GetResourceString("ReadNotSupported");

	internal static string WriteNotSupported => GetResourceString("WriteNotSupported");

	internal static string MailAddressInvalidFormat => GetResourceString("MailAddressInvalidFormat");

	internal static string SmtpAuthResponseInvalid => GetResourceString("SmtpAuthResponseInvalid");

	internal static string net_webstatus_ServerProtocolViolation => GetResourceString("net_webstatus_ServerProtocolViolation");

	internal static string SmtpDataStreamOpen => GetResourceString("SmtpDataStreamOpen");

	internal static string SmtpEhloResponseInvalid => GetResourceString("SmtpEhloResponseInvalid");

	internal static string net_io_readfailure => GetResourceString("net_io_readfailure");

	internal static string net_io_connectionclosed => GetResourceString("net_io_connectionclosed");

	internal static string SmtpInvalidResponse => GetResourceString("SmtpInvalidResponse");

	internal static string SmtpServiceNotAvailable => GetResourceString("SmtpServiceNotAvailable");

	internal static string MailSubjectInvalidFormat => GetResourceString("MailSubjectInvalidFormat");

	internal static string MailServerDoesNotSupportStartTls => GetResourceString("MailServerDoesNotSupportStartTls");

	internal static string MailHostNotFound => GetResourceString("MailHostNotFound");

	internal static string SmtpGetIisPickupDirectoryNotSupported => GetResourceString("SmtpGetIisPickupDirectoryNotSupported");

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
