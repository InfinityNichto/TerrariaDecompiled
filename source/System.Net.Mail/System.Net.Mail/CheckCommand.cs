using System.Net.Mime;
using System.Runtime.ExceptionServices;

namespace System.Net.Mail;

internal static class CheckCommand
{
	private static readonly AsyncCallback s_onReadLine = OnReadLine;

	private static readonly AsyncCallback s_onWrite = OnWrite;

	internal static IAsyncResult BeginSend(SmtpConnection conn, AsyncCallback callback, object state)
	{
		MultiAsyncResult multiAsyncResult = new MultiAsyncResult(conn, callback, state);
		multiAsyncResult.Enter();
		IAsyncResult asyncResult = conn.BeginFlush(s_onWrite, multiAsyncResult);
		if (asyncResult.CompletedSynchronously)
		{
			conn.EndFlush(asyncResult);
			multiAsyncResult.Leave();
		}
		SmtpReplyReader nextReplyReader = conn.Reader.GetNextReplyReader();
		multiAsyncResult.Enter();
		IAsyncResult asyncResult2 = nextReplyReader.BeginReadLine(s_onReadLine, multiAsyncResult);
		if (asyncResult2.CompletedSynchronously)
		{
			LineInfo lineInfo = nextReplyReader.EndReadLine(asyncResult2);
			if (!(multiAsyncResult.Result is Exception))
			{
				multiAsyncResult.Result = lineInfo;
			}
			multiAsyncResult.Leave();
		}
		multiAsyncResult.CompleteSequence();
		return multiAsyncResult;
	}

	internal static object EndSend(IAsyncResult result, out string response)
	{
		object obj = MultiAsyncResult.End(result);
		if (obj is Exception source)
		{
			ExceptionDispatchInfo.Throw(source);
		}
		LineInfo lineInfo = (LineInfo)obj;
		response = lineInfo.Line;
		return lineInfo.StatusCode;
	}

	private static void OnReadLine(IAsyncResult result)
	{
		if (result.CompletedSynchronously)
		{
			return;
		}
		MultiAsyncResult multiAsyncResult = (MultiAsyncResult)result.AsyncState;
		try
		{
			SmtpConnection smtpConnection = (SmtpConnection)multiAsyncResult.Context;
			LineInfo lineInfo = smtpConnection.Reader.CurrentReader.EndReadLine(result);
			if (!(multiAsyncResult.Result is Exception))
			{
				multiAsyncResult.Result = lineInfo;
			}
			multiAsyncResult.Leave();
		}
		catch (Exception result2)
		{
			multiAsyncResult.Leave(result2);
		}
	}

	private static void OnWrite(IAsyncResult result)
	{
		if (!result.CompletedSynchronously)
		{
			MultiAsyncResult multiAsyncResult = (MultiAsyncResult)result.AsyncState;
			try
			{
				SmtpConnection smtpConnection = (SmtpConnection)multiAsyncResult.Context;
				smtpConnection.EndFlush(result);
				multiAsyncResult.Leave();
			}
			catch (Exception result2)
			{
				multiAsyncResult.Leave(result2);
			}
		}
	}

	internal static SmtpStatusCode Send(SmtpConnection conn, out string response)
	{
		conn.Flush();
		SmtpReplyReader nextReplyReader = conn.Reader.GetNextReplyReader();
		LineInfo lineInfo = nextReplyReader.ReadLine();
		response = lineInfo.Line;
		nextReplyReader.Close();
		return lineInfo.StatusCode;
	}
}
