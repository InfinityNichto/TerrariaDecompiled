using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Property)]
[Obsolete("RecommendedAsConfigurableAttribute has been deprecated. Use System.ComponentModel.SettingsBindableAttribute instead.")]
public class RecommendedAsConfigurableAttribute : Attribute
{
	public static readonly RecommendedAsConfigurableAttribute No = new RecommendedAsConfigurableAttribute(recommendedAsConfigurable: false);

	public static readonly RecommendedAsConfigurableAttribute Yes = new RecommendedAsConfigurableAttribute(recommendedAsConfigurable: true);

	public static readonly RecommendedAsConfigurableAttribute Default = No;

	public bool RecommendedAsConfigurable { get; }

	public RecommendedAsConfigurableAttribute(bool recommendedAsConfigurable)
	{
		RecommendedAsConfigurable = recommendedAsConfigurable;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is RecommendedAsConfigurableAttribute recommendedAsConfigurableAttribute)
		{
			return recommendedAsConfigurableAttribute.RecommendedAsConfigurable == RecommendedAsConfigurable;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return !RecommendedAsConfigurable;
	}
}
