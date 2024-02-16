using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class WarningException : SystemException
{
	public string? HelpUrl { get; }

	public string? HelpTopic { get; }

	public WarningException()
		: this(null, null, null)
	{
	}

	public WarningException(string? message)
		: this(message, null, null)
	{
	}

	public WarningException(string? message, string? helpUrl)
		: this(message, helpUrl, null)
	{
	}

	public WarningException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public WarningException(string? message, string? helpUrl, string? helpTopic)
		: base(message)
	{
		HelpUrl = helpUrl;
		HelpTopic = helpTopic;
	}

	protected WarningException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		HelpUrl = (string)info.GetValue("helpUrl", typeof(string));
		HelpTopic = (string)info.GetValue("helpTopic", typeof(string));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("helpUrl", HelpUrl);
		info.AddValue("helpTopic", HelpTopic);
	}
}
