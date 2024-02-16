using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class GenericSecurityDescriptor
{
	internal abstract GenericAcl? GenericSacl { get; }

	internal abstract GenericAcl? GenericDacl { get; }

	private bool IsCraftedAefaDacl
	{
		get
		{
			if (GenericDacl is DiscretionaryAcl discretionaryAcl)
			{
				return discretionaryAcl.EveryOneFullAccessForNullDacl;
			}
			return false;
		}
	}

	public static byte Revision => 1;

	public abstract ControlFlags ControlFlags { get; }

	public abstract SecurityIdentifier? Owner { get; set; }

	public abstract SecurityIdentifier? Group { get; set; }

	public int BinaryLength
	{
		get
		{
			int num = 20;
			if (Owner != null)
			{
				num += Owner.BinaryLength;
			}
			if (Group != null)
			{
				num += Group.BinaryLength;
			}
			if ((ControlFlags & ControlFlags.SystemAclPresent) != 0 && GenericSacl != null)
			{
				num += GenericSacl.BinaryLength;
			}
			if ((ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0 && GenericDacl != null && !IsCraftedAefaDacl)
			{
				num += GenericDacl.BinaryLength;
			}
			return num;
		}
	}

	private static void MarshalInt(byte[] binaryForm, int offset, int number)
	{
		binaryForm[offset] = (byte)number;
		binaryForm[offset + 1] = (byte)(number >> 8);
		binaryForm[offset + 2] = (byte)(number >> 16);
		binaryForm[offset + 3] = (byte)(number >> 24);
	}

	internal static int UnmarshalInt(byte[] binaryForm, int offset)
	{
		return binaryForm[offset] + (binaryForm[offset + 1] << 8) + (binaryForm[offset + 2] << 16) + (binaryForm[offset + 3] << 24);
	}

	internal GenericSecurityDescriptor()
	{
	}

	public static bool IsSddlConversionSupported()
	{
		return true;
	}

	public string GetSddlForm(AccessControlSections includeSections)
	{
		byte[] binaryForm = new byte[BinaryLength];
		GetBinaryForm(binaryForm, 0);
		SecurityInfos securityInfos = (SecurityInfos)0;
		if ((includeSections & AccessControlSections.Owner) != 0)
		{
			securityInfos |= SecurityInfos.Owner;
		}
		if ((includeSections & AccessControlSections.Group) != 0)
		{
			securityInfos |= SecurityInfos.Group;
		}
		if ((includeSections & AccessControlSections.Audit) != 0)
		{
			securityInfos |= SecurityInfos.SystemAcl;
		}
		if ((includeSections & AccessControlSections.Access) != 0)
		{
			securityInfos |= SecurityInfos.DiscretionaryAcl;
		}
		string resultSddl;
		switch (Win32.ConvertSdToSddl(binaryForm, 1, securityInfos, out resultSddl))
		{
		case 87:
		case 1305:
			throw new InvalidOperationException();
		default:
			throw new InvalidOperationException();
		case 0:
			return resultSddl;
		}
	}

	public void GetBinaryForm(byte[] binaryForm, int offset)
	{
		if (binaryForm == null)
		{
			throw new ArgumentNullException("binaryForm");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (binaryForm.Length - offset < BinaryLength)
		{
			throw new ArgumentOutOfRangeException("binaryForm", System.SR.ArgumentOutOfRange_ArrayTooSmall);
		}
		int num = offset;
		byte b = (byte)((this is RawSecurityDescriptor rawSecurityDescriptor && (ControlFlags & ControlFlags.RMControlValid) != 0) ? rawSecurityDescriptor.ResourceManagerControl : 0);
		int num2 = (int)ControlFlags;
		if (IsCraftedAefaDacl)
		{
			num2 &= -5;
		}
		binaryForm[offset] = Revision;
		binaryForm[offset + 1] = b;
		binaryForm[offset + 2] = (byte)num2;
		binaryForm[offset + 3] = (byte)(num2 >> 8);
		int offset2 = offset + 4;
		int offset3 = offset + 8;
		int offset4 = offset + 12;
		int offset5 = offset + 16;
		offset += 20;
		if (Owner != null)
		{
			MarshalInt(binaryForm, offset2, offset - num);
			Owner.GetBinaryForm(binaryForm, offset);
			offset += Owner.BinaryLength;
		}
		else
		{
			MarshalInt(binaryForm, offset2, 0);
		}
		if (Group != null)
		{
			MarshalInt(binaryForm, offset3, offset - num);
			Group.GetBinaryForm(binaryForm, offset);
			offset += Group.BinaryLength;
		}
		else
		{
			MarshalInt(binaryForm, offset3, 0);
		}
		if ((ControlFlags & ControlFlags.SystemAclPresent) != 0 && GenericSacl != null)
		{
			MarshalInt(binaryForm, offset4, offset - num);
			GenericSacl.GetBinaryForm(binaryForm, offset);
			offset += GenericSacl.BinaryLength;
		}
		else
		{
			MarshalInt(binaryForm, offset4, 0);
		}
		if ((ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0 && GenericDacl != null && !IsCraftedAefaDacl)
		{
			MarshalInt(binaryForm, offset5, offset - num);
			GenericDacl.GetBinaryForm(binaryForm, offset);
			offset += GenericDacl.BinaryLength;
		}
		else
		{
			MarshalInt(binaryForm, offset5, 0);
		}
	}
}
