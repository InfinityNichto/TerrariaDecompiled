using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Net.Mail;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SmtpFailedRecipientsException : SmtpFailedRecipientException, ISerializable
{
	private readonly SmtpFailedRecipientException[] _innerExceptions;

	public SmtpFailedRecipientException[] InnerExceptions => _innerExceptions;

	public SmtpFailedRecipientsException()
	{
		_innerExceptions = Array.Empty<SmtpFailedRecipientException>();
	}

	public SmtpFailedRecipientsException(string? message)
		: base(message)
	{
		_innerExceptions = Array.Empty<SmtpFailedRecipientException>();
	}

	public SmtpFailedRecipientsException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		SmtpFailedRecipientException ex = innerException as SmtpFailedRecipientException;
		_innerExceptions = ((ex == null) ? Array.Empty<SmtpFailedRecipientException>() : new SmtpFailedRecipientException[1] { ex });
	}

	protected SmtpFailedRecipientsException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_innerExceptions = (SmtpFailedRecipientException[])info.GetValue("innerExceptions", typeof(SmtpFailedRecipientException[]));
	}

	public SmtpFailedRecipientsException(string? message, SmtpFailedRecipientException[] innerExceptions)
		: base(message, (innerExceptions != null && innerExceptions.Length != 0) ? innerExceptions[0].FailedRecipient : null, (innerExceptions != null && innerExceptions.Length != 0) ? innerExceptions[0] : null)
	{
		if (innerExceptions == null)
		{
			throw new ArgumentNullException("innerExceptions");
		}
		_innerExceptions = ((innerExceptions == null) ? Array.Empty<SmtpFailedRecipientException>() : innerExceptions);
	}

	internal SmtpFailedRecipientsException(List<SmtpFailedRecipientException> innerExceptions, bool allFailed)
		: base(allFailed ? System.SR.SmtpAllRecipientsFailed : System.SR.SmtpRecipientFailed, (innerExceptions != null && innerExceptions.Count > 0) ? innerExceptions[0].FailedRecipient : null, (innerExceptions != null && innerExceptions.Count > 0) ? innerExceptions[0] : null)
	{
		if (innerExceptions == null)
		{
			throw new ArgumentNullException("innerExceptions");
		}
		_innerExceptions = innerExceptions.ToArray();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		GetObjectData(serializationInfo, streamingContext);
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
		serializationInfo.AddValue("innerExceptions", _innerExceptions, typeof(SmtpFailedRecipientException[]));
	}
}
