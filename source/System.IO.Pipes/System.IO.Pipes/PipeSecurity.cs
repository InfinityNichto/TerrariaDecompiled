using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public class PipeSecurity : NativeObjectSecurity
{
	public override Type AccessRightType => typeof(PipeAccessRights);

	public override Type AccessRuleType => typeof(PipeAccessRule);

	public override Type AuditRuleType => typeof(PipeAuditRule);

	public PipeSecurity()
		: base(isContainer: false, ResourceType.KernelObject)
	{
	}

	internal PipeSecurity(SafePipeHandle safeHandle, AccessControlSections includeSections)
		: base(isContainer: false, ResourceType.KernelObject, safeHandle, includeSections)
	{
	}

	public void AddAccessRule(PipeAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		AddAccessRule((AccessRule)rule);
	}

	public void SetAccessRule(PipeAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		SetAccessRule((AccessRule)rule);
	}

	public void ResetAccessRule(PipeAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		ResetAccessRule((AccessRule)rule);
	}

	public bool RemoveAccessRule(PipeAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		AuthorizationRuleCollection accessRules = GetAccessRules(includeExplicit: true, includeInherited: true, rule.IdentityReference.GetType());
		for (int i = 0; i < accessRules.Count; i++)
		{
			if (accessRules[i] is PipeAccessRule pipeAccessRule && pipeAccessRule.PipeAccessRights == rule.PipeAccessRights && pipeAccessRule.IdentityReference == rule.IdentityReference && pipeAccessRule.AccessControlType == rule.AccessControlType)
			{
				return RemoveAccessRule((AccessRule)rule);
			}
		}
		if (rule.PipeAccessRights != PipeAccessRights.FullControl)
		{
			return RemoveAccessRule((AccessRule)new PipeAccessRule(rule.IdentityReference, PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny), isInherited: false, rule.AccessControlType));
		}
		return RemoveAccessRule((AccessRule)rule);
	}

	public void RemoveAccessRuleSpecific(PipeAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		AuthorizationRuleCollection accessRules = GetAccessRules(includeExplicit: true, includeInherited: true, rule.IdentityReference.GetType());
		for (int i = 0; i < accessRules.Count; i++)
		{
			if (accessRules[i] is PipeAccessRule pipeAccessRule && pipeAccessRule.PipeAccessRights == rule.PipeAccessRights && pipeAccessRule.IdentityReference == rule.IdentityReference && pipeAccessRule.AccessControlType == rule.AccessControlType)
			{
				RemoveAccessRuleSpecific((AccessRule)rule);
				return;
			}
		}
		if (rule.PipeAccessRights != PipeAccessRights.FullControl)
		{
			RemoveAccessRuleSpecific((AccessRule)new PipeAccessRule(rule.IdentityReference, PipeAccessRule.AccessMaskFromRights(rule.PipeAccessRights, AccessControlType.Deny), isInherited: false, rule.AccessControlType));
		}
		else
		{
			RemoveAccessRuleSpecific((AccessRule)rule);
		}
	}

	public void AddAuditRule(PipeAuditRule rule)
	{
		AddAuditRule((AuditRule)rule);
	}

	public void SetAuditRule(PipeAuditRule rule)
	{
		SetAuditRule((AuditRule)rule);
	}

	public bool RemoveAuditRule(PipeAuditRule rule)
	{
		return RemoveAuditRule((AuditRule)rule);
	}

	public void RemoveAuditRuleAll(PipeAuditRule rule)
	{
		RemoveAuditRuleAll((AuditRule)rule);
	}

	public void RemoveAuditRuleSpecific(PipeAuditRule rule)
	{
		RemoveAuditRuleSpecific((AuditRule)rule);
	}

	public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
	{
		if (inheritanceFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_NonContainerInvalidAnyFlag, "inheritanceFlags");
		}
		if (propagationFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_NonContainerInvalidAnyFlag, "propagationFlags");
		}
		return new PipeAccessRule(identityReference, accessMask, isInherited, type);
	}

	public sealed override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
	{
		if (inheritanceFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_NonContainerInvalidAnyFlag, "inheritanceFlags");
		}
		if (propagationFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_NonContainerInvalidAnyFlag, "propagationFlags");
		}
		return new PipeAuditRule(identityReference, accessMask, isInherited, flags);
	}

	private AccessControlSections GetAccessControlSectionsFromChanges()
	{
		AccessControlSections accessControlSections = AccessControlSections.None;
		if (base.AccessRulesModified)
		{
			accessControlSections = AccessControlSections.Access;
		}
		if (base.AuditRulesModified)
		{
			accessControlSections |= AccessControlSections.Audit;
		}
		if (base.OwnerModified)
		{
			accessControlSections |= AccessControlSections.Owner;
		}
		if (base.GroupModified)
		{
			accessControlSections |= AccessControlSections.Group;
		}
		return accessControlSections;
	}

	protected internal void Persist(SafeHandle handle)
	{
		WriteLock();
		try
		{
			AccessControlSections accessControlSectionsFromChanges = GetAccessControlSectionsFromChanges();
			Persist(handle, accessControlSectionsFromChanges);
			bool flag2 = (base.AccessRulesModified = false);
			bool flag4 = (base.AuditRulesModified = flag2);
			bool ownerModified = (base.GroupModified = flag4);
			base.OwnerModified = ownerModified;
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected internal void Persist(string name)
	{
		WriteLock();
		try
		{
			AccessControlSections accessControlSectionsFromChanges = GetAccessControlSectionsFromChanges();
			Persist(name, accessControlSectionsFromChanges);
			bool flag2 = (base.AccessRulesModified = false);
			bool flag4 = (base.AuditRulesModified = flag2);
			bool ownerModified = (base.GroupModified = flag4);
			base.OwnerModified = ownerModified;
		}
		finally
		{
			WriteUnlock();
		}
	}
}
