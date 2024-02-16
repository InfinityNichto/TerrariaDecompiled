using System.Runtime.Serialization;

namespace System.Text.RegularExpressions;

[Serializable]
public sealed class RegexParseException : ArgumentException
{
	public RegexParseError Error { get; }

	public int Offset { get; }

	internal RegexParseException(RegexParseError error, int offset, string message)
		: base(message)
	{
		Error = error;
		Offset = offset;
	}

	private RegexParseException(SerializationInfo info, StreamingContext context)
	{
		throw new NotImplementedException();
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.SetType(typeof(ArgumentException));
	}
}
