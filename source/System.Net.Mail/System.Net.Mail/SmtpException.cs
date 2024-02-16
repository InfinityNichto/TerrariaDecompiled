using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Net.Mail;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SmtpException : Exception, ISerializable
{
	private SmtpStatusCode _statusCode = SmtpStatusCode.GeneralFailure;

	public SmtpStatusCode StatusCode
	{
		get
		{
			return _statusCode;
		}
		set
		{
			_statusCode = value;
		}
	}

	private static string GetMessageForStatus(SmtpStatusCode statusCode, string serverResponse)
	{
		return GetMessageForStatus(statusCode) + " " + System.SR.Format(System.SR.MailServerResponse, serverResponse);
	}

	private static string GetMessageForStatus(SmtpStatusCode statusCode)
	{
		return statusCode switch
		{
			SmtpStatusCode.SyntaxError => System.SR.SmtpSyntaxError, 
			SmtpStatusCode.CommandNotImplemented => System.SR.SmtpCommandNotImplemented, 
			SmtpStatusCode.BadCommandSequence => System.SR.SmtpBadCommandSequence, 
			SmtpStatusCode.CommandParameterNotImplemented => System.SR.SmtpCommandParameterNotImplemented, 
			SmtpStatusCode.SystemStatus => System.SR.SmtpSystemStatus, 
			SmtpStatusCode.HelpMessage => System.SR.SmtpHelpMessage, 
			SmtpStatusCode.ServiceReady => System.SR.SmtpServiceReady, 
			SmtpStatusCode.ServiceClosingTransmissionChannel => System.SR.SmtpServiceClosingTransmissionChannel, 
			SmtpStatusCode.ServiceNotAvailable => System.SR.SmtpServiceNotAvailable, 
			SmtpStatusCode.Ok => System.SR.SmtpOK, 
			SmtpStatusCode.UserNotLocalWillForward => System.SR.SmtpUserNotLocalWillForward, 
			SmtpStatusCode.MailboxBusy => System.SR.SmtpMailboxBusy, 
			SmtpStatusCode.MailboxUnavailable => System.SR.SmtpMailboxUnavailable, 
			SmtpStatusCode.LocalErrorInProcessing => System.SR.SmtpLocalErrorInProcessing, 
			SmtpStatusCode.UserNotLocalTryAlternatePath => System.SR.SmtpUserNotLocalTryAlternatePath, 
			SmtpStatusCode.InsufficientStorage => System.SR.SmtpInsufficientStorage, 
			SmtpStatusCode.ExceededStorageAllocation => System.SR.SmtpExceededStorageAllocation, 
			SmtpStatusCode.MailboxNameNotAllowed => System.SR.SmtpMailboxNameNotAllowed, 
			SmtpStatusCode.StartMailInput => System.SR.SmtpStartMailInput, 
			SmtpStatusCode.TransactionFailed => System.SR.SmtpTransactionFailed, 
			SmtpStatusCode.ClientNotPermitted => System.SR.SmtpClientNotPermitted, 
			SmtpStatusCode.MustIssueStartTlsFirst => System.SR.SmtpMustIssueStartTlsFirst, 
			_ => System.SR.SmtpCommandUnrecognized, 
		};
	}

	public SmtpException(SmtpStatusCode statusCode)
		: base(GetMessageForStatus(statusCode))
	{
		_statusCode = statusCode;
	}

	public SmtpException(SmtpStatusCode statusCode, string? message)
		: base(message)
	{
		_statusCode = statusCode;
	}

	public SmtpException()
		: this(SmtpStatusCode.GeneralFailure)
	{
	}

	public SmtpException(string? message)
		: base(message)
	{
	}

	public SmtpException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected SmtpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		_statusCode = (SmtpStatusCode)serializationInfo.GetInt32("Status");
	}

	internal SmtpException(SmtpStatusCode statusCode, string serverMessage, bool serverResponse)
		: base(GetMessageForStatus(statusCode, serverMessage))
	{
		_statusCode = statusCode;
	}

	internal SmtpException(string message, string serverResponse)
		: base(message + " " + System.SR.Format(System.SR.MailServerResponse, serverResponse))
	{
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		GetObjectData(serializationInfo, streamingContext);
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
		serializationInfo.AddValue("Status", (int)_statusCode, typeof(int));
	}
}
