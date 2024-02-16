using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

public class WindowsPrincipal : ClaimsPrincipal
{
	private readonly WindowsIdentity _identity;

	public override IIdentity Identity => _identity;

	public virtual IEnumerable<Claim> UserClaims
	{
		get
		{
			foreach (ClaimsIdentity identity in Identities)
			{
				if (!(identity is WindowsIdentity windowsIdentity))
				{
					continue;
				}
				foreach (Claim userClaim in windowsIdentity.UserClaims)
				{
					yield return userClaim;
				}
			}
		}
	}

	public virtual IEnumerable<Claim> DeviceClaims
	{
		get
		{
			foreach (ClaimsIdentity identity in Identities)
			{
				if (!(identity is WindowsIdentity windowsIdentity))
				{
					continue;
				}
				foreach (Claim deviceClaim in windowsIdentity.DeviceClaims)
				{
					yield return deviceClaim;
				}
			}
		}
	}

	public WindowsPrincipal(WindowsIdentity ntIdentity)
		: base(ntIdentity)
	{
		_identity = ntIdentity ?? throw new ArgumentNullException("ntIdentity");
	}

	public override bool IsInRole(string role)
	{
		if (role == null || role.Length == 0)
		{
			return false;
		}
		NTAccount identity = new NTAccount(role);
		IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection(1);
		identityReferenceCollection.Add(identity);
		IdentityReferenceCollection identityReferenceCollection2 = NTAccount.Translate(identityReferenceCollection, typeof(SecurityIdentifier), forceSuccess: false);
		if (identityReferenceCollection2[0] is SecurityIdentifier sid && IsInRole(sid))
		{
			return true;
		}
		return base.IsInRole(role);
	}

	public virtual bool IsInRole(WindowsBuiltInRole role)
	{
		if (role < WindowsBuiltInRole.Administrator || role > WindowsBuiltInRole.Replicator)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, (int)role), "role");
		}
		return IsInRole((int)role);
	}

	public virtual bool IsInRole(int rid)
	{
		Span<int> span = stackalloc int[2] { 32, rid };
		return IsInRole(new SecurityIdentifier(IdentifierAuthority.NTAuthority, span));
	}

	public virtual bool IsInRole(SecurityIdentifier sid)
	{
		if (sid == null)
		{
			throw new ArgumentNullException("sid");
		}
		if (_identity.AccessToken.IsInvalid)
		{
			return false;
		}
		SafeAccessTokenHandle phNewToken = SafeAccessTokenHandle.InvalidHandle;
		if (_identity.ImpersonationLevel == TokenImpersonationLevel.None && !global::Interop.Advapi32.DuplicateTokenEx(_identity.AccessToken, 8u, IntPtr.Zero, 2u, 2u, ref phNewToken))
		{
			throw new SecurityException(new Win32Exception().Message);
		}
		bool IsMember = false;
		if (!global::Interop.Advapi32.CheckTokenMembership((_identity.ImpersonationLevel != 0) ? _identity.AccessToken : phNewToken, sid.BinaryForm, ref IsMember))
		{
			throw new SecurityException(new Win32Exception().Message);
		}
		phNewToken.Dispose();
		return IsMember;
	}

	private static IPrincipal GetDefaultInstance()
	{
		return new WindowsPrincipal(WindowsIdentity.GetCurrent());
	}
}
