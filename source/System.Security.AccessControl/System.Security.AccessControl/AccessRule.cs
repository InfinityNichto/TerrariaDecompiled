using System.Security.Principal;

namespace System.Security.AccessControl;

public class AccessRule<T> : AccessRule where T : struct
{
	public T Rights => (T)(object)base.AccessMask;

	public AccessRule(IdentityReference identity, T rights, AccessControlType type)
		: this(identity, (int)(object)rights, isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}

	public AccessRule(string identity, T rights, AccessControlType type)
		: this((IdentityReference)new NTAccount(identity), (int)(object)rights, isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}

	public AccessRule(IdentityReference identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: this(identity, (int)(object)rights, isInherited: false, inheritanceFlags, propagationFlags, type)
	{
	}

	public AccessRule(string identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: this((IdentityReference)new NTAccount(identity), (int)(object)rights, isInherited: false, inheritanceFlags, propagationFlags, type)
	{
	}

	internal AccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
	{
	}
}
public abstract class AccessRule : AuthorizationRule
{
	private readonly AccessControlType _type;

	public AccessControlType AccessControlType => _type;

	protected AccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
	{
		if (type != 0 && type != AccessControlType.Deny)
		{
			throw new ArgumentOutOfRangeException("type", System.SR.ArgumentOutOfRange_Enum);
		}
		if ((inheritanceFlags < InheritanceFlags.None) || inheritanceFlags > (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit))
		{
			throw new ArgumentOutOfRangeException("inheritanceFlags", System.SR.Format(System.SR.Argument_InvalidEnumValue, inheritanceFlags, "InheritanceFlags"));
		}
		if ((propagationFlags < PropagationFlags.None) || propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
		{
			throw new ArgumentOutOfRangeException("propagationFlags", System.SR.Format(System.SR.Argument_InvalidEnumValue, inheritanceFlags, "PropagationFlags"));
		}
		_type = type;
	}
}
