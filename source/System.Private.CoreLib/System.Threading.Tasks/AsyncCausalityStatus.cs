namespace System.Threading.Tasks;

internal enum AsyncCausalityStatus
{
	Started,
	Completed,
	Canceled,
	Error
}
