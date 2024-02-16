using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class ObjectAuditRule : AuditRule
{
	private readonly Guid _objectType;

	private readonly Guid _inheritedObjectType;

	private readonly ObjectAceFlags _objectFlags;

	public Guid ObjectType => _objectType;

	public Guid InheritedObjectType => _inheritedObjectType;

	public ObjectAceFlags ObjectFlags => _objectFlags;

	protected ObjectAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AuditFlags auditFlags)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags)
	{
		if (!objectType.Equals(Guid.Empty) && ((uint)accessMask & 0x13Bu) != 0)
		{
			_objectType = objectType;
			_objectFlags |= ObjectAceFlags.ObjectAceTypePresent;
		}
		else
		{
			_objectType = Guid.Empty;
		}
		if (!inheritedObjectType.Equals(Guid.Empty) && (inheritanceFlags & InheritanceFlags.ContainerInherit) != 0)
		{
			_inheritedObjectType = inheritedObjectType;
			_objectFlags |= ObjectAceFlags.InheritedObjectAceTypePresent;
		}
		else
		{
			_inheritedObjectType = Guid.Empty;
		}
	}
}
