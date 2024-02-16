using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl;

public abstract class FileSystemSecurity : NativeObjectSecurity
{
	public override Type AccessRightType => typeof(FileSystemRights);

	public override Type AccessRuleType => typeof(FileSystemAccessRule);

	public override Type AuditRuleType => typeof(FileSystemAuditRule);

	internal FileSystemSecurity(bool isContainer)
		: base(isContainer, ResourceType.FileObject, _HandleErrorCode, isContainer)
	{
	}

	internal FileSystemSecurity(bool isContainer, string name, AccessControlSections includeSections, bool isDirectory)
		: base(isContainer, ResourceType.FileObject, System.IO.PathInternal.EnsureExtendedPrefixIfNeeded(Path.GetFullPath(name)), includeSections, _HandleErrorCode, isDirectory)
	{
	}

	internal FileSystemSecurity(bool isContainer, SafeFileHandle handle, AccessControlSections includeSections, bool isDirectory)
		: base(isContainer, ResourceType.FileObject, handle, includeSections, _HandleErrorCode, isDirectory)
	{
	}

	private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
	{
		Exception result = null;
		switch (errorCode)
		{
		case 123:
			result = new ArgumentException(System.SR.Argument_InvalidName, "name");
			break;
		case 6:
			result = new ArgumentException(System.SR.AccessControl_InvalidHandle);
			break;
		case 2:
			result = ((context != null && context is bool && (bool)context) ? ((IOException)((name == null || name.Length == 0) ? new DirectoryNotFoundException() : new DirectoryNotFoundException(name))) : ((IOException)((name == null || name.Length == 0) ? new FileNotFoundException() : new FileNotFoundException(name))));
			break;
		}
		return result;
	}

	public sealed override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
	{
		return new FileSystemAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
	}

	public sealed override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
	{
		return new FileSystemAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
	}

	internal AccessControlSections GetAccessControlSectionsFromChanges()
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

	internal void Persist(string fullPath)
	{
		WriteLock();
		try
		{
			AccessControlSections accessControlSectionsFromChanges = GetAccessControlSectionsFromChanges();
			Persist(fullPath, accessControlSectionsFromChanges);
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

	internal void Persist(SafeFileHandle handle, string fullPath)
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

	public void AddAccessRule(FileSystemAccessRule rule)
	{
		AddAccessRule((AccessRule)rule);
	}

	public void SetAccessRule(FileSystemAccessRule rule)
	{
		SetAccessRule((AccessRule)rule);
	}

	public void ResetAccessRule(FileSystemAccessRule rule)
	{
		ResetAccessRule((AccessRule)rule);
	}

	public bool RemoveAccessRule(FileSystemAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		AuthorizationRuleCollection accessRules = GetAccessRules(includeExplicit: true, includeInherited: true, rule.IdentityReference.GetType());
		for (int i = 0; i < accessRules.Count; i++)
		{
			if (accessRules[i] is FileSystemAccessRule fileSystemAccessRule && fileSystemAccessRule.FileSystemRights == rule.FileSystemRights && fileSystemAccessRule.IdentityReference == rule.IdentityReference && fileSystemAccessRule.AccessControlType == rule.AccessControlType)
			{
				return RemoveAccessRule((AccessRule)rule);
			}
		}
		FileSystemAccessRule rule2 = new FileSystemAccessRule(rule.IdentityReference, FileSystemAccessRule.AccessMaskFromRights(rule.FileSystemRights, AccessControlType.Deny), rule.IsInherited, rule.InheritanceFlags, rule.PropagationFlags, rule.AccessControlType);
		return RemoveAccessRule((AccessRule)rule2);
	}

	public void RemoveAccessRuleAll(FileSystemAccessRule rule)
	{
		RemoveAccessRuleAll((AccessRule)rule);
	}

	public void RemoveAccessRuleSpecific(FileSystemAccessRule rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		AuthorizationRuleCollection accessRules = GetAccessRules(includeExplicit: true, includeInherited: true, rule.IdentityReference.GetType());
		for (int i = 0; i < accessRules.Count; i++)
		{
			if (accessRules[i] is FileSystemAccessRule fileSystemAccessRule && fileSystemAccessRule.FileSystemRights == rule.FileSystemRights && fileSystemAccessRule.IdentityReference == rule.IdentityReference && fileSystemAccessRule.AccessControlType == rule.AccessControlType)
			{
				RemoveAccessRuleSpecific((AccessRule)rule);
				return;
			}
		}
		FileSystemAccessRule rule2 = new FileSystemAccessRule(rule.IdentityReference, FileSystemAccessRule.AccessMaskFromRights(rule.FileSystemRights, AccessControlType.Deny), rule.IsInherited, rule.InheritanceFlags, rule.PropagationFlags, rule.AccessControlType);
		RemoveAccessRuleSpecific((AccessRule)rule2);
	}

	public void AddAuditRule(FileSystemAuditRule rule)
	{
		AddAuditRule((AuditRule)rule);
	}

	public void SetAuditRule(FileSystemAuditRule rule)
	{
		SetAuditRule((AuditRule)rule);
	}

	public bool RemoveAuditRule(FileSystemAuditRule rule)
	{
		return RemoveAuditRule((AuditRule)rule);
	}

	public void RemoveAuditRuleAll(FileSystemAuditRule rule)
	{
		RemoveAuditRuleAll((AuditRule)rule);
	}

	public void RemoveAuditRuleSpecific(FileSystemAuditRule rule)
	{
		RemoveAuditRuleSpecific((AuditRule)rule);
	}
}
