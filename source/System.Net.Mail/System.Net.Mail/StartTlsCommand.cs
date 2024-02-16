namespace System.Net.Mail;

internal static class StartTlsCommand
{
	internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
	{
		PrepareCommand(conn);
		return CheckCommand.BeginSend(conn, callback, state);
	}

	private static void CheckResponse(SmtpStatusCode statusCode, string response)
	{
		if (statusCode != SmtpStatusCode.ServiceReady)
		{
			if (statusCode != SmtpStatusCode.ClientNotPermitted)
			{
			}
			if (statusCode < (SmtpStatusCode)400)
			{
				throw new SmtpException(System.SR.net_webstatus_ServerProtocolViolation, response);
			}
			throw new SmtpException(statusCode, response, serverResponse: true);
		}
	}

	internal static void EndSend(IAsyncResult result)
	{
		string response;
		SmtpStatusCode statusCode = (SmtpStatusCode)CheckCommand.EndSend(result, out response);
		CheckResponse(statusCode, response);
	}

	private static void PrepareCommand(SmtpConnection conn)
	{
		if (conn.IsStreamOpen)
		{
			throw new InvalidOperationException(System.SR.SmtpDataStreamOpen);
		}
		conn.BufferBuilder.Append(SmtpCommands.StartTls);
		conn.BufferBuilder.Append(SmtpCommands.CRLF);
	}

	internal static void Send(SmtpConnection conn)
	{
		PrepareCommand(conn);
		string response;
		SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
		CheckResponse(statusCode, response);
	}
}
