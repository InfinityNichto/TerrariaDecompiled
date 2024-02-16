using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class QualifiedAce : KnownAce
{
	private readonly bool _isCallback;

	private readonly AceQualifier _qualifier;

	private byte[] _opaque;

	public AceQualifier AceQualifier => _qualifier;

	public bool IsCallback => _isCallback;

	internal abstract int MaxOpaqueLengthInternal { get; }

	public int OpaqueLength
	{
		get
		{
			if (_opaque != null)
			{
				return _opaque.Length;
			}
			return 0;
		}
	}

	private AceQualifier QualifierFromType(AceType type, out bool isCallback)
	{
		switch (type)
		{
		case AceType.AccessAllowed:
			isCallback = false;
			return AceQualifier.AccessAllowed;
		case AceType.AccessDenied:
			isCallback = false;
			return AceQualifier.AccessDenied;
		case AceType.SystemAudit:
			isCallback = false;
			return AceQualifier.SystemAudit;
		case AceType.SystemAlarm:
			isCallback = false;
			return AceQualifier.SystemAlarm;
		case AceType.AccessAllowedCallback:
			isCallback = true;
			return AceQualifier.AccessAllowed;
		case AceType.AccessDeniedCallback:
			isCallback = true;
			return AceQualifier.AccessDenied;
		case AceType.SystemAuditCallback:
			isCallback = true;
			return AceQualifier.SystemAudit;
		case AceType.SystemAlarmCallback:
			isCallback = true;
			return AceQualifier.SystemAlarm;
		case AceType.AccessAllowedObject:
			isCallback = false;
			return AceQualifier.AccessAllowed;
		case AceType.AccessDeniedObject:
			isCallback = false;
			return AceQualifier.AccessDenied;
		case AceType.SystemAuditObject:
			isCallback = false;
			return AceQualifier.SystemAudit;
		case AceType.SystemAlarmObject:
			isCallback = false;
			return AceQualifier.SystemAlarm;
		case AceType.AccessAllowedCallbackObject:
			isCallback = true;
			return AceQualifier.AccessAllowed;
		case AceType.AccessDeniedCallbackObject:
			isCallback = true;
			return AceQualifier.AccessDenied;
		case AceType.SystemAuditCallbackObject:
			isCallback = true;
			return AceQualifier.SystemAudit;
		case AceType.SystemAlarmCallbackObject:
			isCallback = true;
			return AceQualifier.SystemAlarm;
		default:
			throw new InvalidOperationException();
		}
	}

	internal QualifiedAce(AceType type, AceFlags flags, int accessMask, SecurityIdentifier sid, byte[] opaque)
		: base(type, flags, accessMask, sid)
	{
		_qualifier = QualifierFromType(type, out _isCallback);
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
			if (opaque.Length > MaxOpaqueLengthInternal)
			{
				throw new ArgumentOutOfRangeException("opaque", System.SR.Format(System.SR.ArgumentOutOfRange_ArrayLength, 0, MaxOpaqueLengthInternal));
			}
			if (opaque.Length % 4 != 0)
			{
				throw new ArgumentOutOfRangeException("opaque", System.SR.Format(System.SR.ArgumentOutOfRange_ArrayLengthMultiple, 4));
			}
		}
		_opaque = opaque;
	}
}
