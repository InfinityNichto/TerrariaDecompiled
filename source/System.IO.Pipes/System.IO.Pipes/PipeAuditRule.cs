using System.Security.AccessControl;
using System.Security.Principal;

namespace System.IO.Pipes;

public sealed class PipeAuditRule : AuditRule
{
	public PipeAccessRights PipeAccessRights => PipeAccessRule.RightsFromAccessMask(base.AccessMask);

	public PipeAuditRule(IdentityReference identity, PipeAccessRights rights, AuditFlags flags)
		: this(identity, AccessMaskFromRights(rights), isInherited: false, flags)
	{
	}

	public PipeAuditRule(string identity, PipeAccessRights rights, AuditFlags flags)
		: this(new NTAccount(identity), AccessMaskFromRights(rights), isInherited: false, flags)
	{
	}

	internal PipeAuditRule(IdentityReference identity, int accessMask, bool isInherited, AuditFlags flags)
		: base(identity, accessMask, isInherited, InheritanceFlags.None, PropagationFlags.None, flags)
	{
	}

	private static int AccessMaskFromRights(PipeAccessRights rights)
	{
		if (rights < (PipeAccessRights)0 || rights > (PipeAccessRights.FullControl | PipeAccessRights.AccessSystemSecurity))
		{
			throw new ArgumentOutOfRangeException("rights", System.SR.ArgumentOutOfRange_NeedValidPipeAccessRights);
		}
		return (int)rights;
	}
}
