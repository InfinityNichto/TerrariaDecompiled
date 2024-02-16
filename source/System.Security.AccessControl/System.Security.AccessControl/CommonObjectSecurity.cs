using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class CommonObjectSecurity : ObjectSecurity
{
	protected CommonObjectSecurity(bool isContainer)
		: base(isContainer, isDS: false)
	{
	}

	internal CommonObjectSecurity(CommonSecurityDescriptor securityDescriptor)
		: base(securityDescriptor)
	{
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
				if ((_securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
				{
					commonAcl = _securityDescriptor.DiscretionaryAcl;
				}
			}
			else if ((_securityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0)
			{
				commonAcl = _securityDescriptor.SystemAcl;
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
					CommonAce commonAce = commonAcl[i] as CommonAce;
					if (AceNeedsTranslation(commonAce, access, includeExplicit, includeInherited))
					{
						identityReferenceCollection2.Add(commonAce.SecurityIdentifier);
					}
				}
				identityReferenceCollection = identityReferenceCollection2.Translate(targetType);
			}
			int num = 0;
			for (int j = 0; j < commonAcl.Count; j++)
			{
				CommonAce commonAce2 = commonAcl[j] as CommonAce;
				if (AceNeedsTranslation(commonAce2, access, includeExplicit, includeInherited))
				{
					IdentityReference identityReference = ((targetType == typeof(SecurityIdentifier)) ? commonAce2.SecurityIdentifier : identityReferenceCollection[num++]);
					if (access)
					{
						authorizationRuleCollection.AddRule(AccessRuleFactory(type: (commonAce2.AceQualifier != 0) ? AccessControlType.Deny : AccessControlType.Allow, identityReference: identityReference, accessMask: commonAce2.AccessMask, isInherited: commonAce2.IsInherited, inheritanceFlags: commonAce2.InheritanceFlags, propagationFlags: commonAce2.PropagationFlags));
					}
					else
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

	private bool AceNeedsTranslation([NotNullWhen(true)] CommonAce ace, bool isAccessAce, bool includeExplicit, bool includeInherited)
	{
		if (ace == null)
		{
			return false;
		}
		if (isAccessAce)
		{
			if (ace.AceQualifier != 0 && ace.AceQualifier != AceQualifier.AccessDenied)
			{
				return false;
			}
		}
		else if (ace.AceQualifier != AceQualifier.SystemAudit)
		{
			return false;
		}
		if ((includeExplicit && (ace.AceFlags & AceFlags.Inherited) == 0) || (includeInherited && (ace.AceFlags & AceFlags.Inherited) != 0))
		{
			return true;
		}
		return false;
	}

	protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			bool flag = true;
			if (_securityDescriptor.DiscretionaryAcl == null)
			{
				if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
				{
					modified = false;
					return flag;
				}
				_securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(base.IsContainer, base.IsDS, GenericAcl.AclRevision, 1);
				_securityDescriptor.AddControlFlags(ControlFlags.DiscretionaryAclPresent);
			}
			SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));
			if (rule.AccessControlType == AccessControlType.Allow)
			{
				switch (modification)
				{
				case AccessControlModification.Add:
					_securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Set:
					_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Reset:
					_securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
					_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Remove:
					flag = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.RemoveAll:
					flag = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
					if (!flag)
					{
						throw new InvalidOperationException();
					}
					break;
				case AccessControlModification.RemoveSpecific:
					_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				default:
					throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
				}
			}
			else
			{
				if (rule.AccessControlType != AccessControlType.Deny)
				{
					throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, (int)rule.AccessControlType), "rule");
				}
				switch (modification)
				{
				case AccessControlModification.Add:
					_securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Set:
					_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Reset:
					_securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
					_securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Remove:
					flag = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.RemoveAll:
					flag = _securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
					if (!flag)
					{
						throw new InvalidOperationException();
					}
					break;
				case AccessControlModification.RemoveSpecific:
					_securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				default:
					throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
				}
			}
			modified = flag;
			base.AccessRulesModified |= modified;
			return flag;
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			bool flag = true;
			if (_securityDescriptor.SystemAcl == null)
			{
				if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
				{
					modified = false;
					return flag;
				}
				_securityDescriptor.SystemAcl = new SystemAcl(base.IsContainer, base.IsDS, GenericAcl.AclRevision, 1);
				_securityDescriptor.AddControlFlags(ControlFlags.SystemAclPresent);
			}
			SecurityIdentifier sid = (SecurityIdentifier)rule.IdentityReference.Translate(typeof(SecurityIdentifier));
			switch (modification)
			{
			case AccessControlModification.Add:
				_securityDescriptor.SystemAcl.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
				break;
			case AccessControlModification.Set:
				_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
				break;
			case AccessControlModification.Reset:
				_securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
				break;
			case AccessControlModification.Remove:
				flag = _securityDescriptor.SystemAcl.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
				break;
			case AccessControlModification.RemoveAll:
				flag = _securityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Success | AuditFlags.Failure, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
				if (!flag)
				{
					throw new InvalidOperationException();
				}
				break;
			case AccessControlModification.RemoveSpecific:
				_securityDescriptor.SystemAcl.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
				break;
			default:
				throw new ArgumentOutOfRangeException("modification", System.SR.ArgumentOutOfRange_Enum);
			}
			modified = flag;
			base.AuditRulesModified |= modified;
			return flag;
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void AddAccessRule(AccessRule rule)
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

	protected void SetAccessRule(AccessRule rule)
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

	protected void ResetAccessRule(AccessRule rule)
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

	protected bool RemoveAccessRule(AccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			if (_securityDescriptor == null)
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

	protected void RemoveAccessRuleAll(AccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			if (_securityDescriptor != null)
			{
				ModifyAccess(AccessControlModification.RemoveAll, rule, out var _);
			}
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void RemoveAccessRuleSpecific(AccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		WriteLock();
		try
		{
			if (_securityDescriptor != null)
			{
				ModifyAccess(AccessControlModification.RemoveSpecific, rule, out var _);
			}
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected void AddAuditRule(AuditRule rule)
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

	protected void SetAuditRule(AuditRule rule)
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

	protected bool RemoveAuditRule(AuditRule rule)
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

	protected void RemoveAuditRuleAll(AuditRule rule)
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

	protected void RemoveAuditRuleSpecific(AuditRule rule)
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
