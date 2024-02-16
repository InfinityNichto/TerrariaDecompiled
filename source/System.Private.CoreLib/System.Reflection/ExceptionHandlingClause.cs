namespace System.Reflection;

public class ExceptionHandlingClause
{
	public virtual ExceptionHandlingClauseOptions Flags => ExceptionHandlingClauseOptions.Clause;

	public virtual int TryOffset => 0;

	public virtual int TryLength => 0;

	public virtual int HandlerOffset => 0;

	public virtual int HandlerLength => 0;

	public virtual int FilterOffset
	{
		get
		{
			throw new InvalidOperationException(SR.Arg_EHClauseNotFilter);
		}
	}

	public virtual Type? CatchType => null;

	protected ExceptionHandlingClause()
	{
	}

	public override string ToString()
	{
		return $"Flags={Flags}, TryOffset={TryOffset}, TryLength={TryLength}, HandlerOffset={HandlerOffset}, HandlerLength={HandlerLength}, CatchType={CatchType}";
	}
}
