using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal sealed class ChainPal : IDisposable, IChainPal
{
	private readonly struct X509ChainErrorMapping
	{
		public readonly CertTrustErrorStatus Win32Flag;

		public readonly int Win32ErrorCode;

		public readonly X509ChainStatusFlags ChainStatusFlag;

		public readonly string Message;

		public X509ChainErrorMapping(CertTrustErrorStatus win32Flag, int win32ErrorCode, X509ChainStatusFlags chainStatusFlag)
		{
			Win32Flag = win32Flag;
			Win32ErrorCode = win32ErrorCode;
			ChainStatusFlag = chainStatusFlag;
			Message = global::Interop.Kernel32.GetMessage(win32ErrorCode);
		}
	}

	private SafeX509ChainHandle _chain;

	private static readonly X509ChainErrorMapping[] s_x509ChainErrorMappings = new X509ChainErrorMapping[23]
	{
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_SIGNATURE_VALID, -2146869244, X509ChainStatusFlags.NotSignatureValid),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_SIGNATURE_VALID, -2146869244, X509ChainStatusFlags.CtlNotSignatureValid),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_UNTRUSTED_ROOT, -2146762487, X509ChainStatusFlags.UntrustedRoot),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_PARTIAL_CHAIN, -2146762486, X509ChainStatusFlags.PartialChain),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_REVOKED, -2146885616, X509ChainStatusFlags.Revoked),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_VALID_FOR_USAGE, -2146762480, X509ChainStatusFlags.NotValidForUsage),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_VALID_FOR_USAGE, -2146762480, X509ChainStatusFlags.CtlNotValidForUsage),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_VALID, -2146762495, X509ChainStatusFlags.NotTimeValid),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_TIME_VALID, -2146762495, X509ChainStatusFlags.CtlNotTimeValid),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_NAME_CONSTRAINTS, -2146762476, X509ChainStatusFlags.InvalidNameConstraints),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_NAME_CONSTRAINT, -2146762476, X509ChainStatusFlags.HasNotSupportedNameConstraint),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_DEFINED_NAME_CONSTRAINT, -2146762476, X509ChainStatusFlags.HasNotDefinedNameConstraint),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_PERMITTED_NAME_CONSTRAINT, -2146762476, X509ChainStatusFlags.HasNotPermittedNameConstraint),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_EXCLUDED_NAME_CONSTRAINT, -2146762476, X509ChainStatusFlags.HasExcludedNameConstraint),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_POLICY_CONSTRAINTS, -2146762477, X509ChainStatusFlags.InvalidPolicyConstraints),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_NO_ISSUANCE_CHAIN_POLICY, -2146762477, X509ChainStatusFlags.NoIssuanceChainPolicy),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_BASIC_CONSTRAINTS, -2146869223, X509ChainStatusFlags.InvalidBasicConstraints),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_NESTED, -2146762494, X509ChainStatusFlags.NotTimeNested),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_REVOCATION_STATUS_UNKNOWN, -2146885614, X509ChainStatusFlags.RevocationStatusUnknown),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_OFFLINE_REVOCATION, -2146885613, X509ChainStatusFlags.OfflineRevocation),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_EXPLICIT_DISTRUST, -2146762479, X509ChainStatusFlags.ExplicitDistrust),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT, -2146762491, X509ChainStatusFlags.HasNotSupportedCriticalExtension),
		new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_WEAK_SIGNATURE, -2146877418, X509ChainStatusFlags.HasWeakSignature)
	};

	public unsafe X509ChainElement[] ChainElements
	{
		get
		{
			CERT_CHAIN_CONTEXT* ptr = (CERT_CHAIN_CONTEXT*)(void*)_chain.DangerousGetHandle();
			CERT_SIMPLE_CHAIN* rgpChain = *ptr->rgpChain;
			X509ChainElement[] array = new X509ChainElement[rgpChain->cElement];
			for (int i = 0; i < rgpChain->cElement; i++)
			{
				CERT_CHAIN_ELEMENT* ptr2 = rgpChain->rgpElement[i];
				X509Certificate2 certificate = new X509Certificate2((IntPtr)ptr2->pCertContext);
				X509ChainStatus[] chainStatusInformation = GetChainStatusInformation(ptr2->TrustStatus.dwErrorStatus);
				string information = Marshal.PtrToStringUni(ptr2->pwszExtendedErrorInfo);
				X509ChainElement x509ChainElement = new X509ChainElement(certificate, chainStatusInformation, information);
				array[i] = x509ChainElement;
			}
			GC.KeepAlive(this);
			return array;
		}
	}

	public unsafe X509ChainStatus[] ChainStatus
	{
		get
		{
			CERT_CHAIN_CONTEXT* ptr = (CERT_CHAIN_CONTEXT*)(void*)_chain.DangerousGetHandle();
			X509ChainStatus[] chainStatusInformation = GetChainStatusInformation(ptr->TrustStatus.dwErrorStatus);
			GC.KeepAlive(this);
			return chainStatusInformation;
		}
	}

	public SafeX509ChainHandle SafeHandle => _chain;

	public unsafe static ChainPal BuildChain(bool useMachineContext, ICertificatePal cert, X509Certificate2Collection extraStore, OidCollection applicationPolicy, OidCollection certificatePolicy, X509RevocationMode revocationMode, X509RevocationFlag revocationFlag, X509Certificate2Collection customTrustStore, X509ChainTrustMode trustMode, DateTime verificationTime, TimeSpan timeout, bool disableAia)
	{
		CertificatePal certificatePal = (CertificatePal)cert;
		using SafeChainEngineHandle safeChainEngineHandle = GetChainEngine(trustMode, customTrustStore, useMachineContext);
		using SafeCertStoreHandle hStore = ConvertStoreToSafeHandle(extraStore);
		CERT_CHAIN_PARA pChainPara = default(CERT_CHAIN_PARA);
		pChainPara.cbSize = Marshal.SizeOf<CERT_CHAIN_PARA>();
		int numOids;
		using SafeHandle safeHandle = applicationPolicy.ToLpstrArray(out numOids);
		if (!safeHandle.IsInvalid)
		{
			pChainPara.RequestedUsage.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
			pChainPara.RequestedUsage.Usage.cUsageIdentifier = numOids;
			pChainPara.RequestedUsage.Usage.rgpszUsageIdentifier = safeHandle.DangerousGetHandle();
		}
		int numOids2;
		using SafeHandle safeHandle2 = certificatePolicy.ToLpstrArray(out numOids2);
		if (!safeHandle2.IsInvalid)
		{
			pChainPara.RequestedIssuancePolicy.dwType = CertUsageMatchType.USAGE_MATCH_TYPE_AND;
			pChainPara.RequestedIssuancePolicy.Usage.cUsageIdentifier = numOids2;
			pChainPara.RequestedIssuancePolicy.Usage.rgpszUsageIdentifier = safeHandle2.DangerousGetHandle();
		}
		pChainPara.dwUrlRetrievalTimeout = (int)Math.Floor(timeout.TotalMilliseconds);
		FILETIME fILETIME = FILETIME.FromDateTime(verificationTime);
		CertChainFlags dwFlags = MapRevocationFlags(revocationMode, revocationFlag, disableAia);
		if (!global::Interop.crypt32.CertGetCertificateChain(safeChainEngineHandle.DangerousGetHandle(), certificatePal.CertContext, &fILETIME, hStore, ref pChainPara, dwFlags, IntPtr.Zero, out var ppChainContext))
		{
			return null;
		}
		return new ChainPal(ppChainContext);
	}

	private static SafeChainEngineHandle GetChainEngine(X509ChainTrustMode trustMode, X509Certificate2Collection customTrustStore, bool useMachineContext)
	{
		if (trustMode == X509ChainTrustMode.CustomRootTrust)
		{
			using (SafeCertStoreHandle safeCertStoreHandle = ConvertStoreToSafeHandle(customTrustStore, returnEmptyHandle: true))
			{
				CERT_CHAIN_ENGINE_CONFIG config = default(CERT_CHAIN_ENGINE_CONFIG);
				config.cbSize = Marshal.SizeOf<CERT_CHAIN_ENGINE_CONFIG>();
				config.hExclusiveRoot = safeCertStoreHandle.DangerousGetHandle();
				return global::Interop.crypt32.CertCreateCertificateChainEngine(ref config);
			}
		}
		return useMachineContext ? SafeChainEngineHandle.MachineChainEngine : SafeChainEngineHandle.UserChainEngine;
	}

	private static SafeCertStoreHandle ConvertStoreToSafeHandle(X509Certificate2Collection extraStore, bool returnEmptyHandle = false)
	{
		if ((extraStore == null || extraStore.Count == 0) && !returnEmptyHandle)
		{
			return SafePointerHandle<SafeCertStoreHandle>.InvalidHandle;
		}
		return ((StorePal)StorePal.LinkFromCertificateCollection(extraStore)).SafeCertStoreHandle;
	}

	private static CertChainFlags MapRevocationFlags(X509RevocationMode revocationMode, X509RevocationFlag revocationFlag, bool disableAia)
	{
		CertChainFlags certChainFlags = (disableAia ? (CertChainFlags.CERT_CHAIN_DISABLE_AUTH_ROOT_AUTO_UPDATE | CertChainFlags.CERT_CHAIN_DISABLE_AIA) : CertChainFlags.None);
		switch (revocationMode)
		{
		case X509RevocationMode.NoCheck:
			return certChainFlags;
		case X509RevocationMode.Offline:
			certChainFlags |= CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CACHE_ONLY;
			break;
		}
		return revocationFlag switch
		{
			X509RevocationFlag.EndCertificateOnly => certChainFlags | CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_END_CERT, 
			X509RevocationFlag.EntireChain => certChainFlags | CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN, 
			_ => certChainFlags | CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT, 
		};
	}

	private ChainPal(SafeX509ChainHandle chain)
	{
		_chain = chain;
	}

	public static IChainPal FromHandle(IntPtr chainContext)
	{
		if (chainContext == IntPtr.Zero)
		{
			throw new ArgumentNullException("chainContext");
		}
		SafeX509ChainHandle safeX509ChainHandle = global::Interop.crypt32.CertDuplicateCertificateChain(chainContext);
		if (safeX509ChainHandle == null || safeX509ChainHandle.IsInvalid)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidContextHandle, "chainContext");
		}
		return new ChainPal(safeX509ChainHandle);
	}

	public unsafe bool? Verify(X509VerificationFlags flags, out Exception exception)
	{
		exception = null;
		CERT_CHAIN_POLICY_PARA pPolicyPara = default(CERT_CHAIN_POLICY_PARA);
		pPolicyPara.cbSize = sizeof(CERT_CHAIN_POLICY_PARA);
		pPolicyPara.dwFlags = (int)flags;
		CERT_CHAIN_POLICY_STATUS pPolicyStatus = default(CERT_CHAIN_POLICY_STATUS);
		pPolicyStatus.cbSize = sizeof(CERT_CHAIN_POLICY_STATUS);
		if (!global::Interop.crypt32.CertVerifyCertificateChainPolicy(ChainPolicy.CERT_CHAIN_POLICY_BASE, _chain, ref pPolicyPara, ref pPolicyStatus))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			exception = lastWin32Error.ToCryptographicException();
			return null;
		}
		return pPolicyStatus.dwError == 0;
	}

	public static bool ReleaseSafeX509ChainHandle(IntPtr handle)
	{
		global::Interop.crypt32.CertFreeCertificateChain(handle);
		return true;
	}

	public void Dispose()
	{
		SafeX509ChainHandle chain = _chain;
		_chain = null;
		chain?.Dispose();
	}

	private static X509ChainStatus[] GetChainStatusInformation(CertTrustErrorStatus dwStatus)
	{
		if (dwStatus == CertTrustErrorStatus.CERT_TRUST_NO_ERROR)
		{
			return Array.Empty<X509ChainStatus>();
		}
		int num = 0;
		for (uint num2 = (uint)dwStatus; num2 != 0; num2 >>= 1)
		{
			if ((num2 & (true ? 1u : 0u)) != 0)
			{
				num++;
			}
		}
		X509ChainStatus[] array = new X509ChainStatus[num];
		int num3 = 0;
		X509ChainErrorMapping[] array2 = s_x509ChainErrorMappings;
		for (int i = 0; i < array2.Length; i++)
		{
			X509ChainErrorMapping x509ChainErrorMapping = array2[i];
			if ((dwStatus & x509ChainErrorMapping.Win32Flag) != 0)
			{
				array[num3].StatusInformation = x509ChainErrorMapping.Message;
				array[num3].Status = x509ChainErrorMapping.ChainStatusFlag;
				num3++;
				dwStatus &= ~x509ChainErrorMapping.Win32Flag;
			}
		}
		int num4 = 0;
		for (uint num5 = (uint)dwStatus; num5 != 0; num5 >>= 1)
		{
			if ((num5 & (true ? 1u : 0u)) != 0)
			{
				array[num3].Status = (X509ChainStatusFlags)(1 << num4);
				array[num3].StatusInformation = System.SR.Unknown_Error;
				num3++;
			}
			num4++;
		}
		return array;
	}
}
