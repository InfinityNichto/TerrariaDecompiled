using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Xml;

public sealed class XmlDictionaryReaderQuotas
{
	private bool _readOnly;

	private int _maxStringContentLength;

	private int _maxArrayLength;

	private int _maxDepth;

	private int _maxNameTableCharCount;

	private int _maxBytesPerRead;

	private XmlDictionaryReaderQuotaTypes _modifiedQuotas;

	private const int DefaultMaxDepth = 32;

	private const int DefaultMaxStringContentLength = 8192;

	private const int DefaultMaxArrayLength = 16384;

	private const int DefaultMaxBytesPerRead = 4096;

	private const int DefaultMaxNameTableCharCount = 16384;

	private static readonly XmlDictionaryReaderQuotas s_maxQuota = new XmlDictionaryReaderQuotas(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, XmlDictionaryReaderQuotaTypes.MaxDepth | XmlDictionaryReaderQuotaTypes.MaxStringContentLength | XmlDictionaryReaderQuotaTypes.MaxArrayLength | XmlDictionaryReaderQuotaTypes.MaxBytesPerRead | XmlDictionaryReaderQuotaTypes.MaxNameTableCharCount);

	public static XmlDictionaryReaderQuotas Max => s_maxQuota;

	[DefaultValue(8192)]
	public int MaxStringContentLength
	{
		get
		{
			return _maxStringContentLength;
		}
		set
		{
			if (_readOnly)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.QuotaIsReadOnly, "MaxStringContentLength")));
			}
			if (value <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.QuotaMustBePositive, "value"));
			}
			_maxStringContentLength = value;
			_modifiedQuotas |= XmlDictionaryReaderQuotaTypes.MaxStringContentLength;
		}
	}

	[DefaultValue(16384)]
	public int MaxArrayLength
	{
		get
		{
			return _maxArrayLength;
		}
		set
		{
			if (_readOnly)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.QuotaIsReadOnly, "MaxArrayLength")));
			}
			if (value <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.QuotaMustBePositive, "value"));
			}
			_maxArrayLength = value;
			_modifiedQuotas |= XmlDictionaryReaderQuotaTypes.MaxArrayLength;
		}
	}

	[DefaultValue(4096)]
	public int MaxBytesPerRead
	{
		get
		{
			return _maxBytesPerRead;
		}
		set
		{
			if (_readOnly)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.QuotaIsReadOnly, "MaxBytesPerRead")));
			}
			if (value <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.QuotaMustBePositive, "value"));
			}
			_maxBytesPerRead = value;
			_modifiedQuotas |= XmlDictionaryReaderQuotaTypes.MaxBytesPerRead;
		}
	}

	[DefaultValue(32)]
	public int MaxDepth
	{
		get
		{
			return _maxDepth;
		}
		set
		{
			if (_readOnly)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.QuotaIsReadOnly, "MaxDepth")));
			}
			if (value <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.QuotaMustBePositive, "value"));
			}
			_maxDepth = value;
			_modifiedQuotas |= XmlDictionaryReaderQuotaTypes.MaxDepth;
		}
	}

	[DefaultValue(16384)]
	public int MaxNameTableCharCount
	{
		get
		{
			return _maxNameTableCharCount;
		}
		set
		{
			if (_readOnly)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.QuotaIsReadOnly, "MaxNameTableCharCount")));
			}
			if (value <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.QuotaMustBePositive, "value"));
			}
			_maxNameTableCharCount = value;
			_modifiedQuotas |= XmlDictionaryReaderQuotaTypes.MaxNameTableCharCount;
		}
	}

	public XmlDictionaryReaderQuotaTypes ModifiedQuotas => _modifiedQuotas;

	public XmlDictionaryReaderQuotas()
	{
		_maxDepth = 32;
		_maxStringContentLength = 8192;
		_maxArrayLength = 16384;
		_maxBytesPerRead = 4096;
		_maxNameTableCharCount = 16384;
	}

	private XmlDictionaryReaderQuotas(int maxDepth, int maxStringContentLength, int maxArrayLength, int maxBytesPerRead, int maxNameTableCharCount, XmlDictionaryReaderQuotaTypes modifiedQuotas)
	{
		_maxDepth = maxDepth;
		_maxStringContentLength = maxStringContentLength;
		_maxArrayLength = maxArrayLength;
		_maxBytesPerRead = maxBytesPerRead;
		_maxNameTableCharCount = maxNameTableCharCount;
		_modifiedQuotas = modifiedQuotas;
		MakeReadOnly();
	}

	public void CopyTo(XmlDictionaryReaderQuotas quotas)
	{
		if (quotas == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("quotas"));
		}
		if (quotas._readOnly)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.QuotaCopyReadOnly));
		}
		InternalCopyTo(quotas);
	}

	internal void InternalCopyTo(XmlDictionaryReaderQuotas quotas)
	{
		quotas._maxStringContentLength = _maxStringContentLength;
		quotas._maxArrayLength = _maxArrayLength;
		quotas._maxDepth = _maxDepth;
		quotas._maxNameTableCharCount = _maxNameTableCharCount;
		quotas._maxBytesPerRead = _maxBytesPerRead;
		quotas._modifiedQuotas = _modifiedQuotas;
	}

	internal void MakeReadOnly()
	{
		_readOnly = true;
	}
}
