using System;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal struct CngAlgorithmCore
{
	private readonly string _disposedName;

	public CngAlgorithm DefaultKeyType;

	private CngKey _lazyKey;

	private bool _disposed;

	public CngAlgorithmCore(string disposedName)
	{
		this = default(CngAlgorithmCore);
		_disposedName = disposedName;
	}

	public static CngKey Duplicate(CngKey key)
	{
		using SafeNCryptKeyHandle keyHandle = key.Handle;
		return CngKey.Open(keyHandle, key.IsEphemeral ? CngKeyHandleOpenOptions.EphemeralKey : CngKeyHandleOpenOptions.None);
	}

	public bool IsKeyGeneratedNamedCurve()
	{
		ThrowIfDisposed();
		if (_lazyKey != null)
		{
			return _lazyKey.IsECNamedCurve();
		}
		return false;
	}

	public void DisposeKey()
	{
		if (_lazyKey != null)
		{
			_lazyKey.Dispose();
			_lazyKey = null;
		}
	}

	public CngKey GetOrGenerateKey(int keySize, CngAlgorithm algorithm)
	{
		ThrowIfDisposed();
		if (_lazyKey != null && _lazyKey.KeySize != keySize)
		{
			DisposeKey();
		}
		if (_lazyKey == null)
		{
			CngKeyCreationParameters cngKeyCreationParameters = new CngKeyCreationParameters
			{
				ExportPolicy = CngExportPolicies.AllowPlaintextExport
			};
			CngProperty item = new CngProperty("Length", BitConverter.GetBytes(keySize), CngPropertyOptions.None);
			cngKeyCreationParameters.Parameters.Add(item);
			_lazyKey = CngKey.Create(algorithm, null, cngKeyCreationParameters);
		}
		return _lazyKey;
	}

	public CngKey GetOrGenerateKey(ECCurve? curve)
	{
		ThrowIfDisposed();
		if (_lazyKey != null)
		{
			return _lazyKey;
		}
		CngKeyCreationParameters cngKeyCreationParameters = new CngKeyCreationParameters
		{
			ExportPolicy = CngExportPolicies.AllowPlaintextExport
		};
		if (curve.Value.IsNamed)
		{
			cngKeyCreationParameters.Parameters.Add(CngKey.GetPropertyFromNamedCurve(curve.Value));
		}
		else
		{
			if (!curve.Value.IsPrime)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.Value.CurveType.ToString()));
			}
			ECCurve curve2 = curve.Value;
			byte[] primeCurveParameterBlob = System.Security.Cryptography.ECCng.GetPrimeCurveParameterBlob(ref curve2);
			CngProperty item = new CngProperty("ECCParameters", primeCurveParameterBlob, CngPropertyOptions.None);
			cngKeyCreationParameters.Parameters.Add(item);
		}
		try
		{
			_lazyKey = CngKey.Create(DefaultKeyType ?? CngAlgorithm.ECDsa, null, cngKeyCreationParameters);
		}
		catch (CryptographicException ex)
		{
			global::Interop.NCrypt.ErrorCode hResult = (global::Interop.NCrypt.ErrorCode)ex.HResult;
			if ((curve.Value.IsNamed && hResult == global::Interop.NCrypt.ErrorCode.NTE_INVALID_PARAMETER) || hResult == global::Interop.NCrypt.ErrorCode.NTE_NOT_SUPPORTED)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.Value.Oid.FriendlyName), ex);
			}
			throw;
		}
		return _lazyKey;
	}

	public void SetKey(CngKey key)
	{
		ThrowIfDisposed();
		DisposeKey();
		_lazyKey = key;
	}

	public void Dispose()
	{
		DisposeKey();
		_disposed = true;
	}

	internal void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(_disposedName);
		}
	}
}
