using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net;

internal interface ISSPIInterface
{
	System.Net.SecurityPackageInfoClass[] SecurityPackages { get; set; }

	int EnumerateSecurityPackages(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer pkgArray);

	int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, ref System.Net.Security.SafeSspiAuthDataHandle authdata, out System.Net.Security.SafeFreeCredentials outCredential);

	int AcquireDefaultCredential(string moduleName, global::Interop.SspiCli.CredentialUse usage, out System.Net.Security.SafeFreeCredentials outCredential);

	int AcceptSecurityContext(System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, System.Net.Security.InputSecurityBuffers inputBuffers, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags);

	int InitializeSecurityContext(ref System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, System.Net.Security.InputSecurityBuffers inputBuffers, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags);

	int QueryContextAttributes(System.Net.Security.SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type handleType, out SafeHandle refHandle);

	int QuerySecurityContextToken(System.Net.Security.SafeDeleteContext phContext, out System.Net.Security.SecurityContextTokenHandle phToken);

	int CompleteAuthToken(ref System.Net.Security.SafeDeleteSslContext refContext, in System.Net.Security.SecurityBuffer inputBuffer);
}
