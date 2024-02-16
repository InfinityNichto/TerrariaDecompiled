using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal struct CngSymmetricAlgorithmCore
{
	private readonly ICngSymmetricAlgorithm _outer;

	private string _keyName;

	private readonly CngProvider _provider;

	private readonly CngKeyOpenOptions _optionOptions;

	private bool KeyInPlainText => _keyName == null;

	public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer)
	{
		_outer = outer;
		_keyName = null;
		_provider = null;
		_optionOptions = CngKeyOpenOptions.None;
	}

	public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer, string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
	{
		if (keyName == null)
		{
			throw new ArgumentNullException("keyName");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		_outer = outer;
		_keyName = keyName;
		_provider = provider;
		_optionOptions = openOptions;
		using CngKey cngKey = ProduceCngKey();
		CngAlgorithm algorithm = cngKey.Algorithm;
		string nCryptAlgorithmIdentifier = _outer.GetNCryptAlgorithmIdentifier();
		if (nCryptAlgorithmIdentifier != algorithm.Algorithm)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CngKeyWrongAlgorithm, algorithm.Algorithm, nCryptAlgorithmIdentifier));
		}
		_outer.BaseKeySize = cngKey.KeySize;
	}

	public byte[] GetKeyIfExportable()
	{
		if (KeyInPlainText)
		{
			return _outer.BaseKey;
		}
		using CngKey cngKey = ProduceCngKey();
		return cngKey.GetSymmetricKeyDataIfExportable(_outer.GetNCryptAlgorithmIdentifier());
	}

	public void SetKey(byte[] key)
	{
		_outer.BaseKey = key;
		_keyName = null;
	}

	public void SetKeySize(int keySize, ICngSymmetricAlgorithm outer)
	{
		outer.BaseKeySize = keySize;
		_keyName = null;
	}

	public void GenerateKey()
	{
		byte[] key = Internal.Cryptography.Helpers.GenerateRandom(_outer.BaseKeySize.BitSizeToByteSize());
		SetKey(key);
	}

	public void GenerateIV()
	{
		byte[] iV = Internal.Cryptography.Helpers.GenerateRandom(_outer.BlockSize.BitSizeToByteSize());
		_outer.IV = iV;
	}

	public ICryptoTransform CreateEncryptor()
	{
		return CreateCryptoTransform(encrypting: true);
	}

	public ICryptoTransform CreateDecryptor()
	{
		return CreateCryptoTransform(encrypting: false);
	}

	public ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateCryptoTransform(rgbKey, rgbIV, encrypting: true, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	public ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateCryptoTransform(rgbKey, rgbIV, encrypting: false, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	private ICryptoTransform CreateCryptoTransform(bool encrypting)
	{
		if (KeyInPlainText)
		{
			return CreateCryptoTransform(_outer.BaseKey, _outer.IV, encrypting, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
		}
		return CreatePersistedCryptoTransformCore(ProduceCngKey, _outer.IV, encrypting, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	public Internal.Cryptography.UniversalCryptoTransform CreateCryptoTransform(byte[] iv, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		if (KeyInPlainText)
		{
			return CreateCryptoTransform(_outer.BaseKey, iv, encrypting, padding, mode, feedbackSizeInBits);
		}
		return CreatePersistedCryptoTransformCore(ProduceCngKey, iv, encrypting, padding, mode, feedbackSizeInBits);
	}

	private Internal.Cryptography.UniversalCryptoTransform CreateCryptoTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		if (rgbKey == null)
		{
			throw new ArgumentNullException("rgbKey");
		}
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		byte[] array = rgbKey.CloneByteArray();
		long num = (long)array.Length * 8L;
		if (num > int.MaxValue || !((int)num).IsLegalSize(_outer.LegalKeySizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (_outer.IsWeakKey(array))
		{
			throw new CryptographicException(System.SR.Cryptography_WeakKey);
		}
		if (rgbIV != null && rgbIV.Length != _outer.BlockSize.BitSizeToByteSize())
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
		}
		byte[] iv = mode.GetCipherIv(rgbIV).CloneByteArray();
		array = _outer.PreprocessKey(array);
		return CreateEphemeralCryptoTransformCore(array, iv, encrypting, padding, mode, feedbackSizeInBits);
	}

	private Internal.Cryptography.UniversalCryptoTransform CreateEphemeralCryptoTransformCore(byte[] key, byte[] iv, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		int blockSizeInBytes = _outer.BlockSize.BitSizeToByteSize();
		Internal.NativeCrypto.SafeAlgorithmHandle ephemeralModeHandle = _outer.GetEphemeralModeHandle(mode, feedbackSizeInBits);
		Internal.Cryptography.BasicSymmetricCipher cipher = new Internal.Cryptography.BasicSymmetricCipherBCrypt(ephemeralModeHandle, mode, blockSizeInBytes, _outer.GetPaddingSize(mode, feedbackSizeInBits), key, ownsParentHandle: false, iv, encrypting);
		return Internal.Cryptography.UniversalCryptoTransform.Create(padding, cipher, encrypting);
	}

	private Internal.Cryptography.UniversalCryptoTransform CreatePersistedCryptoTransformCore(Func<CngKey> cngKeyFactory, byte[] iv, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		int blockSizeInBytes = _outer.BlockSize.BitSizeToByteSize();
		Internal.Cryptography.BasicSymmetricCipher cipher = new BasicSymmetricCipherNCrypt(cngKeyFactory, mode, blockSizeInBytes, iv, encrypting, _outer.GetPaddingSize(mode, feedbackSizeInBits));
		return Internal.Cryptography.UniversalCryptoTransform.Create(padding, cipher, encrypting);
	}

	private CngKey ProduceCngKey()
	{
		return CngKey.Open(_keyName, _provider, _optionOptions);
	}

	private void ValidateFeedbackSize(CipherMode mode, int feedbackSizeInBits)
	{
		if (mode != CipherMode.CFB)
		{
			return;
		}
		if (KeyInPlainText)
		{
			if (!_outer.IsValidEphemeralFeedbackSize(feedbackSizeInBits))
			{
				throw new CryptographicException(string.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedbackSizeInBits, CipherMode.CFB));
			}
		}
		else if (feedbackSizeInBits != 8)
		{
			throw new CryptographicException(string.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedbackSizeInBits, CipherMode.CFB));
		}
	}
}
