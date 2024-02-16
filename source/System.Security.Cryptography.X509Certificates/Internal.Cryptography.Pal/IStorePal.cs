using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal;

internal interface IStorePal : IDisposable
{
	SafeHandle SafeHandle { get; }

	void CloneTo(X509Certificate2Collection collection);

	void Add(ICertificatePal cert);

	void Remove(ICertificatePal cert);
}
