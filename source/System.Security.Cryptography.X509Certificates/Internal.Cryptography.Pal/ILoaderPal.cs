using System;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal;

internal interface ILoaderPal : IDisposable
{
	void MoveTo(X509Certificate2Collection collection);
}
