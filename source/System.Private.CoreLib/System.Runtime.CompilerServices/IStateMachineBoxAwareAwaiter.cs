namespace System.Runtime.CompilerServices;

internal interface IStateMachineBoxAwareAwaiter
{
	void AwaitUnsafeOnCompleted(IAsyncStateMachineBox box);
}
