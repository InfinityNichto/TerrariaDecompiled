using System.Collections.Generic;

namespace Terraria.GameContent.ItemDropRules;

public class ItemDropWithConditionRule : CommonDrop
{
	public IItemDropRuleCondition condition;

	public ItemDropWithConditionRule(int itemId, int chanceDenominator, int amountDroppedMinimum, int amountDroppedMaximum, IItemDropRuleCondition condition, int chanceNumerator = 1)
		: base(itemId, chanceDenominator, amountDroppedMinimum, amountDroppedMaximum, chanceNumerator)
	{
		this.condition = condition;
	}

	public override bool CanDrop(DropAttemptInfo info)
	{
		return condition.CanDrop(info);
	}

	public override void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
	{
		DropRateInfoChainFeed ratesInfo2 = ratesInfo.With(1f);
		ratesInfo2.AddCondition(condition);
		float num = (float)chanceNumerator / (float)chanceDenominator;
		float dropRate = num * ratesInfo2.parentDroprateChance;
		drops.Add(new DropRateInfo(itemId, amountDroppedMinimum, amountDroppedMaximum, dropRate, ratesInfo2.conditions));
		Chains.ReportDroprates(base.ChainedRules, num, drops, ratesInfo2);
	}
}
