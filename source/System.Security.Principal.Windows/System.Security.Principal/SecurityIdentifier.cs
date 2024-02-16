using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

public sealed class SecurityIdentifier : IdentityReference, IComparable<SecurityIdentifier>
{
	public static readonly int MinBinaryLength = 8;

	public static readonly int MaxBinaryLength = 68;

	private IdentifierAuthority _identifierAuthority;

	private int[] _subAuthorities;

	private byte[] _binaryForm;

	private SecurityIdentifier _accountDomainSid;

	private bool _accountDomainSidInitialized;

	private string _sddlForm;

	internal static byte Revision => 1;

	internal byte[] BinaryForm => _binaryForm;

	internal IdentifierAuthority IdentifierAuthority => _identifierAuthority;

	internal int SubAuthorityCount => _subAuthorities.Length;

	public int BinaryLength => _binaryForm.Length;

	public SecurityIdentifier? AccountDomainSid
	{
		get
		{
			if (!_accountDomainSidInitialized)
			{
				_accountDomainSid = GetAccountDomainSid();
				_accountDomainSidInitialized = true;
			}
			return _accountDomainSid;
		}
	}

	public override string Value => ToString().ToUpperInvariant();

	[MemberNotNull("_binaryForm")]
	[MemberNotNull("_subAuthorities")]
	private void CreateFromParts(IdentifierAuthority identifierAuthority, ReadOnlySpan<int> subAuthorities)
	{
		if (subAuthorities.Length > 15)
		{
			throw new ArgumentOutOfRangeException("subAuthorities.Length", subAuthorities.Length, System.SR.Format(System.SR.IdentityReference_InvalidNumberOfSubauthorities, 15));
		}
		if (identifierAuthority < IdentifierAuthority.NullAuthority || identifierAuthority > (IdentifierAuthority)281474976710655L)
		{
			throw new ArgumentOutOfRangeException("identifierAuthority", identifierAuthority, System.SR.IdentityReference_IdentifierAuthorityTooLarge);
		}
		_identifierAuthority = identifierAuthority;
		_subAuthorities = subAuthorities.ToArray();
		_binaryForm = new byte[8 + 4 * _subAuthorities.Length];
		_binaryForm[0] = Revision;
		_binaryForm[1] = (byte)_subAuthorities.Length;
		for (int i = 0; i < 6; i++)
		{
			_binaryForm[2 + i] = (byte)(((ulong)_identifierAuthority >> (5 - i) * 8) & 0xFF);
		}
		for (int j = 0; j < _subAuthorities.Length; j++)
		{
			for (byte b = 0; b < 4; b++)
			{
				_binaryForm[8 + 4 * j + b] = (byte)((ulong)_subAuthorities[j] >> b * 8);
			}
		}
	}

	[MemberNotNull("_binaryForm")]
	[MemberNotNull("_subAuthorities")]
	private void CreateFromBinaryForm(byte[] binaryForm, int offset)
	{
		if (binaryForm == null)
		{
			throw new ArgumentNullException("binaryForm");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", offset, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (binaryForm.Length - offset < MinBinaryLength)
		{
			throw new ArgumentOutOfRangeException("binaryForm", System.SR.ArgumentOutOfRange_ArrayTooSmall);
		}
		if (binaryForm[offset] != Revision)
		{
			throw new ArgumentException(System.SR.IdentityReference_InvalidSidRevision, "binaryForm");
		}
		int num = binaryForm[offset + 1];
		if (num > 15)
		{
			throw new ArgumentException(System.SR.Format(System.SR.IdentityReference_InvalidNumberOfSubauthorities, 15), "binaryForm");
		}
		int num2 = 8 + 4 * num;
		if (binaryForm.Length - offset < num2)
		{
			throw new ArgumentException(System.SR.ArgumentOutOfRange_ArrayTooSmall, "binaryForm");
		}
		Span<int> span = stackalloc int[15];
		IdentifierAuthority identifierAuthority = (IdentifierAuthority)(((ulong)binaryForm[offset + 2] << 40) + ((ulong)binaryForm[offset + 3] << 32) + ((ulong)binaryForm[offset + 4] << 24) + ((ulong)binaryForm[offset + 5] << 16) + ((ulong)binaryForm[offset + 6] << 8) + binaryForm[offset + 7]);
		for (int i = 0; i < num; i++)
		{
			span[i] = binaryForm[offset + 8 + 4 * i] + (binaryForm[offset + 8 + 4 * i + 1] << 8) + (binaryForm[offset + 8 + 4 * i + 2] << 16) + (binaryForm[offset + 8 + 4 * i + 3] << 24);
		}
		CreateFromParts(identifierAuthority, span.Slice(0, num));
	}

	public SecurityIdentifier(string sddlForm)
	{
		if (sddlForm == null)
		{
			throw new ArgumentNullException("sddlForm");
		}
		byte[] resultSid;
		int num = Win32.CreateSidFromString(sddlForm, out resultSid);
		switch (num)
		{
		case 1337:
			throw new ArgumentException(System.SR.Argument_InvalidValue, "sddlForm");
		case 8:
			throw new OutOfMemoryException();
		default:
			throw new Win32Exception(num);
		case 0:
			CreateFromBinaryForm(resultSid, 0);
			break;
		}
	}

	public SecurityIdentifier(byte[] binaryForm, int offset)
	{
		if (binaryForm == null)
		{
			throw new ArgumentNullException("binaryForm");
		}
		CreateFromBinaryForm(binaryForm, offset);
	}

	public SecurityIdentifier(IntPtr binaryForm)
		: this(Win32.ConvertIntPtrSidToByteArraySid(binaryForm), 0)
	{
	}

	public SecurityIdentifier(WellKnownSidType sidType, SecurityIdentifier? domainSid)
	{
		int windowsAccountDomainSid;
		switch (sidType)
		{
		case WellKnownSidType.LogonIdsSid:
			throw new ArgumentException(System.SR.IdentityReference_CannotCreateLogonIdsSid, "sidType");
		default:
			throw new ArgumentException(System.SR.Argument_InvalidValue, "sidType");
		case WellKnownSidType.AccountAdministratorSid:
		case WellKnownSidType.AccountGuestSid:
		case WellKnownSidType.AccountKrbtgtSid:
		case WellKnownSidType.AccountDomainAdminsSid:
		case WellKnownSidType.AccountDomainUsersSid:
		case WellKnownSidType.AccountDomainGuestsSid:
		case WellKnownSidType.AccountComputersSid:
		case WellKnownSidType.AccountControllersSid:
		case WellKnownSidType.AccountCertAdminsSid:
		case WellKnownSidType.AccountSchemaAdminsSid:
		case WellKnownSidType.AccountEnterpriseAdminsSid:
		case WellKnownSidType.AccountPolicyAdminsSid:
		case WellKnownSidType.AccountRasAndIasServersSid:
		{
			if (domainSid == null)
			{
				throw new ArgumentNullException("domainSid", System.SR.Format(System.SR.IdentityReference_DomainSidRequired, sidType));
			}
			windowsAccountDomainSid = Win32.GetWindowsAccountDomainSid(domainSid, out var resultSid);
			switch (windowsAccountDomainSid)
			{
			case 122:
				throw new OutOfMemoryException();
			case 1257:
				throw new ArgumentException(System.SR.IdentityReference_NotAWindowsDomain, "domainSid");
			default:
				throw new Win32Exception(windowsAccountDomainSid);
			case 0:
				break;
			}
			if (resultSid != domainSid)
			{
				throw new ArgumentException(System.SR.IdentityReference_NotAWindowsDomain, "domainSid");
			}
			break;
		}
		case WellKnownSidType.NullSid:
		case WellKnownSidType.WorldSid:
		case WellKnownSidType.LocalSid:
		case WellKnownSidType.CreatorOwnerSid:
		case WellKnownSidType.CreatorGroupSid:
		case WellKnownSidType.CreatorOwnerServerSid:
		case WellKnownSidType.CreatorGroupServerSid:
		case WellKnownSidType.NTAuthoritySid:
		case WellKnownSidType.DialupSid:
		case WellKnownSidType.NetworkSid:
		case WellKnownSidType.BatchSid:
		case WellKnownSidType.InteractiveSid:
		case WellKnownSidType.ServiceSid:
		case WellKnownSidType.AnonymousSid:
		case WellKnownSidType.ProxySid:
		case WellKnownSidType.EnterpriseControllersSid:
		case WellKnownSidType.SelfSid:
		case WellKnownSidType.AuthenticatedUserSid:
		case WellKnownSidType.RestrictedCodeSid:
		case WellKnownSidType.TerminalServerSid:
		case WellKnownSidType.RemoteLogonIdSid:
		case WellKnownSidType.LocalSystemSid:
		case WellKnownSidType.LocalServiceSid:
		case WellKnownSidType.NetworkServiceSid:
		case WellKnownSidType.BuiltinDomainSid:
		case WellKnownSidType.BuiltinAdministratorsSid:
		case WellKnownSidType.BuiltinUsersSid:
		case WellKnownSidType.BuiltinGuestsSid:
		case WellKnownSidType.BuiltinPowerUsersSid:
		case WellKnownSidType.BuiltinAccountOperatorsSid:
		case WellKnownSidType.BuiltinSystemOperatorsSid:
		case WellKnownSidType.BuiltinPrintOperatorsSid:
		case WellKnownSidType.BuiltinBackupOperatorsSid:
		case WellKnownSidType.BuiltinReplicatorSid:
		case WellKnownSidType.BuiltinPreWindows2000CompatibleAccessSid:
		case WellKnownSidType.BuiltinRemoteDesktopUsersSid:
		case WellKnownSidType.BuiltinNetworkConfigurationOperatorsSid:
		case WellKnownSidType.NtlmAuthenticationSid:
		case WellKnownSidType.DigestAuthenticationSid:
		case WellKnownSidType.SChannelAuthenticationSid:
		case WellKnownSidType.ThisOrganizationSid:
		case WellKnownSidType.OtherOrganizationSid:
		case WellKnownSidType.BuiltinIncomingForestTrustBuildersSid:
		case WellKnownSidType.BuiltinPerformanceMonitoringUsersSid:
		case WellKnownSidType.BuiltinPerformanceLoggingUsersSid:
		case WellKnownSidType.BuiltinAuthorizationAccessSid:
		case WellKnownSidType.WinBuiltinTerminalServerLicenseServersSid:
		case WellKnownSidType.WinBuiltinDCOMUsersSid:
		case WellKnownSidType.WinBuiltinIUsersSid:
		case WellKnownSidType.WinIUserSid:
		case WellKnownSidType.WinBuiltinCryptoOperatorsSid:
		case WellKnownSidType.WinUntrustedLabelSid:
		case WellKnownSidType.WinLowLabelSid:
		case WellKnownSidType.WinMediumLabelSid:
		case WellKnownSidType.WinHighLabelSid:
		case WellKnownSidType.WinSystemLabelSid:
		case WellKnownSidType.WinWriteRestrictedCodeSid:
		case WellKnownSidType.WinCreatorOwnerRightsSid:
		case WellKnownSidType.WinCacheablePrincipalsGroupSid:
		case WellKnownSidType.WinNonCacheablePrincipalsGroupSid:
		case WellKnownSidType.WinEnterpriseReadonlyControllersSid:
		case WellKnownSidType.WinAccountReadonlyControllersSid:
		case WellKnownSidType.WinBuiltinEventLogReadersGroup:
		case WellKnownSidType.WinNewEnterpriseReadonlyControllersSid:
		case WellKnownSidType.WinBuiltinCertSvcDComAccessGroup:
		case WellKnownSidType.WinMediumPlusLabelSid:
		case WellKnownSidType.WinLocalLogonSid:
		case WellKnownSidType.WinConsoleLogonSid:
		case WellKnownSidType.WinThisOrganizationCertificateSid:
		case WellKnownSidType.WinApplicationPackageAuthoritySid:
		case WellKnownSidType.WinBuiltinAnyPackageSid:
		case WellKnownSidType.WinCapabilityInternetClientSid:
		case WellKnownSidType.WinCapabilityInternetClientServerSid:
		case WellKnownSidType.WinCapabilityPrivateNetworkClientServerSid:
		case WellKnownSidType.WinCapabilityPicturesLibrarySid:
		case WellKnownSidType.WinCapabilityVideosLibrarySid:
		case WellKnownSidType.WinCapabilityMusicLibrarySid:
		case WellKnownSidType.WinCapabilityDocumentsLibrarySid:
		case WellKnownSidType.WinCapabilitySharedUserCertificatesSid:
		case WellKnownSidType.WinCapabilityEnterpriseAuthenticationSid:
		case WellKnownSidType.WinCapabilityRemovableStorageSid:
			break;
		}
		windowsAccountDomainSid = Win32.CreateWellKnownSid(sidType, domainSid, out var resultSid2);
		switch (windowsAccountDomainSid)
		{
		case 87:
			throw new ArgumentException(new Win32Exception(windowsAccountDomainSid).Message, "sidType/domainSid");
		default:
			throw new Win32Exception(windowsAccountDomainSid);
		case 0:
			CreateFromBinaryForm(resultSid2, 0);
			break;
		}
	}

	internal SecurityIdentifier(IdentifierAuthority identifierAuthority, ReadOnlySpan<int> subAuthorities)
	{
		CreateFromParts(identifierAuthority, subAuthorities);
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		return this == o as SecurityIdentifier;
	}

	public bool Equals(SecurityIdentifier sid)
	{
		return this == sid;
	}

	public override int GetHashCode()
	{
		int num = ((long)IdentifierAuthority).GetHashCode();
		for (int i = 0; i < SubAuthorityCount; i++)
		{
			num ^= GetSubAuthority(i);
		}
		return num;
	}

	public override string ToString()
	{
		if (_sddlForm == null)
		{
			Span<char> span = stackalloc char[189];
			span[0] = 'S';
			span[1] = '-';
			span[2] = '1';
			span[3] = '-';
			int num = 4;
			ulong identifierAuthority = (ulong)_identifierAuthority;
			identifierAuthority.TryFormat(span.Slice(num), out var charsWritten);
			num += charsWritten;
			int[] subAuthorities = _subAuthorities;
			for (int i = 0; i < subAuthorities.Length; i++)
			{
				span[num] = '-';
				num++;
				uint num2 = (uint)subAuthorities[i];
				num2.TryFormat(span.Slice(num), out charsWritten);
				num += charsWritten;
			}
			_sddlForm = span.Slice(0, num).ToString();
		}
		return _sddlForm;
	}

	internal static bool IsValidTargetTypeStatic(Type targetType)
	{
		if (targetType == typeof(NTAccount))
		{
			return true;
		}
		if (targetType == typeof(SecurityIdentifier))
		{
			return true;
		}
		return false;
	}

	public override bool IsValidTargetType(Type targetType)
	{
		return IsValidTargetTypeStatic(targetType);
	}

	internal SecurityIdentifier GetAccountDomainSid()
	{
		SecurityIdentifier resultSid;
		int windowsAccountDomainSid = Win32.GetWindowsAccountDomainSid(this, out resultSid);
		return windowsAccountDomainSid switch
		{
			122 => throw new OutOfMemoryException(), 
			1257 => null, 
			0 => resultSid, 
			_ => throw new Win32Exception(windowsAccountDomainSid), 
		};
	}

	public bool IsAccountSid()
	{
		if (!_accountDomainSidInitialized)
		{
			_accountDomainSid = GetAccountDomainSid();
			_accountDomainSidInitialized = true;
		}
		if (_accountDomainSid == null)
		{
			return false;
		}
		return true;
	}

	public override IdentityReference Translate(Type targetType)
	{
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		if (targetType == typeof(SecurityIdentifier))
		{
			return this;
		}
		if (targetType == typeof(NTAccount))
		{
			IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection(1);
			identityReferenceCollection.Add(this);
			IdentityReferenceCollection identityReferenceCollection2 = Translate(identityReferenceCollection, targetType, forceSuccess: true);
			return identityReferenceCollection2[0];
		}
		throw new ArgumentException(System.SR.IdentityReference_MustBeIdentityReference, "targetType");
	}

	public static bool operator ==(SecurityIdentifier? left, SecurityIdentifier? right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null || (object)right == null)
		{
			return false;
		}
		return left.CompareTo(right) == 0;
	}

	public static bool operator !=(SecurityIdentifier? left, SecurityIdentifier? right)
	{
		return !(left == right);
	}

	public int CompareTo(SecurityIdentifier? sid)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		if (IdentifierAuthority < sid.IdentifierAuthority)
		{
			return -1;
		}
		if (IdentifierAuthority > sid.IdentifierAuthority)
		{
			return 1;
		}
		if (SubAuthorityCount < sid.SubAuthorityCount)
		{
			return -1;
		}
		if (SubAuthorityCount > sid.SubAuthorityCount)
		{
			return 1;
		}
		for (int i = 0; i < SubAuthorityCount; i++)
		{
			int num = GetSubAuthority(i) - sid.GetSubAuthority(i);
			if (num != 0)
			{
				return num;
			}
		}
		return 0;
	}

	internal int GetSubAuthority(int index)
	{
		return _subAuthorities[index];
	}

	public bool IsWellKnown(WellKnownSidType type)
	{
		return Win32.IsWellKnownSid(this, type);
	}

	public void GetBinaryForm(byte[] binaryForm, int offset)
	{
		_binaryForm.CopyTo(binaryForm, offset);
	}

	public bool IsEqualDomainSid(SecurityIdentifier sid)
	{
		return Win32.IsEqualDomainSid(this, sid);
	}

	private static IdentityReferenceCollection TranslateToNTAccounts(IdentityReferenceCollection sourceSids, out bool someFailed)
	{
		if (sourceSids == null)
		{
			throw new ArgumentNullException("sourceSids");
		}
		if (sourceSids.Count == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyCollection, "sourceSids");
		}
		IntPtr[] array = new IntPtr[sourceSids.Count];
		GCHandle[] array2 = new GCHandle[sourceSids.Count];
		SafeLsaPolicyHandle safeLsaPolicyHandle = null;
		SafeLsaMemoryHandle referencedDomains = null;
		SafeLsaMemoryHandle names = null;
		try
		{
			int num = 0;
			foreach (IdentityReference sourceSid in sourceSids)
			{
				if (!(sourceSid is SecurityIdentifier securityIdentifier))
				{
					throw new ArgumentException(System.SR.Argument_ImproperType, "sourceSids");
				}
				array2[num] = GCHandle.Alloc(securityIdentifier.BinaryForm, GCHandleType.Pinned);
				array[num] = array2[num].AddrOfPinnedObject();
				num++;
			}
			safeLsaPolicyHandle = Win32.LsaOpenPolicy(null, PolicyRights.POLICY_LOOKUP_NAMES);
			someFailed = false;
			uint num2 = global::Interop.Advapi32.LsaLookupSids(safeLsaPolicyHandle, sourceSids.Count, array, out referencedDomains, out names);
			switch (num2)
			{
			case 3221225495u:
			case 3221225626u:
				throw new OutOfMemoryException();
			case 3221225506u:
				throw new UnauthorizedAccessException();
			case 3221225587u:
			case 263u:
				someFailed = true;
				break;
			default:
			{
				uint error = global::Interop.Advapi32.LsaNtStatusToWinError(num2);
				throw new Win32Exception((int)error);
			}
			case 0u:
				break;
			}
			names.Initialize((uint)sourceSids.Count, (uint)Marshal.SizeOf<global::Interop.LSA_TRANSLATED_NAME>());
			Win32.InitializeReferencedDomainsPointer(referencedDomains);
			IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection(sourceSids.Count);
			if (num2 == 0 || num2 == 263)
			{
				global::Interop.LSA_REFERENCED_DOMAIN_LIST lSA_REFERENCED_DOMAIN_LIST = referencedDomains.Read<global::Interop.LSA_REFERENCED_DOMAIN_LIST>(0uL);
				string[] array3 = new string[lSA_REFERENCED_DOMAIN_LIST.Entries];
				for (int i = 0; i < lSA_REFERENCED_DOMAIN_LIST.Entries; i++)
				{
					global::Interop.LSA_TRUST_INFORMATION lSA_TRUST_INFORMATION = Marshal.PtrToStructure<global::Interop.LSA_TRUST_INFORMATION>(new IntPtr((long)lSA_REFERENCED_DOMAIN_LIST.Domains + i * Marshal.SizeOf<global::Interop.LSA_TRUST_INFORMATION>()));
					array3[i] = Marshal.PtrToStringUni(lSA_TRUST_INFORMATION.Name.Buffer, lSA_TRUST_INFORMATION.Name.Length / 2);
				}
				global::Interop.LSA_TRANSLATED_NAME[] array4 = new global::Interop.LSA_TRANSLATED_NAME[sourceSids.Count];
				names.ReadArray(0uL, array4, 0, array4.Length);
				for (int j = 0; j < sourceSids.Count; j++)
				{
					global::Interop.LSA_TRANSLATED_NAME lSA_TRANSLATED_NAME = array4[j];
					switch ((SidNameUse)lSA_TRANSLATED_NAME.Use)
					{
					case SidNameUse.User:
					case SidNameUse.Group:
					case SidNameUse.Alias:
					case SidNameUse.WellKnownGroup:
					case SidNameUse.Computer:
					{
						string accountName = Marshal.PtrToStringUni(lSA_TRANSLATED_NAME.Name.Buffer, lSA_TRANSLATED_NAME.Name.Length / 2);
						string domainName = array3[lSA_TRANSLATED_NAME.DomainIndex];
						identityReferenceCollection.Add(new NTAccount(domainName, accountName));
						break;
					}
					default:
						someFailed = true;
						identityReferenceCollection.Add(sourceSids[j]);
						break;
					}
				}
			}
			else
			{
				for (int k = 0; k < sourceSids.Count; k++)
				{
					identityReferenceCollection.Add(sourceSids[k]);
				}
			}
			return identityReferenceCollection;
		}
		finally
		{
			for (int l = 0; l < sourceSids.Count; l++)
			{
				if (array2[l].IsAllocated)
				{
					array2[l].Free();
				}
			}
			safeLsaPolicyHandle?.Dispose();
			referencedDomains?.Dispose();
			names?.Dispose();
		}
	}

	internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceSids, Type targetType, bool forceSuccess)
	{
		bool someFailed;
		IdentityReferenceCollection identityReferenceCollection = Translate(sourceSids, targetType, out someFailed);
		if (forceSuccess && someFailed)
		{
			IdentityReferenceCollection identityReferenceCollection2 = new IdentityReferenceCollection();
			foreach (IdentityReference item in identityReferenceCollection)
			{
				if (item.GetType() != targetType)
				{
					identityReferenceCollection2.Add(item);
				}
			}
			throw new IdentityNotMappedException(System.SR.IdentityReference_IdentityNotMapped, identityReferenceCollection2);
		}
		return identityReferenceCollection;
	}

	internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceSids, Type targetType, out bool someFailed)
	{
		if (sourceSids == null)
		{
			throw new ArgumentNullException("sourceSids");
		}
		if (targetType == typeof(NTAccount))
		{
			return TranslateToNTAccounts(sourceSids, out someFailed);
		}
		throw new ArgumentException(System.SR.IdentityReference_MustBeIdentityReference, "targetType");
	}
}
