using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class NameNode : ExpressionNode
{
	internal string _name;

	internal bool _found;

	internal DataColumn _column;

	internal override bool IsSqlColumn => _column.IsSqlType;

	internal NameNode(DataTable table, char[] text, int start, int pos)
		: base(table)
	{
		_name = ParseName(text, start, pos);
	}

	internal NameNode(DataTable table, string name)
		: base(table)
	{
		_name = name;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
		if (table == null)
		{
			throw ExprException.UnboundName(_name);
		}
		try
		{
			_column = table.Columns[_name];
		}
		catch (Exception e)
		{
			_found = false;
			if (!ADP.IsCatchableExceptionType(e))
			{
				throw;
			}
			throw ExprException.UnboundName(_name);
		}
		if (_column == null)
		{
			throw ExprException.UnboundName(_name);
		}
		_name = _column.ColumnName;
		_found = true;
		int i;
		for (i = 0; i < list.Count; i++)
		{
			DataColumn dataColumn = list[i];
			if (_column == dataColumn)
			{
				break;
			}
		}
		if (i >= list.Count)
		{
			list.Add(_column);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		throw ExprException.EvalNoContext();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		if (!_found)
		{
			throw ExprException.UnboundName(_name);
		}
		if (row == null)
		{
			if (IsTableConstant())
			{
				return _column.DataExpression.Evaluate();
			}
			throw ExprException.UnboundName(_name);
		}
		return _column[row.GetRecordFromVersion(version)];
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] records)
	{
		throw ExprException.ComputeNotAggregate(ToString());
	}

	internal override bool IsConstant()
	{
		return false;
	}

	internal override bool IsTableConstant()
	{
		if (_column != null && _column.Computed)
		{
			return _column.DataExpression.IsTableAggregate();
		}
		return false;
	}

	internal override bool HasLocalAggregate()
	{
		if (_column != null && _column.Computed)
		{
			return _column.DataExpression.HasLocalAggregate();
		}
		return false;
	}

	internal override bool HasRemoteAggregate()
	{
		if (_column != null && _column.Computed)
		{
			return _column.DataExpression.HasRemoteAggregate();
		}
		return false;
	}

	internal override bool DependsOn(DataColumn column)
	{
		if (_column == column)
		{
			return true;
		}
		if (_column.Computed)
		{
			return _column.DataExpression.DependsOn(column);
		}
		return false;
	}

	internal override ExpressionNode Optimize()
	{
		return this;
	}

	internal static string ParseName(char[] text, int start, int pos)
	{
		char c = '\0';
		string text2 = string.Empty;
		int num = start;
		int num2 = pos;
		checked
		{
			if (text[start] == '`')
			{
				start++;
				pos--;
				c = '\\';
				text2 = "`";
			}
			else if (text[start] == '[')
			{
				start++;
				pos--;
				c = '\\';
				text2 = "]\\";
			}
		}
		if (c != 0)
		{
			int num3 = start;
			for (int i = start; i < pos; i++)
			{
				if (text[i] == c && i + 1 < pos && text2.Contains(text[i + 1]))
				{
					i++;
				}
				text[num3] = text[i];
				num3++;
			}
			pos = num3;
		}
		if (pos == start)
		{
			throw ExprException.InvalidName(new string(text, num, num2 - num));
		}
		return new string(text, start, pos - start);
	}
}
