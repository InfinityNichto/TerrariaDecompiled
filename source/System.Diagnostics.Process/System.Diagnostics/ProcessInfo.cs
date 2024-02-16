using System.Collections.Generic;

namespace System.Diagnostics;

internal sealed class ProcessInfo
{
	internal readonly List<ThreadInfo> _threadInfoList;

	internal int BasePriority { get; set; }

	internal string ProcessName { get; set; } = string.Empty;


	internal int ProcessId { get; set; }

	internal long PoolPagedBytes { get; set; }

	internal long PoolNonPagedBytes { get; set; }

	internal long VirtualBytes { get; set; }

	internal long VirtualBytesPeak { get; set; }

	internal long WorkingSetPeak { get; set; }

	internal long WorkingSet { get; set; }

	internal long PageFileBytesPeak { get; set; }

	internal long PageFileBytes { get; set; }

	internal long PrivateBytes { get; set; }

	internal int SessionId { get; set; }

	internal int HandleCount { get; set; }

	internal ProcessInfo()
	{
		_threadInfoList = new List<ThreadInfo>();
	}

	internal ProcessInfo(int threadsNumber)
	{
		_threadInfoList = new List<ThreadInfo>(threadsNumber);
	}
}
