using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

internal sealed class ConstNode : ExpressionNode
{
	internal readonly object _val;

	internal ConstNode(DataTable table, ValueType type, object constant)
		: this(table, type, constant, fParseQuotes: true)
	{
	}

	internal ConstNode(DataTable table, ValueType type, object constant, bool fParseQuotes)
		: base(table)
	{
		switch (type)
		{
		case ValueType.Null:
			_val = DBNull.Value;
			break;
		case ValueType.Numeric:
			_val = SmallestNumeric(constant);
			break;
		case ValueType.Decimal:
			_val = SmallestDecimal(constant);
			break;
		case ValueType.Float:
			_val = Convert.ToDouble(constant, NumberFormatInfo.InvariantInfo);
			break;
		case ValueType.Bool:
			_val = Convert.ToBoolean(constant, CultureInfo.InvariantCulture);
			break;
		case ValueType.Str:
			if (fParseQuotes)
			{
				_val = ((string)constant).Replace("''", "'");
			}
			else
			{
				_val = (string)constant;
			}
			break;
		case ValueType.Date:
			_val = DateTime.Parse((string)constant, CultureInfo.InvariantCulture);
			break;
		default:
			_val = constant;
			break;
		}
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		return _val;
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

	private object SmallestDecimal(object constant)
	{
		if (constant == null)
		{
			return 0.0;
		}
		if (constant is string s)
		{
			if (decimal.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var result))
			{
				return result;
			}
			if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out var result2))
			{
				return result2;
			}
		}
		else if (constant is IConvertible convertible)
		{
			try
			{
				return convertible.ToDecimal(NumberFormatInfo.InvariantInfo);
			}
			catch (ArgumentException e)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			}
			catch (FormatException e2)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e2);
			}
			catch (InvalidCastException e3)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e3);
			}
			catch (OverflowException e4)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e4);
			}
			try
			{
				return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
			}
			catch (ArgumentException e5)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e5);
			}
			catch (FormatException e6)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e6);
			}
			catch (InvalidCastException e7)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e7);
			}
			catch (OverflowException e8)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e8);
			}
		}
		return constant;
	}

	private object SmallestNumeric(object constant)
	{
		if (constant == null)
		{
			return 0;
		}
		if (constant is string s)
		{
			if (int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var result))
			{
				return result;
			}
			if (long.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var result2))
			{
				return result2;
			}
			if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out var result3))
			{
				return result3;
			}
		}
		else if (constant is IConvertible convertible)
		{
			try
			{
				return convertible.ToInt32(NumberFormatInfo.InvariantInfo);
			}
			catch (ArgumentException e)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			}
			catch (FormatException e2)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e2);
			}
			catch (InvalidCastException e3)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e3);
			}
			catch (OverflowException e4)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e4);
			}
			try
			{
				return convertible.ToInt64(NumberFormatInfo.InvariantInfo);
			}
			catch (ArgumentException e5)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e5);
			}
			catch (FormatException e6)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e6);
			}
			catch (InvalidCastException e7)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e7);
			}
			catch (OverflowException e8)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e8);
			}
			try
			{
				return convertible.ToDouble(NumberFormatInfo.InvariantInfo);
			}
			catch (ArgumentException e9)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e9);
			}
			catch (FormatException e10)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e10);
			}
			catch (InvalidCastException e11)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e11);
			}
			catch (OverflowException e12)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e12);
			}
		}
		return constant;
	}
}
