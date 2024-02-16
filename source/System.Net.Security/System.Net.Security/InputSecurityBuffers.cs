namespace System.Net.Security;

internal ref struct InputSecurityBuffers
{
	internal int Count;

	internal InputSecurityBuffer _item0;

	internal InputSecurityBuffer _item1;

	internal InputSecurityBuffer _item2;

	internal void SetNextBuffer(InputSecurityBuffer buffer)
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
