using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal interface IExportPal : IDisposable
{
	byte[] Export(X509ContentType contentType, SafePasswordHandle password);
}
