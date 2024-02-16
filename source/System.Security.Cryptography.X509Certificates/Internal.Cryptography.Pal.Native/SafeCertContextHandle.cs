using System;

namespace Internal.Cryptography.Pal.Native;

internal class SafeCertContextHandle : SafePointerHandle<SafeCertContextHandle>
{
	private SafeCertContextHandle _parent;

	public unsafe CERT_CONTEXT* CertContext => (CERT_CONTEXT*)(void*)handle;

	public bool HasPersistedPrivateKey => CertHasProperty(CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID);

	public bool HasEphemeralPrivateKey => CertHasProperty(CertContextPropId.CERT_KEY_CONTEXT_PROP_ID);

	public bool ContainsPrivateKey
	{
		get
		{
			if (!HasPersistedPrivateKey)
			{
				return HasEphemeralPrivateKey;
			}
			return true;
		}
	}

	public SafeCertContextHandle()
	{
	}

	public SafeCertContextHandle(SafeCertContextHandle parent)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		bool success = false;
		parent.DangerousAddRef(ref success);
		_parent = parent;
		SetHandle(_parent.handle);
	}

	internal new void SetHandle(IntPtr handle)
	{
		base.SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		if (_parent != null)
		{
			_parent.DangerousRelease();
			_parent = null;
		}
		else
		{
			global::Interop.Crypt32.CertFreeCertificateContext(handle);
		}
		SetHandle(IntPtr.Zero);
		return true;
	}

	public unsafe CERT_CONTEXT* Disconnect()
	{
		CERT_CONTEXT* result = (CERT_CONTEXT*)(void*)handle;
		SetHandle(IntPtr.Zero);
		return result;
	}

	public SafeCertContextHandle Duplicate()
	{
		return global::Interop.crypt32.CertDuplicateCertificateContext(handle);
	}

	private bool CertHasProperty(CertContextPropId propertyId)
	{
		int pcbData = 0;
		return global::Interop.crypt32.CertGetCertificateContextProperty(this, propertyId, null, ref pcbData);
	}
}
