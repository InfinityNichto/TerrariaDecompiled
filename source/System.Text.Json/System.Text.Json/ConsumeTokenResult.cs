namespace System.Text.Json;

internal enum ConsumeTokenResult : byte
{
	Success,
	NotEnoughDataRollBackState,
	IncompleteNoRollBackNecessary
}
