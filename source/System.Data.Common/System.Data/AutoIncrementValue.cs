namespace System.Data;

internal abstract class AutoIncrementValue
{
	internal bool Auto { get; set; }

	internal abstract object Current { get; set; }

	internal abstract long Seed { get; set; }

	internal abstract long Step { get; set; }

	internal abstract Type DataType { get; }

	internal abstract void SetCurrent(object value, IFormatProvider formatProvider);

	internal abstract void SetCurrentAndIncrement(object value);

	internal abstract void MoveAfter();

	internal AutoIncrementValue Clone()
	{
		AutoIncrementValue autoIncrementValue = ((this is AutoIncrementInt64) ? ((AutoIncrementValue)new AutoIncrementInt64()) : ((AutoIncrementValue)new AutoIncrementBigInteger()));
		autoIncrementValue.Auto = Auto;
		autoIncrementValue.Seed = Seed;
		autoIncrementValue.Step = Step;
		autoIncrementValue.Current = Current;
		return autoIncrementValue;
	}
}
