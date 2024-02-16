using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal sealed class MemberHolder
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal readonly Type _memberType;

	internal readonly StreamingContext _context;

	internal MemberHolder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, StreamingContext ctx)
	{
		_memberType = type;
		_context = ctx;
	}

	public override int GetHashCode()
	{
		return _memberType.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is MemberHolder memberHolder && (object)memberHolder._memberType == _memberType)
		{
			return memberHolder._context.State == _context.State;
		}
		return false;
	}
}
