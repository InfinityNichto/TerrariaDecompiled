namespace System.Net.Mail;

internal static class MailCommand
{
	internal static IAsyncResult BeginSend(SmtpConnection conn, byte[] command, MailAddress from, bool allowUnicode, AsyncCallback callback, object state)
	{
		PrepareCommand(conn, command, from, allowUnicode);
		return CheckCommand.BeginSend(conn, callback, state);
	}

	private static void CheckResponse(SmtpStatusCode statusCode, string response)
	{
		if (statusCode == SmtpStatusCode.Ok)
		{
			return;
		}
		switch (statusCode)
		{
		default:
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

	private static void PrepareCommand(SmtpConnection conn, byte[] command, MailAddress from, bool allowUnicode)
	{
		if (conn.IsStreamOpen)
		{
			throw new InvalidOperationException(System.SR.SmtpDataStreamOpen);
		}
		conn.BufferBuilder.Append(command);
		string smtpAddress = from.GetSmtpAddress(allowUnicode);
		conn.BufferBuilder.Append(smtpAddress, allowUnicode);
		if (allowUnicode)
		{
			conn.BufferBuilder.Append(" BODY=8BITMIME SMTPUTF8");
		}
		conn.BufferBuilder.Append(SmtpCommands.CRLF);
	}

	internal static void Send(SmtpConnection conn, byte[] command, MailAddress from, bool allowUnicode)
	{
		PrepareCommand(conn, command, from, allowUnicode);
		string response;
		SmtpStatusCode statusCode = CheckCommand.Send(conn, out response);
		CheckResponse(statusCode, response);
	}
}
