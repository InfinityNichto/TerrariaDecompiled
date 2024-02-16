using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class DiscretionaryAcl : CommonAcl
{
	private static readonly SecurityIdentifier _sidEveryone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

	private bool everyOneFullAccessForNullDacl;

	internal bool EveryOneFullAccessForNullDacl
	{
		get
		{
			return everyOneFullAccessForNullDacl;
		}
		set
		{
			everyOneFullAccessForNullDacl = value;
		}
	}

	public DiscretionaryAcl(bool isContainer, bool isDS, int capacity)
		: this(isContainer, isDS, isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, capacity)
	{
	}

	public DiscretionaryAcl(bool isContainer, bool isDS, byte revision, int capacity)
		: base(isContainer, isDS, revision, capacity)
	{
	}

	public DiscretionaryAcl(bool isContainer, bool isDS, RawAcl? rawAcl)
		: this(isContainer, isDS, rawAcl, trusted: false)
	{
	}

	internal DiscretionaryAcl(bool isContainer, bool isDS, RawAcl rawAcl, bool trusted)
		: base(isContainer, isDS, (rawAcl == null) ? new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, 0) : rawAcl, trusted, isDacl: true)
	{
	}

	public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
	{
		CheckAccessType(accessType);
		CheckFlags(inheritanceFlags, propagationFlags);
		everyOneFullAccessForNullDacl = false;
		AddQualifiedAce(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
	}

	public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
	{
		CheckAccessType(accessType);
		CheckFlags(inheritanceFlags, propagationFlags);
		everyOneFullAccessForNullDacl = false;
		SetQualifiedAce(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
	}

	public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
	{
		CheckAccessType(accessType);
		everyOneFullAccessForNullDacl = false;
		return RemoveQualifiedAces(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), saclSemantics: false, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
	}

	public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
	{
		CheckAccessType(accessType);
		everyOneFullAccessForNullDacl = false;
		RemoveQualifiedAcesSpecific(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
	}

	public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
	{
		AddAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
	}

	public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (!base.IsDS)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_OnlyValidForDS);
		}
		CheckAccessType(accessType);
		CheckFlags(inheritanceFlags, propagationFlags);
		everyOneFullAccessForNullDacl = false;
		AddQualifiedAce(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
	}

	public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
	{
		SetAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
	}

	public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (!base.IsDS)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_OnlyValidForDS);
		}
		CheckAccessType(accessType);
		CheckFlags(inheritanceFlags, propagationFlags);
		everyOneFullAccessForNullDacl = false;
		SetQualifiedAce(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
	}

	public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
	{
		return RemoveAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
	}

	public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (!base.IsDS)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_OnlyValidForDS);
		}
		CheckAccessType(accessType);
		everyOneFullAccessForNullDacl = false;
		return RemoveQualifiedAces(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), saclSemantics: false, objectFlags, objectType, inheritedObjectType);
	}

	public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
	{
		RemoveAccessSpecific(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
	}

	public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (!base.IsDS)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_OnlyValidForDS);
		}
		CheckAccessType(accessType);
		everyOneFullAccessForNullDacl = false;
		RemoveQualifiedAcesSpecific(sid, (accessType != 0) ? AceQualifier.AccessDenied : AceQualifier.AccessAllowed, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
	}

	internal override void OnAclModificationTried()
	{
		everyOneFullAccessForNullDacl = false;
	}

	internal static DiscretionaryAcl CreateAllowEveryoneFullAccess(bool isDS, bool isContainer)
	{
		DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, 1);
		discretionaryAcl.AddAccess(AccessControlType.Allow, _sidEveryone, -1, isContainer ? (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit) : InheritanceFlags.None, PropagationFlags.None);
		discretionaryAcl.everyOneFullAccessForNullDacl = true;
		return discretionaryAcl;
	}
}
