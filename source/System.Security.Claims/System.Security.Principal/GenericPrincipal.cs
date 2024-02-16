using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace System.Security.Principal;

public class GenericPrincipal : ClaimsPrincipal
{
	private readonly IIdentity m_identity;

	private readonly string[] m_roles;

	public override IIdentity Identity => m_identity;

	public GenericPrincipal(IIdentity identity, string[]? roles)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		m_identity = identity;
		if (roles != null)
		{
			m_roles = (string[])roles.Clone();
		}
		else
		{
			m_roles = null;
		}
		AddIdentityWithRoles(m_identity, m_roles);
	}

	private void AddIdentityWithRoles(IIdentity identity, string[] roles)
	{
		ClaimsIdentity claimsIdentity2 = ((!(identity is ClaimsIdentity claimsIdentity)) ? new ClaimsIdentity(identity) : claimsIdentity.Clone());
		if (roles != null && roles.Length != 0)
		{
			List<Claim> list = new List<Claim>(roles.Length);
			foreach (string value in roles)
			{
				if (!string.IsNullOrWhiteSpace(value))
				{
					list.Add(new Claim(claimsIdentity2.RoleClaimType, value, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", claimsIdentity2));
				}
			}
			claimsIdentity2.ExternalClaims.Add(list);
		}
		base.AddIdentity(claimsIdentity2);
	}

	public override bool IsInRole([NotNullWhen(true)] string? role)
	{
		if (role == null || m_roles == null)
		{
			return false;
		}
		for (int i = 0; i < m_roles.Length; i++)
		{
			if (string.Equals(m_roles[i], role, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return base.IsInRole(role);
	}

	private static IPrincipal GetDefaultInstance()
	{
		return new GenericPrincipal(new GenericIdentity(string.Empty), new string[1] { string.Empty });
	}
}
