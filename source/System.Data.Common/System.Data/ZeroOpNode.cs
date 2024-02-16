using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class ZeroOpNode : ExpressionNode
{
	internal readonly int _op;

	internal ZeroOpNode(int op)
		: base(null)
	{
		_op = op;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		return _op switch
		{
			33 => true, 
			34 => false, 
			32 => DBNull.Value, 
			_ => DBNull.Value, 
		};
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		return Eval();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] recordNos)
	{
		return Eval();
	}

	internal override bool IsConstant()
	{
		return true;
	}

	internal override bool IsTableConstant()
	{
		return true;
	}

	internal override bool HasLocalAggregate()
	{
		return false;
	}

	internal override bool HasRemoteAggregate()
	{
		return false;
	}

	internal override ExpressionNode Optimize()
	{
		return this;
	}
}
