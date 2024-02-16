using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal interface IChainPal : IDisposable
{
	X509ChainElement[] ChainElements { get; }

	X509ChainStatus[] ChainStatus { get; }

	SafeX509ChainHandle SafeHandle { get; }

	bool? Verify(X509VerificationFlags flags, out Exception exception);
}
