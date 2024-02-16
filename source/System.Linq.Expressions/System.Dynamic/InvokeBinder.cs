using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class InvokeBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(object);

	public CallInfo CallInfo { get; }

	internal sealed override bool IsStandardBinder => true;

	protected InvokeBinder(CallInfo callInfo)
	{
		ContractUtils.RequiresNotNull(callInfo, "callInfo");
		CallInfo = callInfo;
	}

	public DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		return FallbackInvoke(target, args, null);
	}

	public abstract DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject? errorSuggestion);

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.RequiresNotNullItems(args, "args");
		return target.BindInvoke(this, args);
	}
}
