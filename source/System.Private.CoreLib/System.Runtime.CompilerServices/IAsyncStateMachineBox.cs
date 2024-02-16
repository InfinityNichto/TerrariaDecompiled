namespace System.Runtime.CompilerServices;

internal interface IAsyncStateMachineBox
{
	Action MoveNextAction { get; }

	void MoveNext();

	IAsyncStateMachine GetStateMachineObject();

	void ClearStateUponCompletion();
}
