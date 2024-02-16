namespace System.Threading;

internal sealed class ReaderWriterCount
{
	public long lockID;

	public int readercount;

	public int writercount;

	public int upgradecount;

	public ReaderWriterCount next;
}
