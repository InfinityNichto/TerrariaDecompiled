using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReLogic.Peripherals.RGB;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RgbProcessorAttribute : Attribute
{
	public readonly ReadOnlyCollection<EffectDetailLevel> SupportedDetailLevels;

	public bool IsTransparent { get; set; }

	public RgbProcessorAttribute(params EffectDetailLevel[] detailLevels)
	{
		IsTransparent = false;
		SupportedDetailLevels = new ReadOnlyCollection<EffectDetailLevel>(detailLevels.Distinct().ToList());
	}
}
