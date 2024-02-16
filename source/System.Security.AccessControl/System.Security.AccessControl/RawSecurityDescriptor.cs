using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class RawSecurityDescriptor : GenericSecurityDescriptor
{
	private SecurityIdentifier _owner;

	private SecurityIdentifier _group;

	private ControlFlags _flags;

	private RawAcl _sacl;

	private RawAcl _dacl;

	private byte _rmControl;

	internal override GenericAcl? GenericSacl => _sacl;

	internal override GenericAcl? GenericDacl => _dacl;

	public override ControlFlags ControlFlags => _flags;

	public override SecurityIdentifier? Owner
	{
		get
		{
			return _owner;
		}
		set
		{
			_owner = value;
		}
	}

	public override SecurityIdentifier? Group
	{
		get
		{
			return _group;
		}
		set
		{
			_group = value;
		}
	}

	public RawAcl? SystemAcl
	{
		get
		{
			return _sacl;
		}
		set
		{
			_sacl = value;
		}
	}

	public RawAcl? DiscretionaryAcl
	{
		get
		{
			return _dacl;
		}
		set
		{
			_dacl = value;
		}
	}

	public byte ResourceManagerControl
	{
		get
		{
			return _rmControl;
		}
		set
		{
			_rmControl = value;
		}
	}

	private void CreateFromParts(ControlFlags flags, SecurityIdentifier owner, SecurityIdentifier group, RawAcl systemAcl, RawAcl discretionaryAcl)
	{
		SetFlags(flags);
		Owner = owner;
		Group = group;
		SystemAcl = systemAcl;
		DiscretionaryAcl = discretionaryAcl;
		ResourceManagerControl = 0;
	}

	public RawSecurityDescriptor(ControlFlags flags, SecurityIdentifier? owner, SecurityIdentifier? group, RawAcl? systemAcl, RawAcl? discretionaryAcl)
	{
		CreateFromParts(flags, owner, group, systemAcl, discretionaryAcl);
	}

	public RawSecurityDescriptor(string sddlForm)
		: this(BinaryFormFromSddlForm(sddlForm), 0)
	{
	}

	public RawSecurityDescriptor(byte[] binaryForm, int offset)
	{
		if (binaryForm == null)
		{
			throw new ArgumentNullException("binaryForm");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (binaryForm.Length - offset < 20)
		{
			throw new ArgumentOutOfRangeException("binaryForm", System.SR.ArgumentOutOfRange_ArrayTooSmall);
		}
		if (binaryForm[offset] != GenericSecurityDescriptor.Revision)
		{
			throw new ArgumentOutOfRangeException("binaryForm", System.SR.AccessControl_InvalidSecurityDescriptorRevision);
		}
		byte resourceManagerControl = binaryForm[offset + 1];
		ControlFlags controlFlags = (ControlFlags)(binaryForm[offset + 2] + (binaryForm[offset + 3] << 8));
		if ((controlFlags & ControlFlags.SelfRelative) == 0)
		{
			throw new ArgumentException(System.SR.AccessControl_InvalidSecurityDescriptorSelfRelativeForm, "binaryForm");
		}
		int num = GenericSecurityDescriptor.UnmarshalInt(binaryForm, offset + 4);
		SecurityIdentifier owner = ((num == 0) ? null : new SecurityIdentifier(binaryForm, offset + num));
		int num2 = GenericSecurityDescriptor.UnmarshalInt(binaryForm, offset + 8);
		SecurityIdentifier group = ((num2 == 0) ? null : new SecurityIdentifier(binaryForm, offset + num2));
		int num3 = GenericSecurityDescriptor.UnmarshalInt(binaryForm, offset + 12);
		RawAcl systemAcl = (((controlFlags & ControlFlags.SystemAclPresent) == 0 || num3 == 0) ? null : new RawAcl(binaryForm, offset + num3));
		int num4 = GenericSecurityDescriptor.UnmarshalInt(binaryForm, offset + 16);
		CreateFromParts(controlFlags, owner, group, systemAcl, ((controlFlags & ControlFlags.DiscretionaryAclPresent) == 0 || num4 == 0) ? null : new RawAcl(binaryForm, offset + num4));
		if ((controlFlags & ControlFlags.RMControlValid) != 0)
		{
			ResourceManagerControl = resourceManagerControl;
		}
	}

	private static byte[] BinaryFormFromSddlForm(string sddlForm)
	{
		if (sddlForm == null)
		{
			throw new ArgumentNullException("sddlForm");
		}
		IntPtr resultSd = IntPtr.Zero;
		uint resultSdLength = 0u;
		byte[] array = null;
		try
		{
			if (!global::Interop.Advapi32.ConvertStringSdToSd(sddlForm, GenericSecurityDescriptor.Revision, out resultSd, ref resultSdLength))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				switch (lastWin32Error)
				{
				case 87:
				case 1305:
				case 1336:
				case 1338:
					throw new ArgumentException(System.SR.ArgumentException_InvalidSDSddlForm, "sddlForm");
				case 8:
					throw new OutOfMemoryException();
				case 1337:
					throw new ArgumentException(System.SR.AccessControl_InvalidSidInSDDLString, "sddlForm");
				default:
					throw new Win32Exception(lastWin32Error, System.SR.Format(System.SR.AccessControl_UnexpectedError, lastWin32Error));
				case 0:
					break;
				}
			}
			array = new byte[resultSdLength];
			Marshal.Copy(resultSd, array, 0, (int)resultSdLength);
			return array;
		}
		finally
		{
			if (resultSd != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(resultSd);
			}
		}
	}

	public void SetFlags(ControlFlags flags)
	{
		_flags = flags | ControlFlags.SelfRelative;
	}
}
