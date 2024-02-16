using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

public sealed class NTAccount : IdentityReference
{
	private readonly string _name;

	public override string Value => ToString();

	public NTAccount(string domainName, string accountName)
	{
		if (accountName == null)
		{
			throw new ArgumentNullException("accountName");
		}
		if (accountName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_StringZeroLength, "accountName");
		}
		if (accountName.Length > 256)
		{
			throw new ArgumentException(System.SR.IdentityReference_AccountNameTooLong, "accountName");
		}
		if (domainName != null && domainName.Length > 255)
		{
			throw new ArgumentException(System.SR.IdentityReference_DomainNameTooLong, "domainName");
		}
		if (domainName == null || domainName.Length == 0)
		{
			_name = accountName;
		}
		else
		{
			_name = domainName + "\\" + accountName;
		}
	}

	public NTAccount(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_StringZeroLength, "name");
		}
		if (name.Length > 512)
		{
			throw new ArgumentException(System.SR.IdentityReference_AccountNameTooLong, "name");
		}
		_name = name;
	}

	public override bool IsValidTargetType(Type targetType)
	{
		if (targetType == typeof(SecurityIdentifier))
		{
			return true;
		}
		if (targetType == typeof(NTAccount))
		{
			return true;
		}
		return false;
	}

	public override IdentityReference Translate(Type targetType)
	{
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		if (targetType == typeof(NTAccount))
		{
			return this;
		}
		if (targetType == typeof(SecurityIdentifier))
		{
			IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection(1);
			identityReferenceCollection.Add(this);
			IdentityReferenceCollection identityReferenceCollection2 = Translate(identityReferenceCollection, targetType, forceSuccess: true);
			return identityReferenceCollection2[0];
		}
		throw new ArgumentException(System.SR.IdentityReference_MustBeIdentityReference, "targetType");
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		return this == o as NTAccount;
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
	}

	public override string ToString()
	{
		return _name;
	}

	internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceAccounts, Type targetType, bool forceSuccess)
	{
		bool someFailed;
		IdentityReferenceCollection identityReferenceCollection = Translate(sourceAccounts, targetType, out someFailed);
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

	internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceAccounts, Type targetType, out bool someFailed)
	{
		if (sourceAccounts == null)
		{
			throw new ArgumentNullException("sourceAccounts");
		}
		if (targetType == typeof(SecurityIdentifier))
		{
			return TranslateToSids(sourceAccounts, out someFailed);
		}
		throw new ArgumentException(System.SR.IdentityReference_MustBeIdentityReference, "targetType");
	}

	public static bool operator ==(NTAccount? left, NTAccount? right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null || (object)right == null)
		{
			return false;
		}
		return left.ToString().Equals(right.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	public static bool operator !=(NTAccount? left, NTAccount? right)
	{
		return !(left == right);
	}

	private static IdentityReferenceCollection TranslateToSids(IdentityReferenceCollection sourceAccounts, out bool someFailed)
	{
		if (sourceAccounts == null)
		{
			throw new ArgumentNullException("sourceAccounts");
		}
		if (sourceAccounts.Count == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyCollection, "sourceAccounts");
		}
		SafeLsaPolicyHandle safeLsaPolicyHandle = null;
		SafeLsaMemoryHandle referencedDomains = null;
		SafeLsaMemoryHandle sids = null;
		try
		{
			global::Interop.Advapi32.MARSHALLED_UNICODE_STRING[] array = new global::Interop.Advapi32.MARSHALLED_UNICODE_STRING[sourceAccounts.Count];
			int num = 0;
			foreach (IdentityReference sourceAccount in sourceAccounts)
			{
				if (!(sourceAccount is NTAccount nTAccount))
				{
					throw new ArgumentException(System.SR.Argument_ImproperType, "sourceAccounts");
				}
				array[num].Buffer = nTAccount.ToString();
				if (array[num].Buffer.Length * 2 + 2 > 65535)
				{
					throw new InvalidOperationException();
				}
				array[num].Length = (ushort)(array[num].Buffer.Length * 2);
				array[num].MaximumLength = (ushort)(array[num].Length + 2);
				num++;
			}
			safeLsaPolicyHandle = Win32.LsaOpenPolicy(null, PolicyRights.POLICY_LOOKUP_NAMES);
			someFailed = false;
			uint num2 = global::Interop.Advapi32.LsaLookupNames2(safeLsaPolicyHandle, 0, sourceAccounts.Count, array, out referencedDomains, out sids);
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
				_ = 1789;
				throw new Win32Exception((int)error);
			}
			case 0u:
				break;
			}
			IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection(sourceAccounts.Count);
			if (num2 == 0 || num2 == 263)
			{
				sids.Initialize((uint)sourceAccounts.Count, (uint)Marshal.SizeOf<global::Interop.LSA_TRANSLATED_SID2>());
				Win32.InitializeReferencedDomainsPointer(referencedDomains);
				global::Interop.LSA_TRANSLATED_SID2[] array2 = new global::Interop.LSA_TRANSLATED_SID2[sourceAccounts.Count];
				sids.ReadArray(0uL, array2, 0, array2.Length);
				for (int i = 0; i < sourceAccounts.Count; i++)
				{
					global::Interop.LSA_TRANSLATED_SID2 lSA_TRANSLATED_SID = array2[i];
					switch ((SidNameUse)lSA_TRANSLATED_SID.Use)
					{
					case SidNameUse.User:
					case SidNameUse.Group:
					case SidNameUse.Alias:
					case SidNameUse.WellKnownGroup:
					case SidNameUse.Computer:
						identityReferenceCollection.Add(new SecurityIdentifier(lSA_TRANSLATED_SID.Sid));
						break;
					default:
						someFailed = true;
						identityReferenceCollection.Add(sourceAccounts[i]);
						break;
					}
				}
			}
			else
			{
				for (int j = 0; j < sourceAccounts.Count; j++)
				{
					identityReferenceCollection.Add(sourceAccounts[j]);
				}
			}
			return identityReferenceCollection;
		}
		finally
		{
			safeLsaPolicyHandle?.Dispose();
			referencedDomains?.Dispose();
			sids?.Dispose();
		}
	}
}
