using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class DirectoryObjectSecurity : ObjectSecurity
{
	protected DirectoryObjectSecurity()
		: base(isContainer: true, isDS: true)
	{
	}

	protected DirectoryObjectSecurity(CommonSecurityDescriptor securityDescriptor)
		: base(securityDescriptor)
	{
		if (securityDescriptor == null)
		{
			throw new ArgumentNullException("securityDescriptor");
		}
	}

	private static bool IsValidTargetTypeStatic(Type targetType)
	{
		if (targetType == typeof(NTAccount))
		{
			return true;
		}
		if (targetType == typeof(SecurityIdentifier))
		{
			return true;
		}
		return false;
	}

	private AuthorizationRuleCollection GetRules(bool access, bool includeExplicit, bool includeInherited, Type targetType)
	{
		ReadLock();
		try
		{
			AuthorizationRuleCollection authorizationRuleCollection = new AuthorizationRuleCollection();
			if (!IsValidTargetTypeStatic(targetType))
			{
				throw new ArgumentException(System.SR.Arg_MustBeIdentityReferenceType, "targetType");
			}
			CommonAcl commonAcl = null;
			if (access)
			{
				if ((base.SecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
				{
					commonAcl = base.SecurityDescriptor.DiscretionaryAcl;
				}
			}
			else if ((base.SecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0)
			{
				commonAcl = base.SecurityDescriptor.SystemAcl;
			}
			if (commonAcl == null)
			{
				return authorizationRuleCollection;
			}
			IdentityReferenceCollection identityReferenceCollection = null;
			if (targetType != typeof(SecurityIdentifier))
			{
				IdentityReferenceCollection identityReferenceCollection2 = new IdentityReferenceCollection(commonAcl.Count);
				for (int i = 0; i < commonAcl.Count; i++)
				{
					QualifiedAce qualifiedAce = commonAcl[i] as QualifiedAce;
					if (qualifiedAce == null || qualifiedAce.IsCallback)
					{
						continue;
					}
					if (access)
					{
						if (qualifiedAce.AceQualifier != 0 && qualifiedAce.AceQualifier != AceQualifier.AccessDenied)
						{
							continue;
						}
					}
					else if (qualifiedAce.AceQualifier != AceQualifier.SystemAudit)
					{
						continue;
					}
					identityReferenceCollection2.Add(qualifiedAce.SecurityIdentifier);
				}
				identityReferenceCollection = identityReferenceCollection2.Translate(targetType);
			}
			for (int j = 0; j < commonAcl.Count; j++)
			{
				QualifiedAce qualifiedAce2 = commonAcl[j] as CommonAce;
				if (qualifiedAce2 == null)
				{
					qualifiedAce2 = commonAcl[j] as ObjectAce;
					if (qualifiedAce2 == null)
					{
						continue;
					}
				}
				if (qualifiedAce2.IsCallback)
				{
					continue;
				}
				if (access)
				{
					if (qualifiedAce2.AceQualifier != 0 && qualifiedAce2.AceQualifier != AceQualifier.AccessDenied)
					{
						continue;
					}
				}
				else if (qualifiedAce2.AceQualifier != AceQualifier.SystemAudit)
				{
					continue;
				}
				if ((!includeExplicit || (qualifiedAce2.AceFlags & AceFlags.Inherited) != 0) && (!includeInherited || (qualifiedAce2.AceFlags & AceFlags.Inherited) == 0))
				{
					continue;
				}
				IdentityReference identityReference = ((targetType == typeof(SecurityIdentifier)) ? qualifiedAce2.SecurityIdentifier : identityReferenceCollection[j]);
				if (access)
				{
					AccessControlType type = ((qualifiedAce2.AceQualifier != 0) ? AccessControlType.Deny : AccessControlType.Allow);
					if (qualifiedAce2 is ObjectAce objectAce)
					{
						authorizationRuleCollection.AddRule(AccessRuleFactory(identityReference, objectAce.AccessMask, objectAce.IsInherited, objectAce.InheritanceFlags, objectAce.PropagationFlags, type, objectAce.ObjectAceType, objectAce.InheritedObjectAceType));
						continue;
					}
					CommonAce commonAce = qualifiedAce2 as CommonAce;
					if (!(commonAce == null))
					{
						authorizationRuleCollection.AddRule(AccessRuleFactory(identityReference, commonAce.AccessMask, commonAce.IsInherited, commonAce.InheritanceFlags, commonAce.PropagationFlags, type));
					}
				}
				else if (qualifiedAce2 is ObjectAce objectAce2)
				{
					authorizationRuleCollection.AddRule(AuditRuleFactory(identityReference, objectAce2.AccessMask, objectAce2.IsInherited, objectAce2.InheritanceFlags, objectAce2.PropagationFlags, objectAce2.AuditFlags, objectAce2.ObjectAceType, objectAce2.InheritedObjectAceType));
				}
				else
				{
					CommonAce commonAce2 = qualifiedAce2 as CommonAce;
					if (!(commonAce2 == null))
					{
						authorizationRuleCollection.AddRule(AuditRuleFactory(identityReference, commonAce2.AccessMask, commonAce2.IsInherited, commonAce2.InheritanceFlags, commonAce2.PropagationFlags, commonAce2.AuditFlags));
					}
				}
			}
			return authorizationRuleCollection;
		}
		finally
		{
			ReadUnlock();
		}
	}

	private bool ModifyAccess(AccessControlModification modification, ObjectAccessRule rule, out bool modified)
	{
		bool flag = true;
		if (base.SecurityDescriptor.DiscretionaryAcl == null)
		{
			if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
			{
				modified = false;
				return flag;
			}
			base.SecurityDescriptor.AddDiscretionaryAcl(GenericAcl.AclRevisionDS, 1);
		}
		else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) && rule.ObjectFlags != 0 && base.SecurityDescriptor.DiscretionaryAcl.Revision < GenericAcl.AclRevisionDS)
		{
			byte[] array = new byte[base.SecurityDescriptor.DiscretionaryAcl.BinaryLength];
			base.SecurityDescriptor.DiscretionaryAcl.GetBinaryForm(array, 0);
			array[0] = GenericAcl.AclRevisionDS;
			base.SecurityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(base.IsContainer, base.IsDS, new RawAcl(array, 0));
		}
		SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));
		if (rule.AccessControlType == AccessControlType.Allow)
		{
			switch (modification)
			{
			case AccessControlModification.Add:
				base.SecurityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule);
				break;
			case AccessControlModification.Set:
				base.SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
				break;
			case AccessControlModification.Reset:
				base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
				base.SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule);
				break;
			case AccessControlModification.Remove:
				flag = base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule);
				break;
			case AccessControlModification.RemoveAll:
				flag = base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
				if (!flag)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_RemoveFail);
				}
				break;
			case AccessControlModification.RemoveSpecific:
				base.SecurityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule);
				break;
			default:
				throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
			}
		}
		else
		{
			if (rule.AccessControlType != AccessControlType.Deny)
			{
				throw new ArgumentException(System.SR.Format(System.SR.TypeUnrecognized_AccessControl, rule.AccessControlType));
			}
			switch (modification)
			{
			case AccessControlModification.Add:
				base.SecurityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule);
				break;
			case AccessControlModification.Set:
				base.SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
				break;
			case AccessControlModification.Reset:
				base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
				base.SecurityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule);
				break;
			case AccessControlModification.Remove:
				flag = base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule);
				break;
			case AccessControlModification.RemoveAll:
				flag = base.SecurityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
				if (!flag)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_RemoveFail);
				}
				break;
			case AccessControlModification.RemoveSpecific:
				base.SecurityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule);
				break;
			default:
				throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
			}
		}
		modified = flag;
		base.AccessRulesModified |= modified;
		return flag;
	}

	private bool ModifyAudit(AccessControlModification modification, ObjectAuditRule rule, out bool modified)
	{
		bool flag = true;
		if (base.SecurityDescriptor.SystemAcl == null)
		{
			if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
			{
				modified = false;
				return flag;
			}
			base.SecurityDescriptor.AddSystemAcl(GenericAcl.AclRevisionDS, 1);
		}
		else if ((modification == AccessControlModification.Add || modification == AccessControlModification.Set || modification == AccessControlModification.Reset) && rule.ObjectFlags != 0 && base.SecurityDescriptor.SystemAcl.Revision < GenericAcl.AclRevisionDS)
		{
			byte[] array = new byte[base.SecurityDescriptor.SystemAcl.BinaryLength];
			base.SecurityDescriptor.SystemAcl.GetBinaryForm(array, 0);
			array[0] = GenericAcl.AclRevisionDS;
			base.SecurityDescriptor.SystemAcl = new SystemAcl(base.IsContainer, base.IsDS, new RawAcl(array, 0));
		}
		SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));
		switch (modification)
		{
		case AccessControlModification.Add:
			base.SecurityDescriptor.SystemAcl.AddAudit(sid, rule);
			break;
		case AccessControlModification.Set:
			base.SecurityDescriptor.SystemAcl.SetAudit(sid, rule);
			break;
		case AccessControlModification.Reset:
			base.SecurityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Success | AuditFlags.Failure, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
			base.SecurityDescriptor.SystemAcl.SetAudit(sid, rule);
			break;
		case AccessControlModification.Remove:
			flag = base.SecurityDescriptor.SystemAcl.RemoveAudit(sid, rule);
			break;
		case AccessControlModification.RemoveAll:
			flag = base.SecurityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Success | AuditFlags.Failure, sid, -1, InheritanceFlags.ContainerInherit, PropagationFlags.None, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
			if (!flag)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_RemoveFail);
			}
			break;
		case AccessControlModification.RemoveSpecific:
			base.SecurityDescriptor.SystemAcl.RemoveAuditSpecific(sid, rule);
			break;
		default:
			throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
		}
		modified = flag;
		base.AuditRulesModified |= modified;
		return flag;
	}

	public virtual AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type, Guid objectType, Guid inheritedObjectType)
	{
		throw System.NotImplemented.ByDesign;
	}

	public virtual AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags, Guid objectType, Guid inheritedObjectType)
	{
		throw System.NotImplemented.ByDesign;
	}

	protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
	{
		return ModifyAccess(modification, (ObjectAccessRule)rule, out modified);
	}

	protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
	{
		return ModifyAudit(modification, (ObjectAuditRule)rule, out modified);
	}

	protected void AddAccessRule(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAccess(AccessControlModification.Add, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void SetAccessRule(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAccess(AccessControlModification.Set, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void ResetAccessRule(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAccess(AccessControlModification.Reset, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected bool RemoveAccessRule(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			if (base.SecurityDescriptor == null)
			{
				return true;
			}
			bool modified;
			return ModifyAccess(AccessControlModification.Remove, rule, out modified);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void RemoveAccessRuleAll(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			if (base.SecurityDescriptor != null)
			{
				ModifyAccess(AccessControlModification.RemoveAll, rule, out var _);
			}
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void RemoveAccessRuleSpecific(ObjectAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		if (base.SecurityDescriptor == null)
		{
			return;
		}
		WriteLock();
		try
		{
			ModifyAccess(AccessControlModification.RemoveSpecific, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void AddAuditRule(ObjectAuditRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAudit(AccessControlModification.Add, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void SetAuditRule(ObjectAuditRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAudit(AccessControlModification.Set, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected bool RemoveAuditRule(ObjectAuditRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			bool modified;
			return ModifyAudit(AccessControlModification.Remove, rule, out modified);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void RemoveAuditRuleAll(ObjectAuditRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAudit(AccessControlModification.RemoveAll, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void RemoveAuditRuleSpecific(ObjectAuditRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			ModifyAudit(AccessControlModification.RemoveSpecific, rule, out var _);
		}
		finally
		{
			WriteUnlock();
		}
	}

	public AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, Type targetType)
	{
		return GetRules(access: true, includeExplicit, includeInherited, targetType);
	}

	public AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, Type targetType)
	{
		return GetRules(access: false, includeExplicit, includeInherited, targetType);
	}
}
