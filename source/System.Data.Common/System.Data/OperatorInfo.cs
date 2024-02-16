namespace System.Data;

internal sealed class OperatorInfo
{
	internal Nodes _type;

	internal int _op;

	internal int _priority;

	internal OperatorInfo(Nodes type, int op, int pri)
	{
		_type = type;
		_op = op;
		_priority = pri;
	}
}
