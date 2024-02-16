using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class CommonAce : QualifiedAce
{
	public override int BinaryLength => 8 + base.SecurityIdentifier.BinaryLength + base.OpaqueLength;

	internal override int MaxOpaqueLengthInternal => MaxOpaqueLength(base.IsCallback);

	public CommonAce(AceFlags flags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, bool isCallback, byte[]? opaque)
		: base(TypeFromQualifier(isCallback, qualifier), flags, accessMask, sid, opaque)
	{
	}

	private static AceType TypeFromQualifier(bool isCallback, AceQualifier qualifier)
	{
		return qualifier switch
		{
			AceQualifier.AccessAllowed => isCallback ? AceType.AccessAllowedCallback : AceType.AccessAllowed, 
			AceQualifier.AccessDenied => (!isCallback) ? AceType.AccessDenied : AceType.AccessDeniedCallback, 
			AceQualifier.SystemAudit => isCallback ? AceType.SystemAuditCallback : AceType.SystemAudit, 
			AceQualifier.SystemAlarm => isCallback ? AceType.SystemAlarmCallback : AceType.SystemAlarm, 
			_ => throw new ArgumentOutOfRangeException("qualifier", System.SR.ArgumentOutOfRange_Enum), 
		};
	}

	internal static bool ParseBinaryForm(byte[] binaryForm, int offset, out AceQualifier qualifier, out int accessMask, [NotNullWhen(true)] out SecurityIdentifier sid, out bool isCallback, out byte[] opaque)
	{
		GenericAce.VerifyHeader(binaryForm, offset);
		if (binaryForm.Length - offset >= 8 + System.Security.Principal.SecurityIdentifier.MinBinaryLength)
		{
			AceType aceType = (AceType)binaryForm[offset];
			if (aceType == AceType.AccessAllowed || aceType == AceType.AccessDenied || aceType == AceType.SystemAudit || aceType == AceType.SystemAlarm)
			{
				isCallback = false;
			}
			else
			{
				if (aceType != AceType.AccessAllowedCallback && aceType != AceType.AccessDeniedCallback && aceType != AceType.SystemAuditCallback && aceType != AceType.SystemAlarmCallback)
				{
					goto IL_0114;
				}
				isCallback = true;
			}
			if (aceType == AceType.AccessAllowed || aceType == AceType.AccessAllowedCallback)
			{
				qualifier = AceQualifier.AccessAllowed;
			}
			else if (aceType == AceType.AccessDenied || aceType == AceType.AccessDeniedCallback)
			{
				qualifier = AceQualifier.AccessDenied;
			}
			else if (aceType == AceType.SystemAudit || aceType == AceType.SystemAuditCallback)
			{
				qualifier = AceQualifier.SystemAudit;
			}
			else
			{
				if (aceType != AceType.SystemAlarm && aceType != AceType.SystemAlarmCallback)
				{
					goto IL_0114;
				}
				qualifier = AceQualifier.SystemAlarm;
			}
			int num = offset + 4;
			int num2 = 0;
			accessMask = binaryForm[num] + (binaryForm[num + 1] << 8) + (binaryForm[num + 2] << 16) + (binaryForm[num + 3] << 24);
			num2 += 4;
			sid = new SecurityIdentifier(binaryForm, num + num2);
			opaque = null;
			int num3 = (binaryForm[offset + 3] << 8) + binaryForm[offset + 2];
			if (num3 % 4 == 0)
			{
				int num4 = num3 - 4 - 4 - (byte)sid.BinaryLength;
				if (num4 > 0)
				{
					opaque = new byte[num4];
					for (int i = 0; i < num4; i++)
					{
						opaque[i] = binaryForm[offset + num3 - num4 + i];
					}
				}
				return true;
			}
		}
		goto IL_0114;
		IL_0114:
		qualifier = AceQualifier.AccessAllowed;
		accessMask = 0;
		sid = null;
		isCallback = false;
		opaque = null;
		return false;
	}

	public static int MaxOpaqueLength(bool isCallback)
	{
		return 65527 - System.Security.Principal.SecurityIdentifier.MaxBinaryLength;
	}

	public override void GetBinaryForm(byte[] binaryForm, int offset)
	{
		MarshalHeader(binaryForm, offset);
		int num = offset + 4;
		int num2 = 0;
		binaryForm[num] = (byte)base.AccessMask;
		binaryForm[num + 1] = (byte)(base.AccessMask >> 8);
		binaryForm[num + 2] = (byte)(base.AccessMask >> 16);
		binaryForm[num + 3] = (byte)(base.AccessMask >> 24);
		num2 += 4;
		base.SecurityIdentifier.GetBinaryForm(binaryForm, num + num2);
		num2 += base.SecurityIdentifier.BinaryLength;
		if (GetOpaque() != null)
		{
			if (base.OpaqueLength > MaxOpaqueLengthInternal)
			{
				throw new InvalidOperationException();
			}
			GetOpaque().CopyTo(binaryForm, num + num2);
		}
	}
}
