using System.ComponentModel;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[Obsolete("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TripleDESCryptoServiceProvider : TripleDES
{
	private readonly TripleDES _impl;

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

	public override int BlockSize
	{
		get
		{
			return _impl.BlockSize;
		}
		set
		{
			_impl.BlockSize = value;
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

	public override KeySizes[] LegalBlockSizes => _impl.LegalBlockSizes;

	public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

	public TripleDESCryptoServiceProvider()
	{
		_impl = TripleDES.Create();
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return _impl.CreateEncryptor();
	}

	public override ICryptoTransform CreateDecryptor()
	{
		return _impl.CreateDecryptor();
	}

	public override void GenerateIV()
	{
		_impl.GenerateIV();
	}

	public override void GenerateKey()
	{
		_impl.GenerateKey();
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return _impl.CreateEncryptor(rgbKey, Internal.Cryptography.Helpers.TrimLargeIV(rgbIV, BlockSize));
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
	{
		return _impl.CreateDecryptor(rgbKey, Internal.Cryptography.Helpers.TrimLargeIV(rgbIV, BlockSize));
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_impl.Dispose();
			base.Dispose(disposing);
		}
	}
}
