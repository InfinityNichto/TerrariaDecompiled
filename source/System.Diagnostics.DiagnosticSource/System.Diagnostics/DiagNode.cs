namespace System.Diagnostics;

internal sealed class DiagNode<T>
{
	public T Value;

	public DiagNode<T> Next;

	public DiagNode(T value)
	{
		Value = value;
	}
}
