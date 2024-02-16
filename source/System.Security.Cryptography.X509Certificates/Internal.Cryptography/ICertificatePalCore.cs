using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal interface ICertificatePalCore : IDisposable
{
	bool HasPrivateKey { get; }

	IntPtr Handle { get; }

	string Issuer { get; }

	string Subject { get; }

	string LegacyIssuer { get; }

	string LegacySubject { get; }

	byte[] Thumbprint { get; }

	string KeyAlgorithm { get; }

	byte[] KeyAlgorithmParameters { get; }

	byte[] PublicKeyValue { get; }

	byte[] SerialNumber { get; }

	string SignatureAlgorithm { get; }

	DateTime NotAfter { get; }

	DateTime NotBefore { get; }

	byte[] RawData { get; }

	byte[] Export(X509ContentType contentType, SafePasswordHandle password);
}
