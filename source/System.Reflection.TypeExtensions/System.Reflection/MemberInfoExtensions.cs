namespace System.Reflection;

public static class MemberInfoExtensions
{
	public static bool HasMetadataToken(this MemberInfo member)
	{
		ArgumentNullException.ThrowIfNull(member, "member");
		try
		{
			return member.GetMetadataTokenOrZeroOrThrow() != 0;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public static int GetMetadataToken(this MemberInfo member)
	{
		ArgumentNullException.ThrowIfNull(member, "member");
		int metadataTokenOrZeroOrThrow = member.GetMetadataTokenOrZeroOrThrow();
		if (metadataTokenOrZeroOrThrow == 0)
		{
			throw new InvalidOperationException(System.SR.NoMetadataTokenAvailable);
		}
		return metadataTokenOrZeroOrThrow;
	}

	private static int GetMetadataTokenOrZeroOrThrow(this MemberInfo member)
	{
		int metadataToken = member.MetadataToken;
		if ((metadataToken & 0xFFFFFF) == 0)
		{
			return 0;
		}
		return metadataToken;
	}
}
