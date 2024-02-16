using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net;

internal static class CertificateValidation
{
	internal unsafe static SslPolicyErrors BuildChainAndVerifyProperties(X509Chain chain, X509Certificate2 remoteCertificate, bool checkCertName, bool isServer, string hostName)
	{
		SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
		bool flag = chain.Build(remoteCertificate);
		if (!flag && chain.SafeHandle.DangerousGetHandle() == IntPtr.Zero)
		{
			throw new CryptographicException(Marshal.GetLastPInvokeError());
		}
		if (checkCertName)
		{
			uint num = 0u;
			global::Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA sSL_EXTRA_CERT_CHAIN_POLICY_PARA = default(global::Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA);
			sSL_EXTRA_CERT_CHAIN_POLICY_PARA.cbSize = (uint)sizeof(global::Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA);
			sSL_EXTRA_CERT_CHAIN_POLICY_PARA.dwAuthType = (isServer ? 1u : 2u);
			sSL_EXTRA_CERT_CHAIN_POLICY_PARA.fdwChecks = 0u;
			sSL_EXTRA_CERT_CHAIN_POLICY_PARA.pwszServerName = null;
			global::Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA sSL_EXTRA_CERT_CHAIN_POLICY_PARA2 = sSL_EXTRA_CERT_CHAIN_POLICY_PARA;
			global::Interop.Crypt32.CERT_CHAIN_POLICY_PARA cERT_CHAIN_POLICY_PARA = default(global::Interop.Crypt32.CERT_CHAIN_POLICY_PARA);
			cERT_CHAIN_POLICY_PARA.cbSize = (uint)sizeof(global::Interop.Crypt32.CERT_CHAIN_POLICY_PARA);
			cERT_CHAIN_POLICY_PARA.dwFlags = 0u;
			cERT_CHAIN_POLICY_PARA.pvExtraPolicyPara = &sSL_EXTRA_CERT_CHAIN_POLICY_PARA2;
			global::Interop.Crypt32.CERT_CHAIN_POLICY_PARA cpp = cERT_CHAIN_POLICY_PARA;
			fixed (char* pwszServerName = hostName)
			{
				sSL_EXTRA_CERT_CHAIN_POLICY_PARA2.pwszServerName = pwszServerName;
				cpp.dwFlags |= 4031u;
				SafeX509ChainHandle safeHandle = chain.SafeHandle;
				num = Verify(safeHandle, ref cpp);
				if (num == 2148204815u)
				{
					sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
				}
			}
		}
		if (!flag)
		{
			sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
		}
		return sslPolicyErrors;
	}

	private unsafe static uint Verify(SafeX509ChainHandle chainContext, ref global::Interop.Crypt32.CERT_CHAIN_POLICY_PARA cpp)
	{
		global::Interop.Crypt32.CERT_CHAIN_POLICY_STATUS pPolicyStatus = default(global::Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);
		pPolicyStatus.cbSize = (uint)sizeof(global::Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);
		bool flag = global::Interop.Crypt32.CertVerifyCertificateChainPolicy((IntPtr)4, chainContext, ref cpp, ref pPolicyStatus);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(chainContext, $"CertVerifyCertificateChainPolicy returned: {flag}. Status: {pPolicyStatus.dwError}", "Verify");
		}
		return pPolicyStatus.dwError;
	}
}
