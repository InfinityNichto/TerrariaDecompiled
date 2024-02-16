namespace System.Net.Security;

internal sealed class ProtocolToken
{
	internal SecurityStatusPal Status;

	internal byte[] Payload;

	internal int Size;

	internal bool Failed
	{
		get
		{
			if (Status.ErrorCode != SecurityStatusPalErrorCode.OK)
			{
				return Status.ErrorCode != SecurityStatusPalErrorCode.ContinueNeeded;
			}
			return false;
		}
	}

	internal bool Done => Status.ErrorCode == SecurityStatusPalErrorCode.OK;

	internal ProtocolToken(byte[] data, SecurityStatusPal status)
	{
		Status = status;
		Payload = data;
		Size = ((data != null) ? data.Length : 0);
	}

	internal Exception GetException()
	{
		if (!Done)
		{
			return SslStreamPal.GetException(Status);
		}
		return null;
	}
}
