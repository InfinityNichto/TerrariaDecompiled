using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl;

public sealed class CompoundAce : KnownAce
{
	private CompoundAceType _compoundAceType;

	public CompoundAceType CompoundAceType
	{
		get
		{
			return _compoundAceType;
		}
		set
		{
			_compoundAceType = value;
		}
	}

	public override int BinaryLength => 12 + base.SecurityIdentifier.BinaryLength;

	public CompoundAce(AceFlags flags, int accessMask, CompoundAceType compoundAceType, SecurityIdentifier sid)
		: base(AceType.AccessAllowedCompound, flags, accessMask, sid)
	{
		_compoundAceType = compoundAceType;
	}

	internal static bool ParseBinaryForm(byte[] binaryForm, int offset, out int accessMask, out CompoundAceType compoundAceType, [NotNullWhen(true)] out SecurityIdentifier sid)
	{
		GenericAce.VerifyHeader(binaryForm, offset);
		if (binaryForm.Length - offset >= 12 + System.Security.Principal.SecurityIdentifier.MinBinaryLength)
		{
			int num = offset + 4;
			int num2 = 0;
			accessMask = binaryForm[num] + (binaryForm[num + 1] << 8) + (binaryForm[num + 2] << 16) + (binaryForm[num + 3] << 24);
			num2 += 4;
			compoundAceType = (CompoundAceType)(binaryForm[num + num2] + (binaryForm[num + num2 + 1] << 8));
			num2 += 4;
			sid = new SecurityIdentifier(binaryForm, num + num2);
			return true;
		}
		accessMask = 0;
		compoundAceType = (CompoundAceType)0;
		sid = null;
		return false;
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
		binaryForm[num + num2] = (byte)(ushort)CompoundAceType;
		binaryForm[num + num2 + 1] = (byte)((ushort)CompoundAceType >> 8);
		binaryForm[num + num2 + 2] = 0;
		binaryForm[num + num2 + 3] = 0;
		num2 += 4;
		base.SecurityIdentifier.GetBinaryForm(binaryForm, num + num2);
	}
}
