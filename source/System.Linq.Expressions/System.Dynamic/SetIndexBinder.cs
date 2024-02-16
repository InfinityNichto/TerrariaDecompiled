using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class SetIndexBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public CallInfo CallInfo { get; }

	internal sealed override bool IsStandardBinder => true;

	protected SetIndexBinder(CallInfo callInfo)
	{
		ContractUtils.RequiresNotNull(callInfo, "callInfo");
		CallInfo = callInfo;
	}

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.RequiresNotNull(args, "args");
		ContractUtils.Requires(args.Length >= 2, "args");
		DynamicMetaObject value = args[^1];
		DynamicMetaObject[] array = args.RemoveLast();
		ContractUtils.RequiresNotNull(value, "args");
		ContractUtils.RequiresNotNullItems(array, "args");
		return target.BindSetIndex(this, array, value);
	}

	public DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
	{
		return FallbackSetIndex(target, indexes, value, null);
	}

	public abstract DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject? errorSuggestion);
}
