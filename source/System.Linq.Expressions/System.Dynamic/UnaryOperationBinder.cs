using System.Dynamic.Utils;
using System.Linq.Expressions;

namespace System.Dynamic;

public abstract class UnaryOperationBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType
	{
		get
		{
			ExpressionType operation = Operation;
			if ((uint)(operation - 83) <= 1u)
			{
				return typeof(bool);
			}
			return typeof(object);
		}
	}

	public ExpressionType Operation { get; }

	internal sealed override bool IsStandardBinder => true;

	protected UnaryOperationBinder(ExpressionType operation)
	{
		ContractUtils.Requires(OperationIsValid(operation), "operation");
		Operation = operation;
	}

	public DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target)
	{
		return FallbackUnaryOperation(target, null);
	}

	public abstract DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject? errorSuggestion);

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[]? args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.Requires(args == null || args.Length == 0, "args");
		return target.BindUnaryOperation(this);
	}

	internal static bool OperationIsValid(ExpressionType operation)
	{
		switch (operation)
		{
		case ExpressionType.Negate:
		case ExpressionType.UnaryPlus:
		case ExpressionType.Not:
		case ExpressionType.Decrement:
		case ExpressionType.Extension:
		case ExpressionType.Increment:
		case ExpressionType.OnesComplement:
		case ExpressionType.IsTrue:
		case ExpressionType.IsFalse:
			return true;
		default:
			return false;
		}
	}
}
