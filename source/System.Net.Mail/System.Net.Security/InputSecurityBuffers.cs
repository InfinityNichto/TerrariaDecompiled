namespace System.Net.Security;

internal ref struct InputSecurityBuffers
{
	internal int Count;

	internal System.Net.Security.InputSecurityBuffer _item0;

	internal System.Net.Security.InputSecurityBuffer _item1;

	internal System.Net.Security.InputSecurityBuffer _item2;

	internal void SetNextBuffer(System.Net.Security.InputSecurityBuffer buffer)
	{
		if (Count == 0)
		{
			_item0 = buffer;
		}
		else if (Count == 1)
		{
			_item1 = buffer;
		}
		else
		{
			_item2 = buffer;
		}
		Count++;
	}
}
