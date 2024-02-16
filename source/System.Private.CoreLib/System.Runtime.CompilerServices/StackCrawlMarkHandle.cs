using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal ref struct StackCrawlMarkHandle
{
	private unsafe void* _ptr;

	internal unsafe StackCrawlMarkHandle(ref StackCrawlMark stackMark)
	{
		_ptr = Unsafe.AsPointer(ref stackMark);
	}
}
