namespace System.Reflection;

public class LocalVariableInfo
{
	public virtual Type LocalType => null;

	public virtual int LocalIndex => 0;

	public virtual bool IsPinned => false;

	protected LocalVariableInfo()
	{
	}

	public override string ToString()
	{
		if (IsPinned)
		{
			return $"{LocalType} ({LocalIndex}) (pinned)";
		}
		return $"{LocalType} ({LocalIndex})";
	}
}
