using System.Runtime.ConstrainedExecution;

namespace System.Net.Security;

internal sealed class SafeCredentialReference : CriticalFinalizerObject, IDisposable
{
	internal System.Net.Security.SafeFreeCredentials Target { get; private set; }

	internal static System.Net.Security.SafeCredentialReference CreateReference(System.Net.Security.SafeFreeCredentials target)
	{
		if (target.IsInvalid || target.IsClosed)
		{
			return null;
		}
		return new System.Net.Security.SafeCredentialReference(target);
	}

	private SafeCredentialReference(System.Net.Security.SafeFreeCredentials target)
	{
		bool success = false;
		target.DangerousAddRef(ref success);
		Target = target;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		Target?.DangerousRelease();
		Target = null;
	}

	~SafeCredentialReference()
	{
		Dispose(disposing: false);
	}
}
