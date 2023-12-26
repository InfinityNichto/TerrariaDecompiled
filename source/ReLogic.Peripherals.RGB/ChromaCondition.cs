using System;

namespace ReLogic.Peripherals.RGB;

public abstract class ChromaCondition
{
	public class Custom : ChromaCondition
	{
		private Func<bool> _condition;

		public Custom(Func<bool> condition)
		{
			_condition = condition;
		}

		public override bool IsActive()
		{
			return _condition();
		}
	}

	public abstract bool IsActive();
}
