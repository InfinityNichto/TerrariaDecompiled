namespace System.Net.Mail;

internal struct LineInfo
{
	private readonly string _line;

	private readonly SmtpStatusCode _statusCode;

	internal string Line => _line;

	internal SmtpStatusCode StatusCode => _statusCode;

	internal LineInfo(SmtpStatusCode statusCode, string line)
	{
		_statusCode = statusCode;
		_line = line;
	}
}
