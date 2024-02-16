namespace System.Threading;

internal enum StackCrawlMark
{
	LookForMe,
	LookForMyCaller,
	LookForMyCallersCaller,
	LookForThread
}
