namespace System;

public struct UriCreationOptions
{
	private bool _disablePathAndQueryCanonicalization;

	public bool DangerousDisablePathAndQueryCanonicalization
	{
		readonly get
		{
			return _disablePathAndQueryCanonicalization;
		}
		set
		{
			_disablePathAndQueryCanonicalization = value;
		}
	}
}
