using System.Dynamic.Utils;
using System.Linq.Expressions;

namespace System.Dynamic;

public abstract class BinaryOperationBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public ExpressionType Operation { get; }

	internal sealed override bool IsStandardBinder => true;

	protected BinaryOperationBinder(ExpressionType operation)
	{
		ContractUtils.Requires(OperationIsValid(operation), "operation");
		Operation = operation;
	}

	public DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg)
	{
		return FallbackBinaryOperation(target, arg, null);
	}

	public abstract DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject? errorSuggestion);

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.RequiresNotNull(args, "args");
		ContractUtils.Requires(args.Length == 1, "args");
		DynamicMetaObject dynamicMetaObject = args[0];
		ContractUtils.RequiresNotNull(dynamicMetaObject, "args");
		return target.BindBinaryOperation(this, dynamicMetaObject);
	}

	internal static bool OperationIsValid(ExpressionType operation)
	{
		switch (operation)
		{
		case ExpressionType.Add:
		case ExpressionType.And:
		case ExpressionType.Divide:
		case ExpressionType.Equal:
		case ExpressionType.ExclusiveOr:
		case ExpressionType.GreaterThan:
		case ExpressionType.GreaterThanOrEqual:
		case ExpressionType.LeftShift:
		case ExpressionType.LessThan:
		case ExpressionType.LessThanOrEqual:
		case ExpressionType.Modulo:
		case ExpressionType.Multiply:
		case ExpressionType.NotEqual:
		case ExpressionType.Or:
		case ExpressionType.Power:
		case ExpressionType.RightShift:
		case ExpressionType.Subtract:
		case ExpressionType.Extension:
		case ExpressionType.AddAssign:
		case ExpressionType.AndAssign:
		case ExpressionType.DivideAssign:
		case ExpressionType.ExclusiveOrAssign:
		case ExpressionType.LeftShiftAssign:
		case ExpressionType.ModuloAssign:
		case ExpressionType.MultiplyAssign:
		case ExpressionType.OrAssign:
		case ExpressionType.PowerAssign:
		case ExpressionType.RightShiftAssign:
		case ExpressionType.SubtractAssign:
			return true;
		default:
			return false;
		}
	}
}
