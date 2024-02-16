using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace System.Security.AccessControl;

public abstract class ObjectSecurity
{
	private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

	internal readonly CommonSecurityDescriptor _securityDescriptor;

	private bool _ownerModified;

	private bool _groupModified;

	private bool _saclModified;

	private bool _daclModified;

	protected CommonSecurityDescriptor SecurityDescriptor => _securityDescriptor;

	protected bool OwnerModified
	{
		get
		{
			if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForReadOrWrite);
			}
			return _ownerModified;
		}
		set
		{
			if (!_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForWrite);
			}
			_ownerModified = value;
		}
	}

	protected bool GroupModified
	{
		get
		{
			if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForReadOrWrite);
			}
			return _groupModified;
		}
		set
		{
			if (!_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForWrite);
			}
			_groupModified = value;
		}
	}

	protected bool AuditRulesModified
	{
		get
		{
			if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForReadOrWrite);
			}
			return _saclModified;
		}
		set
		{
			if (!_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForWrite);
			}
			_saclModified = value;
		}
	}

	protected bool AccessRulesModified
	{
		get
		{
			if (!_lock.IsReadLockHeld && !_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForReadOrWrite);
			}
			return _daclModified;
		}
		set
		{
			if (!_lock.IsWriteLockHeld)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_MustLockForWrite);
			}
			_daclModified = value;
		}
	}

	protected bool IsContainer => _securityDescriptor.IsContainer;

	protected bool IsDS => _securityDescriptor.IsDS;

	public bool AreAccessRulesProtected
	{
		get
		{
			ReadLock();
			try
			{
				return (_securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0;
			}
			finally
			{
				ReadUnlock();
			}
		}
	}

	public bool AreAuditRulesProtected
	{
		get
		{
			ReadLock();
			try
			{
				return (_securityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) != 0;
			}
			finally
			{
				ReadUnlock();
			}
		}
	}

	public bool AreAccessRulesCanonical
	{
		get
		{
			ReadLock();
			try
			{
				return _securityDescriptor.IsDiscretionaryAclCanonical;
			}
			finally
			{
				ReadUnlock();
			}
		}
	}

	public bool AreAuditRulesCanonical
	{
		get
		{
			ReadLock();
			try
			{
				return _securityDescriptor.IsSystemAclCanonical;
			}
			finally
			{
				ReadUnlock();
			}
		}
	}

	public abstract Type AccessRightType { get; }

	public abstract Type AccessRuleType { get; }

	public abstract Type AuditRuleType { get; }

	protected ObjectSecurity()
	{
	}

	protected ObjectSecurity(bool isContainer, bool isDS)
		: this()
	{
		DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, 5);
		_securityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, ControlFlags.None, null, null, null, discretionaryAcl);
	}

	protected ObjectSecurity(CommonSecurityDescriptor securityDescriptor)
		: this()
	{
		if (securityDescriptor == null)
		{
			throw new ArgumentNullException("securityDescriptor");
		}
		_securityDescriptor = securityDescriptor;
	}

	private void UpdateWithNewSecurityDescriptor(RawSecurityDescriptor newOne, AccessControlSections includeSections)
	{
		if ((includeSections & AccessControlSections.Owner) != 0)
		{
			_ownerModified = true;
			_securityDescriptor.Owner = newOne.Owner;
		}
		if ((includeSections & AccessControlSections.Group) != 0)
		{
			_groupModified = true;
			_securityDescriptor.Group = newOne.Group;
		}
		if ((includeSections & AccessControlSections.Audit) != 0)
		{
			_saclModified = true;
			if (newOne.SystemAcl != null)
			{
				_securityDescriptor.SystemAcl = new SystemAcl(IsContainer, IsDS, newOne.SystemAcl, trusted: true);
			}
			else
			{
				_securityDescriptor.SystemAcl = null;
			}
			_securityDescriptor.UpdateControlFlags(ControlFlags.SystemAclPresent | ControlFlags.SystemAclAutoInherited | ControlFlags.SystemAclProtected, newOne.ControlFlags & (ControlFlags.SystemAclPresent | ControlFlags.SystemAclAutoInherited | ControlFlags.SystemAclProtected));
		}
		if ((includeSections & AccessControlSections.Access) != 0)
		{
			_daclModified = true;
			if (newOne.DiscretionaryAcl != null)
			{
				_securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, newOne.DiscretionaryAcl, trusted: true);
			}
			else
			{
				_securityDescriptor.DiscretionaryAcl = null;
			}
			ControlFlags controlFlags = _securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent;
			_securityDescriptor.UpdateControlFlags(ControlFlags.DiscretionaryAclPresent | ControlFlags.DiscretionaryAclAutoInherited | ControlFlags.DiscretionaryAclProtected, (newOne.ControlFlags | controlFlags) & (ControlFlags.DiscretionaryAclPresent | ControlFlags.DiscretionaryAclAutoInherited | ControlFlags.DiscretionaryAclProtected));
		}
	}

	protected void ReadLock()
	{
		_lock.EnterReadLock();
	}

	protected void ReadUnlock()
	{
		_lock.ExitReadLock();
	}

	protected void WriteLock()
	{
		_lock.EnterWriteLock();
	}

	protected void WriteUnlock()
	{
		_lock.ExitWriteLock();
	}

	protected virtual void Persist(string name, AccessControlSections includeSections)
	{
		throw System.NotImplemented.ByDesign;
	}

	protected virtual void Persist(bool enableOwnershipPrivilege, string name, AccessControlSections includeSections)
	{
		Privilege privilege = null;
		try
		{
			if (enableOwnershipPrivilege)
			{
				privilege = new Privilege("SeTakeOwnershipPrivilege");
				try
				{
					privilege.Enable();
				}
				catch (PrivilegeNotHeldException)
				{
				}
			}
			Persist(name, includeSections);
		}
		catch
		{
			privilege?.Revert();
			throw;
		}
		finally
		{
			privilege?.Revert();
		}
	}

	protected virtual void Persist(SafeHandle handle, AccessControlSections includeSections)
	{
		throw System.NotImplemented.ByDesign;
	}

	public IdentityReference? GetOwner(Type targetType)
	{
		ReadLock();
		try
		{
			if (_securityDescriptor.Owner == null)
			{
				return null;
			}
			return _securityDescriptor.Owner.Translate(targetType);
		}
		finally
		{
			ReadUnlock();
		}
	}

	public void SetOwner(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		WriteLock();
		try
		{
			_securityDescriptor.Owner = identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
			_ownerModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public IdentityReference? GetGroup(Type targetType)
	{
		ReadLock();
		try
		{
			if (_securityDescriptor.Group == null)
			{
				return null;
			}
			return _securityDescriptor.Group.Translate(targetType);
		}
		finally
		{
			ReadUnlock();
		}
	}

	public void SetGroup(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		WriteLock();
		try
		{
			_securityDescriptor.Group = identity.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
			_groupModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public virtual void PurgeAccessRules(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		WriteLock();
		try
		{
			_securityDescriptor.PurgeAccessControl((SecurityIdentifier)identity.Translate(typeof(SecurityIdentifier)));
			_daclModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public virtual void PurgeAuditRules(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		WriteLock();
		try
		{
			_securityDescriptor.PurgeAudit((SecurityIdentifier)identity.Translate(typeof(SecurityIdentifier)));
			_saclModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public void SetAccessRuleProtection(bool isProtected, bool preserveInheritance)
	{
		WriteLock();
		try
		{
			_securityDescriptor.SetDiscretionaryAclProtection(isProtected, preserveInheritance);
			_daclModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public void SetAuditRuleProtection(bool isProtected, bool preserveInheritance)
	{
		WriteLock();
		try
		{
			_securityDescriptor.SetSystemAclProtection(isProtected, preserveInheritance);
			_saclModified = true;
		}
		finally
		{
			WriteUnlock();
		}
	}

	public static bool IsSddlConversionSupported()
	{
		return true;
	}

	public string GetSecurityDescriptorSddlForm(AccessControlSections includeSections)
	{
		ReadLock();
		try
		{
			return _securityDescriptor.GetSddlForm(includeSections);
		}
		finally
		{
			ReadUnlock();
		}
	}

	public void SetSecurityDescriptorSddlForm(string sddlForm)
	{
		SetSecurityDescriptorSddlForm(sddlForm, AccessControlSections.All);
	}

	public void SetSecurityDescriptorSddlForm(string sddlForm, AccessControlSections includeSections)
	{
		if (sddlForm == null)
		{
			throw new ArgumentNullException("sddlForm");
		}
		if ((includeSections & AccessControlSections.All) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "includeSections");
		}
		WriteLock();
		try
		{
			UpdateWithNewSecurityDescriptor(new RawSecurityDescriptor(sddlForm), includeSections);
		}
		finally
		{
			WriteUnlock();
		}
	}

	public byte[] GetSecurityDescriptorBinaryForm()
	{
		ReadLock();
		try
		{
			byte[] array = new byte[_securityDescriptor.BinaryLength];
			_securityDescriptor.GetBinaryForm(array, 0);
			return array;
		}
		finally
		{
			ReadUnlock();
		}
	}

	public void SetSecurityDescriptorBinaryForm(byte[] binaryForm)
	{
		SetSecurityDescriptorBinaryForm(binaryForm, AccessControlSections.All);
	}

	public void SetSecurityDescriptorBinaryForm(byte[] binaryForm, AccessControlSections includeSections)
	{
		if (binaryForm == null)
		{
			throw new ArgumentNullException("binaryForm");
		}
		if ((includeSections & AccessControlSections.All) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "includeSections");
		}
		WriteLock();
		try
		{
			UpdateWithNewSecurityDescriptor(new RawSecurityDescriptor(binaryForm, 0), includeSections);
		}
		finally
		{
			WriteUnlock();
		}
	}

	protected abstract bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified);

	protected abstract bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified);

	public virtual bool ModifyAccessRule(AccessControlModification modification, AccessRule rule, out bool modified)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		if (!AccessRuleType.IsAssignableFrom(rule.GetType()))
		{
			throw new ArgumentException(System.SR.AccessControl_InvalidAccessRuleType, "rule");
		}
		WriteLock();
		try
		{
			return ModifyAccess(modification, rule, out modified);
		}
		finally
		{
			WriteUnlock();
		}
	}

	public virtual bool ModifyAuditRule(AccessControlModification modification, AuditRule rule, out bool modified)
	{
		if (rule == null)
		{
			throw new ArgumentNullException("rule");
		}
		if (!AuditRuleType.IsAssignableFrom(rule.GetType()))
		{
			throw new ArgumentException(System.SR.AccessControl_InvalidAuditRuleType, "rule");
		}
		WriteLock();
		try
		{
			return ModifyAudit(modification, rule, out modified);
		}
		finally
		{
			WriteUnlock();
		}
	}

	public abstract AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);

	public abstract AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
}
public abstract class ObjectSecurity<T> : NativeObjectSecurity where T : struct
{
	public override Type AccessRightType => typeof(T);

	public override Type AccessRuleType => typeof(AccessRule<T>);

	public override Type AuditRuleType => typeof(AuditRule<T>);

	protected ObjectSecurity(bool isContainer, ResourceType resourceType)
		: base(isContainer, resourceType, null, null)
	{
	}

	protected ObjectSecurity(bool isContainer, ResourceType resourceType, string? name, AccessControlSections includeSections)
		: base(isContainer, resourceType, name, includeSections, null, null)
	{
	}

	protected ObjectSecurity(bool isContainer, ResourceType resourceType, string? name, AccessControlSections includeSections, ExceptionFromErrorCode? exceptionFromErrorCode, object? exceptionContext)
		: base(isContainer, resourceType, name, includeSections, exceptionFromErrorCode, exceptionContext)
	{
	}

	protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle? safeHandle, AccessControlSections includeSections)
		: base(isContainer, resourceType, safeHandle, includeSections, null, null)
	{
	}

	protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle? safeHandle, AccessControlSections includeSections, ExceptionFromErrorCode? exceptionFromErrorCode, object? exceptionContext)
		: base(isContainer, resourceType, safeHandle, includeSections, exceptionFromErrorCode, exceptionContext)
	{
	}

	public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
	{
		return new AccessRule<T>(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
	}

	public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
	{
		return new AuditRule<T>(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
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

	public virtual void AddAccessRule(AccessRule<T> rule)
	{
		AddAccessRule((AccessRule)rule);
	}

	public virtual void SetAccessRule(AccessRule<T> rule)
	{
		SetAccessRule((AccessRule)rule);
	}

	public virtual void ResetAccessRule(AccessRule<T> rule)
	{
		ResetAccessRule((AccessRule)rule);
	}

	public virtual bool RemoveAccessRule(AccessRule<T> rule)
	{
		return RemoveAccessRule((AccessRule)rule);
	}

	public virtual void RemoveAccessRuleAll(AccessRule<T> rule)
	{
		RemoveAccessRuleAll((AccessRule)rule);
	}

	public virtual void RemoveAccessRuleSpecific(AccessRule<T> rule)
	{
		RemoveAccessRuleSpecific((AccessRule)rule);
	}

	public virtual void AddAuditRule(AuditRule<T> rule)
	{
		AddAuditRule((AuditRule)rule);
	}

	public virtual void SetAuditRule(AuditRule<T> rule)
	{
		SetAuditRule((AuditRule)rule);
	}

	public virtual bool RemoveAuditRule(AuditRule<T> rule)
	{
		return RemoveAuditRule((AuditRule)rule);
	}

	public virtual void RemoveAuditRuleAll(AuditRule<T> rule)
	{
		RemoveAuditRuleAll((AuditRule)rule);
	}

	public virtual void RemoveAuditRuleSpecific(AuditRule<T> rule)
	{
		RemoveAuditRuleSpecific((AuditRule)rule);
	}
}
