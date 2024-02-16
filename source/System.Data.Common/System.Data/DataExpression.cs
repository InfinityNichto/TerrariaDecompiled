using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class DataExpression : IFilter
{
	internal string _originalExpression;

	private readonly bool _parsed;

	private bool _bound;

	private ExpressionNode _expr;

	private DataTable _table;

	private readonly StorageType _storageType;

	private readonly Type _dataType;

	private DataColumn[] _dependency = Array.Empty<DataColumn>();

	internal string Expression
	{
		get
		{
			if (_originalExpression == null)
			{
				return "";
			}
			return _originalExpression;
		}
	}

	internal ExpressionNode ExpressionNode => _expr;

	internal bool HasValue => _expr != null;

	[RequiresUnreferencedCode("Members of types used in the expression might be trimmed")]
	internal DataExpression(DataTable table, string expression)
		: this(table, expression, null)
	{
	}

	[RequiresUnreferencedCode("Members of types used in the expression might be trimmed")]
	internal DataExpression(DataTable table, string expression, Type type)
	{
		ExpressionParser expressionParser = new ExpressionParser(table);
		expressionParser.LoadExpression(expression);
		_originalExpression = expression;
		_expr = null;
		if (expression != null)
		{
			_storageType = DataStorage.GetStorageType(type);
			if (_storageType == StorageType.BigInteger)
			{
				throw ExprException.UnsupportedDataType(type);
			}
			_dataType = type;
			_expr = expressionParser.Parse();
			_parsed = true;
			if (_expr != null && table != null)
			{
				Bind(table);
			}
			else
			{
				_bound = false;
			}
		}
	}

	internal void Bind(DataTable table)
	{
		_table = table;
		if (table != null && _expr != null)
		{
			List<DataColumn> list = new List<DataColumn>();
			_expr.Bind(table, list);
			_expr = _expr.Optimize();
			_table = table;
			_bound = true;
			_dependency = list.ToArray();
		}
	}

	internal bool DependsOn(DataColumn column)
	{
		if (_expr != null)
		{
			return _expr.DependsOn(column);
		}
		return false;
	}

	internal object Evaluate()
	{
		return Evaluate((DataRow)null, DataRowVersion.Default);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Constructors taking expression are marked as unsafe")]
	internal object Evaluate(DataRow row, DataRowVersion version)
	{
		if (!_bound)
		{
			Bind(_table);
		}
		object obj;
		if (_expr != null)
		{
			obj = _expr.Eval(row, version);
			if (obj != DBNull.Value || StorageType.Uri < _storageType)
			{
				try
				{
					if (StorageType.Object != _storageType)
					{
						obj = SqlConvert.ChangeType2(obj, _storageType, _dataType, _table.FormatProvider);
					}
				}
				catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
				{
					ExceptionBuilder.TraceExceptionForCapture(ex);
					throw ExprException.DatavalueConvertion(obj, _dataType, ex);
				}
			}
		}
		else
		{
			obj = null;
		}
		return obj;
	}

	internal object Evaluate(DataRow[] rows)
	{
		return Evaluate(rows, DataRowVersion.Default);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Constructors taking expression are marked as unsafe")]
	internal object Evaluate(DataRow[] rows, DataRowVersion version)
	{
		if (!_bound)
		{
			Bind(_table);
		}
		if (_expr != null)
		{
			List<int> list = new List<int>();
			foreach (DataRow dataRow in rows)
			{
				if (dataRow.RowState != DataRowState.Deleted && (version != DataRowVersion.Original || dataRow._oldRecord != -1))
				{
					list.Add(dataRow.GetRecordFromVersion(version));
				}
			}
			int[] recordNos = list.ToArray();
			return _expr.Eval(recordNos);
		}
		return DBNull.Value;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Constructors taking expression are marked as unsafe")]
	public bool Invoke(DataRow row, DataRowVersion version)
	{
		if (_expr == null)
		{
			return true;
		}
		if (row == null)
		{
			throw ExprException.InvokeArgument();
		}
		object value = _expr.Eval(row, version);
		try
		{
			return ToBoolean(value);
		}
		catch (EvaluateException)
		{
			throw ExprException.FilterConvertion(Expression);
		}
	}

	internal DataColumn[] GetDependency()
	{
		return _dependency;
	}

	internal bool IsTableAggregate()
	{
		if (_expr != null)
		{
			return _expr.IsTableConstant();
		}
		return false;
	}

	internal static bool IsUnknown(object value)
	{
		return DataStorage.IsObjectNull(value);
	}

	internal bool HasLocalAggregate()
	{
		if (_expr != null)
		{
			return _expr.HasLocalAggregate();
		}
		return false;
	}

	internal bool HasRemoteAggregate()
	{
		if (_expr != null)
		{
			return _expr.HasRemoteAggregate();
		}
		return false;
	}

	internal static bool ToBoolean(object value)
	{
		if (IsUnknown(value))
		{
			return false;
		}
		if (value is bool)
		{
			return (bool)value;
		}
		if (value is SqlBoolean sqlBoolean)
		{
			return sqlBoolean.IsTrue;
		}
		if (value is string)
		{
			try
			{
				return bool.Parse((string)value);
			}
			catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
			{
				ExceptionBuilder.TraceExceptionForCapture(ex);
				throw ExprException.DatavalueConvertion(value, typeof(bool), ex);
			}
		}
		throw ExprException.DatavalueConvertion(value, typeof(bool), null);
	}
}
