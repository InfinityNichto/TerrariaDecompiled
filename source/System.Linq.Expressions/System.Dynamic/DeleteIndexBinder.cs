using System.Dynamic.Utils;

namespace System.Dynamic;

public abstract class DeleteIndexBinder : DynamicMetaObjectBinder
{
	public sealed override Type ReturnType => typeof(void);

	public CallInfo CallInfo { get; }

	internal sealed override bool IsStandardBinder => true;

	protected DeleteIndexBinder(CallInfo callInfo)
	{
		ContractUtils.RequiresNotNull(callInfo, "callInfo");
		CallInfo = callInfo;
	}

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		ContractUtils.RequiresNotNull(target, "target");
		ContractUtils.RequiresNotNullItems(args, "args");
		return target.BindDeleteIndex(this, args);
	}

	public DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes)
	{
		return FallbackDeleteIndex(target, indexes, null);
	}

	public abstract DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject? errorSuggestion);
}
