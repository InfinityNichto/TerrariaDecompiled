using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class FileSystemAccessRule : AccessRule
{
	public FileSystemRights FileSystemRights => RightsFromAccessMask(base.AccessMask);

	public FileSystemAccessRule(IdentityReference identity, FileSystemRights fileSystemRights, AccessControlType type)
		: this(identity, AccessMaskFromRights(fileSystemRights, type), isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}

	public FileSystemAccessRule(string identity, FileSystemRights fileSystemRights, AccessControlType type)
		: this(new NTAccount(identity), AccessMaskFromRights(fileSystemRights, type), isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}

	public FileSystemAccessRule(IdentityReference identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: this(identity, AccessMaskFromRights(fileSystemRights, type), isInherited: false, inheritanceFlags, propagationFlags, type)
	{
	}

	public FileSystemAccessRule(string identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: this(new NTAccount(identity), AccessMaskFromRights(fileSystemRights, type), isInherited: false, inheritanceFlags, propagationFlags, type)
	{
	}

	internal FileSystemAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
	{
	}

	internal static int AccessMaskFromRights(FileSystemRights fileSystemRights, AccessControlType controlType)
	{
		if (fileSystemRights < (FileSystemRights)0 || fileSystemRights > FileSystemRights.FullControl)
		{
			throw new ArgumentOutOfRangeException("fileSystemRights", System.SR.Format(System.SR.Argument_InvalidEnumValue, fileSystemRights, "FileSystemRights"));
		}
		switch (controlType)
		{
		case AccessControlType.Allow:
			fileSystemRights |= FileSystemRights.Synchronize;
			break;
		case AccessControlType.Deny:
			if (fileSystemRights != FileSystemRights.FullControl && fileSystemRights != (FileSystemRights.Modify | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership | FileSystemRights.Synchronize))
			{
				fileSystemRights &= ~FileSystemRights.Synchronize;
			}
			break;
		}
		return (int)fileSystemRights;
	}

	internal static FileSystemRights RightsFromAccessMask(int accessMask)
	{
		return (FileSystemRights)accessMask;
	}
}
