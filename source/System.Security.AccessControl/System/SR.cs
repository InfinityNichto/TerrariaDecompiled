using System.Resources;
using FxResources.System.Security.AccessControl;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string AccessControl_AclTooLong => GetResourceString("AccessControl_AclTooLong");

	internal static string AccessControl_InvalidAccessRuleType => GetResourceString("AccessControl_InvalidAccessRuleType");

	internal static string AccessControl_InvalidAuditRuleType => GetResourceString("AccessControl_InvalidAuditRuleType");

	internal static string AccessControl_InvalidOwner => GetResourceString("AccessControl_InvalidOwner");

	internal static string AccessControl_InvalidGroup => GetResourceString("AccessControl_InvalidGroup");

	internal static string AccessControl_InvalidHandle => GetResourceString("AccessControl_InvalidHandle");

	internal static string AccessControl_InvalidSecurityDescriptorRevision => GetResourceString("AccessControl_InvalidSecurityDescriptorRevision");

	internal static string AccessControl_InvalidSecurityDescriptorSelfRelativeForm => GetResourceString("AccessControl_InvalidSecurityDescriptorSelfRelativeForm");

	internal static string AccessControl_InvalidSidInSDDLString => GetResourceString("AccessControl_InvalidSidInSDDLString");

	internal static string AccessControl_MustSpecifyContainerAcl => GetResourceString("AccessControl_MustSpecifyContainerAcl");

	internal static string AccessControl_MustSpecifyDirectoryObjectAcl => GetResourceString("AccessControl_MustSpecifyDirectoryObjectAcl");

	internal static string AccessControl_MustSpecifyLeafObjectAcl => GetResourceString("AccessControl_MustSpecifyLeafObjectAcl");

	internal static string AccessControl_MustSpecifyNonDirectoryObjectAcl => GetResourceString("AccessControl_MustSpecifyNonDirectoryObjectAcl");

	internal static string AccessControl_NoAssociatedSecurity => GetResourceString("AccessControl_NoAssociatedSecurity");

	internal static string AccessControl_UnexpectedError => GetResourceString("AccessControl_UnexpectedError");

	internal static string Arg_EnumAtLeastOneFlag => GetResourceString("Arg_EnumAtLeastOneFlag");

	internal static string Arg_EnumIllegalVal => GetResourceString("Arg_EnumIllegalVal");

	internal static string Arg_InvalidOperationException => GetResourceString("Arg_InvalidOperationException");

	internal static string Arg_MustBeIdentityReferenceType => GetResourceString("Arg_MustBeIdentityReferenceType");

	internal static string Argument_ArgumentZero => GetResourceString("Argument_ArgumentZero");

	internal static string Argument_InvalidAnyFlag => GetResourceString("Argument_InvalidAnyFlag");

	internal static string Argument_InvalidEnumValue => GetResourceString("Argument_InvalidEnumValue");

	internal static string Argument_InvalidName => GetResourceString("Argument_InvalidName");

	internal static string Argument_InvalidPrivilegeName => GetResourceString("Argument_InvalidPrivilegeName");

	internal static string Argument_InvalidSafeHandle => GetResourceString("Argument_InvalidSafeHandle");

	internal static string ArgumentException_InvalidAceBinaryForm => GetResourceString("ArgumentException_InvalidAceBinaryForm");

	internal static string ArgumentException_InvalidAclBinaryForm => GetResourceString("ArgumentException_InvalidAclBinaryForm");

	internal static string ArgumentException_InvalidSDSddlForm => GetResourceString("ArgumentException_InvalidSDSddlForm");

	internal static string ArgumentOutOfRange_ArrayLength => GetResourceString("ArgumentOutOfRange_ArrayLength");

	internal static string ArgumentOutOfRange_ArrayLengthMultiple => GetResourceString("ArgumentOutOfRange_ArrayLengthMultiple");

	internal static string ArgumentOutOfRange_ArrayTooSmall => GetResourceString("ArgumentOutOfRange_ArrayTooSmall");

	internal static string ArgumentOutOfRange_Enum => GetResourceString("ArgumentOutOfRange_Enum");

	internal static string ArgumentOutOfRange_InvalidUserDefinedAceType => GetResourceString("ArgumentOutOfRange_InvalidUserDefinedAceType");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string InvalidOperation_ModificationOfNonCanonicalAcl => GetResourceString("InvalidOperation_ModificationOfNonCanonicalAcl");

	internal static string InvalidOperation_MustBeSameThread => GetResourceString("InvalidOperation_MustBeSameThread");

	internal static string InvalidOperation_MustLockForReadOrWrite => GetResourceString("InvalidOperation_MustLockForReadOrWrite");

	internal static string InvalidOperation_MustLockForWrite => GetResourceString("InvalidOperation_MustLockForWrite");

	internal static string InvalidOperation_MustRevertPrivilege => GetResourceString("InvalidOperation_MustRevertPrivilege");

	internal static string InvalidOperation_NoSecurityDescriptor => GetResourceString("InvalidOperation_NoSecurityDescriptor");

	internal static string InvalidOperation_OnlyValidForDS => GetResourceString("InvalidOperation_OnlyValidForDS");

	internal static string InvalidOperation_DisconnectedPipe => GetResourceString("InvalidOperation_DisconnectedPipe");

	internal static string NotSupported_SetMethod => GetResourceString("NotSupported_SetMethod");

	internal static string PrivilegeNotHeld_Default => GetResourceString("PrivilegeNotHeld_Default");

	internal static string PrivilegeNotHeld_Named => GetResourceString("PrivilegeNotHeld_Named");

	internal static string Rank_MultiDimNotSupported => GetResourceString("Rank_MultiDimNotSupported");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}
}
