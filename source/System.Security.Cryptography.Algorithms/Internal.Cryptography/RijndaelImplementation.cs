using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

[Obsolete("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId = "SYSLIB0022", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
internal sealed class RijndaelImplementation : Rijndael
{
	private readonly Aes _impl;

	public override int BlockSize
	{
		get
		{
			return _impl.BlockSize;
		}
		set
		{
			switch (value)
			{
			case 192:
			case 256:
				throw new PlatformNotSupportedException(System.SR.Cryptography_Rijndael_BlockSize);
			default:
				throw new CryptographicException(System.SR.Cryptography_Rijndael_BlockSize);
			case 128:
				break;
			}
		}
	}

	public override int FeedbackSize
	{
		get
		{
			return _impl.FeedbackSize;
		}
		set
		{
			_impl.FeedbackSize = value;
		}
	}

	public override byte[] IV
	{
		get
		{
			return _impl.IV;
		}
		set
		{
			_impl.IV = value;
		}
	}

	public override byte[] Key
	{
		get
		{
			return _impl.Key;
		}
		set
		{
			_impl.Key = value;
		}
	}

	public override int KeySize
	{
		get
		{
			return _impl.KeySize;
		}
		set
		{
			_impl.KeySize = value;
		}
	}

	public override CipherMode Mode
	{
		get
		{
			return _impl.Mode;
		}
		set
		{
			_impl.Mode = value;
		}
	}

	public override PaddingMode Padding
	{
		get
		{
			return _impl.Padding;
		}
		set
		{
			_impl.Padding = value;
		}
	}

	public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

	internal RijndaelImplementation()
	{
		LegalBlockSizesValue = new KeySizes[1]
		{
			new KeySizes(128, 128, 0)
		};
		_impl = Aes.Create();
		_impl.FeedbackSize = 128;
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return _impl.CreateEncryptor();
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return _impl.CreateEncryptor(rgbKey, rgbIV);
	}

	public override ICryptoTransform CreateDecryptor()
	{
		return _impl.CreateDecryptor();
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return _impl.CreateDecryptor(rgbKey, rgbIV);
	}

	public override void GenerateIV()
	{
		_impl.GenerateIV();
	}

	public override void GenerateKey()
	{
		_impl.GenerateKey();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_impl.Dispose();
		}
	}
}
