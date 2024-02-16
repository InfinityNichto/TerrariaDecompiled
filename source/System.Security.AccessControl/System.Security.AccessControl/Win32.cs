using System.Runtime.InteropServices;
using System.Security.Principal;

namespace System.Security.AccessControl;

internal static class Win32
{
	internal static int ConvertSdToSddl(byte[] binaryForm, int requestedRevision, SecurityInfos si, out string resultSddl)
	{
		uint resultStringLength = 0u;
		if (!global::Interop.Advapi32.ConvertSdToStringSd(binaryForm, (uint)requestedRevision, (uint)si, out var resultString, ref resultStringLength))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			resultSddl = null;
			if (lastWin32Error == 8)
			{
				throw new OutOfMemoryException();
			}
			return lastWin32Error;
		}
		resultSddl = Marshal.PtrToStringUni(resultString);
		Marshal.FreeHGlobal(resultString);
		return 0;
	}

	internal static int GetSecurityInfo(ResourceType resourceType, string name, SafeHandle handle, AccessControlSections accessControlSections, out RawSecurityDescriptor resultSd)
	{
		resultSd = null;
		SecurityInfos securityInfos = (SecurityInfos)0;
		Privilege privilege = null;
		if ((accessControlSections & AccessControlSections.Owner) != 0)
		{
			securityInfos |= SecurityInfos.Owner;
		}
		if ((accessControlSections & AccessControlSections.Group) != 0)
		{
			securityInfos |= SecurityInfos.Group;
		}
		if ((accessControlSections & AccessControlSections.Access) != 0)
		{
			securityInfos |= SecurityInfos.DiscretionaryAcl;
		}
		if ((accessControlSections & AccessControlSections.Audit) != 0)
		{
			securityInfos |= SecurityInfos.SystemAcl;
			privilege = new Privilege("SeSecurityPrivilege");
		}
		int num;
		IntPtr securityDescriptor;
		try
		{
			if (privilege != null)
			{
				try
				{
					privilege.Enable();
				}
				catch (PrivilegeNotHeldException)
				{
				}
			}
			IntPtr sidOwner;
			IntPtr sidGroup;
			IntPtr dacl;
			IntPtr sacl;
			if (name != null)
			{
				num = (int)global::Interop.Advapi32.GetSecurityInfoByName(name, (uint)resourceType, (uint)securityInfos, out sidOwner, out sidGroup, out dacl, out sacl, out securityDescriptor);
			}
			else
			{
				if (handle == null)
				{
					throw new ArgumentException();
				}
				if (handle.IsInvalid)
				{
					throw new ArgumentException(System.SR.Argument_InvalidSafeHandle, "handle");
				}
				num = (int)global::Interop.Advapi32.GetSecurityInfoByHandle(handle, (uint)resourceType, (uint)securityInfos, out sidOwner, out sidGroup, out dacl, out sacl, out securityDescriptor);
			}
			if (num == 0 && IntPtr.Zero.Equals(securityDescriptor))
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_NoSecurityDescriptor);
			}
			switch (num)
			{
			case 1300:
			case 1314:
				throw new PrivilegeNotHeldException("SeSecurityPrivilege");
			case 5:
			case 1347:
				throw new UnauthorizedAccessException();
			case 0:
				break;
			default:
				goto IL_013f;
			}
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
		uint securityDescriptorLength = global::Interop.Advapi32.GetSecurityDescriptorLength(securityDescriptor);
		byte[] array = new byte[securityDescriptorLength];
		Marshal.Copy(securityDescriptor, array, 0, (int)securityDescriptorLength);
		Marshal.FreeHGlobal(securityDescriptor);
		resultSd = new RawSecurityDescriptor(array, 0);
		return 0;
		IL_013f:
		if (num == 8)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}

	internal static int SetSecurityInfo(ResourceType type, string name, SafeHandle handle, SecurityInfos securityInformation, SecurityIdentifier owner, SecurityIdentifier group, GenericAcl sacl, GenericAcl dacl)
	{
		byte[] array = null;
		byte[] array2 = null;
		byte[] array3 = null;
		byte[] array4 = null;
		Privilege privilege = null;
		if (owner != null)
		{
			int binaryLength = owner.BinaryLength;
			array = new byte[binaryLength];
			owner.GetBinaryForm(array, 0);
		}
		if (group != null)
		{
			int binaryLength = group.BinaryLength;
			array2 = new byte[binaryLength];
			group.GetBinaryForm(array2, 0);
		}
		if (dacl != null)
		{
			int binaryLength = dacl.BinaryLength;
			array4 = new byte[binaryLength];
			dacl.GetBinaryForm(array4, 0);
		}
		if (sacl != null)
		{
			int binaryLength = sacl.BinaryLength;
			array3 = new byte[binaryLength];
			sacl.GetBinaryForm(array3, 0);
		}
		if ((securityInformation & SecurityInfos.SystemAcl) != 0)
		{
			privilege = new Privilege("SeSecurityPrivilege");
		}
		int num;
		try
		{
			if (privilege != null)
			{
				try
				{
					privilege.Enable();
				}
				catch (PrivilegeNotHeldException)
				{
				}
			}
			if (name != null)
			{
				num = (int)global::Interop.Advapi32.SetSecurityInfoByName(name, (uint)type, (uint)securityInformation, array, array2, array4, array3);
			}
			else
			{
				if (handle == null)
				{
					throw new ArgumentException();
				}
				if (handle.IsInvalid)
				{
					throw new ArgumentException(System.SR.Argument_InvalidSafeHandle, "handle");
				}
				num = (int)global::Interop.Advapi32.SetSecurityInfoByHandle(handle, (uint)type, (uint)securityInformation, array, array2, array4, array3);
			}
			switch (num)
			{
			case 1300:
			case 1314:
				throw new PrivilegeNotHeldException("SeSecurityPrivilege");
			case 5:
			case 1347:
				throw new UnauthorizedAccessException();
			case 0:
				break;
			default:
				goto IL_0145;
			}
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
		return 0;
		IL_0145:
		if (num == 8)
		{
			throw new OutOfMemoryException();
		}
		return num;
	}
}
