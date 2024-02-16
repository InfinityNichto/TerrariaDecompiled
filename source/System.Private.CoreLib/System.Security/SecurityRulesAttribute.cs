namespace System.Security;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class SecurityRulesAttribute : Attribute
{
	public bool SkipVerificationInFullTrust { get; set; }

	public SecurityRuleSet RuleSet { get; }

	public SecurityRulesAttribute(SecurityRuleSet ruleSet)
	{
		RuleSet = ruleSet;
	}
}
