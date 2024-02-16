namespace System.Net.Mail;

internal static class QuitCommand
{
	private static void PrepareCommand(SmtpConnection conn)
	{
		if (conn.IsStreamOpen)
		{
			throw new InvalidOperationException(System.SR.SmtpDataStreamOpen);
		}
		conn.BufferBuilder.Append(SmtpCommands.Quit);
	}

	internal static void Send(SmtpConnection conn)
	{
		PrepareCommand(conn);
		conn.Flush();
	}
}
