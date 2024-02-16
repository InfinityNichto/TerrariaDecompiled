using System.Security.Principal;

namespace System.Security.AccessControl;

public class AuditRule<T> : AuditRule where T : struct
{
	public T Rights => (T)(object)base.AccessMask;

	public AuditRule(IdentityReference identity, T rights, AuditFlags flags)
		: this(identity, rights, InheritanceFlags.None, PropagationFlags.None, flags)
	{
	}

	public AuditRule(IdentityReference identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: this(identity, (int)(object)rights, isInherited: false, inheritanceFlags, propagationFlags, flags)
	{
	}

	public AuditRule(string identity, T rights, AuditFlags flags)
		: this((IdentityReference)new NTAccount(identity), rights, InheritanceFlags.None, PropagationFlags.None, flags)
	{
	}

	public AuditRule(string identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: this((IdentityReference)new NTAccount(identity), (int)(object)rights, isInherited: false, inheritanceFlags, propagationFlags, flags)
	{
	}

	internal AuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
	{
	}
}
public abstract class AuditRule : AuthorizationRule
{
	private readonly AuditFlags _flags;

	public AuditFlags AuditFlags => _flags;

	protected AuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
	{
		if (auditFlags == AuditFlags.None)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "auditFlags");
		}
		if (((uint)auditFlags & 0xFFFFFFFCu) != 0)
		{
			throw new ArgumentOutOfRangeException("auditFlags", System.SR.ArgumentOutOfRange_Enum);
		}
		_flags = auditFlags;
	}
}
