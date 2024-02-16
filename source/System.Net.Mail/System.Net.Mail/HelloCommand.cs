namespace System.Net.Mail;

internal static class HelloCommand
{
	internal static IAsyncResult BeginSend(SmtpConnection conn, string domain, AsyncCallback callback, object state)
	{
		PrepareCommand(conn, domain);
		return CheckCommand.BeginSend(conn, callback, state);
	}

	private static void CheckResponse(SmtpStatusCode statusCode, string serverResponse)
	{
		if (statusCode == SmtpStatusCode.Ok)
		{
			return;
		}
		if (statusCode < (SmtpStatusCode)400)
		{
			throw new SmtpException(System.SR.net_webstatus_ServerProtocolViolation, serverResponse);
		}
		throw new SmtpException(statusCode, serverResponse, serverResponse: true);
	}

	internal static void EndSend(IAsyncResult result)
	{
		string response;
		SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
		CheckResponse(statusCode, response);
	}

	private static void PrepareCommand(SmtpConnection conn, string domain)
	{
		if (conn.IsStreamOpen)
		{
			throw new InvalidOperationException(System.SR.SmtpDataStreamOpen);
		}
		conn.BufferBuilder.Append(SmtpCommands.Hello);
		conn.BufferBuilder.Append(domain);
		conn.BufferBuilder.Append(SmtpCommands.CRLF);
	}

	internal static void Send(SmtpConnection conn, string domain)
	{
		PrepareCommand(conn, domain);
		string response;
		SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
		CheckResponse(statusCode, response);
	}
}
