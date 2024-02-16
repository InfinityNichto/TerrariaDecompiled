using System.Collections;

namespace System.Security.AccessControl;

public sealed class AuthorizationRuleCollection : ReadOnlyCollectionBase
{
	public AuthorizationRule? this[int index] => base.InnerList[index] as AuthorizationRule;

	public void AddRule(AuthorizationRule? rule)
	{
		base.InnerList.Add(rule);
	}

	public void CopyTo(AuthorizationRule[] rules, int index)
	{
		((ICollection)this).CopyTo((Array)rules, index);
	}
}
