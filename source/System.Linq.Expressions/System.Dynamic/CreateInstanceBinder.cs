using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class CreateInstanceBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public CallInfo CallInfo { get; }

	internal sealed override bool IsStandardBinder => true;

	protected CreateInstanceBinder(CallInfo callInfo)
	{
		ContractUtils.RequiresNotNull(callInfo, "callInfo");
		CallInfo = callInfo;
	}

	public DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		return FallbackCreateInstance(target, args, null);
	}

	public abstract DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject? errorSuggestion);

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.RequiresNotNullItems(args, "args");
		return target.BindCreateInstance(this, args);
	}
}
