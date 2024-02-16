using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class CommonSecurityDescriptor : GenericSecurityDescriptor
{
	private bool _isContainer;

	private bool _isDS;

	private RawSecurityDescriptor _rawSd;

	private SystemAcl _sacl;

	private DiscretionaryAcl _dacl;

	internal sealed override GenericAcl? GenericSacl => _sacl;

	internal sealed override GenericAcl? GenericDacl => _dacl;

	public bool IsContainer => _isContainer;

	public bool IsDS => _isDS;

	public override ControlFlags ControlFlags => _rawSd.ControlFlags;

	public override SecurityIdentifier? Owner
	{
		get
		{
			return _rawSd.Owner;
		}
		set
		{
			_rawSd.Owner = value;
		}
	}

	public override SecurityIdentifier? Group
	{
		get
		{
			return _rawSd.Group;
		}
		set
		{
			_rawSd.Group = value;
		}
	}

	public SystemAcl? SystemAcl
	{
		get
		{
			return _sacl;
		}
		set
		{
			if (value != null)
			{
				if (value.IsContainer != IsContainer)
				{
					throw new ArgumentException(IsContainer ? System.SR.AccessControl_MustSpecifyContainerAcl : System.SR.AccessControl_MustSpecifyLeafObjectAcl, "value");
				}
				if (value.IsDS != IsDS)
				{
					throw new ArgumentException(IsDS ? System.SR.AccessControl_MustSpecifyDirectoryObjectAcl : System.SR.AccessControl_MustSpecifyNonDirectoryObjectAcl, "value");
				}
			}
			_sacl = value;
			if (_sacl != null)
			{
				_rawSd.SystemAcl = _sacl.RawAcl;
				AddControlFlags(ControlFlags.SystemAclPresent);
			}
			else
			{
				_rawSd.SystemAcl = null;
				RemoveControlFlags(ControlFlags.SystemAclPresent);
			}
		}
	}

	public DiscretionaryAcl? DiscretionaryAcl
	{
		get
		{
			return _dacl;
		}
		set
		{
			if (value != null)
			{
				if (value.IsContainer != IsContainer)
				{
					throw new ArgumentException(IsContainer ? System.SR.AccessControl_MustSpecifyContainerAcl : System.SR.AccessControl_MustSpecifyLeafObjectAcl, "value");
				}
				if (value.IsDS != IsDS)
				{
					throw new ArgumentException(IsDS ? System.SR.AccessControl_MustSpecifyDirectoryObjectAcl : System.SR.AccessControl_MustSpecifyNonDirectoryObjectAcl, "value");
				}
			}
			if (value == null)
			{
				_dacl = System.Security.AccessControl.DiscretionaryAcl.CreateAllowEveryoneFullAccess(IsDS, IsContainer);
			}
			else
			{
				_dacl = value;
			}
			_rawSd.DiscretionaryAcl = _dacl.RawAcl;
			AddControlFlags(ControlFlags.DiscretionaryAclPresent);
		}
	}

	public bool IsSystemAclCanonical
	{
		get
		{
			if (SystemAcl != null)
			{
				return SystemAcl.IsCanonical;
			}
			return true;
		}
	}

	public bool IsDiscretionaryAclCanonical
	{
		get
		{
			if (DiscretionaryAcl != null)
			{
				return DiscretionaryAcl.IsCanonical;
			}
			return true;
		}
	}

	internal bool IsSystemAclPresent => (_rawSd.ControlFlags & ControlFlags.SystemAclPresent) != 0;

	internal bool IsDiscretionaryAclPresent => (_rawSd.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0;

	[MemberNotNull("_rawSd")]
	private void CreateFromParts(bool isContainer, bool isDS, ControlFlags flags, SecurityIdentifier owner, SecurityIdentifier group, SystemAcl systemAcl, DiscretionaryAcl discretionaryAcl)
	{
		if (systemAcl != null && systemAcl.IsContainer != isContainer)
		{
			throw new ArgumentException(isContainer ? System.SR.AccessControl_MustSpecifyContainerAcl : System.SR.AccessControl_MustSpecifyLeafObjectAcl, "systemAcl");
		}
		if (discretionaryAcl != null && discretionaryAcl.IsContainer != isContainer)
		{
			throw new ArgumentException(isContainer ? System.SR.AccessControl_MustSpecifyContainerAcl : System.SR.AccessControl_MustSpecifyLeafObjectAcl, "discretionaryAcl");
		}
		_isContainer = isContainer;
		if (systemAcl != null && systemAcl.IsDS != isDS)
		{
			throw new ArgumentException(isDS ? System.SR.AccessControl_MustSpecifyDirectoryObjectAcl : System.SR.AccessControl_MustSpecifyNonDirectoryObjectAcl, "systemAcl");
		}
		if (discretionaryAcl != null && discretionaryAcl.IsDS != isDS)
		{
			throw new ArgumentException(isDS ? System.SR.AccessControl_MustSpecifyDirectoryObjectAcl : System.SR.AccessControl_MustSpecifyNonDirectoryObjectAcl, "discretionaryAcl");
		}
		_isDS = isDS;
		_sacl = systemAcl;
		if (discretionaryAcl == null)
		{
			discretionaryAcl = System.Security.AccessControl.DiscretionaryAcl.CreateAllowEveryoneFullAccess(_isDS, _isContainer);
		}
		_dacl = discretionaryAcl;
		ControlFlags controlFlags = flags | ControlFlags.DiscretionaryAclPresent;
		controlFlags = ((systemAcl != null) ? (controlFlags | ControlFlags.SystemAclPresent) : (controlFlags & ~ControlFlags.SystemAclPresent));
		_rawSd = new RawSecurityDescriptor(controlFlags, owner, group, systemAcl?.RawAcl, discretionaryAcl.RawAcl);
	}

	public CommonSecurityDescriptor(bool isContainer, bool isDS, ControlFlags flags, SecurityIdentifier? owner, SecurityIdentifier? group, SystemAcl? systemAcl, DiscretionaryAcl? discretionaryAcl)
	{
		CreateFromParts(isContainer, isDS, flags, owner, group, systemAcl, discretionaryAcl);
	}

	public CommonSecurityDescriptor(bool isContainer, bool isDS, RawSecurityDescriptor rawSecurityDescriptor)
		: this(isContainer, isDS, rawSecurityDescriptor, trusted: false)
	{
	}

	internal CommonSecurityDescriptor(bool isContainer, bool isDS, RawSecurityDescriptor rawSecurityDescriptor, bool trusted)
	{
		if (rawSecurityDescriptor == null)
		{
			throw new ArgumentNullException("rawSecurityDescriptor");
		}
		CreateFromParts(isContainer, isDS, rawSecurityDescriptor.ControlFlags, rawSecurityDescriptor.Owner, rawSecurityDescriptor.Group, (rawSecurityDescriptor.SystemAcl == null) ? null : new SystemAcl(isContainer, isDS, rawSecurityDescriptor.SystemAcl, trusted), (rawSecurityDescriptor.DiscretionaryAcl == null) ? null : new DiscretionaryAcl(isContainer, isDS, rawSecurityDescriptor.DiscretionaryAcl, trusted));
	}

	public CommonSecurityDescriptor(bool isContainer, bool isDS, string sddlForm)
		: this(isContainer, isDS, new RawSecurityDescriptor(sddlForm), trusted: true)
	{
	}

	public CommonSecurityDescriptor(bool isContainer, bool isDS, byte[] binaryForm, int offset)
		: this(isContainer, isDS, new RawSecurityDescriptor(binaryForm, offset), trusted: true)
	{
	}

	public void SetSystemAclProtection(bool isProtected, bool preserveInheritance)
	{
		if (!isProtected)
		{
			RemoveControlFlags(ControlFlags.SystemAclProtected);
			return;
		}
		if (!preserveInheritance && SystemAcl != null)
		{
			SystemAcl.RemoveInheritedAces();
		}
		AddControlFlags(ControlFlags.SystemAclProtected);
	}

	public void SetDiscretionaryAclProtection(bool isProtected, bool preserveInheritance)
	{
		if (!isProtected)
		{
			RemoveControlFlags(ControlFlags.DiscretionaryAclProtected);
		}
		else
		{
			if (!preserveInheritance && DiscretionaryAcl != null)
			{
				DiscretionaryAcl.RemoveInheritedAces();
			}
			AddControlFlags(ControlFlags.DiscretionaryAclProtected);
		}
		if (DiscretionaryAcl != null && DiscretionaryAcl.EveryOneFullAccessForNullDacl)
		{
			DiscretionaryAcl.EveryOneFullAccessForNullDacl = false;
		}
	}

	public void PurgeAccessControl(SecurityIdentifier sid)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		if (DiscretionaryAcl != null)
		{
			DiscretionaryAcl.Purge(sid);
		}
	}

	public void PurgeAudit(SecurityIdentifier sid)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		if (SystemAcl != null)
		{
			SystemAcl.Purge(sid);
		}
	}

	public void AddDiscretionaryAcl(byte revision, int trusted)
	{
		DiscretionaryAcl = new DiscretionaryAcl(IsContainer, IsDS, revision, trusted);
		AddControlFlags(ControlFlags.DiscretionaryAclPresent);
	}

	public void AddSystemAcl(byte revision, int trusted)
	{
		SystemAcl = new SystemAcl(IsContainer, IsDS, revision, trusted);
		AddControlFlags(ControlFlags.SystemAclPresent);
	}

	internal void UpdateControlFlags(ControlFlags flagsToUpdate, ControlFlags newFlags)
	{
		ControlFlags flags = newFlags | (_rawSd.ControlFlags & ~flagsToUpdate);
		_rawSd.SetFlags(flags);
	}

	internal void AddControlFlags(ControlFlags flags)
	{
		_rawSd.SetFlags(_rawSd.ControlFlags | flags);
	}

	internal void RemoveControlFlags(ControlFlags flags)
	{
		_rawSd.SetFlags(_rawSd.ControlFlags & ~flags);
	}
}
