using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Data;

internal sealed class FunctionNode : ExpressionNode
{
	internal readonly string _name;

	internal readonly int _info = -1;

	internal int _argumentCount;

	internal ExpressionNode[] _arguments;

	private readonly TypeLimiter _capturedLimiter;

	private static readonly Function[] s_funcs = new Function[16]
	{
		new Function("Abs", FunctionId.Abs, typeof(object), IsValidateArguments: true, IsVariantArgumentList: false, 1, typeof(object), null, null),
		new Function("IIf", FunctionId.Iif, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 3, typeof(object), typeof(object), typeof(object)),
		new Function("In", FunctionId.In, typeof(bool), IsValidateArguments: false, IsVariantArgumentList: true, 1, null, null, null),
		new Function("IsNull", FunctionId.IsNull, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 2, typeof(object), typeof(object), null),
		new Function("Len", FunctionId.Len, typeof(int), IsValidateArguments: true, IsVariantArgumentList: false, 1, typeof(string), null, null),
		new Function("Substring", FunctionId.Substring, typeof(string), IsValidateArguments: true, IsVariantArgumentList: false, 3, typeof(string), typeof(int), typeof(int)),
		new Function("Trim", FunctionId.Trim, typeof(string), IsValidateArguments: true, IsVariantArgumentList: false, 1, typeof(string), null, null),
		new Function("Convert", FunctionId.Convert, typeof(object), IsValidateArguments: false, IsVariantArgumentList: true, 1, typeof(object), null, null),
		new Function("DateTimeOffset", FunctionId.DateTimeOffset, typeof(DateTimeOffset), IsValidateArguments: false, IsVariantArgumentList: true, 3, typeof(DateTime), typeof(int), typeof(int)),
		new Function("Max", FunctionId.Max, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("Min", FunctionId.Min, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("Sum", FunctionId.Sum, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("Count", FunctionId.Count, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("Var", FunctionId.Var, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("StDev", FunctionId.StDev, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null),
		new Function("Avg", FunctionId.Avg, typeof(object), IsValidateArguments: false, IsVariantArgumentList: false, 1, null, null, null)
	};

	internal FunctionId Aggregate
	{
		get
		{
			if (IsAggregate)
			{
				return s_funcs[_info]._id;
			}
			return FunctionId.none;
		}
	}

	internal bool IsAggregate => s_funcs[_info]._id == FunctionId.Sum || s_funcs[_info]._id == FunctionId.Avg || s_funcs[_info]._id == FunctionId.Min || s_funcs[_info]._id == FunctionId.Max || s_funcs[_info]._id == FunctionId.Count || s_funcs[_info]._id == FunctionId.StDev || s_funcs[_info]._id == FunctionId.Var;

	internal FunctionNode(DataTable table, string name)
		: base(table)
	{
		_capturedLimiter = TypeLimiter.Capture();
		_name = name;
		for (int i = 0; i < s_funcs.Length; i++)
		{
			if (string.Equals(s_funcs[i]._name, name, StringComparison.OrdinalIgnoreCase))
			{
				_info = i;
				break;
			}
		}
		if (_info < 0)
		{
			throw ExprException.UndefinedFunction(_name);
		}
	}

	internal void AddArgument(ExpressionNode argument)
	{
		if (!s_funcs[_info]._isVariantArgumentList && _argumentCount >= s_funcs[_info]._argumentCount)
		{
			throw ExprException.FunctionArgumentCount(_name);
		}
		if (_arguments == null)
		{
			_arguments = new ExpressionNode[1];
		}
		else if (_argumentCount == _arguments.Length)
		{
			ExpressionNode[] array = new ExpressionNode[_argumentCount * 2];
			Array.Copy(_arguments, array, _argumentCount);
			_arguments = array;
		}
		_arguments[_argumentCount++] = argument;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
		Check();
		if (s_funcs[_info]._id == FunctionId.Convert)
		{
			if (_argumentCount != 2)
			{
				throw ExprException.FunctionArgumentCount(_name);
			}
			_arguments[0].Bind(table, list);
			if (_arguments[1].GetType() == typeof(NameNode))
			{
				NameNode nameNode = (NameNode)_arguments[1];
				_arguments[1] = new ConstNode(table, ValueType.Str, nameNode._name);
			}
			_arguments[1].Bind(table, list);
		}
		else
		{
			for (int i = 0; i < _argumentCount; i++)
			{
				_arguments[i].Bind(table, list);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		return Eval(null, DataRowVersion.Default);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		object[] array = new object[_argumentCount];
		if (s_funcs[_info]._id == FunctionId.Convert)
		{
			if (_argumentCount != 2)
			{
				throw ExprException.FunctionArgumentCount(_name);
			}
			array[0] = _arguments[0].Eval(row, version);
			array[1] = GetDataType(_arguments[1]);
		}
		else if (s_funcs[_info]._id != FunctionId.Iif)
		{
			for (int i = 0; i < _argumentCount; i++)
			{
				array[i] = _arguments[i].Eval(row, version);
				if (!s_funcs[_info]._isValidateArguments)
				{
					continue;
				}
				if (array[i] == DBNull.Value || typeof(object) == s_funcs[_info]._parameters[i])
				{
					return DBNull.Value;
				}
				if (!(array[i].GetType() != s_funcs[_info]._parameters[i]))
				{
					continue;
				}
				if (s_funcs[_info]._parameters[i] == typeof(int) && ExpressionNode.IsInteger(DataStorage.GetStorageType(array[i].GetType())))
				{
					array[i] = Convert.ToInt32(array[i], base.FormatProvider);
					continue;
				}
				if (s_funcs[_info]._id == FunctionId.Trim || s_funcs[_info]._id == FunctionId.Substring || s_funcs[_info]._id == FunctionId.Len)
				{
					if (typeof(string) != array[i].GetType() && typeof(SqlString) != array[i].GetType())
					{
						throw ExprException.ArgumentType(s_funcs[_info]._name, i + 1, s_funcs[_info]._parameters[i]);
					}
					continue;
				}
				throw ExprException.ArgumentType(s_funcs[_info]._name, i + 1, s_funcs[_info]._parameters[i]);
			}
		}
		return EvalFunction(s_funcs[_info]._id, array, row, version);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] recordNos)
	{
		throw ExprException.ComputeNotAggregate(ToString());
	}

	internal override bool IsConstant()
	{
		bool flag = true;
		for (int i = 0; i < _argumentCount; i++)
		{
			flag = flag && _arguments[i].IsConstant();
		}
		return flag;
	}

	internal override bool IsTableConstant()
	{
		for (int i = 0; i < _argumentCount; i++)
		{
			if (!_arguments[i].IsTableConstant())
			{
				return false;
			}
		}
		return true;
	}

	internal override bool HasLocalAggregate()
	{
		for (int i = 0; i < _argumentCount; i++)
		{
			if (_arguments[i].HasLocalAggregate())
			{
				return true;
			}
		}
		return false;
	}

	internal override bool HasRemoteAggregate()
	{
		for (int i = 0; i < _argumentCount; i++)
		{
			if (_arguments[i].HasRemoteAggregate())
			{
				return true;
			}
		}
		return false;
	}

	internal override bool DependsOn(DataColumn column)
	{
		for (int i = 0; i < _argumentCount; i++)
		{
			if (_arguments[i].DependsOn(column))
			{
				return true;
			}
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Constant expressions are safe to be evaluated.")]
	internal override ExpressionNode Optimize()
	{
		for (int i = 0; i < _argumentCount; i++)
		{
			_arguments[i] = _arguments[i].Optimize();
		}
		if (s_funcs[_info]._id == FunctionId.In)
		{
			if (!IsConstant())
			{
				throw ExprException.NonConstantArgument();
			}
		}
		else if (IsConstant())
		{
			return new ConstNode(base.table, ValueType.Object, Eval(), fParseQuotes: false);
		}
		return this;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private Type GetDataType(ExpressionNode node)
	{
		Type type = node.GetType();
		string text = null;
		if (type == typeof(NameNode))
		{
			text = ((NameNode)node)._name;
		}
		if (type == typeof(ConstNode))
		{
			text = ((ConstNode)node)._val.ToString();
		}
		if (text == null)
		{
			throw ExprException.ArgumentType(s_funcs[_info]._name, 2, typeof(Type));
		}
		Type type2 = Type.GetType(text);
		if (type2 == null)
		{
			throw ExprException.InvalidType(text);
		}
		TypeLimiter.EnsureTypeIsAllowed(type2, _capturedLimiter);
		return type2;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private object EvalFunction(FunctionId id, object[] argumentValues, DataRow row, DataRowVersion version)
	{
		switch (id)
		{
		case FunctionId.Abs:
		{
			StorageType storageType2 = DataStorage.GetStorageType(argumentValues[0].GetType());
			if (ExpressionNode.IsInteger(storageType2))
			{
				return Math.Abs((long)argumentValues[0]);
			}
			if (ExpressionNode.IsNumeric(storageType2))
			{
				return Math.Abs((double)argumentValues[0]);
			}
			throw ExprException.ArgumentTypeInteger(s_funcs[_info]._name, 1);
		}
		case FunctionId.cBool:
			return DataStorage.GetStorageType(argumentValues[0].GetType()) switch
			{
				StorageType.Boolean => (bool)argumentValues[0], 
				StorageType.Int32 => (int)argumentValues[0] != 0, 
				StorageType.Double => (double)argumentValues[0] != 0.0, 
				StorageType.String => bool.Parse((string)argumentValues[0]), 
				_ => throw ExprException.DatatypeConvertion(argumentValues[0].GetType(), typeof(bool)), 
			};
		case FunctionId.cInt:
			return Convert.ToInt32(argumentValues[0], base.FormatProvider);
		case FunctionId.cDate:
			return Convert.ToDateTime(argumentValues[0], base.FormatProvider);
		case FunctionId.cDbl:
			return Convert.ToDouble(argumentValues[0], base.FormatProvider);
		case FunctionId.cStr:
			return Convert.ToString(argumentValues[0], base.FormatProvider);
		case FunctionId.Charindex:
			if (DataStorage.IsObjectNull(argumentValues[0]) || DataStorage.IsObjectNull(argumentValues[1]))
			{
				return DBNull.Value;
			}
			if (argumentValues[0] is SqlString)
			{
				argumentValues[0] = ((SqlString)argumentValues[0]).Value;
			}
			if (argumentValues[1] is SqlString)
			{
				argumentValues[1] = ((SqlString)argumentValues[1]).Value;
			}
			return ((string)argumentValues[1]).IndexOf((string)argumentValues[0], StringComparison.Ordinal);
		case FunctionId.Iif:
		{
			object value = _arguments[0].Eval(row, version);
			if (DataExpression.ToBoolean(value))
			{
				return _arguments[1].Eval(row, version);
			}
			return _arguments[2].Eval(row, version);
		}
		case FunctionId.In:
			throw ExprException.NYI(s_funcs[_info]._name);
		case FunctionId.IsNull:
			if (DataStorage.IsObjectNull(argumentValues[0]))
			{
				return argumentValues[1];
			}
			return argumentValues[0];
		case FunctionId.Len:
			if (argumentValues[0] is SqlString)
			{
				if (((SqlString)argumentValues[0]).IsNull)
				{
					return DBNull.Value;
				}
				argumentValues[0] = ((SqlString)argumentValues[0]).Value;
			}
			return ((string)argumentValues[0]).Length;
		case FunctionId.Substring:
		{
			int num = (int)argumentValues[1] - 1;
			int num2 = (int)argumentValues[2];
			if (num < 0)
			{
				throw ExprException.FunctionArgumentOutOfRange("index", "Substring");
			}
			if (num2 < 0)
			{
				throw ExprException.FunctionArgumentOutOfRange("length", "Substring");
			}
			if (num2 == 0)
			{
				return string.Empty;
			}
			if (argumentValues[0] is SqlString)
			{
				argumentValues[0] = ((SqlString)argumentValues[0]).Value;
			}
			int length = ((string)argumentValues[0]).Length;
			if (num > length)
			{
				return DBNull.Value;
			}
			if (num + num2 > length)
			{
				num2 = length - num;
			}
			return ((string)argumentValues[0]).Substring(num, num2);
		}
		case FunctionId.Trim:
			if (DataStorage.IsObjectNull(argumentValues[0]))
			{
				return DBNull.Value;
			}
			if (argumentValues[0] is SqlString)
			{
				argumentValues[0] = ((SqlString)argumentValues[0]).Value;
			}
			return ((string)argumentValues[0]).Trim();
		case FunctionId.Convert:
		{
			if (_argumentCount != 2)
			{
				throw ExprException.FunctionArgumentCount(_name);
			}
			if (argumentValues[0] == DBNull.Value)
			{
				return DBNull.Value;
			}
			Type type = (Type)argumentValues[1];
			StorageType storageType = DataStorage.GetStorageType(type);
			StorageType storageType2 = DataStorage.GetStorageType(argumentValues[0].GetType());
			if (storageType == StorageType.DateTimeOffset && storageType2 == StorageType.String)
			{
				return SqlConvert.ConvertStringToDateTimeOffset((string)argumentValues[0], base.FormatProvider);
			}
			if (StorageType.Object != storageType)
			{
				if (storageType == StorageType.Guid && storageType2 == StorageType.String)
				{
					return new Guid((string)argumentValues[0]);
				}
				if (ExpressionNode.IsFloatSql(storageType2) && ExpressionNode.IsIntegerSql(storageType))
				{
					if (StorageType.Single == storageType2)
					{
						return SqlConvert.ChangeType2((float)SqlConvert.ChangeType2(argumentValues[0], StorageType.Single, typeof(float), base.FormatProvider), storageType, type, base.FormatProvider);
					}
					if (StorageType.Double == storageType2)
					{
						return SqlConvert.ChangeType2((double)SqlConvert.ChangeType2(argumentValues[0], StorageType.Double, typeof(double), base.FormatProvider), storageType, type, base.FormatProvider);
					}
					if (StorageType.Decimal == storageType2)
					{
						return SqlConvert.ChangeType2((decimal)SqlConvert.ChangeType2(argumentValues[0], StorageType.Decimal, typeof(decimal), base.FormatProvider), storageType, type, base.FormatProvider);
					}
				}
				DeserializationToken deserializationToken = ((_capturedLimiter != null) ? SerializationInfo.StartDeserialization() : default(DeserializationToken));
				using (deserializationToken)
				{
					return SqlConvert.ChangeType2(argumentValues[0], storageType, type, base.FormatProvider);
				}
			}
			return argumentValues[0];
		}
		case FunctionId.DateTimeOffset:
			if (argumentValues[0] == DBNull.Value || argumentValues[1] == DBNull.Value || argumentValues[2] == DBNull.Value)
			{
				return DBNull.Value;
			}
			switch (((DateTime)argumentValues[0]).Kind)
			{
			case DateTimeKind.Utc:
				if ((int)argumentValues[1] != 0 && (int)argumentValues[2] != 0)
				{
					throw ExprException.MismatchKindandTimeSpan();
				}
				break;
			case DateTimeKind.Local:
				if (DateTimeOffset.Now.Offset.Hours != (int)argumentValues[1] && DateTimeOffset.Now.Offset.Minutes != (int)argumentValues[2])
				{
					throw ExprException.MismatchKindandTimeSpan();
				}
				break;
			}
			if ((int)argumentValues[1] < -14 || (int)argumentValues[1] > 14)
			{
				throw ExprException.InvalidHoursArgument();
			}
			if ((int)argumentValues[2] < -59 || (int)argumentValues[2] > 59)
			{
				throw ExprException.InvalidMinutesArgument();
			}
			if ((int)argumentValues[1] == 14 && (int)argumentValues[2] > 0)
			{
				throw ExprException.InvalidTimeZoneRange();
			}
			if ((int)argumentValues[1] == -14 && (int)argumentValues[2] < 0)
			{
				throw ExprException.InvalidTimeZoneRange();
			}
			return new DateTimeOffset((DateTime)argumentValues[0], new TimeSpan((int)argumentValues[1], (int)argumentValues[2], 0));
		default:
			throw ExprException.UndefinedFunction(s_funcs[_info]._name);
		}
	}

	internal void Check()
	{
		if (_info < 0)
		{
			throw ExprException.UndefinedFunction(_name);
		}
		if (s_funcs[_info]._isVariantArgumentList)
		{
			if (_argumentCount < s_funcs[_info]._argumentCount)
			{
				if (s_funcs[_info]._id == FunctionId.In)
				{
					throw ExprException.InWithoutList();
				}
				throw ExprException.FunctionArgumentCount(_name);
			}
		}
		else if (_argumentCount != s_funcs[_info]._argumentCount)
		{
			throw ExprException.FunctionArgumentCount(_name);
		}
	}
}
