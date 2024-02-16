namespace System.Runtime.InteropServices;

public sealed class CurrencyWrapper
{
	public decimal WrappedObject { get; }

	public CurrencyWrapper(decimal obj)
	{
		WrappedObject = obj;
	}

	public CurrencyWrapper(object obj)
	{
		if (!(obj is decimal))
		{
			throw new ArgumentException(SR.Arg_MustBeDecimal, "obj");
		}
		WrappedObject = (decimal)obj;
	}
}
