using System.Security.Principal;

namespace System.Security.AccessControl;

public abstract class CommonAcl : GenericAcl
{
	[Flags]
	private enum AF
	{
		CI = 8,
		OI = 4,
		IO = 2,
		NP = 1,
		Invalid = 1
	}

	[Flags]
	private enum PM
	{
		F = 0x10,
		CF = 8,
		CO = 4,
		GF = 2,
		GO = 1,
		Invalid = 1
	}

	private enum ComparisonResult
	{
		LessThan,
		EqualTo,
		GreaterThan
	}

	private static readonly PM[] s_AFtoPM = CreateAFtoPMConversionMatrix();

	private static readonly AF[] s_PMtoAF = CreatePMtoAFConversionMatrix();

	private readonly RawAcl _acl;

	private bool _isDirty;

	private readonly bool _isCanonical;

	private readonly bool _isContainer;

	private readonly bool _isDS;

	internal RawAcl RawAcl => _acl;

	public sealed override byte Revision => _acl.Revision;

	public sealed override int Count
	{
		get
		{
			CanonicalizeIfNecessary();
			return _acl.Count;
		}
	}

	public sealed override int BinaryLength
	{
		get
		{
			CanonicalizeIfNecessary();
			return _acl.BinaryLength;
		}
	}

	public bool IsCanonical => _isCanonical;

	public bool IsContainer => _isContainer;

	public bool IsDS => _isDS;

	public sealed override GenericAce this[int index]
	{
		get
		{
			CanonicalizeIfNecessary();
			return _acl[index].Copy();
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_SetMethod);
		}
	}

	private static PM[] CreateAFtoPMConversionMatrix()
	{
		PM[] array = new PM[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = PM.GO;
		}
		array[0] = PM.F;
		array[4] = PM.F | PM.CO | PM.GO;
		array[5] = PM.F | PM.CO;
		array[6] = PM.CO | PM.GO;
		array[7] = PM.CO;
		array[8] = PM.F | PM.CF | PM.GF;
		array[9] = PM.F | PM.CF;
		array[10] = PM.CF | PM.GF;
		array[11] = PM.CF;
		array[12] = PM.F | PM.CF | PM.CO | PM.GF | PM.GO;
		array[13] = PM.F | PM.CF | PM.CO;
		array[14] = PM.CF | PM.CO | PM.GF | PM.GO;
		array[15] = PM.CF | PM.CO;
		return array;
	}

	private static AF[] CreatePMtoAFConversionMatrix()
	{
		AF[] array = new AF[32];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = AF.NP;
		}
		array[16] = (AF)0;
		array[21] = AF.OI;
		array[20] = AF.OI | AF.NP;
		array[5] = AF.OI | AF.IO;
		array[4] = AF.OI | AF.IO | AF.NP;
		array[26] = AF.CI;
		array[24] = AF.CI | AF.NP;
		array[10] = AF.CI | AF.IO;
		array[8] = AF.CI | AF.IO | AF.NP;
		array[31] = AF.CI | AF.OI;
		array[28] = AF.CI | AF.OI | AF.NP;
		array[15] = AF.CI | AF.OI | AF.IO;
		array[12] = AF.CI | AF.OI | AF.IO | AF.NP;
		return array;
	}

	private static AF AFFromAceFlags(AceFlags aceFlags, bool isDS)
	{
		AF aF = (AF)0;
		if ((aceFlags & AceFlags.ContainerInherit) != 0)
		{
			aF |= AF.CI;
		}
		if (!isDS && (aceFlags & AceFlags.ObjectInherit) != 0)
		{
			aF |= AF.OI;
		}
		if ((aceFlags & AceFlags.InheritOnly) != 0)
		{
			aF |= AF.IO;
		}
		if ((aceFlags & AceFlags.NoPropagateInherit) != 0)
		{
			aF |= AF.NP;
		}
		return aF;
	}

	private static AceFlags AceFlagsFromAF(AF af, bool isDS)
	{
		AceFlags aceFlags = AceFlags.None;
		if ((af & AF.CI) != 0)
		{
			aceFlags |= AceFlags.ContainerInherit;
		}
		if (!isDS && (af & AF.OI) != 0)
		{
			aceFlags |= AceFlags.ObjectInherit;
		}
		if ((af & AF.IO) != 0)
		{
			aceFlags |= AceFlags.InheritOnly;
		}
		if ((af & AF.NP) != 0)
		{
			aceFlags |= AceFlags.NoPropagateInherit;
		}
		return aceFlags;
	}

	private static bool MergeInheritanceBits(AceFlags left, AceFlags right, bool isDS, out AceFlags result)
	{
		result = AceFlags.None;
		AF aF = AFFromAceFlags(left, isDS);
		AF aF2 = AFFromAceFlags(right, isDS);
		PM pM = s_AFtoPM[(int)aF];
		PM pM2 = s_AFtoPM[(int)aF2];
		if (pM == PM.GO || pM2 == PM.GO)
		{
			return false;
		}
		PM pM3 = pM | pM2;
		AF aF3 = s_PMtoAF[(int)pM3];
		if (aF3 == AF.NP)
		{
			return false;
		}
		result = AceFlagsFromAF(aF3, isDS);
		return true;
	}

	private static bool RemoveInheritanceBits(AceFlags existing, AceFlags remove, bool isDS, out AceFlags result, out bool total)
	{
		result = AceFlags.None;
		total = false;
		AF aF = AFFromAceFlags(existing, isDS);
		AF aF2 = AFFromAceFlags(remove, isDS);
		PM pM = s_AFtoPM[(int)aF];
		PM pM2 = s_AFtoPM[(int)aF2];
		if (pM == PM.GO || pM2 == PM.GO)
		{
			return false;
		}
		PM pM3 = pM & ~pM2;
		if (pM3 == (PM)0)
		{
			total = true;
			return true;
		}
		AF aF3 = s_PMtoAF[(int)pM3];
		if (aF3 == AF.NP)
		{
			return false;
		}
		result = AceFlagsFromAF(aF3, isDS);
		return true;
	}

	private void CanonicalizeIfNecessary()
	{
		if (_isDirty)
		{
			Canonicalize(compact: false, this is DiscretionaryAcl);
			_isDirty = false;
		}
	}

	private static int DaclAcePriority(GenericAce ace)
	{
		AceType aceType = ace.AceType;
		if ((ace.AceFlags & AceFlags.Inherited) != 0)
		{
			return 131070 + ace._indexInAcl;
		}
		switch (aceType)
		{
		case AceType.AccessDenied:
		case AceType.AccessDeniedCallback:
			return 0;
		case AceType.AccessDeniedObject:
		case AceType.AccessDeniedCallbackObject:
			return 1;
		case AceType.AccessAllowed:
		case AceType.AccessAllowedCallback:
			return 2;
		case AceType.AccessAllowedObject:
		case AceType.AccessAllowedCallbackObject:
			return 3;
		default:
			return 65535 + ace._indexInAcl;
		}
	}

	private static int SaclAcePriority(GenericAce ace)
	{
		AceType aceType = ace.AceType;
		if ((ace.AceFlags & AceFlags.Inherited) != 0)
		{
			return 131070 + ace._indexInAcl;
		}
		switch (aceType)
		{
		case AceType.SystemAudit:
		case AceType.SystemAlarm:
		case AceType.SystemAuditCallback:
		case AceType.SystemAlarmCallback:
			return 0;
		case AceType.SystemAuditObject:
		case AceType.SystemAlarmObject:
		case AceType.SystemAuditCallbackObject:
		case AceType.SystemAlarmCallbackObject:
			return 1;
		default:
			return 65535 + ace._indexInAcl;
		}
	}

	private static ComparisonResult CompareAces(GenericAce ace1, GenericAce ace2, bool isDacl)
	{
		int num = (isDacl ? DaclAcePriority(ace1) : SaclAcePriority(ace1));
		int num2 = (isDacl ? DaclAcePriority(ace2) : SaclAcePriority(ace2));
		if (num < num2)
		{
			return ComparisonResult.LessThan;
		}
		if (num > num2)
		{
			return ComparisonResult.GreaterThan;
		}
		if (ace1 is KnownAce knownAce && ace2 is KnownAce knownAce2)
		{
			int num3 = knownAce.SecurityIdentifier.CompareTo(knownAce2.SecurityIdentifier);
			if (num3 < 0)
			{
				return ComparisonResult.LessThan;
			}
			if (num3 > 0)
			{
				return ComparisonResult.GreaterThan;
			}
		}
		return ComparisonResult.EqualTo;
	}

	private void QuickSort(int left, int right, bool isDacl)
	{
		if (left >= right)
		{
			return;
		}
		int num = left;
		int num2 = right;
		GenericAce genericAce = _acl[left];
		int num3 = left;
		while (left < right)
		{
			while (CompareAces(_acl[right], genericAce, isDacl) != 0 && left < right)
			{
				right--;
			}
			if (left != right)
			{
				_acl[left] = _acl[right];
				left++;
			}
			while (ComparisonResult.GreaterThan != CompareAces(_acl[left], genericAce, isDacl) && left < right)
			{
				left++;
			}
			if (left != right)
			{
				_acl[right] = _acl[left];
				right--;
			}
		}
		_acl[left] = genericAce;
		num3 = left;
		left = num;
		right = num2;
		if (left < num3)
		{
			QuickSort(left, num3 - 1, isDacl);
		}
		if (right > num3)
		{
			QuickSort(num3 + 1, right, isDacl);
		}
	}

	private bool InspectAce(ref GenericAce ace, bool isDacl)
	{
		KnownAce knownAce = ace as KnownAce;
		if (knownAce != null && knownAce.AccessMask == 0)
		{
			return false;
		}
		if (!IsContainer)
		{
			if ((ace.AceFlags & AceFlags.InheritOnly) != 0)
			{
				return false;
			}
			if ((ace.AceFlags & AceFlags.InheritanceFlags) != 0)
			{
				ace.AceFlags &= ~AceFlags.InheritanceFlags;
			}
		}
		else
		{
			if ((ace.AceFlags & AceFlags.InheritOnly) != 0 && (ace.AceFlags & AceFlags.ContainerInherit) == 0 && (ace.AceFlags & AceFlags.ObjectInherit) == 0)
			{
				return false;
			}
			if ((ace.AceFlags & AceFlags.NoPropagateInherit) != 0 && (ace.AceFlags & AceFlags.ContainerInherit) == 0 && (ace.AceFlags & AceFlags.ObjectInherit) == 0)
			{
				ace.AceFlags &= ~AceFlags.NoPropagateInherit;
			}
		}
		QualifiedAce qualifiedAce = knownAce as QualifiedAce;
		if (isDacl)
		{
			ace.AceFlags &= ~AceFlags.AuditFlags;
			if (qualifiedAce != null && qualifiedAce.AceQualifier != 0 && qualifiedAce.AceQualifier != AceQualifier.AccessDenied)
			{
				return false;
			}
		}
		else
		{
			if ((ace.AceFlags & AceFlags.AuditFlags) == 0)
			{
				return false;
			}
			if (qualifiedAce != null && qualifiedAce.AceQualifier != AceQualifier.SystemAudit)
			{
				return false;
			}
		}
		return true;
	}

	private void RemoveMeaninglessAcesAndFlags(bool isDacl)
	{
		for (int num = _acl.Count - 1; num >= 0; num--)
		{
			GenericAce ace = _acl[num];
			if (!InspectAce(ref ace, isDacl))
			{
				_acl.RemoveAce(num);
			}
		}
	}

	private void Canonicalize(bool compact, bool isDacl)
	{
		for (ushort num = 0; num < _acl.Count; num++)
		{
			_acl[num]._indexInAcl = num;
		}
		QuickSort(0, _acl.Count - 1, isDacl);
		if (!compact)
		{
			return;
		}
		for (int i = 0; i < Count - 1; i++)
		{
			QualifiedAce ace = _acl[i] as QualifiedAce;
			if (!(ace == null))
			{
				QualifiedAce qualifiedAce = _acl[i + 1] as QualifiedAce;
				if (!(qualifiedAce == null) && MergeAces(ref ace, qualifiedAce))
				{
					_acl.RemoveAce(i + 1);
				}
			}
		}
	}

	private void GetObjectTypesForSplit(ObjectAce originalAce, int accessMask, AceFlags aceFlags, out ObjectAceFlags objectFlags, out Guid objectType, out Guid inheritedObjectType)
	{
		objectFlags = ObjectAceFlags.None;
		objectType = Guid.Empty;
		inheritedObjectType = Guid.Empty;
		if (((uint)accessMask & 0x13Bu) != 0)
		{
			objectType = originalAce.ObjectAceType;
			objectFlags |= originalAce.ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent;
		}
		if ((aceFlags & AceFlags.ContainerInherit) != 0)
		{
			inheritedObjectType = originalAce.InheritedObjectAceType;
			objectFlags |= originalAce.ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent;
		}
	}

	private bool ObjectTypesMatch(QualifiedAce ace, QualifiedAce newAce)
	{
		Guid guid = ((ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceType : Guid.Empty);
		Guid g = ((newAce is ObjectAce) ? ((ObjectAce)newAce).ObjectAceType : Guid.Empty);
		return guid.Equals(g);
	}

	private bool InheritedObjectTypesMatch(QualifiedAce ace, QualifiedAce newAce)
	{
		Guid guid = ((ace is ObjectAce) ? ((ObjectAce)ace).InheritedObjectAceType : Guid.Empty);
		Guid g = ((newAce is ObjectAce) ? ((ObjectAce)newAce).InheritedObjectAceType : Guid.Empty);
		return guid.Equals(g);
	}

	private bool AccessMasksAreMergeable(QualifiedAce ace, QualifiedAce newAce)
	{
		if (ObjectTypesMatch(ace, newAce))
		{
			return true;
		}
		ObjectAceFlags objectAceFlags = ((ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceFlags : ObjectAceFlags.None);
		if ((ace.AccessMask & newAce.AccessMask & 0x13B) == (newAce.AccessMask & 0x13B) && (objectAceFlags & ObjectAceFlags.ObjectAceTypePresent) == 0)
		{
			return true;
		}
		return false;
	}

	private bool AceFlagsAreMergeable(QualifiedAce ace, QualifiedAce newAce)
	{
		if (InheritedObjectTypesMatch(ace, newAce))
		{
			return true;
		}
		ObjectAceFlags objectAceFlags = ((ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceFlags : ObjectAceFlags.None);
		if ((objectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) == 0)
		{
			return true;
		}
		return false;
	}

	private bool GetAccessMaskForRemoval(QualifiedAce ace, ObjectAceFlags objectFlags, Guid objectType, ref int accessMask)
	{
		if (((uint)(ace.AccessMask & accessMask) & 0x13Bu) != 0)
		{
			if (ace is ObjectAce objectAce)
			{
				bool flag = true;
				if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0 && (objectAce.ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) == 0)
				{
					return false;
				}
				if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0 && !objectAce.ObjectTypesMatch(objectFlags, objectType))
				{
					accessMask &= -316;
				}
			}
			else if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool GetInheritanceFlagsForRemoval(QualifiedAce ace, ObjectAceFlags objectFlags, Guid inheritedObjectType, ref AceFlags aceFlags)
	{
		if ((ace.AceFlags & AceFlags.ContainerInherit) != 0 && (aceFlags & AceFlags.ContainerInherit) != 0)
		{
			if (ace is ObjectAce objectAce)
			{
				bool flag = true;
				if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0 && (objectAce.ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) == 0)
				{
					return false;
				}
				if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0 && !objectAce.InheritedObjectTypesMatch(objectFlags, inheritedObjectType))
				{
					aceFlags &= ~AceFlags.InheritanceFlags;
				}
			}
			else if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool AceOpaquesMatch(QualifiedAce ace, QualifiedAce newAce)
	{
		byte[] opaque = ace.GetOpaque();
		byte[] opaque2 = newAce.GetOpaque();
		if (opaque == null || opaque2 == null)
		{
			return opaque == opaque2;
		}
		if (opaque.Length != opaque2.Length)
		{
			return false;
		}
		for (int i = 0; i < opaque.Length; i++)
		{
			if (opaque[i] != opaque2[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool AcesAreMergeable(QualifiedAce ace, QualifiedAce newAce)
	{
		if (ace.AceType != newAce.AceType)
		{
			return false;
		}
		if ((ace.AceFlags & AceFlags.Inherited) != 0)
		{
			return false;
		}
		if ((newAce.AceFlags & AceFlags.Inherited) != 0)
		{
			return false;
		}
		if (ace.AceQualifier != newAce.AceQualifier)
		{
			return false;
		}
		if (ace.SecurityIdentifier != newAce.SecurityIdentifier)
		{
			return false;
		}
		if (!AceOpaquesMatch(ace, newAce))
		{
			return false;
		}
		return true;
	}

	private bool MergeAces(ref QualifiedAce ace, QualifiedAce newAce)
	{
		if (!AcesAreMergeable(ace, newAce))
		{
			return false;
		}
		if (ace.AceFlags == newAce.AceFlags)
		{
			if (!(ace is ObjectAce) && !(newAce is ObjectAce))
			{
				ace.AccessMask |= newAce.AccessMask;
				return true;
			}
			if (InheritedObjectTypesMatch(ace, newAce) && AccessMasksAreMergeable(ace, newAce))
			{
				ace.AccessMask |= newAce.AccessMask;
				return true;
			}
		}
		if ((ace.AceFlags & AceFlags.InheritanceFlags) == (newAce.AceFlags & AceFlags.InheritanceFlags) && ace.AccessMask == newAce.AccessMask)
		{
			if (!(ace is ObjectAce) && !(newAce is ObjectAce))
			{
				ace.AceFlags |= newAce.AceFlags & AceFlags.AuditFlags;
				return true;
			}
			if (InheritedObjectTypesMatch(ace, newAce) && ObjectTypesMatch(ace, newAce))
			{
				ace.AceFlags |= newAce.AceFlags & AceFlags.AuditFlags;
				return true;
			}
		}
		if ((ace.AceFlags & AceFlags.AuditFlags) == (newAce.AceFlags & AceFlags.AuditFlags) && ace.AccessMask == newAce.AccessMask)
		{
			AceFlags result;
			if (ace is ObjectAce || newAce is ObjectAce)
			{
				if (ObjectTypesMatch(ace, newAce) && AceFlagsAreMergeable(ace, newAce) && MergeInheritanceBits(ace.AceFlags, newAce.AceFlags, IsDS, out result))
				{
					ace.AceFlags = result | (ace.AceFlags & AceFlags.AuditFlags);
					return true;
				}
			}
			else if (MergeInheritanceBits(ace.AceFlags, newAce.AceFlags, IsDS, out result))
			{
				ace.AceFlags = result | (ace.AceFlags & AceFlags.AuditFlags);
				return true;
			}
		}
		return false;
	}

	private bool CanonicalCheck(bool isDacl)
	{
		if (isDacl)
		{
			int num = 0;
			for (int i = 0; i < _acl.Count; i++)
			{
				GenericAce genericAce = _acl[i];
				int num2;
				if ((genericAce.AceFlags & AceFlags.Inherited) != 0)
				{
					num2 = 2;
				}
				else
				{
					QualifiedAce qualifiedAce = genericAce as QualifiedAce;
					if (qualifiedAce == null)
					{
						return false;
					}
					if (qualifiedAce.AceQualifier == AceQualifier.AccessAllowed)
					{
						num2 = 1;
					}
					else
					{
						if (qualifiedAce.AceQualifier != AceQualifier.AccessDenied)
						{
							return false;
						}
						num2 = 0;
					}
				}
				if (num2 > num)
				{
					num = num2;
				}
				else if (num2 < num)
				{
					return false;
				}
			}
		}
		else
		{
			int num3 = 0;
			for (int j = 0; j < _acl.Count; j++)
			{
				GenericAce genericAce2 = _acl[j];
				if (genericAce2 == null)
				{
					continue;
				}
				int num4;
				if ((genericAce2.AceFlags & AceFlags.Inherited) != 0)
				{
					num4 = 1;
				}
				else
				{
					QualifiedAce qualifiedAce2 = genericAce2 as QualifiedAce;
					if (qualifiedAce2 == null)
					{
						return false;
					}
					if (qualifiedAce2.AceQualifier != AceQualifier.SystemAudit && qualifiedAce2.AceQualifier != AceQualifier.SystemAlarm)
					{
						return false;
					}
					num4 = 0;
				}
				if (num4 > num3)
				{
					num3 = num4;
				}
				else if (num4 < num3)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void ThrowIfNotCanonical()
	{
		if (!_isCanonical)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ModificationOfNonCanonicalAcl);
		}
	}

	internal CommonAcl(bool isContainer, bool isDS, byte revision, int capacity)
	{
		_isContainer = isContainer;
		_isDS = isDS;
		_acl = new RawAcl(revision, capacity);
		_isCanonical = true;
	}

	internal CommonAcl(bool isContainer, bool isDS, RawAcl rawAcl, bool trusted, bool isDacl)
	{
		if (rawAcl == null)
		{
			throw new ArgumentNullException("rawAcl");
		}
		_isContainer = isContainer;
		_isDS = isDS;
		if (trusted)
		{
			_acl = rawAcl;
			RemoveMeaninglessAcesAndFlags(isDacl);
		}
		else
		{
			_acl = new RawAcl(rawAcl.Revision, rawAcl.Count);
			for (int i = 0; i < rawAcl.Count; i++)
			{
				GenericAce ace = rawAcl[i].Copy();
				if (InspectAce(ref ace, isDacl))
				{
					_acl.InsertAce(_acl.Count, ace);
				}
			}
		}
		if (CanonicalCheck(isDacl))
		{
			Canonicalize(compact: true, isDacl);
			_isCanonical = true;
		}
		else
		{
			_isCanonical = false;
		}
	}

	internal void CheckAccessType(AccessControlType accessType)
	{
		if (accessType != 0 && accessType != AccessControlType.Deny)
		{
			throw new ArgumentOutOfRangeException("accessType", System.SR.ArgumentOutOfRange_Enum);
		}
	}

	internal void CheckFlags(InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
	{
		if (IsContainer)
		{
			if (inheritanceFlags == InheritanceFlags.None && propagationFlags != 0)
			{
				throw new ArgumentException(System.SR.Argument_InvalidAnyFlag, "propagationFlags");
			}
			return;
		}
		if (inheritanceFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidAnyFlag, "inheritanceFlags");
		}
		if (propagationFlags != 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidAnyFlag, "propagationFlags");
		}
	}

	internal void AddQualifiedAce(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		ThrowIfNotCanonical();
		bool flag = false;
		if (qualifier == AceQualifier.SystemAudit && (flags & AceFlags.AuditFlags) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "flags");
		}
		if (accessMask == 0)
		{
			throw new ArgumentException(System.SR.Argument_ArgumentZero, "accessMask");
		}
		GenericAce ace = ((IsDS && objectFlags != 0) ? ((QualifiedAce)new ObjectAce(flags, qualifier, accessMask, sid, objectFlags, objectType, inheritedObjectType, isCallback: false, null)) : ((QualifiedAce)new CommonAce(flags, qualifier, accessMask, sid, isCallback: false, null)));
		if (!InspectAce(ref ace, this is DiscretionaryAcl))
		{
			return;
		}
		for (int i = 0; i < Count; i++)
		{
			QualifiedAce ace2 = _acl[i] as QualifiedAce;
			if (!(ace2 == null) && MergeAces(ref ace2, (QualifiedAce)ace))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			_acl.InsertAce(_acl.Count, ace);
			_isDirty = true;
		}
		OnAclModificationTried();
	}

	internal void SetQualifiedAce(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		if (qualifier == AceQualifier.SystemAudit && (flags & AceFlags.AuditFlags) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "flags");
		}
		if (accessMask == 0)
		{
			throw new ArgumentException(System.SR.Argument_ArgumentZero, "accessMask");
		}
		ThrowIfNotCanonical();
		GenericAce ace = ((IsDS && objectFlags != 0) ? ((QualifiedAce)new ObjectAce(flags, qualifier, accessMask, sid, objectFlags, objectType, inheritedObjectType, isCallback: false, null)) : ((QualifiedAce)new CommonAce(flags, qualifier, accessMask, sid, isCallback: false, null)));
		if (!InspectAce(ref ace, this is DiscretionaryAcl))
		{
			return;
		}
		for (int i = 0; i < Count; i++)
		{
			QualifiedAce qualifiedAce = _acl[i] as QualifiedAce;
			if (!(qualifiedAce == null) && (qualifiedAce.AceFlags & AceFlags.Inherited) == 0 && qualifiedAce.AceQualifier == qualifier && !(qualifiedAce.SecurityIdentifier != sid))
			{
				_acl.RemoveAce(i);
				i--;
			}
		}
		_acl.InsertAce(_acl.Count, ace);
		_isDirty = true;
		OnAclModificationTried();
	}

	internal bool RemoveQualifiedAces(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, bool saclSemantics, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (accessMask == 0)
		{
			throw new ArgumentException(System.SR.Argument_ArgumentZero, "accessMask");
		}
		if (qualifier == AceQualifier.SystemAudit && (flags & AceFlags.AuditFlags) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "flags");
		}
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		ThrowIfNotCanonical();
		bool flag = true;
		bool flag2 = true;
		int num = accessMask;
		AceFlags aceFlags = flags;
		byte[] binaryForm = new byte[BinaryLength];
		GetBinaryForm(binaryForm, 0);
		while (true)
		{
			try
			{
				for (int i = 0; i < Count; i++)
				{
					QualifiedAce qualifiedAce = _acl[i] as QualifiedAce;
					if (qualifiedAce == null || (qualifiedAce.AceFlags & AceFlags.Inherited) != 0 || qualifiedAce.AceQualifier != qualifier || qualifiedAce.SecurityIdentifier != sid)
					{
						continue;
					}
					if (IsDS)
					{
						accessMask = num;
						bool flag3 = !GetAccessMaskForRemoval(qualifiedAce, objectFlags, objectType, ref accessMask);
						if ((qualifiedAce.AccessMask & accessMask) == 0)
						{
							continue;
						}
						flags = aceFlags;
						bool flag4 = !GetInheritanceFlagsForRemoval(qualifiedAce, objectFlags, inheritedObjectType, ref flags);
						if (((qualifiedAce.AceFlags & AceFlags.ContainerInherit) == 0 && (flags & AceFlags.ContainerInherit) != 0 && (flags & AceFlags.InheritOnly) != 0) || ((flags & AceFlags.ContainerInherit) == 0 && (qualifiedAce.AceFlags & AceFlags.ContainerInherit) != 0 && (qualifiedAce.AceFlags & AceFlags.InheritOnly) != 0) || ((aceFlags & AceFlags.ContainerInherit) != 0 && (aceFlags & AceFlags.InheritOnly) != 0 && (flags & AceFlags.ContainerInherit) == 0))
						{
							continue;
						}
						if (flag3 || flag4)
						{
							flag2 = false;
							break;
						}
					}
					else if ((qualifiedAce.AccessMask & accessMask) == 0)
					{
						continue;
					}
					if (saclSemantics && (qualifiedAce.AceFlags & flags & AceFlags.AuditFlags) == 0)
					{
						continue;
					}
					AceFlags aceFlags2 = AceFlags.None;
					int num2 = 0;
					ObjectAceFlags objectFlags2 = ObjectAceFlags.None;
					Guid objectType2 = Guid.Empty;
					Guid inheritedObjectType2 = Guid.Empty;
					AceFlags aceFlags3 = AceFlags.None;
					int accessMask2 = 0;
					ObjectAceFlags objectFlags3 = ObjectAceFlags.None;
					Guid objectType3 = Guid.Empty;
					Guid inheritedObjectType3 = Guid.Empty;
					AceFlags aceFlags4 = AceFlags.None;
					int num3 = 0;
					ObjectAceFlags objectFlags4 = ObjectAceFlags.None;
					Guid objectType4 = Guid.Empty;
					Guid inheritedObjectType4 = Guid.Empty;
					AceFlags result = AceFlags.None;
					bool total = false;
					aceFlags2 = qualifiedAce.AceFlags;
					num2 = qualifiedAce.AccessMask & ~accessMask;
					if (qualifiedAce is ObjectAce originalAce)
					{
						GetObjectTypesForSplit(originalAce, num2, aceFlags2, out objectFlags2, out objectType2, out inheritedObjectType2);
					}
					if (saclSemantics)
					{
						aceFlags3 = qualifiedAce.AceFlags & (AceFlags)(~(uint)(flags & AceFlags.AuditFlags));
						accessMask2 = qualifiedAce.AccessMask & accessMask;
						if (qualifiedAce is ObjectAce originalAce2)
						{
							GetObjectTypesForSplit(originalAce2, accessMask2, aceFlags3, out objectFlags3, out objectType3, out inheritedObjectType3);
						}
					}
					aceFlags4 = (qualifiedAce.AceFlags & AceFlags.InheritanceFlags) | (flags & qualifiedAce.AceFlags & AceFlags.AuditFlags);
					num3 = qualifiedAce.AccessMask & accessMask;
					if (!saclSemantics || (aceFlags4 & AceFlags.AuditFlags) != 0)
					{
						if (!RemoveInheritanceBits(aceFlags4, flags, IsDS, out result, out total))
						{
							flag2 = false;
							break;
						}
						if (!total)
						{
							result |= aceFlags4 & AceFlags.AuditFlags;
							if (qualifiedAce is ObjectAce originalAce3)
							{
								GetObjectTypesForSplit(originalAce3, num3, result, out objectFlags4, out objectType4, out inheritedObjectType4);
							}
						}
					}
					if (flag)
					{
						continue;
					}
					if (num2 != 0)
					{
						if (qualifiedAce is ObjectAce && (((ObjectAce)qualifiedAce).ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != 0 && (objectFlags2 & ObjectAceFlags.ObjectAceTypePresent) == 0)
						{
							_acl.RemoveAce(i);
							ObjectAce ace = new ObjectAce(aceFlags2, qualifier, num2, qualifiedAce.SecurityIdentifier, objectFlags2, objectType2, inheritedObjectType2, isCallback: false, null);
							_acl.InsertAce(i, ace);
						}
						else
						{
							qualifiedAce.AceFlags = aceFlags2;
							qualifiedAce.AccessMask = num2;
							if (qualifiedAce is ObjectAce objectAce)
							{
								objectAce.ObjectAceFlags = objectFlags2;
								objectAce.ObjectAceType = objectType2;
								objectAce.InheritedObjectAceType = inheritedObjectType2;
							}
						}
					}
					else
					{
						_acl.RemoveAce(i);
						i--;
					}
					if (saclSemantics && (aceFlags3 & AceFlags.AuditFlags) != 0)
					{
						QualifiedAce ace2 = ((!(qualifiedAce is CommonAce)) ? ((QualifiedAce)new ObjectAce(aceFlags3, qualifier, accessMask2, qualifiedAce.SecurityIdentifier, objectFlags3, objectType3, inheritedObjectType3, isCallback: false, null)) : ((QualifiedAce)new CommonAce(aceFlags3, qualifier, accessMask2, qualifiedAce.SecurityIdentifier, isCallback: false, null)));
						i++;
						_acl.InsertAce(i, ace2);
					}
					if (!total)
					{
						QualifiedAce ace2 = ((!(qualifiedAce is CommonAce)) ? ((QualifiedAce)new ObjectAce(result, qualifier, num3, qualifiedAce.SecurityIdentifier, objectFlags4, objectType4, inheritedObjectType4, isCallback: false, null)) : ((QualifiedAce)new CommonAce(result, qualifier, num3, qualifiedAce.SecurityIdentifier, isCallback: false, null)));
						i++;
						_acl.InsertAce(i, ace2);
					}
				}
			}
			catch (OverflowException)
			{
				_acl.SetBinaryForm(binaryForm, 0);
				return false;
			}
			if (!(flag && flag2))
			{
				break;
			}
			flag = false;
		}
		OnAclModificationTried();
		return flag2;
	}

	internal void RemoveQualifiedAcesSpecific(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
	{
		if (accessMask == 0)
		{
			throw new ArgumentException(System.SR.Argument_ArgumentZero, "accessMask");
		}
		if (qualifier == AceQualifier.SystemAudit && (flags & AceFlags.AuditFlags) == 0)
		{
			throw new ArgumentException(System.SR.Arg_EnumAtLeastOneFlag, "flags");
		}
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		ThrowIfNotCanonical();
		for (int i = 0; i < Count; i++)
		{
			QualifiedAce qualifiedAce = _acl[i] as QualifiedAce;
			if (qualifiedAce == null || (qualifiedAce.AceFlags & AceFlags.Inherited) != 0 || qualifiedAce.AceQualifier != qualifier || qualifiedAce.SecurityIdentifier != sid || qualifiedAce.AceFlags != flags || qualifiedAce.AccessMask != accessMask)
			{
				continue;
			}
			if (IsDS)
			{
				if (qualifiedAce is ObjectAce objectAce && objectFlags != 0)
				{
					if (!objectAce.ObjectTypesMatch(objectFlags, objectType) || !objectAce.InheritedObjectTypesMatch(objectFlags, inheritedObjectType))
					{
						continue;
					}
				}
				else if (qualifiedAce is ObjectAce || objectFlags != 0)
				{
					continue;
				}
			}
			_acl.RemoveAce(i);
			i--;
		}
		OnAclModificationTried();
	}

	internal virtual void OnAclModificationTried()
	{
	}

	public sealed override void GetBinaryForm(byte[] binaryForm, int offset)
	{
		CanonicalizeIfNecessary();
		_acl.GetBinaryForm(binaryForm, offset);
	}

	public void RemoveInheritedAces()
	{
		ThrowIfNotCanonical();
		for (int num = _acl.Count - 1; num >= 0; num--)
		{
			GenericAce genericAce = _acl[num];
			if ((genericAce.AceFlags & AceFlags.Inherited) != 0)
			{
				_acl.RemoveAce(num);
			}
		}
		OnAclModificationTried();
	}

	public void Purge(SecurityIdentifier sid)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		ThrowIfNotCanonical();
		for (int num = Count - 1; num >= 0; num--)
		{
			KnownAce knownAce = _acl[num] as KnownAce;
			if (!(knownAce == null) && (knownAce.AceFlags & AceFlags.Inherited) == 0 && knownAce.SecurityIdentifier == sid)
			{
				_acl.RemoveAce(num);
			}
		}
		OnAclModificationTried();
	}
}
