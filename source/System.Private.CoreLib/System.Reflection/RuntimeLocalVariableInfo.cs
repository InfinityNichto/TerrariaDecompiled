namespace System.Reflection;

internal sealed class RuntimeLocalVariableInfo : LocalVariableInfo
{
	private RuntimeType _type;

	private int _localIndex;

	private bool _isPinned;

	public override Type LocalType => _type;

	public override int LocalIndex => _localIndex;

	public override bool IsPinned => _isPinned;

	private RuntimeLocalVariableInfo()
	{
	}
}
