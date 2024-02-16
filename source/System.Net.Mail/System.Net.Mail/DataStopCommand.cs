namespace System.Net.Mail;

internal static class DataStopCommand
{
	private static void CheckResponse(SmtpStatusCode statusCode, string serverResponse)
	{
		switch (statusCode)
		{
		case SmtpStatusCode.Ok:
			return;
		}
		if (statusCode < (SmtpStatusCode)400)
		{
			throw new SmtpException(System.SR.net_webstatus_ServerProtocolViolation, serverResponse);
		}
		throw new SmtpException(statusCode, serverResponse, serverResponse: true);
	}

	private static void PrepareCommand(SmtpConnection conn)
	{
		if (conn.IsStreamOpen)
		{
			throw new InvalidOperationException(System.SR.SmtpDataStreamOpen);
		}
		conn.BufferBuilder.Append(SmtpCommands.DataStop);
	}

	internal static void Send(SmtpConnection conn)
	{
		PrepareCommand(conn);
		string response;
		SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
		CheckResponse(statusCode, response);
	}
}
