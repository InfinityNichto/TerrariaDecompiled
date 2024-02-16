using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class FileSystemAuditRule : AuditRule
{
	public FileSystemRights FileSystemRights => FileSystemAccessRule.RightsFromAccessMask(base.AccessMask);

	public FileSystemAuditRule(IdentityReference identity, FileSystemRights fileSystemRights, AuditFlags flags)
		: this(identity, fileSystemRights, InheritanceFlags.None, PropagationFlags.None, flags)
	{
	}

	public FileSystemAuditRule(IdentityReference identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: this(identity, AccessMaskFromRights(fileSystemRights), isInherited: false, inheritanceFlags, propagationFlags, flags)
	{
	}

	public FileSystemAuditRule(string identity, FileSystemRights fileSystemRights, AuditFlags flags)
		: this(new NTAccount(identity), fileSystemRights, InheritanceFlags.None, PropagationFlags.None, flags)
	{
	}

	public FileSystemAuditRule(string identity, FileSystemRights fileSystemRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: this(new NTAccount(identity), AccessMaskFromRights(fileSystemRights), isInherited: false, inheritanceFlags, propagationFlags, flags)
	{
	}

	internal FileSystemAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
		: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
	{
	}

	private static int AccessMaskFromRights(FileSystemRights fileSystemRights)
	{
		if (fileSystemRights < (FileSystemRights)0 || fileSystemRights > FileSystemRights.FullControl)
		{
			throw new ArgumentOutOfRangeException("fileSystemRights", System.SR.Format(System.SR.Argument_InvalidEnumValue, fileSystemRights, "FileSystemRights"));
		}
		return (int)fileSystemRights;
	}
}
