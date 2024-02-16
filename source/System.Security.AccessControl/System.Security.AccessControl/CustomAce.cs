namespace System.Security.AccessControl;

public sealed class CustomAce : GenericAce
{
	private byte[] _opaque;

	public static readonly int MaxOpaqueLength = 65531;

	public int OpaqueLength
	{
		get
		{
			if (_opaque == null)
			{
				return 0;
			}
			return _opaque.Length;
		}
	}

	public override int BinaryLength => 4 + OpaqueLength;

	public CustomAce(AceType type, AceFlags flags, byte[]? opaque)
		: base(type, flags)
	{
		if ((int)type <= 16)
		{
			throw new ArgumentOutOfRangeException("type", System.SR.ArgumentOutOfRange_InvalidUserDefinedAceType);
		}
		SetOpaque(opaque);
	}

	public byte[]? GetOpaque()
	{
		return _opaque;
	}

	public void SetOpaque(byte[]? opaque)
	{
		if (opaque != null)
		{
			if (opaque.Length > MaxOpaqueLength)
			{
				throw new ArgumentOutOfRangeException("opaque", System.SR.Format(System.SR.ArgumentOutOfRange_ArrayLength, 0, MaxOpaqueLength));
			}
			if (opaque.Length % 4 != 0)
			{
				throw new ArgumentOutOfRangeException("opaque", System.SR.Format(System.SR.ArgumentOutOfRange_ArrayLengthMultiple, 4));
			}
		}
		_opaque = opaque;
	}

	public override void GetBinaryForm(byte[] binaryForm, int offset)
	{
		MarshalHeader(binaryForm, offset);
		offset += 4;
		if (OpaqueLength != 0)
		{
			if (OpaqueLength > MaxOpaqueLength)
			{
				throw new InvalidOperationException();
			}
			GetOpaque().CopyTo(binaryForm, offset);
		}
	}
}
