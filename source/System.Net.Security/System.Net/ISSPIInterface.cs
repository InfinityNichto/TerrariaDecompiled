using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net;

internal interface ISSPIInterface
{
	SecurityPackageInfoClass[] SecurityPackages { get; set; }

	int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray);

	int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential);

	unsafe int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, global::Interop.SspiCli.SCHANNEL_CRED* authdata, out SafeFreeCredentials outCredential);

	unsafe int AcquireCredentialsHandle(string moduleName, global::Interop.SspiCli.CredentialUse usage, global::Interop.SspiCli.SCH_CREDENTIALS* authdata, out SafeFreeCredentials outCredential);

	int AcquireDefaultCredential(string moduleName, global::Interop.SspiCli.CredentialUse usage, out SafeFreeCredentials outCredential);

	int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteSslContext context, InputSecurityBuffers inputBuffers, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, ref SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags);

	int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteSslContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, InputSecurityBuffers inputBuffers, ref SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags);

	int EncryptMessage(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber);

	int DecryptMessage(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber);

	int MakeSignature(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber);

	int VerifySignature(SafeDeleteContext context, ref global::Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber);

	int QueryContextChannelBinding(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding refHandle);

	int QueryContextAttributes(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type handleType, out SafeHandle refHandle);

	int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken);

	int CompleteAuthToken(ref SafeDeleteSslContext refContext, in SecurityBuffer inputBuffer);

	int ApplyControlToken(ref SafeDeleteContext refContext, in SecurityBuffer inputBuffer);
}
