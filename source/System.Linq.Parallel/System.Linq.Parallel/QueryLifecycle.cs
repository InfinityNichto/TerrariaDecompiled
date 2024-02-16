namespace System.Linq.Parallel;

internal static class QueryLifecycle
{
	internal static void LogicalQueryExecutionBegin(int queryID)
	{
		PlinqEtwProvider.Log.ParallelQueryBegin(queryID);
	}

	internal static void LogicalQueryExecutionEnd(int queryID)
	{
		PlinqEtwProvider.Log.ParallelQueryEnd(queryID);
	}
}
